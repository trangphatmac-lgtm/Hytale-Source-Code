using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
internal class FileDropdownBoxStyle
{
	public enum DropdownBoxAlign
	{
		Top,
		Bottom,
		Left,
		Right
	}

	public PatchStyle DefaultBackground;

	public PatchStyle HoveredBackground;

	public PatchStyle PressedBackground;

	public UIPath DefaultArrowTexturePath;

	public UIPath HoveredArrowTexturePath;

	public UIPath PressedArrowTexturePath;

	public int ArrowWidth;

	public int ArrowHeight;

	public LabelStyle LabelStyle;

	public int HorizontalPadding = 8;

	public int HorizontalRowPadding;

	public DropdownBoxAlign PanelAlign = DropdownBoxAlign.Bottom;

	public int PanelOffset = 5;
}
