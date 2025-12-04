using System;
using HytaleClient.Audio;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class PredictedProjectile : Entity, InteractionSource
{
	public enum Result
	{
		Continue,
		Stop,
		StopNow
	}

	internal enum State
	{
		Active,
		Resting,
		Inactive
	}

	public static bool DebugPrediction;

	private float _nextTickTime;

	private ProjectileConfig _config;

	private bool _justSpawned = true;

	protected const int WaterDetectionExtremaCount = 2;

	protected readonly BlockCollisionProvider _blockCollisionProvider;

	protected readonly EntityRefCollisionProvider _entityCollisionProvider;

	protected readonly BlockTracker _triggerTracker;

	protected readonly RestingSupport _restingSupport;

	protected Vector3 _velocity;

	protected Vector3 _position;

	protected Vector3 _movement;

	protected Vector3 _nextMovement = default(Vector3);

	protected bool _bounced;

	protected int _bounces = 0;

	protected bool _onGround;

	protected Vector3 _reflected;

	protected Vector3 _unreflected;

	protected bool _provideCharacterCollisions = true;

	protected readonly int _creatorUuid = -1;

	protected bool _executeTriggers;

	protected Action<GameInstance, Vector3> _bounceConsumer;

	protected Action<Entity, Vector3, Entity, string> _impactConsumer;

	protected bool _movedInsideSolid;

	protected Vector3 _moveOutOfSolidVelocity = default(Vector3);

	protected Vector3 _contactPosition;

	protected Vector3 _contactNormal;

	protected float _collisionStart;

	protected PhysicsBodyStateUpdater _stateUpdater = new PhysicsBodyStateUpdaterSymplecticEuler();

	protected PhysicsBodyState _stateBefore = new PhysicsBodyState();

	protected PhysicsBodyState _stateAfter = new PhysicsBodyState();

	protected float _displacedMass;

	protected float _subSurfaceVolume;

	protected float _enterFluid;

	protected float _leaveFluid;

	protected bool _inFluid;

	protected int _velocityExtremaCount = int.MaxValue;

	protected State _state = State.Active;

	protected ForceProviderEntity _forceProviderEntity;

	protected ForceProvider[] _forceProviders;

	protected readonly ForceProviderStandardState _forceProviderStandardState = new ForceProviderStandardState();

	protected float _dragMultiplier;

	protected float _dragOffset;

	protected readonly BlockTracker _fluidTracker = new BlockTracker();

	protected bool _isSliding;

	public bool IsSwimming => _velocityExtremaCount <= 0;

	public PredictedProjectile(GameInstance gameInstance, int networkId, int creatorUuid, ProjectileConfig projectileConfig, Vector3 initialForce)
		: base(gameInstance, networkId)
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Invalid comparison between Unknown and I4
		_config = projectileConfig;
		if ((int)_config.PhysicsConfig_.Type == 0)
		{
			_creatorUuid = creatorUuid;
			_blockCollisionProvider = new BlockCollisionProvider();
			_blockCollisionProvider.RequestedCollisionMaterials = 6;
			_blockCollisionProvider.ReportOverlaps = true;
			_entityCollisionProvider = new EntityRefCollisionProvider();
			_triggerTracker = new BlockTracker();
			_restingSupport = new RestingSupport();
			_velocity = default(Vector3);
			_position = default(Vector3);
			_movement = default(Vector3);
			_reflected = default(Vector3);
			_unreflected = default(Vector3);
			_forceProviderEntity = new ForceProviderEntity(this);
			_forceProviderEntity.Density = (float)_config.PhysicsConfig_.Density;
			_forceProviders = new ForceProvider[1] { _forceProviderEntity };
			_forceProviderStandardState.NextTickVelocity = initialForce;
			_impactConsumer = delegate(Entity this_, Vector3 hitPos, Entity target, string detailName)
			{
				//IL_000a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0017: Unknown result type (might be due to invalid IL or missing references)
				//IL_0048: Unknown result type (might be due to invalid IL or missing references)
				InteractionType type2 = (InteractionType)((target != null) ? 19 : 20);
				InteractionContext context2 = InteractionContext.ForProxy(this_, _gameInstance.InventoryModule, type2);
				Vector4 value2 = new Vector4(hitPos.X, hitPos.Y, hitPos.Z, 1f);
				_gameInstance.InteractionModule.StartChain(context2, type2, InteractionModule.ClickType.None, null, target?.NetworkId, value2, detailName);
			};
			_bounceConsumer = delegate(GameInstance instance, Vector3 hitPos)
			{
				//IL_0003: Unknown result type (might be due to invalid IL or missing references)
				//IL_0010: Unknown result type (might be due to invalid IL or missing references)
				//IL_0041: Unknown result type (might be due to invalid IL or missing references)
				InteractionType type = (InteractionType)21;
				InteractionContext context = InteractionContext.ForProxy(this, _gameInstance.InventoryModule, type);
				Vector4 value = new Vector4(hitPos.X, hitPos.Y, hitPos.Z, 1f);
				InteractionModule interactionModule = _gameInstance.InteractionModule;
				Vector4? hitPosition = value;
				interactionModule.StartChain(context, type, InteractionModule.ClickType.None, null, null, hitPosition);
			};
		}
	}

	public override void UpdateWithoutPosition(float deltaTime, float distanceToCamera, bool skipUpdateLogic = false)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		base.UpdateWithoutPosition(deltaTime, distanceToCamera, skipUpdateLogic);
		if (ServerEntity != null && ServerEntity.Disposed)
		{
			ServerEntity = null;
			Removed = true;
			_gameInstance.Engine.RunOnMainThread(this, delegate
			{
				_gameInstance.EntityStoreModule.Despawn(NetworkId);
			}, allowCallFromMainThread: true);
		}
		else
		{
			if (Removed)
			{
				return;
			}
			if (_justSpawned)
			{
				InteractionType type = (InteractionType)18;
				InteractionContext context = InteractionContext.ForProxy(this, _gameInstance.InventoryModule, type);
				_gameInstance.InteractionModule.StartChain(context, type, InteractionModule.ClickType.None, null);
				_justSpawned = false;
				if (Removed)
				{
					return;
				}
			}
			float num = 1f / (float)_gameInstance.ServerUpdatesPerSecond;
			num *= _gameInstance.TimeDilationModifier;
			_nextTickTime -= deltaTime;
			int num2 = 0;
			while (_nextTickTime < 0f)
			{
				_nextTickTime += num;
				num2++;
				PhysicsType type2 = _config.PhysicsConfig_.Type;
				PhysicsType val = type2;
				if ((int)val == 0)
				{
					StandardPhysicsTick(num);
					if (num2 > 5)
					{
						_nextTickTime = num;
						break;
					}
					continue;
				}
				throw new ArgumentOutOfRangeException();
			}
			LookOrientation = base.BodyOrientation;
		}
	}

	public static PredictedProjectile Spawn(Guid id, GameInstance gameInstance, ProjectileConfig config, PlayerEntity creator, Vector3 position, Vector3 direction)
	{
		EntityStoreModule entityStoreModule = gameInstance.EntityStoreModule;
		Vector3 vector = default(Vector3);
		Direction rotationOffset = config.RotationOffset;
		vector.Yaw = PhysicsMath.NormalizeTurnAngle(PhysicsMath.HeadingFromDirection(direction.X, direction.Z)) + rotationOffset.Yaw;
		vector.Pitch = PhysicsMath.PitchFromDirection(direction.X, direction.Y, direction.Z) + rotationOffset.Pitch;
		vector.Roll = rotationOffset.Roll;
		PhysicsMath.VectorFromAngles(vector.Yaw, vector.Pitch, ref direction);
		PredictedProjectile predictedProjectile = new PredictedProjectile(gameInstance, entityStoreModule.NextLocalEntityId(), creator.NetworkId, config, direction * (float)config.LaunchForce);
		predictedProjectile.PredictionId = id;
		predictedProjectile.Predictable = true;
		entityStoreModule.RegisterEntity(predictedProjectile);
		Vector3 calculatedOffset = GetCalculatedOffset(config, vector.Pitch, vector.Yaw);
		position += calculatedOffset;
		predictedProjectile.SetSpawnTransform(position, vector, Vector3.Zero);
		predictedProjectile.SetCharacterModel(config.Model_, null);
		predictedProjectile.UpdateLight();
		if (DebugPrediction)
		{
			predictedProjectile._topTint = new Vector3(1f, 0f, 1f);
			predictedProjectile._bottomTint = new Vector3(1f, 0f, 1f);
		}
		predictedProjectile.RecomputeDragFactors(predictedProjectile.Hitbox);
		gameInstance.AudioModule.TryRegisterSoundObject(position, vector, ref predictedProjectile.SoundObjectReference);
		uint networkWwiseId = ResourceManager.GetNetworkWwiseId(config.LaunchLocalSoundEventIndex);
		gameInstance.EntityStoreModule.QueueSoundEvent(networkWwiseId, predictedProjectile.NetworkId);
		return predictedProjectile;
	}

	private static Vector3 GetCalculatedOffset(ProjectileConfig config, float pitch, float yaw)
	{
		Vector3 value = new Vector3(config.SpawnOffset.X, config.SpawnOffset.Y, config.SpawnOffset.Z);
		Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, yaw);
		Quaternion rotation2 = Quaternion.CreateFromAxisAngle(Vector3.Right, pitch);
		Vector3.Transform(ref value, ref rotation2, out value);
		Vector3.Transform(ref value, ref rotation, out value);
		return value;
	}

	public bool TryGetInteractionId(InteractionType type, out int id)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		if (_config.Interactions.TryGetValue(type, out id))
		{
			return true;
		}
		id = int.MinValue;
		return false;
	}

	private void StandardPhysicsTick(float dt)
	{
		PhysicsConfig physicsConfig_ = _config.PhysicsConfig_;
		if (_state == State.Inactive)
		{
			_velocity = Vector3.Zero;
			return;
		}
		if (_state == State.Resting)
		{
			if (_forceProviderStandardState.ExternalForce.LengthSquared() == 0f && !_restingSupport.HasChanged(_gameInstance))
			{
				return;
			}
			_state = State.Active;
		}
		_position = _nextPosition;
		float mass = _forceProviderEntity.GetMass(base.Hitbox.GetVolume());
		_forceProviderStandardState.ConvertToForces(dt, mass);
		_forceProviderStandardState.UpdateVelocity(ref _velocity);
		if (_velocity.LengthSquared() * dt * dt >= 9.9999994E-11f || _forceProviderStandardState.ExternalForce.LengthSquared() >= 0f)
		{
			_state = State.Active;
		}
		else
		{
			_velocity = Vector3.Zero;
		}
		if (_state == State.Resting && _restingSupport.HasChanged(_gameInstance))
		{
			_state = State.Active;
		}
		_stateBefore.Position = _position;
		_stateBefore.Velocity = _velocity;
		_forceProviderEntity.ForceProviderStandardState = _forceProviderStandardState;
		_stateUpdater.Update(_stateBefore, _stateAfter, mass, dt, _onGround, _forceProviders);
		_velocity = _stateAfter.Velocity;
		_movement = _velocity * dt;
		_forceProviderStandardState.Clear();
		if (_velocity.LengthSquared() * dt * dt >= 9.9999994E-11f)
		{
			_state = State.Active;
		}
		else
		{
			_velocity = Vector3.Zero;
		}
		float num = 1f;
		if (_provideCharacterCollisions)
		{
			Entity ignore = null;
			if (_creatorUuid != -1)
			{
				ignore = _gameInstance.EntityStoreModule.GetEntity(_creatorUuid);
			}
			num = _entityCollisionProvider.ComputeNearest(_gameInstance, base.Hitbox, _position, _movement, this, ignore);
			if (num < 0f || num > 1f)
			{
				num = 1f;
			}
		}
		_bounced = false;
		_onGround = false;
		_moveOutOfSolidVelocity = Vector3.Zero;
		_movedInsideSolid = false;
		_displacedMass = 0f;
		_subSurfaceVolume = 0f;
		_enterFluid = float.MaxValue;
		_leaveFluid = float.MinValue;
		_collisionStart = num;
		_contactPosition = _position + _movement * _collisionStart;
		_contactNormal = Vector3.Zero;
		_isSliding = true;
		Vector3 vector = _position;
		_nextMovement = Vector3.Zero;
		while (_isSliding && _movement != Vector3.Zero)
		{
			_contactPosition = vector + _movement * _collisionStart;
			_isSliding = false;
			_blockCollisionProvider.Cast(_gameInstance, base.Hitbox, vector, _movement, this, _triggerTracker, num);
			_movement = _nextMovement;
			vector = _contactPosition;
		}
		_movement = vector + _nextMovement - _position;
		_fluidTracker.Reset();
		float density = ((_displacedMass > 0f) ? (_displacedMass / _subSurfaceVolume) : 1.2f);
		if (_movedInsideSolid)
		{
			_position += _moveOutOfSolidVelocity * dt;
			_velocity = _moveOutOfSolidVelocity;
			_forceProviderStandardState.DragCoefficient = GetDragCoefficient(density);
			_forceProviderStandardState.DisplacedMass = _displacedMass;
			_forceProviderStandardState.Gravity = (float)physicsConfig_.Gravity;
			FinishTick();
			return;
		}
		float num2 = (_bounced ? _collisionStart : 1f);
		bool flag = false;
		if (!_inFluid && _enterFluid < _collisionStart)
		{
			_inFluid = true;
			num2 = _enterFluid;
			_velocityExtremaCount = 2;
			flag = true;
		}
		else if (_inFluid && _leaveFluid < _collisionStart)
		{
			_inFluid = false;
			num2 = _leaveFluid;
			_velocityExtremaCount = 2;
		}
		if (num2 > 0f && (double)num2 < 1.0)
		{
			_stateUpdater.Update(_stateBefore, _stateAfter, mass, dt * num2, _onGround, _forceProviders);
			_velocity = _stateAfter.Velocity;
		}
		if (_inFluid && _subSurfaceVolume < base.Hitbox.GetVolume() && _velocityExtremaCount > 0)
		{
			float y = _stateBefore.Velocity.Y;
			float y2 = _stateAfter.Velocity.Y;
			if (y * y2 <= 0f)
			{
				_velocityExtremaCount--;
			}
		}
		if (IsSwimming)
		{
			_forceProviderStandardState.ExternalForce.Y -= _stateAfter.Velocity.Y * ((float)physicsConfig_.SwimmingDampingFactor / mass);
		}
		if (flag)
		{
			_forceProviderStandardState.ExternalImpulse += _stateAfter.Velocity * (float)(0.0 - physicsConfig_.HitWaterImpulseLoss) * mass;
		}
		_forceProviderStandardState.DisplacedMass = _displacedMass;
		_forceProviderStandardState.DragCoefficient = GetDragCoefficient(density);
		_forceProviderStandardState.Gravity = (float)physicsConfig_.Gravity;
		if (_entityCollisionProvider.Count > 0)
		{
			EntityContactData contact = _entityCollisionProvider.GetContact(0);
			Entity entityReference = contact.EntityReference;
			if (entityReference != null)
			{
				_position = contact.CollisionPoint;
				_state = State.Inactive;
				if (_impactConsumer != null)
				{
					_impactConsumer(this, _position, entityReference, contact.CollisionDetailName);
				}
			}
			RotateBody(dt);
			FinishTick();
		}
		else if (_bounced)
		{
			_position = _contactPosition;
			_bounces++;
			ComputeReflectedVector(_velocity, _contactNormal, out _velocity);
			if (physicsConfig_.BounceCount == -1 || _bounces <= physicsConfig_.BounceCount)
			{
				_velocity *= (float)physicsConfig_.Bounciness;
			}
			if ((physicsConfig_.BounceCount != -1 && _bounces > physicsConfig_.BounceCount) || (double)(_velocity.LengthSquared() * dt * dt) < physicsConfig_.BounceLimit * physicsConfig_.BounceLimit)
			{
				bool flag2 = _contactNormal == Vector3.Up;
				if (!physicsConfig_.AllowRolling && (physicsConfig_.SticksVertically || flag2))
				{
					_state = State.Resting;
					_restingSupport.Rest(_gameInstance, base.Hitbox, _position);
					_onGround = flag2;
					if (_impactConsumer != null)
					{
						_impactConsumer(this, _position, null, null);
					}
				}
				if (physicsConfig_.AllowRolling)
				{
					_velocity.Y = 0f;
					_velocity *= (float)physicsConfig_.RollingFrictionFactor;
					_onGround = flag2;
				}
				else
				{
					_velocity = Vector3.Zero;
				}
			}
			else if (_bounceConsumer != null)
			{
				_bounceConsumer(_gameInstance, _position);
			}
			RotateBody(dt);
			FinishTick();
		}
		else
		{
			_position += _movement;
			RotateBody(dt);
			FinishTick();
		}
	}

	public Result OnCollision(int blockX, int blockY, int blockZ, Vector3 direction, BlockContactData contactData, BlockData blockData, BoundingBox collider)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Invalid comparison between Unknown and I4
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Invalid comparison between Unknown and I4
		PhysicsConfig physicsConfig_ = _config.PhysicsConfig_;
		Material collisionMaterial = blockData.OriginalBlockType.CollisionMaterial;
		if (physicsConfig_.MoveOutOfSolidSpeed > 0.0 && contactData.Overlapping && (int)collisionMaterial == 1)
		{
			IntVector3? intVector = NearestBlockUtil.FindNearestBlock(_position, delegate(IntVector3 block, MapModule w)
			{
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0035: Invalid comparison between Unknown and I4
				int block2 = w.GetBlock(block.X, block.Y, block.Z, int.MaxValue);
				return block2 != int.MaxValue && (int)w.ClientBlockTypes[block2].CollisionMaterial != 1;
			}, _gameInstance.MapModule);
			Vector3 vector;
			if (intVector.HasValue)
			{
				IntVector3 value = intVector.Value;
				vector = new Vector3(value.X, value.Y, value.Z);
				vector += new Vector3(0.5f, 0.5f, 0.5f);
				vector = (vector - _position).SetLength((float)physicsConfig_.MoveOutOfSolidSpeed);
			}
			else
			{
				vector = new Vector3(0f, (float)physicsConfig_.MoveOutOfSolidSpeed, 0f);
			}
			_moveOutOfSolidVelocity += vector;
			_movedInsideSolid = true;
			return Result.Continue;
		}
		if ((int)collisionMaterial == 2 && !_fluidTracker.IsTracked(blockX, blockY, blockZ))
		{
			float collisionStart = contactData.CollisionStart;
			float collisionEnd = contactData.CollisionEnd;
			if (collisionStart < _enterFluid)
			{
				_enterFluid = collisionStart;
			}
			if (collisionEnd > _leaveFluid)
			{
				_leaveFluid = collisionEnd;
			}
			if (collisionEnd <= collisionStart)
			{
				return Result.Continue;
			}
			float num = 1000f;
			float num2 = PhysicsMath.VolumeOfIntersection(base.Hitbox, _contactPosition, collider, blockX, blockY, blockZ);
			_subSurfaceVolume += num2;
			_displacedMass += num2 * num;
			_fluidTracker.TrackNew(blockX, blockY, blockZ);
			return Result.Continue;
		}
		if (contactData.Overlapping)
		{
			return Result.Continue;
		}
		float num3 = Vector3.Dot(direction, contactData.CollisionNormal);
		if ((int)collisionMaterial != 1 || (double)num3 == 0.0)
		{
		}
		if (num3 >= 0f)
		{
			return Result.Continue;
		}
		_contactPosition = contactData.CollisionPoint;
		_contactNormal = contactData.CollisionNormal;
		if (physicsConfig_.AllowRolling)
		{
			Vector3 vector2 = _stateBefore.Position + _movement - _contactPosition;
			if (vector2 != Vector3.Zero)
			{
				float num4 = Vector3.Dot(vector2, _contactNormal);
				_nextMovement = vector2;
				_nextMovement += _contactNormal * (0f - num4);
				_isSliding = true;
			}
		}
		_collisionStart = contactData.CollisionStart;
		_bounced = true;
		return Result.Stop;
	}

	public Result ProbeCollisionDamage(int blockX, int blockY, int blockZ, Vector3 direction, BlockContactData collisionData, BlockData blockData)
	{
		return Result.Continue;
	}

	public void OnCollisionDamage(int blockX, int blockY, int blockZ, Vector3 direction, BlockContactData collisionData, BlockData blockData)
	{
	}

	public Result ProbeCollisionTrigger(int blockX, int blockY, int blockZ, Vector3 direction, BlockContactData collisionData, BlockData blockData)
	{
		return Result.Continue;
	}

	public void OnCollisionTriggerEnter(int blockX, int blockY, int blockZ, Vector3 direction, BlockContactData collisionData, BlockData blockData)
	{
		if (!_executeTriggers)
		{
		}
	}

	public void OnCollisionTrigger(int blockX, int blockY, int blockZ, Vector3 direction, BlockContactData collisionData, BlockData blockData)
	{
		if (!_executeTriggers)
		{
		}
	}

	public void OnCollisionTriggerExit(int blockX, int blockY, int blockZ)
	{
		if (!_executeTriggers)
		{
		}
	}

	public Result OnCollisionSliceFinished()
	{
		return Result.Continue;
	}

	public void OnCollisionFinished()
	{
	}

	protected void FinishTick()
	{
		SetPosition(_position);
		_entityCollisionProvider.Clear();
	}

	protected void RotateBody(float dt)
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected I4, but got Unknown
		PhysicsConfig physicsConfig_ = _config.PhysicsConfig_;
		if (!physicsConfig_.ComputeYaw && !physicsConfig_.ComputePitch)
		{
			return;
		}
		float x = _stateAfter.Velocity.X;
		float z = _stateAfter.Velocity.Z;
		if (x * x + z * z <= 9.9999994E-11f)
		{
			return;
		}
		Vector3 nextBodyOrientation = _nextBodyOrientation;
		RotationMode rotationMode_ = physicsConfig_.RotationMode_;
		RotationMode val = rotationMode_;
		switch ((int)val)
		{
		case 1:
			if (physicsConfig_.ComputeYaw)
			{
				nextBodyOrientation.Yaw = PhysicsMath.NormalizeTurnAngle(PhysicsMath.HeadingFromDirection(x, z));
			}
			if (physicsConfig_.ComputePitch)
			{
				nextBodyOrientation.Pitch = PhysicsMath.PitchFromDirection(x, _stateAfter.Velocity.Y, z);
			}
			break;
		case 2:
			if (physicsConfig_.ComputeYaw)
			{
				nextBodyOrientation.Yaw = PhysicsMath.NormalizeTurnAngle(PhysicsMath.HeadingFromDirection(x, z));
			}
			if (physicsConfig_.ComputePitch)
			{
				float pitch = nextBodyOrientation.Pitch;
				float num = PhysicsMath.PitchFromDirection(x, _velocity.Y, z);
				float num2 = PhysicsMath.NormalizeTurnAngle(num - pitch);
				float num3 = _velocity.LengthSquared() * dt * physicsConfig_.SpeedRotationFactor;
				if (num2 > num3)
				{
					num = pitch + num3;
					num2 = num3;
				}
				else if (num2 < 0f - num3)
				{
					num = pitch - num3;
					num2 = num3;
				}
				nextBodyOrientation.Pitch = num;
				_forceProviderStandardState.ExternalForce += _stateAfter.Velocity * (num2 * (0f - (float)physicsConfig_.RotationForce));
			}
			break;
		case 3:
			nextBodyOrientation.Yaw = PhysicsMath.NormalizeTurnAngle(PhysicsMath.HeadingFromDirection(x, z));
			nextBodyOrientation.Pitch -= _stateBefore.Velocity.Length() * physicsConfig_.RollingSpeed;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case 0:
			break;
		}
		SetBodyOrientation(nextBodyOrientation);
	}

	public static void ComputeReflectedVector(Vector3 vec, Vector3 normal, out Vector3 result)
	{
		result = vec;
		float num = normal.LengthSquared();
		if (num != 0f)
		{
			float num2 = Vector3.Dot(vec, normal) / num;
			result += normal * (-2f * num2);
		}
	}

	protected float GetDragCoefficient(float density)
	{
		return _dragMultiplier * density + _dragOffset;
	}

	protected void RecomputeDragFactors(BoundingBox boundingBox)
	{
		PhysicsConfig physicsConfig_ = _config.PhysicsConfig_;
		Vector3 size = boundingBox.GetSize();
		float area = size.X * size.Z;
		float mass = _forceProviderEntity.GetMass(boundingBox.GetVolume());
		float num = PhysicsMath.ComputeDragCoefficient((float)physicsConfig_.TerminalVelocityAir, area, mass, (float)physicsConfig_.Gravity);
		float num2 = PhysicsMath.ComputeDragCoefficient((float)physicsConfig_.TerminalVelocityWater, area, mass, (float)physicsConfig_.Gravity);
		_dragMultiplier = (num2 - num) / (float)(physicsConfig_.DensityWater - physicsConfig_.DensityAir);
		_dragOffset = num - _dragMultiplier * (float)physicsConfig_.DensityAir;
	}
}
