using System.Collections.Generic;
using HytaleClient.Data.Map;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels.SelectionCommands;

internal class PitchDropdown : Element
{
	private InGameView _inGameView;

	private DropdownBox _pitchDropdown;

	private readonly Dictionary<string, string> PitchKeyValues = new Dictionary<string, string>
	{
		{ "Pitch=90", "90" },
		{ "Pitch=180", "180" }
	};

	public PitchDropdown(InGameView inGameView, Desktop desktop)
		: base(desktop, null)
	{
		_inGameView = inGameView;
	}

	public void Build()
	{
		Desktop.Provider.TryGetDocument("InGame/Pages/Inventory/BuilderTools/Input/PitchDropdown.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_pitchDropdown = uIFragment.Get<DropdownBox>("PitchDropdown");
	}

	public void SetPitchValues(string blockName)
	{
		if (string.IsNullOrEmpty(blockName))
		{
			blockName = "Empty";
		}
		List<DropdownBox.DropdownEntryInfo> list = new List<DropdownBox.DropdownEntryInfo>();
		List<string> list2 = new List<string> { "0" };
		ClientBlockType clientBlockTypeFromName = _inGameView.InGame.Instance.MapModule.GetClientBlockTypeFromName(blockName);
		foreach (KeyValuePair<string, string> pitchKeyValue in PitchKeyValues)
		{
			if (clientBlockTypeFromName.Variants.ContainsKey(pitchKeyValue.Key))
			{
				list2.Add(pitchKeyValue.Value);
			}
		}
		foreach (string item in list2)
		{
			list.Add(new DropdownBox.DropdownEntryInfo(item, item));
		}
		_pitchDropdown.Entries = list;
		_pitchDropdown.Value = list2[0];
	}

	public string GetCommandArg()
	{
		string text = "";
		if (_pitchDropdown.Value != "0")
		{
			text = text + "|Pitch=" + _pitchDropdown.Value;
		}
		return text;
	}
}
