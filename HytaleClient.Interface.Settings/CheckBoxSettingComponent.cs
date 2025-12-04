using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.Settings;

internal class CheckBoxSettingComponent : SettingComponent<bool>
{
	private CheckBox _checkBox;

	public CheckBoxSettingComponent(Desktop desktop, Group parent, string name, ISettingView settings)
		: base(desktop, parent, name, settings)
	{
		Document doc;
		UIFragment uIFragment = Build("CheckBoxSetting.ui", out doc);
		_checkBox = uIFragment.Get<CheckBox>("CheckBox");
		_checkBox.ValueChanged = delegate
		{
			OnChange(_checkBox.Value);
		};
	}

	public override void SetValue(bool value)
	{
		_checkBox.Value = value;
		if (_checkBox.IsMounted)
		{
			_checkBox.Layout();
		}
	}
}
