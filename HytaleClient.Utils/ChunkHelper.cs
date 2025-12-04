using System;
using System.Runtime.CompilerServices;
using HytaleClient.Math;

namespace HytaleClient.Utils;

internal static class ChunkHelper
{
	public const int Bits = 5;

	public const int Bits2 = 10;

	public const int Size = 32;

	public const int BorderedSize = 34;

	public const int SizeMask = 31;

	public const int BlocksCount = 32768;

	public const int BorderedBlocksCount = 39304;

	public static int Height { get; private set; }

	public static int ChunksPerColumn { get; private set; }

	public static void SetHeight(int height)
	{
		Height = height;
		ChunksPerColumn = Height >> 5;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ChunkCoordinate(int block)
	{
		return block >> 5;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexSection(int block)
	{
		return block >> 5;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long IndexOfChunkColumn(int x, int z)
	{
		return ((long)x << 32) | (z & 0xFFFFFFFFu);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int XOfChunkColumnIndex(long index)
	{
		return (int)(index >> 32);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ZOfChunkColumnIndex(long index)
	{
		return (int)index;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOfWorldBlockInChunk(int x, int y, int z)
	{
		return ((y & 0x1F) << 10) | ((z & 0x1F) << 5) | (x & 0x1F);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOfBlockInChunk(int x, int y, int z)
	{
		return (y * 32 + z) * 32 + x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOfBlockInBorderedChunk(int x, int y, int z)
	{
		return (y * 34 + z) * 34 + x;
	}

	public static int IndexOfBlockInBorderedChunk(int indexInChunk, int chunkOffsetX, int chunkOffsetY, int chunkOffsetZ)
	{
		int num = chunkOffsetX * 32 + indexInChunk % 32;
		if (num < -1 || num >= 33)
		{
			return -1;
		}
		int num2 = chunkOffsetY * 32 + indexInChunk / 32 / 32;
		if (num2 < -1 || num2 >= 33)
		{
			return -1;
		}
		int num3 = chunkOffsetZ * 32 + indexInChunk / 32 % 32;
		if (num3 < -1 || num3 >= 33)
		{
			return -1;
		}
		return IndexOfBlockInBorderedChunk(num + 1, num2 + 1, num3 + 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexInChunkColumn(int x, int z)
	{
		return z * 32 + x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexInBorderedChunkColumn(int x, int z)
	{
		return z * 34 + x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 WorldToChunk(Vector3 worldPos)
	{
		int num = (int)System.Math.Floor(worldPos.X);
		int num2 = (int)System.Math.Floor(worldPos.Y);
		int num3 = (int)System.Math.Floor(worldPos.Z);
		int num4 = num >> 5;
		int num5 = num2 >> 5;
		int num6 = num3 >> 5;
		int num7 = num - num4 * 32;
		int num8 = num2 - num5 * 32;
		int num9 = num3 - num6 * 32;
		return new Vector3(num7, num8, num9);
	}

	public static void GetEnvironmentId(ushort[] environmentsColumn, int worldY, ushort[] tracker, int trackerIndex)
	{
		int num = 1;
		ushort num2 = tracker[trackerIndex + 1];
		int num3 = 0;
		ushort num4 = num2;
		while (num4 < environmentsColumn.Length && worldY >= environmentsColumn[num4])
		{
			num = num4 + 1;
			num3 = ((num4 + 2 < environmentsColumn.Length) ? environmentsColumn[num4 + 2] : ushort.MaxValue);
			num2 = num4;
			num4 += 2;
		}
		if (environmentsColumn[num2] > worldY + 1)
		{
			tracker[trackerIndex] = (ushort)num3;
			tracker[trackerIndex + 1] = (ushort)(num2 + 2);
		}
		tracker[trackerIndex + 2] = environmentsColumn[num];
	}

	public static ushort GetEnvironmentId(ushort[][] environments, int chunkX, int chunkZ, int worldY)
	{
		ushort[] array = environments[(chunkZ << 5) + chunkX];
		int num = 1;
		for (int num2 = array.Length - 2; num2 >= 0; num2 -= 2)
		{
			if (array[num2] <= worldY)
			{
				num = num2 + 1;
				break;
			}
		}
		return array[num];
	}
}
