using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class WeightedTimelineEditor : ValueEditor
{
	private const int HoursPerDay = 24;

	private const string WeightPropertyKey = "Weight";

	private Dictionary<string, NumberField[]> _numberFields = new Dictionary<string, NumberField[]>();

	private Dictionary<string, Group> _entryGroups = new Dictionary<string, Group>();

	private string _idPropertyKey;

	private SchemaNode _weightSchema;

	public SchemaNode IdSchema { get; private set; }

	public WeightedTimelineEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
	}

	protected override void Build()
	{
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Expected O, but got Unknown
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Invalid comparison between Unknown and I4
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Expected O, but got Unknown
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Invalid comparison between Unknown and I4
		Clear();
		_numberFields.Clear();
		_entryGroups.Clear();
		_layoutMode = LayoutMode.Top;
		SchemaNode schemaNode = ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(Schema.Value.Value);
		_weightSchema = ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(schemaNode.Properties["Weight"]);
		foreach (KeyValuePair<string, SchemaNode> property in schemaNode.Properties)
		{
			if (property.Key == "Weight" || property.Value.IsHidden)
			{
				continue;
			}
			IdSchema = ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(property.Value);
			_idPropertyKey = property.Key;
			break;
		}
		_003F val = (JObject)base.Value;
		if ((int)val == 0)
		{
			val = new JObject();
		}
		JObject val2 = (JObject)val;
		Dictionary<string, decimal[]> dictionary = new Dictionary<string, decimal[]>();
		foreach (KeyValuePair<string, JToken> item in val2)
		{
			if (!int.TryParse(item.Key, out var result) || result < 0 || result >= 24 || item.Value == null || (int)item.Value.Type != 2)
			{
				continue;
			}
			JArray val3 = (JArray)item.Value;
			foreach (JToken item2 in val3)
			{
				if (item2 == null || (int)item2.Type != 1)
				{
					continue;
				}
				JToken val4 = item2[(object)_idPropertyKey];
				if (!dictionary.TryGetValue((string)val4, out var value))
				{
					decimal[] array2 = (dictionary[(string)val4] = new decimal[24]);
					value = array2;
					decimal num = ((_weightSchema.DefaultValue != null) ? ((decimal)_weightSchema.DefaultValue) : 0m);
					for (int i = 0; i < value.Length; i++)
					{
						value[i] = num;
					}
				}
				value[result] = JsonUtils.ConvertToDecimal(item2[(object)"Weight"]);
			}
		}
		foreach (KeyValuePair<string, decimal[]> item3 in dictionary)
		{
			BuildEntryGroup(item3.Key, item3.Value);
		}
	}

	private void BuildEntryGroup(string entryId, decimal[] weights)
	{
		Group group = new Group(Desktop, this)
		{
			LayoutMode = LayoutMode.Top,
			Anchor = new Anchor
			{
				Vertical = 5
			}
		};
		_entryGroups.Add(entryId, group);
		Group parent = new Group(Desktop, group)
		{
			LayoutMode = LayoutMode.Left,
			Anchor = new Anchor
			{
				Height = 26,
				Width = 672
			},
			Padding = 
			{
				Horizontal = 8
			},
			Background = new PatchStyle(UInt32Color.FromRGBA(0, 0, 0, 40))
		};
		new Label(Desktop, parent)
		{
			Text = entryId,
			Style = new LabelStyle
			{
				FontSize = 13f,
				VerticalAlignment = LabelStyle.LabelAlignment.Center,
				RenderBold = true
			},
			FlexWeight = 1,
			Padding = new Padding
			{
				Bottom = 3
			}
		};
		new Button(Desktop, parent)
		{
			Anchor = new Anchor
			{
				Width = 16,
				Height = 16
			},
			Style = new Button.ButtonStyle
			{
				Default = new Button.ButtonStyleState
				{
					Background = new PatchStyle(PropertyLabel.IconRemove)
					{
						Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 38)
					}
				},
				Hovered = new Button.ButtonStyleState
				{
					Background = new PatchStyle(PropertyLabel.IconRemove)
					{
						Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 120)
					}
				},
				Pressed = new Button.ButtonStyleState
				{
					Background = new PatchStyle(PropertyLabel.IconRemove)
					{
						Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 100)
					}
				}
			},
			Activating = delegate
			{
				HandleRemoveEntry(entryId);
			}
		};
		Group parent2 = new Group(Desktop, group)
		{
			LayoutMode = LayoutMode.Center
		};
		_numberFields[entryId] = new NumberField[24];
		for (int i = 0; i < weights.Length; i++)
		{
			Group container = new Group(Desktop, parent2)
			{
				Anchor = new Anchor
				{
					Height = 24,
					Width = 24,
					Horizontal = 2
				},
				LayoutMode = LayoutMode.Top
			};
			_numberFields[entryId][i] = BuildNumberField(container, _weightSchema, JToken.op_Implicit(weights[i]), entryId, i);
		}
	}

	private NumberField BuildNumberField(Group container, SchemaNode valueSchema, JToken jValue, string id, int hour)
	{
		decimal num = ((valueSchema.DefaultValue != null) ? JsonUtils.ConvertToDecimal(valueSchema.DefaultValue) : 0m);
		NumberFieldFormat numberFieldFormat = new NumberFieldFormat
		{
			DefaultValue = num,
			MaxDecimalPlaces = valueSchema.MaxDecimalPlaces,
			Suffix = valueSchema.Suffix
		};
		if (valueSchema.Min.HasValue)
		{
			numberFieldFormat.MinValue = JsonUtils.ConvertToDecimal(valueSchema.Min.Value);
		}
		if (valueSchema.Max.HasValue)
		{
			numberFieldFormat.MaxValue = JsonUtils.ConvertToDecimal(valueSchema.Max.Value);
		}
		if (valueSchema.Step.HasValue)
		{
			numberFieldFormat.Step = JsonUtils.ConvertToDecimal(valueSchema.Step.Value);
		}
		return new NumberField(Desktop, container)
		{
			Value = ((jValue != null) ? JsonUtils.ConvertToDecimal(jValue) : num),
			Format = numberFieldFormat,
			Padding = new Padding
			{
				Left = 3
			},
			PlaceholderStyle = new InputFieldStyle
			{
				TextColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 100),
				FontSize = 12f
			},
			Style = new InputFieldStyle
			{
				FontSize = 12f
			},
			Blurred = delegate
			{
				OnNumberFieldBlur(id, hour);
			},
			Validating = OnNumberFieldValidate,
			ValueChanged = delegate
			{
				if (valueSchema.MaxDecimalPlaces == 0)
				{
					int num2 = (int)_numberFields[id][hour].Value;
					HandleWeightValueChanged(hour, id, num2, withheldCommand: true);
				}
				else
				{
					decimal value = _numberFields[id][hour].Value;
					HandleWeightValueChanged(hour, id, value, withheldCommand: true);
				}
			},
			Decoration = new InputFieldDecorationStyle
			{
				Default = new InputFieldDecorationStyleState
				{
					Background = new PatchStyle(UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 20))
				},
				Focused = new InputFieldDecorationStyleState
				{
					OutlineSize = 1,
					OutlineColor = UInt32Color.FromRGBA(205, 240, 252, 206)
				}
			},
			Anchor = new Anchor
			{
				Width = 24,
				Height = 24
			}
		};
	}

	public void HandleInsertEntry(string entryId)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		JToken value = base.Value;
		JToken previousValue = ((value != null) ? value.DeepClone() : null);
		JToken value2 = base.Value;
		JObject val = (JObject)(object)((value2 is JObject) ? value2 : null);
		if (val != null)
		{
			foreach (KeyValuePair<string, JToken> item in val)
			{
				JArray val2 = (JArray)item.Value;
				JObject val3 = new JObject();
				val3.Add(_idPropertyKey, JToken.op_Implicit(entryId));
				val3.Add("Weight", JToken.op_Implicit((_weightSchema.DefaultValue != null) ? JsonUtils.ConvertToDecimal(_weightSchema.DefaultValue) : 0m));
				val2.Add((JToken)val3);
			}
		}
		decimal[] array = new decimal[24];
		decimal num = ((_weightSchema.DefaultValue != null) ? JsonUtils.ConvertToDecimal(_weightSchema.DefaultValue) : 0m);
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = num;
		}
		BuildEntryGroup(entryId, array);
		ConfigEditor.OnChangeValue(Path, base.Value, previousValue, CachesToRebuild?.Caches);
		ConfigEditor.Layout();
	}

	public bool HasEntryId(string entryId)
	{
		return _entryGroups.ContainsKey(entryId);
	}

	private void HandleRemoveEntry(string entryId)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		Group group = _entryGroups[entryId];
		group.Parent.Remove(group);
		_entryGroups.Remove(entryId);
		JToken value = base.Value;
		JToken previousValue = ((value != null) ? value.DeepClone() : null);
		foreach (KeyValuePair<string, JToken> item in (JObject)base.Value)
		{
			JArray val = (JArray)item.Value;
			foreach (JToken item2 in val)
			{
				if ((string)item2[(object)_idPropertyKey] != entryId)
				{
					continue;
				}
				item2.Remove();
				break;
			}
		}
		ConfigEditor.OnChangeValue(Path, base.Value, previousValue, CachesToRebuild?.Caches);
		ConfigEditor.Layout();
	}

	private void HandleWeightValueChanged(int targetHour, string targetId, decimal weight, bool withheldCommand)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Expected O, but got Unknown
		//IL_00b7: Expected O, but got Unknown
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Expected O, but got Unknown
		JToken value = base.Value;
		JToken previousValue = ((value != null) ? value.DeepClone() : null);
		JToken val = (JToken)(((object)base.Value) ?? ((object)new JObject()));
		for (int i = 0; i < 24; i++)
		{
			if (!ParentPropertyEditor.SyncPropertyChanges && i != targetHour)
			{
				continue;
			}
			if (ParentPropertyEditor.SyncPropertyChanges && i != targetHour)
			{
				_numberFields[targetId][i].Value = weight;
			}
			JArray val2 = (JArray)val[(object)i.ToString()];
			if (val2 == null)
			{
				string text = targetHour.ToString();
				JArray val3 = new JArray();
				val2 = val3;
				val[(object)text] = (JToken)val3;
			}
			bool flag = false;
			foreach (JToken item in val2)
			{
				if ((string)item[(object)_idPropertyKey] == targetId)
				{
					item[(object)"Weight"] = JToken.op_Implicit(weight);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				JArray obj = val2;
				JObject val4 = new JObject();
				val4.Add(_idPropertyKey, JToken.op_Implicit(targetId));
				val4.Add("Weight", JToken.op_Implicit(weight));
				obj.Add((JToken)val4);
			}
		}
		bool flag2 = base.Value != val;
		ConfigEditor.OnChangeValue(Path, val, previousValue, CachesToRebuild?.Caches, withheldCommand);
		if (flag2)
		{
			Layout();
		}
	}

	private void OnNumberFieldValidate()
	{
		Validate();
		SubmitUpdateCommand();
	}

	private void OnNumberFieldBlur(string id, int hour)
	{
		if (ParentPropertyEditor.SyncPropertyChanges)
		{
			if (_weightSchema.MaxDecimalPlaces == 0)
			{
				HandleWeightValueChanged(hour, id, (int)_numberFields[id][hour].Value, withheldCommand: false);
			}
			else
			{
				HandleWeightValueChanged(hour, id, _numberFields[id][hour].Value, withheldCommand: false);
			}
		}
		else
		{
			SubmitUpdateCommand();
		}
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)value.Type == 1;
	}

	public override void SetValueRecursively(JToken value)
	{
		if (value != base.Value)
		{
			base.SetValueRecursively(value);
			Build();
		}
	}
}
