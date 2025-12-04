using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
internal class PopupMenuLayerStyle
{
	public PatchStyle Background;

	public int Padding = 2;

	public int BaseHeight = 31;

	public int MaxWidth = 200;

	public int RowHeight = 25;

	public LabelStyle TitleStyle;

	public PatchStyle TitleBackground;

	public LabelStyle ItemLabelStyle;

	public Padding ItemPadding;

	public PatchStyle ItemBackground;

	public int ItemIconSize = 16;

	public PatchStyle HoveredItemBackground;

	public PatchStyle PressedItemBackground;

	public ButtonSounds ItemSounds;
}
