using System;
using HytaleClient.Data.Map;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class BlockCollisionProvider : BoxBlockIterator.BoxIterationConsumer
{
	protected readonly BoxBlockIntersectionEvaluator _boxBlockIntersectionEvaluator = new BoxBlockIntersectionEvaluator();

	protected readonly MovingBoxBoxCollisionEvaluator _movingBoxBoxCollisionEvaluator = new MovingBoxBoxCollisionEvaluator();

	protected readonly BlockDataProvider _blockData = new BlockDataProvider();

	protected BoundingBox _fluidBox = new BoundingBox(Vector3.Zero, Vector3.One);

	protected readonly CollisionTracker _damageTracker = new CollisionTracker();

	protected readonly CollisionTracker _triggerTracker = new CollisionTracker();

	protected readonly BlockTracker _collisionTracker = new BlockTracker();

	public int RequestedCollisionMaterials = 4;

	protected bool _reportOverlaps;

	protected PredictedProjectile _collisionConsumer;

	protected BlockTracker _activeTriggers;

	protected Vector3 _motion;

	protected GameInstance _gameInstance;

	protected float _relativeStopDistance;

	protected PredictedProjectile.Result _collisionState;

	public bool ReportOverlaps
	{
		set
		{
			_reportOverlaps = value;
			_movingBoxBoxCollisionEvaluator.ComputeOverlaps = _reportOverlaps;
		}
	}

	public bool Next()
	{
		return OnSliceFinished();
	}

	public bool Accept(long x, long y, long z)
	{
		return ProcessBlockDynamic((int)x, (int)y, (int)z);
	}

	public void Cast(GameInstance gameInstance, BoundingBox collider, Vector3 pos, Vector3 v, PredictedProjectile collisionConsumer, BlockTracker activeTriggers, float collisionStop)
	{
		_collisionConsumer = collisionConsumer;
		_activeTriggers = activeTriggers;
		_motion = v;
		_gameInstance = gameInstance;
		_blockData.Initialize(gameInstance);
		if (!CollisionMath.IsBelowMovementThreshold(v))
		{
			CastIterative(collider, pos, v, collisionStop);
		}
		else
		{
			CastShortDistance(collider, pos, v);
		}
		collisionConsumer.OnCollisionFinished();
		_blockData.Cleanup();
		_triggerTracker.Reset();
		_damageTracker.Reset();
		_collisionConsumer = null;
		_activeTriggers = null;
		_motion = Vector3.Zero;
	}

	protected void CastShortDistance(BoundingBox collider, Vector3 pos, Vector3 v)
	{
		_boxBlockIntersectionEvaluator.SetBox(collider, pos).OffsetPosition(v);
		collider.ForEachBlock(pos.X + v.X, pos.Y + v.Y, pos.Z + v.Z, 1E-05f, this, (int x, int y, int z, BlockCollisionProvider _this) => _this.ProcessBlockStatic(x, y, z));
		GenerateTriggerExit();
	}

	protected bool ProcessBlockStatic(int x, int y, int z)
	{
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Invalid comparison between Unknown and I4
		_blockData.Read(x, y, z);
		BlockHitbox blockBoundingBoxes = _blockData.GetBlockBoundingBoxes(_gameInstance);
		int num = _blockData.OriginX(x);
		int num2 = _blockData.OriginY(y);
		int num3 = _blockData.OriginZ(z);
		bool flag = _blockData.IsTrigger && !_triggerTracker.IsTracked(num, num2, num3);
		int num4 = _blockData.BlockDamage;
		bool flag2 = CanCollide();
		BoundingBox[] boxes = blockBoundingBoxes.Boxes;
		_boxBlockIntersectionEvaluator.SetDamageAndSubmerged(num4, isSubmerge: false);
		if ((int)_blockData.OriginalBlockType.CollisionMaterial != 2)
		{
			if (num4 != 0 && blockBoundingBoxes.IsOversized())
			{
				if (_damageTracker.IsTracked(num, num2, num3))
				{
					num4 = 0;
				}
				else
				{
					_damageTracker.TrackNew(num, num2, num3);
				}
			}
			if (flag2 && blockBoundingBoxes.IsOversized())
			{
				if (_collisionTracker.IsTracked(num, num2, num3))
				{
					flag2 = false;
				}
				else
				{
					_collisionTracker.TrackNew(num, num2, num3);
				}
			}
			int num5 = 0;
			while ((flag2 || flag || num4 > 0) && num5 < boxes.Length)
			{
				BoundingBox boundingBox = boxes[num5];
				if (!CollisionMath.IsDisjoint(_boxBlockIntersectionEvaluator.IntersectBoxComputeTouch(boundingBox, num, num2, num3)))
				{
					if (flag2 || (_boxBlockIntersectionEvaluator.Overlapping && _reportOverlaps))
					{
						_collisionConsumer.OnCollision(num, num2, num3, _motion, _boxBlockIntersectionEvaluator, _blockData, boundingBox);
						flag2 = false;
					}
					if (flag)
					{
						if (_activeTriggers.IsTracked(num, num2, num3))
						{
							_collisionConsumer.OnCollisionTrigger(num, num2, num3, _motion, _boxBlockIntersectionEvaluator, _blockData);
						}
						else
						{
							_collisionConsumer.OnCollisionTriggerEnter(num, num2, num3, _motion, _boxBlockIntersectionEvaluator, _blockData);
							_activeTriggers.TrackNew(num, num2, num3);
						}
						_triggerTracker.TrackNew(num, num2, num3);
						flag = false;
					}
					if (num4 != 0)
					{
						_collisionConsumer.OnCollisionDamage(num, num2, num3, _motion, _boxBlockIntersectionEvaluator, _blockData);
						num4 = 0;
					}
				}
				num5++;
			}
			ClientBlockType submergeFluid = _blockData.GetSubmergeFluid(_gameInstance);
			if (submergeFluid != null)
			{
				ProcessBlockStaticFluid(x, y, z, submergeFluid, submergeFluid: true);
			}
			return true;
		}
		if (flag)
		{
			_boxBlockIntersectionEvaluator.SetDamageAndSubmerged(num4, isSubmerge: false);
			BoundingBox[] array = boxes;
			foreach (BoundingBox otherBox in array)
			{
				if (!CollisionMath.IsDisjoint(_boxBlockIntersectionEvaluator.IntersectBoxComputeTouch(otherBox, num, num2, num3)))
				{
					_collisionConsumer.OnCollisionTrigger(num, num2, num3, _motion, _boxBlockIntersectionEvaluator, _blockData);
					_triggerTracker.TrackNew(num, num2, num3);
					break;
				}
			}
		}
		ProcessBlockStaticFluid(x, y, z, _blockData.BlockType, submergeFluid: false);
		return true;
	}

	protected void ProcessBlockStaticFluid(int x, int y, int z, ClientBlockType fluid, bool submergeFluid)
	{
		bool flag = false;
		bool flag2 = CanCollide(2);
		if (!(flag || flag2))
		{
			return;
		}
		_fluidBox.Max.Y = _blockData.FillHeight;
		if (!CollisionMath.IsDisjoint(_boxBlockIntersectionEvaluator.IntersectBoxComputeTouch(_fluidBox, x, y, z)))
		{
			_boxBlockIntersectionEvaluator.SetDamageAndSubmerged(0, submergeFluid);
			if (flag2)
			{
				_collisionConsumer.OnCollision(x, y, z, _motion, _boxBlockIntersectionEvaluator, _blockData, _fluidBox);
			}
			if (flag)
			{
				_collisionConsumer.OnCollisionDamage(x, y, z, _motion, _boxBlockIntersectionEvaluator, _blockData);
			}
		}
	}

	protected bool CanCollide()
	{
		return CanCollide(_blockData.CollisionMaterials);
	}

	protected bool CanCollide(int collisionMaterials)
	{
		return (collisionMaterials & RequestedCollisionMaterials) != 0;
	}

	protected void CastIterative(BoundingBox collider, Vector3 pos, Vector3 v, float collisionStop)
	{
		_relativeStopDistance = MathHelper.Clamp(collisionStop, 0f, 1f);
		_collisionState = PredictedProjectile.Result.Continue;
		_movingBoxBoxCollisionEvaluator.SetCollider(collider).SetMove(pos, v);
		collider.ForEachBlock(pos, 1E-05f, this, (int x, int y, int z, BlockCollisionProvider _this) => _this.ProcessBlockDynamic(x, y, z));
		BoxBlockIterator.Iterate(collider, pos, v, v.Length(), this);
		int count = _damageTracker.Count;
		for (int i = 0; i < count; i++)
		{
			BlockContactData contactData = _damageTracker.GetContactData(i);
			if (contactData.CollisionStart <= _relativeStopDistance)
			{
				IntVector3 position = _damageTracker.GetPosition(i);
				_collisionConsumer.OnCollisionDamage(position.X, position.Y, position.Z, _motion, contactData, _damageTracker.GetBlockData(i));
			}
		}
		GenerateTriggerExit();
		count = _triggerTracker.Count;
		for (int j = 0; j < count; j++)
		{
			BlockContactData contactData2 = _triggerTracker.GetContactData(j);
			if (contactData2.CollisionStart <= _relativeStopDistance)
			{
				IntVector3 position2 = _triggerTracker.GetPosition(j);
				int x2 = position2.X;
				int y2 = position2.Y;
				int z2 = position2.Z;
				if (_activeTriggers.IsTracked(x2, y2, z2))
				{
					_collisionConsumer.OnCollisionTrigger(x2, y2, z2, _motion, contactData2, _triggerTracker.GetBlockData(j));
					continue;
				}
				_collisionConsumer.OnCollisionTriggerEnter(x2, y2, z2, _motion, contactData2, _triggerTracker.GetBlockData(j));
				_activeTriggers.TrackNew(x2, y2, z2);
			}
		}
	}

	protected bool OnSliceFinished()
	{
		PredictedProjectile.Result result = _collisionConsumer.OnCollisionSliceFinished();
		if (_collisionState < result)
		{
			_collisionState = result;
		}
		return _collisionState == PredictedProjectile.Result.Continue;
	}

	protected bool ProcessBlockDynamic(int x, int y, int z)
	{
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Invalid comparison between Unknown and I4
		_blockData.Read(x, y, z);
		int num = _blockData.OriginX(x);
		int num2 = _blockData.OriginY(y);
		int num3 = _blockData.OriginZ(z);
		BlockHitbox blockBoundingBoxes = _blockData.GetBlockBoundingBoxes(_gameInstance);
		bool flag = CanCollide();
		int num4 = _blockData.BlockDamage;
		bool flag2 = _blockData.IsTrigger;
		_movingBoxBoxCollisionEvaluator.SetDamageAndSubmerged(num4, isSubmerge: false);
		BlockContactData blockContactData = null;
		BlockContactData blockContactData2 = null;
		if (flag2)
		{
			blockContactData = _triggerTracker.GetContactData(num, num2, num3);
			if (blockContactData != null)
			{
				flag2 = false;
			}
		}
		if (num4 != 0 && blockBoundingBoxes.IsOversized())
		{
			blockContactData2 = _damageTracker.GetContactData(num, num2, num3);
			if (blockContactData2 != null)
			{
				num4 = 0;
			}
		}
		if ((int)_blockData.OriginalBlockType.CollisionMaterial != 2)
		{
			BoundingBox[] boxes = blockBoundingBoxes.Boxes;
			foreach (BoundingBox boundingBox in boxes)
			{
				if (!_movingBoxBoxCollisionEvaluator.IsBoundingBoxColliding(boundingBox, num, num2, num3))
				{
					continue;
				}
				if (_movingBoxBoxCollisionEvaluator.CollisionStart > _relativeStopDistance)
				{
					if (_movingBoxBoxCollisionEvaluator.Overlapping && _reportOverlaps)
					{
						PredictedProjectile.Result result = _collisionConsumer.OnCollision(num, num2, num3, _motion, _movingBoxBoxCollisionEvaluator, _blockData, boundingBox);
						UpdateStopDistance(result);
					}
					continue;
				}
				if (flag || (_movingBoxBoxCollisionEvaluator.Overlapping && _reportOverlaps))
				{
					PredictedProjectile.Result result2 = _collisionConsumer.OnCollision(num, num2, num3, _motion, _movingBoxBoxCollisionEvaluator, _blockData, boundingBox);
					UpdateStopDistance(result2);
				}
				if (flag2)
				{
					blockContactData = ProcessTriggerDynamic(num, num2, num3, blockContactData);
				}
				if (num4 != 0)
				{
					blockContactData2 = ProcessDamageDynamic(num, num2, num3, blockContactData2);
				}
			}
			ClientBlockType submergeFluid = _blockData.GetSubmergeFluid(_gameInstance);
			if (submergeFluid != null)
			{
				ProcessBlockDynamicFluid(x, y, z, submergeFluid, blockContactData2, isSubmergeFluid: true);
			}
			return _collisionState != PredictedProjectile.Result.StopNow;
		}
		if (flag2)
		{
			BoundingBox[] boxes2 = blockBoundingBoxes.Boxes;
			foreach (BoundingBox blockBoundingBox in boxes2)
			{
				if (_movingBoxBoxCollisionEvaluator.IsBoundingBoxColliding(blockBoundingBox, num, num2, num3) && _movingBoxBoxCollisionEvaluator.CollisionStart <= _relativeStopDistance)
				{
					blockContactData = ProcessTriggerDynamic(num, num2, num3, blockContactData);
				}
			}
		}
		ProcessBlockDynamicFluid(x, y, z, _blockData.BlockType, blockContactData2, isSubmergeFluid: false);
		return _collisionState != PredictedProjectile.Result.StopNow;
	}

	protected void ProcessBlockDynamicFluid(int x, int y, int z, ClientBlockType fluid, BlockContactData damageCollisionData, bool isSubmergeFluid)
	{
		bool flag = false;
		bool flag2 = CanCollide(2);
		if (!(flag || flag2))
		{
			return;
		}
		_fluidBox.Max.Y = _blockData.FillHeight;
		if (_movingBoxBoxCollisionEvaluator.IsBoundingBoxColliding(_fluidBox, x, y, z) && _movingBoxBoxCollisionEvaluator.CollisionStart <= _relativeStopDistance)
		{
			_movingBoxBoxCollisionEvaluator.SetDamageAndSubmerged(0, isSubmergeFluid);
			if (flag2)
			{
				PredictedProjectile.Result result = _collisionConsumer.OnCollision(x, y, z, _motion, _movingBoxBoxCollisionEvaluator, _blockData, _fluidBox);
				UpdateStopDistance(result);
			}
			if (flag)
			{
				ProcessDamageDynamic(x, y, z, damageCollisionData);
			}
		}
	}

	protected BlockContactData ProcessTriggerDynamic(int blockX, int blockY, int blockZ, BlockContactData collisionData)
	{
		PredictedProjectile.Result result = _collisionConsumer.ProbeCollisionTrigger(blockX, blockY, blockZ, _motion, _movingBoxBoxCollisionEvaluator, _blockData);
		UpdateStopDistance(result);
		if (collisionData == null)
		{
			return _triggerTracker.TrackNew(blockX, blockY, blockZ, _movingBoxBoxCollisionEvaluator, _blockData);
		}
		float collisionEnd = System.Math.Max(collisionData.CollisionEnd, _movingBoxBoxCollisionEvaluator.CollisionEnd);
		if (_movingBoxBoxCollisionEvaluator.CollisionStart < collisionData.CollisionStart)
		{
			collisionData.Assign(_movingBoxBoxCollisionEvaluator);
		}
		collisionData.CollisionEnd = collisionEnd;
		return collisionData;
	}

	protected BlockContactData ProcessDamageDynamic(int blockX, int blockY, int blockZ, BlockContactData collisionData)
	{
		PredictedProjectile.Result result = _collisionConsumer.ProbeCollisionDamage(blockX, blockY, blockZ, _motion, _movingBoxBoxCollisionEvaluator, _blockData);
		UpdateStopDistance(result);
		if (collisionData == null)
		{
			return _damageTracker.TrackNew(blockX, blockY, blockZ, _movingBoxBoxCollisionEvaluator, _blockData);
		}
		if (_movingBoxBoxCollisionEvaluator.CollisionStart < collisionData.CollisionStart)
		{
			collisionData.Assign(_movingBoxBoxCollisionEvaluator);
		}
		return collisionData;
	}

	protected void UpdateStopDistance(PredictedProjectile.Result result)
	{
		if (result != 0)
		{
			if (_movingBoxBoxCollisionEvaluator.CollisionStart < _relativeStopDistance)
			{
				_relativeStopDistance = _movingBoxBoxCollisionEvaluator.CollisionStart;
			}
			if (result > _collisionState)
			{
				_collisionState = result;
			}
		}
	}

	protected void GenerateTriggerExit()
	{
		for (int num = _activeTriggers.Count - 1; num >= 0; num--)
		{
			IntVector3 position = _activeTriggers.GetPosition(num);
			if (!_triggerTracker.IsTracked(position.X, position.Y, position.Z))
			{
				_collisionConsumer.OnCollisionTriggerExit(position.X, position.Y, position.Z);
				_activeTriggers.Untrack(position.X, position.Y, position.Z);
			}
		}
	}
}
