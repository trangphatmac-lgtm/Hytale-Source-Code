using System.Collections.Generic;
using HytaleClient.AssetEditor.Interface.Modals;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.AssetEditor.Interface.MainMenu;

internal class AssetEditorMainMenuView : Group
{
	public readonly AssetEditorInterface Interface;

	private TextButton _cosmeticsEditorButton;

	private Label _connectionStatusLabel;

	private Label _connectionErrorLabel;

	private TextButton _cancelConnectionButton;

	private TextButton _closeDisconnectPopupButton;

	private TextButton _reconnectButton;

	private Group _disconnectPopup;

	private Label _disconnectReason;

	private Group _serverList;

	private TextButton _addServerButton;

	private TextButton _editServerButton;

	private TextButton _removeServerButton;

	private TextButton.TextButtonStyle _buttonStyle;

	private TextButton.TextButtonStyle _buttonStyleSelected;

	private int _selectedServerIndex = -1;

	private readonly ServerModal _serverModal;

	private readonly ConfirmationModal _confirmationModal;

	public AssetEditorMainMenuView(AssetEditorInterface @interface)
		: base(@interface.Desktop, null)
	{
		Interface = @interface;
		_serverModal = new ServerModal(@interface);
		_confirmationModal = new ConfirmationModal(Desktop, null);
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("MainMenu/ServerButton.ui", out var document);
		_buttonStyle = document.ResolveNamedValue<TextButton.TextButtonStyle>(Interface, "Style");
		_buttonStyleSelected = document.ResolveNamedValue<TextButton.TextButtonStyle>(Interface, "StyleSelected");
		Interface.TryGetDocument("MainMenu/MainMenu.ui", out var document2);
		UIFragment uIFragment = document2.Instantiate(Desktop, this);
		_cosmeticsEditorButton = uIFragment.Get<TextButton>("CosmeticsEditorButton");
		_cosmeticsEditorButton.Activating = delegate
		{
			Interface.App.Editor.OpenCosmeticsEditor();
		};
		_serverList = uIFragment.Get<Group>("ServerList");
		_connectionStatusLabel = uIFragment.Get<Label>("ConnectionStatusLabel");
		_connectionStatusLabel.Visible = false;
		_connectionErrorLabel = uIFragment.Get<Label>("ConnectionErrorLabel");
		_connectionErrorLabel.Visible = false;
		_cancelConnectionButton = uIFragment.Get<TextButton>("CancelConnectionButton");
		_cancelConnectionButton.Visible = false;
		_cancelConnectionButton.Activating = delegate
		{
			Interface.App.MainMenu.CancelConnection();
		};
		_disconnectPopup = uIFragment.Get<Group>("DisconnectPopup");
		_disconnectPopup.Visible = false;
		_disconnectReason = uIFragment.Get<Label>("DisconnectReason");
		_reconnectButton = uIFragment.Get<TextButton>("ReconnectButton");
		_reconnectButton.Activating = delegate
		{
			if (!Interface.App.MainMenu.IsConnectingToServer)
			{
				Interface.App.MainMenu.Reconnect();
			}
		};
		_closeDisconnectPopupButton = uIFragment.Get<TextButton>("CloseDisconnectPopupButton");
		_closeDisconnectPopupButton.Activating = delegate
		{
			Interface.App.MainMenu.CloseDisconnectPopup();
		};
		uIFragment.Get<TextButton>("SettingsButton").Activating = delegate
		{
			Interface.SettingsModal.Open();
		};
		_addServerButton = uIFragment.Get<TextButton>("AddServerButton");
		_addServerButton.Activating = delegate
		{
			_serverModal.Open();
		};
		_editServerButton = uIFragment.Get<TextButton>("EditServerButton");
		_editServerButton.Visible = false;
		_editServerButton.Activating = delegate
		{
			_serverModal.Open(_selectedServerIndex, Interface.App.MainMenu.Servers[_selectedServerIndex]);
		};
		_removeServerButton = uIFragment.Get<TextButton>("RemoveServerButton");
		_removeServerButton.Visible = false;
		_removeServerButton.Activating = delegate
		{
			string text = Desktop.Provider.GetText("ui.assetEditor.mainMenu.removeServerModal.title", new Dictionary<string, string> { 
			{
				"serverName",
				Interface.App.MainMenu.Servers[_selectedServerIndex].Name
			} });
			_confirmationModal.Open(text, "", delegate
			{
				Interface.App.MainMenu.RemoveServer(_selectedServerIndex);
			}, null, Desktop.Provider.GetText("ui.general.remove"));
		};
		uIFragment.Get<Element>("CosmeticsEditorButton").Visible = true;
		uIFragment.Get<Element>("CosmeticsEditorButtonSeparator").Visible = true;
		BuildServerList(doLayout: false);
		_serverModal.Build();
		_confirmationModal.Build();
	}

	protected override void OnMounted()
	{
		if (!Interface.HasMarkupError)
		{
			BuildServerList(doLayout: false);
			UpdateEditButtons(doLayout: false);
		}
	}

	protected override void OnUnmounted()
	{
		UpdateDisconnectPopup();
		UpdateConnectionStatus();
		_selectedServerIndex = -1;
	}

	public void BuildServerList(bool doLayout = true)
	{
		_selectedServerIndex = -1;
		Interface.TryGetDocument("MainMenu/ServerButton.ui", out var document);
		IReadOnlyList<AssetEditorAppMainMenu.Server> servers = Interface.App.MainMenu.Servers;
		_serverList.Clear();
		for (int i = 0; i < servers.Count; i++)
		{
			int index = i;
			AssetEditorAppMainMenu.Server server = servers[index];
			UIFragment uIFragment = document.Instantiate(Desktop, _serverList);
			TextButton button = uIFragment.Get<TextButton>("Button");
			button.Text = server.Name;
			button.Activating = delegate
			{
				OnServerButtonActivating(button, index, forceSelect: false);
			};
			button.DoubleClicking = delegate
			{
				OnServerButtonActivating(button, index, forceSelect: true);
				Interface.App.MainMenu.ConnectToServer(index);
			};
		}
		UpdateEditButtons(doLayout: false);
		if (doLayout)
		{
			Layout();
		}
	}

	private void OnServerButtonActivating(TextButton button, int index, bool forceSelect)
	{
		if (_selectedServerIndex != -1)
		{
			TextButton textButton = (TextButton)_serverList.Children[_selectedServerIndex];
			textButton.Style = _buttonStyle;
			textButton.Layout();
		}
		if (!forceSelect && _selectedServerIndex == index)
		{
			_selectedServerIndex = -1;
			UpdateEditButtons();
			return;
		}
		_selectedServerIndex = index;
		button.Style = _buttonStyleSelected;
		button.Layout();
		UpdateEditButtons();
	}

	private void UpdateEditButtons(bool doLayout = true)
	{
		_editServerButton.Visible = _selectedServerIndex != -1;
		_removeServerButton.Visible = _selectedServerIndex != -1;
		if (doLayout)
		{
			Layout();
		}
	}

	public void UpdateDisconnectPopup()
	{
		AssetEditorAppMainMenu mainMenu = Interface.App.MainMenu;
		_disconnectPopup.Visible = mainMenu.DisplayDisconnectPopup;
		if (mainMenu.DisplayDisconnectPopup)
		{
			_disconnectReason.Text = mainMenu.ServerDisconnectReason ?? "";
		}
		if (base.IsMounted)
		{
			Layout();
		}
	}

	public void UpdateConnectionStatus(bool doLayout = true)
	{
		AssetEditorAppMainMenu mainMenu = Interface.App.MainMenu;
		bool isConnectingToServer = mainMenu.IsConnectingToServer;
		string connectionErrorMessage = mainMenu.ConnectionErrorMessage;
		_cosmeticsEditorButton.Disabled = isConnectingToServer;
		foreach (Element child in _serverList.Children)
		{
			if (child is TextButton textButton)
			{
				textButton.Disabled = isConnectingToServer;
			}
		}
		_connectionStatusLabel.Visible = isConnectingToServer;
		_connectionErrorLabel.Visible = !isConnectingToServer && connectionErrorMessage != null;
		_cancelConnectionButton.Visible = isConnectingToServer;
		if (isConnectingToServer)
		{
			if (mainMenu.ConnectionStage == AssetEditorAppMainMenu.ConnectionStages.Authenticating)
			{
				_connectionStatusLabel.Text = Desktop.Provider.GetText("ui.assetEditor.mainMenu.connection.authenticating");
			}
			else
			{
				_connectionStatusLabel.Text = Desktop.Provider.GetText("ui.assetEditor.mainMenu.connection.connecting");
			}
		}
		else if (connectionErrorMessage != null)
		{
			_connectionErrorLabel.Text = Desktop.Provider.GetText(connectionErrorMessage);
		}
		if (doLayout)
		{
			Layout();
		}
	}
}
