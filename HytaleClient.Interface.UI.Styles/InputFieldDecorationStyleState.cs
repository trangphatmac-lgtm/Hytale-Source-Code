using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class InputFieldDecorationStyleState
{
	public PatchStyle Background;

	public int? OutlineSize;

	public UInt32Color? OutlineColor;

	public InputFieldIcon Icon;

	public InputFieldButtonStyle ClearButtonStyle;
}
