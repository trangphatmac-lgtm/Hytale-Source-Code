using System;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Camera.Controllers.CameraShake;

public abstract class CameraShake
{
	private const float IntensityLerpRate = 0.05f;

	protected Vector3 _offset = Vector3.Zero;

	protected Vector3 _rotation = Vector3.Zero;

	protected float _frequency = 1f;

	protected float _intensity = 1f;

	protected float _targetIntensity = 1f;

	protected float _intensityMultiplier = 1f;

	protected CameraShakeType _activeShakeType = CameraShakeType.None;

	protected readonly int _seed;

	protected readonly CameraShakeTimer _timer = new CameraShakeTimer();

	public CameraShake()
	{
		_seed = Environment.TickCount;
	}

	public void Update(float time, float deltaTime, Quaternion angle)
	{
		if (Update())
		{
			UpdateIntensity();
			_timer.Tick(deltaTime);
			float time2 = GetTimeStep(time) * _frequency;
			float intensity = _intensity * _intensityMultiplier;
			_offset = ComputeOffset(time2, intensity, angle);
			_rotation = ComputeRotation(time2, intensity, angle);
		}
	}

	public void AddShake(ref Vector3 offset, ref Vector3 rotation)
	{
		offset += _offset;
		rotation += _rotation;
	}

	public void SetIntensity(float intensity)
	{
		_intensity = intensity;
		_targetIntensity = intensity;
	}

	public void SetIntensityTarget(float targetIntensity, AccumulationMode accumulationMode)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected I4, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		switch ((int)accumulationMode)
		{
		case 0:
			_targetIntensity = targetIntensity;
			break;
		case 1:
			_targetIntensity += targetIntensity;
			break;
		case 2:
			_targetIntensity = (_targetIntensity + targetIntensity) * 0.5f;
			break;
		default:
			throw new ArgumentOutOfRangeException("accumulationMode", accumulationMode, null);
		}
	}

	public float GetTimeStep(float time)
	{
		return _activeShakeType.Continuous ? time : (_activeShakeType.StartTime + _timer.Time);
	}

	public virtual bool IsActive()
	{
		return _timer.HasStarted();
	}

	public virtual bool IsComplete()
	{
		return _timer.IsComplete();
	}

	public virtual void Stop()
	{
		Reset();
		_timer.Stop();
	}

	public virtual void ExtendDuration()
	{
		_timer.Extend();
	}

	public virtual void Reset()
	{
		_timer.Restart();
		_frequency = 1f;
		_intensity = 1f;
		_targetIntensity = 1f;
		_intensityMultiplier = 1f;
		_offset = Vector3.Zero;
		_rotation = Vector3.Zero;
		_activeShakeType = CameraShakeType.None;
	}

	protected abstract bool Update();

	protected abstract Vector3 ComputeOffset(float time, float intensity, Quaternion angle);

	protected abstract Vector3 ComputeRotation(float time, float intensity, Quaternion angle);

	private void UpdateIntensity()
	{
		if (!MathHelper.WithinEpsilon(_intensity, _targetIntensity))
		{
			_intensity = MathHelper.Lerp(_intensity, _targetIntensity, 0.05f);
		}
	}
}
