using System.Collections.Generic;
using HytaleClient.Data.Items;
using HytaleClient.InGame.Modules;
using HytaleClient.InGame.Modules.BuilderTools.Tools;
using HytaleClient.Interface.InGame.Pages.InventoryPanels;
using HytaleClient.Interface.InGame.Pages.InventoryPanels.SelectionCommands;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Interface.InGame.Pages;

internal class ToolsSettingsPage : HytaleClient.Interface.InGame.Pages.InventoryPanels.Panel
{
	private UIFragment _fragment = null;

	private BuilderToolPanel _builderToolPanel = null;

	private Group _shapeToolSelector;

	public SelectionCommandsPanel _selectionCommandsPanel;

	public ToolsSettingsPage(InGameView inGameView, Element parent = null)
		: base(inGameView, parent)
	{
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Pages/ToolsSettings.ui", out var document);
		_fragment = document.Instantiate(Desktop, this);
		Group group = _fragment.Get<Group>("ToolPanel");
		_builderToolPanel = new BuilderToolPanel(_inGameView);
		_builderToolPanel.Build();
		group.Add(_builderToolPanel);
		Group group2 = _fragment.Get<Group>("SelectionCommandsPanelContainer");
		_selectionCommandsPanel = new SelectionCommandsPanel(_inGameView);
		_selectionCommandsPanel.Build();
		group2.Add(_selectionCommandsPanel);
		_selectionCommandsPanel.Visible = false;
		_builderToolPanel.Visible = false;
		_shapeToolSelector = _fragment.Get<Group>("ShapeToolSelector");
	}

	protected void OnToolSelected(int itemSlot)
	{
		ClientItemStack toolsItem = _inGameView.InGame.Instance.InventoryModule.GetToolsItem(itemSlot);
		_inGameView.InGame.Instance.BuilderToolsModule.TrySelectActiveTool(-8, itemSlot, toolsItem);
		_inGameView.InGame.Instance.InventoryModule.SetActiveToolsSlot(itemSlot);
		OnPlayerCharacterItemChanged(ItemChangeType.Other);
		_builderToolPanel.Visible = _inGameView.InGame.Instance.BuilderToolsModule.HasConfigurationToolBrushDataOrArguments();
		_selectionCommandsPanel.Visible = toolsItem.Id == "EditorTool_PlaySelection";
		Layout();
	}

	public void SelectToolById(string itemId)
	{
		ClientItemStack[] toolItemStacks = _inGameView.InGame.Instance.InventoryModule.GetToolItemStacks();
		ClientItemStack clientItemStack = null;
		int slot = 0;
		for (int i = 0; i < toolItemStacks.Length; i++)
		{
			if (toolItemStacks[i]?.Id == itemId)
			{
				clientItemStack = toolItemStacks[i];
				slot = i;
				break;
			}
		}
		if (clientItemStack != null)
		{
			_inGameView.InGame.Instance.BuilderToolsModule.TrySelectActiveTool(-8, slot, clientItemStack);
			_inGameView.InGame.Instance.InventoryModule.SetActiveToolsSlot(slot);
			OnPlayerCharacterItemChanged(ItemChangeType.Other);
			_builderToolPanel.Visible = _inGameView.InGame.Instance.BuilderToolsModule.HasConfigurationToolBrushDataOrArguments();
			Layout();
		}
	}

	protected override void OnMounted()
	{
		DropdownBox dropdown = _fragment.Get<DropdownBox>("Dropdown");
		dropdown.ValueChanged = delegate
		{
			List<int> list2 = dropdown.selectedIndexes();
			int itemSlot = ((list2.Count > 0) ? list2[0] : 0);
			OnToolSelected(itemSlot);
		};
		List<DropdownBox.DropdownEntryInfo> list = new List<DropdownBox.DropdownEntryInfo>();
		ClientItemStack activeToolsItem = _inGameView.InGame.Instance.InventoryModule.GetActiveToolsItem();
		ClientItemStack[] toolItemStacks = _inGameView.InGame.Instance.InventoryModule.GetToolItemStacks();
		foreach (ClientItemStack clientItemStack in toolItemStacks)
		{
			if (clientItemStack == null || !_inGameView.Items.ContainsKey(clientItemStack.Id))
			{
				continue;
			}
			BuilderTool builderTool = _inGameView.Items[clientItemStack.Id].BuilderTool;
			if (builderTool != null)
			{
				string text = Desktop.Provider.GetText("builderTools.tools." + builderTool.Id + ".name");
				list.Add(new DropdownBox.DropdownEntryInfo(text, clientItemStack.Id));
				if (activeToolsItem != null && clientItemStack.Id == activeToolsItem.Id)
				{
					dropdown.SelectedValues = new List<string> { activeToolsItem.Id };
					OnToolSelected(_inGameView.InGame.Instance.InventoryModule.GetActiveToolsSlot());
				}
			}
		}
		dropdown.Entries = list;
	}

	public bool ContainPosition(Point pos)
	{
		bool flag = _shapeToolSelector.AnchoredRectangle.Contains(pos);
		if (!_builderToolPanel.Visible && !_selectionCommandsPanel.Visible)
		{
			return flag;
		}
		return flag || _builderToolPanel.AnchoredRectangle.Contains(pos) || _selectionCommandsPanel.AnchoredRectangle.Contains(pos);
	}

	protected override void OnUnmounted()
	{
	}

	private bool ContainsNonAssignedMaterialField(JObject jObject)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		if (jObject == null)
		{
			return false;
		}
		IEnumerator<KeyValuePair<string, JToken>> enumerator = jObject.GetEnumerator();
		while (enumerator.MoveNext())
		{
			string key = enumerator.Current.Key;
			JToken value = enumerator.Current.Value;
			if (value is JObject && ContainsNonAssignedMaterialField((JObject)value))
			{
				return true;
			}
			if (key.Contains("Material"))
			{
				return ((object)value)?.ToString().Equals("Empty") ?? true;
			}
		}
		return false;
	}

	public void OnPlayerCharacterItemChanged(ItemChangeType changeType)
	{
		if (changeType == ItemChangeType.Dropped)
		{
			OnMounted();
			return;
		}
		ClientItemStack activeToolsItem = _inGameView.InGame.Instance.InventoryModule.GetActiveToolsItem();
		_builderToolPanel.ConfiguringToolChange(activeToolsItem);
	}
}
