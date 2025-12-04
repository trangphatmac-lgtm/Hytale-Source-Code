using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.Settings;

internal class FloatSliderSettingComponent : SettingComponent<float>
{
	private FloatSliderNumberField _slider;

	public FloatSliderSettingComponent(Desktop desktop, Group parent, string name, ISettingView settings, float min, float max, float step)
		: base(desktop, parent, name, settings)
	{
		Document doc;
		UIFragment uIFragment = Build("FloatSliderSetting.ui", out doc);
		_slider = uIFragment.Get<FloatSliderNumberField>("Slider");
		_slider.Min = min;
		_slider.Max = max;
		_slider.Step = step;
		_slider.ValueChanged = delegate
		{
			OnChange(_slider.Value);
		};
		_slider.NumberFieldMaxDecimalPlaces = 2;
	}

	public override void SetValue(float value)
	{
		_slider.Value = value;
		if (base.IsMounted)
		{
			_slider.Layout();
		}
	}
}
