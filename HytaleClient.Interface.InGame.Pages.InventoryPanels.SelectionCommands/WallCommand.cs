using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels.SelectionCommands;

internal class WallCommand : BaseSelectionCommand
{
	private CheckBox _floorCheckbox;

	private CheckBox _roofCheckbox;

	private BlockSelector _materialSelector;

	private SliderNumberField _thicknessSlider;

	private PitchDropdown _pithDropdown;

	private SoundStyle _unselectSound;

	private SoundStyle _selectSound;

	public WallCommand(InGameView inGameView, Desktop desktop, Element parent = null)
		: base(inGameView, desktop, parent)
	{
	}

	public override void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("InGame/Pages/Inventory/BuilderTools/CommandWall.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_materialSelector = uIFragment.Get<BlockSelector>("MaterialSelector");
		_roofCheckbox = uIFragment.Get<CheckBox>("RoofCheckbox");
		_floorCheckbox = uIFragment.Get<CheckBox>("FloorCheckbox");
		_thicknessSlider = uIFragment.Get<SliderNumberField>("ThicknessSlider");
		document.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "CreateEyedropUnselect", out _unselectSound);
		document.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "CreateEyedropSelect", out _selectSound);
		Group group = uIFragment.Get<Group>("PitchContainer");
		_pithDropdown = new PitchDropdown(_inGameView, Desktop);
		_pithDropdown.Build();
		group.Add(_pithDropdown);
	}

	public override string GetChatCommand()
	{
		string value = _materialSelector.Value;
		bool value2 = _roofCheckbox.Value;
		bool value3 = _floorCheckbox.Value;
		int value4 = _thicknessSlider.Value;
		string text = $"/wall {value}{_pithDropdown.GetCommandArg()} --thickness={value4}";
		if (value2)
		{
			text += " --roof";
		}
		if (value3)
		{
			text += " --floor";
		}
		return text;
	}

	protected override void OnMounted()
	{
		_materialSelector.ValueChanged = delegate
		{
			string value = _materialSelector.Value;
			if (string.IsNullOrEmpty(value))
			{
				Desktop.Provider.PlaySound(_unselectSound);
			}
			else
			{
				Desktop.Provider.PlaySound(_selectSound);
			}
			_pithDropdown.SetPitchValues(value);
			_materialSelector.Value = value;
			Layout();
		};
		_pithDropdown.SetPitchValues(_materialSelector.Value);
	}
}
