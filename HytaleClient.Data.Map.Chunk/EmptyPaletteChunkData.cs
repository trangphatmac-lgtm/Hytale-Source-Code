using System;
using System.Collections.Generic;
using System.IO;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Map.Chunk;

public class EmptyPaletteChunkData : IChunkData
{
	public static readonly EmptyPaletteChunkData Instance = new EmptyPaletteChunkData();

	private EmptyPaletteChunkData()
	{
	}

	public override BlockSetResult Set(int x, int y, int z, int blockId)
	{
		return (blockId == 0) ? BlockSetResult.BLOCK_UNCHANGED : BlockSetResult.REQUIRES_PROMOTE;
	}

	public override BlockSetResult Set(int blockIdx, int blockId)
	{
		return (blockId == 0) ? BlockSetResult.BLOCK_UNCHANGED : BlockSetResult.REQUIRES_PROMOTE;
	}

	public override int Get(int x, int y, int z)
	{
		return 0;
	}

	public override int Get(int blockIdx)
	{
		return 0;
	}

	public override bool ShouldDemote()
	{
		return false;
	}

	public override IChunkData Demote()
	{
		throw new InvalidOperationException("Cannot demote empty chunk section!");
	}

	public override IChunkData Promote()
	{
		return new HalfBytePaletteChunkData();
	}

	public override bool Contains(int blockId)
	{
		return blockId == 0;
	}

	public override int BlockCount()
	{
		return 1;
	}

	public override int Count(int blockId)
	{
		return (blockId == 0) ? 32768 : 0;
	}

	public override HashSet<int> Blocks()
	{
		HashSet<int> hashSet = new HashSet<int>();
		hashSet.Add(0);
		return hashSet;
	}

	public override Dictionary<int, ushort> BlockCounts()
	{
		Dictionary<int, ushort> dictionary = new Dictionary<int, ushort>();
		dictionary[0] = 32768;
		return dictionary;
	}

	public override bool IsSolidAir()
	{
		return true;
	}

	public override void Deserialize(BinaryReader reader, int maxValidBlockTypeId, PaletteType paletteType)
	{
	}
}
