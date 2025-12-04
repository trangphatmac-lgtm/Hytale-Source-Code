using System;
using HytaleClient.Graphics;
using HytaleClient.Math;

namespace HytaleClient.Utils;

public struct ColorHsva
{
	public float H;

	public float S;

	public float V;

	public float A;

	public ColorHsva(float h, float s, float v, float a)
	{
		H = h;
		S = s;
		V = v;
		A = a;
	}

	public static ColorHsva FromRgba(byte r, byte g, byte b, byte a)
	{
		return FromRgba((float)(int)r / 255f, (float)(int)g / 255f, (float)(int)b / 255f, (float)(int)a / 255f);
	}

	public static ColorHsva FromRgba(ColorRgba rgba)
	{
		return FromRgba(rgba.R, rgba.G, rgba.B, rgba.A);
	}

	public static ColorHsva FromUInt32Color(UInt32Color color)
	{
		uint num = (color.ABGR >> 24) & 0xFFu;
		uint num2 = (color.ABGR >> 16) & 0xFFu;
		uint num3 = (color.ABGR >> 8) & 0xFFu;
		uint num4 = color.ABGR & 0xFFu;
		return FromRgba((byte)num4, (byte)num3, (byte)num2, (byte)num);
	}

	public static ColorHsva FromRgba(float r, float g, float b, float a)
	{
		float num = MathHelper.Min(MathHelper.Min(r, g), b);
		float num2 = MathHelper.Max(MathHelper.Max(r, g), b);
		float num3 = num2 - num;
		float v = num2;
		float s;
		float num4;
		if (System.Math.Abs(num3) < 0.001f)
		{
			s = 0f;
			num4 = -1f;
		}
		else
		{
			s = num3 / num2;
			num4 = ((r == num2) ? ((g - b) / num3) : ((g != num2) ? (4f + (r - g) / num3) : (2f + (b - r) / num3)));
			num4 /= 6f;
			if (num4 < 0f)
			{
				num4 += 1f;
			}
		}
		return new ColorHsva(num4, s, v, a);
	}

	public void ToRgba(out byte r, out byte g, out byte b, out byte a)
	{
		ToRgba(out float r2, out float g2, out float b2, out float a2);
		r = (byte)System.Math.Round(r2 * 255f);
		g = (byte)System.Math.Round(g2 * 255f);
		b = (byte)System.Math.Round(b2 * 255f);
		a = (byte)System.Math.Round(a2 * 255f);
	}

	public UInt32Color ToUInt32Color()
	{
		ToRgba(out byte r, out byte g, out byte b, out byte a);
		return UInt32Color.FromRGBA(r, g, b, a);
	}

	public void ToRgba(out float r, out float g, out float b, out float a)
	{
		a = A;
		if (S == 0f)
		{
			r = (g = (b = V));
			return;
		}
		float num = H % 1f * 6f;
		int num2 = (int)System.Math.Floor(num);
		float num3 = num - (float)num2;
		float num4 = V * (1f - S);
		float num5 = V * (1f - S * num3);
		float num6 = V * (1f - S * (1f - num3));
		switch (num2)
		{
		case 0:
			r = V;
			g = num6;
			b = num4;
			break;
		case 1:
			r = num5;
			g = V;
			b = num4;
			break;
		case 2:
			r = num4;
			g = V;
			b = num6;
			break;
		case 3:
			r = num4;
			g = num5;
			b = V;
			break;
		case 4:
			r = num6;
			g = num4;
			b = V;
			break;
		default:
			r = V;
			g = num4;
			b = num5;
			break;
		}
	}

	public static ColorHsva Lerp(ColorHsva color1, ColorHsva color2, float t)
	{
		float s = MathHelper.Lerp(color1.S, color2.S, t);
		float v = MathHelper.Lerp(color1.V, color2.V, t);
		float a = MathHelper.Lerp(color1.A, color2.A, t);
		if (color1.H == -1f || color1.S == 0f)
		{
			color1.H = color2.H;
		}
		else if (color2.H == -1f || color2.S == 0f)
		{
			color2.H = color1.H;
		}
		float num = color2.H - color1.H;
		if (color1.H > color2.H)
		{
			float h = color2.H;
			color2.H = color1.H;
			color1.H = h;
			num = 0f - num;
			t = 1f - t;
		}
		float h2;
		if ((double)num > 0.5)
		{
			color1.H += 1f;
			h2 = (color1.H + t * (color2.H - color1.H)) % 1f;
		}
		else
		{
			h2 = color1.H + t * num;
		}
		return new ColorHsva(h2, s, v, a);
	}
}
