using System;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class NumberEditor : ValueEditor
{
	private NumberField _numberField;

	private Slider _slider;

	public NumberEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
	}

	protected override void OnUnmounted()
	{
		base.OnUnmounted();
		SubmitUpdateCommand();
	}

	protected override void Build()
	{
		NumberFieldFormat numberFieldFormat = new NumberFieldFormat
		{
			DefaultValue = ((Schema.DefaultValue != null) ? JsonUtils.ConvertToDecimal(Schema.DefaultValue) : 0m),
			MaxDecimalPlaces = Schema.MaxDecimalPlaces,
			Suffix = Schema.Suffix
		};
		if (Schema.Min.HasValue)
		{
			numberFieldFormat.MinValue = JsonUtils.ConvertToDecimal(Schema.Min.Value);
		}
		if (Schema.Max.HasValue)
		{
			numberFieldFormat.MaxValue = JsonUtils.ConvertToDecimal(Schema.Max.Value);
		}
		if (Schema.Step.HasValue)
		{
			numberFieldFormat.Step = JsonUtils.ConvertToDecimal(Schema.Step.Value);
		}
		_numberField = new NumberField(Desktop, this)
		{
			Value = GetValue(),
			Format = numberFieldFormat,
			Padding = new Padding
			{
				Left = 5
			},
			Anchor = new Anchor
			{
				Width = 80,
				Left = 0
			},
			PlaceholderStyle = new InputFieldStyle
			{
				TextColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 100),
				FontSize = 14f
			},
			Style = new InputFieldStyle
			{
				FontSize = 14f
			},
			Blurred = OnNumberFieldBlur,
			Validating = OnNumberFieldValidate,
			ValueChanged = OnNumberFieldChange,
			Decoration = new InputFieldDecorationStyle
			{
				Focused = new InputFieldDecorationStyleState
				{
					OutlineSize = 1,
					OutlineColor = UInt32Color.FromRGBA(244, 188, 81, 153)
				}
			}
		};
		SchemaNode parentSchema = _parentSchema;
		if (parentSchema != null && parentSchema.DisplayCompact)
		{
			_numberField.FlexWeight = 1;
		}
		else if (Schema.Min.HasValue && Schema.Max.HasValue)
		{
			_layoutMode = LayoutMode.Left;
			Group parent = new Group(Desktop, this)
			{
				FlexWeight = 1,
				Padding = new Padding
				{
					Horizontal = 12
				}
			};
			decimal num = (decimal)System.Math.Pow(10.0, Schema.MaxDecimalPlaces);
			_slider = new Slider(Desktop, parent)
			{
				Value = (int)(GetValue() * num),
				Style = ConfigEditor.SliderStyle,
				Min = (int)(JsonUtils.ConvertToDecimal(Schema.Min.Value) * num),
				Max = (int)(JsonUtils.ConvertToDecimal(Schema.Max.Value) * num),
				ValueChanged = OnSliderChanged,
				MouseButtonReleased = OnSliderReleased,
				Anchor = new Anchor
				{
					MaxWidth = 250,
					Height = 4,
					Left = 0
				}
			};
		}
	}

	private decimal GetValue()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Invalid comparison between Unknown and I4
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Invalid comparison between Unknown and I4
		if (base.Value == null)
		{
			return (Schema.DefaultValue != null) ? JsonUtils.ConvertToDecimal(Schema.DefaultValue) : 0m;
		}
		if ((int)base.Value.Type == 6 || (int)base.Value.Type == 7)
		{
			return JsonUtils.ConvertToDecimal(base.Value);
		}
		if (Schema.MaxDecimalPlaces > 0 && (int)base.Value.Type == 8)
		{
			return 0m;
		}
		throw new Exception("Invalid value type for " + (object)base.Value);
	}

	private void OnNumberFieldChange()
	{
		if (_slider != null)
		{
			_slider.Value = (int)(_numberField.Value * (decimal)System.Math.Pow(10.0, Schema.MaxDecimalPlaces));
			_slider.Layout();
		}
		if (Schema.MaxDecimalPlaces == 0)
		{
			HandleChangeValue(JToken.op_Implicit((int)_numberField.Value), withheldCommand: true);
		}
		else
		{
			HandleChangeValue(JToken.op_Implicit(_numberField.Value), withheldCommand: true);
		}
	}

	private void OnSliderChanged()
	{
		decimal num = (decimal)_slider.Value / (decimal)System.Math.Pow(10.0, Schema.MaxDecimalPlaces);
		_numberField.Value = num;
		if (Schema.MaxDecimalPlaces == 0)
		{
			HandleChangeValue(JToken.op_Implicit((int)num), withheldCommand: true);
		}
		else
		{
			HandleChangeValue(JToken.op_Implicit(num), withheldCommand: true);
		}
	}

	private void OnSliderReleased()
	{
		SubmitUpdateCommand();
	}

	private void OnNumberFieldValidate()
	{
		Validate();
		SubmitUpdateCommand();
	}

	private void OnNumberFieldBlur()
	{
		SubmitUpdateCommand();
	}

	protected override bool IsValueEmptyOrDefault(JToken value)
	{
		double num = ((Schema.DefaultValue != null) ? ((double)Schema.DefaultValue) : 0.0);
		if (System.Math.Abs(num - (double)value) < 1E-05)
		{
			return true;
		}
		return false;
	}

	public override void Focus()
	{
		Desktop.FocusElement(_numberField);
	}

	protected internal override void UpdateDisplayedValue()
	{
		_numberField.Value = GetValue();
		if (_slider != null)
		{
			_slider.Value = (int)(_numberField.Value * (decimal)System.Math.Pow(10.0, Schema.MaxDecimalPlaces));
			_slider.Layout();
		}
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Invalid comparison between Unknown and I4
		if ((int)value.Type == 6 || (int)value.Type == 7)
		{
			return true;
		}
		if (Schema.MaxDecimalPlaces > 0 && (int)value.Type == 8)
		{
			string text = (string)value;
			if (text == "Infinity" || text == "-Infinity" || text == "NaN")
			{
				return true;
			}
		}
		return false;
	}

	protected override JToken SanitizeValue(JToken value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		if ((int)value.Type == 6 || (int)value.Type == 7)
		{
			double num = (double)value;
			if (Schema.Min.HasValue && num < Schema.Min)
			{
				value = JToken.op_Implicit(Schema.Min.Value);
			}
			else if (Schema.Max.HasValue && num > Schema.Max)
			{
				value = JToken.op_Implicit(Schema.Max.Value);
			}
		}
		return value;
	}
}
