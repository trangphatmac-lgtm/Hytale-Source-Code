using System;
using System.Collections.Generic;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.EntityStats;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.InGame.Modules;

internal class DamageEffectModule : Module
{
	private struct DamageDrawTask
	{
		public GLTexture Texture;

		public Matrix MVPMatrix;

		public float Alpha;
	}

	private struct DamageSpriteInfo
	{
		public GLTexture Texture;

		public int Size;
	}

	private struct AnimationData
	{
		public float IndicatorDuration;

		public float MinAlpha;

		public float MaxAlpha;

		public float MinScaleFactor;

		public float MaxScaleFactor;

		public float ScaleDuration;

		public float MinRadius;

		public float MaxRadius;

		public float RadiusDuration;

		public AnimationData(float indicatorDuration, float minAlpha, float maxAlpha, float minScaleFactor, float maxScaleFactor, float scaleDuration, float minRadius, float maxRadius, float radiusDuration)
		{
			IndicatorDuration = indicatorDuration;
			MinAlpha = minAlpha;
			MaxAlpha = maxAlpha;
			MinScaleFactor = minScaleFactor;
			MaxScaleFactor = maxScaleFactor;
			ScaleDuration = scaleDuration;
			MinRadius = minRadius;
			MaxRadius = maxRadius;
			RadiusDuration = radiusDuration;
		}
	}

	private class DamageIndicator
	{
		private readonly Vector3 _damageSourcePosition;

		private readonly AnimationData _animationData;

		private float _timeElapsed = 0f;

		public string SpriteToDisplay { get; private set; }

		public float Angle { get; private set; }

		public Vector2 OffsetSpritePosition { get; private set; }

		public float Alpha { get; private set; }

		public float ScaleFactor { get; private set; }

		public DamageIndicator(Vector3d damageSourcePosition, string spriteToDisplay, AnimationData animationData)
		{
			_damageSourcePosition = new Vector3((float)damageSourcePosition.X, (float)damageSourcePosition.Y, (float)damageSourcePosition.Z);
			SpriteToDisplay = spriteToDisplay;
			_animationData = animationData;
		}

		public bool IsFinished()
		{
			return _timeElapsed >= _animationData.IndicatorDuration;
		}

		public void Update(float deltaTime, Vector3 playerPosition, float yaw)
		{
			_timeElapsed += deltaTime;
			if (!(_timeElapsed >= _animationData.IndicatorDuration))
			{
				Vector3 vector = new Vector3((float)System.Math.Sin(yaw), 0f, (float)System.Math.Cos(yaw));
				Vector3 vector2 = playerPosition - _damageSourcePosition;
				vector2.Normalize();
				Angle = (float)(System.Math.Atan2(vector.Z, vector.X) - System.Math.Atan2(vector2.Z, vector2.X));
				float num = Angle - MathHelper.ToRadians(90f);
				float num2 = ((_timeElapsed > _animationData.RadiusDuration) ? _animationData.MinRadius : (_animationData.MaxRadius - (float)Easing.QuadEaseOut(_timeElapsed, 0.0, _animationData.MaxRadius - _animationData.MinRadius, _animationData.RadiusDuration)));
				OffsetSpritePosition = new Vector2(num2 * (float)System.Math.Cos(num), num2 * (float)System.Math.Sin(num));
				Alpha = _animationData.MaxAlpha - (float)Easing.QuadEaseIn(_timeElapsed, _animationData.MinAlpha, _animationData.MaxAlpha, _animationData.IndicatorDuration);
				ScaleFactor = ((_timeElapsed > _animationData.ScaleDuration) ? _animationData.MinScaleFactor : (_animationData.MaxScaleFactor - (float)Easing.BackEaseOutExtended(_timeElapsed, 0.0, _animationData.MaxScaleFactor - _animationData.MinScaleFactor, _animationData.ScaleDuration)));
			}
		}
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const int DamageDrawTasksDefaultSize = 10;

	private const int DamageDrawTasksGrowth = 5;

	private const float MinCriticalRatio = 0.2f;

	private const float SoundsLerpSpeed = 0.04f;

	private const string DamageBasicPath = "UI/DamageIndicators/HitIndicatorBasic.png";

	private const string DamageCriticalPath = "UI/DamageIndicators/HitIndicatorCritic.png";

	private const string DamageMeleePath = "UI/DamageIndicators/HitIndicatorMelee.png";

	private const string DamageTexturePath = "UI/DamageScreenEffect.png";

	private const string HealthAlertTexturePath = "UI/HealthAlertScreenEffect.png";

	private const string PlayerHealthRTPCName = "HEALTH";

	private const string PlayerStaminaRTPCName = "STAMINA";

	private const string PlayerSignatureRTPCName = "SIGNATURE";

	public int AngleHideDamage = 0;

	public float HealthAlertThreshold = 0.3f;

	public float MinAlphaHealthBorder = 0.5f;

	public float MaxAlphaHealthBorder = 0.85f;

	public float MinVarianceHealthBorder = 0.08f;

	public float MaxVarianceHealthBorder = 0.15f;

	public float LerpSpeedHealthBorder = 0.2f;

	public float ResetSpeedHealthBorder = 0.05f;

	private readonly ScreenEffectRenderer _damageScreenEffectRenderer;

	private readonly ScreenEffectRenderer _healthAlertScreenEffectRenderer;

	private readonly List<DamageIndicator> _damageIndicators = new List<DamageIndicator>();

	private readonly Dictionary<string, DamageSpriteInfo> _damageIndicatorImages = new Dictionary<string, DamageSpriteInfo>();

	private readonly Dictionary<string, AnimationData> _animationDatas = new Dictionary<string, AnimationData>();

	private Matrix _projectionMatrix;

	private float _windowScale;

	private QuadRenderer _quadRenderer;

	private DamageDrawTask[] _damageDrawTasks = new DamageDrawTask[10];

	private int _damageDrawTasksCount;

	private float _healthBorderTimeElapsed;

	private float _previousHealthRatio = 1f;

	private float _previousStaminaRatio = 1f;

	private float _previousSignatureRatio = 1f;

	private uint _playerHealthRTPCId;

	private int _previousHealthRTPCValue = 100;

	private uint _playerStaminaRTPCId;

	private int _previousStaminaRTPCValue = 100;

	private uint _playerSignatureRTPCId;

	private int _previousSignatureRTPCValue = 100;

	public DamageEffectModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_damageScreenEffectRenderer = new ScreenEffectRenderer(_gameInstance.Engine);
		_healthAlertScreenEffectRenderer = new ScreenEffectRenderer(_gameInstance.Engine);
	}

	public override void Initialize()
	{
		InitDamageImages();
		InitAnimationDatas();
		InitSoundDatas();
		BasicProgram basicProgram = _gameInstance.Engine.Graphics.GPUProgramStore.BasicProgram;
		_quadRenderer = new QuadRenderer(_gameInstance.Engine.Graphics, basicProgram.AttribPosition, basicProgram.AttribTexCoords);
		Resize(_gameInstance.Engine.Window.Viewport.Width, _gameInstance.Engine.Window.Viewport.Height);
	}

	protected override void DoDispose()
	{
		_quadRenderer?.Dispose();
		_damageScreenEffectRenderer.Dispose();
		_healthAlertScreenEffectRenderer.Dispose();
		_gameInstance.Engine.Audio.SetRTPC(_playerHealthRTPCId, 100f);
		_gameInstance.Engine.Audio.SetRTPC(_playerStaminaRTPCId, 100f);
		_gameInstance.Engine.Audio.SetRTPC(_playerSignatureRTPCId, 100f);
	}

	private void InitDamageImages()
	{
		if (_gameInstance.HashesByServerAssetPath.TryGetValue("UI/DamageIndicators/HitIndicatorBasic.png", out var value))
		{
			try
			{
				_damageIndicatorImages.Add("UI/DamageIndicators/HitIndicatorBasic.png", CreateDamageSpriteInfo(value));
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to load damage texture: UI/DamageIndicators/HitIndicatorBasic.png");
			}
		}
		else
		{
			_gameInstance.App.DevTools.Error("Missing damage indicator asset: UI/DamageIndicators/HitIndicatorBasic.png");
		}
		if (_gameInstance.HashesByServerAssetPath.TryGetValue("UI/DamageIndicators/HitIndicatorCritic.png", out var value2))
		{
			try
			{
				_damageIndicatorImages.Add("UI/DamageIndicators/HitIndicatorCritic.png", CreateDamageSpriteInfo(value2));
			}
			catch (Exception ex2)
			{
				Logger.Error(ex2, "Failed to load damage texture: UI/DamageIndicators/HitIndicatorCritic.png");
			}
		}
		else
		{
			_gameInstance.App.DevTools.Error("Missing damage indicator asset: UI/DamageIndicators/HitIndicatorCritic.png");
		}
		if (_gameInstance.HashesByServerAssetPath.TryGetValue("UI/DamageIndicators/HitIndicatorMelee.png", out var value3))
		{
			try
			{
				_damageIndicatorImages.Add("UI/DamageIndicators/HitIndicatorMelee.png", CreateDamageSpriteInfo(value3));
			}
			catch (Exception ex3)
			{
				Logger.Error(ex3, "Failed to load damage texture: UI/DamageIndicators/HitIndicatorMelee.png");
			}
		}
		else
		{
			_gameInstance.App.DevTools.Error("Missing damage indicator asset: UI/DamageIndicators/HitIndicatorMelee.png");
		}
		_damageScreenEffectRenderer.Initialize();
		if (_gameInstance.HashesByServerAssetPath.TryGetValue("UI/DamageScreenEffect.png", out var value4))
		{
			_damageScreenEffectRenderer.RequestTextureUpdate(value4);
			_damageScreenEffectRenderer.Color = new Vector4(1f, 1f, 1f, 0f);
		}
		else
		{
			_gameInstance.App.DevTools.Error("Missing damage asset: UI/DamageScreenEffect.png");
		}
		_healthAlertScreenEffectRenderer.Initialize();
		if (_gameInstance.HashesByServerAssetPath.TryGetValue("UI/HealthAlertScreenEffect.png", out var value5))
		{
			_healthAlertScreenEffectRenderer.RequestTextureUpdate(value5);
			_healthAlertScreenEffectRenderer.Color = new Vector4(1f, 1f, 1f, 0f);
		}
		else
		{
			_gameInstance.App.DevTools.Error("Missing damage asset: UI/HealthAlertScreenEffect.png");
		}
	}

	private DamageSpriteInfo CreateDamageSpriteInfo(string hash)
	{
		Image image = new Image(AssetManager.GetAssetUsingHash(hash));
		DamageSpriteInfo result = default(DamageSpriteInfo);
		result.Texture = CreateTexture(image);
		result.Size = image.Width;
		return result;
	}

	private unsafe GLTexture CreateTexture(Image sprite)
	{
		GLFunctions gL = _gameInstance.Engine.Graphics.GL;
		GLTexture gLTexture = gL.GenTexture();
		gL.BindTexture(GL.TEXTURE_2D, gLTexture);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 10497);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 10497);
		fixed (byte* ptr = sprite.Pixels)
		{
			gL.TexImage2D(GL.TEXTURE_2D, 0, 6408, sprite.Width, sprite.Height, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
		}
		return gLTexture;
	}

	private void InitAnimationDatas()
	{
		_animationDatas.Add("Physical", new AnimationData
		{
			IndicatorDuration = 0.6f,
			MinAlpha = 0f,
			MaxAlpha = 1f,
			MinScaleFactor = 1f,
			MaxScaleFactor = 1.75f,
			ScaleDuration = 0.3f,
			MinRadius = 40f,
			MaxRadius = 90f,
			RadiusDuration = 0.3f
		});
		_animationDatas.Add("Projectile", new AnimationData
		{
			IndicatorDuration = 0.6f,
			MinAlpha = 0f,
			MaxAlpha = 1f,
			MinScaleFactor = 1f,
			MaxScaleFactor = 1.75f,
			ScaleDuration = 0.3f,
			MinRadius = 70f,
			MaxRadius = 120f,
			RadiusDuration = 0.3f
		});
	}

	private void InitSoundDatas()
	{
		if (!_gameInstance.Engine.Audio.ResourceManager.WwiseGameParameterIds.TryGetValue("HEALTH", out _playerHealthRTPCId))
		{
			_gameInstance.App.DevTools.Error("Missing health RTPC: HEALTH");
		}
		if (!_gameInstance.Engine.Audio.ResourceManager.WwiseGameParameterIds.TryGetValue("STAMINA", out _playerStaminaRTPCId))
		{
			_gameInstance.App.DevTools.Error("Missing stamina RTPC: STAMINA");
		}
		if (!_gameInstance.Engine.Audio.ResourceManager.WwiseGameParameterIds.TryGetValue("SIGNATURE", out _playerSignatureRTPCId))
		{
			_gameInstance.App.DevTools.Error("Missing signature energy RTPC: SIGNATURE");
		}
	}

	public void Resize(int width, int height)
	{
		_projectionMatrix = Matrix.CreateTranslation(0f, 0f, -1f) * Matrix.CreateOrthographicOffCenter((float)(-width) / 2f, (float)width / 2f, (float)(-height) / 2f, (float)height / 2f, 0.1f, 1000f);
		_windowScale = (float)_gameInstance.Engine.Window.Viewport.Height / 1080f;
	}

	public void Update(float deltaTime)
	{
		UpdateDamageIndicators(deltaTime);
		float num = 1f;
		ClientEntityStatValue entityStat = _gameInstance.LocalPlayer.GetEntityStat(DefaultEntityStats.Health);
		if (entityStat != null)
		{
			num = entityStat.Value / entityStat.Max;
		}
		if (num != _previousHealthRatio)
		{
			HandleHeartbeatSound(num);
		}
		UpdateFullScreenDamageEffects(num, deltaTime);
		_previousHealthRatio = num;
		float num2 = 1f;
		ClientEntityStatValue entityStat2 = _gameInstance.LocalPlayer.GetEntityStat(DefaultEntityStats.Stamina);
		if (entityStat2 != null)
		{
			num2 = entityStat2.Value / entityStat2.Max;
		}
		if (num2 != _previousStaminaRatio)
		{
			HandleStaminaSound(num2);
		}
		_previousStaminaRatio = num2;
		float num3 = 1f;
		ClientEntityStatValue entityStat3 = _gameInstance.LocalPlayer.GetEntityStat(DefaultEntityStats.SignatureEnergy);
		if (entityStat3 != null)
		{
			num3 = entityStat3.Value / entityStat3.Max;
		}
		if (num3 != _previousSignatureRatio)
		{
			HandleSignatureSound(num3);
		}
		_previousSignatureRatio = num3;
	}

	private void UpdateDamageIndicators(float deltaTime)
	{
		for (int num = _damageIndicators.Count - 1; num >= 0; num--)
		{
			DamageIndicator damageIndicator = _damageIndicators[num];
			damageIndicator.Update(deltaTime, _gameInstance.LocalPlayer.Position, _gameInstance.CameraModule.Controller.Rotation.Yaw);
			if (damageIndicator.IsFinished())
			{
				_damageIndicators.Remove(damageIndicator);
			}
		}
	}

	private void HandleHeartbeatSound(float currentHealthRatio)
	{
		int num = (int)(currentHealthRatio * 100f);
		if (num != _previousHealthRTPCValue)
		{
			_gameInstance.Engine.Audio.SetRTPC(_playerHealthRTPCId, num);
			_previousHealthRTPCValue = num;
		}
	}

	private void HandleStaminaSound(float currentStaminaRatio)
	{
		int num = (int)(currentStaminaRatio * 100f);
		if (num != _previousStaminaRTPCValue)
		{
			_gameInstance.Engine.Audio.SetRTPC(_playerStaminaRTPCId, num);
			_previousStaminaRTPCValue = num;
		}
	}

	private void HandleSignatureSound(float currentSignatureRatio)
	{
		int num = (int)(currentSignatureRatio * 100f);
		if (num != _previousSignatureRTPCValue)
		{
			_gameInstance.Engine.Audio.SetRTPC(_playerSignatureRTPCId, num);
			_previousSignatureRTPCValue = num;
		}
	}

	private float ConvertToNewRange(float value, float oldMinRange, float oldMaxRange, float newMinRange, float newMaxRange)
	{
		return (value - oldMinRange) * (newMaxRange - newMinRange) / (oldMaxRange - oldMinRange) + newMinRange;
	}

	private void UpdateFullScreenDamageEffects(float currentHealthRatio, float deltaTime)
	{
		if (_damageScreenEffectRenderer.Color.W > 0f)
		{
			_gameInstance.ScreenEffectStoreModule.RequestDraw(_damageScreenEffectRenderer);
			_damageScreenEffectRenderer.Color.W = MathHelper.Max(_damageScreenEffectRenderer.Color.W - deltaTime * 0.4f, 0f);
		}
		if (currentHealthRatio <= HealthAlertThreshold)
		{
			float num = ConvertToNewRange(currentHealthRatio, HealthAlertThreshold, 0f, MinAlphaHealthBorder, MaxAlphaHealthBorder);
			_healthBorderTimeElapsed += deltaTime;
			float num2 = _healthBorderTimeElapsed * (float)System.Math.PI;
			float newMaxRange = ConvertToNewRange(currentHealthRatio, HealthAlertThreshold, 0f, MinVarianceHealthBorder, MaxVarianceHealthBorder);
			float num3 = ConvertToNewRange((float)System.Math.Sin(num2), 0f, 1f, 0f, newMaxRange);
			_healthAlertScreenEffectRenderer.Color.W = MathHelper.Lerp(_healthAlertScreenEffectRenderer.Color.W, num + num3, LerpSpeedHealthBorder);
			_gameInstance.ScreenEffectStoreModule.RequestDraw(_healthAlertScreenEffectRenderer);
		}
		else if (_healthAlertScreenEffectRenderer.Color.W > 0f)
		{
			if (_healthAlertScreenEffectRenderer.Color.W <= 0.01f)
			{
				_healthAlertScreenEffectRenderer.Color.W = 0f;
			}
			else
			{
				_healthAlertScreenEffectRenderer.Color.W = MathHelper.Lerp(_healthAlertScreenEffectRenderer.Color.W, 0f, ResetSpeedHealthBorder);
			}
			_gameInstance.ScreenEffectStoreModule.RequestDraw(_healthAlertScreenEffectRenderer);
		}
	}

	public void PrepareForDraw()
	{
		ArrayUtils.GrowArrayIfNecessary(ref _damageDrawTasks, _damageIndicators.Count, 5);
		int num = 0;
		for (int i = 0; i < _damageIndicators.Count; i++)
		{
			DamageIndicator damageIndicator = _damageIndicators[i];
			if ((AngleHideDamage == 0 || !(System.Math.Abs(MathHelper.ToDegrees(damageIndicator.Angle)) < (float)AngleHideDamage / 2f)) && _damageIndicatorImages.TryGetValue(damageIndicator.SpriteToDisplay, out var value))
			{
				int num2 = num;
				_damageDrawTasks[num2].Texture = value.Texture;
				_damageDrawTasks[num2].Alpha = damageIndicator.Alpha;
				CalculateMatrix(num2, value.Size, damageIndicator);
				num++;
			}
		}
		_damageDrawTasksCount = num;
	}

	private void CalculateMatrix(int taskId, float imageSize, DamageIndicator damageEffect)
	{
		float num = imageSize * damageEffect.ScaleFactor * _windowScale;
		Matrix.CreateScale(num, num, 1f, out var result);
		float num2 = num / 2f;
		Matrix.CreateTranslation(0f - num2, 0f - num2, 0f, out _damageDrawTasks[taskId].MVPMatrix);
		Matrix.Multiply(ref result, ref _damageDrawTasks[taskId].MVPMatrix, out _damageDrawTasks[taskId].MVPMatrix);
		Matrix.CreateRotationZ(damageEffect.Angle, out result);
		Matrix.Multiply(ref _damageDrawTasks[taskId].MVPMatrix, ref result, out _damageDrawTasks[taskId].MVPMatrix);
		Matrix.CreateTranslation(num2, num2, 0f, out result);
		Matrix.Multiply(ref _damageDrawTasks[taskId].MVPMatrix, ref result, out _damageDrawTasks[taskId].MVPMatrix);
		Matrix.CreateTranslation(0f - num2 - damageEffect.OffsetSpritePosition.X * _windowScale, 0f - num2 - damageEffect.OffsetSpritePosition.Y * _windowScale, 0f, out result);
		Matrix.Multiply(ref _damageDrawTasks[taskId].MVPMatrix, ref result, out _damageDrawTasks[taskId].MVPMatrix);
		Matrix.Multiply(ref _damageDrawTasks[taskId].MVPMatrix, ref _projectionMatrix, out _damageDrawTasks[taskId].MVPMatrix);
	}

	public bool NeedsDrawing()
	{
		return _damageDrawTasksCount > 0;
	}

	public void Draw()
	{
		BasicProgram basicProgram = _gameInstance.Engine.Graphics.GPUProgramStore.BasicProgram;
		if (!NeedsDrawing())
		{
			throw new Exception("DrawDamageEffects called when it was not required. Please check with NeedsDrawing() first before calling this.");
		}
		basicProgram.AssertInUse();
		basicProgram.Color.AssertValue(_gameInstance.Engine.Graphics.WhiteColor);
		GLFunctions gL = _gameInstance.Engine.Graphics.GL;
		for (int i = 0; i < _damageDrawTasksCount; i++)
		{
			gL.BindTexture(GL.TEXTURE_2D, _damageDrawTasks[i].Texture);
			basicProgram.Opacity.SetValue(_damageDrawTasks[i].Alpha);
			basicProgram.MVPMatrix.SetValue(ref _damageDrawTasks[i].MVPMatrix);
			_quadRenderer.Draw();
		}
	}

	public void AddDamageEffect(Vector3d damageSourcePosition, float damageAmount, DamageCause damageCause)
	{
		if (_animationDatas.TryGetValue(damageCause.Id, out var value))
		{
			_damageIndicators.Add(new DamageIndicator(damageSourcePosition, GetSpritePathToDisplay(damageAmount, damageCause), value));
		}
	}

	private string GetSpritePathToDisplay(float damageAmount, DamageCause damageCause)
	{
		if (damageCause.Id.Equals("Projectile"))
		{
			ClientEntityStatValue entityStat = _gameInstance.LocalPlayer.GetEntityStat(DefaultEntityStats.Health);
			if (entityStat != null)
			{
				return (damageAmount / entityStat.Max > 0.2f) ? "UI/DamageIndicators/HitIndicatorCritic.png" : "UI/DamageIndicators/HitIndicatorBasic.png";
			}
		}
		return "UI/DamageIndicators/HitIndicatorMelee.png";
	}

	public void IncreaseDamageEffect(float alpha)
	{
		_damageScreenEffectRenderer.Color.W = MathHelper.Min(_damageScreenEffectRenderer.Color.W + alpha, 1f);
	}
}
