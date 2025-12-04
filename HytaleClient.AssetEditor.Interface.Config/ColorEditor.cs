using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class ColorEditor : ValueEditor
{
	private ColorPickerDropdownBox _colorPickerDropdownBox;

	private TextField _textInput;

	public ColorEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
	}

	protected override void Build()
	{
		UInt32Color color = GetColor();
		TextField textField = new TextField(Desktop, this);
		SchemaNode parentSchema = _parentSchema;
		textField.Visible = parentSchema == null || parentSchema.Type != SchemaNode.NodeType.Timeline;
		textField.Value = ((string)base.Value) ?? "";
		textField.PlaceholderText = (string)Schema.DefaultValue;
		textField.Decoration = new InputFieldDecorationStyle
		{
			Focused = new InputFieldDecorationStyleState
			{
				OutlineSize = 1,
				OutlineColor = UInt32Color.FromRGBA(244, 188, 81, 153)
			}
		};
		textField.PlaceholderStyle = new InputFieldStyle
		{
			TextColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 100)
		};
		textField.Style = new InputFieldStyle
		{
			FontSize = 14f
		};
		textField.Padding = new Padding
		{
			Left = 32
		};
		textField.Anchor = new Anchor
		{
			Left = 1
		};
		textField.FlexWeight = 1;
		textField.Blurred = OnTextElementBlur;
		textField.Dismissing = OnTextElementDismissing;
		textField.Validating = delegate
		{
			_textInput.Blurred();
		};
		_textInput = textField;
		_colorPickerDropdownBox = new ColorPickerDropdownBox(Desktop, this)
		{
			Color = color,
			Format = Schema.ColorFormat,
			ValueChanged = OnColorPickerChange,
			Style = ConfigEditor.ColorPickerDropdownBoxStyle,
			DisplayTextField = true,
			Anchor = new Anchor
			{
				Width = 24,
				Height = 24,
				Left = 4,
				Top = 4
			}
		};
	}

	private UInt32Color GetColor()
	{
		UInt32Color color = ((Schema.ColorFormat == ColorPicker.ColorFormat.Rgba) ? UInt32Color.Transparent : UInt32Color.White);
		string text = ((base.Value != null) ? ((string)base.Value).Trim() : "");
		if (text == "" && Schema.DefaultValue != null)
		{
			text = (string)Schema.DefaultValue;
		}
		bool flag = false;
		if (text != "")
		{
			flag = TryParseColor(text, out color);
		}
		if (!flag && Schema.DefaultValue != null)
		{
			TryParseColor((string)Schema.DefaultValue, out color);
		}
		return color;
	}

	private bool TryParseColor(string value, out UInt32Color color)
	{
		ColorUtils.ColorFormatType formatType;
		if (Schema.ColorFormat == ColorPicker.ColorFormat.RgbShort)
		{
			if (value.StartsWith("#") && value.Length == 4)
			{
				color = UInt32Color.FromShortHexString(value);
				return true;
			}
		}
		else if (Schema.ColorFormat == ColorPicker.ColorFormat.Rgb)
		{
			if (ColorUtils.TryParseColor(value, out color, out formatType))
			{
				return true;
			}
		}
		else if (ColorUtils.TryParseColorAlpha(value, out color, out formatType))
		{
			return true;
		}
		color = UInt32Color.White;
		return false;
	}

	private void OnTextElementDismissing()
	{
		UpdateDisplayedValue();
	}

	private void OnTextElementBlur()
	{
		string text = _textInput.Value.Trim();
		UInt32Color color;
		if (text == "")
		{
			if (base.Value != null)
			{
				ParentPropertyEditor.HandleRemoveProperty();
			}
		}
		else if (!TryParseColor(text, out color))
		{
			_textInput.Value = ((string)base.Value) ?? "";
		}
		else if (!((string)base.Value == _textInput.Value))
		{
			_colorPickerDropdownBox.Color = color;
			HandleChangeValue(JToken.FromObject((object)_textInput.Value));
			Validate();
		}
	}

	private void OnColorPickerChange()
	{
		string text;
		UInt32Color color;
		if (Schema.ColorFormat == ColorPicker.ColorFormat.RgbShort)
		{
			text = _colorPickerDropdownBox.Color.ToShortHexString();
		}
		else if (Schema.ColorFormat == ColorPicker.ColorFormat.Rgba)
		{
			if (!ColorUtils.TryParseColorAlpha(_textInput.Value.Trim(), out color, out var formatType))
			{
				formatType = ColorUtils.ColorFormatType.HexAlpha;
			}
			text = ColorUtils.FormatColor(_colorPickerDropdownBox.Color, formatType);
		}
		else
		{
			if (!ColorUtils.TryParseColor(_textInput.Value.Trim(), out color, out var formatType2))
			{
				formatType2 = ((Schema.ColorFormat == ColorPicker.ColorFormat.Rgba) ? ColorUtils.ColorFormatType.HexAlpha : ColorUtils.ColorFormatType.Hex);
			}
			text = ColorUtils.FormatColor(_colorPickerDropdownBox.Color, formatType2);
		}
		HandleChangeValue(JToken.FromObject((object)text));
		_textInput.Value = text;
		Validate();
	}

	protected internal override void UpdateDisplayedValue()
	{
		UInt32Color color = GetColor();
		_colorPickerDropdownBox.Color = color;
		_textInput.Value = ((string)base.Value) ?? "";
	}

	protected override bool IsValueEmptyOrDefault(JToken value)
	{
		if (Schema.DefaultValue == null)
		{
			return false;
		}
		if ((string)value == (string)Schema.DefaultValue)
		{
			return true;
		}
		if (TryParseColor((string)value, out var color) && TryParseColor((string)Schema.DefaultValue, out var color2))
		{
			return color.Equals(color2);
		}
		return false;
	}

	public override void Focus()
	{
		_colorPickerDropdownBox.Open();
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)value.Type == 8;
	}
}
