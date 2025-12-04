using System;
using System.Collections;
using System.Collections.Generic;

namespace HytaleClient.Data.Map.Chunk;

public class BytePaletteChunkData : AbstractBytePaletteChunkData
{
	private const int KEY_MASK = 255;

	public const int MAX_SIZE = 256;

	public const int DEMOTE_SIZE = 14;

	public BytePaletteChunkData()
		: base(new byte[32768])
	{
	}

	public BytePaletteChunkData(Dictionary<int, byte> externalToInternal, int[] internalToExternal, BitArray internalIdSet, Dictionary<byte, ushort> internalIdCount, byte[] blocks)
		: base(externalToInternal, internalToExternal, internalIdSet, internalIdCount, blocks)
	{
	}

	internal override byte Get0(int idx)
	{
		return blockData[idx];
	}

	internal override void Set0(int idx, byte b)
	{
		blockData[idx] = b;
	}

	public override bool ShouldDemote()
	{
		return BlockCount() <= 14;
	}

	public override IChunkData Demote()
	{
		return HalfBytePaletteChunkData.fromBytePalette(this);
	}

	public override IChunkData Promote()
	{
		return ShortPaletteChunkData.fromBytePalette(this);
	}

	protected override bool IsValidInternalId(int internalId)
	{
		return (internalId & 0xFF) == internalId;
	}

	protected override int UnsignedInternalId(byte internalId)
	{
		return internalId & 0xFF;
	}

	private static int sUnsignedInternalId(byte internalId)
	{
		return internalId & 0xFF;
	}

	public static BytePaletteChunkData fromHalfBytePalette(HalfBytePaletteChunkData section)
	{
		BytePaletteChunkData bytePaletteChunkData = new BytePaletteChunkData();
		bytePaletteChunkData.externalToInternal.Clear();
		bytePaletteChunkData.internalToExternal = new int[section.internalToExternal.Length * 2];
		foreach (KeyValuePair<int, byte> item in section.externalToInternal)
		{
			int key = item.Key;
			byte value = item.Value;
			bytePaletteChunkData.externalToInternal[key] = value;
			bytePaletteChunkData.internalToExternal[value] = key;
		}
		bytePaletteChunkData.internalIdSet.SetAll(value: false);
		bytePaletteChunkData.internalIdSet.Or(section.internalIdSet);
		bytePaletteChunkData.internalIdCount.Clear();
		foreach (KeyValuePair<byte, ushort> item2 in section.internalIdCount)
		{
			bytePaletteChunkData.internalIdCount[item2.Key] = item2.Value;
		}
		for (int i = 0; i < bytePaletteChunkData.blockData.Length; i++)
		{
			bytePaletteChunkData.blockData[i] = section.Get0(i);
		}
		return bytePaletteChunkData;
	}

	public static BytePaletteChunkData fromShortPalette(ShortPaletteChunkData section)
	{
		if (section.BlockCount() > 256)
		{
			throw new InvalidOperationException("Cannot demote short palette to byte palette. Too many blocks! Count: " + section.BlockCount());
		}
		BytePaletteChunkData bytePaletteChunkData = new BytePaletteChunkData();
		Dictionary<short, byte> dictionary = new Dictionary<short, byte>();
		bytePaletteChunkData.internalToExternal = new int[256];
		bytePaletteChunkData.internalIdSet.SetAll(value: false);
		foreach (KeyValuePair<int, short> item in section.externalToInternal)
		{
			int key = item.Key;
			short value = item.Value;
			byte b = 0;
			while (b < bytePaletteChunkData.internalIdSet.Count && bytePaletteChunkData.internalIdSet[b])
			{
				b++;
			}
			bytePaletteChunkData.internalIdSet[b] = true;
			dictionary[value] = b;
			bytePaletteChunkData.internalToExternal[b] = key;
			bytePaletteChunkData.externalToInternal[key] = b;
		}
		bytePaletteChunkData.internalIdCount.Clear();
		foreach (KeyValuePair<short, ushort> item2 in section.internalIdCount)
		{
			short key2 = item2.Key;
			ushort value2 = item2.Value;
			byte key3 = dictionary[key2];
			bytePaletteChunkData.internalIdCount[key3] = value2;
		}
		for (int i = 0; i < 32768; i++)
		{
			short key4 = section.blockData[i];
			byte b2 = dictionary[key4];
			bytePaletteChunkData.blockData[i] = b2;
		}
		return bytePaletteChunkData;
	}
}
