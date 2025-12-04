using HytaleClient.Data.Map;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Map;

internal struct BlockAccessor
{
	private readonly MapModule _mapModule;

	private Chunk _chunk;

	public BlockAccessor(MapModule mapModule)
	{
		_mapModule = mapModule;
		_chunk = null;
	}

	public int GetBlockId(IntVector3 block)
	{
		int num = block.X >> 5;
		int num2 = block.Y >> 5;
		int num3 = block.Z >> 5;
		if (_chunk == null || _chunk.X != num || _chunk.Y != num2 || _chunk.Z != num3)
		{
			_chunk = _mapModule.GetChunk(num, num2, num3);
		}
		if (_chunk == null)
		{
			return 0;
		}
		return _chunk.Data.GetBlock(block.X, block.Y, block.Z);
	}

	public ClientBlockType GetBlockType(IntVector3 block)
	{
		return _mapModule.ClientBlockTypes[GetBlockId(block)];
	}

	public int GetBlockIdFiller(IntVector3 block)
	{
		ClientBlockType blockType = GetBlockType(block);
		int id = blockType.Id;
		if (blockType.FillerX == 0 && blockType.FillerY == 0 && blockType.FillerZ == 0)
		{
			return id;
		}
		block.X -= blockType.FillerX;
		block.Y -= blockType.FillerY;
		block.Z -= blockType.FillerZ;
		return _mapModule.GetBlock(block.X, block.Y, block.Z, 1);
	}

	public ClientBlockType GetBlockTypeFiller(IntVector3 block)
	{
		return _mapModule.ClientBlockTypes[GetBlockIdFiller(block)];
	}
}
