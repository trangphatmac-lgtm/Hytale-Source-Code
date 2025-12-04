using System.Collections.Generic;
using HytaleClient.Utils;

namespace HytaleClient.Data.Map.Chunk;

public abstract class IChunkDataBase
{
	public virtual int Get(int x, int y, int z)
	{
		return Get(ChunkHelper.IndexOfBlockInChunk(x, y, z));
	}

	public abstract int Get(int blockIdx);

	public abstract bool Contains(int blockId);

	public abstract int BlockCount();

	public abstract int Count(int blockId);

	public abstract HashSet<int> Blocks();

	public abstract Dictionary<int, ushort> BlockCounts();

	public abstract bool IsSolidAir();
}
