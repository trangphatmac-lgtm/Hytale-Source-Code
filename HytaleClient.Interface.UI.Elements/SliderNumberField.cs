using System;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class SliderNumberField : InputElement<int>
{
	private Slider _slider;

	private NumberField _numberField;

	[UIMarkupProperty]
	public SliderStyle SliderStyle = SliderStyle.MakeDefault();

	[UIMarkupProperty]
	public InputFieldStyle NumberFieldStyle = new InputFieldStyle
	{
		RenderBold = true
	};

	private Group _numberFieldContainer;

	[UIMarkupProperty]
	public Anchor NumberFieldContainerAnchor = new Anchor
	{
		Left = 15,
		Width = 40,
		Height = 15
	};

	[UIMarkupProperty]
	public int Min
	{
		get
		{
			return _slider.Min;
		}
		set
		{
			_slider.Min = value;
			_numberField.Format.MinValue = value;
		}
	}

	[UIMarkupProperty]
	public int Max
	{
		get
		{
			return _slider.Max;
		}
		set
		{
			_slider.Max = value;
			_numberField.Format.MaxValue = value;
		}
	}

	[UIMarkupProperty]
	public int Step
	{
		get
		{
			return _slider.Step;
		}
		set
		{
			_slider.Step = value;
			_numberField.Format.Step = value;
		}
	}

	public override int Value
	{
		get
		{
			return _slider.Value;
		}
		set
		{
			_slider.Value = value;
			_numberField.Value = value;
		}
	}

	[UIMarkupProperty]
	public int NumberFieldMaxDecimalPlaces
	{
		get
		{
			return _numberField.Format.MaxDecimalPlaces;
		}
		set
		{
			_numberField.Format.MaxDecimalPlaces = value;
		}
	}

	[UIMarkupProperty]
	public decimal NumberFieldDefaultValue
	{
		get
		{
			return _numberField.Format.DefaultValue;
		}
		set
		{
			_numberField.Format.DefaultValue = value;
		}
	}

	[UIMarkupProperty]
	public string NumberFieldSuffix
	{
		get
		{
			return _numberField.Format.Suffix;
		}
		set
		{
			_numberField.Format.Suffix = value;
		}
	}

	public Action SliderMouseButtonReleased
	{
		set
		{
			_slider.MouseButtonReleased = value;
		}
	}

	public Action NumberFieldValidating
	{
		set
		{
			_numberField.Validating = value;
		}
	}

	public Action NumberFieldDismissing
	{
		set
		{
			_numberField.Dismissing = value;
		}
	}

	public Action NumberFieldBlurred
	{
		set
		{
			_numberField.Blurred = value;
		}
	}

	public Action NumberFieldFocused
	{
		set
		{
			_numberField.Focused = value;
		}
	}

	public SliderNumberField(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		Group parent2 = new Group(desktop, this)
		{
			LayoutMode = LayoutMode.Left
		};
		Group parent3 = new Group(desktop, parent2)
		{
			FlexWeight = 1
		};
		_slider = new Slider(desktop, parent3);
		_numberFieldContainer = new Group(desktop, parent2);
		_numberField = new NumberField(desktop, _numberFieldContainer);
		_slider.ValueChanged = delegate
		{
			_numberField.Value = _slider.Value;
			ValueChanged?.Invoke();
		};
		_numberField.ValueChanged = delegate
		{
			if (_numberField.HasValidValue)
			{
				int value = (int)_numberField.Value;
				_slider.Value = value;
				_slider.Layout();
				ValueChanged?.Invoke();
			}
		};
	}

	protected override void ApplyStyles()
	{
		_slider.Style = SliderStyle;
		_numberFieldContainer.Anchor = NumberFieldContainerAnchor;
		_numberField.Style = NumberFieldStyle;
	}

	public override Element HitTest(Point position)
	{
		return _slider.HitTest(position) ?? _numberField.HitTest(position);
	}
}
