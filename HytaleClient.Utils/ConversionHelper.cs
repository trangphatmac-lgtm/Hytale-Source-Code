#define DEBUG
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Utils;

public static class ConversionHelper
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Point RangeToPoint(Range range)
	{
		return (range.Min > range.Max) ? new Point(range.Max, range.Min) : new Point(range.Min, range.Max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static UShortVector2 RangeToUShortVector2(Range range)
	{
		Debug.Assert(range.Min >= 0 && range.Min <= 65535 && range.Max >= 0 && range.Max <= 65535);
		return (range.Min > range.Max) ? new UShortVector2((ushort)range.Max, (ushort)range.Min) : new UShortVector2((ushort)range.Min, (ushort)range.Max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ByteVector2 RangeToByteVector2(Range range)
	{
		Debug.Assert(range.Min >= 0 && range.Min <= 255 && range.Max >= 0 && range.Max <= 255);
		return (range.Min > range.Max) ? new ByteVector2((byte)range.Max, (byte)range.Min) : new ByteVector2((byte)range.Min, (byte)range.Max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 RangeToVector2(Rangef range)
	{
		return (range.Min > range.Max) ? new Vector2(range.Max, range.Min) : new Vector2(range.Min, range.Max);
	}
}
