using System;
using HytaleClient.AssetEditor.Interface.Elements;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.AssetEditor.Interface.MainMenu;

internal class ServerModal : BaseModal
{
	private TextField _nameInput;

	private TextField _hostnameInput;

	private NumberField _portInput;

	private Label _errorLabel;

	private TextButton _saveButton;

	private int _serverIndex;

	public ServerModal(AssetEditorInterface @interface)
		: base(@interface, "MainMenu/ServerModal.ui")
	{
	}

	protected override void BuildModal(Document doc, UIFragment fragment)
	{
		fragment.Get<TextButton>("SaveButton").Activating = Validate;
		fragment.Get<TextButton>("CancelButton").Activating = Dismiss;
		_nameInput = fragment.Get<TextField>("NameInput");
		_hostnameInput = fragment.Get<TextField>("HostnameInput");
		_portInput = fragment.Get<NumberField>("PortInput");
		_errorLabel = fragment.Get<Label>("ErrorMessage");
		_saveButton = fragment.Get<TextButton>("SaveButton");
	}

	public void Open()
	{
		_serverIndex = -1;
		_errorLabel.Visible = false;
		_title.Text = Desktop.Provider.GetText("ui.assetEditor.mainMenu.serverModal.titleAdd");
		_saveButton.Text = Desktop.Provider.GetText("ui.general.add");
		_nameInput.Value = "";
		_hostnameInput.Value = "";
		_portInput.Value = 5520m;
		OpenInLayer();
		Desktop.FocusElement(_nameInput);
	}

	public void Open(int serverIndex, AssetEditorAppMainMenu.Server server)
	{
		_serverIndex = serverIndex;
		_errorLabel.Visible = false;
		_title.Text = Desktop.Provider.GetText("ui.assetEditor.mainMenu.serverModal.titleUpdate");
		_saveButton.Text = Desktop.Provider.GetText("ui.general.save");
		_nameInput.Value = server.Name;
		_hostnameInput.Value = server.Hostname;
		_portInput.Value = server.Port;
		OpenInLayer();
		Desktop.FocusElement(_nameInput);
	}

	private void ShowError(string message)
	{
		_errorLabel.Text = message;
		_errorLabel.Visible = true;
		Layout();
	}

	protected internal override void Validate()
	{
		string text = _nameInput.Value.Trim();
		string text2 = _hostnameInput.Value.Trim();
		int port = (int)_portInput.Value;
		if (text == string.Empty)
		{
			ShowError(Desktop.Provider.GetText("ui.assetEditor.mainMenu.serverModal.errors.enterName"));
			return;
		}
		if (text2 == string.Empty)
		{
			ShowError(Desktop.Provider.GetText("ui.assetEditor.mainMenu.serverModal.errors.enterHostname"));
			return;
		}
		string text3 = text.ToLowerInvariant();
		int count = _interface.App.MainMenu.Servers.Count;
		for (int i = 0; i < count; i++)
		{
			AssetEditorAppMainMenu.Server server = _interface.App.MainMenu.Servers[i];
			if (!(server.Name.ToLowerInvariant() != text3) && i != _serverIndex)
			{
				ShowError(Desktop.Provider.GetText("ui.assetEditor.mainMenu.serverModal.errors.nameAlreadyInUse"));
				return;
			}
		}
		if (_serverIndex != -1)
		{
			_interface.App.MainMenu.UpdateServer(_serverIndex, text, text2, port);
		}
		else
		{
			_interface.App.MainMenu.AddServer(new AssetEditorAppMainMenu.Server
			{
				DateLastJoined = DateTime.Now,
				Name = text,
				Hostname = text2,
				Port = port
			});
		}
		Dismiss();
	}
}
