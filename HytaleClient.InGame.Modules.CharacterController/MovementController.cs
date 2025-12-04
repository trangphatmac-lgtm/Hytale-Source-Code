using System;
using System.Collections.Generic;
using HytaleClient.Core;
using HytaleClient.Data.Entities;
using HytaleClient.Data.Map;
using HytaleClient.Data.UserSettings;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.CharacterController;

internal abstract class MovementController
{
	public enum DebugMovement
	{
		None,
		RunCycle,
		RunCycleJump
	}

	protected class AppliedVelocity
	{
		public Vector3 Velocity;

		public readonly VelocityConfig Config;

		public bool CanClear;

		public AppliedVelocity(Vector3 velocity, VelocityConfig config)
		{
			Velocity = velocity;
			Config = config;
		}
	}

	protected delegate bool StatePredicate(ref StateFrame frame);

	protected struct StateFrame
	{
		public bool Valid;

		public InputFrame Input;

		public Vector3 Position;

		public Vector3 Velocity;

		public Vector3 MovementForceRotation;

		public ClientMovementStates MovementStates;

		public StateFrame(bool valid)
		{
			Valid = valid;
			Input = new InputFrame(valid);
			Position = default(Vector3);
			Velocity = default(Vector3);
			MovementForceRotation = default(Vector3);
			MovementStates = default(ClientMovementStates);
		}
	}

	protected struct InputFrame
	{
		private Dictionary<InputBinding, bool> BindingDown;

		public InputFrame(bool valid)
		{
			BindingDown = new Dictionary<InputBinding, bool>();
		}

		public void Capture(Input input, InputBinding binding)
		{
			BindingDown[binding] = input.IsBindingHeld(binding);
		}

		public bool IsBindingHeld(InputBinding binding)
		{
			return BindingDown[binding];
		}
	}

	public static DebugMovement DebugMovementMode;

	protected const int CollisionCheckRadius = 2;

	protected const float CollisionPadding = 0.0001f;

	protected const float VelocityEpsilon = 1E-07f;

	public float SpeedMultiplier = 1f;

	public bool MovementEnabled;

	public float RunningKnockbackRemainingTime;

	public float CurrentSpeedMultiplierDiff;

	public float SprintForceDurationLeft = -1f;

	public float SprintForceProgress;

	public float RaycastDistance;

	public float RaycastHeightOffset;

	public RaycastMode RaycastMode = (RaycastMode)0;

	public Vector3 LastMoveForce;

	public Vector3 MovementOffset = Vector3.Zero;

	public Vector3 PreviousMovementOffset = Vector3.Zero;

	public ClientMovementStates MovementStates = ClientMovementStates.Idle;

	public readonly HashSet<int> CollidedEntities = new HashSet<int>();

	public readonly Vector3 EntityHitboxExpand = new Vector3(0.125f, 0f, 0.125f);

	public Vector3 FirstPersonPositionOffset;

	public Vector3 FirstPersonRotationOffset;

	public Vector3 ThirdPersonPositionOffset;

	public Vector3 ThirdPersonRotationOffset;

	public Vector3 MantleCameraOffset = Vector3.Zero;

	public Vector3 CameraRotation;

	protected GameInstance _gameInstance;

	protected Vector3 _velocity;

	protected Vector2 _wishDirection;

	protected readonly FlyCameraController _flyCameraController;

	protected Vector3 _movementForceRotation;

	protected float _firstPersonCameraLerpTime;

	protected float _firstPersonCurrentCameraLerpTime;

	protected Vector3 _firstPersonPositionOffsetLast = Vector3.Zero;

	protected Vector3 _firstPersonPositionOffsetTarget = Vector3.Zero;

	protected Vector3 _firstPersonRotationOffsetLast = Vector3.Zero;

	protected Vector3 _firstPersonRotationOffsetTarget = Vector3.Zero;

	protected float _thirdPersonCameraLerpTime;

	protected float _thirdPersonCurrentCameraLerpTime;

	protected Vector3 _thirdPersonPositionOffsetLast = Vector3.Zero;

	protected Vector3 _thirdPersonPositionOffsetTarget = Vector3.Zero;

	protected Vector3 _thirdPersonRotationOffsetLast = Vector3.Zero;

	protected Vector3 _thirdPersonRotationOffsetTarget = Vector3.Zero;

	protected readonly List<AppliedVelocity> _appliedVelocities = new List<AppliedVelocity>();

	protected bool _collisionForward;

	protected bool _collisionBackward;

	protected bool _collisionLeft;

	protected bool _collisionRight;

	protected const int MaxResolveSteps = 100;

	private const float MaxStateFrameHistoryS = 0.3f;

	protected StateFrame[] _stateFrames = new StateFrame[(int)System.Math.Ceiling(18.0)];

	protected int _stateFrameOffset;

	public float DefaultBlockDrag { get; set; } = 0.82f;


	public float DefaultBlockFriction { get; set; } = 0.18f;


	public float AutoJumpHeightShift { get; protected set; }

	public float CrouchHeightShift { get; protected set; }

	public bool SkipHitDetectionWhenFlying { get; protected set; }

	public bool ApplyMarioFallForce { get; set; }

	public Vector2 WishDirection { get; protected set; }

	public Vector3 Velocity => _velocity;

	public MovementSettings MovementSettings { get; set; } = new MovementSettings();


	protected MovementController(GameInstance gameInstance)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		_gameInstance = gameInstance;
		_flyCameraController = new FlyCameraController(gameInstance);
	}

	public abstract void Tick();

	public abstract void PreUpdate(float timeFraction);

	public abstract void ApplyKnockback(ApplyKnockback packet);

	public abstract void ApplyMovementOffset(Vector3 movementOffset);

	public abstract void RequestVelocityChange(float x, float y, float z, ChangeVelocityType changeType, VelocityConfig config);

	public abstract void VelocityChange(float x, float y, float z, ChangeVelocityType changeType, VelocityConfig config);

	public abstract Vector2 GetWishDirection();

	public void SetSavedMovementStates(SavedMovementStates movementStates)
	{
		MovementStates.IsFlying = movementStates.Flying;
	}

	public void SetFirstPersonCameraOffset(float time, Vector3 position, Vector3 rotation)
	{
		_firstPersonCameraLerpTime = time;
		_firstPersonCurrentCameraLerpTime = 0f;
		_firstPersonPositionOffsetLast = _firstPersonPositionOffsetTarget;
		_firstPersonPositionOffsetTarget = position;
		_firstPersonRotationOffsetLast = _firstPersonRotationOffsetTarget;
		_firstPersonRotationOffsetTarget = rotation;
	}

	public void SetThirdPersonCameraOffset(float time, Vector3 position, Vector3 rotation)
	{
		_thirdPersonCameraLerpTime = time;
		_thirdPersonCurrentCameraLerpTime = 0f;
		_thirdPersonPositionOffsetLast = _thirdPersonPositionOffsetTarget;
		_thirdPersonPositionOffsetTarget = position;
		_thirdPersonRotationOffsetLast = _thirdPersonRotationOffsetTarget;
		_thirdPersonRotationOffsetTarget = rotation;
	}

	public void UpdateMovementSettings(MovementSettings movementSettings)
	{
		MovementSettings = movementSettings;
		if (MovementSettings.BaseSpeed == 0f)
		{
			_velocity.X = 0f;
			_velocity.Z = 0f;
		}
		if (MovementSettings.JumpForce == 0f)
		{
			_velocity.Y = 0f;
		}
	}

	protected void UpdateInputSettings()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		if ((int)_gameInstance.GameMode == 1)
		{
			if (_gameInstance.Input.ConsumeBinding(inputBindings.DecreaseSpeedMultiplier))
			{
				float num = ((SpeedMultiplier >= 2f) ? 1f : 0.1f);
				SpeedMultiplier = (float)System.Math.Round(System.Math.Max(MovementSettings.MinSpeedMultiplier, SpeedMultiplier - num) * 10f) * 0.1f;
				_gameInstance.Chat.Log($"Speed Multiplier: {SpeedMultiplier}");
			}
			if (_gameInstance.Input.ConsumeBinding(inputBindings.IncreaseSpeedMultiplier))
			{
				float num2 = ((SpeedMultiplier >= 1f) ? 1f : 0.1f);
				SpeedMultiplier = (float)System.Math.Round(System.Math.Min(MovementSettings.MaxSpeedMultiplier, SpeedMultiplier + num2) * 10f) * 0.1f;
				_gameInstance.Chat.Log($"Speed Multiplier: {SpeedMultiplier}");
			}
			if (_gameInstance.Input.ConsumeBinding(inputBindings.ToggleCreativeCollision))
			{
				if (!MovementStates.IsFlying || !SkipHitDetectionWhenFlying)
				{
					MovementStates.IsFlying = true;
					SkipHitDetectionWhenFlying = true;
					_gameInstance.Chat.Log("Collision Disabled");
				}
				else
				{
					SkipHitDetectionWhenFlying = false;
					_gameInstance.Chat.Log("Collision Enabled");
				}
			}
		}
		if (_gameInstance.Input.ConsumeBinding(inputBindings.ToggleFlyCamera))
		{
			if (_gameInstance.Input.IsShiftHeld())
			{
				_flyCameraController.ResetPosition();
			}
			else if (_gameInstance.CameraModule.Controller == _flyCameraController)
			{
				_gameInstance.CameraModule.ResetCameraController();
				_gameInstance.Chat.Log("Fly Camera Disabled");
			}
			else
			{
				_gameInstance.CameraModule.SetCustomCameraController(_flyCameraController);
				_gameInstance.Chat.Log("Fly Camera Enabled");
			}
		}
		if (_gameInstance.CameraModule.Controller == _flyCameraController && _gameInstance.Input.ConsumeBinding(inputBindings.ToggleFlyCameraControlTarget))
		{
			_flyCameraController.ToggleControlTarget();
		}
	}

	protected void UpdateCameraSettings()
	{
		if (_firstPersonCurrentCameraLerpTime < _firstPersonCameraLerpTime)
		{
			_firstPersonCurrentCameraLerpTime += 1f / 60f;
		}
		if (_firstPersonCurrentCameraLerpTime > _firstPersonCameraLerpTime)
		{
			_firstPersonCurrentCameraLerpTime = _firstPersonCameraLerpTime;
		}
		if (_thirdPersonCurrentCameraLerpTime < _thirdPersonCameraLerpTime)
		{
			_thirdPersonCurrentCameraLerpTime += 1f / 60f;
		}
		if (_thirdPersonCurrentCameraLerpTime > _thirdPersonCameraLerpTime)
		{
			_thirdPersonCurrentCameraLerpTime = _thirdPersonCameraLerpTime;
		}
		if (_firstPersonCurrentCameraLerpTime == _firstPersonCameraLerpTime)
		{
			FirstPersonPositionOffset = _firstPersonPositionOffsetTarget;
		}
		else
		{
			FirstPersonPositionOffset = Vector3.Lerp(_firstPersonPositionOffsetLast, _firstPersonPositionOffsetTarget, _firstPersonCurrentCameraLerpTime / _firstPersonCameraLerpTime);
		}
		if (_firstPersonCurrentCameraLerpTime == _firstPersonCameraLerpTime)
		{
			FirstPersonRotationOffset = _firstPersonRotationOffsetTarget;
		}
		else
		{
			FirstPersonRotationOffset = Vector3.LerpAngle(_firstPersonRotationOffsetLast, _firstPersonRotationOffsetTarget, _firstPersonCurrentCameraLerpTime / _firstPersonCameraLerpTime);
		}
		if (_thirdPersonCurrentCameraLerpTime == _thirdPersonCameraLerpTime)
		{
			ThirdPersonPositionOffset = _thirdPersonPositionOffsetTarget;
		}
		else
		{
			ThirdPersonPositionOffset = Vector3.Lerp(_thirdPersonPositionOffsetLast, _thirdPersonPositionOffsetTarget, _thirdPersonCurrentCameraLerpTime / _thirdPersonCameraLerpTime);
		}
		if (_thirdPersonCurrentCameraLerpTime == _thirdPersonCameraLerpTime)
		{
			ThirdPersonRotationOffset = _thirdPersonRotationOffsetTarget;
		}
		else
		{
			ThirdPersonRotationOffset = Vector3.LerpAngle(_thirdPersonRotationOffsetLast, _thirdPersonRotationOffsetTarget, _thirdPersonCurrentCameraLerpTime / _thirdPersonCameraLerpTime);
		}
	}

	protected void ComputeWishDirection(bool forceMoveForward, bool canMove, InputFrame input, InputBindings inputBindings)
	{
		if (forceMoveForward || (canMove && input.IsBindingHeld(inputBindings.MoveForwards) && !input.IsBindingHeld(inputBindings.MoveBackwards)))
		{
			_wishDirection.Y = MathHelper.Step(_wishDirection.Y, 1f, MovementSettings.WishDirectionWeightY);
		}
		else if (canMove && input.IsBindingHeld(inputBindings.MoveBackwards) && !input.IsBindingHeld(inputBindings.MoveForwards))
		{
			_wishDirection.Y = MathHelper.Step(_wishDirection.Y, -1f, MovementSettings.WishDirectionWeightY);
		}
		else
		{
			_wishDirection.Y = MathHelper.Step(_wishDirection.Y, 0f, MovementSettings.WishDirectionGravityY);
		}
		if (canMove && input.IsBindingHeld(inputBindings.StrafeRight) && !input.IsBindingHeld(inputBindings.StrafeLeft))
		{
			_wishDirection.X = MathHelper.Step(_wishDirection.X, 1f, MovementSettings.WishDirectionWeightX);
		}
		else if (canMove && input.IsBindingHeld(inputBindings.StrafeLeft) && !input.IsBindingHeld(inputBindings.StrafeRight))
		{
			_wishDirection.X = MathHelper.Step(_wishDirection.X, -1f, MovementSettings.WishDirectionWeightX);
		}
		else
		{
			_wishDirection.X = MathHelper.Step(_wishDirection.X, 0f, MovementSettings.WishDirectionGravityX);
		}
		ComputeEntityEffectWishDirection();
	}

	protected void ComputeEntityEffectWishDirection()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		PlayerEntity localPlayer = _gameInstance.LocalPlayer;
		if (localPlayer != null && (int)_gameInstance.GameMode != 1)
		{
			if (_wishDirection.Y > 0f && localPlayer.DisableForward)
			{
				_wishDirection.Y = 0f;
			}
			else if (_wishDirection.Y < 0f && localPlayer.DisableBackward)
			{
				_wishDirection.Y = 0f;
			}
			if (_wishDirection.X > 0f && localPlayer.DisableRight)
			{
				_wishDirection.X = 0f;
			}
			else if (_wishDirection.X < 0f && localPlayer.DisableLeft)
			{
				_wishDirection.X = 0f;
			}
		}
	}

	protected bool CheckCollision(Vector3 position, Vector3 moveOffset, BoundingBox boundingBox, HitDetection.CollisionAxis axis, out HitDetection.CollisionHitData hitData)
	{
		boundingBox.Translate(new Vector3(position.X, position.Y + 0.0001f, position.Z));
		int num = (int)position.X;
		int num2 = (int)position.Y;
		int num3 = (int)position.Z;
		hitData = default(HitDetection.CollisionHitData);
		float num4 = 0f;
		for (int i = -1; i <= 2; i++)
		{
			int num5 = num2 + i;
			for (int j = -2; j <= 2; j++)
			{
				int num6 = num3 + j;
				for (int k = -2; k <= 2; k++)
				{
					int num7 = num + k;
					if (_gameInstance.HitDetection.CheckBlockCollision(boundingBox, new Vector3(num7, num5, num6), moveOffset, out var hitData2))
					{
						float num8 = 0f;
						switch (axis)
						{
						case HitDetection.CollisionAxis.X:
							num8 = hitData2.Overlap.X;
							break;
						case HitDetection.CollisionAxis.Y:
							num8 = hitData2.Overlap.Y;
							break;
						case HitDetection.CollisionAxis.Z:
							num8 = hitData2.Overlap.Z;
							break;
						}
						if (num4 == 0f || num8 > num4)
						{
							hitData = hitData2;
							num4 = num8;
						}
					}
				}
			}
		}
		EntityStoreModule entityStoreModule = _gameInstance.EntityStoreModule;
		Entity[] allEntities = entityStoreModule.GetAllEntities();
		for (int l = entityStoreModule.PlayerEntityLocalId + 1; l < entityStoreModule.GetEntitiesCount(); l++)
		{
			Entity entity = allEntities[l];
			if (entity.HitboxCollisionConfigIndex == -1 || !entity.IsTangible())
			{
				continue;
			}
			BoundingBox hitbox = entity.Hitbox;
			hitbox.Grow(EntityHitboxExpand * 2f);
			ClientHitboxCollisionConfig clientHitboxCollisionConfig = _gameInstance.ServerSettings.HitboxCollisionConfigs[entity.HitboxCollisionConfigIndex];
			if (clientHitboxCollisionConfig.CollisionType != 0)
			{
				continue;
			}
			Vector3 position2 = entity.Position;
			if (HitDetection.CheckBoxCollision(boundingBox, hitbox, position2.X, position2.Y + 0.0001f, position2.Z, moveOffset, out var hitData3))
			{
				float num9 = 0f;
				switch (axis)
				{
				case HitDetection.CollisionAxis.X:
					num9 = hitData3.Overlap.X;
					break;
				case HitDetection.CollisionAxis.Y:
					num9 = hitData3.Overlap.Y;
					break;
				case HitDetection.CollisionAxis.Z:
					num9 = hitData3.Overlap.Z;
					break;
				}
				if (num4 == 0f || num9 > num4)
				{
					hitData = hitData3;
					hitData.HitEntity = entity.NetworkId;
					num4 = num9;
				}
				if (_gameInstance.EntityStoreModule.DebugInfoNeedsDrawing)
				{
					CollidedEntities.Add(entity.NetworkId);
				}
			}
		}
		return num4 > 0f;
	}

	protected bool IsPositionGap(Vector3 pos)
	{
		return IsPositionGap(pos.X, pos.Y, pos.Z);
	}

	protected bool IsPositionGap(float posX, float posY, float posZ)
	{
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Invalid comparison between Unknown and I4
		int num = (int)System.Math.Floor(posX);
		int num2 = (int)System.Math.Floor(posY);
		int num3 = (int)System.Math.Floor(posZ);
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
				return false;
			}
		}
		return true;
	}

	protected ref StateFrame CaptureStateFrame(Input input, InputBindings bindings)
	{
		if (!_stateFrames[_stateFrameOffset].Valid)
		{
			_stateFrames[_stateFrameOffset] = new StateFrame(valid: true);
		}
		ref StateFrame reference = ref _stateFrames[_stateFrameOffset];
		_stateFrameOffset = (_stateFrameOffset + 1) % _stateFrames.Length;
		reference.Input.Capture(input, bindings.Jump);
		reference.Input.Capture(input, bindings.MoveForwards);
		reference.Input.Capture(input, bindings.MoveBackwards);
		reference.Input.Capture(input, bindings.StrafeLeft);
		reference.Input.Capture(input, bindings.StrafeRight);
		reference.Input.Capture(input, bindings.Sprint);
		reference.Input.Capture(input, bindings.Walk);
		reference.Input.Capture(input, bindings.Crouch);
		reference.Input.Capture(input, bindings.FlyDown);
		reference.Input.Capture(input, bindings.FlyUp);
		reference.Velocity = _velocity;
		reference.Position = _gameInstance.LocalPlayer.Position;
		reference.MovementForceRotation = _gameInstance.CameraModule.Controller.MovementForceRotation;
		reference.MovementStates = MovementStates;
		return ref reference;
	}

	public void InvalidateState()
	{
		for (int i = 0; i < _stateFrames.Length; i++)
		{
			_stateFrames[i].Valid = false;
		}
		_stateFrameOffset = 0;
	}

	protected int? FindMatchingStateFrame(StatePredicate matcher)
	{
		int num = _stateFrameOffset - 1;
		for (num = (_stateFrames.Length + num) % _stateFrames.Length; num != _stateFrameOffset; num = (_stateFrames.Length + num) % _stateFrames.Length)
		{
			ref StateFrame reference = ref _stateFrames[num];
			if (!reference.Valid)
			{
				break;
			}
			if (matcher(ref reference))
			{
				return num;
			}
			num--;
		}
		return null;
	}

	protected int FindNearestStateFrame(Vector3 position, float ping)
	{
		int num = _stateFrameOffset - 1;
		num = (_stateFrames.Length + num) % _stateFrames.Length;
		int? num2 = null;
		float num3 = float.MaxValue;
		while (num != _stateFrameOffset)
		{
			ref StateFrame reference = ref _stateFrames[num];
			if (!reference.Valid)
			{
				break;
			}
			float num4 = (reference.Position - position).LengthSquared();
			if (num4 < 0.001f)
			{
				return num;
			}
			if (num4 < num3)
			{
				num3 = num4;
				num2 = num;
			}
			num--;
			num = (_stateFrames.Length + num) % _stateFrames.Length;
		}
		if (num2.HasValue && num3 < 0.25f)
		{
			return num2.Value;
		}
		num = _stateFrameOffset - (int)System.Math.Ceiling(ping / (1f / 60f));
		for (num = (_stateFrames.Length + num % _stateFrames.Length) % _stateFrames.Length; num != _stateFrameOffset; num %= _stateFrames.Length)
		{
			if (_stateFrames[num].Valid)
			{
				return num;
			}
			num++;
		}
		return num;
	}
}
