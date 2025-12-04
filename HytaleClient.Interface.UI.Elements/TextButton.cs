using System.Collections.Generic;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class TextButton : BaseButton<TextButton.TextButtonStyle, TextButton.TextButtonStyleState>
{
	[UIMarkupData]
	public class TextButtonStyle : BaseButtonStyle<TextButtonStyleState>
	{
	}

	[UIMarkupData]
	public class TextButtonStyleState
	{
		public PatchStyle Background;

		public LabelStyle LabelStyle = new LabelStyle();

		public UIPath LabelMaskTexturePath;
	}

	private readonly Label _label;

	[UIMarkupProperty]
	public string Text
	{
		set
		{
			_label.Text = value;
		}
	}

	[UIMarkupProperty]
	public IList<Label.LabelSpan> TextSpans
	{
		set
		{
			_label.TextSpans = value;
		}
	}

	public TextButton(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_label = new Label(Desktop, this);
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_stateBackgroundPatch = ((_styleState.Background != null) ? Desktop.MakeTexturePatch(_styleState.Background) : null);
		_label.Style = _styleState.LabelStyle;
		_label.MaskTexturePath = _styleState.LabelMaskTexturePath;
	}
}
