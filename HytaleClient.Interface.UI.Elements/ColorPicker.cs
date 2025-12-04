using System;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class ColorPicker : InputElement<UInt32Color>
{
	public enum ColorFormat
	{
		Rgb,
		Rgba,
		RgbShort
	}

	private readonly HsvPicker _hsvPicker;

	private readonly Group _opacitySelectorSpacer;

	private readonly HueSelector _hueSelector;

	private readonly OpacitySelector _opacitySelector;

	private readonly TextField _textField;

	private ColorFormat _colorFormat = ColorFormat.Rgba;

	private bool _wasTextFieldChanged;

	[UIMarkupProperty]
	public ColorPickerStyle Style = new ColorPickerStyle();

	[UIMarkupProperty]
	public bool ResetTransparencyWhenChangingColor;

	public override UInt32Color Value
	{
		get
		{
			new ColorHsva(_hsvPicker.Hue, _hsvPicker.Saturation, _hsvPicker.Value, _opacitySelector.Opacity).ToRgba(out byte r, out byte g, out byte b, out byte a);
			return UInt32Color.FromRGBA(r, g, b, a);
		}
		set
		{
			SetColor(value);
		}
	}

	[UIMarkupProperty]
	public ColorFormat Format
	{
		get
		{
			return _colorFormat;
		}
		set
		{
			_colorFormat = value;
			_hsvPicker.IsShortColor = value == ColorFormat.RgbShort;
		}
	}

	[UIMarkupProperty]
	public bool DisplayTextField
	{
		get
		{
			return _textField.Visible;
		}
		set
		{
			_textField.Visible = value;
		}
	}

	public ColorPicker(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_layoutMode = LayoutMode.Top;
		Group parent2 = new Group(Desktop, this)
		{
			LayoutMode = LayoutMode.Left,
			FlexWeight = 1
		};
		_hsvPicker = new HsvPicker(desktop, parent2)
		{
			FlexWeight = 1,
			ValueChanged = delegate
			{
				if (ResetTransparencyWhenChangingColor && Format == ColorFormat.Rgba && (double)System.Math.Abs(_opacitySelector.Opacity) < 0.0001)
				{
					_opacitySelector.Opacity = 1f;
					_opacitySelector.Layout();
				}
			},
			MouseButtonReleased = delegate
			{
				UpdateTextField(Value);
				ValueChanged?.Invoke();
			},
			IsShortColor = (_colorFormat == ColorFormat.RgbShort)
		};
		new Group(desktop, parent2).Anchor = new Anchor
		{
			Width = 10
		};
		_hueSelector = new HueSelector(desktop, parent2)
		{
			Anchor = new Anchor
			{
				Width = 10
			},
			ValueChanged = delegate(float hue)
			{
				_hsvPicker.SetHue(hue);
				if (ResetTransparencyWhenChangingColor && Format == ColorFormat.Rgba && (double)System.Math.Abs(_opacitySelector.Opacity) < 0.0001)
				{
					_opacitySelector.Opacity = 1f;
					_opacitySelector.Layout();
				}
			},
			MouseButtonReleased = delegate
			{
				UpdateTextField(Value);
				ValueChanged?.Invoke();
			}
		};
		_opacitySelectorSpacer = new Group(desktop, this)
		{
			Anchor = new Anchor
			{
				Height = 10
			}
		};
		_opacitySelector = new OpacitySelector(desktop, this)
		{
			Anchor = new Anchor
			{
				Height = 10,
				Left = 0,
				Right = 20
			},
			MouseButtonReleased = delegate
			{
				UpdateTextField(Value);
				ValueChanged?.Invoke();
			}
		};
		_textField = new TextField(desktop, this)
		{
			Visible = false,
			ValueChanged = OnTextFieldValueChanged,
			Blurred = OnTextFieldBlurred,
			Anchor = new Anchor
			{
				Top = 10
			}
		};
	}

	protected override void OnMounted()
	{
		UpdateTextField(Value);
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_opacitySelector.Visible = Format == ColorFormat.Rgba;
		_opacitySelectorSpacer.Visible = Format == ColorFormat.Rgba;
		_hsvPicker.Style = Style;
		_hueSelector.Style = Style;
		_opacitySelector.Style = Style;
		_textField.Decoration = Style.TextFieldDecoration;
		_textField.Style = Style.TextFieldInputStyle;
		_textField.Padding = Style.TextFieldPadding;
		_textField.Anchor.Height = Style.TextFieldHeight;
	}

	public void SetColor(UInt32Color value, bool updateTextField = true)
	{
		ColorRgba colorRgba = ColorRgba.FromUInt32Color(value);
		ColorHsva colorHsva = ColorHsva.FromRgba(colorRgba.R, colorRgba.G, colorRgba.B, byte.MaxValue);
		_hsvPicker.SetColor(colorHsva.H, colorHsva.S, colorHsva.V);
		_hueSelector.Hue = colorHsva.H;
		_opacitySelector.Opacity = (float)(int)colorRgba.A / 255f;
		if (updateTextField)
		{
			UpdateTextField(value);
		}
	}

	private void UpdateTextField(UInt32Color value)
	{
		switch (_colorFormat)
		{
		case ColorFormat.RgbShort:
			_textField.Value = value.ToShortHexString();
			break;
		case ColorFormat.Rgb:
			_textField.Value = value.ToHexString(includeAlphaChannel: false);
			break;
		case ColorFormat.Rgba:
			_textField.Value = value.ToHexString();
			break;
		}
	}

	public ColorRgba GetColorRgba()
	{
		new ColorHsva(_hsvPicker.Hue, _hsvPicker.Saturation, _hsvPicker.Value, 1f).ToRgba(out byte r, out byte g, out byte b, out byte _);
		return new ColorRgba(r, g, b, (byte)(int)(_opacitySelector.Opacity * 255f));
	}

	private void OnTextFieldValueChanged()
	{
		string value = _textField.Value.Trim();
		if (TryParseColor(value, out var color))
		{
			_wasTextFieldChanged = true;
			SetColor(color, updateTextField: false);
			Layout();
		}
	}

	private bool TryParseColor(string value, out UInt32Color color)
	{
		ColorUtils.ColorFormatType formatType;
		if (Format == ColorFormat.RgbShort)
		{
			if (value.StartsWith("#") && value.Length == 4)
			{
				color = UInt32Color.FromShortHexString(value);
				return true;
			}
		}
		else if (Format == ColorFormat.Rgb)
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

	private void OnTextFieldBlurred()
	{
		UpdateTextField(Value);
		if (_wasTextFieldChanged)
		{
			_wasTextFieldChanged = false;
			ValueChanged?.Invoke();
		}
	}

	public override Element HitTest(Point position)
	{
		if (!base.IsMounted || _waitingForLayoutAfterMount || !_anchoredRectangle.Contains(position))
		{
			return null;
		}
		Element element = _hsvPicker.HitTest(position);
		if (element == null)
		{
			element = _hueSelector.HitTest(position);
		}
		if (element == null)
		{
			element = _opacitySelector.HitTest(position);
		}
		if (element == null)
		{
			element = _textField.HitTest(position);
		}
		return element;
	}
}
