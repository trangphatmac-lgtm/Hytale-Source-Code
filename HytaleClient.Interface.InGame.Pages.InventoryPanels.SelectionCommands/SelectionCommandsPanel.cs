using System.Collections.Generic;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels.SelectionCommands;

internal class SelectionCommandsPanel : Panel
{
	private readonly string[] SelectionCommands = new string[4] { "SET", "WALL", "FILL", "REPLACE" };

	private DropdownBox _modesDropdown;

	private Group _body;

	public const string SelectionToolId = "EditorTool_PlaySelection";

	private BaseSelectionCommand _currentCommand;

	public Group Panel { get; private set; }

	public SelectionCommandsPanel(InGameView inGameView, Element parent = null)
		: base(inGameView, parent)
	{
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Pages/Inventory/BuilderTools/SelectionCommandsPanel.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		Panel = uIFragment.Get<Group>("Panel");
		TextButton textButton = uIFragment.Get<TextButton>("ExecuteCommandButton");
		textButton.Activating = delegate
		{
			ExecuteCommand();
		};
		_body = uIFragment.Get<Group>("Body");
		_modesDropdown = uIFragment.Get<DropdownBox>("ModesDropdown");
		List<DropdownBox.DropdownEntryInfo> list = new List<DropdownBox.DropdownEntryInfo>();
		string[] selectionCommands = SelectionCommands;
		foreach (string text in selectionCommands)
		{
			list.Add(new DropdownBox.DropdownEntryInfo(text, text));
		}
		_modesDropdown.Entries = list;
		_modesDropdown.Value = SelectionCommands[0];
		_modesDropdown.ValueChanged = delegate
		{
			SelectCommand();
		};
		SelectCommand();
	}

	private void ExecuteCommand()
	{
		if (_currentCommand != null)
		{
			string chatCommand = _currentCommand.GetChatCommand();
			_inGameView.InGame.SendChatMessageOrExecuteCommand(chatCommand);
		}
	}

	private void SelectCommand()
	{
		_body.Clear();
		_currentCommand = GetSelectCommand();
		if (_currentCommand != null)
		{
			_currentCommand.Build();
			_body.Add(_currentCommand);
			Layout();
		}
	}

	private BaseSelectionCommand GetSelectCommand()
	{
		if (_modesDropdown.Value == "SET")
		{
			return new SetCommand(_inGameView, Desktop);
		}
		if (_modesDropdown.Value == "WALL")
		{
			return new WallCommand(_inGameView, Desktop);
		}
		if (_modesDropdown.Value == "FILL")
		{
			return new FillCommand(_inGameView, Desktop);
		}
		if (_modesDropdown.Value == "REPLACE")
		{
			return new ReplaceCommand(_inGameView, Desktop);
		}
		return null;
	}
}
