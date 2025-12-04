using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement(AcceptsChildren = true)]
public class Button : BaseButton<Button.ButtonStyle, Button.ButtonStyleState>
{
	[UIMarkupData]
	public class ButtonStyle : BaseButtonStyle<ButtonStyleState>
	{
	}

	[UIMarkupData]
	public class ButtonStyleState
	{
		public PatchStyle Background;
	}

	[UIMarkupProperty]
	public new LayoutMode LayoutMode
	{
		get
		{
			return _layoutMode;
		}
		set
		{
			_layoutMode = value;
		}
	}

	public Button(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_stateBackgroundPatch = ((_styleState.Background != null) ? Desktop.MakeTexturePatch(_styleState.Background) : null);
	}
}
