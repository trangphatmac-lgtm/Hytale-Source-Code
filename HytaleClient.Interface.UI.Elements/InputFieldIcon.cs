using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupData]
public class InputFieldIcon
{
	public enum InputFieldIconSide
	{
		Left,
		Right
	}

	public PatchStyle Texture;

	public int Width;

	public int Height;

	public int Offset;

	public InputFieldIconSide Side;
}
