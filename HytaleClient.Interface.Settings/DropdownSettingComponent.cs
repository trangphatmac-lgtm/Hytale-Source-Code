using System.Collections.Generic;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.Settings;

internal class DropdownSettingComponent : SettingComponent<string>
{
	public DropdownBox Dropdown { get; }

	public DropdownSettingComponent(Desktop desktop, Group parent, string name, ISettingView settings, List<DropdownBox.DropdownEntryInfo> values)
		: base(desktop, parent, name, settings)
	{
		Document doc;
		UIFragment uIFragment = Build("DropdownSetting.ui", out doc);
		Dropdown = uIFragment.Get<DropdownBox>("Dropdown");
		Dropdown.Entries = values;
		Dropdown.ValueChanged = delegate
		{
			OnChange(Dropdown.Value);
		};
	}

	public override void SetValue(string value)
	{
		Dropdown.Value = value;
	}

	public void SetEntries(List<DropdownBox.DropdownEntryInfo> values)
	{
		Dropdown.Entries = values;
	}
}
