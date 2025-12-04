using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class InputFieldStyle
{
	public UIFontName FontName = new UIFontName("Default");

	public float FontSize = 16f;

	public UInt32Color TextColor = UInt32Color.White;

	public bool RenderUppercase;

	public bool RenderBold;

	public bool RenderItalics;
}
