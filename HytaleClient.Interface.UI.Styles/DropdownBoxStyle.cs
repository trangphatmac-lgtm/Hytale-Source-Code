using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class DropdownBoxStyle
{
	[UIMarkupData]
	public class DropdownBoxSearchInputStyle
	{
		public PatchStyle Background;

		public InputFieldIcon Icon;

		public InputFieldStyle Style = new InputFieldStyle();

		public InputFieldStyle PlaceholderStyle = new InputFieldStyle();

		public Anchor Anchor;

		public Padding Padding;

		public string PlaceholderText;

		public InputFieldButtonStyle ClearButtonStyle;
	}

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

	public PatchStyle DisabledBackground;

	public UIPath IconTexturePath;

	public int IconWidth;

	public int IconHeight;

	public UIPath DefaultArrowTexturePath;

	public UIPath HoveredArrowTexturePath;

	public UIPath PressedArrowTexturePath;

	public UIPath DisabledArrowTexturePath;

	public int ArrowWidth;

	public int ArrowHeight;

	public int HorizontalPadding = 8;

	public LabelStyle LabelStyle;

	public LabelStyle DisabledLabelStyle;

	public DropdownBoxSounds Sounds;

	public LabelStyle PanelTitleLabelStyle;

	public int EntryHeight = 40;

	public int EntriesInViewport = 3;

	public int HorizontalEntryPadding;

	public int EntryIconHeight;

	public int EntryIconWidth;

	public PatchStyle EntryIconBackground;

	public PatchStyle SelectedEntryIconBackground;

	public LabelStyle EntryLabelStyle;

	public LabelStyle SelectedEntryLabelStyle;

	public LabelStyle NoItemsLabelStyle;

	public PatchStyle HoveredEntryBackground;

	public PatchStyle PressedEntryBackground;

	public int FocusOutlineSize;

	public UInt32Color FocusOutlineColor;

	public ButtonSounds EntrySounds;

	public ScrollbarStyle PanelScrollbarStyle;

	public PatchStyle PanelBackground;

	public int PanelPadding;

	public DropdownBoxAlign PanelAlign = DropdownBoxAlign.Bottom;

	public int PanelOffset = 5;

	public int? PanelWidth;

	public DropdownBoxSearchInputStyle SearchInputStyle;
}
