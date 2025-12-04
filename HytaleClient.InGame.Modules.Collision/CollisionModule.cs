using System;
using System.Collections.Generic;
using HytaleClient.Data.Map;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Collision;

internal class CollisionModule : Module
{
	public struct BlockOptions
	{
		public static BlockOptions Default = new BlockOptions
		{
			IgnoreFluids = false,
			IgnoreEmptyCollisionMaterial = false,
			BlockWhitelist = null
		};

		public bool IgnoreFluids;

		public bool IgnoreEmptyCollisionMaterial;

		public HashSet<int> BlockWhitelist;
	}

	public struct BlockRaycastOptions
	{
		public static BlockRaycastOptions Default = new BlockRaycastOptions
		{
			RaycastOptions = Raycast.Options.Default,
			Block = BlockOptions.Default
		};

		public Raycast.Options RaycastOptions;

		public BlockOptions Block;
	}

	public struct BlockResult
	{
		public static BlockResult Default = new BlockResult
		{
			Result = Raycast.Result.Default,
			Block = IntVector3.Zero
		};

		public Raycast.Result Result;

		public IntVector3 Block;

		public IntVector3 BlockNormal;

		public int BlockId;

		public ClientBlockType BlockType;

		public int BoxId;

		public BlockPosition GetBlockPosition()
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Expected O, but got Unknown
			return new BlockPosition(Block.X, Block.Y, Block.Z);
		}

		public bool IsSuccess()
		{
			return Result.IsSuccess();
		}
	}

	public struct CombinedOptions
	{
		public static readonly CombinedOptions Default = new CombinedOptions
		{
			RaycastOptions = Raycast.Options.Default,
			EnableBlock = true,
			Block = BlockOptions.Default,
			EnableEntity = true,
			Entity = EntityOptions.Default
		};

		public Raycast.Options RaycastOptions;

		public bool EnableBlock;

		public BlockOptions Block;

		public bool EnableEntity;

		public EntityOptions Entity;
	}

	public struct EntityOptions
	{
		public static EntityOptions Default = new EntityOptions
		{
			CheckLocalPlayer = false,
			CheckOnlyTangibleEntities = false
		};

		public bool CheckLocalPlayer;

		public bool CheckOnlyTangibleEntities;
	}

	public struct EntityRaycastOptions
	{
		public static EntityRaycastOptions Default = new EntityRaycastOptions
		{
			RaycastOptions = Raycast.Options.Default,
			Entity = EntityOptions.Default
		};

		public Raycast.Options RaycastOptions;

		public EntityOptions Entity;
	}

	public struct EntityResult
	{
		public static EntityResult Default = new EntityResult
		{
			Result = Raycast.Result.Default
		};

		public Raycast.Result Result;

		public Entity Entity;

		public bool IsSuccess()
		{
			return Result.IsSuccess();
		}
	}

	public struct CollisionHitData
	{
		public Vector3 Overlap { get; private set; }

		public Vector3 Limit { get; private set; }

		public CollisionHitData(Vector3 overlap, Vector3 limit)
		{
			Overlap = overlap;
			Limit = limit;
		}

		public bool GetXCollideState()
		{
			return Overlap.X > 0f;
		}

		public bool GetYCollideState()
		{
			return Overlap.Y > 0f;
		}

		public bool GetZCollideState()
		{
			return Overlap.Z > 0f;
		}

		public bool HasCollided()
		{
			return Overlap.X > 0f && Overlap.Y > 0f && Overlap.Z > 0f;
		}

		public override string ToString()
		{
			return "{Overlap: " + Overlap.ToString() + ", Limit: " + Limit.ToString() + "}";
		}
	}

	public bool FindTargetBlockOut(ref Ray ray, ref BlockRaycastOptions options, out BlockResult result)
	{
		result = BlockResult.Default;
		return FindTargetBlock(ref ray, ref options, ref result);
	}

	public bool FindTargetBlock(ref Ray ray, ref BlockRaycastOptions options, ref BlockResult result)
	{
		return FindTargetBlock(ref ray, ref options.RaycastOptions, ref options.Block, ref result);
	}

	public bool FindTargetBlock(ref Ray ray, ref Raycast.Options raycastOptions, ref BlockOptions blockOptions, ref BlockResult result)
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Invalid comparison between Unknown and I4
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Invalid comparison between Unknown and I4
		BlockIterator blockIterator = new BlockIterator(ray, raycastOptions.Distance);
		BlockAccessor blockAccessor = new BlockAccessor(_gameInstance.MapModule);
		while (blockIterator.HasNext())
		{
			blockIterator.Step(out var b, out var _, out var _, out var n);
			int blockIdFiller = blockAccessor.GetBlockIdFiller(b);
			if (blockIdFiller == 0 || (blockOptions.BlockWhitelist != null && !blockOptions.BlockWhitelist.Contains(blockIdFiller)))
			{
				continue;
			}
			ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[blockIdFiller];
			Material collisionMaterial = clientBlockType.CollisionMaterial;
			if ((blockOptions.IgnoreFluids && (int)collisionMaterial == 2) || (blockOptions.IgnoreEmptyCollisionMaterial && (int)collisionMaterial == 0))
			{
				continue;
			}
			BlockHitbox blockHitbox = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType];
			for (int i = 0; i < blockHitbox.Boxes.Length; i++)
			{
				BoundingBox box = blockHitbox.Boxes[i];
				if (Raycast.RaycastBox(ray, box, b, ref result.Result, ref raycastOptions))
				{
					result.Block = b;
					result.BlockId = clientBlockType.Id;
					result.BlockType = clientBlockType;
					result.BoxId = i;
					result.BlockNormal = n;
				}
			}
		}
		return result.IsSuccess();
	}

	public CollisionModule(GameInstance gameInstance)
		: base(gameInstance)
	{
	}

	public bool FindNearestTarget(ref Ray ray, ref CombinedOptions options, out BlockResult? blockResult, out EntityResult? entityResult)
	{
		Raycast.Result result;
		return FindNearestTarget(ref ray, ref options, out blockResult, out entityResult, out result);
	}

	public bool FindNearestTarget(ref Ray ray, ref CombinedOptions options, out BlockResult? blockResult, out EntityResult? entityResult, out Raycast.Result result)
	{
		if (!FindAllTargets(ref ray, ref options, out blockResult, out entityResult))
		{
			result = Raycast.Result.Default;
			return false;
		}
		if (blockResult.HasValue && entityResult.HasValue)
		{
			if (blockResult.Value.Result.NearT < entityResult.Value.Result.NearT)
			{
				result = blockResult.Value.Result;
				entityResult = null;
			}
			else
			{
				result = entityResult.Value.Result;
				blockResult = null;
			}
		}
		else if (blockResult.HasValue)
		{
			result = blockResult.Value.Result;
		}
		else
		{
			if (!entityResult.HasValue)
			{
				throw new InvalidOperationException();
			}
			result = entityResult.Value.Result;
		}
		return true;
	}

	public bool FindAllTargets(ref Ray ray, ref CombinedOptions options, out BlockResult? blockResult, out EntityResult? entityResult)
	{
		blockResult = null;
		entityResult = null;
		if (options.EnableBlock)
		{
			BlockResult result = BlockResult.Default;
			if (FindTargetBlock(ref ray, ref options.RaycastOptions, ref options.Block, ref result))
			{
				blockResult = result;
			}
		}
		if (options.EnableEntity)
		{
			EntityResult result2 = EntityResult.Default;
			if (FindTargetEntity(ref ray, ref options.RaycastOptions, ref options.Entity, ref result2))
			{
				entityResult = result2;
			}
		}
		return blockResult.HasValue || entityResult.HasValue;
	}

	public bool FindTargetEntityOut(ref Ray ray, ref EntityRaycastOptions options, out EntityResult result)
	{
		result = EntityResult.Default;
		return FindTargetEntity(ref ray, ref options, ref result);
	}

	public bool FindTargetEntity(ref Ray ray, ref EntityRaycastOptions options, ref EntityResult result)
	{
		return FindTargetEntity(ref ray, ref options.RaycastOptions, ref options.Entity, ref result);
	}

	public bool FindTargetEntity(ref Ray ray, ref Raycast.Options raycastOptions, ref EntityOptions entityOptions, ref EntityResult result)
	{
		Entity[] allEntities = _gameInstance.EntityStoreModule.GetAllEntities();
		int entitiesCount = _gameInstance.EntityStoreModule.GetEntitiesCount();
		for (int i = 0; i < entitiesCount; i++)
		{
			Entity entity = allEntities[i];
			if ((entityOptions.CheckLocalPlayer || entity.NetworkId != _gameInstance.LocalPlayerNetworkId) && (!entityOptions.CheckOnlyTangibleEntities || entity.IsTangible()) && Raycast.RaycastBox(ray, entity.Hitbox, entity.Position, ref result.Result, ref raycastOptions))
			{
				result.Entity = entity;
			}
		}
		return result.IsSuccess();
	}

	public bool CheckBlockCollision(IntVector3 block, BoundingBox box, Vector3 moveOffset, out CollisionHitData hitData)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Invalid comparison between Unknown and I4
		int blockIdFiller = new BlockAccessor(_gameInstance.MapModule).GetBlockIdFiller(block);
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[blockIdFiller];
		hitData = default(CollisionHitData);
		if (blockIdFiller == 0)
		{
			return false;
		}
		Material collisionMaterial = clientBlockType.CollisionMaterial;
		if ((int)collisionMaterial == 0 || (int)collisionMaterial == 2)
		{
			return false;
		}
		BlockHitbox blockHitbox = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType];
		BoundingBox[] boxes = blockHitbox.Boxes;
		foreach (BoundingBox boxB in boxes)
		{
			if (CheckBoxCollision(box, boxB, block.X, block.Y, block.Z, moveOffset, out hitData))
			{
				return true;
			}
		}
		return false;
	}

	public static bool CheckBoxCollision(BoundingBox boxA, BoundingBox boxB, float offsetX, float offsetY, float offsetZ, Vector3 moveOffset, out CollisionHitData hitData)
	{
		Vector3 zero = Vector3.Zero;
		Vector3 zero2 = Vector3.Zero;
		GetRangeOverlap(boxA.Min.X, boxA.Max.X, boxB.Min.X + offsetX, boxB.Max.X + offsetX, moveOffset.X, ref zero2.X, ref zero.X);
		GetRangeOverlap(boxA.Min.Y, boxA.Max.Y, boxB.Min.Y + offsetY, boxB.Max.Y + offsetY, moveOffset.Y, ref zero2.Y, ref zero.Y);
		GetRangeOverlap(boxA.Min.Z, boxA.Max.Z, boxB.Min.Z + offsetZ, boxB.Max.Z + offsetZ, moveOffset.Z, ref zero2.Z, ref zero.Z);
		hitData = new CollisionHitData(zero, zero2);
		return zero.X > 0f && zero.Y > 0f && zero.Z > 0f;
	}

	public static bool Get2DRayIntersection(Vector2 ray1Position, Vector2 ray1Direction, Vector2 ray2Position, Vector2 ray2Direction, out Vector2 intersection)
	{
		intersection = Vector2.Zero;
		float num = ray1Direction.Y / ray1Direction.X;
		float num2 = ray1Position.Y - num * ray1Position.X;
		float num3 = ray2Direction.Y / ray2Direction.X;
		float num4 = ray2Position.Y - num3 * ray2Position.X;
		if (num - num3 == 0f)
		{
			return false;
		}
		float num5 = (num4 - num2) / (num - num3);
		float y = num * num5 + num2;
		intersection = new Vector2(num5, y);
		return true;
	}

	public static bool CheckRayPlaneDistance(Vector3 planePoint, Vector3 planeNormal, Vector3 linePoint, Vector3 lineDirection, out float distance)
	{
		float num = Vector3.Dot(planeNormal, lineDirection);
		if (num == 0f)
		{
			distance = 0f;
			return false;
		}
		distance = (Vector3.Dot(planeNormal, planePoint) - Vector3.Dot(planeNormal, linePoint)) / num;
		return true;
	}

	private static void GetRangeOverlap(float minA, float maxA, float minB, float maxB, float offset, ref float limit, ref float overlap)
	{
		if ((minA <= minB && maxA >= maxB) || (minA > minB && maxA < maxB) || (minA < maxB && minA > minB) || (maxA > minB && maxA < maxB))
		{
			overlap = ((offset > 0f) ? (maxA - minB) : (maxB - minA));
			limit = ((offset > 0f) ? minB : maxB);
		}
	}
}
