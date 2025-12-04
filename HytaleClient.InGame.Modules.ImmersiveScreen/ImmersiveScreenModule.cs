#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Hypixel.ProtoPlus;
using HytaleClient.Graphics;
using HytaleClient.InGame.Modules.ImmersiveScreen.Data;
using HytaleClient.InGame.Modules.ImmersiveScreen.Screens;
using HytaleClient.Interface.CoherentUI;
using HytaleClient.Math;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.ImmersiveScreen;

internal class ImmersiveScreenModule : Module
{
	public class MediaDataEvent
	{
		public int MaxResults;

		public string Query;

		public string PageToken;

		public int Page;

		public string Id;

		public string Nonce;
	}

	private readonly List<BaseImmersiveScreen> _screens = new List<BaseImmersiveScreen>();

	public readonly QuadRenderer CoUIQuadRenderer;

	public readonly WebView CoUIWebView;

	public readonly GLTexture CoUIWebViewTexture;

	private YouTubeDataRequest _youTubeSearchRequest;

	private YouTubeDataRequest _youTubeMostPopularVideosRequest;

	private TwitchDataRequest _twitchMostPopularVideosRequest;

	private TwitchDataRequest _twitchSearchRequest;

	public ImmersiveWebScreen ActiveWebScreen { get; private set; }

	public int GetScreenCount()
	{
		return _screens.Count;
	}

	public ImmersiveScreenModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		GraphicsDevice graphics = _gameInstance.Engine.Graphics;
		GLFunctions gL = graphics.GL;
		CoUIQuadRenderer = new QuadRenderer(graphics, graphics.GPUProgramStore.BasicProgram.AttribPosition, graphics.GPUProgramStore.BasicProgram.AttribTexCoords);
		CoUIWebView = new WebView(_gameInstance.Engine, _gameInstance.App.CoUIManager, "blank", 360, 360, 1f);
		CoUIWebViewTexture = gL.GenTexture();
		gL.BindTexture(GL.TEXTURE_2D, CoUIWebViewTexture);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 33071);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 33071);
		CoUIWebView.RegisterForEvent("immersiveScreens.mediaData.request", this, delegate
		{
			ActiveWebScreen?.SendMediaData();
		});
	}

	protected override void DoDispose()
	{
		CoUIWebView.UnregisterFromEvent("immersiveScreens.mediaData.request");
		_youTubeSearchRequest?.Dispose();
		_youTubeMostPopularVideosRequest?.Dispose();
		_twitchMostPopularVideosRequest?.Dispose();
		_twitchSearchRequest?.Dispose();
		for (int i = 0; i < _screens.Count; i++)
		{
			_screens[i].Dispose();
		}
		GLFunctions gL = _gameInstance.Engine.Graphics.GL;
		gL.DeleteTexture(CoUIWebViewTexture);
		_gameInstance.App.CoUIManager.RunInThread(delegate
		{
			CoUIWebView.Destroy();
			_gameInstance.Engine.RunOnMainThread(_gameInstance.Engine, delegate
			{
				CoUIWebView.Dispose();
			});
		});
		CoUIQuadRenderer.Dispose();
	}

	public void Update(float deltaTime)
	{
		float num = float.PositiveInfinity;
		ImmersiveWebScreen immersiveWebScreen = null;
		foreach (BaseImmersiveScreen screen in _screens)
		{
			screen.Update(deltaTime);
			float num2 = Vector3.Distance(screen.GetOffsetPosition(), _gameInstance.LocalPlayer.Position);
			if (num2 <= screen.MaxVisibilityDistance && screen is ImmersiveWebScreen && num2 < num)
			{
				num = num2;
				immersiveWebScreen = (ImmersiveWebScreen)screen;
			}
		}
		if (ActiveWebScreen != immersiveWebScreen)
		{
			if (ActiveWebScreen != null && (immersiveWebScreen == null || ActiveWebScreen.BlockPosition != immersiveWebScreen.BlockPosition))
			{
				ActiveWebScreen.OnDeactivate();
			}
			ActiveWebScreen = immersiveWebScreen;
			if (ActiveWebScreen != null)
			{
				float scale = _gameInstance.App.Interface.Desktop.Scale;
				CoUIWebView.Resize((int)(ActiveWebScreen.ScreenSizeInPixels.X * scale), (int)(ActiveWebScreen.ScreenSizeInPixels.Y * scale), scale);
				string url = ActiveWebScreen.GetUrl();
				if (url != null)
				{
					CoUIWebView.LoadURL(url);
				}
				ActiveWebScreen.OnActivate();
			}
			else
			{
				CoUIWebView.SetVolume(0.0);
				CoUIWebView.LoadURL("blank");
			}
		}
		if (ActiveWebScreen != null)
		{
			float num3 = MathHelper.Clamp(1f - num / ActiveWebScreen.MaxSoundDistance, 0f, 1f);
			CoUIWebView.SetVolume(num3);
		}
	}

	public bool NeedsDrawing()
	{
		foreach (BaseImmersiveScreen screen in _screens)
		{
			if (screen.NeedsDrawing())
			{
				return true;
			}
		}
		return false;
	}

	public void PrepareForDraw(ref Matrix viewProjectionMatrix)
	{
		if (!NeedsDrawing())
		{
			throw new Exception("PrepareForDraw called when it was not required. Please check with NeedsDrawing() first before calling this.");
		}
		foreach (BaseImmersiveScreen screen in _screens)
		{
			if (screen.NeedsDrawing())
			{
				screen.PrepareForDraw(ref viewProjectionMatrix);
			}
		}
	}

	public void Draw()
	{
		if (!NeedsDrawing())
		{
			throw new Exception("Draw called when it was not required. Please check with NeedsDrawing() first before calling this.");
		}
		foreach (BaseImmersiveScreen screen in _screens)
		{
			if (screen.NeedsDrawing())
			{
				screen.Draw();
			}
		}
	}

	public void HandleUpdatePacket(UpdateImmersiveView packet)
	{
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Invalid comparison between Unknown and I4
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Invalid comparison between Unknown and I4
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Invalid comparison between Unknown and I4
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Invalid comparison between Unknown and I4
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Invalid comparison between Unknown and I4
		if (packet.View == null)
		{
			return;
		}
		Vector3 vector = new Vector3(packet.X, packet.Y, packet.Z);
		BaseImmersiveScreen screenAtBlockPosition = GetScreenAtBlockPosition(vector);
		if (screenAtBlockPosition != null)
		{
			if ((int)packet.UpdateType_ != 2)
			{
				ImmersiveViewType viewType = packet.View.Screen.ViewType;
				ImmersiveViewType val = viewType;
				if ((int)val > 1)
				{
					if ((int)val == 2 && screenAtBlockPosition is ImmersiveWebScreen immersiveWebScreen)
					{
						immersiveWebScreen.SetViewData(packet.View);
						return;
					}
				}
				else if (screenAtBlockPosition is ImmersiveImageScreen immersiveImageScreen)
				{
					immersiveImageScreen.SetViewData(packet.View);
					return;
				}
			}
			screenAtBlockPosition.Dispose();
			_screens.Remove(screenAtBlockPosition);
		}
		if ((int)packet.UpdateType_ == 2)
		{
			return;
		}
		try
		{
			ImmersiveViewType viewType2 = packet.View.Screen.ViewType;
			ImmersiveViewType val2 = viewType2;
			if ((int)val2 > 1)
			{
				if ((int)val2 == 2)
				{
					ImmersiveWebScreen immersiveWebScreen2 = new ImmersiveWebScreen(_gameInstance, vector, packet.View.Screen);
					immersiveWebScreen2.SetViewData(packet.View);
					_screens.Add(immersiveWebScreen2);
				}
			}
			else
			{
				ImmersiveImageScreen immersiveImageScreen2 = new ImmersiveImageScreen(_gameInstance, vector, packet.View.Screen);
				immersiveImageScreen2.SetViewData(packet.View);
				_screens.Add(immersiveImageScreen2);
			}
		}
		catch (Exception arg)
		{
			_gameInstance.App.DevTools.Error($"Error creating {packet.View.Screen.ViewType} view at {vector} - {arg}");
		}
	}

	private BaseImmersiveScreen GetScreenAtBlockPosition(Vector3 blockPosition)
	{
		for (int i = 0; i < _screens.Count; i++)
		{
			if (_screens[i].BlockPosition == blockPosition)
			{
				return _screens[i];
			}
		}
		return null;
	}

	public void SearchYouTube(MediaDataEvent e)
	{
		_youTubeSearchRequest?.Dispose();
		_youTubeSearchRequest = new YouTubeDataRequest(delegate(JObject data, string err)
		{
			_gameInstance.Engine.RunOnMainThread(this, delegate
			{
				_youTubeSearchRequest.Dispose();
				_youTubeSearchRequest = null;
				if (err != null)
				{
					_gameInstance.App.Interface.InGameView.MediaBrowserPage.OnSearchError(e.Nonce, err);
				}
				else
				{
					_gameInstance.App.Interface.InGameView.MediaBrowserPage.OnYouTubeSearchResults(e.Nonce, ((object)data).ToString());
				}
			});
		});
		string text = GetYouTubeVideoId(e.Query);
		if (!string.IsNullOrEmpty(text))
		{
			_youTubeSearchRequest.GetVideo(text);
		}
		else
		{
			_youTubeSearchRequest.SearchVideos(e.Query, e.MaxResults, e.PageToken);
		}
		static string GetYouTubeVideoId(string input)
		{
			Regex regex = new Regex("(?:.+?)?(?:\\/v\\/|watch\\/|\\?v=|\\&v=|youtu\\.be\\/|\\/v=|^youtu\\.be\\/)([a-zA-Z0-9_-]{11})+", RegexOptions.Compiled);
			foreach (Match item in regex.Matches(input))
			{
				using IEnumerator<Group> enumerator2 = (from Group groupdata in item.Groups
					where !groupdata.ToString().StartsWith("http://") && !groupdata.ToString().StartsWith("https://") && !groupdata.ToString().StartsWith("youtu") && !groupdata.ToString().StartsWith("www.")
					select groupdata).GetEnumerator();
				if (enumerator2.MoveNext())
				{
					Group current = enumerator2.Current;
					return current.ToString();
				}
			}
			return "";
		}
	}

	public void GetMostPopularYouTubeVideos(MediaDataEvent e)
	{
		_youTubeMostPopularVideosRequest?.Dispose();
		_youTubeMostPopularVideosRequest = new YouTubeDataRequest(delegate(JObject data, string err)
		{
			_gameInstance.Engine.RunOnMainThread(this, delegate
			{
				_youTubeMostPopularVideosRequest.Dispose();
				_youTubeMostPopularVideosRequest = null;
				if (err != null)
				{
					_gameInstance.App.Interface.InGameView.MediaBrowserPage.OnGetPopularContentError(e.Nonce, err);
				}
				else
				{
					_gameInstance.App.Interface.InGameView.MediaBrowserPage.OnGetYouTubePopularVideos(e.Nonce, ((object)data).ToString());
				}
			});
		});
		_youTubeMostPopularVideosRequest.OnGetPopularVideos(e.MaxResults);
	}

	public void GetMostPopularTwitchStreams(MediaDataEvent e)
	{
		_twitchMostPopularVideosRequest?.Dispose();
		_twitchMostPopularVideosRequest = new TwitchDataRequest(delegate(JObject data, string err)
		{
			_gameInstance.Engine.RunOnMainThread(this, delegate
			{
				_twitchMostPopularVideosRequest.Dispose();
				_twitchMostPopularVideosRequest = null;
				if (err != null)
				{
					_gameInstance.App.Interface.InGameView.MediaBrowserPage.OnGetPopularContentError(e.Nonce, err);
				}
				else
				{
					_gameInstance.App.Interface.InGameView.MediaBrowserPage.OnGetTwitchPopularStreams(e.Nonce, ((object)data).ToString());
				}
			});
		});
		_twitchMostPopularVideosRequest.GetPopularStreams(e.MaxResults);
	}

	public void SearchTwitch(MediaDataEvent e)
	{
		_twitchSearchRequest?.Dispose();
		_twitchSearchRequest = new TwitchDataRequest(delegate(JObject data, string err)
		{
			_gameInstance.Engine.RunOnMainThread(this, delegate
			{
				_twitchSearchRequest.Dispose();
				_twitchSearchRequest = null;
				Trace.WriteLine(((object)data)?.ToString());
				if (err != null)
				{
					_gameInstance.App.Interface.InGameView.MediaBrowserPage.OnSearchError(e.Nonce, err);
				}
				else
				{
					_gameInstance.App.Interface.InGameView.MediaBrowserPage.OnTwitchSearchResults(e.Nonce, ((object)data).ToString());
				}
			});
		});
		_twitchSearchRequest.SearchStreams(e.Query, e.MaxResults, e.Page);
	}

	public void TriggerMediaAction(ImmersiveViewMediaAction action, ClientMediaData media)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected O, but got Unknown
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		if (_gameInstance.ImmersiveScreenModule.ActiveWebScreen != null)
		{
			Vector3 blockPosition = _gameInstance.ImmersiveScreenModule.ActiveWebScreen.BlockPosition;
			_gameInstance.Connection.SendPacket((ProtoPacket)new ImmersiveViewUpdateMedia(new BlockPosition((int)blockPosition.X, (int)blockPosition.Y, (int)blockPosition.Z), action, media?.ToPacket()));
		}
	}
}
