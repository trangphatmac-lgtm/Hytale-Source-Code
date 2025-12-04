using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.Settings;

internal class SliderSettingComponent : SettingComponent<int>
{
	private SliderNumberField _slider;

	public SliderSettingComponent(Desktop desktop, Group parent, string name, ISettingView settings, int min, int max, int step)
		: base(desktop, parent, name, settings)
	{
		Document doc;
		UIFragment uIFragment = Build("SliderSetting.ui", out doc);
		_slider = uIFragment.Get<SliderNumberField>("Slider");
		_slider.Min = min;
		_slider.Max = max;
		_slider.Step = step;
		_slider.ValueChanged = delegate
		{
			OnChange(_slider.Value);
		};
	}

	public override void SetValue(int value)
	{
		_slider.Value = value;
		if (base.IsMounted)
		{
			_slider.Layout();
		}
	}
}
