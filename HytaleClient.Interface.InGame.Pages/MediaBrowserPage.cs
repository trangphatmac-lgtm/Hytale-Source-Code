using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using HytaleClient.Graphics;
using HytaleClient.InGame.Modules.ImmersiveScreen;
using HytaleClient.InGame.Modules.ImmersiveScreen.Data;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Protocol;
using NLog;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.Interface.InGame.Pages;

internal class MediaBrowserPage : InterfaceComponent
{
	private enum ContentType
	{
		Popular,
		Search
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private static int NextNonce;

	private readonly InGameView _inGameView;

	private Label _contentTitle;

	private Group _mediaInfo;

	private Label _mediaTitle;

	private Label _mediaChannel;

	private Label _mediaLiveBadge;

	private CompactTextField _searchInput;

	private Group _results;

	private Group _spinnerContainer;

	private Button _currentTimeBar;

	private Group _currentTimeBarValue;

	private Button _playPauseButton;

	private Label _currentTimeLabel;

	private Group _youtubeLogo;

	private Group _twitchLogo;

	private PatchStyle _playButtonBackground;

	private PatchStyle _pauseButtonBackground;

	private ClientMediaData _currentMediaData;

	private int _targetCurrentTimeBarWidth = -1;

	private string _contentLoadNonce;

	private bool _isContentLoading;

	private bool _initializeCurrentTimeBar;

	private ContentType _activeContentType = ContentType.Popular;

	private MediaService _activeContentPlatform = (MediaService)1;

	private readonly List<ExternalTextureLoader> _thumbnailTextureLoaders = new List<ExternalTextureLoader>();

	private List<TextureArea> _thumbnailTextureAreas = new List<TextureArea>();

	public MediaBrowserPage(InGameView inGameView)
		: base(inGameView.Interface, null)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		_inGameView = inGameView;
	}

	public void Build()
	{
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		Clear();
		DisposeThumbnails();
		Interface.TryGetDocument("InGame/Pages/MediaBrowserPage.ui", out var document);
		_playButtonBackground = document.ResolveNamedValue<PatchStyle>(Interface, "PlayButtonBackground");
		_pauseButtonBackground = document.ResolveNamedValue<PatchStyle>(Interface, "PauseButtonBackground");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_mediaInfo = uIFragment.Get<Group>("MediaInfo");
		_contentTitle = uIFragment.Get<Label>("ContentTitle");
		_searchInput = uIFragment.Get<CompactTextField>("SearchField");
		_searchInput.KeyDown = OnSearchInputKeyPress;
		_searchInput.ValueChanged = delegate
		{
			if (_searchInput.Value.Trim() == "" && _activeContentType == ContentType.Search)
			{
				ShowPopularContent(doLayout: true);
			}
		};
		_results = uIFragment.Get<Group>("Results");
		_results.Visible = !_isContentLoading;
		_spinnerContainer = uIFragment.Get<Group>("SpinnerContainer");
		_spinnerContainer.Visible = _isContentLoading;
		_currentTimeLabel = uIFragment.Get<Label>("CurrentTimeLabel");
		_currentTimeBar = uIFragment.Get<Button>("CurrentTimeBar");
		_currentTimeBar.Activating = OnCurrentTimeBarActivate;
		_currentTimeBarValue = uIFragment.Get<Group>("CurrentTimeBarValue");
		_currentTimeBarValue.Anchor.Width = 0;
		_mediaTitle = uIFragment.Get<Label>("MediaTitle");
		_mediaChannel = uIFragment.Get<Label>("MediaChannel");
		_playPauseButton = uIFragment.Get<Button>("PlayPauseButton");
		_playPauseButton.Activating = OnPlayPauseButtonActivate;
		uIFragment.Get<Button>("StopButton").Activating = OnStopActivate;
		_mediaLiveBadge = uIFragment.Get<Label>("MediaLiveBadge");
		_twitchLogo = uIFragment.Get<Group>("TwitchLogo");
		_youtubeLogo = uIFragment.Get<Group>("YouTubeLogo");
		TabNavigation platformTabs = uIFragment.Get<TabNavigation>("PlatformTabs");
		TabNavigation tabNavigation = platformTabs;
		TabNavigation.Tab[] array = new TabNavigation.Tab[2];
		TabNavigation.Tab tab = new TabNavigation.Tab();
		MediaService val = (MediaService)1;
		tab.Id = ((object)(MediaService)(ref val)).ToString();
		tab.Icon = new PatchStyle("InGame/Pages/TwitchIcon.png");
		array[0] = tab;
		TabNavigation.Tab tab2 = new TabNavigation.Tab();
		val = (MediaService)0;
		tab2.Id = ((object)(MediaService)(ref val)).ToString();
		tab2.Icon = new PatchStyle("InGame/Pages/YouTubeIcon.png");
		array[1] = tab2;
		tabNavigation.Tabs = array;
		TabNavigation tabNavigation2 = platformTabs;
		val = (MediaService)1;
		tabNavigation2.SelectedTab = ((object)(MediaService)(ref val)).ToString();
		platformTabs.SelectedTabChanged = delegate
		{
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			SetContentPlatform((MediaService)Enum.Parse(typeof(MediaService), platformTabs.SelectedTab));
			Layout();
		};
		SetContentPlatform((MediaService)1);
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
		_initializeCurrentTimeBar = true;
		ShowPopularContent(doLayout: true);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	protected override void AfterChildrenLayout()
	{
		if (_initializeCurrentTimeBar)
		{
			_initializeCurrentTimeBar = false;
			if (_currentMediaData != null)
			{
				_currentTimeBarValue.Anchor.Width = (_targetCurrentTimeBarWidth = GetCurrentTimeBarWidth());
				_currentTimeBarValue.Layout();
			}
		}
	}

	private void Animate(float dt)
	{
		if (_targetCurrentTimeBarWidth != -1 && _targetCurrentTimeBarWidth != _currentTimeBarValue.Anchor.Width)
		{
			_currentTimeBarValue.Anchor.Width = (int)MathHelper.Lerp(_currentTimeBarValue.Anchor.Width.Value, _targetCurrentTimeBarWidth, System.Math.Min(dt * 20f, 1f));
			_currentTimeBarValue.Layout();
		}
	}

	private void SetContentPlatform(MediaService platform)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		_activeContentPlatform = platform;
		_twitchLogo.Visible = (int)platform == 1;
		_youtubeLogo.Visible = (int)platform == 0;
		_searchInput.Value = "";
		if (base.IsMounted)
		{
			ShowPopularContent(doLayout: false);
		}
	}

	private int GetCurrentTimeBarWidth()
	{
		float num = ((_currentMediaData.Duration > 0) ? ((float)_currentMediaData.Position / (float)_currentMediaData.Duration) : 0f);
		return (int)(num * (float)Desktop.UnscaleRound(_currentTimeBar.AnchoredRectangle.Width));
	}

	private void OnSearchInputKeyPress(SDL_Keycode key)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Invalid comparison between Unknown and I4
		if ((int)key == 13 && !(_searchInput.Value.Trim() == "") && !_isContentLoading)
		{
			PerformSearch(_searchInput.Value, doLayout: true);
		}
	}

	private void OnCurrentTimeBarActivate()
	{
		if (_currentMediaData != null && _currentMediaData.Duration > 0)
		{
			float num = (float)(Desktop.MousePosition.X - _currentTimeBar.AnchoredRectangle.X) / (float)_currentTimeBar.AnchoredRectangle.Width;
			TriggerMediaAction((ImmersiveViewMediaAction)4, new ClientMediaData
			{
				Position = (int)(num * (float)_currentMediaData.Duration)
			});
		}
	}

	private void OnPlayPauseButtonActivate()
	{
		if (_currentMediaData != null)
		{
			TriggerMediaAction((ImmersiveViewMediaAction)(_currentMediaData.Playing ? 1 : 0));
		}
	}

	private void OnStopActivate()
	{
		if (_currentMediaData != null)
		{
			TriggerMediaAction((ImmersiveViewMediaAction)2);
		}
	}

	public void OnImmersiveViewDataUpdated(ClientMediaData data)
	{
		_currentMediaData = data;
		if (_currentMediaData?.Id != null)
		{
			if (base.IsMounted)
			{
				_targetCurrentTimeBarWidth = GetCurrentTimeBarWidth();
			}
			_playPauseButton.Background = (_currentMediaData.Playing ? _pauseButtonBackground : _playButtonBackground);
			_playPauseButton.Layout();
			_mediaTitle.Text = ((object)(MediaService)(ref _currentMediaData.Platform)).ToString() + ": " + _currentMediaData.Title;
			_mediaTitle.Layout();
			_mediaChannel.Text = _currentMediaData.ChannelName;
			_mediaChannel.Layout();
			TimeSpan timeSpan = TimeSpan.FromSeconds(_currentMediaData.Duration);
			TimeSpan timeSpan2 = TimeSpan.FromSeconds(_currentMediaData.Position);
			_currentTimeLabel.Text = $"{timeSpan2:m\\:ss} / {timeSpan:m\\:ss}";
			_currentTimeLabel.Layout();
			_mediaLiveBadge.Visible = _currentMediaData.Stream;
			_mediaLiveBadge.Parent.Layout();
			if (!_mediaInfo.Visible)
			{
				_mediaInfo.Visible = true;
				Layout();
			}
		}
		else
		{
			_currentTimeBarValue.Anchor.Width = 0;
			_currentTimeBarValue.Layout();
			_playPauseButton.Background = _playButtonBackground;
			_playPauseButton.Layout();
			_mediaTitle.Text = "";
			_mediaTitle.Layout();
			_currentTimeLabel.Text = "0:00 / 0:00";
			_currentTimeLabel.Layout();
			_mediaLiveBadge.Visible = false;
			if (_mediaInfo.Visible)
			{
				_mediaInfo.Visible = false;
				Layout();
			}
		}
	}

	private void ShowPopularContent(bool doLayout)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Invalid comparison between Unknown and I4
		_activeContentType = ContentType.Popular;
		_contentTitle.Text = Interface.GetText(((int)_activeContentPlatform == 1) ? "ui.mediaBrowser.popularStreams" : "ui.mediaBrowser.popularVideos");
		_contentLoadNonce = (++NextNonce).ToString();
		_spinnerContainer.Visible = true;
		_results.Clear();
		_results.Visible = false;
		DisposeThumbnails();
		if (doLayout)
		{
			Layout();
		}
		ImmersiveScreenModule.MediaDataEvent e = new ImmersiveScreenModule.MediaDataEvent
		{
			MaxResults = 25,
			Nonce = _contentLoadNonce
		};
		if ((int)_activeContentPlatform == 1)
		{
			_inGameView.InGame.Instance.ImmersiveScreenModule.GetMostPopularTwitchStreams(e);
		}
		else
		{
			_inGameView.InGame.Instance.ImmersiveScreenModule.GetMostPopularYouTubeVideos(e);
		}
	}

	private void PerformSearch(string query, bool doLayout)
	{
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Invalid comparison between Unknown and I4
		_activeContentType = ContentType.Search;
		_contentTitle.Text = Interface.GetText("ui.mediaBrowser.search", new Dictionary<string, string> { { "query", query } });
		_isContentLoading = true;
		int num = ++NextNonce;
		_contentLoadNonce = num.ToString();
		_spinnerContainer.Visible = true;
		_results.Clear();
		_results.Visible = false;
		DisposeThumbnails();
		if (doLayout)
		{
			Layout();
		}
		ImmersiveScreenModule.MediaDataEvent e = new ImmersiveScreenModule.MediaDataEvent
		{
			MaxResults = 50,
			Query = _searchInput.Value.Trim(),
			Nonce = _contentLoadNonce
		};
		if ((int)_activeContentPlatform == 1)
		{
			_inGameView.InGame.Instance.ImmersiveScreenModule.SearchTwitch(e);
		}
		else
		{
			_inGameView.InGame.Instance.ImmersiveScreenModule.SearchYouTube(e);
		}
	}

	private void DisposeThumbnails()
	{
		foreach (ExternalTextureLoader thumbnailTextureLoader in _thumbnailTextureLoaders)
		{
			thumbnailTextureLoader.Cancel();
		}
		_thumbnailTextureLoaders.Clear();
		foreach (TextureArea thumbnailTextureArea in _thumbnailTextureAreas)
		{
			thumbnailTextureArea.Texture.Dispose();
		}
		_thumbnailTextureAreas.Clear();
	}

	private void BuildStreamList(List<ClientMediaData> items)
	{
		Interface.TryGetDocument("InGame/Pages/MediaBrowserListingEntry.ui", out var document);
		foreach (ClientMediaData media in items)
		{
			try
			{
				UIFragment uIFragment = document.Instantiate(Desktop, null);
				uIFragment.Get<Label>("Title").Text = media.Title;
				uIFragment.Get<Label>("ChannelName").Text = media.ChannelName;
				uIFragment.Get<Label>("Duration").Visible = false;
				uIFragment.Get<Label>("UploadDate").Visible = false;
				uIFragment.Get<Label>("ViewCount").Text = Interface.FormatNumber(media.ViewCount);
				uIFragment.Get<Label>("LiveBadge").Visible = media.Stream;
				if (media.Duration > 0)
				{
					TimeSpan timeSpan = TimeSpan.FromSeconds(media.Duration);
					uIFragment.Get<Label>("Duration").Visible = true;
					uIFragment.Get<Label>("Duration").Text = $"{timeSpan:m\\:ss}";
				}
				if (media.GameTitle != null)
				{
					uIFragment.Get<Label>("GameTitle").Visible = true;
					uIFragment.Get<Label>("GameTitle").Text = media.GameTitle;
				}
				uIFragment.Get<Button>("Button").Activating = delegate
				{
					TriggerMediaAction((ImmersiveViewMediaAction)3, media);
				};
				foreach (Element rootElement in uIFragment.RootElements)
				{
					_results.Add(rootElement);
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to add element for video.");
				Logger.Error<ClientMediaData>(media);
			}
		}
	}

	public void OnSearchError(string nonce, string error)
	{
		if (!(_contentLoadNonce != nonce))
		{
			_contentLoadNonce = null;
			_isContentLoading = false;
			Logger.Error("Failed to search content: {error}", error);
			_results.Clear();
			new Label(Desktop, _results).Text = "Failed to perform search.";
			_results.Layout();
		}
	}

	public void OnGetPopularContentError(string nonce, string error)
	{
		if (!(_contentLoadNonce != nonce))
		{
			_contentLoadNonce = null;
			_isContentLoading = false;
			Logger.Error("Failed to fetch content: {error}", error);
			_results.Clear();
			new Label(Desktop, _results).Text = "Failed to fetch content.";
			_results.Layout();
		}
	}

	private void TriggerMediaAction(ImmersiveViewMediaAction action, ClientMediaData video = null)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		_inGameView.InGame.Instance.ImmersiveScreenModule.TriggerMediaAction(action, video);
	}

	public void ResetState()
	{
		_activeContentType = ContentType.Popular;
		_searchInput.Value = "";
		_mediaTitle.Text = "";
		_mediaLiveBadge.Visible = false;
		_currentTimeBarValue.Anchor.Width = 0;
		_playPauseButton.Background = _playButtonBackground;
		_results.Clear();
		_results.Visible = true;
		_spinnerContainer.Visible = false;
		_isContentLoading = false;
		_contentLoadNonce = null;
		DisposeThumbnails();
	}

	public void OnGetTwitchPopularStreams(string nonce, string json)
	{
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		if (_contentLoadNonce != nonce)
		{
			return;
		}
		_contentLoadNonce = null;
		_spinnerContainer.Visible = false;
		_results.Visible = true;
		JObject val = JObject.Parse(json);
		_isContentLoading = false;
		JArray val2 = Extensions.Value<JArray>((IEnumerable<JToken>)val["data"]);
		List<ClientMediaData> list = new List<ClientMediaData>();
		foreach (JToken item in val2)
		{
			try
			{
				ClientMediaData obj = new ClientMediaData
				{
					Id = Extensions.Value<string>((IEnumerable<JToken>)item[(object)"id"]),
					Title = Extensions.Value<string>((IEnumerable<JToken>)item[(object)"title"]),
					Duration = 0,
					ChannelId = Extensions.Value<string>((IEnumerable<JToken>)item[(object)"user_id"]),
					ChannelName = Extensions.Value<string>((IEnumerable<JToken>)item[(object)"user_name"])
				};
				JToken obj2 = item[(object)"game_title"];
				obj.GameTitle = ((obj2 != null) ? Extensions.Value<string>((IEnumerable<JToken>)obj2) : null);
				obj.PublicationDate = "";
				obj.Position = 0;
				obj.Stream = true;
				obj.Playing = true;
				obj.ViewCount = Extensions.Value<int>((IEnumerable<JToken>)item[(object)"viewer_count"]);
				obj.Platform = (MediaService)1;
				obj.Thumbnail = new ClientMediaData.ClientThumbnails
				{
					Small = Extensions.Value<string>((IEnumerable<JToken>)item[(object)"thumbnail_url"]).Replace("{width}", "200").Replace("{height}", "112"),
					Normal = Extensions.Value<string>((IEnumerable<JToken>)item[(object)"thumbnail_url"]).Replace("{width}", "400").Replace("{height}", "225")
				};
				list.Add(obj);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to add parse stream");
				Logger.Error<JToken>(item);
			}
		}
		BuildStreamList(list);
		Layout();
	}

	public void OnTwitchSearchResults(string nonce, string json)
	{
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		if (_contentLoadNonce != nonce)
		{
			return;
		}
		_contentLoadNonce = null;
		_spinnerContainer.Visible = false;
		_results.Visible = true;
		JObject val = JObject.Parse(json);
		_isContentLoading = false;
		JArray val2 = Extensions.Value<JArray>((IEnumerable<JToken>)val["streams"]);
		List<ClientMediaData> list = new List<ClientMediaData>();
		foreach (JToken item in val2)
		{
			try
			{
				ClientMediaData obj = new ClientMediaData
				{
					Id = Extensions.Value<string>((IEnumerable<JToken>)item[(object)"_id"]),
					Title = Extensions.Value<string>((IEnumerable<JToken>)item[(object)"channel"][(object)"status"]),
					Duration = 0,
					ChannelId = Extensions.Value<string>((IEnumerable<JToken>)item[(object)"channel"][(object)"_id"]),
					ChannelName = Extensions.Value<string>((IEnumerable<JToken>)item[(object)"channel"][(object)"name"]),
					PublicationDate = "",
					Position = 0,
					Stream = true,
					Playing = true
				};
				JToken obj2 = item[(object)"game"];
				obj.GameTitle = ((obj2 != null) ? Extensions.Value<string>((IEnumerable<JToken>)obj2) : null);
				obj.ViewCount = Extensions.Value<int>((IEnumerable<JToken>)item[(object)"viewers"]);
				obj.Platform = (MediaService)1;
				obj.Thumbnail = new ClientMediaData.ClientThumbnails
				{
					Small = Extensions.Value<string>((IEnumerable<JToken>)item[(object)"preview"][(object)"small"]),
					Normal = Extensions.Value<string>((IEnumerable<JToken>)item[(object)"preview"][(object)"medium"])
				};
				list.Add(obj);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to add parse stream");
				Logger.Error<JToken>(item);
			}
		}
		BuildStreamList(list);
		Layout();
	}

	public void OnYouTubeSearchResults(string nonce, string json)
	{
		if (!(_contentLoadNonce != nonce))
		{
			_contentLoadNonce = null;
			_spinnerContainer.Visible = false;
			_results.Visible = true;
			JObject val = JObject.Parse(json);
			_isContentLoading = false;
			List<ClientMediaData> list = new List<ClientMediaData>();
			AddYouTubeVideos(Extensions.Value<JArray>((IEnumerable<JToken>)val["items"]), list);
			BuildStreamList(list);
			Layout();
		}
	}

	public void OnGetYouTubePopularVideos(string nonce, string json)
	{
		if (!(_contentLoadNonce != nonce))
		{
			_contentLoadNonce = null;
			_spinnerContainer.Visible = false;
			_results.Visible = true;
			JObject val = JObject.Parse(json);
			_isContentLoading = false;
			List<ClientMediaData> list = new List<ClientMediaData>();
			AddYouTubeVideos(Extensions.Value<JArray>((IEnumerable<JToken>)val["items"]), list);
			BuildStreamList(list);
			Layout();
		}
	}

	private void AddYouTubeVideos(JArray videos, List<ClientMediaData> list)
	{
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		foreach (JToken video in videos)
		{
			try
			{
				TimeSpan timeSpan = XmlConvert.ToTimeSpan(Extensions.Value<string>((IEnumerable<JToken>)video[(object)"contentDetails"][(object)"duration"]));
				list.Add(new ClientMediaData
				{
					Id = Extensions.Value<string>((IEnumerable<JToken>)video[(object)"id"]),
					Title = WebUtility.HtmlDecode(Extensions.Value<string>((IEnumerable<JToken>)video[(object)"snippet"][(object)"title"])),
					Duration = (int)timeSpan.TotalMilliseconds / 1000,
					ChannelId = Extensions.Value<string>((IEnumerable<JToken>)video[(object)"snippet"][(object)"channelId"]),
					ChannelName = Extensions.Value<string>((IEnumerable<JToken>)video[(object)"snippet"][(object)"channelTitle"]),
					PublicationDate = Extensions.Value<string>((IEnumerable<JToken>)video[(object)"snippet"][(object)"publishedAt"]),
					Position = 0,
					Stream = (Extensions.Value<string>((IEnumerable<JToken>)video[(object)"snippet"][(object)"liveBroadcastContent"]) == "live" || Extensions.Value<string>((IEnumerable<JToken>)video[(object)"snippet"][(object)"liveBroadcastContent"]) == "upcoming"),
					Playing = true,
					ViewCount = Extensions.Value<int>((IEnumerable<JToken>)video[(object)"statistics"][(object)"viewCount"]),
					Platform = (MediaService)0,
					Thumbnail = new ClientMediaData.ClientThumbnails
					{
						Small = Extensions.Value<string>((IEnumerable<JToken>)video[(object)"snippet"][(object)"thumbnails"][(object)"default"][(object)"url"]),
						Normal = Extensions.Value<string>((IEnumerable<JToken>)video[(object)"snippet"][(object)"thumbnails"][(object)"medium"][(object)"url"])
					}
				});
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to add parse stream");
				Logger.Error<JToken>(video);
			}
		}
	}
}
