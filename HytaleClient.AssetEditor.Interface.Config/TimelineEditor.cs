using System;
using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class TimelineEditor : ValueEditor
{
	private const string HourPropertyKey = "Hour";

	private ColorPickerDropdownBox[] _colorPickers = new ColorPickerDropdownBox[24];

	private NumberField[] _numberFields = new NumberField[24];

	private Group[] _containers = new Group[24];

	private string _valuePropertyKey;

	private SchemaNode _valueSchema;

	private bool _isEditorRegistered;

	public TimelineEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
		_layoutMode = LayoutMode.Center;
		if (base.IsMounted && !_isEditorRegistered)
		{
			_isEditorRegistered = true;
			ConfigEditor.MountedTimelineEditors.Add(this);
		}
	}

	protected override void Build()
	{
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Expected O, but got Unknown
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Invalid comparison between Unknown and I4
		SchemaNode schemaNode = ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(Schema.Value);
		if (!schemaNode.Properties.ContainsKey("Hour"))
		{
			throw new Exception("Timeline schema is invalid " + schemaNode);
		}
		_valueSchema = null;
		_valuePropertyKey = null;
		Padding.Vertical = 5;
		foreach (KeyValuePair<string, SchemaNode> property in schemaNode.Properties)
		{
			if (property.Key == "Hour" || property.Value.IsHidden)
			{
				continue;
			}
			_valueSchema = ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(property.Value);
			_valuePropertyKey = property.Key;
			break;
		}
		JToken[] array = (JToken[])(object)new JToken[24];
		if (base.Value != null)
		{
			JArray val = (JArray)base.Value;
			foreach (JToken item in val)
			{
				if (item != null && (int)item.Type != 10)
				{
					int num = (int)item[(object)"Hour"];
					if (num >= 0 && num < 24)
					{
						array[num] = item[(object)_valuePropertyKey];
					}
				}
			}
		}
		for (int i = 0; i < 24; i++)
		{
			Group group = new Group(Desktop, this)
			{
				Anchor = new Anchor
				{
					Height = 24,
					Width = 24,
					Horizontal = 2
				},
				LayoutMode = LayoutMode.Top,
				OutlineColor = UInt32Color.FromRGBA(125, 175, byte.MaxValue, byte.MaxValue)
			};
			_containers[i] = group;
			if (array[i] == null)
			{
				BuildPlaceholder(i);
				continue;
			}
			switch (_valueSchema.Type)
			{
			case SchemaNode.NodeType.Color:
				BuildColorPicker(array[i], i);
				break;
			case SchemaNode.NodeType.Number:
				BuildNumberField(array[i], i);
				break;
			default:
				throw new Exception($"TimelineEditor at {Path} does not support value schema of type " + _valueSchema.Type);
			}
		}
		if (base.IsMounted)
		{
			UpdateHighlightedHour(ConfigEditor.AssetEditorOverlay.WeatherDaytimeBar.CurrentHour);
		}
	}

	protected override void OnMounted()
	{
		base.OnMounted();
		if (ConfigEditor != null)
		{
			UpdateHighlightedHour(ConfigEditor.AssetEditorOverlay.WeatherDaytimeBar.CurrentHour);
			if (!_isEditorRegistered)
			{
				_isEditorRegistered = true;
				ConfigEditor.MountedTimelineEditors.Add(this);
			}
		}
	}

	protected override void OnUnmounted()
	{
		base.OnUnmounted();
		if (_isEditorRegistered)
		{
			_isEditorRegistered = false;
			ConfigEditor?.MountedTimelineEditors.Remove(this);
		}
	}

	public void UpdateHighlightedHour(int hour)
	{
		for (int i = 0; i < 24; i++)
		{
			_containers[i].OutlineSize = ((hour == i) ? 1 : 0);
			if (_colorPickers[i] != null)
			{
				_colorPickers[i].OutlineSize = ((hour == i) ? 1 : 0);
			}
		}
	}

	private void BuildPlaceholder(int hour)
	{
		new TextButton(Desktop, _containers[hour])
		{
			Text = "+",
			TooltipText = Desktop.Provider.GetText("ui.assetEditor.timelineEditor.insertAt", new Dictionary<string, string> { 
			{
				"hour",
				Desktop.Provider.FormatNumber(hour)
			} }),
			TextTooltipStyle = new TextTooltipStyle
			{
				MaxWidth = 140,
				LabelStyle = new LabelStyle
				{
					Wrap = true,
					FontSize = 13f
				}
			},
			Style = new TextButton.TextButtonStyle
			{
				Default = new TextButton.TextButtonStyleState
				{
					Background = new PatchStyle(UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 20)),
					LabelStyle = new LabelStyle
					{
						TextColor = UInt32Color.Transparent
					}
				},
				Hovered = new TextButton.TextButtonStyleState
				{
					Background = new PatchStyle(UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 40)),
					LabelStyle = new LabelStyle
					{
						Alignment = LabelStyle.LabelAlignment.Center,
						TextColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 70),
						RenderBold = true
					}
				}
			},
			OutlineColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 150),
			Anchor = new Anchor
			{
				Width = 24,
				Height = 24
			},
			Activating = delegate
			{
				InsertEntry(hour);
			}
		};
	}

	private void BuildColorPicker(JToken jValue, int hour)
	{
		UInt32Color color = ((_valueSchema.ColorFormat == ColorPicker.ColorFormat.Rgba) ? UInt32Color.Transparent : UInt32Color.White);
		string text = ((jValue != null) ? ((string)jValue).Trim() : "");
		if (text == "" && _valueSchema.DefaultValue != null)
		{
			text = (string)_valueSchema.DefaultValue;
		}
		if (text != "" && TryParseColor(text, out var color2))
		{
			color = color2;
		}
		_colorPickers[hour] = new ColorPickerDropdownBox(Desktop, _containers[hour])
		{
			Color = color,
			Format = _valueSchema.ColorFormat,
			ResetTransparencyWhenChangingColor = true,
			DisplayTextField = true,
			TooltipText = Desktop.Provider.GetText("ui.assetEditor.timelineEditor.updateAt", new Dictionary<string, string> { 
			{
				"hour",
				Desktop.Provider.FormatNumber(hour)
			} }),
			TextTooltipStyle = new TextTooltipStyle
			{
				MaxWidth = 120,
				LabelStyle = new LabelStyle
				{
					Wrap = true,
					FontSize = 13f
				}
			},
			RightClicking = delegate
			{
				RemoveEntry(hour);
			},
			ValueChanged = delegate
			{
				string text2 = ((_valueSchema.ColorFormat != ColorPicker.ColorFormat.RgbShort) ? ColorUtils.FormatColor(_colorPickers[hour].Color, (_valueSchema.ColorFormat == ColorPicker.ColorFormat.Rgba) ? ColorUtils.ColorFormatType.HexAlpha : ColorUtils.ColorFormatType.Hex) : _colorPickers[hour].Color.ToShortHexString());
				HandleTimeValueChanged(hour, JToken.FromObject((object)text2), clear: false, withheldCommand: false);
			},
			Style = ConfigEditor.ColorPickerDropdownBoxStyle,
			OutlineColor = UInt32Color.FromRGBA(125, 175, byte.MaxValue, byte.MaxValue),
			Anchor = new Anchor
			{
				Width = 24,
				Height = 24
			}
		};
	}

	private void BuildNumberField(JToken jValue, int hour)
	{
		decimal num = ((_valueSchema.DefaultValue != null) ? JsonUtils.ConvertToDecimal(_valueSchema.DefaultValue) : 0m);
		NumberFieldFormat numberFieldFormat = new NumberFieldFormat
		{
			DefaultValue = num,
			MaxDecimalPlaces = _valueSchema.MaxDecimalPlaces,
			Suffix = _valueSchema.Suffix
		};
		if (_valueSchema.Min.HasValue)
		{
			numberFieldFormat.MinValue = JsonUtils.ConvertToDecimal(_valueSchema.Min.Value);
		}
		if (_valueSchema.Max.HasValue)
		{
			numberFieldFormat.MaxValue = JsonUtils.ConvertToDecimal(_valueSchema.Max.Value);
		}
		if (_valueSchema.Step.HasValue)
		{
			numberFieldFormat.Step = JsonUtils.ConvertToDecimal(_valueSchema.Step.Value);
		}
		_numberFields[hour] = new NumberField(Desktop, _containers[hour])
		{
			Value = ((jValue != null) ? JsonUtils.ConvertToDecimal(jValue) : num),
			Format = numberFieldFormat,
			TooltipText = Desktop.Provider.GetText("ui.assetEditor.timelineEditor.updateAt", new Dictionary<string, string> { 
			{
				"hour",
				Desktop.Provider.FormatNumber(hour)
			} }),
			TextTooltipStyle = new TextTooltipStyle
			{
				MaxWidth = 120,
				LabelStyle = new LabelStyle
				{
					Wrap = true,
					FontSize = 13f
				}
			},
			Padding = new Padding
			{
				Left = 3
			},
			PlaceholderStyle = new InputFieldStyle
			{
				TextColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 100),
				FontSize = 11f
			},
			Style = new InputFieldStyle
			{
				FontSize = 11f
			},
			Blurred = delegate
			{
				OnNumberFieldBlur(hour);
			},
			Validating = delegate
			{
				OnNumberFieldValidate(hour);
			},
			RightClicking = delegate
			{
				RemoveEntry(hour);
			},
			ValueChanged = delegate
			{
				if (_valueSchema.MaxDecimalPlaces == 0)
				{
					int num2 = (int)_numberFields[hour].Value;
					HandleTimeValueChanged(hour, JToken.op_Implicit(num2), clear: false, withheldCommand: true);
				}
				else
				{
					decimal value = _numberFields[hour].Value;
					HandleTimeValueChanged(hour, JToken.op_Implicit(value), clear: false, withheldCommand: true);
				}
			},
			Decoration = new InputFieldDecorationStyle
			{
				Default = new InputFieldDecorationStyleState
				{
					Background = new PatchStyle(UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 20)),
					OutlineColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 150)
				},
				Focused = new InputFieldDecorationStyleState
				{
					OutlineSize = 1,
					OutlineColor = UInt32Color.FromRGBA(244, 188, 81, 153)
				}
			},
			Anchor = new Anchor
			{
				Width = 24,
				Height = 24
			}
		};
	}

	private bool TryParseColor(string value, out UInt32Color color)
	{
		ColorUtils.ColorFormatType formatType;
		switch (_valueSchema.ColorFormat)
		{
		case ColorPicker.ColorFormat.RgbShort:
			if (value.StartsWith("#") && value.Length == 4)
			{
				color = UInt32Color.FromShortHexString(value);
				return true;
			}
			break;
		case ColorPicker.ColorFormat.Rgb:
			if (ColorUtils.TryParseColor(value, out color, out formatType))
			{
				return true;
			}
			break;
		case ColorPicker.ColorFormat.Rgba:
			if (ColorUtils.TryParseColorAlpha(value, out color, out formatType))
			{
				return true;
			}
			break;
		}
		color = UInt32Color.White;
		return false;
	}

	private void RemoveEntry(int hour)
	{
		HandleTimeValueChanged(hour, null, clear: true, withheldCommand: false);
		_containers[hour].Clear();
		switch (_valueSchema.Type)
		{
		case SchemaNode.NodeType.Color:
			_colorPickers[hour] = null;
			break;
		case SchemaNode.NodeType.Number:
			_numberFields[hour] = null;
			break;
		}
		BuildPlaceholder(hour);
		Layout();
	}

	private void InsertEntry(int hour)
	{
		JToken val = null;
		_containers[hour].Clear();
		switch (_valueSchema.Type)
		{
		case SchemaNode.NodeType.Color:
			val = JToken.op_Implicit((_valueSchema.DefaultValue != null) ? ((string)_valueSchema.DefaultValue) : "#ffffff");
			BuildColorPicker(val, hour);
			_colorPickers[hour].Open();
			break;
		case SchemaNode.NodeType.Number:
			val = JToken.op_Implicit((_valueSchema.DefaultValue != null) ? JsonUtils.ConvertToDecimal(_valueSchema.DefaultValue) : 0m);
			BuildNumberField(val, hour);
			Desktop.FocusElement(_numberFields[hour]);
			break;
		}
		HandleTimeValueChanged(hour, val, clear: false, withheldCommand: false);
		Layout();
	}

	private void OnNumberFieldValidate(int hour)
	{
		if (ParentPropertyEditor.SyncPropertyChanges)
		{
			if (_valueSchema.MaxDecimalPlaces == 0)
			{
				HandleTimeValueChanged(hour, JToken.op_Implicit((int)_numberFields[hour].Value), clear: false, withheldCommand: false);
			}
			else
			{
				HandleTimeValueChanged(hour, JToken.op_Implicit(_numberFields[hour].Value), clear: false, withheldCommand: false);
			}
		}
		else
		{
			SubmitUpdateCommand();
		}
		Validate();
	}

	private void OnNumberFieldBlur(int hour)
	{
		if (ParentPropertyEditor.SyncPropertyChanges)
		{
			if (_valueSchema.MaxDecimalPlaces == 0)
			{
				HandleTimeValueChanged(hour, JToken.op_Implicit((int)_numberFields[hour].Value), clear: false, withheldCommand: false);
			}
			else
			{
				HandleTimeValueChanged(hour, JToken.op_Implicit(_numberFields[hour].Value), clear: false, withheldCommand: false);
			}
		}
		else
		{
			SubmitUpdateCommand();
		}
	}

	private void HandleTimeValueChanged(int targetHour, JToken targetValue, bool clear, bool withheldCommand)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Expected O, but got Unknown
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Invalid comparison between Unknown and I4
		JToken value = base.Value;
		JToken previousValue = ((value != null) ? value.DeepClone() : null);
		JArray val = (JArray)base.Value;
		if (ParentPropertyEditor.SyncPropertyChanges && !withheldCommand)
		{
			val = new JArray();
			if (!clear)
			{
				for (int i = 0; i < 24; i++)
				{
					JArray obj = val;
					JObject val2 = new JObject();
					val2.Add("Hour", JToken.op_Implicit(i));
					val2.Add(_valuePropertyKey, targetValue);
					obj.Add((JToken)val2);
				}
			}
		}
		else
		{
			int num = -1;
			if (base.Value != null)
			{
				JArray val3 = (JArray)base.Value;
				for (int j = 0; j < ((JContainer)val3).Count; j++)
				{
					JToken val4 = val3[j];
					if (val4 != null && (int)val4.Type != 10)
					{
						int num2 = (int)val4[(object)"Hour"];
						if (num2 == targetHour)
						{
							num = j;
							break;
						}
					}
				}
			}
			if (val == null)
			{
				val = new JArray();
			}
			if (num == -1)
			{
				if (!clear)
				{
					JArray obj2 = val;
					JObject val5 = new JObject();
					val5.Add("Hour", JToken.op_Implicit(targetHour));
					val5.Add(_valuePropertyKey, targetValue);
					obj2.Add((JToken)val5);
				}
			}
			else if (!clear)
			{
				val[num][(object)_valuePropertyKey] = targetValue;
			}
			else
			{
				val.RemoveAt(num);
			}
		}
		if (((JContainer)val).Count == 0)
		{
			val = null;
		}
		bool flag = base.Value != val;
		ConfigEditor.OnChangeValue(Path, (JToken)(object)val, previousValue, CachesToRebuild?.Caches, withheldCommand);
		ParentPropertyEditor?.UpdateAppearance();
		if (flag)
		{
			Layout();
		}
	}

	public override void SetValueRecursively(JToken value)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Invalid comparison between Unknown and I4
		if (value == base.Value)
		{
			return;
		}
		base.SetValueRecursively(value);
		JToken[] array = (JToken[])(object)new JToken[24];
		if (base.Value != null)
		{
			JArray val = (JArray)base.Value;
			foreach (JToken item in val)
			{
				if (item != null && (int)item.Type != 10)
				{
					int num = (int)item[(object)"Hour"];
					if (num >= 0 && num < 24)
					{
						array[num] = item[(object)_valuePropertyKey];
					}
				}
			}
		}
		for (int i = 0; i < 24; i++)
		{
			switch (_valueSchema.Type)
			{
			case SchemaNode.NodeType.Color:
				if (_colorPickers[i] != null)
				{
					if (array[i] != null)
					{
						if (TryParseColor((string)array[i], out var color))
						{
							_colorPickers[i].Color = color;
						}
						else
						{
							_colorPickers[i].Color = ((_valueSchema.ColorFormat == ColorPicker.ColorFormat.Rgba) ? UInt32Color.Transparent : UInt32Color.White);
						}
					}
					else
					{
						_colorPickers[i].Parent.Remove(_colorPickers[i]);
						_colorPickers[i] = null;
						BuildPlaceholder(i);
					}
				}
				else if (array[i] != null)
				{
					_containers[i].Clear();
					BuildColorPicker(array[i], i);
				}
				break;
			case SchemaNode.NodeType.Number:
				if (_numberFields[i] != null)
				{
					if (array[i] != null)
					{
						_numberFields[i].Value = JsonUtils.ConvertToDecimal(array[i]);
						break;
					}
					_numberFields[i].Parent.Remove(_numberFields[i]);
					_numberFields[i] = null;
					BuildPlaceholder(i);
				}
				else if (array[i] != null)
				{
					_containers[i].Clear();
					BuildNumberField(array[i], i);
				}
				break;
			default:
				throw new Exception("TimelineEditor does not support value schema of type " + _valueSchema.Type);
			}
		}
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)value.Type == 2;
	}
}
