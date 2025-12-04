using System;
using HytaleClient.InGame.Modules.CharacterController;
using HytaleClient.InGame.Modules.Collision;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.InGame.Modules.Camera.Controllers;

internal class FirstPersonCameraController : ICameraController
{
	private const float EdgePadding = 0.01f;

	private const float TransitionCancellationMovePadding = 0.01f;

	private const float TransitionCancellationLookPadding = 0.001f;

	private Entity _entity;

	protected Vector3 _lookAt;

	protected Vector3 _transitionLookAt;

	protected bool _inTransition = false;

	private readonly GameInstance _gameInstance;

	private float _itemWiggleTickAccumulator;

	private Vector2 _itemWiggleAmountAccumulator;

	public float SpeedModifier { get; } = 1f;


	public bool AllowPitchControls => false;

	public bool DisplayCursor => false;

	public bool DisplayReticle => true;

	public bool SkipCharacterPhysics => false;

	public bool IsFirstPerson => true;

	public bool InteractFromEntity => true;

	public virtual Vector3 MovementForceRotation => _gameInstance.LocalPlayer.GetRelativeMovementStates().IsMounting ? _gameInstance.CharacterControllerModule.MovementController.CameraRotation : AttachedTo.LookOrientation;

	public Entity AttachedTo
	{
		get
		{
			return _entity ?? _gameInstance.LocalPlayer;
		}
		set
		{
			_entity = value;
		}
	}

	public Vector3 AttachmentPosition { get; private set; }

	public Vector3 PositionOffset => Vector3.Zero;

	public Vector3 RotationOffset => Vector3.Zero;

	public Vector3 Position => AttachmentPosition + _gameInstance.CameraModule.CameraShakeController.Offset + _gameInstance.CharacterControllerModule.MovementController.MantleCameraOffset;

	public Vector3 Rotation => AttachedTo.LookOrientation + _gameInstance.CharacterControllerModule.MovementController.FirstPersonRotationOffset + _gameInstance.CameraModule.CameraShakeController.Rotation;

	public Vector3 LookAt => _lookAt;

	public bool CanMove => _entity == null || _entity == _gameInstance.LocalPlayer;

	public FirstPersonCameraController(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
	}

	public void Reset(GameInstance gameInstance, ICameraController previousCameraController)
	{
		if (previousCameraController is ThirdPersonCameraController && !(previousCameraController is FreeRotateCameraController))
		{
			_lookAt = (_transitionLookAt = previousCameraController.LookAt);
			_gameInstance.LocalPlayer.LookAt(_transitionLookAt);
			_inTransition = true;
		}
	}

	public void Update(float deltaTime)
	{
		Quaternion rotation = Quaternion.CreateFromYawPitchRoll(Rotation.Yaw, Rotation.Pitch, 0f);
		Vector3 direction = Vector3.Transform(Vector3.Forward, rotation);
		if (_inTransition)
		{
			_gameInstance.LocalPlayer.LookAt(_transitionLookAt);
		}
		Ray ray = new Ray(Position, direction);
		CollisionModule.CombinedOptions options = CollisionModule.CombinedOptions.Default;
		options.Block.IgnoreEmptyCollisionMaterial = false;
		options.Block.IgnoreFluids = true;
		if (_gameInstance.CollisionModule.FindNearestTarget(ref ray, ref options, out var _, out var _, out var result))
		{
			_lookAt = result.GetTarget();
		}
		else
		{
			_lookAt = ray.GetAt(options.RaycastOptions.Distance);
		}
		UpdateAttachmentPosition(deltaTime);
	}

	private void UpdateAttachmentPosition(float deltaTime)
	{
		if (_entity != null)
		{
			AttachmentPosition = _entity.RenderPosition + new Vector3(0f, AttachedTo.EyeOffset, 0f);
			return;
		}
		MovementController movementController = _gameInstance.CharacterControllerModule.MovementController;
		Vector3 value = new Vector3(movementController.FirstPersonPositionOffset.X, movementController.FirstPersonPositionOffset.Y, movementController.FirstPersonPositionOffset.Z);
		Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, AttachedTo.LookOrientation.Y);
		Vector3.Transform(ref value, ref rotation, out value);
		AttachmentPosition = AttachedTo.RenderPosition + new Vector3(0f, AttachedTo.EyeOffset + movementController.CrouchHeightShift, 0f) + value;
		_gameInstance.CameraModule.CameraShakeController.Update(deltaTime, rotation);
	}

	public void ApplyMove(Vector3 movementOffset)
	{
		Vector3 position = _gameInstance.LocalPlayer.Position;
		_gameInstance.CharacterControllerModule.MovementController.ApplyMovementOffset(movementOffset);
		if (_inTransition && Vector3.Distance(position, _gameInstance.LocalPlayer.Position) > 0.01f)
		{
			_inTransition = false;
		}
	}

	public void ApplyLook(float deltaTime, Vector2 lookOffset)
	{
		Vector3 lookOrientation = _gameInstance.LocalPlayer.LookOrientation;
		ref Vector3 lookOrientation2 = ref _gameInstance.LocalPlayer.LookOrientation;
		lookOrientation2.Pitch = MathHelper.Clamp(lookOrientation2.Pitch + lookOffset.X, -1.5607964f, 1.5607964f);
		lookOrientation2.Yaw = MathHelper.WrapAngle(lookOrientation2.Yaw + lookOffset.Y);
		_itemWiggleTickAccumulator += deltaTime;
		_itemWiggleAmountAccumulator.X += (lookOrientation.Pitch - lookOrientation2.Pitch) * 4f;
		_itemWiggleAmountAccumulator.Y += lookOffset.Y * 4f;
		if (_itemWiggleTickAccumulator > 1f / 12f)
		{
			_itemWiggleTickAccumulator = 1f / 12f;
		}
		while (_itemWiggleTickAccumulator >= 1f / 60f)
		{
			_gameInstance.LocalPlayer.ApplyFirstPersonMouseItemWiggle(_itemWiggleAmountAccumulator.Y, _itemWiggleAmountAccumulator.X);
			_itemWiggleAmountAccumulator.X = (_itemWiggleAmountAccumulator.Y = 0f);
			_itemWiggleTickAccumulator -= 1f / 60f;
		}
		float timeFraction = System.Math.Min(_itemWiggleTickAccumulator / (1f / 60f), 1f);
		_gameInstance.LocalPlayer.UpdateClientInterpolationMouseWiggle(timeFraction);
		if (_inTransition && (lookOffset.X > 0.001f || lookOffset.X < -0.001f || lookOffset.Y > 0.001f || lookOffset.Y < -0.001f))
		{
			_inTransition = false;
		}
	}

	public void SetRotation(Vector3 rotation)
	{
		_inTransition = false;
	}

	public void OnMouseInput(SDL_Event evt)
	{
	}
}
