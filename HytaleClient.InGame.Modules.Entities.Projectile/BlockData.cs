using System;
using HytaleClient.Data.Map;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class BlockData
{
	protected int _blockId = int.MaxValue;

	public ClientBlockType BlockType;

	public ClientBlockType OriginalBlockType;

	protected int _originalBlockTypeId = int.MaxValue;

	protected ClientBlockType _submergeFluid;

	protected int _submergeFluidId = int.MaxValue;

	public float FillHeight;

	public int CollisionMaterials;

	public bool IsFiller;

	protected BlockHitbox _blockBoundingBoxes;

	public bool IsTrigger => false;

	public int BlockDamage => 0;

	public void Assign(BlockData other)
	{
		_blockId = other._blockId;
		BlockType = other.BlockType;
		_originalBlockTypeId = other._originalBlockTypeId;
		OriginalBlockType = other.OriginalBlockType;
		_submergeFluid = other._submergeFluid;
		_submergeFluidId = other._submergeFluidId;
		FillHeight = other.FillHeight;
		CollisionMaterials = other.CollisionMaterials;
		IsFiller = other.IsFiller;
		_blockBoundingBoxes = other._blockBoundingBoxes;
	}

	public void Clear()
	{
		_blockId = int.MaxValue;
		BlockType = null;
		_originalBlockTypeId = int.MaxValue;
		OriginalBlockType = null;
		_submergeFluid = null;
		_submergeFluidId = int.MaxValue;
		_blockBoundingBoxes = null;
	}

	public ClientBlockType GetSubmergeFluid(GameInstance gameInstance)
	{
		if (FillHeight > 0f && _submergeFluid == null && _submergeFluidId == int.MaxValue)
		{
			_submergeFluidId = BlockType.FluidBlockId;
			if (_submergeFluidId == 0)
			{
				throw new Exception("Have fluid key but fill level is 0");
			}
			_submergeFluid = gameInstance.MapModule.ClientBlockTypes[_submergeFluidId];
		}
		return _submergeFluid;
	}

	public BlockHitbox GetBlockBoundingBoxes(GameInstance gameInstance)
	{
		if (_blockBoundingBoxes == null)
		{
			_blockBoundingBoxes = gameInstance.ServerSettings.BlockHitboxes[OriginalBlockType.HitboxType];
		}
		return _blockBoundingBoxes;
	}

	public int OriginX(int x)
	{
		return x - BlockType.FillerX;
	}

	public int OriginY(int y)
	{
		return y - BlockType.FillerY;
	}

	public int OriginZ(int z)
	{
		return z - BlockType.FillerZ;
	}
}
