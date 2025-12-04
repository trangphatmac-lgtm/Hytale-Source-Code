using HytaleClient.Graphics;
using HytaleClient.Math;

namespace HytaleClient.Utils;

public struct ColorRgba
{
	public byte R;

	public byte G;

	public byte B;

	public byte A;

	public ColorRgba(byte r, byte g, byte b, byte a = byte.MaxValue)
	{
		R = r;
		G = g;
		B = b;
		A = a;
	}

	public ColorRgba(uint color)
	{
		R = (byte)(color & 0xFFu);
		G = (byte)((color >> 8) & 0xFFu);
		B = (byte)((color >> 16) & 0xFFu);
		A = (byte)((color >> 24) & 0xFFu);
	}

	public void Darken(float percent)
	{
		R = (byte)MathHelper.Clamp((int)((float)(int)R * (100f + percent) / 100f), 0, 255);
		G = (byte)MathHelper.Clamp((int)((float)(int)G * (100f + percent) / 100f), 0, 255);
		B = (byte)MathHelper.Clamp((int)((float)(int)B * (100f + percent) / 100f), 0, 255);
	}

	public void Lighten(float percent)
	{
		Darken(0f - percent);
	}

	public static ColorRgba FromUInt32Color(UInt32Color color)
	{
		uint num = (color.ABGR >> 24) & 0xFFu;
		uint num2 = (color.ABGR >> 16) & 0xFFu;
		uint num3 = (color.ABGR >> 8) & 0xFFu;
		uint num4 = color.ABGR & 0xFFu;
		return new ColorRgba((byte)num4, (byte)num3, (byte)num2, (byte)num);
	}
}
