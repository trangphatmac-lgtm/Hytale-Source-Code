using System;
using System.Collections.Generic;
using HytaleClient.Core;
using HytaleClient.Data.UserSettings;
using HytaleClient.InGame.Modules.CharacterController.DefaultController;
using HytaleClient.InGame.Modules.CharacterController.MountController;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.CharacterController;

internal class CharacterControllerModule : Module
{
	private const string PlayerSpeedRTPCName = "SPEED";

	private const float RTPCSpeedTolerance = 0.01f;

	private const float NotPressed = 0f;

	private readonly Dictionary<string, MovementController> _movementControllers = new Dictionary<string, MovementController>();

	private uint _playerSpeedRTPCId;

	private float _previousSpeedRTPCValue;

	private float _timeElapsed;

	public MovementController MovementController { get; private set; }

	public float ForwardsTimestamp { get; private set; }

	public float BackwardsTimestamp { get; private set; }

	public float LeftTimestamp { get; private set; }

	public float RightTimestamp { get; private set; }

	public CharacterControllerModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_movementControllers.Add("Default", new DefaultMovementController(gameInstance));
		_movementControllers.Add("Mount", new MountMovementController(gameInstance));
		MovementController = _movementControllers["Default"];
	}

	public override void Initialize()
	{
		if (!_gameInstance.Engine.Audio.ResourceManager.WwiseGameParameterIds.TryGetValue("SPEED", out _playerSpeedRTPCId))
		{
			_gameInstance.App.DevTools.Error("Missing speed RTPC: SPEED");
		}
	}

	public override void Tick()
	{
		MovementController.Tick();
		Vector3 velocity = MovementController.Velocity;
		float num = (float)System.Math.Sqrt((double)(velocity.X * velocity.X) + (double)(velocity.Z * velocity.Z) + (double)(velocity.Y * velocity.Y));
		if (System.Math.Abs(num - _previousSpeedRTPCValue) > 0.01f)
		{
			_gameInstance.Engine.Audio.SetRTPC(_playerSpeedRTPCId, num);
			_previousSpeedRTPCValue = num;
		}
	}

	public void MountNpc(MountNPC packet)
	{
		MountMovementController mountMovementController = (MountMovementController)_movementControllers["Mount"];
		mountMovementController.OnMount(packet);
		MovementSettings movementSettings = MovementController.MovementSettings;
		MovementController = mountMovementController;
		MovementController.MovementSettings = movementSettings;
		Vector3 lookOrientation = _gameInstance.LocalPlayer.LookOrientation;
		lookOrientation.Pitch = 0f;
		lookOrientation.Roll = 0f;
		MovementController.CameraRotation = lookOrientation;
		_gameInstance.EntityStoreModule.MountEntityLocalId = packet.EntityId;
	}

	public void DismountNpc(bool isLocalInteraction = false)
	{
		if (MovementController is MountMovementController mountMovementController)
		{
			mountMovementController.OnDismount(isLocalInteraction);
		}
		MovementController = _movementControllers["Default"];
		_gameInstance.EntityStoreModule.MountEntityLocalId = -1;
	}

	public void Update(float deltaTime)
	{
		_timeElapsed += deltaTime;
		Input input = _gameInstance.Input;
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		ForwardsTimestamp = ((!input.IsBindingHeld(inputBindings.MoveForwards)) ? 0f : ((ForwardsTimestamp == 0f) ? _timeElapsed : ForwardsTimestamp));
		BackwardsTimestamp = ((!input.IsBindingHeld(inputBindings.MoveBackwards)) ? 0f : ((BackwardsTimestamp == 0f) ? _timeElapsed : BackwardsTimestamp));
		LeftTimestamp = ((!input.IsBindingHeld(inputBindings.StrafeLeft)) ? 0f : ((LeftTimestamp == 0f) ? _timeElapsed : LeftTimestamp));
		RightTimestamp = ((!input.IsBindingHeld(inputBindings.StrafeRight)) ? 0f : ((RightTimestamp == 0f) ? _timeElapsed : RightTimestamp));
	}

	public void PreUpdate(float timeFraction)
	{
		MovementController.PreUpdate(timeFraction);
	}
}
