using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class ColorPickerStyle
{
	public PatchStyle ButtonBackground;

	public PatchStyle ButtonFill;

	public PatchStyle OpacitySelectorBackground;

	public InputFieldDecorationStyle TextFieldDecoration;

	public InputFieldStyle TextFieldInputStyle = new InputFieldStyle();

	public Padding TextFieldPadding;

	public int TextFieldHeight = 28;
}
