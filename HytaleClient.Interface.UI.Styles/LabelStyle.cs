using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class LabelStyle
{
	public enum LabelAlignment
	{
		Start,
		Center,
		End
	}

	public LabelAlignment HorizontalAlignment = LabelAlignment.Start;

	public LabelAlignment VerticalAlignment = LabelAlignment.Start;

	public bool Wrap = false;

	public UIFontName FontName = new UIFontName("Default");

	public float FontSize = 16f;

	public UInt32Color TextColor = UInt32Color.White;

	public float LetterSpacing = 0f;

	public bool RenderUppercase;

	public bool RenderBold;

	public bool RenderItalics;

	public bool RenderUnderlined;

	public LabelAlignment Alignment
	{
		set
		{
			HorizontalAlignment = (VerticalAlignment = value);
		}
	}

	public LabelStyle Clone()
	{
		return new LabelStyle
		{
			HorizontalAlignment = HorizontalAlignment,
			VerticalAlignment = VerticalAlignment,
			Wrap = Wrap,
			FontName = FontName,
			FontSize = FontSize,
			TextColor = TextColor,
			LetterSpacing = LetterSpacing,
			RenderBold = RenderBold,
			RenderItalics = RenderItalics,
			RenderUnderlined = RenderUnderlined,
			RenderUppercase = RenderUppercase
		};
	}
}
