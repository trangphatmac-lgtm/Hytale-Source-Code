#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HytaleClient.Math;

namespace HytaleClient.Graphics;

public static class VertexCompression
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint NormalizedXYZToUint(float x, float y, float z)
	{
		uint num = (byte)System.Math.Round((x * 0.5f + 0.5f) * 255f) & 0xFFu;
		uint num2 = (byte)System.Math.Round((y * 0.5f + 0.5f) * 255f) & 0xFFu;
		uint num3 = (byte)System.Math.Round((z * 0.5f + 0.5f) * 255f) & 0xFFu;
		return num | (num2 << 8) | (num3 << 16);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ushort NormalizedTexCoordToUshort(float u)
	{
		return (ushort)((ushort)System.Math.Round(u * 65535f) & 0xFFFFu);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short FloatToSnorm16(float v)
	{
		return (short)MathHelper.Clamp((v >= 0f) ? (v * 32767f + 0.5f) : (v * 32767f - 0.5f), -32768f, 32767f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Snorm16ToFloat(short v)
	{
		return System.Math.Max((float)v / 32767f, -1f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ShortVector2 Vector2ToShortVector2(Vector2 value, float maxRange = 64f)
	{
		ShortVector2 result = default(ShortVector2);
		result.X = FloatToSnorm16(value.X / maxRange);
		result.Y = FloatToSnorm16(value.Y / maxRange);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ShortVector3 Vector3PositionToShortVector3(Vector3 position, float maxRange = 64f)
	{
		ShortVector3 result = default(ShortVector3);
		result.X = FloatToSnorm16(position.X / maxRange);
		result.Y = FloatToSnorm16(position.Y / maxRange);
		result.Z = FloatToSnorm16(position.Z / maxRange);
		return result;
	}

	public static ushort CompressBlockLocalPosition(int x, int y, int z)
	{
		Debug.Assert(x < 32 && x >= 0 && y < 32 && y >= 0 && z < 32 && z >= 0);
		return (ushort)(x | (y << 5) | (z << 10));
	}
}
