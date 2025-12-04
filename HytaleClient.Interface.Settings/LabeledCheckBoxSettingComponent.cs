using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.Settings;

internal class LabeledCheckBoxSettingComponent : SettingComponent<bool>
{
	private LabeledCheckBox _checkBox;

	public LabeledCheckBoxSettingComponent(Desktop desktop, Group parent, string name, ISettingView settings)
		: base(desktop, parent, name, settings)
	{
		Document doc;
		UIFragment uIFragment = Build("LabeledCheckBoxSetting.ui", out doc);
		_checkBox = uIFragment.Get<LabeledCheckBox>("CheckBox");
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
