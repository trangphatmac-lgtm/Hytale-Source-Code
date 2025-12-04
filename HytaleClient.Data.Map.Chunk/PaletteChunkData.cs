using System;
using System.Collections.Generic;

namespace HytaleClient.Data.Map.Chunk;

public class PaletteChunkData : IChunkDataBase
{
	private IChunkData chunkSection;

	public PaletteChunkData()
	{
		chunkSection = EmptyPaletteChunkData.Instance;
	}

	public PaletteChunkData(IChunkData chunkSection)
	{
		this.chunkSection = chunkSection;
	}

	public IChunkData GetChunkSection()
	{
		return chunkSection;
	}

	public void SetChunkSection(IChunkData chunkSection)
	{
		this.chunkSection = chunkSection;
	}

	public override int Get(int blockIdx)
	{
		return chunkSection.Get(blockIdx);
	}

	public void Set(int blockIdx, int blockId)
	{
		switch (chunkSection.Set(blockIdx, blockId))
		{
		case BlockSetResult.REQUIRES_PROMOTE:
			chunkSection = chunkSection.Promote();
			if (chunkSection.Set(blockIdx, blockId) != 0)
			{
				throw new InvalidOperationException("Promoted chunk section failed to correctly add the new block!");
			}
			break;
		default:
			if (chunkSection.ShouldDemote())
			{
				chunkSection = chunkSection.Demote();
			}
			break;
		}
	}

	public override bool Contains(int blockId)
	{
		return chunkSection.Contains(blockId);
	}

	public override int BlockCount()
	{
		return chunkSection.BlockCount();
	}

	public override int Count(int blockId)
	{
		return chunkSection.Count(blockId);
	}

	public override HashSet<int> Blocks()
	{
		return chunkSection.Blocks();
	}

	public override Dictionary<int, ushort> BlockCounts()
	{
		return chunkSection.BlockCounts();
	}

	public override bool IsSolidAir()
	{
		return chunkSection.IsSolidAir();
	}
}
