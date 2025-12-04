using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class ScrollbarStyle
{
	public int Size = 20;

	public int Spacing = 20;

	public bool OnlyVisibleWhenHovered;

	public PatchStyle Background;

	public PatchStyle Handle;

	public PatchStyle HoveredHandle;

	public PatchStyle DraggedHandle;

	public static ScrollbarStyle MakeDefault()
	{
		return new ScrollbarStyle
		{
			Background = new PatchStyle
			{
				Color = UInt32Color.FromRGBA(1145324748u)
			},
			Handle = new PatchStyle
			{
				Color = UInt32Color.FromRGBA(3435973887u)
			},
			HoveredHandle = new PatchStyle
			{
				Color = UInt32Color.FromRGBA(4008636159u)
			},
			DraggedHandle = new PatchStyle
			{
				Color = UInt32Color.FromRGBA(2863311615u)
			}
		};
	}
}
