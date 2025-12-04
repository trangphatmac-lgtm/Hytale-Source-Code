using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class CheckBox : BaseCheckBox<CheckBox.CheckBoxStyle, CheckBoxStyleState>
{
	[UIMarkupData]
	public class CheckBoxStyle : BaseCheckBoxStyle<CheckBoxStyleState>
	{
	}

	public CheckBox(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}
}
