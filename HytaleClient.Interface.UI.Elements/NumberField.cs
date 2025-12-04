using System;
using System.Globalization;
using HytaleClient.Interface.UI.Markup;
using SDL2;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class NumberField : InputField<decimal>
{
	private decimal _value;

	[UIMarkupProperty]
	public NumberFieldFormat Format = new NumberFieldFormat();

	public override decimal Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (value != _value)
			{
				_value = value;
				HasValidValue = true;
				UpdateText();
			}
		}
	}

	public bool HasValidValue { get; private set; }

	public NumberField(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void OnMounted()
	{
		base.OnMounted();
		UpdateText();
	}

	protected override void LayoutSelf()
	{
		UpdateText();
		base.LayoutSelf();
	}

	private bool TryParseAsDecimal(out decimal value)
	{
		string text = _text.ToLowerInvariant().Trim();
		if (Format.Suffix != null)
		{
			string text2 = Format.Suffix.ToLowerInvariant().Trim();
			if (text.EndsWith(text2))
			{
				text = text.Substring(0, text.Length - text2.Length).Trim();
			}
		}
		return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
	}

	private void ClampValue(ref decimal value)
	{
		if (value < Format.MinValue)
		{
			value = Format.MinValue;
		}
		if (value > Format.MaxValue)
		{
			value = Format.MaxValue;
		}
	}

	private void SetValueFromDecimal(decimal value)
	{
		ClampValue(ref value);
		HasValidValue = true;
		_value = value;
		UpdateText();
	}

	private void NudgeNumberValue(bool increment)
	{
		if (!TryParseAsDecimal(out var value))
		{
			if (!string.IsNullOrWhiteSpace(_text))
			{
				return;
			}
			value = Format.DefaultValue;
		}
		try
		{
			value += (increment ? Format.Step : (-Format.Step));
		}
		catch (OverflowException)
		{
			return;
		}
		SetValueFromDecimal(value);
		ValueChanged?.Invoke();
	}

	protected internal override void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Invalid comparison between Unknown and I4
		base.OnKeyDown(keycode, repeat);
		if ((int)keycode != 1073741905)
		{
			if ((int)keycode == 1073741906)
			{
				NudgeNumberValue(increment: true);
			}
		}
		else
		{
			NudgeNumberValue(increment: false);
		}
	}

	protected override void OnTextChanged()
	{
		decimal value;
		if (string.IsNullOrWhiteSpace(_text))
		{
			_value = Format.DefaultValue;
			HasValidValue = true;
			UpdateText();
		}
		else if (!TryParseAsDecimal(out value))
		{
			if (!_isFocused)
			{
				HasValidValue = true;
				UpdateText();
			}
			else
			{
				HasValidValue = false;
			}
		}
		else
		{
			ClampValue(ref value);
			HasValidValue = true;
			_value = value;
		}
	}

	protected internal override void OnBlur()
	{
		decimal value = _value;
		ClampValue(ref _value);
		HasValidValue = true;
		UpdateText();
		if (value != _value)
		{
			ValueChanged?.Invoke();
		}
		base.OnBlur();
	}

	private void UpdateText()
	{
		string text = _text;
		string text2 = Format.Suffix ?? "";
		if (Format.MaxDecimalPlaces > 0)
		{
			_text = ((_value == Format.DefaultValue) ? "" : (_value.ToString("0." + new string('#', Format.MaxDecimalPlaces), CultureInfo.InvariantCulture) + text2));
			_placeholderText = Format.DefaultValue.ToString("0." + new string('#', Format.MaxDecimalPlaces), CultureInfo.InvariantCulture) + text2;
		}
		else
		{
			_text = ((_value == Format.DefaultValue) ? "" : (_value.ToString("0", CultureInfo.InvariantCulture) + text2));
			_placeholderText = Format.DefaultValue.ToString("0", CultureInfo.InvariantCulture) + text2;
		}
		if (_text != text)
		{
			base.CursorIndex = _text.Length;
		}
	}

	protected internal override void Validate()
	{
		decimal value = _value;
		ClampValue(ref _value);
		HasValidValue = true;
		UpdateText();
		if (value != _value)
		{
			ValueChanged?.Invoke();
		}
		base.Validate();
	}
}
