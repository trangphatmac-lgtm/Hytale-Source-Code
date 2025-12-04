using System;
using HytaleClient.Math;

namespace HytaleClient.Utils;

public struct ColorHsla
{
	public float H;

	public float S;

	public float L;

	public float A;

	public ColorHsla(float h, float s, float l, float a = 1f)
	{
		H = h;
		S = s;
		L = l;
		A = a;
	}

	public static ColorHsla FromRgba(byte r, byte g, byte b, byte a = byte.MaxValue)
	{
		return FromRgba((float)(int)r / 255f, (float)(int)g / 255f, (float)(int)b / 255f, (float)(int)a / 255f);
	}

	public static ColorHsla FromRgba(ColorRgba rgba)
	{
		return FromRgba(rgba.R, rgba.G, rgba.B, rgba.A);
	}

	public static ColorHsla FromRgba(float r, float g, float b, float a)
	{
		float num = MathHelper.Min(MathHelper.Min(r, g), b);
		float num2 = MathHelper.Max(MathHelper.Max(r, g), b);
		float num3 = num2 - num;
		float num4 = (num2 + num) / 2f;
		float num5 = (num2 + num) / 2f;
		float num6 = (num2 + num) / 2f;
		if (num2 == num)
		{
			num5 = 0f;
			num4 = 0f;
		}
		else
		{
			num5 = ((num6 > 0.5f) ? (num3 / (2f - num2 - num)) : (num3 / (num2 + num)));
			if (num2 == r)
			{
				num4 = (g - b) / num3 + (float)((g < b) ? 6 : 0);
			}
			else if (num2 == g)
			{
				num4 = (b - r) / num3 + 2f;
			}
			else if (num2 == b)
			{
				num4 = (r - g) / num3 + 4f;
			}
			num4 /= 6f;
		}
		return new ColorHsla(num4, num5, num6, a);
	}

	private float HueToRgbComponent(float p, float q, float t)
	{
		if (t < 0f)
		{
			t += 1f;
		}
		if (t > 1f)
		{
			t -= 1f;
		}
		if (t < 1f / 6f)
		{
			return p + (q - p) * 6f * t;
		}
		if (t < 0.5f)
		{
			return q;
		}
		if (t < 2f / 3f)
		{
			return p + (q - p) * (2f / 3f - t) * 6f;
		}
		return p;
	}

	public void ToRgba(out byte r, out byte g, out byte b, out byte a)
	{
		ToRgb(out r, out g, out b);
		a = (byte)System.Math.Round(A * 255f);
	}

	public void ToRgb(out byte r, out byte g, out byte b)
	{
		ToRgb(out float r2, out float g2, out float b2);
		r = (byte)(int)System.Math.Round(r2 * 255f);
		g = (byte)(int)System.Math.Round(g2 * 255f);
		b = (byte)(int)System.Math.Round(b2 * 255f);
	}

	public void ToRgba(out float r, out float g, out float b, out float a)
	{
		ToRgb(out r, out g, out b);
		a = A;
	}

	public void ToRgb(out float r, out float g, out float b)
	{
		if (S == 0f)
		{
			r = L;
			g = L;
			b = L;
		}
		else
		{
			float num = ((L < 0.5f) ? (L * (1f + S)) : (L + S - L * S));
			float p = 2f * L - num;
			r = HueToRgbComponent(p, num, H + 1f / 3f);
			g = HueToRgbComponent(p, num, H);
			b = HueToRgbComponent(p, num, H - 1f / 3f);
		}
	}

	public void Saturate(float amount)
	{
		S = MathHelper.Clamp(S + amount, 0f, 1f);
	}

	public void Desaturate(float amount)
	{
		S = MathHelper.Clamp(S - amount, 0f, 1f);
	}

	public void Lighten(float amount)
	{
		L = MathHelper.Clamp(L + amount, 0f, 1f);
	}

	public void Darken(float amount)
	{
		L = MathHelper.Clamp(L - amount, 0f, 1f);
	}
}
