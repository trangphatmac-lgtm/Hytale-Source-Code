using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.Settings;

internal class InputBindingSettingComponent : SettingComponent<string>
{
	private TextButton _button;

	public InputBindingSettingComponent(Desktop desktop, Group parent, string name, ISettingView settings)
		: base(desktop, parent, name, settings)
	{
		_name = "ui.settings.bindings." + name;
		Document doc;
		UIFragment uIFragment = Build("InputBindingSetting.ui", out doc);
		_button = uIFragment.Get<TextButton>("Button");
		_button.Activating = delegate
		{
			OnChange(null);
		};
	}

	public override void SetValue(string value)
	{
		_button.Text = value ?? "";
	}
}
