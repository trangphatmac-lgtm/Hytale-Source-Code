using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class LabeledCheckBoxStyleState : CheckBoxStyleState
{
	public LabelStyle DefaultLabelStyle;

	public LabelStyle HoveredLabelStyle;

	public LabelStyle PressedLabelStyle;

	public LabelStyle DisabledLabelStyle;

	public string Text;
}
