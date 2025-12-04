using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Camera.Controllers.CameraShake;

internal class TimedCameraShake : CameraShake
{
	private readonly CameraShakeType _firstPerson;

	private readonly CameraShakeType _thirdPerson;

	private readonly GameInstance _gameInstance;

	public TimedCameraShake(CameraShakeType firstPerson, CameraShakeType thirdPerson, GameInstance gameInstance)
	{
		_firstPerson = firstPerson;
		_thirdPerson = thirdPerson;
		_gameInstance = gameInstance;
	}

	protected override bool Update()
	{
		if (!_gameInstance.App.Settings.CameraShakeEffect)
		{
			return false;
		}
		if (_gameInstance.CameraModule.Controller.IsFirstPerson)
		{
			_intensityMultiplier = _gameInstance.App.Settings.FirstPersonCameraShakeIntensity;
			UpdateActiveShakeType(_firstPerson);
		}
		else
		{
			_intensityMultiplier = _gameInstance.App.Settings.ThirdPersonCameraShakeIntensity;
			UpdateActiveShakeType(_thirdPerson);
		}
		return true;
	}

	protected override Vector3 ComputeOffset(float time, float intensity, Quaternion angle)
	{
		Vector3 value = _activeShakeType.Offset.Eval(_seed, time) * intensity * _timer.Easing;
		Vector3.Transform(ref value, ref angle, out value);
		return value;
	}

	protected override Vector3 ComputeRotation(float time, float intensity, Quaternion angle)
	{
		return _activeShakeType.Rotation.Eval(_seed, time) * intensity * _timer.Easing;
	}

	private void UpdateActiveShakeType(CameraShakeType type)
	{
		if (type != _activeShakeType)
		{
			_activeShakeType = type;
			_timer.Set(type.Duration, type.EaseIn, type.EaseOut, type.EaseInType, type.EaseOutType);
		}
	}
}
