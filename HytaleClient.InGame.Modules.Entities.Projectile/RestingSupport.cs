using System;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class RestingSupport
{
	protected int _supportMinX;

	protected int _supportMaxX;

	protected int _supportMinY;

	protected int _supportMaxY;

	protected int _supportMinZ;

	protected int _supportMaxZ;

	protected int[] _supportBlocks;

	public bool HasChanged(GameInstance gameInstance)
	{
		if (_supportBlocks == null)
		{
			return false;
		}
		int num = 0;
		for (int i = _supportMinZ; i <= _supportMaxZ; i++)
		{
			for (int j = _supportMinX; j <= _supportMaxX; j++)
			{
				ChunkColumn chunkColumn = gameInstance.MapModule.GetChunkColumn(j, i);
				if (chunkColumn == null)
				{
					continue;
				}
				for (int k = _supportMinY; k <= _supportMaxY; k++)
				{
					if (_supportBlocks[num++] != gameInstance.MapModule.GetBlock(j, k, i, int.MaxValue))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public void Rest(GameInstance gameInstance, BoundingBox boundingBox, Vector3 position)
	{
		if (_supportBlocks == null)
		{
			Vector3 size = boundingBox.GetSize();
			int num = (int)(System.Math.Ceiling(size.X + 1f) * System.Math.Ceiling(size.Z + 1f) * System.Math.Ceiling(size.Y + 1f));
			_supportBlocks = new int[num];
		}
		_supportMinX = (int)System.Math.Floor(position.X + boundingBox.Min.X);
		_supportMaxX = (int)System.Math.Floor(position.X + boundingBox.Max.X);
		_supportMinZ = (int)System.Math.Floor(position.Z + boundingBox.Min.Z);
		_supportMaxZ = (int)System.Math.Floor(position.Z + boundingBox.Max.Z);
		_supportMinY = (int)System.Math.Floor(position.Y + boundingBox.Min.Y);
		_supportMaxY = (int)System.Math.Floor(position.Y + boundingBox.Max.Y);
		int num2 = 0;
		for (int i = _supportMinZ; i <= _supportMaxZ; i++)
		{
			for (int j = _supportMinX; j <= _supportMaxX; j++)
			{
				ChunkColumn chunkColumn = gameInstance.MapModule.GetChunkColumn(j, i);
				if (chunkColumn != null)
				{
					for (int k = _supportMinY; k <= _supportMaxY; k++)
					{
						_supportBlocks[num2++] = gameInstance.MapModule.GetBlock(j, k, i, int.MaxValue);
					}
				}
				else
				{
					for (int l = _supportMinY; l <= _supportMaxY; l++)
					{
						_supportBlocks[num2++] = 1;
					}
				}
			}
		}
	}

	public void Clear()
	{
		_supportBlocks = null;
	}
}
