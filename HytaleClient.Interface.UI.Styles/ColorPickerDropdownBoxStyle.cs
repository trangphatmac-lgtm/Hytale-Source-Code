using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class ColorPickerDropdownBoxStyle
{
	[UIMarkupData]
	public class ColorPickerDropdownBoxStateBackground
	{
		public PatchStyle Default;

		public PatchStyle Hovered;

		public PatchStyle Pressed;
	}

	public ColorPickerDropdownBoxStateBackground Background;

	public ColorPickerStyle ColorPickerStyle;

	public ColorPickerDropdownBoxStateBackground Overlay;

	public ColorPickerDropdownBoxStateBackground ArrowBackground;

	public Anchor ArrowAnchor;

	public ButtonSounds Sounds;

	public PatchStyle PanelBackground;

	public int PanelWidth = 300;

	public int PanelHeight = 300;

	public Padding PanelPadding;

	public int PanelOffset;
}
