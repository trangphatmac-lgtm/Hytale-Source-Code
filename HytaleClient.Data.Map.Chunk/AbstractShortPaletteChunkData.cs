using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Map.Chunk;

public abstract class AbstractShortPaletteChunkData : IChunkData
{
	internal Dictionary<int, short> externalToInternal;

	internal int[] internalToExternal;

	internal BitArray internalIdSet;

	internal Dictionary<short, ushort> internalIdCount;

	internal short[] blockData;

	public AbstractShortPaletteChunkData(short[] blocks)
		: this(new Dictionary<int, short>(), new int[512], new BitArray(32768), new Dictionary<short, ushort>(), blocks)
	{
		externalToInternal[0] = 0;
		internalToExternal[0] = 0;
		internalIdSet[0] = true;
		internalIdCount[0] = 32768;
	}

	protected AbstractShortPaletteChunkData(Dictionary<int, short> externalToInternal, int[] internalToExternal, BitArray internalIdSet, Dictionary<short, ushort> internalIdCount, short[] blocks)
	{
		this.externalToInternal = externalToInternal;
		this.internalToExternal = internalToExternal;
		this.internalIdSet = internalIdSet;
		this.internalIdCount = internalIdCount;
		blockData = blocks;
	}

	public override int Get(int blockIdx)
	{
		short num = Get0(blockIdx);
		return internalToExternal[num];
	}

	public override BlockSetResult Set(int blockIdx, int blockId)
	{
		short num = Get0(blockIdx);
		if (externalToInternal.ContainsKey(blockId))
		{
			short num2 = externalToInternal[blockId];
			if (num2 != num)
			{
				bool flag = DecrementBlockCount(num);
				IncrementBlockCount(num2);
				Set0(blockIdx, num2);
				if (flag)
				{
					return BlockSetResult.BLOCK_ADDED_OR_REMOVED;
				}
				return BlockSetResult.BLOCK_CHANGED;
			}
			return BlockSetResult.BLOCK_UNCHANGED;
		}
		int num3 = NextInternalId(num);
		if (!IsValidInternalId(num3))
		{
			return BlockSetResult.REQUIRES_PROMOTE;
		}
		DecrementBlockCount(num);
		short num4 = (short)num3;
		CreateBlockId(num4, blockId);
		Set0(blockIdx, num4);
		return BlockSetResult.BLOCK_ADDED_OR_REMOVED;
	}

	internal abstract short Get0(int idx);

	internal abstract void Set0(int idx, short s);

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
			short key = externalToInternal[blockId];
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
		foreach (KeyValuePair<short, ushort> item in internalIdCount)
		{
			short key = item.Key;
			ushort value = item.Value;
			int key2 = internalToExternal[key];
			dictionary[key2] = value;
		}
		return dictionary;
	}

	private void CreateBlockId(short internalId, int blockId)
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

	private bool DecrementBlockCount(short internalId)
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
		num = (internalIdCount[internalId] = (ushort)(num - 1));
		return false;
	}

	private void IncrementBlockCount(short internalId)
	{
		ushort num = internalIdCount[internalId];
		internalIdCount[internalId] = (ushort)(num + 1);
	}

	private int NextInternalId(short oldInternalId)
	{
		if (internalIdCount[oldInternalId] == 1)
		{
			return UnsignedInternalId(oldInternalId);
		}
		int i;
		for (i = 0; i < internalIdCount.Count && internalIdSet[i]; i++)
		{
		}
		return i;
	}

	protected abstract bool IsValidInternalId(int internalId);

	protected abstract int UnsignedInternalId(short internalId);

	public override void Deserialize(BinaryReader reader, int maxValidBlockTypeId, PaletteType paletteType)
	{
		externalToInternal.Clear();
		internalIdCount.Clear();
		internalIdSet.SetAll(value: false);
		ushort num = reader.ReadUInt16();
		internalToExternal = new int[num];
		for (int i = 0; i < num; i++)
		{
			short num2 = reader.ReadInt16();
			int num3 = reader.ReadInt32();
			ushort value = reader.ReadUInt16();
			if (num3 > maxValidBlockTypeId)
			{
				num3 = 1;
			}
			externalToInternal[num3] = num2;
			if (num2 >= internalToExternal.Length)
			{
				Array.Resize(ref internalToExternal, num2 + 1);
			}
			internalToExternal[num2] = num3;
			internalIdSet[num2] = true;
			internalIdCount[num2] = value;
		}
		MemoryStream memoryStream = (MemoryStream)reader.BaseStream;
		Buffer.BlockCopy(memoryStream.GetBuffer(), (int)memoryStream.Position, blockData, 0, blockData.Length * 2);
		memoryStream.Position += blockData.Length * 2;
	}
}
