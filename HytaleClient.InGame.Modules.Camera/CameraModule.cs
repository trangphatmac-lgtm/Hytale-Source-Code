using System;
using HytaleClient.Data.UserSettings;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.InGame.Modules.Camera.Controllers.CameraShake;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Camera;

internal class CameraModule : Module
{
	private const float FloatEpsilon = 1E-05f;

	private ICameraController[] _controllerTypes;

	private FreeRotateCameraController _freeRotateCameraController;

	private int _controllerIndex;

	private ICameraController _oldController;

	private int _oldControllerIndex;

	private Vector2 _look;

	private Vector2 _smoothedLook;

	private bool _shake = false;

	private Vector2 _offset;

	private Random _random = new Random();

	private float _shakeRadius;

	private float _shakeStartAngle;

	private Vector2 _totalOffset;

	private float _shakeTimer = 0f;

	private Easing.EasingType _mouseSensitivityEasing = Easing.EasingType.Linear;

	private float _easingDurationLeft = 0f;

	private float _easingDuration = 0f;

	private float _mouseSensitivityTarget = 1f;

	private float _mouseSensitivityStart = 1f;

	private float _easingProgress = 1f;

	public ICameraController Controller { get; private set; }

	public CameraShakeController CameraShakeController { get; }

	private void MouseSensitivityEasingUpdate()
	{
		if (_easingDurationLeft != 0f)
		{
			_easingDurationLeft -= 1f / 60f;
			if (_easingDurationLeft <= 0f)
			{
				_easingDurationLeft = 0f;
			}
			_easingProgress = Easing.Ease(Easing.EasingType.Linear, _easingDuration - _easingDurationLeft, 0f, 1f, _easingDuration);
		}
	}

	private float GetCurrentMouseModifierValue()
	{
		float num = _mouseSensitivityStart - (_mouseSensitivityStart - _mouseSensitivityTarget) * _easingProgress;
		if (num < 1E-05f)
		{
			return 0f;
		}
		return num;
	}

	public CameraModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_controllerTypes = new ICameraController[2]
		{
			new FirstPersonCameraController(gameInstance),
			new ThirdPersonCameraController(gameInstance)
		};
		CameraShakeController = new CameraShakeController(gameInstance);
		_freeRotateCameraController = new FreeRotateCameraController(gameInstance);
		_controllerIndex = _gameInstance.App.Settings.SavedCameraIndex;
		UpdateCameraController(_controllerTypes[_controllerIndex]);
	}

	public void SetTargetMouseModifier(float targetModifier, float modiferChangeRateTime)
	{
		_mouseSensitivityStart = GetCurrentMouseModifierValue();
		_mouseSensitivityTarget = targetModifier;
		_easingDuration = modiferChangeRateTime;
		_easingDurationLeft = modiferChangeRateTime;
	}

	public void Update(float deltaTime)
	{
		_offset = Vector2.Zero;
		if (_shake)
		{
			ComputeShakeOffset();
		}
		else if (_totalOffset != Vector2.Zero)
		{
			_offset = -_totalOffset;
			_totalOffset = Vector2.Zero;
		}
		_shakeTimer = MathHelper.Max(0f, _shakeTimer - deltaTime);
		if (_gameInstance.Input.ConsumeBinding(_gameInstance.App.Settings.InputBindings.SwitchCameraMode))
		{
			if (_controllerIndex != -1)
			{
				bool flag = _gameInstance.Input.IsCtrlHeld() && _gameInstance.Input.IsShiftHeld();
				SetCameraControllerIndex(_controllerIndex + ((!flag) ? 1 : (-1)));
			}
		}
		else
		{
			bool flag2 = _gameInstance.Input.IsBindingHeld(_gameInstance.App.Settings.InputBindings.ActivateCameraRotation);
			if (flag2 && Controller != _freeRotateCameraController && _controllerIndex != -1)
			{
				SetCustomCameraController(_freeRotateCameraController);
			}
			else if (!flag2 && Controller == _freeRotateCameraController)
			{
				ResetCameraController();
			}
		}
		MouseSettings mouseSettings = _gameInstance.App.Settings.MouseSettings;
		float num = mouseSettings.MouseXSpeed / 20f;
		float num2 = mouseSettings.MouseYSpeed / 20f;
		MouseSensitivityEasingUpdate();
		float currentMouseModifierValue = GetCurrentMouseModifierValue();
		num *= currentMouseModifierValue;
		num2 *= currentMouseModifierValue;
		if (mouseSettings.MouseRawInputMode)
		{
			_look.X *= 0.01f * num * (mouseSettings.MouseInverted ? (-1f) : 1f);
			_look.Y *= 0.01f * num2;
		}
		else
		{
			_smoothedLook.X += _look.X * (mouseSettings.MouseInverted ? (-1f) : 1f);
			_smoothedLook.Y += _look.Y;
			_smoothedLook.X *= num;
			_smoothedLook.Y *= num2;
			_look.X = _smoothedLook.X * 0.01f;
			_look.Y = _smoothedLook.Y * 0.01f;
		}
		Controller.ApplyLook(deltaTime, _look + _offset);
		_look.X = (_look.Y = 0f);
		Controller.Update(deltaTime);
	}

	private void ComputeShakeOffset()
	{
		_offset = new Vector2((float)(System.Math.Sin(_shakeStartAngle) * (double)_shakeRadius), (float)(System.Math.Cos(_shakeStartAngle) * (double)_shakeRadius));
		_offset /= 20f;
		_totalOffset += _offset;
		_shakeRadius -= 0.1f;
		_shakeStartAngle += _random.NextFloat(0f, (float)System.Math.PI * 2f);
		if (_shakeTimer == 0f || _shakeRadius <= 0f)
		{
			_shake = false;
			ResetCameraController();
		}
	}

	public void Shake(float duration, float force)
	{
		_shake = true;
		_shakeRadius = force;
		_shakeStartAngle = _random.NextFloat(0f, (float)System.Math.PI * 2f);
		_totalOffset = Vector2.Zero;
		_shakeTimer += duration;
	}

	private void UpdateCameraController(ICameraController cameraController)
	{
		ICameraController controller = Controller;
		Controller = cameraController;
		cameraController.Reset(_gameInstance, controller);
		_gameInstance.App.Interface.TriggerEvent("crosshair.setVisible", Controller.IsFirstPerson || Controller.DisplayReticle);
		_gameInstance.Engine.Window.SetCursorVisible(Controller.DisplayCursor);
	}

	public void OffsetLook(float x, float y)
	{
		_look.X += x;
		_look.Y += y;
	}

	public void SetCameraControllerIndex(int cameraControllerIndex)
	{
		_controllerIndex = cameraControllerIndex;
		if (_controllerIndex < 0)
		{
			_controllerIndex = _controllerTypes.Length - 1;
		}
		else if (_controllerIndex >= _controllerTypes.Length)
		{
			_controllerIndex = 0;
		}
		_gameInstance.App.Settings.SavedCameraIndex = _controllerIndex;
		_gameInstance.App.Settings.Save();
		UpdateCameraController(_controllerTypes[_controllerIndex]);
	}

	public bool IsCustomCameraControllerSet()
	{
		return _controllerIndex == -1;
	}

	public void SetCustomCameraController(ICameraController cameraController)
	{
		if (cameraController == null)
		{
			ResetCameraController();
			return;
		}
		if (_controllerIndex != -1)
		{
			_oldControllerIndex = _controllerIndex;
			_controllerIndex = -1;
			_oldController = Controller;
		}
		UpdateCameraController(cameraController);
	}

	public void ResetCameraController()
	{
		if (_controllerIndex == -1)
		{
			UpdateCameraController(_oldController);
			_controllerIndex = _oldControllerIndex;
			_oldControllerIndex = -1;
			_oldController = null;
		}
	}

	public void LockCamera()
	{
		_controllerIndex = -1;
	}

	public Ray GetLookRay()
	{
		Quaternion rotation = Quaternion.CreateFromYawPitchRoll(Controller.Rotation.Yaw, Controller.Rotation.Pitch, 0f);
		Vector3 direction = Vector3.Transform(Vector3.Forward, rotation);
		return new Ray(Controller.Position, direction);
	}
}
