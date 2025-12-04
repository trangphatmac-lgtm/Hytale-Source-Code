using System.Collections.Generic;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Camera.Controllers.CameraShake;

internal class CameraShakeController
{
	private float _time;

	private bool _enabled = true;

	protected readonly GameInstance _gameInstance;

	private readonly ViewBobbingCameraShake _viewBobbingCameraShake;

	private readonly List<CameraShake> _activeCameraShakes = new List<CameraShake>();

	private readonly Dictionary<int, CameraShake> _cameraShakes = new Dictionary<int, CameraShake>();

	public Vector3 Offset { get; private set; } = Vector3.Zero;


	public Vector3 Rotation { get; private set; } = Vector3.Zero;


	public CameraShakeController(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
		_viewBobbingCameraShake = new ViewBobbingCameraShake(_gameInstance);
		_activeCameraShakes.Add(_viewBobbingCameraShake);
	}

	public void Reset()
	{
		for (int num = _activeCameraShakes.Count - 1; num >= 0; num--)
		{
			CameraShake cameraShake = _activeCameraShakes[num];
			cameraShake.Reset();
			if (num != 0)
			{
				_activeCameraShakes.RemoveAt(num);
			}
		}
	}

	public CameraShake GetCameraShake(int cameraShakeIndex)
	{
		if (_cameraShakes.ContainsKey(cameraShakeIndex))
		{
			return _cameraShakes[cameraShakeIndex];
		}
		return null;
	}

	public bool PlayCameraShake(int cameraShakeIndex, float intensity, AccumulationMode accumulationMode)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		CameraShake cameraShake = GetCameraShake(cameraShakeIndex);
		if (cameraShake == null)
		{
			return false;
		}
		if (cameraShake.IsActive())
		{
			cameraShake.ExtendDuration();
			cameraShake.SetIntensityTarget(intensity, accumulationMode);
		}
		else
		{
			cameraShake.SetIntensity(intensity);
			_activeCameraShakes.Add(cameraShake);
		}
		return true;
	}

	public void Update(float deltaTime, Quaternion angle)
	{
		if (_gameInstance.IsBuilderModeEnabled())
		{
			if (_enabled)
			{
				Reset();
				_enabled = false;
			}
		}
		else
		{
			_enabled = true;
			_time += deltaTime;
			UpdateActiveCameraShakes(deltaTime, angle, out var offset, out var rotation);
			Offset = offset;
			Rotation = rotation;
		}
	}

	public void UpdateViewBobbingAssets(UpdateViewBobbing packet)
	{
		_viewBobbingCameraShake.UpdateViewBobbingTypes(packet);
	}

	public void UpdateCameraShakeAssets(UpdateCameraShake packet)
	{
		Reset();
		foreach (KeyValuePair<int, CameraShake> profile in packet.Profiles)
		{
			if (profile.Value == null)
			{
				_cameraShakes.Remove(profile.Key);
				continue;
			}
			CameraShake value = profile.Value;
			CameraShakeType firstPerson = new CameraShakeType(value.FirstPerson);
			CameraShakeType thirdPerson = new CameraShakeType(value.ThirdPerson);
			_cameraShakes[profile.Key] = new TimedCameraShake(firstPerson, thirdPerson, _gameInstance);
		}
	}

	private void UpdateActiveCameraShakes(float deltaTime, Quaternion angle, out Vector3 offset, out Vector3 rotation)
	{
		offset = Vector3.Zero;
		rotation = Vector3.Zero;
		for (int num = _activeCameraShakes.Count - 1; num >= 0; num--)
		{
			CameraShake cameraShake = _activeCameraShakes[num];
			cameraShake.Update(_time, deltaTime, angle);
			cameraShake.AddShake(ref offset, ref rotation);
			if (cameraShake.IsComplete())
			{
				cameraShake.Reset();
				_activeCameraShakes.RemoveAt(num);
			}
		}
	}
}
