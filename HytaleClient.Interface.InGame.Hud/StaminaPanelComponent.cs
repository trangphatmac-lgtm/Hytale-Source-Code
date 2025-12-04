using System.Collections.Generic;
using HytaleClient.Data.EntityStats;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using NLog;
using Wwise;

namespace HytaleClient.Interface.InGame.Hud;

internal class StaminaPanelComponent : InterfaceComponent
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const string StaminaDepletedEffectId = "Stamina_Broken";

	public const string StaminaErrorEffectId = "Stamina_Error_State";

	private const string StaminaErrorWWiseId = "STAMINA_ERROR_STATE";

	private const string StaminaLowAlertWWiseId = "STAMINA_LOW_ALERT";

	private const string StaminaOverdrawnAlertWWiseId = "STAMINA_EXHAUSTED_ALERT";

	private const float DEFAULT_ANIMATION_SPEED = 10f;

	private const int NO_PLAYBACK_ID = -1;

	public readonly InGameView InGameView;

	private Group _staminaBar;

	private ProgressBar _staminaBarProgress;

	private ProgressBar _staminaBarProgressDrain;

	private PatchStyle _staminaBarBackground;

	private PatchStyle _staminaBarBackgroundDepleted;

	private PatchStyle _staminaBarBackgroundError;

	private PatchStyle _staminaBarBackgroundErrorOutline;

	private PatchStyle _staminaBarBackgroundOverdrawn;

	private PatchStyle _staminaBarBackgroundOverdrawnOutline;

	private Group _staminaBarError;

	private Group _staminaBarErrorOutline;

	private Group _debugStaminaInfo;

	private Label _debugStaminaValue;

	private Label _debugStaminaRegenRate;

	private Label _debugStaminaAlertState;

	private float _flashOutlineRenderValue;

	private float _staminaErrorRenderValue;

	private float _staminaRenderValue;

	private float _staminaRenderValueTarget;

	private float _depletionTimer;

	private float _flashTimer;

	private bool _isErrorActive;

	private float _progressBarAnimationSpeed;

	private float _drainProgressBarAnimationSpeed;

	private float _drainProgressTime;

	private float _errorAnimationSpeed;

	private float _progressBarErrorThreshold;

	private float _defaultErrorFlashTimer;

	private float _defaultStaminaErrorRenderValue;

	private float _minimumDrainValue;

	private bool _isLowAlert;

	private int _lowAlertPlaybackId = -1;

	private float _lowAlertOutlineOpacityStart;

	private float _lowAlertOutlineOpacityTarget;

	private bool _isOutOfStamina;

	private float _lowAlertAnimationSpeed;

	private float _lowAlertFlashTimer;

	private bool _isOverdrawnAlert;

	private int _overdrawnAlertPlaybackId;

	private float _overdrawnAlertFlashTimer;

	private float _overdrawnAlertAnimationSpeed;

	public bool ShouldDisplay
	{
		get
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Invalid comparison between Unknown and I4
			PlayerEntity playerEntity = InGameView.InGame.Instance?.LocalPlayer;
			if (playerEntity == null)
			{
				return false;
			}
			if ((int)InGameView.InGame.Instance.GameMode == 1)
			{
				return false;
			}
			ClientEntityStatValue entityStat = InGameView.InGame.Instance.LocalPlayer.GetEntityStat(DefaultEntityStats.Stamina);
			return InGameView.Wielding || entityStat?.Value < entityStat?.Max || ShouldDisplayFlash;
		}
	}

	private bool ShouldDisplayFlash => AlertType != AlertType.None;

	private AlertType AlertType
	{
		get
		{
			if (_isErrorActive)
			{
				return AlertType.Error;
			}
			if (_isOverdrawnAlert)
			{
				return AlertType.Overdrawn;
			}
			if (_isLowAlert)
			{
				return AlertType.Low;
			}
			return AlertType.None;
		}
	}

	public StaminaPanelComponent(InGameView view)
		: base(view.Interface, view.HudContainer)
	{
		InGameView = view;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Hud/Stamina/StaminaPanel.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_staminaBar = uIFragment.Get<Group>("StaminaBar");
		_staminaBarProgress = uIFragment.Get<ProgressBar>("StaminaBarProgress");
		_staminaBarProgressDrain = uIFragment.Get<ProgressBar>("StaminaBarDrainEffect");
		_staminaBarError = uIFragment.Get<Group>("StaminaBarError");
		_staminaBarErrorOutline = uIFragment.Get<Group>("StaminaBarErrorOutline");
		_debugStaminaInfo = uIFragment.Get<Group>("DebugStaminaInfo");
		_debugStaminaValue = uIFragment.Get<Label>("DebugStaminaValue");
		_debugStaminaRegenRate = uIFragment.Get<Label>("DebugStaminaRegenRate");
		_debugStaminaAlertState = uIFragment.Get<Label>("DebugStaminaAlertState");
		_staminaBarBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "StaminaBarBackground");
		_staminaBarBackgroundDepleted = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "StaminaBarBackgroundDepleted");
		_staminaBarBackgroundError = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "StaminaBarBackgroundError");
		_staminaBarBackgroundErrorOutline = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "StaminaBarBackgroundErrorOutline");
		_staminaBarBackgroundOverdrawn = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "StaminaBarBackgroundOverdrawn");
		_staminaBarBackgroundOverdrawnOutline = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "StaminaBarBackgroundOverdrawnOutline");
		_progressBarAnimationSpeed = document.ResolveNamedValue<float>(Desktop.Provider, "ProgressBarAnimationSpeed");
		_drainProgressBarAnimationSpeed = document.ResolveNamedValue<float>(Desktop.Provider, "DrainProgressBarAnimationSpeed");
		_drainProgressTime = document.ResolveNamedValue<float>(Desktop.Provider, "DrainProgressTime");
		_minimumDrainValue = document.ResolveNamedValue<float>(Desktop.Provider, "MinimumDrainValue");
		_errorAnimationSpeed = document.ResolveNamedValue<float>(Desktop.Provider, "ErrorAnimationSpeed");
		_progressBarErrorThreshold = document.ResolveNamedValue<float>(Desktop.Provider, "ProgressBarErrorThreshold");
		_defaultErrorFlashTimer = document.ResolveNamedValue<float>(Desktop.Provider, "ErrorFlashTimer");
		_defaultStaminaErrorRenderValue = document.ResolveNamedValue<float>(Desktop.Provider, "StaminaErrorRenderValue");
		_lowAlertOutlineOpacityStart = document.ResolveNamedValue<float>(Desktop.Provider, "LowAlertOutlineOpacityStart");
		_lowAlertAnimationSpeed = document.ResolveNamedValue<float>(Desktop.Provider, "LowAlertAnimationSpeed");
		_lowAlertFlashTimer = document.ResolveNamedValue<float>(Desktop.Provider, "LowAlertFlashTimer");
		_overdrawnAlertFlashTimer = document.ResolveNamedValue<float>(Desktop.Provider, "OverdrawnAlertFlashTimer");
		_overdrawnAlertAnimationSpeed = document.ResolveNamedValue<float>(Desktop.Provider, "OverdrawnAlertAnimationSpeed");
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
		StopSound(_lowAlertPlaybackId);
		StopSound(_overdrawnAlertPlaybackId);
	}

	private void Animate(float deltaTime)
	{
		UpdateProgressBarBackground();
		UpdateFlash(deltaTime);
		AnimateFlash(deltaTime);
		UpdateProgressBarValues(deltaTime);
	}

	private void UpdateProgressBarBackground()
	{
		AlertType alertType = AlertType;
		AlertType alertType2 = alertType;
		if (alertType2 == AlertType.Overdrawn)
		{
			_staminaBarError.Background = _staminaBarBackgroundOverdrawn;
			_staminaBarErrorOutline.Background = _staminaBarBackgroundOverdrawnOutline;
		}
		else
		{
			_staminaBarError.Background = _staminaBarBackgroundError;
			_staminaBarErrorOutline.Background = _staminaBarBackgroundErrorOutline;
		}
	}

	private void UpdateFlash(float deltaTime)
	{
		if (ShouldDisplayFlash && !_isOutOfStamina)
		{
			_flashTimer -= deltaTime;
			if (_flashTimer <= 0f)
			{
				TriggerFlash();
			}
		}
		else
		{
			_flashTimer = 0f;
		}
	}

	private void TriggerFlash()
	{
		_flashTimer = (_isErrorActive ? _defaultErrorFlashTimer : (_isOverdrawnAlert ? _overdrawnAlertFlashTimer : _lowAlertFlashTimer));
		_staminaErrorRenderValue = _defaultStaminaErrorRenderValue;
	}

	private void AnimateFlash(float deltaTime)
	{
		AnimateFlashOutline(deltaTime);
		AnimateFlashBar(deltaTime);
	}

	private void AnimateFlashOutline(float deltaTime)
	{
		if (_isOutOfStamina)
		{
			_staminaBarErrorOutline.Background.Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
			_staminaBarErrorOutline.Visible = true;
			_staminaBarErrorOutline.Layout(_staminaBarErrorOutline.Parent.RectangleAfterPadding);
			return;
		}
		float opacityTargetValue = GetOpacityTargetValue();
		if (_flashOutlineRenderValue != opacityTargetValue)
		{
			_flashOutlineRenderValue = MathHelper.Lerp(_flashOutlineRenderValue, opacityTargetValue, deltaTime * GetAnimationSpeed());
			_staminaBarErrorOutline.Background.Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(255f * _flashOutlineRenderValue));
			_staminaBarErrorOutline.Visible = true;
			_staminaBarErrorOutline.Layout(_staminaBarErrorOutline.Parent.RectangleAfterPadding);
		}
		else if (!ShouldDisplayFlash && _staminaBarErrorOutline.Visible)
		{
			_staminaBarErrorOutline.Visible = false;
			_staminaBarErrorOutline.Layout();
		}
	}

	private void AnimateFlashBar(float deltaTime)
	{
		if (_isOutOfStamina)
		{
			_staminaBarError.Background.Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
			_staminaBarError.Visible = true;
			_staminaBarError.Layout(_staminaBarError.Parent.RectangleAfterPadding);
		}
		else if (_staminaErrorRenderValue > 0f)
		{
			if (_staminaErrorRenderValue <= _progressBarErrorThreshold)
			{
				_staminaErrorRenderValue = 0f;
			}
			else
			{
				_staminaErrorRenderValue = MathHelper.Lerp(_staminaErrorRenderValue, 0f, deltaTime * GetAnimationSpeed());
			}
			byte a = (byte)(MathHelper.Min(255f, 255f * _staminaErrorRenderValue) * GetOpacityTargetValue());
			_staminaBarError.Background.Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, a);
			_staminaBarError.Visible = true;
			_staminaBarError.Layout(_staminaBarError.Parent.RectangleAfterPadding);
		}
		else if (_staminaBarError.Visible)
		{
			_staminaBarError.Visible = false;
		}
	}

	private float GetOpacityTargetValue()
	{
		return AlertType switch
		{
			AlertType.Error => 1f, 
			AlertType.Overdrawn => 1f, 
			AlertType.Low => _lowAlertOutlineOpacityTarget, 
			_ => 0f, 
		};
	}

	private float GetAnimationSpeed()
	{
		return AlertType switch
		{
			AlertType.Low => _lowAlertAnimationSpeed, 
			AlertType.Error => _errorAnimationSpeed, 
			AlertType.Overdrawn => _overdrawnAlertAnimationSpeed, 
			_ => 10f, 
		};
	}

	private void UpdateProgressBarValues(float deltaTime)
	{
		if (_staminaRenderValue != _staminaRenderValueTarget)
		{
			_staminaRenderValue = MathHelper.Lerp(_staminaRenderValue, _staminaRenderValueTarget, deltaTime * _progressBarAnimationSpeed);
			_staminaBarProgress.Value = _staminaRenderValue;
			_staminaBarProgress.Layout();
		}
		if (_depletionTimer > 0f)
		{
			_depletionTimer -= deltaTime;
		}
		else if (_staminaBarProgressDrain.Value != _staminaBarProgress.Value)
		{
			_staminaBarProgressDrain.Value = MathHelper.Lerp(_staminaBarProgressDrain.Value, _staminaBarProgress.Value, deltaTime * _drainProgressBarAnimationSpeed);
			_staminaBarProgressDrain.Layout();
		}
	}

	public void OnStatChanged(ClientEntityStatValue staminaEntityStat, float? nullablePreviousValue)
	{
		if (!(staminaEntityStat.Max <= 0f))
		{
			if (!nullablePreviousValue.HasValue)
			{
				nullablePreviousValue = (_staminaBarProgress.Value = (_staminaRenderValue = (_staminaRenderValueTarget = staminaEntityStat.Value)));
				_staminaBarProgress.Layout();
			}
			float num = nullablePreviousValue.Value;
			float num2 = num - staminaEntityStat.Value;
			if (staminaEntityStat.Value < num)
			{
				_depletionTimer = _drainProgressTime;
				_staminaBarProgressDrain.Value = ((num2 > 0f && num2 < _minimumDrainValue) ? staminaEntityStat.Value : num) / staminaEntityStat.Max;
				_staminaBarProgressDrain.Layout();
			}
			_isOutOfStamina = staminaEntityStat.Value <= staminaEntityStat.Min;
			UpdateLowAlert(staminaEntityStat);
			UpdateOverdrawnAlert(staminaEntityStat);
			UpdateStaminaBarProgress(staminaEntityStat);
			UpdateStaminaPanelVisibility(staminaEntityStat, num);
			UpdateStaminaDebugInfo(staminaEntityStat, num);
		}
	}

	private void UpdateLowAlert(ClientEntityStatValue staminaEntityStat)
	{
		HytaleClient.Data.UserSettings.Settings settings = InGameView.InGame.Instance.App.Settings;
		float num = (float)settings.StaminaLowAlertPercent / 100f;
		float num2 = staminaEntityStat.Value / staminaEntityStat.Max;
		_lowAlertOutlineOpacityTarget = MathHelper.Clamp(MathHelper.ConvertToNewRange(num2, 0f, num, 1f, _lowAlertOutlineOpacityStart), _lowAlertOutlineOpacityStart, 1f);
		if (!_isLowAlert && num2 <= num && num2 > 0f)
		{
			_isLowAlert = true;
			_lowAlertPlaybackId = PlaySound("STAMINA_LOW_ALERT");
		}
		else if (_isLowAlert && num2 > num)
		{
			_isLowAlert = false;
			_lowAlertPlaybackId = StopSound(_lowAlertPlaybackId);
		}
	}

	private void UpdateOverdrawnAlert(ClientEntityStatValue staminaEntityStat)
	{
		if (!_isErrorActive && !_isOverdrawnAlert && staminaEntityStat.Value < 0f)
		{
			_isOverdrawnAlert = true;
			StopSound(_overdrawnAlertPlaybackId);
			_overdrawnAlertPlaybackId = PlaySound("STAMINA_EXHAUSTED_ALERT");
		}
		else if (_isErrorActive || (_isOverdrawnAlert && staminaEntityStat.Value >= 0f))
		{
			_isOverdrawnAlert = false;
			_overdrawnAlertPlaybackId = StopSound(_overdrawnAlertPlaybackId);
		}
	}

	private void UpdateStaminaBarProgress(ClientEntityStatValue staminaEntityStat)
	{
		_staminaRenderValueTarget = staminaEntityStat.Value / staminaEntityStat.Max;
	}

	private void UpdateStaminaPanelVisibility(ClientEntityStatValue staminaEntityStat, float previousValue)
	{
		if (staminaEntityStat.Value == staminaEntityStat.Max || previousValue == staminaEntityStat.Max)
		{
			InGameView.UpdateStaminaPanelVisibility(doLayout: true);
		}
		else if (base.IsMounted)
		{
			Layout();
		}
	}

	private void UpdateStaminaDebugInfo(ClientEntityStatValue staminaEntityStat, float previousValue)
	{
		if (InGameView.InGame.Instance.App.Settings.StaminaDebugInfo)
		{
			double num = staminaEntityStat.Value;
			double num2 = staminaEntityStat.Value - previousValue;
			_debugStaminaValue.Text = num.ToString("0.00");
			_debugStaminaRegenRate.Text = num2.ToString("0.00");
			_debugStaminaAlertState.Text = AlertType.ToString();
			_debugStaminaInfo.Visible = true;
			_debugStaminaInfo.Layout(_debugStaminaInfo.Parent.RectangleAfterPadding);
		}
		else if (_debugStaminaInfo.Visible)
		{
			_debugStaminaInfo.Visible = false;
			_debugStaminaInfo.Layout();
		}
	}

	public void ResetState()
	{
		_staminaBarProgress.Value = (_staminaRenderValue = (_staminaRenderValueTarget = 0f));
	}

	public void OnEffectAdded(int effectIndex)
	{
		Dictionary<string, int> entityEffectIndicesByIds = InGameView.InGame.Instance.EntityStoreModule.EntityEffectIndicesByIds;
		int value2;
		if (entityEffectIndicesByIds.TryGetValue("Stamina_Broken", out var value) && effectIndex == value)
		{
			SetDepletion(depleted: true);
		}
		else if (entityEffectIndicesByIds.TryGetValue("Stamina_Error_State", out value2) && effectIndex == value2)
		{
			_isErrorActive = true;
			PlaySound("STAMINA_ERROR_STATE");
		}
	}

	public void OnEffectRemoved(int effectIndex)
	{
		Dictionary<string, int> entityEffectIndicesByIds = InGameView.InGame.Instance.EntityStoreModule.EntityEffectIndicesByIds;
		int value2;
		if (entityEffectIndicesByIds.TryGetValue("Stamina_Broken", out var value) && effectIndex == value)
		{
			SetDepletion(depleted: false);
		}
		else if (entityEffectIndicesByIds.TryGetValue("Stamina_Error_State", out value2) && effectIndex == value2)
		{
			_isErrorActive = false;
		}
	}

	private void SetDepletion(bool depleted)
	{
		_staminaBar.Background = (depleted ? _staminaBarBackgroundDepleted : _staminaBarBackground);
		if (base.IsMounted)
		{
			InGameView.UpdateStaminaPanelVisibility(doLayout: true);
		}
	}

	private int PlaySound(string soundEventId)
	{
		if (Interface.App.Engine.Audio.ResourceManager.WwiseEventIds.TryGetValue(soundEventId, out var value))
		{
			return InGameView.InGame.Instance.AudioModule.PlayLocalSoundEvent(value);
		}
		Logger.Warn("Could not load sound: {0}", soundEventId);
		return -1;
	}

	private int StopSound(int playbackId)
	{
		if (playbackId != -1)
		{
			InGameView.InGame.Instance.AudioModule.ActionOnEvent(playbackId, (AkActionOnEventType)0);
		}
		return -1;
	}
}
