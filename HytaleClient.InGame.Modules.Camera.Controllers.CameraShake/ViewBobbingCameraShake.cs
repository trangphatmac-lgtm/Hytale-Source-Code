using System.Collections.Generic;
using HytaleClient.InGame.Modules.CharacterController;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Camera.Controllers.CameraShake;

internal class ViewBobbingCameraShake : CameraShake
{
	private const float DefaultEaseIn = 0.5f;

	private const float DefaultDuration = 0f;

	private MovementType _movementType;

	private readonly GameInstance _gameInstance;

	private readonly Dictionary<MovementType, CameraShakeType> _viewBobbingTypes = new Dictionary<MovementType, CameraShakeType>();

	public ViewBobbingCameraShake(GameInstance gameInstance)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		_movementType = (MovementType)0;
		_gameInstance = gameInstance;
	}

	public void UpdateViewBobbingTypes(UpdateViewBobbing packet)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		foreach (KeyValuePair<MovementType, ViewBobbing> profile in packet.Profiles)
		{
			if (profile.Value == null)
			{
				_viewBobbingTypes[profile.Key] = CameraShakeType.None;
				continue;
			}
			ViewBobbing value = profile.Value;
			_viewBobbingTypes[profile.Key] = new CameraShakeType(value.FirstPerson);
		}
		_activeShakeType = _viewBobbingTypes[_movementType] ?? CameraShakeType.None;
	}

	public override void Reset()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		base.Reset();
		_movementType = (MovementType)0;
	}

	public override bool IsComplete()
	{
		return false;
	}

	protected override bool Update()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Invalid comparison between Unknown and I4
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		if (!_gameInstance.App.Settings.ViewBobbingEffect || !_gameInstance.CameraModule.Controller.IsFirstPerson)
		{
			return false;
		}
		MovementController movementController = _gameInstance.CharacterControllerModule.MovementController;
		MovementType movementType = GetMovementType(movementController);
		if ((int)movementType == 0)
		{
			_timer.Set(0f, 0.5f);
			_timer.Restart();
			return false;
		}
		if (_movementType != movementType || !MathHelper.WithinEpsilon(_frequency, movementController.SpeedMultiplier))
		{
			_movementType = movementType;
			_activeShakeType = _viewBobbingTypes[_movementType] ?? CameraShakeType.None;
			_timer.Set(0f, _activeShakeType.EaseIn);
			_timer.Restart();
			_frequency = movementController.SpeedMultiplier;
			_intensityMultiplier = _gameInstance.App.Settings.ViewBobbingIntensity;
		}
		return true;
	}

	protected override Vector3 ComputeOffset(float time, float intensity, Quaternion angle)
	{
		Vector3 value = _activeShakeType.Offset.Eval(_seed, time) * intensity;
		Vector3.Transform(ref value, ref angle, out value);
		return Vector3.Lerp(_offset, value, _timer.Easing);
	}

	protected override Vector3 ComputeRotation(float time, float intensity, Quaternion angle)
	{
		Vector3 value = _activeShakeType.Rotation.Eval(_seed, time) * intensity;
		return Vector3.Lerp(_rotation, value, _timer.Easing);
	}

	private static MovementType GetMovementType(MovementController controller)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		if (controller.MovementStates.IsFlying)
		{
			return (MovementType)8;
		}
		if (controller.MovementStates.IsSwimming)
		{
			return (MovementType)7;
		}
		if (controller.MovementStates.IsIdle || controller.MovementStates.IsHorizontalIdle)
		{
			return (MovementType)1;
		}
		if (controller.MovementStates.IsClimbing)
		{
			return (MovementType)6;
		}
		if (controller.MovementStates.IsSwimming)
		{
			return (MovementType)7;
		}
		if (!controller.MovementStates.IsOnGround)
		{
			return (MovementType)0;
		}
		if (controller.MovementStates.IsCrouching)
		{
			return (MovementType)2;
		}
		if (controller.MovementStates.IsWalking)
		{
			return (MovementType)3;
		}
		if (controller.MovementStates.IsSprinting || (controller.SprintForceDurationLeft > 0f && !controller.MovementStates.IsWalking))
		{
			return (MovementType)5;
		}
		return (MovementType)4;
	}
}
