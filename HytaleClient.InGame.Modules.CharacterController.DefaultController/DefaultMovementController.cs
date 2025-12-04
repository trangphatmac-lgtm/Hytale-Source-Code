#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HytaleClient.Audio;
using HytaleClient.Core;
using HytaleClient.Data.ClientInteraction;
using HytaleClient.Data.Entities;
using HytaleClient.Data.EntityStats;
using HytaleClient.Data.Items;
using HytaleClient.Data.Map;
using HytaleClient.Data.UserSettings;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using SDL2;

namespace HytaleClient.InGame.Modules.CharacterController.DefaultController;

internal class DefaultMovementController : MovementController
{
	private const float AutoJumpMaxHeight = 0.625f;

	private const int DoubleJumpMaxDelay = 300;

	private const float MaxCycleMovement = 0.25f;

	private const int SurfaceCheckPadding = 3;

	private const float BaseForwardWalkSpeedMultiplier = 0.3f;

	private const float BaseBackwardWalkSpeedMultiplier = 0.3f;

	private const float BaseStrafeWalkSpeedMultiplier = 0.3f;

	private const float BaseForwardRunSpeedMultiplier = 1f;

	private const float BaseBackwardRunSpeedMultiplier = 0.65f;

	private const float BaseForwardSlideSpeedMultiplier = 0.55f;

	private const float BaseStrafeRunSpeedMultiplier = 0.8f;

	private const float BaseForwardCrouchSpeedMultiplier = 0.55f;

	private const float BaseBackwardCrouchSpeedMultiplier = 0.4f;

	private const float BaseStrafeCrouchSpeedMultiplier = 0.45f;

	private const float BaseForwardSprintSpeedMultiplier = 1.65f;

	private const float BaseForwardRollSpeedMultiplier = 1f;

	private const double CurveModifier = 0.75;

	private const double CurveMultiplier = 2.0;

	private const double MinVelocity = 18.0;

	private const double MinMultiplier = 10.0;

	private const float MaxCurve = 0.3f;

	private const string FallPenaltyWwiseId = "PLAYER_LAND_PENALTY_MAJOR";

	private static readonly HitDetection.RaycastOptions FallRaycastOptions = new HitDetection.RaycastOptions
	{
		Distance = 1f,
		IgnoreEmptyCollisionMaterial = true
	};

	private ClientBlockType _blockUnderFeet;

	private ClientBlockType _lastBlockUnderFeet;

	public const string Id = "Default";

	public const float KnockbackSimulationTime = 1.5f;

	private const string AllowsMovementTag = "Allows=Movement";

	private const float HeightShiftStep = 0.1f;

	private const int MaxJumpCombos = 3;

	private const float MaxJumpComboFactor = 0.5f;

	private const float BaseSprintForceMultiplier = 0.4f;

	private ClientMovementStates _previousMovementStates = ClientMovementStates.Idle;

	private float _previousFallingVelocity = 0f;

	private float _baseSpeedMultiplier = 1f;

	private float _speedMultiplier = 1f;

	public new float CurrentSpeedMultiplierDiff;

	private bool _wasOnGround;

	private bool _touchedGround;

	private float _acceleration;

	private HitDetection.RaycastHit _groundHit;

	private bool _wasClimbing;

	private Vector3 _climbingBlockPosition = Vector3.Zero;

	private int _climbingBlockId;

	private Vector3? _knockbackHitPosition;

	private Vector3 _requestedVelocity;

	private ChangeVelocityType? _requestedVelocityChangeType;

	private int _autoJumpFrame;

	private int _autoJumpFrameCount;

	private float _autoJumpHeight;

	private float _previousAutoJumpHeightShift;

	private float _nextAutoJumpHeightShift;

	private long _jumpReleaseTime;

	private bool _wasHoldingJump;

	private bool _canStartRunning;

	private float _sprintForceInitialSpeed;

	private float _lastLateralSpeed;

	private float _targetCrouchHeightShift;

	private float _previousCrouchHeightShift;

	private float _nextCrouchHeightShift;

	private bool _fluidJump;

	private float _swimJumpLastY;

	private int _jumpCombo;

	private readonly FluidFXMovementSettings _averageFluidMovementSettings;

	private float _jumpBufferDurationLeft;

	private float _jumpInputVelocity;

	private bool _jumpInputConsumed = true;

	private bool _jumpInputReleased = true;

	private bool _wasFalling;

	private float _yStartFalling;

	private float _yStartInAir;

	private float _fallEffectDurationLeft;

	private float _fallEffectSpeedMultiplier;

	private float _fallEffectJumpForce;

	private float _consecutiveMomentumLoss;

	private bool _fallEffectToApply;

	private float _jumpObstacleDurationLeft;

	private const string MantlingWWiseId = "MANTLING";

	private const float MantleDuration = 1f;

	private const float BlockLeftMargin = 0.3f;

	private const float BlockRightMargin = 0.7f;

	private const float HalfBlockSize = 0.5f;

	private const float MantleJumpHeight = 2f;

	private const float ModelHeight = 1.8f;

	private Vector3 _nextMantleCameraOffset = Vector3.Zero;

	private Vector3 _previousMantleCameraOffset = Vector3.Zero;

	private float _mantleDurationLeft;

	private Vector3 _mantleOffset = Vector3.Zero;

	private float _rollForceDurationLeft = -1f;

	private float _rollForceProgress;

	private float _rollForceInitialSpeed = 0f;

	private float _curRollSpeedMultiplier = 1f;

	private float _slideForceInitialSpeed = 0f;

	private const float BaseSlideForceMultiplier = 0.02f;

	private float _slideForceDurationLeft = -1f;

	private float _slideForceProgress;

	public DefaultMovementController(GameInstance gameInstance)
		: base(gameInstance)
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		_averageFluidMovementSettings = new FluidFXMovementSettings();
	}

	public override void Tick()
	{
		Input input = _gameInstance.Input;
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		_previousMovementStates = MovementStates;
		_previousFallingVelocity = _velocity.Y;
		if (MovementController.DebugMovementMode == DebugMovement.RunCycle || MovementController.DebugMovementMode == DebugMovement.RunCycleJump)
		{
			FakeInput((SDL_Keycode)119, (SDL_Scancode)26);
			_gameInstance.CameraModule.OffsetLook(0f, -7.5f);
			if (MovementController.DebugMovementMode == DebugMovement.RunCycleJump)
			{
				FakeInput((SDL_Keycode)32, (SDL_Scancode)44);
			}
		}
		StateFrame currentFrame = CaptureStateFrame(input, inputBindings);
		if (_knockbackHitPosition.HasValue)
		{
			int num = FindNearestStateFrame(_knockbackHitPosition.Value, (float)(_gameInstance.TimeModule.GetAveragePing((PongType)2) / 1000000.0));
			ref StateFrame reference = ref _stateFrames[num];
			_velocity = reference.Velocity;
			_gameInstance.LocalPlayer.SetPosition(_knockbackHitPosition.Value);
			MovementStates = reference.MovementStates;
			_knockbackHitPosition = null;
			while (num != _stateFrameOffset)
			{
				ref StateFrame reference2 = ref _stateFrames[num];
				num = (_stateFrames.Length + num + 1) % _stateFrames.Length;
				reference2.Position = _gameInstance.LocalPlayer.Position;
				reference2.Velocity = _velocity;
				DoTick(ref reference2);
			}
		}
		if ((!MovementStates.IsFlying || !base.SkipHitDetectionWhenFlying) && IsColliding(ref currentFrame, out var hitData))
		{
			int? num2 = FindMatchingStateFrame(delegate(ref StateFrame frame)
			{
				return !IsColliding(ref frame, out hitData);
			});
			if (!num2.HasValue)
			{
				Trace.WriteLine("Can't resolve. Pushing away");
				if (hitData.HitEntity.HasValue)
				{
					int num3 = 0;
					int value = hitData.HitEntity.Value;
					int num4 = 0;
					do
					{
						num4++;
						if (num4 > 100)
						{
							break;
						}
						if (value != hitData.HitEntity.Value)
						{
							num3 = 0;
							value = hitData.HitEntity.Value;
						}
						num3++;
						Entity entity = _gameInstance.EntityStoreModule.GetEntity(hitData.HitEntity.Value);
						Vector3 vector = entity.NextPosition - entity.PreviousPosition;
						currentFrame.Position += vector * num3 * (1f / 60f);
					}
					while (IsColliding(ref currentFrame, out hitData) && hitData.HitEntity.HasValue);
					Trace.WriteLine($"Resolved after {num4} steps");
				}
				_gameInstance.LocalPlayer.SetPosition(currentFrame.Position);
			}
			else
			{
				Trace.WriteLine("Resolving collision by resim");
				int num5 = num2.Value;
				ref StateFrame reference3 = ref _stateFrames[num5];
				_velocity = reference3.Velocity;
				_gameInstance.LocalPlayer.SetPosition(reference3.Position);
				MovementStates = reference3.MovementStates;
				while (num5 != _stateFrameOffset)
				{
					ref StateFrame reference4 = ref _stateFrames[num5];
					num5 = (_stateFrames.Length + num5 + 1) % _stateFrames.Length;
					reference4.Position = _gameInstance.LocalPlayer.Position;
					reference4.Velocity = _velocity;
					DoTick(ref reference4);
				}
			}
		}
		DoTick(ref currentFrame);
		void FakeInput(SDL_Keycode key, SDL_Scancode scan)
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			input.OnUserInput(new SDL_Event
			{
				type = (SDL_EventType)768,
				key = 
				{
					repeat = 0
				},
				key = 
				{
					keysym = 
					{
						sym = key
					}
				},
				key = 
				{
					keysym = 
					{
						scancode = scan
					}
				}
			});
		}
	}

	public override void PreUpdate(float timeFraction)
	{
		base.AutoJumpHeightShift = MathHelper.Lerp(_previousAutoJumpHeightShift, _nextAutoJumpHeightShift, timeFraction);
		base.CrouchHeightShift = MathHelper.Lerp(_previousCrouchHeightShift, _nextCrouchHeightShift, timeFraction);
		if (_mantleDurationLeft > 0f)
		{
			MantleCameraOffset.X = MathHelper.Lerp(_previousMantleCameraOffset.X, _nextMantleCameraOffset.X, timeFraction);
			MantleCameraOffset.Z = MathHelper.Lerp(_previousMantleCameraOffset.Z, _nextMantleCameraOffset.Z, timeFraction);
			MantleCameraOffset.Y = MathHelper.Lerp(_previousMantleCameraOffset.Y, _nextMantleCameraOffset.Y, timeFraction);
		}
		PlayerEntity localPlayer = _gameInstance.LocalPlayer;
		localPlayer.UpdateClientInterpolation(timeFraction);
	}

	public override void ApplyKnockback(ApplyKnockback applyKnockback)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		RequestVelocityChange(applyKnockback.X, applyKnockback.Y, applyKnockback.Z, applyKnockback.ChangeType, null);
		RunningKnockbackRemainingTime = 1.5f;
		_knockbackHitPosition = new Vector3((float)applyKnockback.HitPosition.X, (float)applyKnockback.HitPosition.Y, (float)applyKnockback.HitPosition.Z);
	}

	public override void ApplyMovementOffset(Vector3 movementOffset)
	{
		ICameraController controller = _gameInstance.CameraModule.Controller;
		if ((MovementStates.IsFlying && base.SkipHitDetectionWhenFlying) || controller.SkipCharacterPhysics)
		{
			_gameInstance.LocalPlayer.SetPosition(_gameInstance.LocalPlayer.Position + movementOffset);
			MovementStates.IsOnGround = (_wasOnGround = false);
		}
		else
		{
			_wasOnGround = MovementStates.IsOnGround;
			int num = (int)System.Math.Ceiling(movementOffset.Length() / 0.25f);
			Vector3 offset = movementOffset / num;
			for (int i = 0; i < num; i++)
			{
				DoMoveCycle(offset);
			}
			if (num == 0)
			{
				_gameInstance.LocalPlayer.SetPosition(_gameInstance.LocalPlayer.Position);
			}
			if (MovementStates.IsOnGround)
			{
				_fluidJump = false;
				_velocity.Y = 0f;
			}
		}
		_wasFalling = MovementStates.IsFalling;
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
		Vector2 wishDirection = _wishDirection;
		if (MovementStates.IsClimbing)
		{
			wishDirection.X = 0f;
		}
		return wishDirection;
	}

	private bool IsColliding(ref StateFrame currentFrame, out HitDetection.CollisionHitData hitData)
	{
		return CheckCollision(currentFrame.Position, Vector3.Zero, HitDetection.CollisionAxis.X, out hitData) || CheckCollision(currentFrame.Position, Vector3.Zero, HitDetection.CollisionAxis.Y, out hitData) || CheckCollision(currentFrame.Position, Vector3.Zero, HitDetection.CollisionAxis.Z, out hitData);
	}

	private bool CheckCollision(Vector3 position, Vector3 moveOffset, HitDetection.CollisionAxis axis, out HitDetection.CollisionHitData hitData)
	{
		BoundingBox hitbox = _gameInstance.LocalPlayer.Hitbox;
		return CheckCollision(position, moveOffset, hitbox, axis, out hitData);
	}

	private void DoTick(ref StateFrame frame)
	{
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Invalid comparison between Unknown and I4
		//IL_1268: Unknown result type (might be due to invalid IL or missing references)
		//IL_126c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1271: Unknown result type (might be due to invalid IL or missing references)
		//IL_1310: Unknown result type (might be due to invalid IL or missing references)
		//IL_1316: Invalid comparison between Unknown and I4
		//IL_147a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1480: Invalid comparison between Unknown and I4
		CollidedEntities.Clear();
		MovementStates.IsEntityCollided = false;
		_movementForceRotation = frame.MovementForceRotation;
		ref InputFrame input = ref frame.Input;
		UpdateInputSettings();
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		ICameraController controller = _gameInstance.CameraModule.Controller;
		bool flag = controller.CanMove && MovementEnabled;
		bool skipCharacterPhysics = controller.SkipCharacterPhysics;
		UpdateCameraSettings();
		if (flag && !_wasHoldingJump && input.IsBindingHeld(inputBindings.Jump) && base.MovementSettings.CanFly)
		{
			long epochMilliseconds = TimeHelper.GetEpochMilliseconds();
			long num = epochMilliseconds - _jumpReleaseTime;
			if (num < 300)
			{
				MovementStates.IsFlying = !MovementStates.IsFlying;
				_jumpReleaseTime = -1L;
			}
			else
			{
				_jumpReleaseTime = epochMilliseconds;
			}
		}
		if (_jumpBufferDurationLeft > 0f)
		{
			_jumpBufferDurationLeft -= 1f / 60f;
		}
		bool flag2 = (int)_gameInstance.GameMode == 1;
		bool flag3 = flag2 || !_gameInstance.LocalPlayer.DisableJump;
		bool flag4 = flag2 || !_gameInstance.LocalPlayer.DisableCrouch;
		bool flag5 = flag2 || !_gameInstance.LocalPlayer.DisableSprint;
		bool flag6 = input.IsBindingHeld(inputBindings.Jump) && flag3;
		if (!_wasHoldingJump && flag6 && (!base.MovementSettings.AutoJumpDisableJumping || (base.MovementSettings.AutoJumpDisableJumping && _autoJumpFrame <= 0)) && !MovementStates.IsRolling)
		{
			_jumpBufferDurationLeft = base.MovementSettings.JumpBufferDuration;
			_jumpInputVelocity = _velocity.Y;
			_jumpInputConsumed = false;
			_jumpInputReleased = false;
		}
		else if (_wasHoldingJump && !flag6)
		{
			_jumpInputReleased = true;
		}
		ClientEntityStatValue entityStat = _gameInstance.LocalPlayer.GetEntityStat(DefaultEntityStats.Stamina);
		if (MovementStates.IsSprinting && entityStat != null && entityStat.Value <= 0f)
		{
			MovementStates.IsSprinting = false;
			_canStartRunning = false;
		}
		if (_jumpObstacleDurationLeft > 0f)
		{
			_jumpObstacleDurationLeft -= 1f / 60f;
		}
		if (_mantleDurationLeft > 0f)
		{
			UpdateMantle();
		}
		bool flag7 = input.IsBindingHeld(inputBindings.Sprint);
		bool flag8 = flag && input.IsBindingHeld(inputBindings.MoveForwards) && _canStartRunning && flag7 && (flag2 || !_gameInstance.LocalPlayer.DisableSprint) && (MovementStates.IsFlying || !input.IsBindingHeld(inputBindings.Crouch));
		if (_gameInstance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)4))
		{
			UpdateSliding(flag, ref input);
		}
		if (!flag7 || MovementStates.IsIdle)
		{
			_canStartRunning = true;
		}
		bool flag9 = false;
		UpdateFluidData();
		UpdateSpecialBlocks();
		BoundingBox hitbox = _gameInstance.LocalPlayer.Hitbox;
		int num2 = (base.MovementSettings.InvertedGravity ? 1 : (-1));
		float num3 = 1f;
		if (_velocity.Y < 0f && !_gameInstance.IsBuilderModeEnabled())
		{
			num3 = _blockUnderFeet.MovementSettings.TerminalVelocityModifier;
		}
		float num4 = num3 * (float)num2 * GetVerticalMoveSpeed() * PhysicsMath.GetTerminalVelocity(base.MovementSettings.Mass, 0.001225f, System.Math.Abs((hitbox.Max.X - hitbox.Min.X) * (hitbox.Max.Z - hitbox.Min.Z)), base.MovementSettings.DragCoefficient);
		float num5 = (float)num2 * PhysicsMath.GetAcceleration(_velocity.Y, num4) * (1f / 60f);
		if (SprintForceDurationLeft > 0f)
		{
			SprintForceDurationLeft -= 1f / 60f;
		}
		if (_slideForceDurationLeft > 0f)
		{
			_slideForceDurationLeft -= 1f / 60f;
		}
		if (MovementStates.IsFlying || skipCharacterPhysics)
		{
			MovementStates.IsFalling = false;
			MovementStates.IsSprinting = flag8;
			MovementStates.IsWalking = flag && input.IsBindingHeld(inputBindings.Walk);
			MovementStates.IsJumping = flag && input.IsBindingHeld(inputBindings.Jump);
			UpdateCrouching(flag && !MovementStates.IsJumping, ref input, ref inputBindings);
			_fluidJump = false;
			if (flag && input.IsBindingHeld(inputBindings.FlyDown))
			{
				_velocity.Y = (0f - base.MovementSettings.VerticalFlySpeed) * SpeedMultiplier * _gameInstance.CameraModule.Controller.SpeedModifier;
			}
			else if (flag && input.IsBindingHeld(inputBindings.FlyUp))
			{
				_velocity.Y = base.MovementSettings.VerticalFlySpeed * SpeedMultiplier * _gameInstance.CameraModule.Controller.SpeedModifier;
				MovementStates.IsOnGround = false;
			}
			else
			{
				_velocity.Y = 0f;
			}
		}
		else if (MovementStates.IsClimbing && !MovementStates.IsOnGround)
		{
			if (_gameInstance.MapModule.GetBlock((int)_climbingBlockPosition.X, (int)_climbingBlockPosition.Y, (int)_climbingBlockPosition.Z, 1) != _climbingBlockId)
			{
				MovementStates.IsClimbing = false;
			}
			else
			{
				MovementStates.IsFalling = false;
				MovementStates.IsSprinting = false;
				MovementStates.IsJumping = flag && HasJumpInputQueued();
				UpdateCrouching(flag && !MovementStates.IsJumping, ref input, ref inputBindings);
				_fluidJump = false;
				_velocity.Y = 0f;
				if (input.IsBindingHeld(inputBindings.StrafeLeft) || input.IsBindingHeld(inputBindings.StrafeRight) || input.IsBindingHeld(inputBindings.MoveBackwards))
				{
					flag9 = true;
				}
				else if (MovementStates.IsJumping)
				{
					_velocity.Y = base.MovementSettings.JumpForce;
					if ((_collisionForward || _collisionBackward) && (_collisionLeft || _collisionRight))
					{
						_velocity.Z = (_velocity.X = base.MovementSettings.JumpForce * 0.25f);
					}
					else if (_collisionForward || _collisionBackward)
					{
						_velocity.Z = base.MovementSettings.JumpForce * 0.4f;
					}
					else
					{
						_velocity.X = base.MovementSettings.JumpForce * 0.4f;
					}
					if (_collisionRight)
					{
						_velocity.X *= -1f;
					}
					if (_collisionBackward)
					{
						_velocity.Z *= -1f;
					}
					MovementStates.IsClimbing = false;
					_jumpBufferDurationLeft = 0f;
					_jumpInputConsumed = true;
				}
			}
		}
		else if (MovementStates.IsSwimming)
		{
			MovementStates.IsSprinting = flag8 && flag5;
			MovementStates.IsJumping = flag6 && flag && input.IsBindingHeld(inputBindings.Jump);
			UpdateCrouching(flag, ref input, ref inputBindings);
			if (!MovementStates.IsSwimJumping)
			{
				_velocity.Y = 0f;
				if (MovementStates.IsJumping)
				{
					_velocity.Y = _averageFluidMovementSettings.SwimUpSpeed;
				}
				else if (MovementStates.IsCrouching)
				{
					_velocity.Y = (flag4 ? _averageFluidMovementSettings.SwimDownSpeed : 0f);
				}
				float num6 = -1f;
				for (int i = 0; i <= GetHitboxHeight() + 3; i++)
				{
					if (!GetRelativeFluid(0f, i, 0f, out var _))
					{
						num6 = (int)System.Math.Floor(_gameInstance.LocalPlayer.Position.Y + (float)i);
						if (GetRelativeFluid(0f, i - 1, 0f, out var blockTypeOut2) && blockTypeOut2.VerticalFill != blockTypeOut2.MaxFillLevel)
						{
							num6 -= 1f - (float)(int)blockTypeOut2.VerticalFill / (float)(int)blockTypeOut2.MaxFillLevel;
						}
						break;
					}
				}
				float y = _gameInstance.LocalPlayer.Position.Y;
				int hitboxHeight = GetHitboxHeight();
				float num7 = num6 - (float)hitboxHeight * 0.7f;
				if (num6 != -1f && y > num7)
				{
					float num8 = (num7 - y) * 10f;
					if (System.Math.Abs(num8) > 0.1f)
					{
						_velocity.Y = num8;
					}
				}
				if (_velocity.Y == 0f && (num6 == -1f || y < num6 - (float)hitboxHeight))
				{
					_velocity.Y = _averageFluidMovementSettings.SinkSpeed;
				}
				float num9 = y + _velocity.Y * (1f / 60f);
				float num10 = (float)System.Math.Ceiling((float)hitboxHeight * 0.5f);
				if (num6 != -1f && num9 > y && num9 >= num6 - num10)
				{
					_velocity.Y = num6 - num10 - y;
				}
				if (MovementStates.IsJumping && _velocity.Y >= 0f && System.Math.Abs(y - num7) < 0.2f)
				{
					MovementStates.IsSwimJumping = true;
					_velocity.Y = base.MovementSettings.SwimJumpForce;
					_swimJumpLastY = y;
				}
			}
			_fluidJump = MovementStates.IsSwimJumping;
			MovementStates.IsFalling = _velocity.Y < 0f && !MovementStates.IsCrouching;
		}
		else if (MovementStates.IsMantling)
		{
			_velocity.Y = 0f;
		}
		else
		{
			MovementStates.IsSprinting = flag8 && (MovementStates.IsSprinting || MovementStates.IsOnGround);
			MovementStates.IsWalking = flag && !MovementStates.IsSprinting && input.IsBindingHeld(inputBindings.Walk) && MovementStates.IsOnGround;
			flag9 |= MovementStates.IsRolling;
			if (_gameInstance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)3) && _gameInstance.App.Settings.SprintForce)
			{
				UpdateSprintForceValues();
			}
			if (_gameInstance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)4))
			{
				UpdateSlidingForceValues();
			}
			if (_velocity.Y < num4 && num5 > 0f)
			{
				_velocity.Y = System.Math.Min(_velocity.Y + num5, num4);
			}
			else if (_velocity.Y > num4 && num5 < 0f)
			{
				_velocity.Y = System.Math.Max(_velocity.Y + num5, num4);
			}
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
			CheckBounce(flag);
			if (flag && MovementStates.IsOnGround)
			{
				MovementStates.IsJumping = HasJumpInputQueued() && _wasOnGround;
				if (base.MovementSettings.AutoJumpDisableJumping && _autoJumpFrame > 0)
				{
					MovementStates.IsJumping = false;
				}
				MovementStates.IsFalling = false;
				_touchedGround = true;
				if (MovementStates.IsJumping)
				{
					_velocity.Y = ComputeJumpForce();
					if (_fallEffectDurationLeft > 0f)
					{
						_velocity.Y = _fallEffectJumpForce;
					}
					MovementStates.IsOnGround = false;
					_fluidJump = MovementStates.IsInFluid;
					_jumpCombo = (int)MathHelper.Min(_jumpCombo + 1, 3f);
					_jumpBufferDurationLeft = 0f;
					_jumpInputConsumed = true;
					if (_fallEffectDurationLeft <= 0f)
					{
						_consecutiveMomentumLoss = 0f;
					}
					base.ApplyMarioFallForce = true;
				}
				else if (flag3 && _gameInstance.App.Settings.AutoJumpGap && MovementStates.IsSprinting && _wishDirection.Y != 0f && IsGapAhead())
				{
					_velocity.Y = ComputeJumpForce();
					MovementStates.IsOnGround = false;
					_fluidJump = MovementStates.IsInFluid;
					_jumpCombo = (int)MathHelper.Min(_jumpCombo + 1, 3f);
					_jumpBufferDurationLeft = 0f;
					_jumpInputConsumed = true;
					base.ApplyMarioFallForce = false;
				}
			}
			else
			{
				if (_gameInstance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)0) && flag && _touchedGround && !_wasHoldingJump)
				{
					MovementStates.IsJumping = _gameInstance.Input.IsBindingDown(inputBindings.Jump);
					if (MovementStates.IsJumping)
					{
						InventoryModule inventoryModule = _gameInstance.InventoryModule;
						ClientItemStack[] armorInventory = inventoryModule._armorInventory;
						foreach (ClientItemStack clientItemStack in armorInventory)
						{
							if (clientItemStack != null && clientItemStack.Id.Equals("Trinket_Magic_Feather"))
							{
								MovementStates.IsFalling = false;
								_touchedGround = false;
								_gameInstance.InteractionModule.StartChain((InteractionType)17, InteractionModule.ClickType.Single, null);
								base.ApplyMarioFallForce = false;
							}
						}
					}
				}
				Vector3 position = _gameInstance.LocalPlayer.Position;
				position.Y += 0.1f;
				MovementStates.IsFalling = _velocity.Y < 0f && !_gameInstance.HitDetection.RaycastBlock(position, Vector3.Down, FallRaycastOptions, out var _);
				if (MovementStates.IsFalling)
				{
					MovementStates.IsJumping = false;
					MovementStates.IsSwimJumping = false;
				}
			}
			if (_jumpCombo != 0 && ((MovementStates.IsOnGround && _wasOnGround) || System.Math.Abs(_velocity.X) <= 1E-07f || System.Math.Abs(_velocity.Z) <= 1E-07f))
			{
				_jumpCombo = 0;
			}
			UpdateCrouching(flag, ref input, ref inputBindings);
			MovementStates.IsSprinting &= !MovementStates.IsCrouching && !MovementStates.IsSliding;
		}
		_wasHoldingJump = flag6;
		Vector3 vector = CreateRepulsionVector();
		_velocity += vector;
		ComputeWishDirection(flag9, flag, input, inputBindings);
		base.WishDirection = _wishDirection;
		ComputeMoveForce();
		if (!MovementStates.IsFlying && _requestedVelocityChangeType.HasValue)
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
		_wasClimbing = MovementStates.IsClimbing && !MovementStates.IsOnGround;
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
		velocity = ApplyExternalForces(velocity);
		PreviousMovementOffset = MovementOffset;
		MovementOffset = velocity;
		if ((int)RaycastMode == 0)
		{
			CheckEntityCollision(velocity);
		}
		else
		{
			Vector3 movement = Vector3.CreateFromYawPitch(_gameInstance.LocalPlayer.LookOrientation.Yaw, _gameInstance.LocalPlayer.LookOrientation.Pitch);
			CheckEntityCollision(movement);
		}
		ComputeFallEffect();
		controller.ApplyMove(velocity * (1f / 60f));
		RunningKnockbackRemainingTime -= 1f / 60f;
		if (!MovementStates.IsOnGround && RunningKnockbackRemainingTime > 0f)
		{
			RunningKnockbackRemainingTime = 1.5f;
		}
		else if (RunningKnockbackRemainingTime <= 0f)
		{
			RunningKnockbackRemainingTime = 0f;
		}
		float num11 = ((_fallEffectDurationLeft > 0f) ? (_speedMultiplier * _fallEffectSpeedMultiplier) : _speedMultiplier);
		CurrentSpeedMultiplierDiff = (num11 - _baseSpeedMultiplier) / _baseSpeedMultiplier;
		_gameInstance.LocalPlayer.OnSpeedMultipliersChanged(CurrentSpeedMultiplierDiff);
		_gameInstance.App.Interface.InGameView.OnCharacterControllerTicked(MovementStates);
	}

	private void CheckBounce(bool canMove)
	{
		if (!canMove || _wasOnGround || !_blockUnderFeet.MovementSettings.IsBouncy || _gameInstance.IsBuilderModeEnabled())
		{
			return;
		}
		_velocity.Y = _blockUnderFeet.MovementSettings.BounceVelocity;
		MovementStates.IsOnGround = true;
		MovementStates.IsJumping = false;
		_fluidJump = MovementStates.IsInFluid;
		_jumpCombo = (int)MathHelper.Min(_jumpCombo + 1, 3f);
		_jumpBufferDurationLeft = 0f;
		_jumpInputConsumed = true;
		if (_gameInstance.ServerSettings.BlockSoundSets[_blockUnderFeet.BlockSoundSetIndex].SoundEventIndices.TryGetValue((BlockSoundEvent)5, out var value))
		{
			uint networkWwiseId = ResourceManager.GetNetworkWwiseId(value);
			if (networkWwiseId != 0)
			{
				float x = _gameInstance.LocalPlayer.Position.X;
				float num = _gameInstance.LocalPlayer.Position.Y - 1f;
				float z = _gameInstance.LocalPlayer.Position.Z;
				Vector3 position = new Vector3(x + 0.5f, num + 0.5f, z + 0.5f);
				_gameInstance.AudioModule.PlaySoundEvent(networkWwiseId, position, Vector3.Zero);
			}
		}
	}

	private void UpdateFluidData()
	{
		int hitboxHeight = GetHitboxHeight();
		int num = 0;
		for (int i = 0; i < hitboxHeight; i++)
		{
			if (GetRelativeFluid(0f, i, 0f, out var blockTypeOut))
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
		MovementStates.IsInFluid = num > 0;
		MovementStates.IsSwimming = num == hitboxHeight;
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
		if (MovementStates.IsSwimJumping)
		{
			if (_gameInstance.LocalPlayer.Position.Y <= _swimJumpLastY)
			{
				MovementStates.IsSwimJumping = false;
			}
			_swimJumpLastY = _gameInstance.LocalPlayer.Position.Y;
		}
	}

	public void UpdateSpecialBlocks()
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Invalid comparison between Unknown and I4
		int worldX = (int)System.Math.Floor(_gameInstance.LocalPlayer.Position.X);
		int worldY = (int)System.Math.Floor((double)_gameInstance.LocalPlayer.Position.Y - 0.2);
		int worldZ = (int)System.Math.Floor(_gameInstance.LocalPlayer.Position.Z);
		int block = _gameInstance.MapModule.GetBlock(worldX, worldY, worldZ, 0);
		ClientBlockType clientBlockType = (_blockUnderFeet = _gameInstance.MapModule.ClientBlockTypes[block]);
		if ((int)clientBlockType.CollisionMaterial == 1 || _lastBlockUnderFeet == null)
		{
			_lastBlockUnderFeet = clientBlockType;
		}
	}

	private int GetHitboxHeight()
	{
		return (int)System.Math.Max(System.Math.Ceiling(_gameInstance.LocalPlayer.Hitbox.GetSize().Y), 1.0);
	}

	private bool GetRelativeFluid(float xOffset, float yOffset, float zOffset, out ClientBlockType blockTypeOut)
	{
		int worldX = (int)System.Math.Floor(_gameInstance.LocalPlayer.Position.X + xOffset);
		int worldY = (int)System.Math.Floor(_gameInstance.LocalPlayer.Position.Y + yOffset);
		int worldZ = (int)System.Math.Floor(_gameInstance.LocalPlayer.Position.Z + zOffset);
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

	private void UpdateCrouching(bool canMove, ref InputFrame input, ref InputBindings inputBindings)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		bool flag = (int)_gameInstance.GameMode == 1 || !_gameInstance.LocalPlayer.DisableCrouch;
		MovementStates.IsCrouching = canMove && input.IsBindingHeld(inputBindings.Crouch) && flag;
		MovementStates.IsForcedCrouching = false;
		if (MovementStates.IsFlying && base.SkipHitDetectionWhenFlying)
		{
			_targetCrouchHeightShift = 0f;
		}
		else if (MovementStates.IsCrouching)
		{
			MovementStates.IsForcedCrouching = EnforceCrouch();
			_targetCrouchHeightShift = ((!MovementStates.IsFlying || MovementStates.IsForcedCrouching) ? _gameInstance.LocalPlayer.CrouchOffset : 0f);
		}
		else
		{
			MovementStates.IsForcedCrouching = EnforceCrouch();
			MovementStates.IsCrouching = MovementStates.IsForcedCrouching;
			_targetCrouchHeightShift = (MovementStates.IsCrouching ? _gameInstance.LocalPlayer.CrouchOffset : 0f);
		}
	}

	private bool EnforceCrouch()
	{
		PlayerEntity localPlayer = _gameInstance.LocalPlayer;
		Vector3 position = localPlayer.Position;
		BoundingBox defaultHitbox = localPlayer.DefaultHitbox;
		defaultHitbox.Translate(position);
		float y = defaultHitbox.Max.Y;
		defaultHitbox.Translate(new Vector3(0f, 0.0001f, 0f));
		bool result = false;
		for (int i = -2; i <= 2; i++)
		{
			for (int j = -2; j <= 2; j++)
			{
				Vector3 pos = new Vector3(position.X + (float)j, y, position.Z + (float)i);
				if (_gameInstance.HitDetection.CheckBlockCollision(defaultHitbox, pos, Vector3.Up, out var hitData) && hitData.GetYCollideState())
				{
					result = true;
					if (hitData.Overlap.Y >= 0f - localPlayer.CrouchOffset)
					{
						return false;
					}
				}
			}
		}
		return result;
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

	private void UpdateSprintForceValues()
	{
		if (MovementStates.IsSprinting == _previousMovementStates.IsSprinting && MovementStates.IsInFluid == _previousMovementStates.IsInFluid)
		{
			return;
		}
		if (MovementStates.IsIdle && !_previousMovementStates.IsIdle)
		{
			SprintForceDurationLeft = -1f;
			return;
		}
		_sprintForceInitialSpeed = (_previousMovementStates.IsIdle ? 0f : _lastLateralSpeed);
		if (MovementStates.IsSprinting)
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
		float num = 0.8f;
		float num2 = 0.2f;
		if (System.Math.Abs(vector2.X) > System.Math.Abs(vector2.Z))
		{
			if (vector2.X > 0f && System.Math.Abs(position.X) % 1f < num2)
			{
				return false;
			}
			if (vector2.X < 0f && System.Math.Abs(position.X) % 1f > num)
			{
				return false;
			}
		}
		else
		{
			if (vector2.Z > 0f && System.Math.Abs(position.Z) % 1f > num)
			{
				return false;
			}
			if (vector2.Z < 0f && System.Math.Abs(position.Z) % 1f < num2)
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

	private Vector3 CreateRepulsionVector()
	{
		Entity[] allEntities = _gameInstance.EntityStoreModule.GetAllEntities();
		Vector2 vector = new Vector2(_gameInstance.LocalPlayer.Position.X, _gameInstance.LocalPlayer.Position.Z);
		Vector3 vector2 = Vector3.Zero;
		BoundingBox hitbox = _gameInstance.LocalPlayer.Hitbox;
		hitbox.Translate(_gameInstance.LocalPlayer.Position);
		for (int i = 0; i < _gameInstance.EntityStoreModule.GetEntitiesCount(); i++)
		{
			Entity entity = allEntities[i];
			if (entity == null || entity.NetworkId == _gameInstance.LocalPlayerNetworkId || !entity.IsTangible() || entity.RepulsionConfigIndex == -1 || entity.Position.Equals(_gameInstance.LocalPlayer.Position))
			{
				continue;
			}
			Vector2 vector3 = new Vector2(entity.Position.X, entity.Position.Z);
			float num = Vector2.Distance(vector, vector3);
			ClientRepulsionConfig clientRepulsionConfig = _gameInstance.ServerSettings.RepulsionConfigs[entity.RepulsionConfigIndex];
			float radius = clientRepulsionConfig.Radius;
			BoundingBox hitbox2 = entity.Hitbox;
			hitbox2.Translate(entity.Position);
			entity.LastPush = Vector2.Zero;
			if (MovementStates.IsOnGround && num <= radius && (double)num > 0.1 && IntersectsY(hitbox, hitbox2))
			{
				float num2 = (radius - num) / radius;
				Vector2 lastPush = vector - vector3;
				lastPush.Normalize();
				float num3 = clientRepulsionConfig.MaxForce;
				int num4 = 1;
				if (num3 < 0f)
				{
					num4 = -1;
					num3 *= (float)num4;
				}
				float num5 = System.Math.Max(clientRepulsionConfig.MinForce, num3 * num2);
				num5 *= (float)num4;
				if (_wishDirection.Length() == 0f)
				{
					num5 = 1f;
				}
				lastPush *= num5;
				entity.LastPush = lastPush;
				vector2 = ((!(vector2 == Vector3.Zero)) ? ((vector2 + new Vector3(lastPush.X, 0f, lastPush.Y)) / 2f) : new Vector3(lastPush.X, 0f, lastPush.Y));
			}
		}
		return vector2;
	}

	private static bool IntersectsY(BoundingBox a, BoundingBox b)
	{
		return Intersects(a.Min.Y, a.Max.Y, b.Min.Y, b.Max.Y);
	}

	private static bool Intersects(double x1, double x2, double y1, double y2)
	{
		return x2 >= y1 && y2 >= x1;
	}

	private void ComputeMoveForce()
	{
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Invalid comparison between Unknown and I4
		LastMoveForce = Vector3.Zero;
		float num = 0f;
		float num2 = 1f;
		float num3 = 1f;
		float value = (float)System.Math.Sqrt(_velocity.X * _velocity.X + _velocity.Z * _velocity.Z);
		if (!MovementStates.IsFlying && !MovementStates.IsClimbing && !_gameInstance.IsBuilderModeEnabled())
		{
			num = ((!MovementStates.IsOnGround && !MovementStates.IsSwimming) ? MathHelper.ConvertToNewRange(value, base.MovementSettings.AirDragMinSpeed, base.MovementSettings.AirDragMaxSpeed, base.MovementSettings.AirDragMin, base.MovementSettings.AirDragMax) : _lastBlockUnderFeet.MovementSettings.Drag);
			num2 = ((!MovementStates.IsOnGround && !MovementStates.IsSwimming) ? MathHelper.ConvertToNewRange(value, base.MovementSettings.AirFrictionMinSpeed, base.MovementSettings.AirFrictionMaxSpeed, base.MovementSettings.AirFrictionMax, base.MovementSettings.AirFrictionMin) : _lastBlockUnderFeet.MovementSettings.Friction);
			num3 = base.MovementSettings.Acceleration;
		}
		_velocity.X *= num;
		_velocity.Z *= num;
		Vector3 movementForceRotation = _movementForceRotation;
		bool flag = (_gameInstance.App.Settings.UseNewFlyCamera && MovementStates.IsFlying) || _gameInstance.CameraModule.Controller.AllowPitchControls;
		Quaternion rotation = Quaternion.CreateFromYawPitchRoll(movementForceRotation.Y, flag ? movementForceRotation.X : 0f, 0f);
		Vector3 vector = Vector3.Transform(Vector3.Forward, rotation);
		Vector3 vector2 = Vector3.Transform(Vector3.Right, rotation);
		Vector3 vector3 = vector * _wishDirection.Y + vector2 * _wishDirection.X;
		if (vector3.LengthSquared() < 0.0001f)
		{
			_acceleration *= num3;
			return;
		}
		vector3.Normalize();
		float num4 = 1f;
		if ((int)_gameInstance.GameMode == 1)
		{
			float num5 = SpeedMultiplier;
			if (MovementStates.IsSprinting)
			{
				num5 = ((SpeedMultiplier < 1f) ? 1f : SpeedMultiplier);
			}
			num4 = num5 * _gameInstance.CameraModule.Controller.SpeedModifier;
		}
		else if (!MovementStates.IsOnGround && !MovementStates.IsSwimming && !_wasClimbing)
		{
			num4 += MathHelper.ConvertToNewRange(value, base.MovementSettings.AirControlMinSpeed, base.MovementSettings.AirControlMaxSpeed, base.MovementSettings.AirControlMaxMultiplier, base.MovementSettings.AirControlMinMultiplier);
		}
		float wishSpeed = GetHorizontalMoveSpeed() * num4;
		if (_gameInstance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)3) && _gameInstance.App.Settings.SprintForce)
		{
			ComputeSprintForce(ref wishSpeed);
		}
		if (_gameInstance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)5))
		{
			ComputeAndUpdateRollingForce();
		}
		if (_gameInstance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)4))
		{
			ComputeSlideForce(ref wishSpeed);
		}
		if (!_gameInstance.IsBuilderModeEnabled())
		{
			wishSpeed *= _lastBlockUnderFeet.MovementSettings.HorizontalSpeedMultiplier;
		}
		float num6 = Vector3.Dot(_velocity, vector3);
		if (!MovementStates.IsOnGround)
		{
			Vector3 vector4 = vector * _wishDirection.Y;
			vector4.Normalize();
			float num7 = Vector3.Dot(_velocity, vector4);
			if (num7 > num6)
			{
				num6 = num7;
			}
		}
		float num8 = wishSpeed - num6;
		if (!(num8 <= 0f))
		{
			float num9 = wishSpeed * num2;
			if (_fallEffectDurationLeft > 0f)
			{
				num9 *= _fallEffectSpeedMultiplier;
			}
			if (_jumpObstacleDurationLeft > 0f)
			{
				num9 *= 1f - (MovementStates.IsSprinting ? base.MovementSettings.AutoJumpObstacleSprintSpeedLoss : base.MovementSettings.AutoJumpObstacleSpeedLoss);
			}
			if (num9 > num8)
			{
				num9 = num8;
			}
			_acceleration += ((base.MovementSettings.BaseSpeed != 0f) ? (num9 * (wishSpeed / base.MovementSettings.BaseSpeed * num3)) : 0f);
			if (_acceleration > num9)
			{
				_acceleration = num9;
			}
			_acceleration *= ComputeMomentumLossMultiplier();
			vector3.X *= _acceleration;
			vector3.Y *= GetVerticalMoveSpeed() * num4;
			vector3.Z *= _acceleration;
			LastMoveForce = vector3;
			_velocity += vector3;
			_lastLateralSpeed = (float)System.Math.Sqrt((double)(_velocity.X * _velocity.X) + (double)(_velocity.Z * _velocity.Z));
		}
	}

	private float ComputeJumpForce()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		float num = base.MovementSettings.JumpForce;
		if (!_gameInstance.IsBuilderModeEnabled())
		{
			num *= _blockUnderFeet.MovementSettings.JumpForceMultiplier;
		}
		if ((int)_gameInstance.GameMode != 1 || SpeedMultiplier <= 1f)
		{
			return num;
		}
		return num + System.Math.Min((SpeedMultiplier - 1f) * _gameInstance.App.Settings.JumpForceSpeedMultiplierStep, _gameInstance.App.Settings.MaxJumpForceSpeedMultiplier);
	}

	private float GetHorizontalMoveSpeed()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		if ((int)_gameInstance.GameMode == 1 && MovementStates.IsFlying)
		{
			return base.MovementSettings.HorizontalFlySpeed * (MovementStates.IsSprinting ? base.MovementSettings.ForwardSprintSpeedMultiplier : 1f);
		}
		Vector2 zero = Vector2.Zero;
		Vector2 zero2 = Vector2.Zero;
		if (MovementStates.IsRolling)
		{
			_wishDirection.X = 0f;
			zero.Y = 1f;
			zero2.Y = _curRollSpeedMultiplier;
		}
		else if (MovementStates.IsSprinting)
		{
			zero.Y = 1.65f;
			zero2.Y = base.MovementSettings.ForwardSprintSpeedMultiplier;
		}
		else if (MovementStates.IsSliding)
		{
			zero.Y = 0.55f;
			zero2.Y = base.MovementSettings.StrafeWalkSpeedMultiplier;
		}
		else if (MovementStates.IsCrouching)
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
		else if (MovementStates.IsWalking)
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
		float baseSpeedMultiplier = 0f;
		float num = 0f;
		if (zero2.Y > 0f)
		{
			baseSpeedMultiplier = zero.Y;
			num = zero2.Y;
		}
		else if (zero2.X > 0f)
		{
			baseSpeedMultiplier = zero.X;
			num = zero2.X;
		}
		float num2 = 1f;
		if (MovementStates.IsJumping || MovementStates.IsFalling)
		{
			num2 = MathHelper.Lerp(base.MovementSettings.AirSpeedMultiplier, base.MovementSettings.ComboAirSpeedMultiplier, ((float)_jumpCombo - 1f) * 0.5f);
		}
		float num3 = ((MovementStates.IsInFluid || _fluidJump) ? _averageFluidMovementSettings.HorizontalSpeedMultiplier : 1f);
		float num4 = _gameInstance.InteractionModule.ForEachInteraction((InteractionChain chain, ClientInteraction interaction, float mul) => mul * interaction.Interaction.HorizontalSpeedMultiplier, 1f);
		float horizontalSpeedMultiplier = _gameInstance.LocalPlayer.HorizontalSpeedMultiplier;
		_baseSpeedMultiplier = baseSpeedMultiplier;
		_speedMultiplier = num * num2 * num3 * num4 * horizontalSpeedMultiplier;
		return base.MovementSettings.BaseSpeed * _speedMultiplier;
	}

	private void ComputeSprintForce(ref float wishSpeed)
	{
		if (!(SprintForceDurationLeft < 0f) && !MovementStates.IsWalking && !MovementStates.IsSliding)
		{
			Settings settings = _gameInstance.App.Settings;
			Easing.EasingType easingType = (MovementStates.IsSprinting ? settings.SprintAccelerationEasingType : settings.SprintDecelerationEasingType);
			float num = (MovementStates.IsSprinting ? settings.SprintAccelerationDuration : settings.SprintDecelerationDuration);
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
			_baseSpeedMultiplier = 1.65f;
			_speedMultiplier = ((base.MovementSettings.BaseSpeed <= 0f) ? 1.65f : (wishSpeed / base.MovementSettings.BaseSpeed + 0.4f));
		}
	}

	private float ComputeMomentumLossMultiplier()
	{
		float num = 1f;
		if (MovementStates.IsOnGround || MovementStates.IsSwimming || !(_consecutiveMomentumLoss > 0f))
		{
			return num;
		}
		for (int i = 0; (float)i < _consecutiveMomentumLoss; i++)
		{
			num *= 1f - base.MovementSettings.FallMomentumLoss;
		}
		return num;
	}

	private float GetVerticalMoveSpeed()
	{
		if (MovementStates.IsFlying)
		{
			return base.MovementSettings.VerticalFlySpeed;
		}
		if (MovementStates.IsSwimming)
		{
			if (_gameInstance.LocalPlayer.LookOrientation.Pitch >= 0f)
			{
				return _averageFluidMovementSettings.SwimUpSpeed;
			}
			return _averageFluidMovementSettings.SwimDownSpeed;
		}
		return 1f;
	}

	private Vector3 ApplyExternalForces(Vector3 movementOffset)
	{
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Invalid comparison between Unknown and I4
		if (MovementStates.IsFlying)
		{
			if (_appliedVelocities.Count > 0)
			{
				_appliedVelocities.Clear();
			}
			return movementOffset;
		}
		for (int i = 0; i < _appliedVelocities.Count; i++)
		{
			AppliedVelocity appliedVelocity = _appliedVelocities[i];
			if (appliedVelocity.Velocity.Y + _velocity.Y <= 0f || appliedVelocity.Velocity.Y < 0f)
			{
				appliedVelocity.CanClear = true;
			}
			if (MovementStates.IsOnGround && appliedVelocity.CanClear)
			{
				appliedVelocity.Velocity.Y = 0f;
			}
			movementOffset += appliedVelocity.Velocity;
			float num;
			float num2;
			if (MovementStates.IsOnGround)
			{
				num = appliedVelocity.Config.GroundResistance;
				num2 = appliedVelocity.Config.GroundResistanceMax;
			}
			else
			{
				num = appliedVelocity.Config.AirResistance;
				num2 = appliedVelocity.Config.AirResistanceMax;
			}
			float num3 = num;
			if (num2 >= 0f)
			{
				VelocityThresholdStyle style = appliedVelocity.Config.Style;
				VelocityThresholdStyle val = style;
				if ((int)val != 0)
				{
					if ((int)val != 1)
					{
						throw new ArgumentOutOfRangeException();
					}
					float num4 = appliedVelocity.Velocity.LengthSquared();
					if (num4 < appliedVelocity.Config.Threshold * appliedVelocity.Config.Threshold)
					{
						float num5 = num4 / (appliedVelocity.Config.Threshold * appliedVelocity.Config.Threshold);
						num3 = num * num5 + num2 * (1f - num5);
					}
				}
				else
				{
					float num4 = appliedVelocity.Velocity.Length();
					if (num4 < appliedVelocity.Config.Threshold)
					{
						float num6 = num4 / appliedVelocity.Config.Threshold;
						num3 = num * num6 + num2 * (1f - num6);
					}
				}
			}
			appliedVelocity.Velocity.X *= num3;
			appliedVelocity.Velocity.Z *= num3;
			if ((double)appliedVelocity.Velocity.LengthSquared() < 0.001)
			{
				_appliedVelocities.RemoveAt(i);
				i--;
			}
		}
		return movementOffset;
	}

	private void CheckEntityCollision(Vector3 movement)
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Invalid comparison between Unknown and I4
		if (RaycastDistance <= 0f || MovementStates.IsEntityCollided || !_gameInstance.LocalPlayer.IsTangible())
		{
			return;
		}
		Vector3 rayPosition = Vector3.Add(_gameInstance.LocalPlayer.Position, new Vector3(0f, RaycastHeightOffset, 0f));
		Vector3 vector = new Vector3(movement.X, movement.Y, movement.Z);
		if (!(vector == Vector3.Zero))
		{
			Vector3 rayDirection = Vector3.Normalize(vector);
			if ((int)RaycastMode == 1)
			{
				Vector3 lookOrientation = _gameInstance.LocalPlayer.LookOrientation;
				Quaternion rotation = Quaternion.CreateFromYawPitchRoll(lookOrientation.Yaw, lookOrientation.Pitch, 0f);
				Vector3 vector2 = Vector3.Transform(Vector3.Forward, rotation);
				rayPosition = new Vector3(_gameInstance.LocalPlayer.Position.X, _gameInstance.LocalPlayer.Position.Y + _gameInstance.LocalPlayer.EyeOffset + RaycastHeightOffset, _gameInstance.LocalPlayer.Position.Z);
				rayDirection = vector2;
			}
			_gameInstance.HitDetection.RaycastEntity(rayPosition, rayDirection, RaycastDistance, checkOnlyTangibleEntities: false, out var entityHitData);
			if (entityHitData.Entity != null)
			{
				MovementStates.IsEntityCollided = true;
			}
		}
	}

	private void ComputeFallEffect()
	{
		if (!_gameInstance.IsBuilderModeEnabled())
		{
			if (_fallEffectDurationLeft > 0f)
			{
				_fallEffectDurationLeft -= 1f / 60f;
			}
			double num = 0f - _velocity.Y;
			if (num > 18.0 && _wasFalling && !MovementStates.IsFalling)
			{
				double num2 = (System.Math.Pow(0.75 * (num - 18.0), 2.0) + 10.0) / 100.0;
				_fallEffectDurationLeft = base.MovementSettings.FallEffectDuration;
				_fallEffectSpeedMultiplier = System.Math.Max((float)(1.0 - num2), 0.3f);
				_fallEffectJumpForce = base.MovementSettings.FallJumpForce;
				_gameInstance.AudioModule.PlayLocalSoundEvent("PLAYER_LAND_PENALTY_MAJOR");
			}
			_fallEffectToApply = false;
		}
	}

	private void DoMoveCycle(Vector3 offset)
	{
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		Input input = _gameInstance.Input;
		PlayerEntity localPlayer = _gameInstance.LocalPlayer;
		Vector3 size = localPlayer.Hitbox.GetSize();
		Vector3 checkPos = localPlayer.Position;
		float num = ((MovementStates.IsFlying || MovementStates.IsOnGround || MovementStates.IsSwimming) ? 0.625f : (5f / 32f));
		bool flag = false;
		bool flag2 = false;
		_previousAutoJumpHeightShift = _nextAutoJumpHeightShift;
		checkPos.Y += offset.Y;
		HitDetection.CollisionHitData hitData;
		bool flag3 = CheckCollision(checkPos, offset, HitDetection.CollisionAxis.Y, out hitData);
		if (MovementStates.IsOnGround && offset.Y < 0f)
		{
			if (!flag3)
			{
				MovementStates.IsOnGround = false;
			}
			else
			{
				checkPos.Y -= offset.Y;
			}
		}
		else if (flag3)
		{
			if (offset.Y <= 0f)
			{
				MovementStates.IsOnGround = true;
				checkPos.Y = hitData.Limit.Y;
				if (_gameInstance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)5))
				{
					CheckAndSetRollingState();
				}
			}
			else
			{
				MovementStates.IsOnGround = false;
				_jumpCombo = 0;
				checkPos.Y -= offset.Y;
			}
			foreach (AppliedVelocity appliedVelocity in _appliedVelocities)
			{
				appliedVelocity.Velocity.Y = 0f;
			}
			_velocity.Y = 0f;
		}
		else
		{
			MovementStates.IsOnGround = false;
		}
		int num2 = 0;
		MovementStates.IsClimbing = false;
		_collisionForward = (_collisionBackward = (_collisionLeft = (_collisionRight = false)));
		if (MovementStates.IsIdle && CheckEntityCollision(checkPos, offset, out var _, out var entityCollided))
		{
			Vector3 vector = checkPos - entityCollided.Position;
			vector.Normalize();
			vector *= base.MovementSettings.CollisionExpulsionForce;
			offset.X = vector.X;
			offset.Z = vector.Z;
		}
		HitDetection.CollisionHitData hitData2;
		if (offset.X != 0f && !MovementStates.IsMantling)
		{
			checkPos.X += offset.X;
			bool flag4 = CheckCollision(checkPos, offset, HitDetection.CollisionAxis.X, out hitData);
			ClientHitboxCollisionConfig hitboxCollisionConfig2;
			Entity entityCollided2;
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
				bool flag5 = false;
				Vector3 zero = Vector3.Zero;
				num2 = _gameInstance.MapModule.GetBlock((int)System.Math.Floor(hitData.Limit.X + offset.X), (int)System.Math.Floor(checkPos.Y), (int)System.Math.Floor(checkPos.Z), 1);
				flag5 = _gameInstance.MapModule.ClientBlockTypes[num2].MovementSettings.IsClimbable;
				if (flag5)
				{
					BlockHitbox blockHitbox = _gameInstance.ServerSettings.BlockHitboxes[_gameInstance.MapModule.ClientBlockTypes[num2].HitboxType];
					zero = new Vector3((int)System.Math.Floor(hitData.Limit.X + offset.X), (int)System.Math.Floor(checkPos.Y), (int)System.Math.Floor(checkPos.Z));
					num2 = _gameInstance.MapModule.GetBlock((int)zero.X, (int)zero.Y, (int)zero.Z, 1);
					float num3 = System.Math.Abs((hitData.Limit.X + offset.X) % 1f);
					if (hitData.Limit.X < 0f)
					{
						num3 = 1f - num3;
					}
					float num4 = checkPos.Z % 1f;
					if (num4 < 0f)
					{
						num4 = 1f + num4;
					}
					flag5 = num3 >= blockHitbox.BoundingBox.Min.X && num3 <= blockHitbox.BoundingBox.Max.X && num4 >= blockHitbox.BoundingBox.Min.Z && num4 <= blockHitbox.BoundingBox.Max.Z;
				}
				else
				{
					zero = new Vector3((int)System.Math.Floor(checkPos.X), (int)System.Math.Floor(checkPos.Y), (int)System.Math.Floor(checkPos.Z));
					num2 = _gameInstance.MapModule.GetBlock((int)zero.X, (int)zero.Y, (int)zero.Z, 1);
					ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[num2];
					if (clientBlockType.MovementSettings.IsClimbable)
					{
						BlockHitbox blockHitbox2 = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType];
						float num5 = checkPos.Z % 1f;
						if (num5 < 0f)
						{
							num5 = 1f + num5;
						}
						flag5 = ((offset.X < 0f && blockHitbox2.BoundingBox.Min.X == 0f) || (offset.X > 0f && blockHitbox2.BoundingBox.Max.X == 1f)) && num5 >= blockHitbox2.BoundingBox.Min.Z && num5 <= blockHitbox2.BoundingBox.Max.Z;
					}
				}
				if (!MovementStates.IsOnGround)
				{
					float num6 = float.PositiveInfinity;
					if (_gameInstance.HitDetection.RaycastBlock(checkPos, Vector3.Down, FallRaycastOptions, out _groundHit))
					{
						num6 = _groundHit.Distance;
					}
					if (_gameInstance.HitDetection.RaycastBlock(_gameInstance.LocalPlayer.Position, Vector3.Down, FallRaycastOptions, out _groundHit))
					{
						num6 = System.Math.Min(num6, _groundHit.Distance);
					}
					if (num6 < 0.375f)
					{
						num = 0.625f;
					}
				}
				bool canReach = hitData.Limit.Y > checkPos.Y && hitData.Limit.Y - checkPos.Y <= num;
				canReach = CanJumpObstacle(canReach, hitData, checkPos, new Vector3(hitData.Limit.X + offset.X, checkPos.Y, checkPos.Z), new Vector2(hitData.Limit.X + offset.X - checkPos.X, 0f), 90f);
				if (!MovementStates.IsClimbing && canReach && (MovementStates.IsFlying || MovementStates.IsSwimming || offset.Y < 0f))
				{
					float y = checkPos.Y;
					checkPos.Y = hitData.Limit.Y;
					if (CheckCollision(checkPos, offset, HitDetection.CollisionAxis.X, out hitData2))
					{
						if (offset.X <= 0f)
						{
							checkPos.X = hitData.Limit.X + size.X * 0.5f + 0.0001f;
						}
						else
						{
							checkPos.X = hitData.Limit.X - size.X * 0.5f - 0.0001f;
						}
						checkPos.Y = y;
					}
					else
					{
						flag = true;
						_autoJumpHeight = hitData.Overlap.Y;
						checkPos.Y = hitData.Limit.Y + 0.0001f;
					}
				}
				else if (flag5 && !MovementStates.IsClimbing)
				{
					_collisionLeft = offset.X <= 0f;
					_collisionRight = !_collisionLeft;
					if (_collisionLeft)
					{
						checkPos.X = hitData.Limit.X + size.Z * 0.5f + 0.0001f;
					}
					else
					{
						checkPos.X = hitData.Limit.X - size.Z * 0.5f - 0.0001f;
					}
					ClientBlockType clientBlockType2 = _gameInstance.MapModule.ClientBlockTypes[num2];
					float num7 = 0f;
					if (input.IsBindingHeld(inputBindings.MoveForwards))
					{
						num7 = base.MovementSettings.ClimbSpeed * clientBlockType2.MovementSettings.ClimbUpSpeedMultiplier;
					}
					else if (MovementStates.IsCrouching || input.IsBindingHeld(inputBindings.MoveBackwards))
					{
						num7 = (0f - base.MovementSettings.ClimbSpeed) * clientBlockType2.MovementSettings.ClimbDownSpeedMultiplier;
					}
					checkPos.Y += num7;
					if (CheckCollision(checkPos, new Vector3(0f, num7, 0f), HitDetection.CollisionAxis.Y, out hitData2))
					{
						if (input.IsBindingHeld(inputBindings.MoveForwards))
						{
							checkPos.Y = hitData2.Limit.Y - size.Y - 0.0001f;
						}
						else
						{
							checkPos.Y = hitData2.Limit.Y + 0.0001f;
							MovementStates.IsOnGround = true;
						}
						offset.Y = 0f;
					}
					MovementStates.IsClimbing = true;
					flag2 = true;
					_climbingBlockPosition = zero;
					_climbingBlockId = num2;
				}
				else if (hitData.Overlap.X >= 0f)
				{
					_collisionLeft = offset.X <= 0f;
					_collisionRight = !_collisionLeft;
					if (_collisionLeft)
					{
						checkPos.X = hitData.Limit.X + size.X * 0.5f + 0.0001f;
					}
					else
					{
						checkPos.X = hitData.Limit.X - size.X * 0.5f - 0.0001f;
					}
					_velocity.X = 0f;
				}
			}
			else if (!MovementStates.IsIdle && CheckEntityCollision(checkPos, offset, out hitboxCollisionConfig2, out entityCollided2))
			{
				ComputeHitboxCollisionOffsetMovement(hitboxCollisionConfig2, ref checkPos, offset, entityCollided2, (Axis)0);
			}
			else if (CheckMantling(checkPos))
			{
				Mantle(ref checkPos);
				localPlayer.SetPositionTeleport(checkPos);
			}
		}
		if (offset.Z != 0f && !MovementStates.IsMantling)
		{
			checkPos.Z += offset.Z;
			ClientHitboxCollisionConfig hitboxCollisionConfig3;
			Entity entityCollided3;
			if (CheckCollision(checkPos, offset, HitDetection.CollisionAxis.Z, out hitData))
			{
				if (offset.Z > 0f && _requestedVelocity.Z > 0f)
				{
					_requestedVelocity.Z = 0f;
				}
				else if (offset.Z < 0f && _requestedVelocity.Z < 0f)
				{
					_requestedVelocity.Z = 0f;
				}
				bool flag6 = flag2;
				Vector3 climbingBlockPosition = Vector3.Zero;
				if (!flag6)
				{
					climbingBlockPosition = new Vector3((int)System.Math.Floor(checkPos.X), (int)System.Math.Floor(checkPos.Y), (int)System.Math.Floor(hitData.Limit.Z + offset.Z));
					num2 = _gameInstance.MapModule.GetBlock((int)climbingBlockPosition.X, (int)climbingBlockPosition.Y, (int)climbingBlockPosition.Z, 1);
					flag6 = _gameInstance.MapModule.ClientBlockTypes[num2].MovementSettings.IsClimbable;
					if (flag6)
					{
						BlockHitbox blockHitbox3 = _gameInstance.ServerSettings.BlockHitboxes[_gameInstance.MapModule.ClientBlockTypes[num2].HitboxType];
						float num8 = System.Math.Abs((hitData.Limit.Z + offset.Z) % 1f);
						if (hitData.Limit.Z < 0f)
						{
							num8 = 1f - num8;
						}
						float num9 = checkPos.X % 1f;
						if (num9 < 0f)
						{
							num9 = 1f + num9;
						}
						flag6 = num8 >= blockHitbox3.BoundingBox.Min.Z && num8 <= blockHitbox3.BoundingBox.Max.Z && num9 >= blockHitbox3.BoundingBox.Min.X && num9 <= blockHitbox3.BoundingBox.Max.X;
					}
					else
					{
						climbingBlockPosition = new Vector3((int)System.Math.Floor(checkPos.X), (int)System.Math.Floor(checkPos.Y), (int)System.Math.Floor(checkPos.Z));
						num2 = _gameInstance.MapModule.GetBlock((int)climbingBlockPosition.X, (int)climbingBlockPosition.Y, (int)climbingBlockPosition.Z, 1);
						ClientBlockType clientBlockType3 = _gameInstance.MapModule.ClientBlockTypes[num2];
						if (clientBlockType3.MovementSettings.IsClimbable)
						{
							BlockHitbox blockHitbox4 = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType3.HitboxType];
							float num10 = checkPos.X % 1f;
							if (num10 < 0f)
							{
								num10 = 1f + num10;
							}
							flag6 = ((offset.Z < 0f && blockHitbox4.BoundingBox.Min.Z == 0f) || (offset.Z > 0f && blockHitbox4.BoundingBox.Max.Z == 1f)) && num10 >= blockHitbox4.BoundingBox.Min.X && num10 <= blockHitbox4.BoundingBox.Max.X;
						}
					}
				}
				if (!MovementStates.IsOnGround)
				{
					float num11 = float.PositiveInfinity;
					if (_gameInstance.HitDetection.RaycastBlock(checkPos, Vector3.Down, FallRaycastOptions, out _groundHit))
					{
						num11 = _groundHit.Distance;
					}
					if (_gameInstance.HitDetection.RaycastBlock(_gameInstance.LocalPlayer.Position, Vector3.Down, FallRaycastOptions, out _groundHit))
					{
						num11 = System.Math.Min(num11, _groundHit.Distance);
					}
					if (num11 < 0.375f)
					{
						num = 0.625f;
					}
				}
				bool canReach2 = hitData.Limit.Y > checkPos.Y && hitData.Limit.Y - checkPos.Y < num;
				canReach2 = CanJumpObstacle(canReach2, hitData, checkPos, new Vector3(checkPos.X, checkPos.Y, hitData.Limit.Z + offset.Z), new Vector2(0f, hitData.Limit.Z + offset.Z - checkPos.Z), -90f);
				if (!MovementStates.IsClimbing && canReach2 && (MovementStates.IsFlying || MovementStates.IsSwimming || offset.Y < 0f))
				{
					float y2 = checkPos.Y;
					checkPos.Y = hitData.Limit.Y;
					if (CheckCollision(checkPos, offset, HitDetection.CollisionAxis.Z, out hitData2))
					{
						if (offset.Z <= 0f)
						{
							checkPos.Z = hitData.Limit.Z + size.Z * 0.5f + 0.0001f;
						}
						else
						{
							checkPos.Z = hitData.Limit.Z - size.Z * 0.5f - 0.0001f;
						}
						checkPos.Y = y2;
					}
					else
					{
						flag = true;
						_autoJumpHeight = hitData.Overlap.Y;
						checkPos.Y = hitData.Limit.Y + 0.0001f;
					}
				}
				else if (flag6 && !flag2)
				{
					_collisionForward = offset.Z <= 0f;
					_collisionBackward = !_collisionForward;
					if (_collisionForward)
					{
						checkPos.Z = hitData.Limit.Z + size.Z * 0.5f + 0.0001f;
					}
					else
					{
						checkPos.Z = hitData.Limit.Z - size.Z * 0.5f - 0.0001f;
					}
					ClientBlockType clientBlockType4 = _gameInstance.MapModule.ClientBlockTypes[num2];
					float num12 = 0f;
					if (input.IsBindingHeld(inputBindings.MoveForwards))
					{
						num12 = base.MovementSettings.ClimbSpeed * clientBlockType4.MovementSettings.ClimbUpSpeedMultiplier;
					}
					else if (MovementStates.IsCrouching || input.IsBindingHeld(inputBindings.MoveBackwards))
					{
						num12 = (0f - base.MovementSettings.ClimbSpeed) * clientBlockType4.MovementSettings.ClimbDownSpeedMultiplier;
					}
					checkPos.Y += num12;
					if (CheckCollision(checkPos, new Vector3(0f, num12, 0f), HitDetection.CollisionAxis.Y, out hitData2))
					{
						if (input.IsBindingHeld(inputBindings.MoveForwards))
						{
							checkPos.Y = hitData2.Limit.Y - size.Y - 0.0001f;
						}
						else
						{
							checkPos.Y = hitData2.Limit.Y + 0.0001f;
							MovementStates.IsOnGround = true;
						}
						offset.Y = 0f;
					}
					MovementStates.IsClimbing = true;
					_climbingBlockPosition = climbingBlockPosition;
					_climbingBlockId = num2;
				}
				else if (hitData.Overlap.Z >= 0f)
				{
					_collisionForward = offset.Z <= 0f;
					_collisionBackward = !_collisionForward;
					if (_collisionForward)
					{
						checkPos.Z = hitData.Limit.Z + size.Z * 0.5f + 0.0001f;
					}
					else
					{
						checkPos.Z = hitData.Limit.Z - size.Z * 0.5f - 0.0001f;
					}
					_velocity.Z = 0f;
				}
			}
			else if (!MovementStates.IsIdle && CheckEntityCollision(checkPos, offset, out hitboxCollisionConfig3, out entityCollided3))
			{
				ComputeHitboxCollisionOffsetMovement(hitboxCollisionConfig3, ref checkPos, offset, entityCollided3, (Axis)2);
			}
			else if (CheckMantling(checkPos))
			{
				Mantle(ref checkPos);
				localPlayer.SetPositionTeleport(checkPos);
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
		localPlayer.SetPosition(checkPos);
	}

	private bool CheckEntityCollision(Vector3 position, Vector3 moveOffset, out ClientHitboxCollisionConfig hitboxCollisionConfig, out Entity entityCollided)
	{
		hitboxCollisionConfig = null;
		entityCollided = null;
		if (!_gameInstance.LocalPlayer.IsTangible())
		{
			return false;
		}
		BoundingBox hitbox = _gameInstance.LocalPlayer.Hitbox;
		BoundingBox boundingBox = new BoundingBox(hitbox.Min, hitbox.Max);
		boundingBox.Translate(new Vector3(position.X + moveOffset.X, position.Y + 0.0001f + moveOffset.Y, position.Z + moveOffset.Z));
		EntityStoreModule entityStoreModule = _gameInstance.EntityStoreModule;
		Entity[] allEntities = entityStoreModule.GetAllEntities();
		BoundingBox box = default(BoundingBox);
		for (int i = entityStoreModule.PlayerEntityLocalId + 1; i < entityStoreModule.GetEntitiesCount(); i++)
		{
			Entity entity = allEntities[i];
			if (entity.HitboxCollisionConfigIndex == -1 || !entity.IsTangible())
			{
				continue;
			}
			ClientHitboxCollisionConfig clientHitboxCollisionConfig = _gameInstance.ServerSettings.HitboxCollisionConfigs[entity.HitboxCollisionConfigIndex];
			if (clientHitboxCollisionConfig.CollisionType == ClientHitboxCollisionConfig.ClientCollisionType.Hard)
			{
				continue;
			}
			box.Min = entity.Hitbox.Min;
			box.Max = entity.Hitbox.Max;
			if (boundingBox.IntersectsExclusive(box, entity.Position.X, entity.Position.Y, entity.Position.Z))
			{
				if (hitboxCollisionConfig == null || clientHitboxCollisionConfig.SoftCollisionOffsetRatio > hitboxCollisionConfig.SoftCollisionOffsetRatio)
				{
					hitboxCollisionConfig = clientHitboxCollisionConfig;
					entityCollided = entity;
				}
				if (_gameInstance.EntityStoreModule.DebugInfoNeedsDrawing)
				{
					CollidedEntities.Add(entity.NetworkId);
				}
			}
		}
		return hitboxCollisionConfig != null;
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
		if (!MovementStates.IsOnGround || MovementStates.IsCrouching)
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
		float num = MathHelper.WrapAngle(_gameInstance.LocalPlayer.LookOrientation.Y + MathHelper.ToRadians(angleOffset));
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
		_jumpObstacleDurationLeft = (MovementStates.IsSprinting ? base.MovementSettings.AutoJumpObstacleSprintEffectDuration : base.MovementSettings.AutoJumpObstacleEffectDuration);
		_gameInstance.LocalPlayer.SetServerAnimation(MovementStates.IsSprinting ? "StepSprint" : (MovementStates.IsWalking ? "StepWalk" : "StepRun"), (AnimationSlot)0, 0f, storeCurrentAnimationId: true);
		return true;
	}

	private void ComputeHitboxCollisionOffsetMovement(ClientHitboxCollisionConfig hitboxCollisionConfig, ref Vector3 checkPos, Vector3 offset, Entity entityCollided, Axis axis)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Invalid comparison between Unknown and I4
		ClientHitboxCollisionConfig.ClientCollisionType collisionType = hitboxCollisionConfig.CollisionType;
		ClientHitboxCollisionConfig.ClientCollisionType clientCollisionType = collisionType;
		if (clientCollisionType == ClientHitboxCollisionConfig.ClientCollisionType.Soft)
		{
			if ((int)axis == 0)
			{
				checkPos.X += 0f - offset.X + offset.X * hitboxCollisionConfig.SoftCollisionOffsetRatio;
			}
			else if ((int)axis == 2)
			{
				checkPos.Z += 0f - offset.Z + offset.Z * hitboxCollisionConfig.SoftCollisionOffsetRatio;
			}
		}
	}

	private void UpdateViewModifiers()
	{
		_previousCrouchHeightShift = _nextCrouchHeightShift;
		if (_nextCrouchHeightShift != _targetCrouchHeightShift)
		{
			float step = ((MovementStates.IsOnGround || MovementStates.IsClimbing) ? 0.1f : 0.055f);
			_nextCrouchHeightShift = MathHelper.Step(base.CrouchHeightShift, _targetCrouchHeightShift, step);
		}
		Settings settings = _gameInstance.App.Settings;
		float num = settings.FieldOfView;
		if (settings.SprintFovEffect && !_gameInstance.IsBuilderModeEnabled() && (MovementStates.IsSprinting || (SprintForceDurationLeft > 0f && !MovementStates.IsWalking && !MovementStates.IsIdle)))
		{
			num *= 1f + (settings.SprintFovIntensity - 1f) * ((!(SprintForceDurationLeft > 0f)) ? 1f : (MovementStates.IsSprinting ? SprintForceProgress : (1f - SprintForceProgress)));
		}
		if (MovementStates.IsInFluid || _fluidJump)
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
		if (MovementStates.IsMantling || MovementStates.IsRolling)
		{
			MovementStates.IsIdle = false;
			MovementStates.IsHorizontalIdle = false;
		}
		else
		{
			MovementStates.IsIdle = System.Math.Abs(_velocity.Y) <= 1E-07f && (_wishDirection == Vector2.Zero || (System.Math.Abs(_velocity.X) <= 1E-07f && System.Math.Abs(_velocity.Z) <= 1E-07f));
			MovementStates.IsHorizontalIdle = _wishDirection == Vector2.Zero;
		}
	}

	private bool IsDisallowedInteraction(KeyValuePair<int, InteractionChain> chainPair)
	{
		return !chainPair.Value.InitialRootInteraction.Tags.Contains(_gameInstance.ServerSettings.GetServerTag("Allows=Movement"));
	}

	private bool CheckMantling(Vector3 position)
	{
		if (MovementStates.IsFlying)
		{
			return false;
		}
		if (!_gameInstance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)2))
		{
			return false;
		}
		if (!_gameInstance.App.Settings.Mantling)
		{
			return false;
		}
		if (_wishDirection.Y <= 0f)
		{
			return false;
		}
		if (_velocity.Y < _gameInstance.App.Settings.MinVelocityMantling || _velocity.Y > _gameInstance.App.Settings.MaxVelocityMantling)
		{
			return false;
		}
		if ((System.Math.Abs(position.Y) + 1.8f) % 1f < _gameInstance.App.Settings.MantleBlockHeight)
		{
			return false;
		}
		if (_gameInstance.InteractionModule.Chains.Count > 0 && _gameInstance.InteractionModule.Chains.Any(IsDisallowedInteraction))
		{
			return false;
		}
		if (MovementStates.IsCrouching || MovementStates.IsSliding)
		{
			return false;
		}
		_mantleOffset = Vector3.Zero;
		float num = position.X;
		float num2 = (float)System.Math.Floor(position.Y) - 1f;
		float num3 = position.Z;
		if (!IsPositionGap(num, num2, num3))
		{
			return false;
		}
		float num4 = MathHelper.WrapAngle(_gameInstance.LocalPlayer.LookOrientation.Y + MathHelper.ToRadians(90f));
		Vector2 vector = new Vector2((float)System.Math.Cos(num4), (float)System.Math.Sin(num4));
		vector.Normalize();
		if (System.Math.Abs(vector.X) > System.Math.Abs(vector.Y))
		{
			if (vector.X > 0f)
			{
				num += 0.5f;
				_mantleOffset.X = 0.5f;
			}
			else
			{
				num -= 0.5f;
				_mantleOffset.X = -0.5f;
			}
			float num5 = System.Math.Abs(num3);
			if (num5 % 1f < 0.3f && !IsPositionGap(num, num2 + 2f + 1f, num3 + 1f))
			{
				_mantleOffset.Z -= 0.3f - num5 % 1f;
			}
			else if (num5 % 1f > 0.7f && !IsPositionGap(num, num2 + 2f + 1f, num3 - 1f))
			{
				_mantleOffset.Z -= 0.7f - num5 % 1f;
			}
		}
		else
		{
			if (vector.Y > 0f)
			{
				num3 -= 0.5f;
				_mantleOffset.Z = -0.5f;
			}
			else
			{
				num3 += 0.5f;
				_mantleOffset.Z = 0.5f;
			}
			float num6 = System.Math.Abs(num);
			if (num6 % 1f < 0.3f && !IsPositionGap(num - 1f, num2 + 2f + 1f, num3))
			{
				_mantleOffset.X = 0.3f - num6 % 1f;
			}
			else if (num6 % 1f > 0.7f && !IsPositionGap(num + 1f, num2 + 2f + 1f, num3))
			{
				_mantleOffset.X = 0.7f - num6 % 1f;
			}
		}
		if (IsPositionGap(num, num2 + 2f, num3))
		{
			return false;
		}
		if (!IsPositionGap(num, num2 + 2f + 1f, num3))
		{
			return false;
		}
		if (!IsPositionGap(num, num2 + 2f + 2f, num3))
		{
			return false;
		}
		_mantleOffset.Y -= 1f - GetBlockHeight(num, num2 + 2f, num3);
		return true;
	}

	private float GetBlockHeight(float posX, float posY, float posZ)
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
				BlockHitbox blockHitbox = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType];
				return blockHitbox.BoundingBox.Max.Y;
			}
		}
		return 0f;
	}

	private void Mantle(ref Vector3 checkPos)
	{
		MovementStates.IsMantling = true;
		MovementStates.IsJumping = false;
		MovementStates.IsFalling = false;
		MovementStates.IsOnGround = false;
		checkPos.Y = (float)System.Math.Floor(checkPos.Y) + 2f;
		checkPos += _mantleOffset;
		_velocity = Vector3.Zero;
		_mantleDurationLeft = 1f;
		MantleCameraOffset.X = 0f - _mantleOffset.X;
		MantleCameraOffset.Y = _gameInstance.App.Settings.MantlingCameraOffsetY;
		MantleCameraOffset.Z = 0f - _mantleOffset.Z;
		_previousMantleCameraOffset = MantleCameraOffset;
		_nextMantleCameraOffset = MantleCameraOffset;
		_gameInstance.AudioModule.PlayLocalSoundEvent("MANTLING");
	}

	private void UpdateMantle()
	{
		_mantleDurationLeft -= 1f / 60f;
		_previousMantleCameraOffset = _nextMantleCameraOffset;
		_nextMantleCameraOffset.X = MathHelper.ConvertToNewRange(_mantleDurationLeft, 1f, 0f, 0f - _mantleOffset.X, 0f);
		_nextMantleCameraOffset.Z = MathHelper.ConvertToNewRange(_mantleDurationLeft, 1f, 0f, 0f - _mantleOffset.Z, 0f);
		_nextMantleCameraOffset.Y = MathHelper.ConvertToNewRange(_mantleDurationLeft, 1f, 0f, _gameInstance.App.Settings.MantlingCameraOffsetY, 0f);
		if (!(_mantleDurationLeft > 0f))
		{
			MovementStates.IsMantling = false;
			MantleCameraOffset = Vector3.Zero;
			_previousMantleCameraOffset = Vector3.Zero;
			_nextMantleCameraOffset = Vector3.Zero;
		}
	}

	private void CheckAndSetRollingState()
	{
		if (!_wasOnGround && MovementStates.IsOnGround)
		{
			InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
			Input input = _gameInstance.Input;
			if (input.IsBindingHeld(inputBindings.Crouch) && !(_previousFallingVelocity > 0f - base.MovementSettings.MinFallSpeedToEngageRoll) && (_gameInstance.InteractionModule.Chains.Count <= 0 || !_gameInstance.InteractionModule.Chains.Any(IsDisallowedInteraction)))
			{
				MovementStates.IsRolling = true;
				InitializeRollingForceValues();
			}
		}
	}

	private void InitializeRollingForceValues()
	{
		if (!MovementStates.IsRolling)
		{
			_rollForceDurationLeft = -1f;
			return;
		}
		_rollForceInitialSpeed = base.MovementSettings.BaseSpeed * base.MovementSettings.RollStartSpeedModifier;
		_curRollSpeedMultiplier = base.MovementSettings.RollStartSpeedModifier;
		_fallEffectDurationLeft = 0f;
		if (MovementStates.IsRolling)
		{
			if (_rollForceInitialSpeed < base.MovementSettings.BaseSpeed)
			{
				_rollForceInitialSpeed = base.MovementSettings.BaseSpeed;
			}
			_rollForceDurationLeft = base.MovementSettings.RollTimeToComplete;
		}
		else
		{
			_rollForceDurationLeft = base.MovementSettings.RollTimeToComplete;
		}
		if (_rollForceDurationLeft <= 0f)
		{
			_rollForceDurationLeft = -1f;
			MovementStates.IsRolling = false;
		}
	}

	private void ComputeAndUpdateRollingForce()
	{
		if (!(_rollForceDurationLeft < 0f) && MovementStates.IsRolling)
		{
			Settings settings = _gameInstance.App.Settings;
			Easing.EasingType sprintDecelerationEasingType = settings.SprintDecelerationEasingType;
			float rollTimeToComplete = base.MovementSettings.RollTimeToComplete;
			if (rollTimeToComplete == _rollForceDurationLeft && base.MovementSettings.BaseSpeed > 0f)
			{
				float value = _rollForceInitialSpeed * rollTimeToComplete / (base.MovementSettings.BaseSpeed * base.MovementSettings.RollStartSpeedModifier - base.MovementSettings.BaseSpeed * base.MovementSettings.RollExitSpeedModifier);
				_rollForceDurationLeft = MathHelper.Clamp(value, 0f, rollTimeToComplete);
			}
			_rollForceProgress = Easing.Ease(sprintDecelerationEasingType, rollTimeToComplete - _rollForceDurationLeft, 0f, 1f, rollTimeToComplete);
			_baseSpeedMultiplier = 1f;
			_curRollSpeedMultiplier = base.MovementSettings.RollStartSpeedModifier - (base.MovementSettings.RollStartSpeedModifier - base.MovementSettings.RollExitSpeedModifier) * _rollForceProgress;
			UpdateRollingDurationLeft();
		}
	}

	private void UpdateRollingDurationLeft()
	{
		_rollForceDurationLeft -= 1f / 60f;
		if (_rollForceDurationLeft <= 0f)
		{
			_rollForceDurationLeft = 0f;
			MovementStates.IsRolling = false;
		}
	}

	private void UpdateSliding(bool canMove, ref InputFrame input)
	{
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		float num = (float)System.Math.Sqrt(_velocity.X * _velocity.X + _velocity.Z * _velocity.Z);
		if (!MovementStates.IsSliding)
		{
			MovementStates.IsSliding = CanSlide(canMove, ref input);
		}
		else
		{
			MovementStates.IsSliding = input.IsBindingHeld(inputBindings.Crouch) && canMove && !(num <= base.MovementSettings.SlideExitSpeed) && MovementStates.IsOnGround;
		}
	}

	private void ComputeSlideForce(ref float wishSpeed)
	{
		if (!(_slideForceDurationLeft < 0f) && MovementStates.IsSliding && MovementStates.IsOnGround)
		{
			wishSpeed = base.MovementSettings.SlideExitSpeed;
			_baseSpeedMultiplier = 0.55f;
			_speedMultiplier = ((base.MovementSettings.BaseSpeed <= 0f) ? 0.55f : (wishSpeed / base.MovementSettings.BaseSpeed + 0.55f));
			Settings settings = _gameInstance.App.Settings;
			Easing.EasingType slideDecelerationEasingType = settings.SlideDecelerationEasingType;
			float slideDecelerationDuration = settings.SlideDecelerationDuration;
			if (slideDecelerationDuration == _slideForceDurationLeft && base.MovementSettings.BaseSpeed > 0f)
			{
				float num = base.MovementSettings.SlideExitSpeed - base.MovementSettings.BaseSpeed;
				float baseSpeed = base.MovementSettings.BaseSpeed;
				float num2 = baseSpeed * base.MovementSettings.ForwardCrouchSpeedMultiplier - _slideForceInitialSpeed;
				float value = num2 * slideDecelerationDuration / num;
				_slideForceDurationLeft = MathHelper.Clamp(value, 0f, slideDecelerationDuration);
			}
			_slideForceProgress = Easing.Ease(slideDecelerationEasingType, slideDecelerationDuration - _slideForceDurationLeft, 0f, 1f, slideDecelerationDuration);
			wishSpeed = _slideForceInitialSpeed + (wishSpeed - _slideForceInitialSpeed) * _slideForceProgress;
			_baseSpeedMultiplier = 0.55f;
			_speedMultiplier = ((base.MovementSettings.BaseSpeed <= 0f) ? 0.55f : (wishSpeed / base.MovementSettings.BaseSpeed + 0.02f));
		}
	}

	private void UpdateSlidingForceValues()
	{
		if (MovementStates.IsSliding == _previousMovementStates.IsSliding)
		{
			return;
		}
		if (MovementStates.IsIdle && !_previousMovementStates.IsIdle)
		{
			_slideForceDurationLeft = -1f;
			return;
		}
		_slideForceInitialSpeed = _lastLateralSpeed;
		if (MovementStates.IsSliding)
		{
			_slideForceDurationLeft = _gameInstance.App.Settings.SlideDecelerationDuration;
		}
		if (_slideForceDurationLeft <= 0f)
		{
			_slideForceDurationLeft = -1f;
		}
	}

	private bool CanSlide(bool canMove, ref InputFrame input)
	{
		if (!canMove)
		{
			return false;
		}
		if (_gameInstance.App.Settings.SlideDecelerationDuration <= 0f)
		{
			return false;
		}
		if (!MovementStates.IsOnGround)
		{
			return false;
		}
		if (MovementStates.IsRolling)
		{
			return false;
		}
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		if (!input.IsBindingHeld(inputBindings.Crouch))
		{
			return false;
		}
		float num = (float)System.Math.Sqrt(_velocity.X * _velocity.X + _velocity.Z * _velocity.Z);
		if (num < base.MovementSettings.MinSlideEntrySpeed)
		{
			return false;
		}
		if (_gameInstance.InteractionModule.Chains.Count > 0 && _gameInstance.InteractionModule.Chains.Any(IsDisallowedInteraction))
		{
			return false;
		}
		return true;
	}
}
