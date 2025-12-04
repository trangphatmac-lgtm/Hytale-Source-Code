namespace HytaleClient.Utils;

public struct ColorRgb
{
	public static readonly ColorRgb Zero = new ColorRgb(0, 0, 0);

	public byte R;

	public byte G;

	public byte B;

	public ColorRgb(byte r, byte g, byte b)
	{
		R = r;
		G = g;
		B = b;
	}
}
