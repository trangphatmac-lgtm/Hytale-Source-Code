using System;
using System.Collections;
using System.Collections.Generic;

namespace HytaleClient.Data.Map.Chunk;

public class HalfBytePaletteChunkData : AbstractBytePaletteChunkData
{
	private const int KEY_MASK = 15;

	public const int MAX_SIZE = 16;

	public HalfBytePaletteChunkData()
		: base(new byte[16384])
	{
	}

	public HalfBytePaletteChunkData(Dictionary<int, byte> externalToInternal, int[] internalToExternal, BitArray internalIdSet, Dictionary<byte, ushort> internalIdCount, byte[] blocks)
		: base(externalToInternal, internalToExternal, internalIdSet, internalIdCount, blocks)
	{
	}

	internal override void Set0(int idx, byte b)
	{
		int num = idx >> 1;
		byte b2 = blockData[num];
		b = (byte)(b & 0xFu);
		int num2 = idx & 1;
		b = (byte)(b << ((num2 ^ 1) << 2));
		b2 = (byte)(b2 & (15 << (num2 << 2)));
		b2 |= b;
		blockData[num] = b2;
	}

	internal override byte Get0(int idx)
	{
		int num = idx >> 1;
		byte b = blockData[num];
		int num2 = idx & 1;
		b = (byte)(b >> ((num2 ^ 1) << 2));
		return (byte)(b & 0xFu);
	}

	public override bool ShouldDemote()
	{
		return IsSolidAir();
	}

	public override IChunkData Demote()
	{
		return EmptyPaletteChunkData.Instance;
	}

	public override IChunkData Promote()
	{
		return BytePaletteChunkData.fromHalfBytePalette(this);
	}

	protected override bool IsValidInternalId(int internalId)
	{
		return (internalId & 0xF) == internalId;
	}

	protected override int UnsignedInternalId(byte internalId)
	{
		return internalId & 0xF;
	}

	private static int sUnsignedInternalId(byte internalId)
	{
		return internalId & 0xF;
	}

	public static HalfBytePaletteChunkData fromBytePalette(BytePaletteChunkData section)
	{
		if (section.BlockCount() > 16)
		{
			throw new InvalidOperationException("Cannot demote byte palette to half byte palette. Too many blocks! Count: " + section.BlockCount());
		}
		HalfBytePaletteChunkData halfBytePaletteChunkData = new HalfBytePaletteChunkData();
		Dictionary<byte, byte> dictionary = new Dictionary<byte, byte>();
		halfBytePaletteChunkData.internalToExternal = new int[16];
		halfBytePaletteChunkData.internalIdSet.SetAll(value: false);
		foreach (KeyValuePair<int, byte> item in section.externalToInternal)
		{
			int key = item.Key;
			byte value = item.Value;
			byte b = 0;
			while (b < halfBytePaletteChunkData.internalIdSet.Count && halfBytePaletteChunkData.internalIdSet[b])
			{
				b++;
			}
			halfBytePaletteChunkData.internalIdSet[sUnsignedInternalId(b)] = true;
			dictionary[value] = b;
			halfBytePaletteChunkData.internalToExternal[b] = key;
			halfBytePaletteChunkData.externalToInternal[key] = b;
		}
		halfBytePaletteChunkData.internalIdCount.Clear();
		foreach (KeyValuePair<byte, ushort> item2 in section.internalIdCount)
		{
			byte key2 = item2.Key;
			ushort value2 = item2.Value;
			byte key3 = dictionary[key2];
			halfBytePaletteChunkData.internalIdCount[key3] = value2;
		}
		for (int i = 0; i < section.blockData.Length; i++)
		{
			byte key4 = section.blockData[i];
			byte b2 = dictionary[key4];
			halfBytePaletteChunkData.Set0(i, b2);
		}
		return halfBytePaletteChunkData;
	}
}
