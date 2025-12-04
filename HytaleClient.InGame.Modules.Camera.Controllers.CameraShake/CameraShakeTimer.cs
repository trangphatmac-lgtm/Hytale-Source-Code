using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Camera.Controllers.CameraShake;

public class CameraShakeTimer
{
	private float _duration;

	private float _easeInDuration;

	private float _easeOutDuration;

	private float _holdTime;

	private float _completionTime;

	private Easing.EasingType _easeInType;

	private Easing.EasingType _easeOutType;

	public float Time { get; private set; }

	public float Easing { get; private set; }

	public bool HasStarted()
	{
		return Time > 0f;
	}

	public bool IsComplete()
	{
		return Time > _completionTime;
	}

	public void Restart()
	{
		Time = 0f;
		Easing = 0f;
		_holdTime = _easeInDuration + _duration;
		_completionTime = _easeInDuration + _duration + _easeOutDuration;
	}

	public void Stop()
	{
		Time = _completionTime;
		Easing = 0f;
	}

	public void Extend()
	{
		_holdTime += _duration;
		_completionTime = _holdTime + _easeOutDuration;
	}

	public void Set(float duration, float easeIn, float easeOut = 0f, Easing.EasingType easeInType = HytaleClient.Math.Easing.EasingType.Linear, Easing.EasingType easeOutType = HytaleClient.Math.Easing.EasingType.Linear)
	{
		_duration = duration;
		_easeInDuration = easeIn;
		_easeOutDuration = easeOut;
		_easeInType = easeInType;
		_easeOutType = easeOutType;
		_holdTime = easeIn + duration;
		_completionTime = easeIn + duration + easeOut;
	}

	public void Tick(float deltaTime)
	{
		if (!(Time > _completionTime))
		{
			Time += deltaTime;
			if (_easeInDuration > 0f && Time < _easeInDuration)
			{
				Easing = HytaleClient.Math.Easing.Ease(_easeInType, Time / _easeInDuration);
			}
			else if (Time <= _holdTime)
			{
				Easing = 1f;
			}
			else if (_easeOutDuration > 0f)
			{
				Easing = HytaleClient.Math.Easing.Ease(_easeOutType, (_completionTime - Time) / _easeOutDuration);
			}
		}
	}
}
