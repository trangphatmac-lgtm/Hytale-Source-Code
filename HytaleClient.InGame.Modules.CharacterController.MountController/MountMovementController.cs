using System;
using Hypixel.ProtoPlus;
using HytaleClient.Core;
using HytaleClient.Data.ClientInteraction;
using HytaleClient.Data.Entities;
using HytaleClient.Data.Entities.Initializers;
using HytaleClient.Data.EntityStats;
using HytaleClient.Data.Map;
using HytaleClient.Data.UserSettings;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules.CharacterController.MountController;

internal class MountMovementController : MovementController
{
	private const float AutoJumpMaxHeight = 0.625f;

	private const int DoubleJumpMaxDelay = 300;

	private const float MaxCycleMovement = 0.25f;

	private const int MaxJumpCombos = 3;

	private const float MaxJumpComboFactor = 0.5f;

	private const int SurfaceCheckPadding = 3;

	private const float BaseForwardWalkSpeedMultiplier = 0.3f;

	private const float BaseBackwardWalkSpeedMultiplier = 0.3f;

	private const float BaseStrafeWalkSpeedMultiplier = 0.3f;

	private const float BaseForwardRunSpeedMultiplier = 1f;

	private const float BaseBackwardRunSpeedMultiplier = 0.65f;

	private const float BaseStrafeRunSpeedMultiplier = 0.8f;

	private const float BaseForwardCrouchSpeedMultiplier = 0.55f;

	private const float BaseBackwardCrouchSpeedMultiplier = 0.4f;

	private const float BaseStrafeCrouchSpeedMultiplier = 0.45f;

	private const float BaseForwardSprintSpeedMultiplier = 1.65f;

	private static readonly HitDetection.RaycastOptions FallRaycastOptions = new HitDetection.RaycastOptions
	{
		Distance = 1f,
		IgnoreEmptyCollisionMaterial = true
	};

	public const string Id = "Mount";

	public int MountEntityId;

	public ClientMovementStates MountMovementStates = ClientMovementStates.Idle;

	private readonly FluidFXMovementSettings _averageFluidMovementSettings;

	private Vector3 _anchor;

	private bool _wasOnGround;

	private bool _wasFalling;

	private bool _fluidJump;

	private float _swimJumpLastY;

	private int _jumpCombo;

	private float _acceleration;

	private float _lastLateralSpeed;

	private int _autoJumpFrame;

	private int _autoJumpFrameCount;

	private float _autoJumpHeight;

	private float _previousAutoJumpHeightShift;

	private float _nextAutoJumpHeightShift;

	private float _jumpObstacleDurationLeft;

	private Vector3 _requestedVelocity;

	private ChangeVelocityType? _requestedVelocityChangeType;

	private HitDetection.RaycastHit _groundHit;

	private long _jumpReleaseTime;

	private bool _wasHoldingJump;

	private bool _canStartRunning;

	private float _jumpBufferDurationLeft;

	private float _jumpInputVelocity;

	private bool _jumpInputConsumed = true;

	private bool _jumpInputReleased = true;

	private float _sprintForceInitialSpeed;

	private ClientMovementStates _previousMovementStates = ClientMovementStates.Idle;

	private float _runForceDurationLeft;

	private float _runForceInitialSpeed;

	private float _runForceProgress;

	private bool _runForward;

	private bool _wasIntendingToRun;

	private bool _isDecelerating;

	private bool _needToReleaseInput;

	public MountMovementController(GameInstance gameInstance)
		: base(gameInstance)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		_averageFluidMovementSettings = new FluidFXMovementSettings();
		MovementEnabled = true;
	}

	public override void Tick()
	{
		Input input = _gameInstance.Input;
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		_previousMovementStates = MountMovementStates;
		StateFrame currentFrame = CaptureStateFrame(input, inputBindings);
		DoTick(ref currentFrame);
	}

	public override void PreUpdate(float timeFraction)
	{
		PlayerEntity localPlayer = _gameInstance.LocalPlayer;
		localPlayer.UpdateClientInterpolation(timeFraction);
		_gameInstance.EntityStoreModule.GetEntity(MountEntityId)?.UpdateInterpolation(timeFraction);
	}

	public override void ApplyKnockback(ApplyKnockback packet)
	{
	}

	public override void ApplyMovementOffset(Vector3 movementOffset)
	{
		ICameraController controller = _gameInstance.CameraModule.Controller;
		if ((MountMovementStates.IsFlying && base.SkipHitDetectionWhenFlying) || controller.SkipCharacterPhysics)
		{
			MoveEntities(_gameInstance.LocalPlayer.Position - _anchor + movementOffset);
			MountMovementStates.IsOnGround = (_wasOnGround = false);
		}
		else
		{
			_wasOnGround = MountMovementStates.IsOnGround;
			int num = (int)System.Math.Ceiling(movementOffset.Length() / 0.25f);
			Vector3 offset = movementOffset / num;
			for (int i = 0; i < num; i++)
			{
				DoMoveCycle(offset);
			}
			if (num == 0)
			{
				MoveEntities(_gameInstance.LocalPlayer.Position - _anchor);
			}
			if (MountMovementStates.IsOnGround)
			{
				_fluidJump = false;
				_velocity.Y = 0f;
			}
		}
		_wasFalling = MountMovementStates.IsFalling;
		if (controller.CanMove)
		{
			UpdateViewModifiers();
		}
		UpdateMovementStates();
	}

	public override void RequestVelocityChange(float x, float y, float z, ChangeVelocityType changeType, VelocityConfig config)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Invalid comparison between Unknown and I4
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Invalid comparison between Unknown and I4
		if (_gameInstance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)1) && config != null)
		{
			if ((int)changeType == 1)
			{
				_appliedVelocities.Clear();
			}
			_appliedVelocities.Add(new AppliedVelocity(new Vector3(x, y, z), config));
			return;
		}
		if (!_requestedVelocityChangeType.HasValue || _requestedVelocityChangeType == (ChangeVelocityType?)0)
		{
			_requestedVelocityChangeType = changeType;
		}
		if ((int)changeType == 0)
		{
			_requestedVelocity.X += x;
			_requestedVelocity.Y += y;
			_requestedVelocity.Z += z;
		}
		else if ((int)changeType == 1)
		{
			_requestedVelocity.X = x;
			_requestedVelocity.Y = y;
			_requestedVelocity.Z = z;
		}
	}

	public override void VelocityChange(float x, float y, float z, ChangeVelocityType changeType, VelocityConfig config)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Invalid comparison between Unknown and I4
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Invalid comparison between Unknown and I4
		if (_gameInstance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)1) && config != null)
		{
			if ((int)changeType == 1)
			{
				_appliedVelocities.Clear();
				_velocity = Vector3.Zero;
			}
			_appliedVelocities.Add(new AppliedVelocity(new Vector3(x, y, z), config));
		}
		else if ((int)changeType == 0)
		{
			_velocity.X += x;
			_velocity.Y += y;
			_velocity.Z += z;
		}
		else if ((int)changeType == 1)
		{
			_velocity.X = x;
			_velocity.Y = y;
			_velocity.Z = z;
		}
	}

	public override Vector2 GetWishDirection()
	{
		return _wishDirection;
	}

	public void OnMount(MountNPC packet)
	{
		_anchor.X = packet.AnchorX;
		_anchor.Y = packet.AnchorY;
		_anchor.Z = packet.AnchorZ;
		MountEntityId = packet.EntityId;
		_gameInstance.LocalPlayer.IsMounting = true;
		MovementStates.IsMounting = true;
	}

	public void OnDismount(bool isLocalInteraction = false)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		_anchor = Vector3.Zero;
		_gameInstance.LocalPlayer.IsMounting = false;
		MovementStates.IsMounting = false;
		if (isLocalInteraction)
		{
			_gameInstance.Connection.SendPacket((ProtoPacket)new DismountNPC());
		}
	}

	protected new ref StateFrame CaptureStateFrame(Input input, InputBindings bindings)
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
		reference.Position = _gameInstance.LocalPlayer.Position - _anchor;
		reference.MovementForceRotation = _gameInstance.CameraModule.Controller.MovementForceRotation;
		reference.MovementStates = MountMovementStates;
		return ref reference;
	}

	private void DoTick(ref StateFrame currentFrame)
	{
		_movementForceRotation = currentFrame.MovementForceRotation;
		ref InputFrame input = ref currentFrame.Input;
		UpdateInputSettings();
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		ICameraController controller = _gameInstance.CameraModule.Controller;
		bool flag = input.IsBindingHeld(inputBindings.MoveForwards);
		bool flag2 = input.IsBindingHeld(inputBindings.MoveBackwards);
		if (_needToReleaseInput)
		{
			if (!flag && !flag2)
			{
				_needToReleaseInput = false;
			}
			if (_runForward && flag && !flag2)
			{
				_needToReleaseInput = false;
			}
			if (!_runForward && flag2 && !flag)
			{
				_needToReleaseInput = false;
			}
		}
		bool flag3 = controller.CanMove && MovementEnabled;
		if (_gameInstance.App.Settings.MountRequireNewInput && _needToReleaseInput)
		{
			flag3 = false;
		}
		bool canJump = controller.CanMove && MovementEnabled;
		bool skipCharacterPhysics = controller.SkipCharacterPhysics;
		UpdateCameraSettings();
		CheckDoubleJumpToFly(flag3, input, inputBindings);
		HandleJumpBuffer(input, inputBindings);
		CheckOutOfStaminaSprint();
		if (_jumpObstacleDurationLeft > 0f)
		{
			_jumpObstacleDurationLeft -= 1f / 60f;
		}
		bool flag4 = input.IsBindingHeld(inputBindings.Sprint);
		bool canRun = flag3 && flag && !flag2 && _canStartRunning && flag4 && (MountMovementStates.IsFlying || !input.IsBindingHeld(inputBindings.Crouch)) && _runForceDurationLeft <= 0f;
		if (!flag4)
		{
			_canStartRunning = true;
		}
		Entity entity = _gameInstance.EntityStoreModule.GetEntity(MountEntityId);
		if (entity != null)
		{
			UpdateFluidData(entity);
			UpdateRunForceDuration(flag2, flag, flag4);
			ApplyMovementStateLogic(input, inputBindings, flag3, canRun, skipCharacterPhysics, entity, canJump);
			if (SprintForceDurationLeft > 0f)
			{
				SprintForceDurationLeft -= 1f / 60f;
			}
			ComputeWishDirection(flag3, currentFrame.Input, inputBindings);
			ComputeTurnRate(currentFrame.Input, inputBindings);
			base.WishDirection = _wishDirection;
			ComputeMoveForce();
			ApplyRequestedVelocityVector();
			if (System.Math.Abs(_velocity.X) <= 1E-07f)
			{
				_velocity.X = 0f;
			}
			if (System.Math.Abs(_velocity.Y) <= 1E-07f)
			{
				_velocity.Y = 0f;
			}
			if (System.Math.Abs(_velocity.Z) <= 1E-07f)
			{
				_velocity.Z = 0f;
			}
			Vector3 velocity = _velocity;
			PreviousMovementOffset = MovementOffset;
			MovementOffset = velocity;
			controller.ApplyMove(velocity * (1f / 60f));
			_gameInstance.App.Interface.InGameView.OnCharacterControllerTicked(MountMovementStates);
		}
	}

	private void ApplyRequestedVelocityVector()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Invalid comparison between Unknown and I4
		if (!MountMovementStates.IsFlying && _requestedVelocityChangeType.HasValue)
		{
			if (_requestedVelocityChangeType == (ChangeVelocityType?)0)
			{
				_velocity.X += _requestedVelocity.X * (1f - base.DefaultBlockDrag) * base.MovementSettings.VelocityResistance;
				_velocity.Y += _requestedVelocity.Y;
				_velocity.Z += _requestedVelocity.Z * (1f - base.DefaultBlockDrag) * base.MovementSettings.VelocityResistance;
			}
			else if ((int)_requestedVelocityChangeType.GetValueOrDefault() == 1)
			{
				_velocity.X = _requestedVelocity.X;
				_velocity.Y = _requestedVelocity.Y;
				_velocity.Z = _requestedVelocity.Z;
			}
		}
		_requestedVelocity.X = (_requestedVelocity.Y = (_requestedVelocity.Z = 0f));
		_requestedVelocityChangeType = null;
	}

	private void CheckDoubleJumpToFly(bool canMove, InputFrame input, InputBindings inputBindings)
	{
		if (canMove && !_wasHoldingJump && input.IsBindingHeld(inputBindings.Jump) && base.MovementSettings.CanFly)
		{
			long epochMilliseconds = TimeHelper.GetEpochMilliseconds();
			long num = epochMilliseconds - _jumpReleaseTime;
			if (num < 300)
			{
				MountMovementStates.IsFlying = !MountMovementStates.IsFlying;
				_jumpReleaseTime = -1L;
			}
			else
			{
				_jumpReleaseTime = epochMilliseconds;
			}
		}
	}

	private void HandleJumpBuffer(InputFrame input, InputBindings inputBindings)
	{
		if (_jumpBufferDurationLeft > 0f)
		{
			_jumpBufferDurationLeft -= 1f / 60f;
		}
		bool flag = input.IsBindingHeld(inputBindings.Jump);
		if (!_wasHoldingJump && flag && (!base.MovementSettings.AutoJumpDisableJumping || (base.MovementSettings.AutoJumpDisableJumping && _autoJumpFrame <= 0)))
		{
			_jumpBufferDurationLeft = base.MovementSettings.JumpBufferDuration;
			_jumpInputVelocity = _velocity.Y;
			_jumpInputConsumed = false;
			_jumpInputReleased = false;
		}
		else if (_wasHoldingJump && !flag)
		{
			_jumpInputReleased = true;
		}
		_wasHoldingJump = flag;
	}

	private void CheckOutOfStaminaSprint()
	{
		if (MountMovementStates.IsSprinting && _gameInstance.LocalPlayer.GetEntityStat(DefaultEntityStats.Stamina).Value <= 0f)
		{
			MountMovementStates.IsSprinting = false;
			_canStartRunning = false;
		}
	}

	private void UpdateFluidData(Entity mountEntity)
	{
		int hitboxHeight = GetHitboxHeight(mountEntity);
		int num = 0;
		for (int i = 0; i < hitboxHeight; i++)
		{
			if (GetRelativeFluid(mountEntity.Position.X, mountEntity.Position.Y + (float)i, mountEntity.Position.Z, out var blockTypeOut))
			{
				if (num == 0)
				{
					_averageFluidMovementSettings.SwimUpSpeed = 0f;
					_averageFluidMovementSettings.SwimDownSpeed = 0f;
					_averageFluidMovementSettings.SinkSpeed = 0f;
					_averageFluidMovementSettings.HorizontalSpeedMultiplier = 0f;
					_averageFluidMovementSettings.FieldOfViewMultiplier = 0f;
					_averageFluidMovementSettings.EntryVelocityMultiplier = 0f;
				}
				num++;
				FluidFX val = _gameInstance.ServerSettings.FluidFXs[blockTypeOut.FluidFXIndex];
				if (val.MovementSettings != null)
				{
					FluidFXMovementSettings averageFluidMovementSettings = _averageFluidMovementSettings;
					averageFluidMovementSettings.SwimUpSpeed += val.MovementSettings.SwimUpSpeed;
					FluidFXMovementSettings averageFluidMovementSettings2 = _averageFluidMovementSettings;
					averageFluidMovementSettings2.SwimDownSpeed += val.MovementSettings.SwimDownSpeed;
					FluidFXMovementSettings averageFluidMovementSettings3 = _averageFluidMovementSettings;
					averageFluidMovementSettings3.SinkSpeed += val.MovementSettings.SinkSpeed;
					FluidFXMovementSettings averageFluidMovementSettings4 = _averageFluidMovementSettings;
					averageFluidMovementSettings4.HorizontalSpeedMultiplier += val.MovementSettings.HorizontalSpeedMultiplier;
					FluidFXMovementSettings averageFluidMovementSettings5 = _averageFluidMovementSettings;
					averageFluidMovementSettings5.FieldOfViewMultiplier += val.MovementSettings.FieldOfViewMultiplier;
					FluidFXMovementSettings averageFluidMovementSettings6 = _averageFluidMovementSettings;
					averageFluidMovementSettings6.EntryVelocityMultiplier += val.MovementSettings.EntryVelocityMultiplier;
				}
			}
		}
		MountMovementStates.IsInFluid = num > 0;
		MountMovementStates.IsSwimming = num == hitboxHeight;
		if (num > 1)
		{
			FluidFXMovementSettings averageFluidMovementSettings7 = _averageFluidMovementSettings;
			averageFluidMovementSettings7.SwimUpSpeed /= (float)num;
			FluidFXMovementSettings averageFluidMovementSettings8 = _averageFluidMovementSettings;
			averageFluidMovementSettings8.SwimDownSpeed /= (float)num;
			FluidFXMovementSettings averageFluidMovementSettings9 = _averageFluidMovementSettings;
			averageFluidMovementSettings9.SinkSpeed /= (float)num;
			FluidFXMovementSettings averageFluidMovementSettings10 = _averageFluidMovementSettings;
			averageFluidMovementSettings10.HorizontalSpeedMultiplier /= (float)num;
			FluidFXMovementSettings averageFluidMovementSettings11 = _averageFluidMovementSettings;
			averageFluidMovementSettings11.FieldOfViewMultiplier /= (float)num;
			FluidFXMovementSettings averageFluidMovementSettings12 = _averageFluidMovementSettings;
			averageFluidMovementSettings12.EntryVelocityMultiplier /= (float)num;
		}
		if (MountMovementStates.IsSwimJumping)
		{
			if (mountEntity.Position.Y <= _swimJumpLastY)
			{
				MountMovementStates.IsSwimJumping = false;
			}
			_swimJumpLastY = mountEntity.Position.Y;
		}
	}

	private int GetHitboxHeight(Entity entity)
	{
		return (int)System.Math.Max(System.Math.Ceiling(entity.Hitbox.GetSize().Y), 1.0);
	}

	private bool GetRelativeFluid(float posX, float posY, float posZ, out ClientBlockType blockTypeOut)
	{
		int worldX = (int)System.Math.Floor(posX);
		int worldY = (int)System.Math.Floor(posY);
		int worldZ = (int)System.Math.Floor(posZ);
		int block = _gameInstance.MapModule.GetBlock(worldX, worldY, worldZ, 0);
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
		if (clientBlockType.FluidFXIndex != 0)
		{
			blockTypeOut = clientBlockType;
			return true;
		}
		blockTypeOut = null;
		return false;
	}

	private void UpdateRunForceDuration(bool isBackwardsHeld, bool isForwardsHeld, bool isSprintingHeld)
	{
		if (_runForceDurationLeft <= 0f)
		{
			return;
		}
		Settings settings = _gameInstance.App.Settings;
		float num = 1f / 60f;
		if ((_runForward && isBackwardsHeld) || (!_runForward && isForwardsHeld))
		{
			num *= settings.MountForcedDecelerationMultiplier;
		}
		else if (_runForward && isForwardsHeld && isSprintingHeld)
		{
			num *= settings.MountForcedAccelerationMultiplier;
		}
		_runForceDurationLeft -= num;
		if (!(_runForceDurationLeft > 0f))
		{
			if (_isDecelerating && (isForwardsHeld || isBackwardsHeld))
			{
				_needToReleaseInput = true;
			}
			_isDecelerating = false;
		}
	}

	protected void ComputeWishDirection(bool canMove, InputFrame input, InputBindings inputBindings)
	{
		if (canMove && input.IsBindingHeld(inputBindings.MoveForwards) && !input.IsBindingHeld(inputBindings.MoveBackwards))
		{
			_wishDirection.Y = MathHelper.Step(_wishDirection.Y, 1f, base.MovementSettings.WishDirectionWeightY);
		}
		else if (canMove && input.IsBindingHeld(inputBindings.MoveBackwards) && !input.IsBindingHeld(inputBindings.MoveForwards))
		{
			_wishDirection.Y = MathHelper.Step(_wishDirection.Y, -1f, base.MovementSettings.WishDirectionWeightY);
		}
		else
		{
			_wishDirection.Y = MathHelper.Step(_wishDirection.Y, 0f, base.MovementSettings.WishDirectionGravityY);
		}
	}

	private void ApplyMovementStateLogic(InputFrame input, InputBindings inputBindings, bool canMove, bool canRun, bool skipCharacterPhysics, Entity mountEntity, bool canJump)
	{
		if (MountMovementStates.IsFlying || skipCharacterPhysics)
		{
			ApplyFlyingMovementStateLogic(input, inputBindings, canMove, canRun);
		}
		else if (MountMovementStates.IsSwimming)
		{
			ApplySwimmingMovementStateLogic(input, inputBindings, canRun, canJump, mountEntity);
		}
		else
		{
			ApplyDefaultMovementStateLogic(input, inputBindings, canMove, canRun, canJump);
		}
	}

	private void ApplyFlyingMovementStateLogic(InputFrame input, InputBindings inputBindings, bool canMove, bool canRun)
	{
		MountMovementStates.IsFalling = false;
		MountMovementStates.IsSprinting = canRun;
		MountMovementStates.IsWalking = canMove && input.IsBindingHeld(inputBindings.Walk);
		MountMovementStates.IsJumping = canMove && input.IsBindingHeld(inputBindings.Jump);
		_fluidJump = false;
		if (canMove && input.IsBindingHeld(inputBindings.FlyDown))
		{
			_velocity.Y = (0f - base.MovementSettings.VerticalFlySpeed) * SpeedMultiplier * _gameInstance.CameraModule.Controller.SpeedModifier;
		}
		else if (canMove && input.IsBindingHeld(inputBindings.FlyUp))
		{
			_velocity.Y = base.MovementSettings.VerticalFlySpeed * SpeedMultiplier * _gameInstance.CameraModule.Controller.SpeedModifier;
			MountMovementStates.IsOnGround = false;
		}
		else
		{
			_velocity.Y = 0f;
		}
	}

	private void ApplySwimmingMovementStateLogic(InputFrame input, InputBindings inputBindings, bool canRun, bool canJump, Entity mountEntity)
	{
		MountMovementStates.IsSprinting = canRun;
		MountMovementStates.IsJumping = canJump && input.IsBindingHeld(inputBindings.Jump);
		if (!MountMovementStates.IsSwimJumping)
		{
			_velocity.Y = 0f;
			if (MountMovementStates.IsJumping)
			{
				_velocity.Y = _averageFluidMovementSettings.SwimUpSpeed;
			}
			else if (MountMovementStates.IsCrouching)
			{
				_velocity.Y = _averageFluidMovementSettings.SwimDownSpeed;
			}
			float num = -1f;
			for (int i = 0; i <= GetHitboxHeight(mountEntity) + 3; i++)
			{
				if (!GetRelativeFluid(mountEntity.Position.X, mountEntity.Position.Y + (float)i, mountEntity.Position.Z, out var _))
				{
					num = (int)System.Math.Floor(mountEntity.Position.Y + (float)i);
					if (GetRelativeFluid(mountEntity.Position.X, mountEntity.Position.Y + (float)i - 1f, mountEntity.Position.Z, out var blockTypeOut2) && blockTypeOut2.VerticalFill != blockTypeOut2.MaxFillLevel)
					{
						num -= 1f - (float)(int)blockTypeOut2.VerticalFill / (float)(int)blockTypeOut2.MaxFillLevel;
					}
					break;
				}
			}
			float y = mountEntity.Position.Y;
			int hitboxHeight = GetHitboxHeight(mountEntity);
			float num2 = num - (float)hitboxHeight * 0.7f;
			if (num != -1f && y > num2)
			{
				float num3 = (num2 - y) * 10f;
				if (System.Math.Abs(num3) > 0.1f)
				{
					_velocity.Y = num3;
				}
			}
			if (_velocity.Y == 0f && (num == -1f || y < num - (float)hitboxHeight))
			{
				_velocity.Y = _averageFluidMovementSettings.SinkSpeed;
			}
			float num4 = y + _velocity.Y * (1f / 60f);
			float num5 = (float)System.Math.Ceiling((float)hitboxHeight * 0.5f);
			if (num != -1f && num4 > y && num4 >= num - num5)
			{
				_velocity.Y = num - num5 - y;
			}
			if (MountMovementStates.IsJumping && _velocity.Y >= 0f && System.Math.Abs(y - num2) < 0.2f)
			{
				MountMovementStates.IsSwimJumping = true;
				_velocity.Y = base.MovementSettings.SwimJumpForce;
				_swimJumpLastY = y;
			}
		}
		_fluidJump = MountMovementStates.IsSwimJumping;
		MountMovementStates.IsFalling = _velocity.Y < 0f && !MountMovementStates.IsCrouching;
	}

	private void ApplyDefaultMovementStateLogic(InputFrame input, InputBindings inputBindings, bool canMove, bool canRun, bool canJump)
	{
		UpdateRunForceValues(input, inputBindings, canRun);
		MountMovementStates.IsSprinting = canRun && (MountMovementStates.IsSprinting || MountMovementStates.IsOnGround) && _runForceDurationLeft <= 0f;
		MountMovementStates.IsWalking = canMove && !MountMovementStates.IsSprinting && input.IsBindingHeld(inputBindings.Walk) && MountMovementStates.IsOnGround;
		if (_gameInstance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)3) && _gameInstance.App.Settings.SprintForce)
		{
			UpdateSprintForceValues();
		}
		ApplyGravity();
		if (base.ApplyMarioFallForce)
		{
			if (_velocity.Y > 0f && !input.IsBindingHeld(inputBindings.Jump))
			{
				_velocity.Y -= base.MovementSettings.MarioJumpFallForce * (1f / 60f);
			}
			else if (_velocity.Y <= 0f)
			{
				base.ApplyMarioFallForce = false;
			}
		}
		if (canJump && MountMovementStates.IsOnGround)
		{
			MountMovementStates.IsJumping = HasJumpInputQueued() && _wasOnGround;
			if (base.MovementSettings.AutoJumpDisableJumping && _autoJumpFrame > 0)
			{
				MountMovementStates.IsJumping = false;
			}
			MountMovementStates.IsFalling = false;
			if (MountMovementStates.IsJumping)
			{
				_velocity.Y = ComputeJumpForce();
				MountMovementStates.IsOnGround = false;
				_fluidJump = MountMovementStates.IsInFluid;
				_jumpCombo = (int)MathHelper.Min(_jumpCombo + 1, 3f);
				_jumpBufferDurationLeft = 0f;
				_jumpInputConsumed = true;
				base.ApplyMarioFallForce = true;
			}
			else if (_gameInstance.App.Settings.AutoJumpGap && MountMovementStates.IsSprinting && _wishDirection.Y != 0f && IsGapAhead())
			{
				_velocity.Y = ComputeJumpForce();
				MountMovementStates.IsOnGround = false;
				_fluidJump = MountMovementStates.IsInFluid;
				_jumpCombo = (int)MathHelper.Min(_jumpCombo + 1, 3f);
				_jumpBufferDurationLeft = 0f;
				_jumpInputConsumed = true;
				base.ApplyMarioFallForce = false;
			}
		}
		if (_jumpCombo != 0 && ((MountMovementStates.IsOnGround && _wasOnGround) || System.Math.Abs(_velocity.X) <= 1E-07f || System.Math.Abs(_velocity.Z) <= 1E-07f))
		{
			_jumpCombo = 0;
		}
		MountMovementStates.IsSprinting &= !MountMovementStates.IsCrouching;
	}

	private void UpdateRunForceValues(InputFrame input, InputBindings inputBindings, bool canRun)
	{
		if ((MountMovementStates.IsSprinting && canRun) || MountMovementStates.IsWalking)
		{
			return;
		}
		Settings settings = _gameInstance.App.Settings;
		if (settings.MountRequireNewInput && _needToReleaseInput)
		{
			return;
		}
		bool flag = input.IsBindingHeld(inputBindings.MoveForwards);
		bool flag2 = input.IsBindingHeld(inputBindings.MoveBackwards);
		bool flag3 = flag ^ flag2;
		if (flag3 && _isDecelerating)
		{
			if (_runForward && flag2)
			{
				flag3 = false;
			}
			if (!_runForward && flag)
			{
				flag3 = false;
			}
		}
		if (MountMovementStates.IsSprinting && !canRun)
		{
			flag3 = false;
		}
		if (flag3 == _wasIntendingToRun)
		{
			_wasIntendingToRun = flag3;
			return;
		}
		_runForceInitialSpeed = (float)System.Math.Sqrt((double)(_velocity.X * _velocity.X) + (double)(_velocity.Z * _velocity.Z));
		_wasIntendingToRun = flag3;
		if (flag3)
		{
			_runForward = flag;
			_runForceDurationLeft = (_runForward ? settings.MountForwardsAccelerationDuration : settings.MountBackwardsAccelerationDuration);
			_isDecelerating = false;
		}
		else
		{
			_runForceDurationLeft = (_runForward ? settings.MountForwardsDecelerationDuration : settings.MountBackwardsDecelerationDuration);
			_isDecelerating = true;
		}
		if (_runForceDurationLeft <= 0f)
		{
			_isDecelerating = false;
		}
	}

	private void UpdateSprintForceValues()
	{
		if (MountMovementStates.IsSprinting == _previousMovementStates.IsSprinting && MountMovementStates.IsInFluid == _previousMovementStates.IsInFluid)
		{
			return;
		}
		if (MountMovementStates.IsIdle && !_previousMovementStates.IsIdle)
		{
			SprintForceDurationLeft = -1f;
			return;
		}
		_sprintForceInitialSpeed = (_previousMovementStates.IsIdle ? 0f : _lastLateralSpeed);
		if (MountMovementStates.IsSprinting)
		{
			if (_sprintForceInitialSpeed < base.MovementSettings.BaseSpeed)
			{
				_sprintForceInitialSpeed = base.MovementSettings.BaseSpeed;
			}
			SprintForceDurationLeft = _gameInstance.App.Settings.SprintAccelerationDuration;
		}
		else
		{
			SprintForceDurationLeft = _gameInstance.App.Settings.SprintDecelerationDuration;
		}
		if (SprintForceDurationLeft <= 0f)
		{
			SprintForceDurationLeft = -1f;
		}
	}

	private bool HasJumpInputQueued()
	{
		if (!_jumpInputConsumed && !_jumpInputReleased)
		{
			return true;
		}
		if (_jumpBufferDurationLeft > 0f && _jumpInputVelocity <= base.MovementSettings.JumpBufferMaxYVelocity)
		{
			return true;
		}
		return false;
	}

	private float ComputeJumpForce()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		float jumpForce = base.MovementSettings.JumpForce;
		if ((int)_gameInstance.GameMode != 1 || SpeedMultiplier <= 1f)
		{
			return jumpForce;
		}
		return jumpForce + System.Math.Min((SpeedMultiplier - 1f) * _gameInstance.App.Settings.JumpForceSpeedMultiplierStep, _gameInstance.App.Settings.MaxJumpForceSpeedMultiplier);
	}

	private void ApplyGravity()
	{
		Entity entity = _gameInstance.EntityStoreModule.GetEntity(MountEntityId);
		BoundingBox hitbox = entity.Hitbox;
		int num = (base.MovementSettings.InvertedGravity ? 1 : (-1));
		float num2 = (float)num * PhysicsMath.GetTerminalVelocity(base.MovementSettings.Mass, 0.001225f, System.Math.Abs((hitbox.Max.X - hitbox.Min.X) * (hitbox.Max.Z - hitbox.Min.Z)), base.MovementSettings.DragCoefficient);
		float num3 = (float)num * PhysicsMath.GetAcceleration(_velocity.Y, num2) * (1f / 60f);
		if (_velocity.Y < num2 && num3 > 0f)
		{
			_velocity.Y = System.Math.Min(_velocity.Y + num3, num2);
		}
		else if (_velocity.Y > num2 && num3 < 0f)
		{
			_velocity.Y = System.Math.Max(_velocity.Y + num3, num2);
		}
	}

	private bool IsGapAhead()
	{
		Vector3 position = _gameInstance.LocalPlayer.Position;
		Vector3 vector = new Vector3(position.X, (float)System.Math.Floor(position.Y) - 1f, position.Z);
		Vector3 vector2 = new Vector3(_velocity.X, 0f, _velocity.Z);
		vector2.Normalize();
		if (!IsPositionGap(vector))
		{
			return false;
		}
		if (System.Math.Abs(vector2.X) > System.Math.Abs(vector2.Z))
		{
			if (vector2.X > 0f && System.Math.Abs(position.X) % 1f < 0.2f)
			{
				return false;
			}
			if (vector2.X < 0f && System.Math.Abs(position.X) % 1f > 0.8f)
			{
				return false;
			}
		}
		else
		{
			if (vector2.Z > 0f && System.Math.Abs(position.Z) % 1f > 0.8f)
			{
				return false;
			}
			if (vector2.Z < 0f && System.Math.Abs(position.Z) % 1f < 0.2f)
			{
				return false;
			}
		}
		if (!IsPositionGap(vector + vector2))
		{
			return false;
		}
		if (!IsPositionGap(vector + new Vector3(0f, -1f, 0f)))
		{
			return false;
		}
		return true;
	}

	private void ComputeTurnRate(InputFrame input, InputBindings inputBindings)
	{
		float num = 0f;
		if (input.IsBindingHeld(inputBindings.StrafeLeft) && !input.IsBindingHeld(inputBindings.StrafeRight))
		{
			num = 1f;
		}
		else if (input.IsBindingHeld(inputBindings.StrafeRight) && !input.IsBindingHeld(inputBindings.StrafeLeft))
		{
			num = -1f;
		}
		Settings settings = _gameInstance.App.Settings;
		float num2 = MathHelper.ConvertToNewRange(_lastLateralSpeed, settings.MountSpeedMinTurnRate, settings.MountSpeedMaxTurnRate, settings.MountMinTurnRate, settings.MountMaxTurnRate);
		CameraRotation.Y += MathHelper.ToRadians(num * num2 * (1f / 60f));
	}

	private void ComputeMoveForce()
	{
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Invalid comparison between Unknown and I4
		LastMoveForce = Vector3.Zero;
		float num = 0f;
		float num2 = 1f;
		float num3 = 1f;
		float value = (float)System.Math.Sqrt(_velocity.X * _velocity.X + _velocity.Z * _velocity.Z);
		if (!MountMovementStates.IsFlying && !MountMovementStates.IsClimbing)
		{
			num = ((!MountMovementStates.IsOnGround && !MountMovementStates.IsSwimming && MountMovementStates.IsFalling) ? MathHelper.ConvertToNewRange(value, base.MovementSettings.AirDragMinSpeed, base.MovementSettings.AirDragMaxSpeed, base.MovementSettings.AirDragMin, base.MovementSettings.AirDragMax) : base.DefaultBlockDrag);
			num2 = ((!MountMovementStates.IsOnGround && !MountMovementStates.IsSwimming && MountMovementStates.IsFalling) ? MathHelper.ConvertToNewRange(value, base.MovementSettings.AirFrictionMinSpeed, base.MovementSettings.AirFrictionMaxSpeed, base.MovementSettings.AirFrictionMax, base.MovementSettings.AirFrictionMin) : (1f - num));
			num3 = base.MovementSettings.Acceleration;
		}
		_velocity.X *= num;
		_velocity.Z *= num;
		Vector3 movementForceRotation = _movementForceRotation;
		Quaternion rotation = Quaternion.CreateFromYawPitchRoll(movementForceRotation.Y, _gameInstance.CameraModule.Controller.AllowPitchControls ? movementForceRotation.X : 0f, 0f);
		Vector3 vector = Vector3.Transform(Vector3.Forward, rotation);
		Vector3 vector2 = vector * (_isDecelerating ? ((float)(_runForward ? 1 : (-1))) : _wishDirection.Y);
		if (vector2.LengthSquared() < 0.0001f)
		{
			_acceleration *= num3;
			return;
		}
		vector2.Normalize();
		float num4 = (((int)_gameInstance.GameMode == 1) ? (SpeedMultiplier * _gameInstance.CameraModule.Controller.SpeedModifier) : 1f);
		if (!MountMovementStates.IsOnGround && !MountMovementStates.IsSwimming)
		{
			num4 += MathHelper.ConvertToNewRange(value, base.MovementSettings.AirControlMinSpeed, base.MovementSettings.AirControlMaxSpeed, base.MovementSettings.AirControlMaxMultiplier, base.MovementSettings.AirControlMinMultiplier);
		}
		float wishSpeed = GetHorizontalMoveSpeed() * num4;
		ComputeRunForce(ref wishSpeed);
		if (_gameInstance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)3) && _gameInstance.App.Settings.SprintForce)
		{
			ComputeSprintForce(ref wishSpeed);
		}
		float num5 = Vector3.Dot(_velocity, vector2);
		if (!MountMovementStates.IsOnGround)
		{
			Vector3 vector3 = vector * _wishDirection.Y;
			vector3.Normalize();
			float num6 = Vector3.Dot(_velocity, vector3);
			if (num6 > num5)
			{
				num5 = num6;
			}
		}
		float num7 = wishSpeed - num5;
		if (!(num7 <= 0f))
		{
			float num8 = wishSpeed * num2;
			if (_jumpObstacleDurationLeft > 0f)
			{
				num8 *= 1f - (MountMovementStates.IsSprinting ? base.MovementSettings.AutoJumpObstacleSprintSpeedLoss : base.MovementSettings.AutoJumpObstacleSpeedLoss);
			}
			if (num8 > num7)
			{
				num8 = num7;
			}
			_acceleration += ((base.MovementSettings.BaseSpeed != 0f) ? (num8 * (wishSpeed / base.MovementSettings.BaseSpeed * num3)) : 0f);
			if (_acceleration > num8)
			{
				_acceleration = num8;
			}
			vector2.X *= _acceleration;
			vector2.Y *= GetVerticalMoveSpeed() * num4;
			vector2.Z *= _acceleration;
			LastMoveForce = vector2;
			_velocity += vector2;
			_lastLateralSpeed = (float)System.Math.Sqrt((double)(_velocity.X * _velocity.X) + (double)(_velocity.Z * _velocity.Z));
		}
	}

	private void ComputeRunForce(ref float wishSpeed)
	{
		if (_runForceDurationLeft <= 0f || MountMovementStates.IsWalking || MountMovementStates.IsSprinting)
		{
			return;
		}
		Settings settings = _gameInstance.App.Settings;
		Easing.EasingType easingType;
		float num;
		if (_wasIntendingToRun)
		{
			easingType = (_runForward ? settings.MountForwardsAccelerationEasingType : settings.MountBackwardsAccelerationEasingType);
			num = (_runForward ? settings.MountForwardsAccelerationDuration : settings.MountBackwardsAccelerationDuration);
		}
		else
		{
			easingType = (_runForward ? settings.MountForwardsDecelerationEasingType : settings.MountBackwardsDecelerationEasingType);
			num = (_runForward ? settings.MountForwardsDecelerationDuration : settings.MountBackwardsDecelerationDuration);
		}
		if (num == _runForceDurationLeft && base.MovementSettings.BaseSpeed > 0f)
		{
			float value = _runForceInitialSpeed * num / base.MovementSettings.BaseSpeed;
			if (_isDecelerating)
			{
				_runForceDurationLeft = MathHelper.Clamp(value, 0f, num);
			}
			else
			{
				_runForceDurationLeft = num - MathHelper.Clamp(value, 0f, num);
			}
		}
		_runForceProgress = Easing.Ease(easingType, num - _runForceDurationLeft, 0f, 1f, num);
		wishSpeed = _runForceInitialSpeed + (wishSpeed - _runForceInitialSpeed) * _runForceProgress;
	}

	private void ComputeSprintForce(ref float wishSpeed)
	{
		if (!(SprintForceDurationLeft < 0f) && !MountMovementStates.IsWalking)
		{
			Settings settings = _gameInstance.App.Settings;
			Easing.EasingType easingType = (MountMovementStates.IsSprinting ? settings.SprintAccelerationEasingType : settings.SprintDecelerationEasingType);
			float num = (MountMovementStates.IsSprinting ? settings.SprintAccelerationDuration : settings.SprintDecelerationDuration);
			if (num == SprintForceDurationLeft && base.MovementSettings.BaseSpeed > 0f)
			{
				float num2 = base.MovementSettings.BaseSpeed * base.MovementSettings.ForwardSprintSpeedMultiplier - base.MovementSettings.BaseSpeed;
				float num3 = (MovementStates.IsFlying ? base.MovementSettings.HorizontalFlySpeed : base.MovementSettings.BaseSpeed);
				float num4 = (MovementStates.IsSprinting ? (num3 * base.MovementSettings.ForwardSprintSpeedMultiplier - _sprintForceInitialSpeed) : (_sprintForceInitialSpeed - num3));
				float value = num4 * num / num2;
				SprintForceDurationLeft = MathHelper.Clamp(value, 0f, num);
			}
			SprintForceProgress = Easing.Ease(easingType, num - SprintForceDurationLeft, 0f, 1f, num);
			wishSpeed = _sprintForceInitialSpeed + (wishSpeed - _sprintForceInitialSpeed) * SprintForceProgress;
		}
	}

	private float GetHorizontalMoveSpeed()
	{
		if (MountMovementStates.IsFlying)
		{
			return base.MovementSettings.HorizontalFlySpeed * (MountMovementStates.IsSprinting ? base.MovementSettings.ForwardSprintSpeedMultiplier : 1f);
		}
		Vector2 zero = Vector2.Zero;
		Vector2 zero2 = Vector2.Zero;
		if (MountMovementStates.IsSprinting)
		{
			zero.Y = 1.65f;
			zero2.Y = base.MovementSettings.ForwardSprintSpeedMultiplier;
		}
		else if (MountMovementStates.IsCrouching)
		{
			if (_wishDirection.X != 0f)
			{
				zero.X = 0.45f;
				zero2.X = base.MovementSettings.StrafeCrouchSpeedMultiplier;
			}
			if (_wishDirection.Y > 0f)
			{
				zero.Y = 0.55f;
				zero2.Y = base.MovementSettings.ForwardCrouchSpeedMultiplier;
			}
			else if (_wishDirection.Y < 0f)
			{
				zero.Y = 0.4f;
				zero2.Y = base.MovementSettings.BackwardCrouchSpeedMultiplier;
			}
		}
		else if (MountMovementStates.IsWalking)
		{
			if (_wishDirection.X != 0f)
			{
				zero.X = 0.3f;
				zero2.X = base.MovementSettings.StrafeWalkSpeedMultiplier;
			}
			if (_wishDirection.Y > 0f)
			{
				zero.Y = 0.3f;
				zero2.Y = base.MovementSettings.ForwardWalkSpeedMultiplier;
			}
			else if (_wishDirection.Y < 0f)
			{
				zero.Y = 0.3f;
				zero2.Y = base.MovementSettings.BackwardWalkSpeedMultiplier;
			}
		}
		else
		{
			if (_wishDirection.X != 0f)
			{
				zero.X = 0.8f;
				zero2.X = base.MovementSettings.StrafeRunSpeedMultiplier;
			}
			if (!_isDecelerating)
			{
				if (_wishDirection.Y > 0f)
				{
					zero.Y = 1f;
					zero2.Y = base.MovementSettings.ForwardRunSpeedMultiplier;
				}
				else if (_wishDirection.Y < 0f)
				{
					zero.Y = 0.65f;
					zero2.Y = base.MovementSettings.BackwardRunSpeedMultiplier;
				}
			}
		}
		float num = 0f;
		float num2 = 0f;
		if (zero2.Y > 0f)
		{
			num = zero.Y;
			num2 = zero2.Y;
		}
		else if (zero2.X > 0f)
		{
			num = zero.X;
			num2 = zero2.X;
		}
		float num3 = 1f;
		if (MountMovementStates.IsJumping || MountMovementStates.IsFalling)
		{
			num3 = MathHelper.Lerp(base.MovementSettings.AirSpeedMultiplier, base.MovementSettings.ComboAirSpeedMultiplier, ((float)_jumpCombo - 1f) * 0.5f);
		}
		float num4 = ((MountMovementStates.IsInFluid || _fluidJump) ? _averageFluidMovementSettings.HorizontalSpeedMultiplier : 1f);
		float num5 = _gameInstance.InteractionModule.ForEachInteraction((InteractionChain chain, ClientInteraction interaction, float mul) => mul * interaction.Interaction.HorizontalSpeedMultiplier, 1f);
		float horizontalSpeedMultiplier = _gameInstance.LocalPlayer.HorizontalSpeedMultiplier;
		float num6 = num2 * num3 * num4 * num5 * horizontalSpeedMultiplier;
		return base.MovementSettings.BaseSpeed * num6;
	}

	private float GetVerticalMoveSpeed()
	{
		if (MountMovementStates.IsFlying)
		{
			return base.MovementSettings.VerticalFlySpeed;
		}
		if (MountMovementStates.IsSwimming)
		{
			if (_gameInstance.LocalPlayer.LookOrientation.Pitch >= 0f)
			{
				return _averageFluidMovementSettings.SwimUpSpeed;
			}
			return _averageFluidMovementSettings.SwimDownSpeed;
		}
		return 1f;
	}

	private void DoMoveCycle(Vector3 offset)
	{
		Entity entity = _gameInstance.EntityStoreModule.GetEntity(MountEntityId);
		if (entity == null)
		{
			return;
		}
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		Input input = _gameInstance.Input;
		Vector3 size = entity.Hitbox.GetSize();
		Vector3 position = entity.Position;
		float num = ((MountMovementStates.IsFlying || MountMovementStates.IsOnGround || MountMovementStates.IsSwimming) ? 0.625f : (5f / 32f));
		bool flag = false;
		bool flag2 = false;
		_previousAutoJumpHeightShift = _nextAutoJumpHeightShift;
		position.Y += offset.Y;
		HitDetection.CollisionHitData hitData;
		bool flag3 = CheckCollision(position, offset, HitDetection.CollisionAxis.Y, out hitData);
		if (MountMovementStates.IsOnGround && offset.Y < 0f)
		{
			if (!flag3)
			{
				MountMovementStates.IsOnGround = false;
			}
			else
			{
				position.Y -= offset.Y;
			}
		}
		else if (flag3)
		{
			if (offset.Y <= 0f)
			{
				MountMovementStates.IsOnGround = true;
				position.Y = hitData.Limit.Y;
			}
			else
			{
				MountMovementStates.IsOnGround = false;
				_jumpCombo = 0;
				position.Y -= offset.Y;
			}
			foreach (AppliedVelocity appliedVelocity in _appliedVelocities)
			{
				appliedVelocity.Velocity.Y = 0f;
			}
			_velocity.Y = 0f;
		}
		else
		{
			MountMovementStates.IsOnGround = false;
		}
		int num2 = 0;
		MountMovementStates.IsClimbing = false;
		_collisionForward = (_collisionBackward = (_collisionLeft = (_collisionRight = false)));
		HitDetection.CollisionHitData hitData2;
		if (offset.X != 0f && !MountMovementStates.IsMantling)
		{
			position.X += offset.X;
			bool flag4 = CheckCollision(position, offset, HitDetection.CollisionAxis.X, out hitData);
			if (hitData.Overlap.X > 0f)
			{
				if (offset.Y > 0f && _requestedVelocity.X > 0f)
				{
					_requestedVelocity.X = 0f;
				}
				else if (offset.Y < 0f && _requestedVelocity.X < 0f)
				{
					_requestedVelocity.X = 0f;
				}
				num2 = _gameInstance.MapModule.GetBlock((int)System.Math.Floor(hitData.Limit.X + offset.X), (int)System.Math.Floor(position.Y), (int)System.Math.Floor(position.Z), 1);
				if (!MountMovementStates.IsOnGround)
				{
					float num3 = float.PositiveInfinity;
					if (_gameInstance.HitDetection.RaycastBlock(position, Vector3.Down, FallRaycastOptions, out _groundHit))
					{
						num3 = _groundHit.Distance;
					}
					if (_gameInstance.HitDetection.RaycastBlock(entity.Position, Vector3.Down, FallRaycastOptions, out _groundHit))
					{
						num3 = System.Math.Min(num3, _groundHit.Distance);
					}
					if (num3 < 0.375f)
					{
						num = 0.625f;
					}
				}
				bool canReach = hitData.Limit.Y > position.Y && hitData.Limit.Y - position.Y <= num;
				canReach = CanJumpObstacle(canReach, hitData, position, new Vector3(hitData.Limit.X + offset.X, position.Y, position.Z), new Vector2(hitData.Limit.X + offset.X - position.X, 0f), 90f);
				if (!MountMovementStates.IsClimbing && canReach && (MountMovementStates.IsFlying || MountMovementStates.IsSwimming || offset.Y < 0f))
				{
					float y = position.Y;
					position.Y = hitData.Limit.Y;
					if (CheckCollision(position, offset, HitDetection.CollisionAxis.X, out hitData2))
					{
						if (offset.X <= 0f)
						{
							position.X = hitData.Limit.X + size.X * 0.5f + 0.0001f;
						}
						else
						{
							position.X = hitData.Limit.X - size.X * 0.5f - 0.0001f;
						}
						position.Y = y;
					}
					else
					{
						flag = true;
						_autoJumpHeight = hitData.Overlap.Y;
						position.Y = hitData.Limit.Y + 0.0001f;
					}
				}
				else if (hitData.Overlap.X >= 0f)
				{
					_collisionLeft = offset.X <= 0f;
					_collisionRight = !_collisionLeft;
					if (_collisionLeft)
					{
						position.X = hitData.Limit.X + size.X * 0.5f + 0.0001f;
					}
					else
					{
						position.X = hitData.Limit.X - size.X * 0.5f - 0.0001f;
					}
					_velocity.X = 0f;
				}
			}
			else if (!MountMovementStates.IsFlying && MountMovementStates.IsOnGround && input.IsBindingHeld(inputBindings.Crouch))
			{
				Vector3 position2 = new Vector3(position.X, position.Y - 0.625f, position.Z);
				CheckCollision(position2, offset, HitDetection.CollisionAxis.X, out hitData);
				if (hitData.Overlap.Y <= 0f)
				{
					position.X -= offset.X;
				}
			}
		}
		if (offset.Z != 0f && !MountMovementStates.IsMantling)
		{
			position.Z += offset.Z;
			if (CheckCollision(position, offset, HitDetection.CollisionAxis.Z, out hitData))
			{
				if (offset.Z > 0f && _requestedVelocity.Z > 0f)
				{
					_requestedVelocity.Z = 0f;
				}
				else if (offset.Z < 0f && _requestedVelocity.Z < 0f)
				{
					_requestedVelocity.Z = 0f;
				}
				bool flag5 = flag2;
				Vector3 zero = Vector3.Zero;
				if (!MountMovementStates.IsOnGround)
				{
					float num4 = float.PositiveInfinity;
					if (_gameInstance.HitDetection.RaycastBlock(position, Vector3.Down, FallRaycastOptions, out _groundHit))
					{
						num4 = _groundHit.Distance;
					}
					if (_gameInstance.HitDetection.RaycastBlock(entity.Position, Vector3.Down, FallRaycastOptions, out _groundHit))
					{
						num4 = System.Math.Min(num4, _groundHit.Distance);
					}
					if (num4 < 0.375f)
					{
						num = 0.625f;
					}
				}
				bool canReach2 = hitData.Limit.Y > position.Y && hitData.Limit.Y - position.Y < num;
				canReach2 = CanJumpObstacle(canReach2, hitData, position, new Vector3(position.X, position.Y, hitData.Limit.Z + offset.Z), new Vector2(0f, hitData.Limit.Z + offset.Z - position.Z), -90f);
				if (!MountMovementStates.IsClimbing && canReach2 && (MountMovementStates.IsFlying || MountMovementStates.IsSwimming || offset.Y < 0f))
				{
					float y2 = position.Y;
					position.Y = hitData.Limit.Y;
					if (CheckCollision(position, offset, HitDetection.CollisionAxis.Z, out hitData2))
					{
						if (offset.Z <= 0f)
						{
							position.Z = hitData.Limit.Z + size.Z * 0.5f + 0.0001f;
						}
						else
						{
							position.Z = hitData.Limit.Z - size.Z * 0.5f - 0.0001f;
						}
						position.Y = y2;
					}
					else
					{
						flag = true;
						_autoJumpHeight = hitData.Overlap.Y;
						position.Y = hitData.Limit.Y + 0.0001f;
					}
				}
				else if (hitData.Overlap.Z >= 0f)
				{
					_collisionForward = offset.Z <= 0f;
					_collisionBackward = !_collisionForward;
					if (_collisionForward)
					{
						position.Z = hitData.Limit.Z + size.Z * 0.5f + 0.0001f;
					}
					else
					{
						position.Z = hitData.Limit.Z - size.Z * 0.5f - 0.0001f;
					}
					_velocity.Z = 0f;
				}
			}
			else if (!MountMovementStates.IsFlying && MountMovementStates.IsOnGround && input.IsBindingHeld(inputBindings.Crouch))
			{
				Vector3 position3 = new Vector3(position.X, position.Y - 0.625f, position.Z);
				CheckCollision(position3, offset, HitDetection.CollisionAxis.Z, out hitData);
				if (hitData.Overlap.Y <= 0f)
				{
					position.Z -= offset.Z;
				}
			}
		}
		if (flag)
		{
			float val = 1f / (System.Math.Max(System.Math.Abs(_velocity.X), System.Math.Abs(_velocity.Z)) * 0.25f);
			val = System.Math.Min(System.Math.Max(val, 0.01f), 1.5f);
			_autoJumpFrameCount = (int)System.Math.Floor(20f * val * _autoJumpHeight);
			if (_autoJumpFrameCount > 0)
			{
				_autoJumpFrame = _autoJumpFrameCount;
				_nextAutoJumpHeightShift = 0f - _autoJumpHeight;
			}
			else
			{
				_autoJumpFrame = 0;
				_nextAutoJumpHeightShift = 0f;
			}
		}
		if (_autoJumpFrame > 0)
		{
			_nextAutoJumpHeightShift += _autoJumpHeight / (float)_autoJumpFrameCount;
			_autoJumpFrame--;
		}
		MoveEntities(position);
	}

	private bool CanJumpObstacle(bool canReach, HitDetection.CollisionHitData hitData, Vector3 checkPos, Vector3 blockPos, Vector2 collision, float angleOffset)
	{
		if (!_gameInstance.App.Settings.AutoJumpObstacle)
		{
			return canReach;
		}
		if (canReach)
		{
			return true;
		}
		if (_jumpObstacleDurationLeft > 0f)
		{
			return false;
		}
		if (!MountMovementStates.IsOnGround || MountMovementStates.IsCrouching)
		{
			return false;
		}
		if (hitData.Limit.Y <= checkPos.Y || hitData.Limit.Y - checkPos.Y > 1f)
		{
			return false;
		}
		if (IsPositionGap(blockPos.X, blockPos.Y, blockPos.Z) || !IsPositionGap(blockPos.X, blockPos.Y + 1f, blockPos.Z))
		{
			return false;
		}
		collision.Normalize();
		float num = MathHelper.WrapAngle(CameraRotation.Y + MathHelper.ToRadians(angleOffset));
		Vector2 value = new Vector2((float)System.Math.Cos(num), (float)System.Math.Sin(num));
		value.Normalize();
		float num2 = Vector2.Dot(value, collision);
		num2 /= value.Length() * collision.Length();
		float radians = (float)System.Math.Acos(num2);
		float num3 = MathHelper.ToDegrees(radians);
		if (num3 > base.MovementSettings.AutoJumpObstacleMaxAngle)
		{
			return false;
		}
		_jumpObstacleDurationLeft = (MountMovementStates.IsSprinting ? base.MovementSettings.AutoJumpObstacleSprintEffectDuration : base.MovementSettings.AutoJumpObstacleEffectDuration);
		return true;
	}

	private bool CheckCollision(Vector3 position, Vector3 moveOffset, HitDetection.CollisionAxis axis, out HitDetection.CollisionHitData hitData)
	{
		hitData = default(HitDetection.CollisionHitData);
		Entity entity = _gameInstance.EntityStoreModule.GetEntity(MountEntityId);
		if (entity == null)
		{
			return false;
		}
		if (CheckCollision(position, moveOffset, entity.Hitbox, axis, out hitData))
		{
			return true;
		}
		BoundingBox hitbox = _gameInstance.LocalPlayer.Hitbox;
		return CheckCollision(position + _anchor, moveOffset, hitbox, axis, out hitData);
	}

	private void MoveEntities(Vector3 position)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Expected O, but got Unknown
		Entity entity = _gameInstance.EntityStoreModule.GetEntity(MountEntityId);
		if (entity != null)
		{
			entity.SetPosition(position);
			entity.LookOrientation = CameraRotation;
			entity.SetBodyOrientation(CameraRotation);
			entity.ServerMovementStates = MountMovementStates;
			_gameInstance.Connection.SendPacket((ProtoPacket)new EntityMovement(MountEntityId, position.ToPositionPacket(), entity.BodyOrientation.ToDirectionPacket(), ClientMovementStatesProtocolHelper.ToPacket(ref MountMovementStates)));
			PlayerEntity localPlayer = _gameInstance.LocalPlayer;
			localPlayer.SetPosition(position + _anchor);
			localPlayer.SetBodyOrientation(CameraRotation);
		}
	}

	private new bool CheckCollision(Vector3 position, Vector3 moveOffset, BoundingBox boundingBox, HitDetection.CollisionAxis axis, out HitDetection.CollisionHitData hitData)
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
			if (l == MountEntityId)
			{
				continue;
			}
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
				if (_gameInstance.EntityStoreModule.DebugInfoNeedsDrawing && !CollidedEntities.Contains(entity.NetworkId))
				{
					CollidedEntities.Add(entity.NetworkId);
				}
			}
		}
		return num4 > 0f;
	}

	private void UpdateViewModifiers()
	{
		Settings settings = _gameInstance.App.Settings;
		float num = settings.FieldOfView;
		if (settings.SprintFovEffect && !_gameInstance.IsBuilderModeEnabled() && (MountMovementStates.IsSprinting || (SprintForceDurationLeft > 0f && !MountMovementStates.IsWalking && !MountMovementStates.IsIdle)))
		{
			num *= 1f + (settings.SprintFovIntensity - 1f) * ((!(SprintForceDurationLeft > 0f)) ? 1f : (MountMovementStates.IsSprinting ? SprintForceProgress : (1f - SprintForceProgress)));
		}
		if (MountMovementStates.IsInFluid || _fluidJump)
		{
			num *= _averageFluidMovementSettings.FieldOfViewMultiplier;
		}
		if (System.Math.Abs(_gameInstance.ActiveFieldOfView - num) > 1f)
		{
			float fieldOfView = MathHelper.Lerp(_gameInstance.ActiveFieldOfView, System.Math.Min(num, 180f), 0.1f);
			_gameInstance.SetFieldOfView(fieldOfView);
		}
		_gameInstance.LocalPlayer.ApplyFirstPersonMovementItemWiggle(_wishDirection.X * -0.5f, (float)System.Math.Sign(_velocity.Y) * -0.5f, _wishDirection.Y * 0.5f);
	}

	private void UpdateMovementStates()
	{
		if (System.Math.Abs(_velocity.Y) <= 1E-07f && (_wishDirection == Vector2.Zero || (System.Math.Abs(_velocity.X) <= 1E-07f && System.Math.Abs(_velocity.Z) <= 1E-07f)) && _runForceDurationLeft <= 0f)
		{
			MountMovementStates.IsIdle = true;
		}
		else
		{
			MountMovementStates.IsIdle = false;
		}
		MountMovementStates.IsHorizontalIdle = _wishDirection == Vector2.Zero && _runForceDurationLeft <= 0f;
	}
}
