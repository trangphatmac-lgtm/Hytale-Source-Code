using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class InputFieldButtonStyle
{
	public enum InputFieldButtonSide
	{
		Left,
		Right
	}

	public PatchStyle Texture;

	public PatchStyle HoveredTexture;

	public PatchStyle PressedTexture;

	public int Width;

	public int Height;

	public int Offset;

	public InputFieldButtonSide Side = InputFieldButtonSide.Right;
}
