namespace HytaleClient.Graphics.Fonts;

public class FontFamily
{
	public readonly Font RegularFont;

	public readonly Font BoldFont;

	public FontFamily(Font regularFont, Font boldFont)
	{
		RegularFont = regularFont;
		BoldFont = boldFont;
	}
}
