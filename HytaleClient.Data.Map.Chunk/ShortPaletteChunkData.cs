using System;
using System.Collections;
using System.Collections.Generic;

namespace HytaleClient.Data.Map.Chunk;

public class ShortPaletteChunkData : AbstractShortPaletteChunkData
{
	private const int KEY_MASK = 65535;

	private const int MAX_SIZE = 65536;

	private const int DEMOTE_SIZE = 254;

	public ShortPaletteChunkData()
		: base(new short[32768])
	{
	}

	public ShortPaletteChunkData(Dictionary<int, short> externalToInternal, int[] internalToExternal, BitArray internalIdSet, Dictionary<short, ushort> internalIdCount, short[] blocks)
		: base(externalToInternal, internalToExternal, internalIdSet, internalIdCount, blocks)
	{
	}

	internal override short Get0(int idx)
	{
		return blockData[idx];
	}

	internal override void Set0(int idx, short s)
	{
		blockData[idx] = s;
	}

	public override bool ShouldDemote()
	{
		return BlockCount() <= 254;
	}

	public override IChunkData Demote()
	{
		return BytePaletteChunkData.fromShortPalette(this);
	}

	public override IChunkData Promote()
	{
		throw new InvalidOperationException("Short palette cannot be promoted.");
	}

	protected override bool IsValidInternalId(int internalId)
	{
		return (internalId & 0xFFFF) == internalId;
	}

	protected override int UnsignedInternalId(short internalId)
	{
		return internalId & 0xFFFF;
	}

	public static ShortPaletteChunkData fromBytePalette(BytePaletteChunkData section)
	{
		Dictionary<int, short> dictionary = new Dictionary<int, short>();
		int[] array = new int[section.internalToExternal.Length * 2];
		foreach (KeyValuePair<int, byte> item in section.externalToInternal)
		{
			int key = item.Key;
			byte value = item.Value;
			dictionary[key] = value;
			array[value] = key;
		}
		BitArray bitArray = new BitArray(32768);
		for (int i = 0; i < 256; i++)
		{
			bitArray[i] = section.internalIdSet[i];
		}
		Dictionary<short, ushort> dictionary2 = new Dictionary<short, ushort>();
		foreach (KeyValuePair<byte, ushort> item2 in section.internalIdCount)
		{
			byte key2 = item2.Key;
			ushort value2 = item2.Value;
			dictionary2[key2] = value2;
		}
		short[] array2 = new short[32768];
		for (int j = 0; j < 32768; j++)
		{
			array2[j] = section.blockData[j];
		}
		return new ShortPaletteChunkData(dictionary, array, bitArray, dictionary2, array2);
	}
}
