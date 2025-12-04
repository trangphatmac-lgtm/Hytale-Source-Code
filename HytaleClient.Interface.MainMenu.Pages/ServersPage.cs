using System;
using System.Collections.Generic;
using HytaleClient.Application;
using HytaleClient.Application.Services.Api;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Utils;

namespace HytaleClient.Interface.MainMenu.Pages;

internal class ServersPage : InterfaceComponent
{
	public readonly MainMenuView MainMenuView;

	private TextButton _showInternetServersButton;

	private TextButton _showRecentServersButton;

	private TextButton _showDirectConnectButton;

	private TextButton _showFavoriteServersButton;

	private TextButton _showFriendServersButton;

	private TextButton _serverJoinButton;

	private TextButton _favoriteServerButton;

	private Button.ButtonStyle _serverBrowserRowButtonStyle;

	private Button.ButtonStyle _serverBrowserRowButtonSelectedStyle;

	private TextButton.TextButtonStyle _serverBrowserTopTextButtonStyle;

	private TextButton.TextButtonStyle _serverBrowserTopTextButtonSelectedStyle;

	private TextField _searchTextInput;

	private Group _directConnectPopup;

	private TextField _directConnectAddressTextInput;

	private Label _directConnectPopupStatusLabel;

	private TextButton _directConnectPopupCancelButton;

	private TextButton _directConnectPopupConnectButton;

	private Group _serversTableBody;

	private Group _activeTagsTableBody;

	private Group _serversTableStatus;

	private Element _serversTableLoading;

	private Label _serversTableStatusText;

	private List<string> _tagsSelected;

	private Server[] _servers;

	private Guid _selectedServer;

	private Server _selectedServerDetails;

	private Comparison<Server> _listComparison;

	private Group[] _columnButtonSortCarets;

	private Group[] _columnButtonReverseSortCarets;

	private bool _reverseSort;

	private string _serversErrorMessage;

	private Group _serverDetailsGroup;

	private Label _serverDescriptionLabel;

	private Label _ipLabel;

	private Label _languagesLabel;

	private Label _uuidLabel;

	private Label _onlineLabel;

	private Label _favoriteServerLabel;

	private Label _branchLabel;

	private SoundStyle _joinServerSound;

	private SoundStyle _connectionErrorSound;

	private readonly Dictionary<Guid, Button> _serverButtons = new Dictionary<Guid, Button>();

	public ServersPage(MainMenuView mainMenuView)
		: base(mainMenuView.Interface, null)
	{
		MainMenuView = mainMenuView;
	}

	public void Build()
	{
		_serverButtons.Clear();
		Clear();
		Interface.TryGetDocument("MainMenu/Servers/ServersPage.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_serverBrowserTopTextButtonStyle = document.ResolveNamedValue<TextButton.TextButtonStyle>(Interface, "ServerBrowserTopTextButtonStyle");
		_serverBrowserTopTextButtonSelectedStyle = document.ResolveNamedValue<TextButton.TextButtonStyle>(Interface, "ServerBrowserTopTextButtonSelectedStyle");
		_serverBrowserRowButtonStyle = document.ResolveNamedValue<Button.ButtonStyle>(Interface, "ServerBrowserRowButtonStyle");
		_serverBrowserRowButtonSelectedStyle = document.ResolveNamedValue<Button.ButtonStyle>(Interface, "ServerBrowserRowButtonSelectedStyle");
		_serversTableBody = uIFragment.Get<Group>("ServersTableBody");
		_serversTableLoading = uIFragment.Get<Element>("ServersTableLoading");
		_serversTableStatus = uIFragment.Get<Group>("ServersTableStatus");
		_serversTableStatusText = uIFragment.Get<Label>("ServersTableStatusText");
		_joinServerSound = document.ResolveNamedValue<SoundStyle>(Interface, "JoinServerSound");
		_connectionErrorSound = document.ResolveNamedValue<SoundStyle>(Interface, "ConnectionErrorSound");
		_columnButtonSortCarets = new Group[3];
		_columnButtonReverseSortCarets = new Group[3];
		BuildColumnHeaderButton(0, uIFragment.Get<Button>("NameColumnHeaderButton"), Server.NameSort);
		BuildColumnHeaderButton(1, uIFragment.Get<Button>("RatingColumnHeaderButton"), Server.RatingSort);
		BuildColumnHeaderButton(2, uIFragment.Get<Button>("OnlineColumnHeaderButton"), Server.OnlinePlayersSort);
		SetServerSortOptions(0, Server.NameSort);
		_showInternetServersButton = uIFragment.Get<TextButton>("ShowInternetServersButton");
		_showInternetServersButton.Activating = delegate
		{
			ClearSearchTextInput();
			Interface.App.MainMenu.FetchAndShowPublicServers();
		};
		_showFavoriteServersButton = uIFragment.Get<TextButton>("ShowFavoriteServersButton");
		_showFavoriteServersButton.Activating = delegate
		{
			ClearSearchTextInput();
			Interface.App.MainMenu.FetchAndShowFavoriteServers();
		};
		_showRecentServersButton = uIFragment.Get<TextButton>("ShowRecentServersButton");
		_showRecentServersButton.Activating = delegate
		{
			ClearSearchTextInput();
			Interface.App.MainMenu.FetchAndShowRecentServers();
		};
		_showFriendServersButton = uIFragment.Get<TextButton>("ShowFriendsServersButton");
		_showFriendServersButton.Activating = delegate
		{
			ClearSearchTextInput();
			Interface.App.MainMenu.ShowFriendsServers();
		};
		_searchTextInput = uIFragment.Get<TextField>("ServerSearchField");
		_searchTextInput.ValueChanged = Search;
		_activeTagsTableBody = uIFragment.Get<Group>("ActiveTags");
		_showDirectConnectButton = uIFragment.Get<TextButton>("DirectConnectButton");
		_showDirectConnectButton.Activating = OnDirectConnectButtonActivate;
		Interface.TryGetDocument("MainMenu/Servers/DirectConnectPopup.ui", out var document2);
		UIFragment uIFragment2 = document2.Instantiate(Desktop, null);
		_directConnectPopup = (Group)uIFragment2.RootElements[0];
		_directConnectPopup.Validating = OnDirectConnectPopupValidate;
		_directConnectPopup.Dismissing = OnDirectConnectPopupDismiss;
		_directConnectAddressTextInput = uIFragment2.Get<TextField>("ServerAddress");
		_directConnectPopupStatusLabel = uIFragment2.Get<Label>("StatusLabel");
		_directConnectPopupCancelButton = uIFragment2.Get<TextButton>("CancelButton");
		_directConnectPopupCancelButton.Activating = OnDirectConnectPopupDismiss;
		_directConnectPopupConnectButton = uIFragment2.Get<TextButton>("ConnectButton");
		_directConnectPopupConnectButton.Activating = OnDirectConnectPopupValidate;
		_serverJoinButton = uIFragment.Get<TextButton>("ServerJoin");
		_serverJoinButton.Activating = delegate
		{
			Interface.App.MainMenu.TryConnectToServer(_selectedServerDetails);
			Interface.PlaySound(_joinServerSound);
		};
		_serverDetailsGroup = uIFragment.Get<Group>("ServerDetails");
		_serverDescriptionLabel = uIFragment.Get<Label>("ServerDescriptionCell");
		_ipLabel = uIFragment.Get<Label>("Ip");
		_languagesLabel = uIFragment.Get<Label>("Languages");
		_uuidLabel = uIFragment.Get<Label>("Uuid");
		_onlineLabel = uIFragment.Get<Label>("Online");
		_favoriteServerLabel = uIFragment.Get<Label>("FavoriteServerLabel");
		_branchLabel = uIFragment.Get<Label>("ServerBranch");
		_favoriteServerButton = uIFragment.Get<TextButton>("FavoriteServerButton");
		_favoriteServerButton.Activating = OnActiveFavoriteServerButton;
		_tagsSelected = new List<string>();
		if (base.IsMounted)
		{
			BuildServerList();
		}
		ApplyTabButtonStyles();
		void BuildColumnHeaderButton(int columnIndex, Button button, Comparison<Server> comparison)
		{
			button.Activating = delegate
			{
				OnChangeSort(columnIndex, comparison);
			};
			_columnButtonSortCarets[columnIndex] = button.Find<Group>("SortCaret");
			_columnButtonReverseSortCarets[columnIndex] = button.Find<Group>("ReverseSortCaret");
		}
	}

	protected override void OnMounted()
	{
		MainMenuView.ShowTopBar(showTopBar: true);
		Interface.App.MainMenu.FetchAndShowPublicServers();
		ClearSearchTextInput();
	}

	protected override void OnUnmounted()
	{
		_selectedServerDetails = null;
		Interface.App.MainMenu.CancelFetchServerDetails();
	}

	public void FocusTextSearchInput()
	{
		Desktop.FocusElement(_searchTextInput);
	}

	private void OnDirectConnectButtonActivate()
	{
		_directConnectPopupStatusLabel.Text = "";
		_directConnectPopupStatusLabel.Visible = false;
		_directConnectAddressTextInput.SelectAll();
		Desktop.SetLayer(2, _directConnectPopup);
		Desktop.FocusElement(_directConnectAddressTextInput);
	}

	private void OnActiveFavoriteServerButton()
	{
		if (_selectedServerDetails == null)
		{
			return;
		}
		AppMainMenu mainMenu = Interface.App.MainMenu;
		if (_selectedServerDetails.IsFavorite)
		{
			mainMenu.RemoveServerFromFavorites(_selectedServerDetails.UUID);
			int serverIndex = GetServerIndex(_selectedServerDetails.UUID);
			if (mainMenu.ActiveServerListTab == AppMainMenu.ServerListTab.Favorites && serverIndex > -1)
			{
				ArrayUtils.RemoveAt(ref _servers, serverIndex);
				BuildServerList();
				return;
			}
		}
		else
		{
			mainMenu.AddServerToFavorites(_selectedServerDetails.UUID);
		}
		_selectedServerDetails.IsFavorite = !_selectedServerDetails.IsFavorite;
		BuildServerDetailsPanel();
	}

	private void OnDirectConnectPopupDismiss()
	{
		Desktop.ClearLayer(2);
	}

	private void DirectConnect()
	{
		string text = _directConnectAddressTextInput.Value;
		if (text.Length == 0)
		{
			text = "127.0.0.1";
		}
		if (!Interface.App.MainMenu.CanConnectToServer("connect to " + text, out var reason))
		{
			_directConnectPopupStatusLabel.Text = reason;
			_directConnectPopupStatusLabel.Visible = true;
			_directConnectPopup.Layout();
			return;
		}
		Interface.Logger.Info("Direct connecting to multiplayer server at {0}", text);
		if (!HostnameHelper.TryParseHostname(text, 5520, out var hostname, out var port, out var error))
		{
			Interface.Logger.Warn("Invalid address: {0}", error);
			_directConnectPopupStatusLabel.Text = "Invalid address: " + error;
			_directConnectPopupStatusLabel.Visible = true;
			_directConnectPopup.Layout();
			Interface.PlaySound(_connectionErrorSound);
		}
		else
		{
			Interface.PlaySound(_joinServerSound);
			Interface.FadeOut(delegate
			{
				Interface.App.GameLoading.Open(hostname, port);
				Interface.FadeIn();
			});
		}
	}

	private void OnDirectConnectPopupValidate()
	{
		DirectConnect();
	}

	private void OnChangeSort(int columnIndex, Comparison<Server> comparison)
	{
		SetServerSortOptions(columnIndex, comparison);
		BuildServerList();
	}

	private void SetServerSortOptions(int columnIndex, Comparison<Server> comparison)
	{
		if (_listComparison == comparison)
		{
			_reverseSort = !_reverseSort;
		}
		else
		{
			_listComparison = comparison;
			_reverseSort = false;
		}
		for (int i = 0; i < _columnButtonSortCarets.Length; i++)
		{
			bool flag = i == columnIndex;
			_columnButtonSortCarets[i].Visible = flag && !_reverseSort;
			_columnButtonReverseSortCarets[i].Visible = flag && _reverseSort;
		}
	}

	public void HandleAutoConnectOnStartup(string address)
	{
		if (!Interface.HasMarkupError)
		{
			OnDirectConnectButtonActivate();
			_directConnectAddressTextInput.Value = address;
			DirectConnect();
		}
	}

	public void OnFailedToToggleFavoriteServer(string errorMessage)
	{
		AppMainMenu mainMenu = Interface.App.MainMenu;
		if (base.IsMounted && mainMenu.ActiveServerListTab == AppMainMenu.ServerListTab.Favorites)
		{
			mainMenu.FetchAndShowFavoriteServers();
		}
	}

	public void OnServersReceived(Server[] servers)
	{
		_servers = servers;
		_serversErrorMessage = ((servers == null) ? Desktop.Provider.GetText("ui.mainMenu.servers.failedToFetch") : null);
		BuildServerList();
	}

	public void SetSelectedServerDetails(Server server, bool doLayout = true)
	{
		_selectedServerDetails = server;
		BuildServerDetailsPanel(doLayout);
	}

	private void BuildServerDetailsPanel(bool doLayout = true)
	{
		Server selectedServerDetails = _selectedServerDetails;
		if (selectedServerDetails != null)
		{
			_serverDetailsGroup.Visible = true;
			_serverDescriptionLabel.Text = selectedServerDetails.Description;
			_ipLabel.Text = Desktop.Provider.GetText("ui.mainMenu.servers.details.host", new Dictionary<string, string> { { "host", selectedServerDetails.Host } });
			_languagesLabel.Text = Desktop.Provider.GetText("ui.mainMenu.servers.details.languages", new Dictionary<string, string> { 
			{
				"languages",
				string.Join(", ", selectedServerDetails.Languages)
			} });
			_uuidLabel.Text = Desktop.Provider.GetText("ui.mainMenu.servers.details.uuid", new Dictionary<string, string> { 
			{
				"uuid",
				selectedServerDetails.UUID.ToString()
			} });
			_onlineLabel.Text = Desktop.Provider.GetText("ui.mainMenu.servers.details.online", new Dictionary<string, string> { 
			{
				"online",
				selectedServerDetails.IsOnline.ToString()
			} });
			_branchLabel.Text = Desktop.Provider.GetText("ui.mainMenu.servers.details.branch", new Dictionary<string, string> { { "name", selectedServerDetails.Version } });
			if (selectedServerDetails.IsFavorite)
			{
				_favoriteServerLabel.Text = Desktop.Provider.GetText("ui.mainMenu.servers.details.favoriteYes");
				_favoriteServerButton.Text = Desktop.Provider.GetText("ui.general.remove");
			}
			else
			{
				_favoriteServerLabel.Text = Desktop.Provider.GetText("ui.mainMenu.servers.details.favoriteNo");
				_favoriteServerButton.Text = Desktop.Provider.GetText("ui.general.add");
			}
		}
		else
		{
			_serverDetailsGroup.Visible = false;
		}
		if (doLayout)
		{
			Layout();
		}
	}

	private void BuildServerListRows()
	{
		Interface.TryGetDocument("MainMenu/Servers/ServersTableRow.ui", out var document);
		Interface.TryGetDocument("MainMenu/Servers/ServersRowTags.ui", out var document2);
		Server[] array = (Server[])_servers.Clone();
		Array.Sort(array, _listComparison);
		if (_reverseSort)
		{
			Array.Reverse((Array)array);
		}
		_serverButtons.Clear();
		foreach (Server server in array)
		{
			UIFragment uIFragment = document.Instantiate(Desktop, _serversTableBody);
			Button button = uIFragment.Get<Button>("Row");
			_serverButtons[server.UUID] = button;
			uIFragment.Get<Label>("NameCellLabel").Text = server.Name;
			uIFragment.Get<Label>("RatingCellLabel").Text = "Rating";
			uIFragment.Get<Label>("OnlineCellLabel").Text = $"{server.OnlinePlayers} / {server.MaxPlayers}";
			if (server.UUID.Equals(_selectedServer))
			{
				button.Style = _serverBrowserRowButtonSelectedStyle;
			}
			Group root = uIFragment.Get<Group>("TagsTableBody");
			foreach (string tag in server.Tags)
			{
				UIFragment uIFragment2 = document2.Instantiate(Desktop, root);
				TextButton textButton = uIFragment2.Get<TextButton>("ServerTag");
				textButton.Text = tag;
				textButton.Activating = delegate
				{
					if (!_tagsSelected.Contains(tag))
					{
						_tagsSelected.Add(tag);
						SearchByTag();
					}
				};
			}
			uIFragment.Get<Button>("Row").Activating = delegate
			{
				OnSelectServer(server);
			};
			uIFragment.Get<Button>("Row").DoubleClicking = delegate
			{
				Interface.App.MainMenu.TryConnectToServer(server);
				Interface.PlaySound(_joinServerSound);
			};
		}
	}

	public void BuildServerList()
	{
		if (Interface.HasMarkupError)
		{
			return;
		}
		_selectedServer = Guid.Empty;
		_selectedServerDetails = null;
		_serversTableBody.Clear();
		SetSelectedServerDetails(null, doLayout: false);
		bool isFetchingList = Interface.App.MainMenu.IsFetchingList;
		if (_servers != null && !isFetchingList)
		{
			_serversTableStatus.Visible = false;
			if (_servers.Length != 0)
			{
				_selectedServer = _servers[0].UUID;
				Interface.App.MainMenu.FetchServerDetails(_selectedServer);
			}
			BuildServerListRows();
		}
		else
		{
			_serversTableStatus.Visible = true;
			_serversTableLoading.Visible = isFetchingList;
			if (isFetchingList)
			{
				_serversTableStatusText.Text = Desktop.Provider.GetText("ui.general.loading");
			}
			else
			{
				_serversTableStatusText.Text = _serversErrorMessage ?? "failedToFetchServers";
			}
		}
		Layout();
	}

	private void OnSelectServer(Server server)
	{
		if (Desktop.IsShiftKeyDown)
		{
			Interface.ModalDialog.Setup(new ModalDialog.DialogSetup
			{
				Title = "Reboot server?",
				Text = $"{server.Name}\n{server.OnlinePlayers} / {server.MaxPlayers}",
				OnConfirm = delegate
				{
					Interface.App.MainMenu.RebootServer(server.Host);
				}
			});
			Desktop.SetLayer(4, Interface.ModalDialog);
			return;
		}
		Interface.App.MainMenu.FetchServerDetails(server.UUID);
		if (_selectedServer != Guid.Empty && _serverButtons.TryGetValue(_selectedServer, out var value))
		{
			value.Style = _serverBrowserRowButtonStyle;
		}
		_selectedServer = server.UUID;
		_serverButtons[_selectedServer].Style = _serverBrowserRowButtonSelectedStyle;
		Layout();
	}

	public void OnActiveTabChanged(bool cleanTags = true)
	{
		ApplyTabButtonStyles();
		if (cleanTags)
		{
			_tagsSelected.Clear();
		}
		BuildSearchedTags();
	}

	public void ApplyTabButtonStyles()
	{
		_showInternetServersButton.Style = _serverBrowserTopTextButtonStyle;
		_showFriendServersButton.Style = _serverBrowserTopTextButtonStyle;
		_showFavoriteServersButton.Style = _serverBrowserTopTextButtonStyle;
		_showRecentServersButton.Style = _serverBrowserTopTextButtonStyle;
		switch (Interface.App.MainMenu.ActiveServerListTab)
		{
		case AppMainMenu.ServerListTab.Internet:
			_showInternetServersButton.Style = _serverBrowserTopTextButtonSelectedStyle;
			break;
		case AppMainMenu.ServerListTab.Favorites:
			_showFavoriteServersButton.Style = _serverBrowserTopTextButtonSelectedStyle;
			break;
		case AppMainMenu.ServerListTab.Recent:
			_showRecentServersButton.Style = _serverBrowserTopTextButtonSelectedStyle;
			break;
		}
	}

	public void ClearTags()
	{
		_tagsSelected.Clear();
		BuildSearchedTags();
	}

	private void BuildSearchedTags()
	{
		_activeTagsTableBody.Clear();
		Interface.TryGetDocument("MainMenu/Servers/ActiveSearchedTags.ui", out var document);
		foreach (string tag in _tagsSelected)
		{
			UIFragment uIFragment = document.Instantiate(Desktop, _activeTagsTableBody);
			TextButton textButton = uIFragment.Get<TextButton>("ActiveTag");
			textButton.Text = tag;
			textButton.Activating = delegate
			{
				_tagsSelected.Remove(tag);
				SearchByTag();
			};
		}
		Layout();
	}

	private void ClearSearchTextInput()
	{
		_searchTextInput.Value = "";
	}

	private void SearchByTag()
	{
		ClearSearchTextInput();
		Interface.App.MainMenu.FetchAndShowPublicServers(null, _tagsSelected.ToArray());
		BuildSearchedTags();
	}

	private void Search()
	{
		string text = _searchTextInput.Value.Trim();
		if (text.Length > 2)
		{
			ClearTags();
		}
		Interface.App.MainMenu.FetchAndShowPublicServers(text);
	}

	private int GetServerIndex(Guid uuid)
	{
		for (int i = 0; i < _servers.Length; i++)
		{
			Server server = _servers[i];
			if (server.UUID.Equals(uuid))
			{
				return i;
			}
		}
		return -1;
	}
}
