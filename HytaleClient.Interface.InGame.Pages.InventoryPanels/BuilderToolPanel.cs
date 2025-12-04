using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HytaleClient.Data.Items;
using HytaleClient.InGame.Modules.BuilderTools.Tools;
using HytaleClient.InGame.Modules.BuilderTools.Tools.Brush;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class BuilderToolPanel : Panel
{
	private struct BrushSetting
	{
		public string Key;

		public BuilderToolArgType Type;

		public string LabelPrefix;
	}

	private class BuilderToolField<TInput, TValue> where TInput : InputElement<TValue>
	{
		public readonly Label LabelPrefix;

		public readonly Label Label;

		public readonly Label LabelSuffix;

		public readonly TInput Input;

		public BuilderToolField(UIFragment fragment, string inputName)
		{
			LabelPrefix = fragment.Get<Label>("LabelPrefix");
			Label = fragment.Get<Label>("Label");
			LabelSuffix = fragment.Get<Label>("LabelSuffix");
			Input = fragment.Get<TInput>(inputName);
		}

		public void BindInput(string key, Action<string, string> valueChanged)
		{
			Input.ValueChanged = delegate
			{
				valueChanged(key, Input.Value.ToString());
			};
		}
	}

	private class BuilderToolBlockSelectorField : BuilderToolField<BlockSelector, string>
	{
		public readonly Group EmptyFilter;

		public readonly CheckBox EmptyFilterCheckbox;

		public BuilderToolBlockSelectorField(InGameView inGameView, UIFragment fragment, string inputName)
			: base(fragment, inputName)
		{
			BuilderToolBlockSelectorField builderToolBlockSelectorField = this;
			EmptyFilter = fragment.Get<Group>("EmptyFilter");
			EmptyFilterCheckbox = fragment.Get<CheckBox>("EmptyFilterCheckbox");
			fragment.Get<Button>("ActionClear").Activating = delegate
			{
				inGameView.InGame.Instance.AudioModule.PlayLocalSoundEvent("UI_CLEAR");
				builderToolBlockSelectorField.Input.Reset();
			};
		}
	}

	private ClientItemStack _selectedTool;

	private Document _blockSelectorDoc;

	private Document _checkboxDoc;

	private Document _dropdownDoc;

	private Document _sliderDoc;

	private Document _numberInputDoc;

	private Document _textInputDoc;

	private Document _multilineTextInputDoc;

	private Label _titleName;

	private Group _body;

	private Label _infoLabel;

	private Group _selectedMaterialContainer;

	private BuilderToolBlockSelectorField _selectedMaterial;

	private Button _generalSettingsTab;

	private Button.ButtonStyle _generalSettingsTabStyle;

	private Button.ButtonStyle _generalSettingsTabActivatedStyle;

	private Group _generalSettingsContainer;

	private Group _maskSettingsTabContainer;

	private Button _maskSettingsTab;

	private Button.ButtonStyle _maskSettingsTabStyle;

	private Button.ButtonStyle _maskSettingsTabActivatedStyle;

	private Group _maskSettingsContainer;

	private Group _maskSettingsBlockSelectorsContainer;

	private Group _maskSettingsCommandsContainer;

	private BuilderToolField<MultilineTextField, string> _maskCommands;

	private Group _footer;

	private CheckBox _useCustomMaskCommandEntry;

	private readonly List<BrushSetting> _brushGeneralSettings = new List<BrushSetting>
	{
		new BrushSetting
		{
			Key = "Width",
			Type = (BuilderToolArgType)2
		},
		new BrushSetting
		{
			Key = "Height",
			Type = (BuilderToolArgType)2
		},
		new BrushSetting
		{
			Key = "Shape",
			Type = (BuilderToolArgType)10
		},
		new BrushSetting
		{
			Key = "Origin",
			Type = (BuilderToolArgType)10
		},
		new BrushSetting
		{
			Key = "MirrorAxis",
			Type = (BuilderToolArgType)10
		},
		new BrushSetting
		{
			Key = "Thickness",
			Type = (BuilderToolArgType)2
		}
	};

	private readonly List<BrushSetting> _brushMaskSettings = new List<BrushSetting>
	{
		new BrushSetting
		{
			Key = "Mask"
		},
		new BrushSetting
		{
			Key = "MaskAbove",
			LabelPrefix = "> "
		},
		new BrushSetting
		{
			Key = "MaskNot",
			LabelPrefix = "! "
		},
		new BrushSetting
		{
			Key = "MaskBelow",
			LabelPrefix = "< "
		},
		new BrushSetting
		{
			Key = "MaskAdjacent",
			LabelPrefix = "~ "
		},
		new BrushSetting
		{
			Key = "MaskNeighbor",
			LabelPrefix = "^ "
		}
	};

	public Group Panel { get; private set; }

	public BuilderToolPanel(InGameView inGameView, Element parent = null)
		: base(inGameView, parent)
	{
	}//IL_001e: Unknown result type (might be due to invalid IL or missing references)
	//IL_0042: Unknown result type (might be due to invalid IL or missing references)
	//IL_0067: Unknown result type (might be due to invalid IL or missing references)
	//IL_008c: Unknown result type (might be due to invalid IL or missing references)
	//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
	//IL_00d5: Unknown result type (might be due to invalid IL or missing references)


	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Pages/Inventory/BuilderTools/BuilderToolPanel.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		Panel = uIFragment.Get<Group>("Panel");
		Interface.TryGetDocument("InGame/Pages/Inventory/BuilderTools/Input/BlockSelector.ui", out _blockSelectorDoc);
		Interface.TryGetDocument("InGame/Pages/Inventory/BuilderTools/Input/Checkbox.ui", out _checkboxDoc);
		Interface.TryGetDocument("InGame/Pages/Inventory/BuilderTools/Input/Dropdown.ui", out _dropdownDoc);
		Interface.TryGetDocument("InGame/Pages/Inventory/BuilderTools/Input/Slider.ui", out _sliderDoc);
		Interface.TryGetDocument("InGame/Pages/Inventory/BuilderTools/Input/Number.ui", out _numberInputDoc);
		Interface.TryGetDocument("InGame/Pages/Inventory/BuilderTools/Input/Text.ui", out _textInputDoc);
		Interface.TryGetDocument("InGame/Pages/Inventory/BuilderTools/Input/MultilineText.ui", out _multilineTextInputDoc);
		_titleName = uIFragment.Get<Label>("NameLabel");
		_body = uIFragment.Get<Group>("Body");
		_body.KeepScrollPosition = true;
		_infoLabel = uIFragment.Get<Label>("InfoLabel");
		_selectedMaterialContainer = uIFragment.Get<Group>("SelectedMaterial");
		_selectedMaterial = CreateBlockSelectorField(_selectedMaterialContainer, null, null);
		_selectedMaterial.BindInput("Material", delegate(string argId, string value)
		{
			_inGameView.InGame.Instance.AudioModule.PlayLocalSoundEvent(string.IsNullOrEmpty(value) ? "CREATE_EYEDROP_UNSELECT" : "CREATE_EYEDROP_SELECT");
			BrushArgValueChanged(argId, value);
		});
		_generalSettingsTab = uIFragment.Get<Button>("GeneralSettingsTab");
		_generalSettingsTab.Activating = delegate
		{
			if (!_generalSettingsContainer.Visible)
			{
				_generalSettingsContainer.Visible = true;
				_maskSettingsContainer.Visible = false;
				_footer.Visible = false;
				Layout();
			}
		};
		_generalSettingsTabStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "GeneralSettingsTabStyle");
		_generalSettingsTabActivatedStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "GeneralSettingsTabActivatedStyle");
		_generalSettingsContainer = uIFragment.Get<Group>("GeneralSettingsContainer");
		_maskSettingsTabContainer = uIFragment.Get<Group>("MaskSettingsTabContainer");
		_maskSettingsTab = _maskSettingsTabContainer.Find<Button>("MaskSettingsTab");
		_maskSettingsTab.Activating = delegate
		{
			if (!_maskSettingsContainer.Visible)
			{
				_generalSettingsContainer.Visible = false;
				_maskSettingsContainer.Visible = true;
				_maskSettingsBlockSelectorsContainer.Visible = !_useCustomMaskCommandEntry.Value;
				_maskSettingsCommandsContainer.Visible = _useCustomMaskCommandEntry.Value;
				_footer.Visible = true;
				Layout();
			}
		};
		_maskSettingsTabStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "MaskSettingsTabStyle");
		_maskSettingsTabActivatedStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "MaskSettingsTabActivatedStyle");
		_maskSettingsContainer = uIFragment.Get<Group>("MaskSettingsContainer");
		_maskSettingsContainer.Visible = false;
		_maskSettingsBlockSelectorsContainer = uIFragment.Get<Group>("MaskSettingsBlockSelectorsContainer");
		_maskSettingsCommandsContainer = uIFragment.Get<Group>("MaskSettingsCommandsContainer");
		_maskCommands = CreateMultilineTextField(_maskSettingsCommandsContainer, null, "");
		_maskCommands.BindInput("MaskCommands", BrushArgValueChanged);
		_footer = uIFragment.Get<Group>("Footer");
		_footer.Visible = false;
		_useCustomMaskCommandEntry = uIFragment.Get<CheckBox>("UseCustomMaskCommandEntryCheckbox");
		_useCustomMaskCommandEntry.ValueChanged = delegate
		{
			bool value2 = _useCustomMaskCommandEntry.Value;
			_maskSettingsBlockSelectorsContainer.Visible = !value2;
			_maskSettingsCommandsContainer.Visible = value2;
			Layout();
			BrushArgValueChanged("UseMaskCommands", value2.ToString());
		};
		Refresh(doLayout: false);
	}

	protected override void ApplyStyles()
	{
		_generalSettingsTab.Style = (_generalSettingsContainer.IsMounted ? _generalSettingsTabActivatedStyle : _generalSettingsTabStyle);
		_maskSettingsTab.Style = (_maskSettingsContainer.IsMounted ? _maskSettingsTabActivatedStyle : _maskSettingsTabStyle);
	}

	protected override void OnMounted()
	{
		Refresh();
	}

	private string ApplyEmptyFilter(string value, bool withEmpty)
	{
		bool flag = HasEmptyBlock(value);
		if (withEmpty && !flag)
		{
			return AppendEmptyBlock(value);
		}
		if (!withEmpty && flag)
		{
			return RemoveEmptyBlock(value);
		}
		return value;
	}

	private bool HasEmptyBlock(string value)
	{
		return value.Split(new char[1] { ',' }).Contains("Empty");
	}

	private string AppendEmptyBlock(string value)
	{
		return string.IsNullOrEmpty(value) ? "Empty" : string.Join(",", value, "Empty");
	}

	private string RemoveEmptyBlock(string value)
	{
		return string.Join(",", from item in value.Split(new char[1] { ',' })
			where item != "Empty"
			select item);
	}

	private void BrushMaskArgValueChanged(string argId, string value, bool withEmpty)
	{
		_inGameView.InGame.Instance.AudioModule.PlayLocalSoundEvent("CREATE_MASK_ADD");
		BrushArgValueChanged(argId, ApplyEmptyFilter(value, withEmpty));
	}

	private void BrushArgValueChanged(string argId, string value)
	{
		ArgValueChanged((BuilderToolArgGroup)1, argId, value);
	}

	private void ToolArgValueChanged(string argId, string value)
	{
		ArgValueChanged((BuilderToolArgGroup)0, argId, value);
	}

	private void ArgValueChanged(BuilderToolArgGroup argGroup, string argId, string value)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		Interface.TriggerEventFromInterface("builderTools.argValueChange", argGroup, argId, value);
	}

	public void ConfiguringToolChange(ToolInstance toolInstance)
	{
		ClientItemStack item = toolInstance?.ItemStack;
		ConfiguringToolChange(item);
	}

	public void ConfiguringToolChange(ClientItemStack item)
	{
		if (_selectedTool != item)
		{
			if (base.IsMounted)
			{
				Update(item);
			}
			_selectedTool = item;
		}
	}

	public void Refresh(bool doLayout = true)
	{
		Update(_selectedTool, doLayout);
	}

	private string GetBrushArgLabelText(string key)
	{
		return Desktop.Provider.GetText("builderTools.brush.args." + key + ".name");
	}

	private List<DropdownBox.DropdownEntryInfo> GetDropdownEntries<T>(string langKey)
	{
		return Enum.GetValues(typeof(T)).Cast<T>().Select(delegate(T type)
		{
			string text = type.ToString();
			return new DropdownBox.DropdownEntryInfo(Desktop.Provider.GetText(langKey + "." + text), text);
		})
			.ToList();
	}

	private List<DropdownBox.DropdownEntryInfo> GetBrushArgOptions(string key)
	{
		switch (key)
		{
		case "Shape":
			return GetDropdownEntries<BrushShape>("builderTools.brush.shape");
		case "Origin":
			return GetDropdownEntries<BrushOrigin>("builderTools.brush.origin");
		case "RotationAxis":
		case "MirrorAxis":
			return GetDropdownEntries<BrushAxis>("builderTools.brush.axis");
		case "RotationAngle":
			return GetDropdownEntries<Rotation>("builderTools.brush.rotation");
		default:
			return null;
		}
	}

	private T GetBrushDataValue<T>(BuilderToolBrushData data, string key)
	{
		return (T)((object)data).GetType().GetField(key).GetValue(data);
	}

	private void DisplayInfo(string key, bool doLayout)
	{
		_infoLabel.Visible = true;
		_infoLabel.Text = Desktop.Provider.GetText(key);
		_generalSettingsContainer.Visible = true;
		_maskSettingsContainer.Visible = false;
		_footer.Visible = false;
		if (doLayout)
		{
			Layout();
		}
	}

	private void Update(ClientItemStack item = null, bool doLayout = true)
	{
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Expected I4, but got Unknown
		//IL_0740: Unknown result type (might be due to invalid IL or missing references)
		//IL_0747: Expected O, but got Unknown
		//IL_079b: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_083c: Unknown result type (might be due to invalid IL or missing references)
		//IL_083e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0840: Unknown result type (might be due to invalid IL or missing references)
		//IL_0842: Unknown result type (might be due to invalid IL or missing references)
		//IL_0844: Unknown result type (might be due to invalid IL or missing references)
		//IL_0877: Expected I4, but got Unknown
		_titleName.Text = Desktop.Provider.GetText("ui.builderTools.name");
		_selectedMaterialContainer.Visible = false;
		_maskSettingsTabContainer.Visible = false;
		_body.Visible = false;
		_footer.Visible = false;
		_generalSettingsContainer.Clear();
		_maskSettingsBlockSelectorsContainer.Clear();
		if (item == null || _inGameView.Items[item.Id]?.BuilderTool == null)
		{
			DisplayInfo("ui.builderTools.selectATool", doLayout);
			return;
		}
		BuilderTool tool = _inGameView.Items[item.Id].BuilderTool;
		_inGameView.Items.TryGetValue(item.Id, out var _);
		_titleName.Text = Desktop.Provider.GetText("builderTools.tools." + tool.Id + ".name");
		if (!tool.ToolItem.IsBrush && tool.ToolItem.Args.Count == 0)
		{
			DisplayInfo("ui.builderTools.noToolSettings", doLayout);
			return;
		}
		_infoLabel.Visible = false;
		_body.Visible = true;
		if (tool.ToolItem.IsBrush)
		{
			_selectedMaterialContainer.Visible = true;
			_maskSettingsTabContainer.Visible = true;
			Dictionary<string, string> dictionary = new BrushData(item, tool).ToArgValues();
			_selectedMaterial.Label.Text = GetBrushArgLabelText("Material");
			_selectedMaterial.Input.Capacity = 7;
			_selectedMaterial.Input.Value = dictionary["Material"];
			_useCustomMaskCommandEntry.Value = bool.Parse(dictionary["UseMaskCommands"]);
			_maskCommands.Label.Text = GetBrushArgLabelText("MaskCommands");
			_maskCommands.Input.Value = dictionary["MaskCommands"];
			foreach (BrushSetting brushGeneralSetting in _brushGeneralSettings)
			{
				BuilderToolArgType type = brushGeneralSetting.Type;
				string key3 = brushGeneralSetting.Key;
				string brushArgLabelText = GetBrushArgLabelText(key3);
				string value2 = dictionary[key3];
				BuilderToolArgType val2 = type;
				BuilderToolArgType val3 = val2;
				BuilderToolField<SliderNumberField, int> intField;
				switch ((int)val3)
				{
				case 0:
				{
					BuilderToolField<CheckBox, bool> builderToolField4 = CreateCheckBoxField(_generalSettingsContainer, brushArgLabelText, value2);
					builderToolField4.BindInput(key3, BrushArgValueChanged);
					break;
				}
				case 1:
				{
					BuilderToolFloatArg brushDataValue2 = GetBrushDataValue<BuilderToolFloatArg>(tool.ToolItem.BrushData, key3);
					BuilderToolField<NumberField, decimal> builderToolField3 = CreateNumberField(_generalSettingsContainer, brushArgLabelText, value2, brushDataValue2.Min, brushDataValue2.Max);
					builderToolField3.BindInput(key3, BrushArgValueChanged);
					break;
				}
				case 2:
				{
					BuilderToolIntArg brushDataValue = GetBrushDataValue<BuilderToolIntArg>(tool.ToolItem.BrushData, key3);
					intField = CreateSliderField(_generalSettingsContainer, brushArgLabelText, value2, brushDataValue.Min, brushDataValue.Max);
					intField.Input.SliderMouseButtonReleased = SliderCallback;
					intField.Input.NumberFieldBlurred = SliderCallback;
					break;
				}
				case 3:
				{
					BuilderToolField<TextField, string> builderToolField2 = CreateTextField(_generalSettingsContainer, brushArgLabelText, value2);
					builderToolField2.BindInput(key3, BrushArgValueChanged);
					break;
				}
				case 4:
				case 5:
				{
					BuilderToolBlockSelectorField builderToolBlockSelectorField = CreateBlockSelectorField(_generalSettingsContainer, brushArgLabelText, value2);
					builderToolBlockSelectorField.BindInput(key3, BrushArgValueChanged);
					break;
				}
				case 10:
				{
					BuilderToolField<DropdownBox, string> builderToolField = CreateDropdownField(_generalSettingsContainer, brushArgLabelText, value2, GetBrushArgOptions(key3));
					builderToolField.BindInput(key3, BrushArgValueChanged);
					break;
				}
				}
				void SliderCallback()
				{
					BrushArgValueChanged(key3, intField.Input.Value.ToString());
				}
			}
			foreach (BrushSetting brushMaskSetting in _brushMaskSettings)
			{
				string key2 = brushMaskSetting.Key;
				BuilderToolBlockSelectorField field = CreateBlockSelectorField(_maskSettingsBlockSelectorsContainer, GetBrushArgLabelText(key2), dictionary[key2], 7);
				field.LabelPrefix.Text = brushMaskSetting.LabelPrefix;
				field.BindInput(key2, delegate(string k, string v)
				{
					BrushMaskArgValueChanged(k, v, field.EmptyFilterCheckbox.Value);
				});
				field.EmptyFilter.Visible = true;
				field.EmptyFilterCheckbox.Value = HasEmptyBlock(dictionary[key2]);
				field.EmptyFilterCheckbox.ValueChanged = delegate
				{
					BrushArgValueChanged(key2, ApplyEmptyFilter(field.Input.Value, field.EmptyFilterCheckbox.Value));
				};
			}
			_maskSettingsBlockSelectorsContainer.Visible = !_useCustomMaskCommandEntry.Value;
			_maskSettingsCommandsContainer.Visible = _useCustomMaskCommandEntry.Value;
			_footer.Visible = _maskSettingsContainer.Visible;
		}
		else
		{
			_generalSettingsContainer.Visible = true;
			_maskSettingsContainer.Visible = false;
			_footer.Visible = false;
		}
		if (tool.ToolItem.Args.Count > 0)
		{
			JToken val4 = null;
			JObject metadata = item.Metadata;
			bool? flag = ((metadata != null) ? new bool?(metadata.TryGetValue("ToolData", ref val4)) : null);
			if (!flag.HasValue || flag == false)
			{
				val4 = (JToken)new JObject();
			}
			JToken val5 = ((JToken)tool.GetDefaultArgData()).DeepClone();
			foreach (KeyValuePair<string, BuilderToolArg> item2 in tool.ToolItem.Args.ToImmutableSortedDictionary())
			{
				BuilderToolArgType argType = item2.Value.ArgType;
				string key = item2.Key;
				string text = Desktop.Provider.GetText("builderTools.tools." + tool.Id + ".args." + key + ".name");
				string value3 = ((object)(val4[(object)item2.Key] ?? val5[(object)item2.Key])).ToString();
				BuilderToolArgType val6 = argType;
				BuilderToolArgType val7 = val6;
				BuilderToolField<SliderNumberField, int> sliderField;
				switch ((int)val7)
				{
				case 4:
				case 5:
				{
					BuilderToolBlockSelectorField builderToolBlockSelectorField2 = CreateBlockSelectorField(_generalSettingsContainer, text, value3);
					builderToolBlockSelectorField2.BindInput(key, ToolArgValueChanged);
					break;
				}
				case 3:
				{
					BuilderToolField<TextField, string> builderToolField8 = CreateTextField(_generalSettingsContainer, text, value3);
					builderToolField8.BindInput(key, ToolArgValueChanged);
					break;
				}
				case 0:
				{
					BuilderToolField<CheckBox, bool> builderToolField7 = CreateCheckBoxField(_generalSettingsContainer, text, value3);
					builderToolField7.BindInput(key, ToolArgValueChanged);
					break;
				}
				case 2:
					sliderField = CreateSliderField(_generalSettingsContainer, text, value3, item2.Value.IntArg.Min, item2.Value.IntArg.Max);
					sliderField.Input.SliderMouseButtonReleased = SliderCallback;
					sliderField.Input.NumberFieldBlurred = SliderCallback;
					break;
				case 1:
				{
					BuilderToolField<NumberField, decimal> builderToolField6 = CreateNumberField(_generalSettingsContainer, text, value3, item2.Value.FloatArg.Min, item2.Value.FloatArg.Max);
					builderToolField6.BindInput(key, ToolArgValueChanged);
					break;
				}
				case 10:
				{
					List<DropdownBox.DropdownEntryInfo> options = tool.ToolItem.Args[key].OptionArg.Options.Select(delegate(string val)
					{
						string text2 = Desktop.Provider.GetText("builderTools.tools." + tool.Id + ".args." + key + ".values." + val);
						return new DropdownBox.DropdownEntryInfo(text2, val);
					}).ToList();
					BuilderToolField<DropdownBox, string> builderToolField5 = CreateDropdownField(_generalSettingsContainer, text, value3, options);
					builderToolField5.BindInput(key, ToolArgValueChanged);
					break;
				}
				}
				void SliderCallback()
				{
					ToolArgValueChanged(key, sliderField.Input.Value.ToString());
				}
			}
		}
		if (doLayout)
		{
			Layout();
		}
	}

	private BuilderToolBlockSelectorField CreateBlockSelectorField(Group container, string label, string value, int capacity = 1)
	{
		UIFragment fragment = _blockSelectorDoc.Instantiate(Desktop, container);
		BuilderToolBlockSelectorField builderToolBlockSelectorField = new BuilderToolBlockSelectorField(_inGameView, fragment, "BlockSelector");
		builderToolBlockSelectorField.Label.Text = label;
		builderToolBlockSelectorField.Input.Capacity = capacity;
		builderToolBlockSelectorField.Input.Value = value;
		return builderToolBlockSelectorField;
	}

	private BuilderToolField<CheckBox, bool> CreateCheckBoxField(Group container, string label, string value)
	{
		UIFragment fragment = _checkboxDoc.Instantiate(Desktop, container);
		BuilderToolField<CheckBox, bool> builderToolField = new BuilderToolField<CheckBox, bool>(fragment, "Checkbox");
		builderToolField.Label.Text = label;
		builderToolField.Input.Value = bool.TryParse(value, out var result) && result;
		return builderToolField;
	}

	private BuilderToolField<DropdownBox, string> CreateDropdownField(Group container, string label, string value, List<DropdownBox.DropdownEntryInfo> options)
	{
		UIFragment fragment = _dropdownDoc.Instantiate(Desktop, container);
		BuilderToolField<DropdownBox, string> builderToolField = new BuilderToolField<DropdownBox, string>(fragment, "Dropdown");
		builderToolField.Label.Text = label;
		builderToolField.Input.Entries = options;
		builderToolField.Input.Value = value;
		return builderToolField;
	}

	private BuilderToolField<SliderNumberField, int> CreateSliderField(Group container, string label, string value, int min, int max)
	{
		UIFragment fragment = _sliderDoc.Instantiate(Desktop, container);
		BuilderToolField<SliderNumberField, int> builderToolField = new BuilderToolField<SliderNumberField, int>(fragment, "Slider");
		builderToolField.Label.Text = label;
		builderToolField.Input.Value = int.Parse(value);
		builderToolField.Input.Min = min;
		builderToolField.Input.Max = max;
		return builderToolField;
	}

	private BuilderToolField<NumberField, decimal> CreateNumberField(Group container, string label, string value, float min, float max)
	{
		UIFragment fragment = _numberInputDoc.Instantiate(Desktop, container);
		BuilderToolField<NumberField, decimal> builderToolField = new BuilderToolField<NumberField, decimal>(fragment, "NumberField");
		builderToolField.Label.Text = label;
		builderToolField.Input.Value = decimal.Parse(value);
		builderToolField.Input.Format.MinValue = Convert.ToDecimal(min);
		builderToolField.Input.Format.MaxValue = Convert.ToDecimal(max);
		return builderToolField;
	}

	private BuilderToolField<TextField, string> CreateTextField(Group container, string label, string value)
	{
		UIFragment fragment = _textInputDoc.Instantiate(Desktop, container);
		BuilderToolField<TextField, string> builderToolField = new BuilderToolField<TextField, string>(fragment, "TextField");
		builderToolField.Label.Text = label;
		builderToolField.Input.Value = value;
		return builderToolField;
	}

	private BuilderToolField<MultilineTextField, string> CreateMultilineTextField(Group container, string label, string value, int maxLines = 3)
	{
		UIFragment fragment = _multilineTextInputDoc.Instantiate(Desktop, container);
		BuilderToolField<MultilineTextField, string> builderToolField = new BuilderToolField<MultilineTextField, string>(fragment, "MultilineTextField");
		builderToolField.Label.Text = label;
		builderToolField.Input.Value = value;
		builderToolField.Input.MaxLines = maxLines;
		return builderToolField;
	}
}
