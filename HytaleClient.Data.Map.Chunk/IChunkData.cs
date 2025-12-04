using System.IO;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.Data.Map.Chunk;

public abstract class IChunkData : IChunkDataBase
{
	public virtual BlockSetResult Set(int x, int y, int z, int blockId)
	{
		return Set(ChunkHelper.IndexOfBlockInChunk(x, y, z), blockId);
	}

	public abstract BlockSetResult Set(int blockIdx, int blockId);

	public override bool IsSolidAir()
	{
		return BlockCount() == 1 && Contains(0);
	}

	public abstract bool ShouldDemote();

	public abstract IChunkData Demote();

	public abstract IChunkData Promote();

	public abstract void Deserialize(BinaryReader reader, int maxValidBlockTypeId, PaletteType paletteType);
}
