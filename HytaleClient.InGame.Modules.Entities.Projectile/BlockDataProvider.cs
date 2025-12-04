using HytaleClient.Data.Map;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class BlockDataProvider : BlockData
{
	protected const int FullLevel = 8;

	protected const int InvalidChunkSectionIndex = int.MinValue;

	protected GameInstance _gameInstance;

	protected ChunkColumn _chunk;

	protected int _chunkSectionIndex;

	protected Chunk _chunkSection;

	public void Initialize(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
		_blockId = int.MaxValue;
		Cleanup0();
	}

	public void Cleanup()
	{
		_gameInstance = null;
		Cleanup0();
	}

	public void Read(int x, int y, int z)
	{
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Invalid comparison between Unknown and I4
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Invalid comparison between Unknown and I4
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Invalid comparison between Unknown and I4
		int num = ReadBlockId(x, y, z);
		if (_blockId == num)
		{
			return;
		}
		switch (num)
		{
		case 0:
			SetBlock(0, _gameInstance.MapModule.ClientBlockTypes[0], 1);
			return;
		case 1:
			SetBlock(int.MaxValue, _gameInstance.MapModule.ClientBlockTypes[1], 4);
			return;
		}
		_blockId = num;
		BlockType = _gameInstance.MapModule.ClientBlockTypes[_blockId];
		if (BlockType.Unknown)
		{
			SetBlock(num, BlockType, 4);
			return;
		}
		IsFiller = BlockType.FillerX != 0 || BlockType.FillerY != 0 || BlockType.FillerZ != 0;
		if (IsFiller)
		{
			_originalBlockTypeId = BlockType.VariantOriginalId;
			if (_originalBlockTypeId == 0)
			{
				_originalBlockTypeId = 1;
			}
			OriginalBlockType = _gameInstance.MapModule.ClientBlockTypes[_originalBlockTypeId];
			CollisionMaterials = (((int)OriginalBlockType.CollisionMaterial == 1) ? 4 : 0);
			CollisionMaterials += MaterialFromFillLevel(BlockType);
		}
		else
		{
			_originalBlockTypeId = _blockId;
			OriginalBlockType = BlockType;
			if ((int)BlockType.CollisionMaterial == 1)
			{
				CollisionMaterials = 4;
				if (BlockType.HitboxType != 0)
				{
					CollisionMaterials += MaterialFromFillLevel(BlockType);
				}
			}
			else
			{
				CollisionMaterials = MaterialFromFillLevel(BlockType);
			}
		}
		FillHeight = (((int)BlockType.CollisionMaterial == 2 || BlockType.FluidBlockId != 0) ? ((float)(int)BlockType.VerticalFill / 8f) : 0f);
		_blockBoundingBoxes = null;
	}

	protected int ReadBlockId(int x, int y, int z)
	{
		int num = ChunkHelper.ChunkCoordinate(x);
		int num2 = ChunkHelper.ChunkCoordinate(z);
		if (_chunk == null || _chunk.X != num || _chunk.Z != num2)
		{
			_chunk = _gameInstance.MapModule.GetChunkColumn(num, num2);
			_chunkSectionIndex = int.MinValue;
			_chunkSection = null;
		}
		if (_chunk == null)
		{
			return 1;
		}
		int num3 = ChunkHelper.IndexSection(y);
		if (_chunkSection == null || _chunkSection.Y != num3)
		{
			_chunkSectionIndex = num3;
			_chunkSection = ((num3 >= 0 && num3 < ChunkHelper.ChunksPerColumn) ? _chunk.GetChunk(num3) : null);
		}
		if (_chunkSection == null)
		{
			return 0;
		}
		return _chunkSection.Data.GetBlock(x, y, z);
	}

	protected void SetBlock(int id, ClientBlockType type, int material, BlockHitbox box)
	{
		_blockId = id;
		BlockType = type;
		OriginalBlockType = BlockType;
		_originalBlockTypeId = _blockId;
		CollisionMaterials = material;
		_blockBoundingBoxes = box;
		IsFiller = false;
		FillHeight = 0f;
	}

	protected void SetBlock(int id, ClientBlockType type, int material)
	{
		SetBlock(id, type, material, _gameInstance.ServerSettings.BlockHitboxes[0]);
	}

	protected void Cleanup0()
	{
		_chunk = null;
		_chunkSectionIndex = int.MinValue;
		_chunkSection = null;
		BlockType = null;
		_blockId = int.MaxValue;
		OriginalBlockType = null;
		_originalBlockTypeId = int.MaxValue;
		_blockBoundingBoxes = null;
	}

	protected static int MaterialFromFillLevel(ClientBlockType blockType)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		return (((int)blockType.CollisionMaterial == 2 || blockType.FluidBlockId != int.MaxValue) ? blockType.VerticalFill : 0) switch
		{
			0 => 1, 
			8 => 2, 
			_ => 3, 
		};
	}
}
