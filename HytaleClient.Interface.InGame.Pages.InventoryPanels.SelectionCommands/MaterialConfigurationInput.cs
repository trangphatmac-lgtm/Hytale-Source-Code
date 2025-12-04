using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels.SelectionCommands;

internal class MaterialConfigurationInput : Element
{
	public delegate void OnRemove(MaterialConfigurationInput materialConfigurationInput);

	private OnRemove _onRemove;

	private Button _removeButton;

	private InGameView _inGameView;

	private PitchDropdown _pitchDropdown;

	private BlockSelector _blockSelector;

	private SliderNumberField _weightSlider;

	private bool _hasWeight;

	private bool _hasPitch;

	private int _blockCapacity;

	private SoundStyle _unselectSound;

	private SoundStyle _selectSound;

	public MaterialConfigurationInput(InGameView inGameView, Desktop desktop, OnRemove onRemove, bool hasWeight = true, bool hasPitch = true, int blockCapacity = 1)
		: base(desktop, null)
	{
		_onRemove = onRemove;
		_inGameView = inGameView;
		_hasWeight = hasWeight;
		_hasPitch = hasPitch;
		_blockCapacity = blockCapacity;
	}

	public void Build()
	{
		Clear();
		Group root = new Group(Desktop, this)
		{
			LayoutMode = LayoutMode.Top
		};
		Desktop.Provider.TryGetDocument("InGame/Pages/Inventory/BuilderTools/Input/BlockSelectorConfiguration.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, root);
		document.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "CreateEyedropUnselect", out _unselectSound);
		document.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "CreateEyedropSelect", out _selectSound);
		_blockSelector = uIFragment.Get<BlockSelector>("BlockSelector");
		_blockSelector.Capacity = _blockCapacity;
		_weightSlider = uIFragment.Get<SliderNumberField>("WeightSlider");
		if (!_hasWeight)
		{
			uIFragment.Get<Group>("WeightSliderContainer").Visible = false;
		}
		Group group = uIFragment.Get<Group>("PitchContainer");
		_pitchDropdown = new PitchDropdown(_inGameView, Desktop);
		_pitchDropdown.Build();
		group.Add(_pitchDropdown);
		if (!_hasPitch)
		{
			group.Visible = false;
		}
		_removeButton = uIFragment.Get<Button>("ActionClear");
		_removeButton.Activating = delegate
		{
			_inGameView.InGame.Instance.AudioModule.PlayLocalSoundEvent("UI_CLEAR");
			if (_onRemove != null)
			{
				_onRemove(this);
			}
		};
	}

	protected override void OnMounted()
	{
		_blockSelector.ValueChanged = delegate
		{
			string value = _blockSelector.Value;
			if (string.IsNullOrEmpty(value))
			{
				Desktop.Provider.PlaySound(_unselectSound);
			}
			else
			{
				Desktop.Provider.PlaySound(_selectSound);
			}
			SetPitchValues();
			_blockSelector.Value = value;
			Layout();
		};
		SetPitchValues();
	}

	private void SetPitchValues()
	{
		if (_hasPitch)
		{
			_pitchDropdown.SetPitchValues(_blockSelector.Value);
		}
	}

	public string GetCommandArgs()
	{
		string text = _blockSelector.Value;
		int value = _weightSlider.Value;
		if (string.IsNullOrEmpty(text))
		{
			text = "Empty";
		}
		string text2 = text ?? "";
		if (_hasPitch)
		{
			text2 += _pitchDropdown.GetCommandArg();
		}
		if (_hasWeight)
		{
			text2 = $"{value} {text2}";
		}
		return text2;
	}

	public void HideRemoveButton()
	{
		_removeButton.Visible = false;
	}

	public void ShowRemoveButton()
	{
		_removeButton.Visible = true;
	}
}
