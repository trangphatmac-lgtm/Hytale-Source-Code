using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Map.Chunk;

public abstract class AbstractBytePaletteChunkData : IChunkData
{
	internal Dictionary<int, byte> externalToInternal;

	internal int[] internalToExternal;

	internal BitArray internalIdSet;

	internal Dictionary<byte, ushort> internalIdCount;

	internal byte[] blockData;

	protected AbstractBytePaletteChunkData(byte[] blocks)
		: this(new Dictionary<int, byte>(), new int[16], new BitArray(256), new Dictionary<byte, ushort>(), blocks)
	{
		externalToInternal[0] = 0;
		internalToExternal[0] = 0;
		internalIdSet[0] = true;
		internalIdCount[0] = 32768;
	}

	protected AbstractBytePaletteChunkData(Dictionary<int, byte> externalToInternal, int[] internalToExternal, BitArray internalIdSet, Dictionary<byte, ushort> internalIdCount, byte[] blocks)
	{
		this.externalToInternal = externalToInternal;
		this.internalToExternal = internalToExternal;
		this.internalIdSet = internalIdSet;
		this.internalIdCount = internalIdCount;
		blockData = blocks;
	}

	public override int Get(int blockIdx)
	{
		byte b = Get0(blockIdx);
		return internalToExternal[b];
	}

	public override BlockSetResult Set(int blockIdx, int blockId)
	{
		byte b = Get0(blockIdx);
		if (externalToInternal.ContainsKey(blockId))
		{
			byte b2 = externalToInternal[blockId];
			if (b2 != b)
			{
				bool flag = DecrementBlockCount(b);
				IncrementBlockCount(b2);
				Set0(blockIdx, b2);
				if (flag)
				{
					return BlockSetResult.BLOCK_ADDED_OR_REMOVED;
				}
				return BlockSetResult.BLOCK_CHANGED;
			}
			return BlockSetResult.BLOCK_UNCHANGED;
		}
		int num = NextInternalId(b);
		if (!IsValidInternalId(num))
		{
			return BlockSetResult.REQUIRES_PROMOTE;
		}
		DecrementBlockCount(b);
		byte b3 = (byte)num;
		CreateBlockId(b3, blockId);
		Set0(blockIdx, b3);
		return BlockSetResult.BLOCK_ADDED_OR_REMOVED;
	}

	internal abstract byte Get0(int idx);

	internal abstract void Set0(int idx, byte b);

	public override bool Contains(int blockId)
	{
		return externalToInternal.ContainsKey(blockId);
	}

	public override int BlockCount()
	{
		return internalIdCount.Count;
	}

	public override int Count(int blockId)
	{
		if (externalToInternal.ContainsKey(blockId))
		{
			byte key = externalToInternal[blockId];
			return internalIdCount[key];
		}
		return 0;
	}

	public override HashSet<int> Blocks()
	{
		return new HashSet<int>(externalToInternal.Keys);
	}

	public override Dictionary<int, ushort> BlockCounts()
	{
		Dictionary<int, ushort> dictionary = new Dictionary<int, ushort>();
		foreach (KeyValuePair<byte, ushort> item in internalIdCount)
		{
			byte key = item.Key;
			ushort value = item.Value;
			int key2 = internalToExternal[key];
			dictionary[key2] = value;
		}
		return dictionary;
	}

	private void CreateBlockId(byte internalId, int blockId)
	{
		if (internalId >= internalToExternal.Length)
		{
			Array.Resize(ref internalToExternal, internalToExternal.Length * 2);
		}
		internalToExternal[internalId] = blockId;
		externalToInternal[blockId] = internalId;
		internalIdSet[UnsignedInternalId(internalId)] = true;
		internalIdCount[internalId] = 1;
	}

	private bool DecrementBlockCount(byte internalId)
	{
		ushort num = internalIdCount[internalId];
		if (num == 1)
		{
			internalIdCount.Remove(internalId);
			externalToInternal.Remove(internalToExternal[internalId]);
			internalToExternal[internalId] = -1;
			internalIdSet[UnsignedInternalId(internalId)] = false;
			return true;
		}
		internalIdCount[internalId] = (ushort)(num - 1);
		return false;
	}

	private void IncrementBlockCount(byte internalId)
	{
		ushort num = internalIdCount[internalId];
		internalIdCount[internalId] = (ushort)(num + 1);
	}

	private int NextInternalId(byte oldInternalId)
	{
		if (internalIdCount[oldInternalId] == 1)
		{
			return UnsignedInternalId(oldInternalId);
		}
		int i;
		for (i = 0; i < 256 && internalIdSet[i]; i++)
		{
		}
		return i;
	}

	protected abstract bool IsValidInternalId(int internalId);

	protected abstract int UnsignedInternalId(byte internalId);

	public override void Deserialize(BinaryReader reader, int maxValidBlockTypeId, PaletteType paletteType)
	{
		externalToInternal.Clear();
		internalIdCount.Clear();
		internalIdSet.SetAll(value: false);
		ushort num = reader.ReadUInt16();
		internalToExternal = new int[num];
		for (int i = 0; i < num; i++)
		{
			byte b = reader.ReadByte();
			int num2 = reader.ReadInt32();
			ushort value = reader.ReadUInt16();
			if (num2 > maxValidBlockTypeId)
			{
				num2 = 1;
			}
			externalToInternal[num2] = b;
			if (b >= internalToExternal.Length)
			{
				Array.Resize(ref internalToExternal, b + 1);
			}
			internalToExternal[b] = num2;
			internalIdSet[b] = true;
			internalIdCount[b] = value;
		}
		reader.Read(blockData, 0, blockData.Length);
	}
}
