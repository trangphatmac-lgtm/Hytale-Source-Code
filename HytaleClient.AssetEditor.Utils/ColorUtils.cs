using System;
using HytaleClient.Graphics;
using HytaleClient.Math;

namespace HytaleClient.AssetEditor.Utils;

public static class ColorUtils
{
	public enum ColorFormatType
	{
		Hex,
		Rgb,
		HexAlpha,
		Rgba,
		RgbaHex
	}

	public const string DefaultRgbaColor = "rgba(#ffffff, 1)";

	public const string DefaultRgbColor = "#ffffff";

	public const string DefaultRgbShortColor = "#fffff";

	public static bool TryParseColor(string text, out UInt32Color color, out ColorFormatType formatType)
	{
		color = UInt32Color.White;
		formatType = ColorFormatType.Hex;
		if (text.StartsWith("#") && text.Length == 7)
		{
			try
			{
				color = UInt32Color.FromHexString(text);
			}
			catch (Exception)
			{
				return false;
			}
			formatType = ColorFormatType.Hex;
			return true;
		}
		if (text.StartsWith("rgb("))
		{
			text = text.Substring("rgb(".Length).TrimEnd(new char[1] { ')' });
			string[] array = text.Split(new char[1] { ',' });
			if (array.Length != 3)
			{
				return false;
			}
			if (!uint.TryParse(array[0].Trim(), out var result) || !uint.TryParse(array[1].Trim(), out var result2) || !uint.TryParse(array[2].Trim(), out var result3))
			{
				return false;
			}
			color = UInt32Color.FromRGBA((byte)result, (byte)result2, (byte)result3, byte.MaxValue);
			formatType = ColorFormatType.Rgb;
			return true;
		}
		return false;
	}

	public static bool TryParseColorAlpha(string text, out UInt32Color color, out ColorFormatType formatType)
	{
		color = UInt32Color.White;
		formatType = ColorFormatType.Hex;
		if (text.StartsWith("#") && (text.Length == 9 || text.Length == 7))
		{
			try
			{
				formatType = ColorFormatType.HexAlpha;
				color = UInt32Color.FromHexString(text);
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}
		if (text.StartsWith("rgba("))
		{
			text = text.Substring("rgba(".Length).TrimEnd(new char[1] { ')' });
			string[] array = text.Split(new char[1] { ',' });
			if (text.StartsWith("#"))
			{
				if (array.Length != 2)
				{
					return false;
				}
				string text2 = array[0].Trim();
				int value = (int)(float.Parse(array[1].Trim()) * 255f);
				value = MathHelper.Clamp(value, 0, 255);
				try
				{
					color = UInt32Color.FromHexString(text2);
				}
				catch (Exception)
				{
					return false;
				}
				color.SetA((byte)value);
				formatType = ColorFormatType.RgbaHex;
				return true;
			}
			if (array.Length != 4)
			{
				return false;
			}
			if (!uint.TryParse(array[0].Trim(), out var result) || !uint.TryParse(array[1].Trim(), out var result2) || !uint.TryParse(array[2].Trim(), out var result3) || !uint.TryParse(array[3].Trim(), out var result4))
			{
				return false;
			}
			color = UInt32Color.FromRGBA((byte)result, (byte)result2, (byte)result3, (byte)result4);
			formatType = ColorFormatType.Rgba;
			return true;
		}
		return false;
	}

	public static string FormatColor(UInt32Color color, ColorFormatType formatType)
	{
		return formatType switch
		{
			ColorFormatType.Rgb => FormatRgbColor(color), 
			ColorFormatType.Rgba => FormatRgbaColor(color), 
			ColorFormatType.RgbaHex => FormatRgbaHexColor(color), 
			ColorFormatType.Hex => color.ToHexString(includeAlphaChannel: false), 
			ColorFormatType.HexAlpha => color.ToHexString(), 
			_ => null, 
		};
	}

	public static string FormatRgbaHexColor(UInt32Color color)
	{
		string text = "rgba(";
		text += color.ToHexString(includeAlphaChannel: false);
		text += ", ";
		text += ((float)(int)color.GetA() / 255f).ToString("0.###");
		return text + ")";
	}

	public static string FormatRgbColor(UInt32Color color)
	{
		string text = "rgb(";
		text += color.GetR();
		text += ", ";
		text += color.GetG();
		text += ", ";
		text += color.GetB();
		return text + ")";
	}

	public static string FormatRgbaColor(UInt32Color color)
	{
		string text = "rgb(";
		text += color.GetR();
		text += ", ";
		text += color.GetG();
		text += ", ";
		text += color.GetB();
		text += ", ";
		text += color.GetA();
		return text + ")";
	}
}
