using System;
using HytaleClient.Core;
using HytaleClient.Data.UserSettings;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.InGame.Modules.Camera.Controllers;

internal class FlyCameraController : ICameraController
{
	private const float WalkSpeed = 0.125f;

	private const float RunSpeed = 1.25f;

	private const float MovementSmoothingFactor = 0.7f;

	private Entity _entity;

	private readonly GameInstance _gameInstance;

	private Vector3 _move;

	private Vector3 _previousPosition;

	private Vector3 _nextPosition;

	private bool _isFirstUsage = true;

	private float _tickAccumulator;

	public float SpeedModifier => CanMove ? 1f : 3f;

	public bool AllowPitchControls => true;

	public bool DisplayCursor => false;

	public bool DisplayReticle => false;

	public bool SkipCharacterPhysics => false;

	public bool IsFirstPerson => false;

	public bool InteractFromEntity => false;

	public Vector3 MovementForceRotation => CanMove ? new Vector3(0f, _gameInstance.LocalPlayer.LookOrientation.Yaw, 0f) : Rotation;

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

	public Vector3 AttachmentPosition => AttachedTo.RenderPosition + new Vector3(0f, AttachedTo.EyeOffset, 0f);

	public Vector3 PositionOffset => Vector3.Zero;

	public Vector3 RotationOffset => Vector3.Zero;

	public Vector3 Position { get; private set; }

	public Vector3 Rotation { get; private set; }

	public Vector3 LookAt { get; private set; }

	public bool CanMove { get; private set; }

	public FlyCameraController(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
	}

	public void Reset(GameInstance gameInstance, ICameraController previousCameraController)
	{
		if (_isFirstUsage)
		{
			Position = previousCameraController.Position;
			Rotation = previousCameraController.Rotation;
			_isFirstUsage = false;
		}
		_previousPosition = Position;
		_nextPosition = Position;
		_move = Vector3.Zero;
	}

	public void ResetPosition()
	{
		Position = AttachmentPosition;
		Rotation = AttachedTo.LookOrientation;
		_previousPosition = Position;
		_nextPosition = Position;
		_move = Vector3.Zero;
	}

	public void Update(float deltaTime)
	{
		if (CanMove)
		{
			return;
		}
		Quaternion rotation = Quaternion.CreateFromYawPitchRoll(Rotation.Yaw, Rotation.Pitch, 0f);
		Vector3 vector = Vector3.Transform(Vector3.Forward, rotation);
		Vector3 vector2 = Vector3.Transform(Vector3.Right, rotation);
		Input input = _gameInstance.Input;
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		float num = _gameInstance.CharacterControllerModule.MovementController.SpeedMultiplier * (input.IsBindingHeld(inputBindings.Sprint) ? 1.25f : 0.125f);
		_tickAccumulator += deltaTime;
		if (_tickAccumulator > 1f / 12f)
		{
			_tickAccumulator = 1f / 12f;
		}
		while (_tickAccumulator >= 1f / 60f)
		{
			if (input.IsBindingHeld(inputBindings.MoveForwards))
			{
				_move += vector * num;
			}
			if (input.IsBindingHeld(inputBindings.MoveBackwards))
			{
				_move -= vector * num;
			}
			if (input.IsBindingHeld(inputBindings.StrafeRight))
			{
				_move += vector2 * num;
			}
			if (input.IsBindingHeld(inputBindings.StrafeLeft))
			{
				_move -= vector2 * num;
			}
			if (input.IsBindingHeld(inputBindings.Crouch))
			{
				_move -= Vector3.Up * num;
			}
			if (input.IsBindingHeld(inputBindings.Jump))
			{
				_move += Vector3.Up * num;
			}
			_move *= 0.7f;
			_previousPosition = _nextPosition;
			_nextPosition += _move;
			_tickAccumulator -= 1f / 60f;
		}
		float amount = System.Math.Min(_tickAccumulator / (1f / 60f), 1f);
		Position = Vector3.Lerp(_previousPosition, _nextPosition, amount);
	}

	public void ApplyMove(Vector3 movementOffset)
	{
		_gameInstance.CharacterControllerModule.MovementController.ApplyMovementOffset(movementOffset);
	}

	public void ApplyLook(float deltaTime, Vector2 lookOffset)
	{
		if (CanMove)
		{
			ApplyLookPlayer(lookOffset);
			return;
		}
		Vector3 rotation = Rotation;
		Rotation = new Vector3(MathHelper.Clamp(rotation.X + lookOffset.X, -(float)System.Math.PI / 2f, (float)System.Math.PI / 2f), MathHelper.WrapAngle(rotation.Y + lookOffset.Y), rotation.Roll);
	}

	private void ApplyLookPlayer(Vector2 lookOffset)
	{
		PlayerEntity localPlayer = _gameInstance.LocalPlayer;
		Vector3 lookOrientation = localPlayer.LookOrientation;
		lookOrientation = new Vector3(MathHelper.Clamp(lookOrientation.X + lookOffset.X, -(float)System.Math.PI / 2f, (float)System.Math.PI / 2f), MathHelper.WrapAngle(lookOrientation.Y + lookOffset.Y), lookOrientation.Roll);
		localPlayer.LookOrientation.Yaw = lookOrientation.Yaw;
		localPlayer.LookOrientation.Pitch = lookOrientation.Pitch;
		localPlayer.UpdateModelLookOrientation();
	}

	public void SetRotation(Vector3 rotation)
	{
		Rotation = rotation;
	}

	public void OnMouseInput(SDL_Event evt)
	{
	}

	public void ToggleControlTarget()
	{
		CanMove = !CanMove;
		_gameInstance.Chat.Log("Now controlling the " + (CanMove ? "player" : "camera"));
	}
}
