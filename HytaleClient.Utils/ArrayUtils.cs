#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HytaleClient.Utils;

public static class ArrayUtils
{
	public delegate bool IsRemovableDuringSparseCompression<T>(ref T element) where T : struct;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GrowArrayIfNecessary<T>(ref T[] array, int itemCount, int growth)
	{
		if (itemCount >= array.Length)
		{
			Array.Resize(ref array, itemCount + growth);
		}
	}

	public static void RemoveAt<T>(ref T[] array, int index)
	{
		for (int i = index; i < array.Length - 1; i++)
		{
			array[i] = array[i + 1];
		}
		Array.Resize(ref array, array.Length - 1);
	}

	public static int CompactArray<T>(ref T[] array, int startIndex, int count, IsRemovableDuringSparseCompression<T> isRemovableDuringSparseCompression) where T : struct
	{
		Debug.Assert(startIndex < array.Length);
		Debug.Assert(startIndex + count <= array.Length);
		int num = startIndex + count;
		int num2 = 0;
		for (int i = startIndex; i < num; i++)
		{
			if (!isRemovableDuringSparseCompression(ref array[i]))
			{
				num2++;
				continue;
			}
			for (int j = i + 1; j < num; j++)
			{
				if (!isRemovableDuringSparseCompression(ref array[j]))
				{
					array[i++] = array[j];
					num2++;
				}
			}
			break;
		}
		return num2;
	}
}
