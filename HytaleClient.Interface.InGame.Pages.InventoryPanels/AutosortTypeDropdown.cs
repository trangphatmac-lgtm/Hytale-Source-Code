using System;
using System.Collections.Generic;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class AutosortTypeDropdown : Element
{
	private DropdownBox _dropdown;

	public SortType SortType;

	public Action SortTypeChanged;

	public AutosortTypeDropdown(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	public void Build()
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		Clear();
		Desktop.Provider.TryGetDocument("InGame/Pages/Inventory/AutosortTypeDropdown.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_dropdown = uIFragment.Get<DropdownBox>("AutosortTypeDropdown");
		List<DropdownBox.DropdownEntryInfo> list = new List<DropdownBox.DropdownEntryInfo>();
		foreach (SortType value in Enum.GetValues(typeof(SortType)))
		{
			SortType val = value;
			string text = Desktop.Provider.GetText($"ui.windows.autoSort.types.{val}");
			list.Add(new DropdownBox.DropdownEntryInfo(text, ((object)(SortType)(ref val)).ToString(), val == SortType));
		}
		_dropdown.Entries = list;
		_dropdown.ValueChanged = delegate
		{
			SetType(_dropdown.SelectedValues[0]);
		};
	}

	public void SetType(string type)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		SetType((SortType)Enum.Parse(typeof(SortType), type));
	}

	public void SetType(SortType type)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		SortType = type;
		SortTypeChanged();
	}
}
