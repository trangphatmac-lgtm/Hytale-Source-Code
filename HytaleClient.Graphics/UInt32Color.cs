using System;
using System.Runtime.CompilerServices;

namespace HytaleClient.Graphics;

public struct UInt32Color
{
	public static readonly UInt32Color White = new UInt32Color
	{
		ABGR = uint.MaxValue
	};

	public static readonly UInt32Color Black = new UInt32Color
	{
		ABGR = 4278190080u
	};

	public static readonly UInt32Color Transparent = new UInt32Color
	{
		ABGR = 0u
	};

	public uint ABGR { get; private set; }

	public bool IsTransparent => ABGR == 0;

	public static UInt32Color FromRGBA(byte r, byte g, byte b, byte a)
	{
		UInt32Color result = default(UInt32Color);
		result.ABGR = (uint)(r | (g << 8) | (b << 16) | (a << 24));
		return result;
	}

	public static UInt32Color FromRGBA(uint rgba)
	{
		uint num = (rgba >> 24) & 0xFFu;
		uint num2 = (rgba >> 16) & 0xFFu;
		uint num3 = (rgba >> 8) & 0xFFu;
		uint num4 = rgba & 0xFFu;
		UInt32Color result = default(UInt32Color);
		result.ABGR = num | (num2 << 8) | (num3 << 16) | (num4 << 24);
		return result;
	}

	public string ToHexString(bool includeAlphaChannel = true)
	{
		string text = "#";
		text += ((byte)(ABGR & 0xFFu)).ToString("x2");
		text += ((byte)(ABGR >> 8) & 0xFF).ToString("x2");
		text += ((byte)(ABGR >> 16) & 0xFF).ToString("x2");
		if (includeAlphaChannel)
		{
			text += ((byte)(ABGR >> 24) & 0xFF).ToString("x2");
		}
		return text;
	}

	public string ToShortHexString()
	{
		string text = "#";
		text += ((byte)((float)(ABGR & 0xFFu) / 255f * 15f)).ToString("x1");
		text += ((byte)((float)((ABGR >> 8) & 0xFFu) / 255f * 15f)).ToString("x1");
		return text + ((byte)((float)((ABGR >> 16) & 0xFFu) / 255f * 15f)).ToString("x1");
	}

	public static UInt32Color FromShortHexString(string text)
	{
		byte b = Convert.ToByte(text.Substring(1, 1), 16);
		byte b2 = Convert.ToByte(text.Substring(2, 1), 16);
		byte b3 = Convert.ToByte(text.Substring(3, 1), 16);
		byte b4 = ((text.Length < 5) ? byte.MaxValue : Convert.ToByte(text.Substring(4, 1), 16));
		return FromRGBA((byte)((b << 4) | b), (byte)((b2 << 4) | b2), (byte)((b3 << 4) | b3), (byte)((b4 << 4) | b4));
	}

	public static UInt32Color FromHexString(string text)
	{
		byte r = Convert.ToByte(text.Substring(1, 2), 16);
		byte g = Convert.ToByte(text.Substring(3, 2), 16);
		byte b = Convert.ToByte(text.Substring(5, 2), 16);
		byte a = ((text.Length == 7) ? byte.MaxValue : Convert.ToByte(text.Substring(7, 2), 16));
		return FromRGBA(r, g, b, a);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetA(byte a)
	{
		ABGR = (uint)(a << 24) | (ABGR & 0xFFFFFFu);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetB(byte b)
	{
		ABGR = (uint)(b << 16) | (ABGR & 0xFF00FFFFu);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetG(byte g)
	{
		ABGR = (uint)(g << 8) | (ABGR & 0xFFFF00FFu);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetR(byte r)
	{
		ABGR = r | (ABGR & 0xFFFFFF00u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte GetA()
	{
		return (byte)((ABGR >> 24) & 0xFFu);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte GetB()
	{
		return (byte)((ABGR >> 16) & 0xFFu);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte GetG()
	{
		return (byte)((ABGR >> 8) & 0xFFu);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte GetR()
	{
		return (byte)(ABGR & 0xFFu);
	}
}
