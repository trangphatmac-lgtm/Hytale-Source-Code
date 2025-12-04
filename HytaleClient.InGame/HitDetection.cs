using System;
using System.Collections.Generic;
using HytaleClient.Data.Map;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame;

internal class HitDetection
{
	public enum CollisionAxis
	{
		X,
		Y,
		Z
	}

	public struct EntityHitData
	{
		public Entity Entity { get; private set; }

		public RayBoxCollision RayBoxCollision { get; private set; }

		public float ClosestDistance { get; private set; }

		public EntityHitData(Entity entity, RayBoxCollision rayBoxCollision, float closestDistance)
		{
			Entity = entity;
			RayBoxCollision = rayBoxCollision;
			ClosestDistance = closestDistance;
		}
	}

	public struct CollisionHitData
	{
		public int? HitEntity;

		public Vector3 Overlap { get; private set; }

		public Vector3 Limit { get; private set; }

		public CollisionHitData(Vector3 overlap, Vector3 limit)
		{
			Overlap = overlap;
			Limit = limit;
			HitEntity = null;
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

	public struct BoxIntersection
	{
		public Vector3 Position { get; private set; }

		public Vector3 Normal { get; private set; }

		public BoxIntersection(Vector3 position, Vector3 normal)
		{
			Position = position;
			Normal = normal;
		}
	}

	public struct RayBoxCollision
	{
		public Vector3 Position { get; private set; }

		public Vector3 Normal { get; private set; }

		public Vector2 TextureCoord { get; private set; }

		public RayBoxCollision(Vector3 position, Vector3 normal, Vector2 textureCoord)
		{
			Position = position;
			Normal = normal;
			TextureCoord = textureCoord;
		}
	}

	public class RaycastOptions
	{
		public float Distance = 128f;

		public bool IgnoreFluids = false;

		public bool IgnoreEmptyCollisionMaterial = false;

		public bool CheckOversizedBoxes = false;

		public bool CheckOnlyTangibleEntities = true;

		public bool ReturnEndpointBlock = false;

		public int RequiredBlockTag = int.MinValue;
	}

	public struct RaycastHit
	{
		public Vector3 BlockPosition { get; private set; }

		public Vector3 BlockPositionNoFiller { get; private set; }

		public Vector3 BlockOrigin { get; private set; }

		public Vector3 StartPosition { get; private set; }

		public Vector3 HitPosition { get; private set; }

		public Vector3 Normal { get; private set; }

		public Vector3 BlockNormal { get; private set; }

		public Vector2 TextureCoord { get; private set; }

		public float Distance { get; private set; }

		public int BlockId { get; private set; }

		public int BlockHitboxId { get; private set; }

		public int BoxId { get; private set; }

		public RaycastHit(Vector3 blockPosition, Vector3 blockPositionNoFiller, Vector3 blockOrigin, Vector3 startPosition, Vector3 hitPosition, Vector3 normal, Vector3 blockNormal, Vector2 textureCoord, float distance, int blockId, int blockHitboxId, int boxId)
		{
			BlockPosition = blockPosition;
			BlockPositionNoFiller = blockPositionNoFiller;
			BlockOrigin = blockOrigin;
			StartPosition = startPosition;
			HitPosition = hitPosition;
			Normal = normal;
			BlockNormal = blockNormal;
			TextureCoord = textureCoord;
			Distance = distance;
			BlockId = blockId;
			BlockHitboxId = blockHitboxId;
			BoxId = boxId;
		}
	}

	private const int MaxRaycastIterations = 5000;

	public const float DefaultRaycastDistance = 128f;

	private const int OversizeRaycastDistance = 1;

	private static RaycastOptions DefaultRaycastOptions = new RaycastOptions();

	private readonly GameInstance _gameInstance;

	public HitDetection(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
	}

	public bool Raycast(Vector3 origin, Vector3 direction, RaycastOptions options, out bool hasFoundTargetBlock, out RaycastHit blockHitData, out bool hasFoundTargetEntity, out EntityHitData entityHitData)
	{
		hasFoundTargetBlock = _gameInstance.HitDetection.RaycastBlock(origin, direction, options, out blockHitData);
		hasFoundTargetEntity = _gameInstance.HitDetection.RaycastEntity(origin, direction, hasFoundTargetBlock ? blockHitData.Distance : options.Distance, options.CheckOnlyTangibleEntities, out entityHitData);
		return hasFoundTargetBlock | hasFoundTargetEntity;
	}

	public bool RaycastBlock(Vector3 origin, Vector3 direction, out RaycastHit raycastHit)
	{
		return RaycastBlock(origin, direction, DefaultRaycastOptions, out raycastHit);
	}

	public bool RaycastBlock(Vector3 origin, Vector3 direction, RaycastOptions options, out RaycastHit raycastHit)
	{
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Invalid comparison between Unknown and I4
		//IL_0590: Unknown result type (might be due to invalid IL or missing references)
		//IL_0596: Invalid comparison between Unknown and I4
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Invalid comparison between Unknown and I4
		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b6: Invalid comparison between Unknown and I4
		int num = 0;
		BoxIntersection intersection = new BoxIntersection(origin, Vector3.Zero);
		float num2 = options.Distance;
		RayBoxCollision rayBoxCollision = default(RayBoxCollision);
		bool flag = false;
		raycastHit = default(RaycastHit);
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		Chunk chunk = null;
		RayBoxCollision collision;
		while (Vector3.Distance(origin, intersection.Position) <= options.Distance && num < 5000)
		{
			if (num != 0)
			{
				NextGridIntersection(intersection.Position, direction, out intersection);
			}
			num++;
			int num6 = (int)System.Math.Floor(intersection.Position.X) - ((intersection.Normal.X > 0f) ? 1 : 0);
			int num7 = (int)System.Math.Floor(intersection.Position.Y) - ((intersection.Normal.Y > 0f) ? 1 : 0);
			int num8 = (int)System.Math.Floor(intersection.Position.Z) - ((intersection.Normal.Z > 0f) ? 1 : 0);
			int num9 = num6;
			int num10 = num7;
			int num11 = num8;
			int num12 = num6 >> 5;
			int num13 = num7 >> 5;
			int num14 = num8 >> 5;
			if (chunk == null || chunk.X != num12 || chunk.Y != num13 || chunk.Z != num14)
			{
				chunk = _gameInstance.MapModule.GetChunk(num12, num13, num14);
			}
			if (chunk == null)
			{
				continue;
			}
			int block = chunk.Data.GetBlock(num6, num7, num8);
			if (block == 0)
			{
				num3 = num6;
				num4 = num7;
				num5 = num8;
				continue;
			}
			ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
			if (clientBlockType.FillerX != 0 || clientBlockType.FillerY != 0 || clientBlockType.FillerZ != 0)
			{
				num6 -= clientBlockType.FillerX;
				num7 -= clientBlockType.FillerY;
				num8 -= clientBlockType.FillerZ;
				block = _gameInstance.MapModule.GetBlock(num6, num7, num8, 1);
				clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
			}
			Material collisionMaterial = clientBlockType.CollisionMaterial;
			if ((options.IgnoreFluids && (int)collisionMaterial == 2) || (options.IgnoreEmptyCollisionMaterial && (int)collisionMaterial == 0))
			{
				continue;
			}
			if (options.RequiredBlockTag != int.MinValue)
			{
				Dictionary<int, int[]> tagIndexes = clientBlockType.TagIndexes;
				if (tagIndexes != null && !tagIndexes.ContainsKey(options.RequiredBlockTag))
				{
					continue;
				}
			}
			float num15 = options.Distance;
			BlockHitbox blockHitbox = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType];
			int boxId = 0;
			for (int i = 0; i < blockHitbox.Boxes.Length; i++)
			{
				if (CheckRayBoxCollision(blockHitbox.Boxes[i], num6, num7, num8, intersection.Position, direction, out collision))
				{
					float num16 = Vector3.Distance(origin, collision.Position);
					if (!(num16 >= num15))
					{
						num15 = num16;
						rayBoxCollision = collision;
						boxId = i;
					}
				}
			}
			if (!(num15 < num2))
			{
				continue;
			}
			Vector3 vector = new Vector3(num6, num7, num8);
			Vector3 blockPositionNoFiller = new Vector3(num9, num10, num11);
			num2 = num15;
			raycastHit = new RaycastHit(vector, blockPositionNoFiller, vector, origin, rayBoxCollision.Position, rayBoxCollision.Normal, intersection.Normal, rayBoxCollision.TextureCoord, num2, block, clientBlockType.HitboxType, boxId);
			flag = true;
			break;
		}
		if (options.ReturnEndpointBlock && !flag)
		{
			Vector3 vector2 = new Vector3(num3, num4, num5);
			Vector3 blockPositionNoFiller2 = new Vector3(num3, num4, num5);
			int block2 = _gameInstance.MapModule.GetBlock(num3, num4, num5, 1);
			ClientBlockType clientBlockType2 = _gameInstance.MapModule.ClientBlockTypes[block2];
			int boxId2 = 0;
			raycastHit = new RaycastHit(vector2, blockPositionNoFiller2, vector2, origin, rayBoxCollision.Position, rayBoxCollision.Normal, intersection.Normal, rayBoxCollision.TextureCoord, options.Distance - 1f, block2, clientBlockType2.HitboxType, boxId2);
			flag = true;
		}
		if (options.CheckOversizedBoxes)
		{
			int num17 = 1;
			int num18 = (int)origin.X;
			int num19 = (int)origin.Y;
			int num20 = (int)origin.Z;
			for (int j = -num17; j <= num17; j++)
			{
				for (int k = -num17; k <= num17; k++)
				{
					for (int l = -num17; l <= num17; l++)
					{
						int num21 = j + num18;
						int num22 = k + num19;
						int num23 = l + num20;
						int num24 = num21 >> 5;
						int num25 = num22 >> 5;
						int num26 = num23 >> 5;
						if (chunk == null || chunk.X != num24 || chunk.Y != num25 || chunk.Z != num26)
						{
							chunk = _gameInstance.MapModule.GetChunk(num24, num25, num26);
						}
						if (chunk == null)
						{
							continue;
						}
						int block3 = chunk.Data.GetBlock(num21, num22, num23);
						if (block3 == 0)
						{
							continue;
						}
						ClientBlockType clientBlockType3 = _gameInstance.MapModule.ClientBlockTypes[block3];
						if ((options.IgnoreFluids && (int)clientBlockType3.CollisionMaterial == 2) || (options.IgnoreEmptyCollisionMaterial && (int)clientBlockType3.CollisionMaterial == 0))
						{
							continue;
						}
						BlockHitbox blockHitbox2 = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType3.HitboxType];
						if (!(blockHitbox2.BoundingBox.Min.X < 0f) && !(blockHitbox2.BoundingBox.Min.Y < 0f) && !(blockHitbox2.BoundingBox.Min.Z < 0f) && !(blockHitbox2.BoundingBox.Max.X > 1f) && !(blockHitbox2.BoundingBox.Max.Y > 1f) && !(blockHitbox2.BoundingBox.Max.Z > 1f))
						{
							continue;
						}
						int boxId3 = 0;
						float num15 = options.Distance;
						int num27 = num21 - clientBlockType3.FillerX;
						int num28 = num22 - clientBlockType3.FillerY;
						int num29 = num23 - clientBlockType3.FillerZ;
						for (int m = 0; m < blockHitbox2.Boxes.Length; m++)
						{
							if (CheckRayBoxCollision(blockHitbox2.Boxes[m], num27, num28, num29, origin, direction, out collision))
							{
								float num30 = Vector3.Distance(origin, collision.Position);
								if (!(num30 >= num15))
								{
									num15 = num30;
									rayBoxCollision = collision;
									boxId3 = m;
								}
							}
						}
						if (num15 < num2)
						{
							Vector3 vector3 = new Vector3((float)System.Math.Floor(rayBoxCollision.Position.X), (float)System.Math.Floor(rayBoxCollision.Position.Y), (float)System.Math.Floor(rayBoxCollision.Position.Z));
							if (rayBoxCollision.Normal.X == 1f)
							{
								vector3.X = (float)System.Math.Ceiling(rayBoxCollision.Position.X) - 1f;
							}
							else if (rayBoxCollision.Normal.Y == 1f)
							{
								vector3.Y = (float)System.Math.Ceiling(rayBoxCollision.Position.Y) - 1f;
							}
							else if (rayBoxCollision.Normal.Z == 1f)
							{
								vector3.Z = (float)System.Math.Ceiling(rayBoxCollision.Position.Z) - 1f;
							}
							num2 = num15;
							raycastHit = new RaycastHit(vector3, vector3, new Vector3(num27, num28, num29), origin, rayBoxCollision.Position, rayBoxCollision.Normal, intersection.Normal, rayBoxCollision.TextureCoord, num2, block3, clientBlockType3.HitboxType, boxId3);
							flag = true;
						}
					}
				}
			}
		}
		return flag;
	}

	public bool RaycastEntity(Vector3 rayPosition, Vector3 rayDirection, float maxDistance, bool checkOnlyTangibleEntities, out EntityHitData entityHitData)
	{
		float num = maxDistance;
		Entity entity = null;
		RayBoxCollision rayBoxCollision = default(RayBoxCollision);
		Entity[] allEntities = _gameInstance.EntityStoreModule.GetAllEntities();
		int entitiesCount = _gameInstance.EntityStoreModule.GetEntitiesCount();
		for (int i = 0; i < entitiesCount; i++)
		{
			Entity entity2 = allEntities[i];
			if (entity2.NetworkId == _gameInstance.LocalPlayerNetworkId || (checkOnlyTangibleEntities && !entity2.IsTangible()))
			{
				continue;
			}
			BoundingBox hitbox = entity2.Hitbox;
			hitbox.Translate(entity2.Position);
			if (CheckRayBoxCollision(hitbox, rayPosition, rayDirection, out var collision))
			{
				float num2 = Vector3.Distance(rayPosition, collision.Position);
				if (!(num2 >= num))
				{
					num = num2;
					entity = entity2;
					rayBoxCollision = collision;
				}
			}
		}
		entityHitData = new EntityHitData(entity, rayBoxCollision, num);
		return entity != null;
	}

	public static bool CheckRayBoxCollision(BoundingBox box, Vector3 position, Vector3 direction, out RayBoxCollision collision, bool checkReverse = false)
	{
		return CheckRayBoxCollision(box, 0f, 0f, 0f, position, direction, out collision, checkReverse);
	}

	public static bool CheckRayBoxCollision(BoundingBox box, float offsetX, float offsetY, float offsetZ, Vector3 position, Vector3 direction, out RayBoxCollision collision, bool checkReverse = false)
	{
		Vector3 zero = Vector3.Zero;
		Vector2 zero2 = Vector2.Zero;
		float num = box.Min.X + offsetX;
		float num2 = box.Max.X + offsetX;
		float num3 = box.Min.Y + offsetY;
		float num4 = box.Max.Y + offsetY;
		float num5 = box.Min.Z + offsetZ;
		float num6 = box.Max.Z + offsetZ;
		if (direction.X >= 0f)
		{
			float num7 = (num - position.X) / direction.X;
			if (checkReverse || num7 >= 0f)
			{
				zero = new Vector3(position.X + num7 * direction.X, position.Y + num7 * direction.Y, position.Z + num7 * direction.Z);
				if (zero.Y >= num3 && zero.Y <= num4 && zero.Z >= num5 && zero.Z <= num6)
				{
					collision = new RayBoxCollision(textureCoord: new Vector2(1f - (num6 - zero.Z) / (num6 - num5), 1f - (num4 - zero.Y) / (num4 - num3)), position: zero, normal: Vector3.Left);
					return true;
				}
			}
		}
		else
		{
			float num7 = (position.X - num2) / (0f - direction.X);
			if (checkReverse || num7 >= 0f)
			{
				zero = new Vector3(position.X + num7 * direction.X, position.Y + num7 * direction.Y, position.Z + num7 * direction.Z);
				if (zero.Y >= num3 && zero.Y <= num4 && zero.Z >= num5 && zero.Z <= num6)
				{
					collision = new RayBoxCollision(textureCoord: new Vector2((num6 - zero.Z) / (num6 - num5), 1f - (num4 - zero.Y) / (num4 - num3)), position: zero, normal: Vector3.Right);
					return true;
				}
			}
		}
		if (direction.Y >= 0f)
		{
			float num7 = (num3 - position.Y) / direction.Y;
			if (checkReverse || num7 >= 0f)
			{
				zero = new Vector3(position.X + num7 * direction.X, position.Y + num7 * direction.Y, position.Z + num7 * direction.Z);
				if (zero.X >= num && zero.X <= num2 && zero.Z >= num5 && zero.Z <= num6)
				{
					collision = new RayBoxCollision(textureCoord: new Vector2((num2 - zero.X) / (num2 - num), (num6 - zero.Z) / (num6 - num5)), position: zero, normal: Vector3.Down);
					return true;
				}
			}
		}
		else
		{
			float num7 = (position.Y - num4) / (0f - direction.Y);
			if (checkReverse || num7 >= 0f)
			{
				zero = new Vector3(position.X + num7 * direction.X, position.Y + num7 * direction.Y, position.Z + num7 * direction.Z);
				if (zero.X >= num && zero.X <= num2 && zero.Z >= num5 && zero.Z <= num6)
				{
					collision = new RayBoxCollision(textureCoord: new Vector2((num2 - zero.X) / (num2 - num), (num6 - zero.Z) / (num6 - num5)), position: zero, normal: Vector3.Up);
					return true;
				}
			}
		}
		if (direction.Z >= 0f)
		{
			float num7 = (num5 - position.Z) / direction.Z;
			if (checkReverse || num7 >= 0f)
			{
				zero = new Vector3(position.X + num7 * direction.X, position.Y + num7 * direction.Y, position.Z + num7 * direction.Z);
				if (zero.X >= num && zero.X <= num2 && zero.Y >= num3 && zero.Y <= num4)
				{
					collision = new RayBoxCollision(textureCoord: new Vector2((num2 - zero.X) / (num2 - num), 1f - (num4 - zero.Y) / (num4 - num3)), position: zero, normal: Vector3.Forward);
					return true;
				}
			}
		}
		else
		{
			float num7 = (position.Z - num6) / (0f - direction.Z);
			if (checkReverse || num7 >= 0f)
			{
				zero = new Vector3(position.X + num7 * direction.X, position.Y + num7 * direction.Y, position.Z + num7 * direction.Z);
				if (zero.X >= num && zero.X <= num2 && zero.Y >= num3 && zero.Y <= num4)
				{
					collision = new RayBoxCollision(textureCoord: new Vector2(1f - (num2 - zero.X) / (num2 - num), 1f - (num4 - zero.Y) / (num4 - num3)), position: zero, normal: Vector3.Backward);
					return true;
				}
			}
		}
		collision = default(RayBoxCollision);
		return false;
	}

	private static void NextGridIntersection(Vector3 position, Vector3 direction, out BoxIntersection intersection)
	{
		double num2;
		double num;
		double num3 = (num2 = (num = 1.0));
		if (direction.X > 0f)
		{
			num3 = (System.Math.Floor(position.X) + 1.0 - (double)position.X) / (double)direction.X;
		}
		else if (direction.X < 0f)
		{
			num3 = (System.Math.Floor(0f - position.X) + 1.0 + (double)position.X) / (double)(0f - direction.X);
		}
		if (direction.Y > 0f)
		{
			num2 = (System.Math.Floor(position.Y) + 1.0 - (double)position.Y) / (double)direction.Y;
		}
		else if (direction.Y < 0f)
		{
			num2 = (System.Math.Floor(0f - position.Y) + 1.0 + (double)position.Y) / (double)(0f - direction.Y);
		}
		if (direction.Z > 0f)
		{
			num = (System.Math.Floor(position.Z) + 1.0 - (double)position.Z) / (double)direction.Z;
		}
		else if (direction.Z < 0f)
		{
			num = (System.Math.Floor(0f - position.Z) + 1.0 + (double)position.Z) / (double)(0f - direction.Z);
		}
		double num4 = System.Math.Min(num3, System.Math.Min(num2, num));
		Vector3 zero = Vector3.Zero;
		if (direction.X >= 0f && num4 == num3)
		{
			zero.X = -1f;
		}
		else if (direction.X < 0f && num4 == num3)
		{
			zero.X = 1f;
		}
		else if (direction.Y >= 0f && num4 == num2)
		{
			zero.Y = -1f;
		}
		else if (direction.Y < 0f && num4 == num2)
		{
			zero.Y = 1f;
		}
		else if (direction.Z >= 0f && num4 == num)
		{
			zero.Z = -1f;
		}
		else if (direction.Z < 0f && num4 == num)
		{
			zero.Z = 1f;
		}
		Vector3 position2 = new Vector3((float)((double)position.X + num4 * (double)direction.X), (float)((double)position.Y + num4 * (double)direction.Y), (float)((double)position.Z + num4 * (double)direction.Z));
		intersection = new BoxIntersection(position2, zero);
	}

	public bool CheckBlockCollision(BoundingBox box, Vector3 pos, Vector3 moveOffset, out CollisionHitData hitData)
	{
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Invalid comparison between Unknown and I4
		int num = (int)System.Math.Floor(pos.X);
		int num2 = (int)System.Math.Floor(pos.Y);
		int num3 = (int)System.Math.Floor(pos.Z);
		int block = _gameInstance.MapModule.GetBlock(num, num2, num3, 1);
		if (block > 0)
		{
			ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
			if (clientBlockType.FillerX != 0 || clientBlockType.FillerY != 0 || clientBlockType.FillerZ != 0)
			{
				num -= clientBlockType.FillerX;
				num2 -= clientBlockType.FillerY;
				num3 -= clientBlockType.FillerZ;
				block = _gameInstance.MapModule.GetBlock(num, num2, num3, 1);
				clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
			}
			Material collisionMaterial = clientBlockType.CollisionMaterial;
			if ((int)collisionMaterial != 0 && (int)collisionMaterial != 2)
			{
				BlockHitbox blockHitbox = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType];
				BoundingBox[] boxes = blockHitbox.Boxes;
				foreach (BoundingBox boxB in boxes)
				{
					if (CheckBoxCollision(box, boxB, num, num2, num3, moveOffset, out hitData))
					{
						return true;
					}
				}
			}
		}
		hitData = default(CollisionHitData);
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

	public static bool CheckRayPlaneIntersection(Vector3 planePoint, Vector3 planeNormal, Vector3 linePoint, Vector3 lineDirection, out Vector3 intersection, bool forwardOnly = false)
	{
		float distance;
		bool flag = CheckRayPlaneDistance(planePoint, planeNormal, linePoint, lineDirection, out distance);
		intersection = linePoint + lineDirection * distance;
		return flag && (!forwardOnly || distance > 0f);
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

	public static float DistanceSquaredToPlanePointFromRayIntersection(Vector3 planePoint, Vector3 planeNormal, Vector3 rayPoint, Vector3 rayVector, out Vector3 intersection)
	{
		Vector3 vector = rayPoint - planePoint;
		float num = Vector3.Dot(vector, planeNormal);
		float num2 = Vector3.Dot(rayVector, planeNormal);
		if (num2 == 0f)
		{
			intersection = Vector3.NaN;
			return 0f;
		}
		float num3 = num / num2;
		intersection = rayPoint - rayVector * num3;
		return Vector3.DistanceSquared(intersection, planePoint);
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
