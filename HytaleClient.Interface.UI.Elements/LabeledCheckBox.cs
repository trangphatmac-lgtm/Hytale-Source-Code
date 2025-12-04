using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class LabeledCheckBox : BaseCheckBox<LabeledCheckBox.LabeledCheckBoxStyle, LabeledCheckBoxStyleState>
{
	[UIMarkupData]
	public class LabeledCheckBoxStyle : BaseCheckBoxStyle<LabeledCheckBoxStyleState>
	{
	}

	private readonly Label _label;

	public LabeledCheckBox(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_label = new Label(Desktop, this);
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		LabeledCheckBoxStyleState labeledCheckBoxStyleState = (Value ? Style.Checked : Style.Unchecked);
		if (Disabled)
		{
			_label.Style = labeledCheckBoxStyleState.DisabledLabelStyle ?? labeledCheckBoxStyleState.DefaultLabelStyle ?? new LabelStyle();
		}
		else if (base.CapturedMouseButton == 1u)
		{
			_label.Style = labeledCheckBoxStyleState.PressedLabelStyle ?? labeledCheckBoxStyleState.HoveredLabelStyle ?? labeledCheckBoxStyleState.DefaultLabelStyle ?? new LabelStyle();
		}
		else if (base.IsHovered)
		{
			_label.Style = labeledCheckBoxStyleState.HoveredLabelStyle ?? labeledCheckBoxStyleState.DefaultLabelStyle ?? new LabelStyle();
		}
		else
		{
			_label.Style = labeledCheckBoxStyleState.DefaultLabelStyle ?? new LabelStyle();
		}
		_label.Text = labeledCheckBoxStyleState.Text ?? "";
	}
}
