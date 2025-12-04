using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class SliderStyle
{
	public PatchStyle Background;

	public PatchStyle Fill;

	public PatchStyle Handle;

	public int HandleWidth;

	public int HandleHeight;

	public ButtonSounds Sounds;

	public static SliderStyle MakeDefault()
	{
		return new SliderStyle
		{
			Background = new PatchStyle(UInt32Color.FromRGBA(1145324748u)),
			Fill = new PatchStyle(UInt32Color.FromRGBA(1717987020u)),
			Handle = new PatchStyle(UInt32Color.FromRGBA(3435973887u)),
			HandleHeight = 16,
			HandleWidth = 16
		};
	}
}
