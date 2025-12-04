#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Data.Map;
using HytaleClient.Data.Weather;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Particles;
using HytaleClient.Graphics.Sky;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules;

internal class WeatherModule : Module
{
	public enum FogMode
	{
		Dynamic,
		Static,
		Off
	}

	public static byte TotalMoonPhases = 6;

	public static byte DaylightPortion = 50;

	public const float SunDefaultHeight = 2f;

	private const float ChangeWeatherDelay = 10f;

	private const float InverseChangeWeatherDelay = 0.1f;

	private const float MoonsetHeight = 0.2f;

	public readonly SkyRenderer SkyRenderer;

	private int _previousEnvironmentIndex;

	private int _serverWeatherIndex = 0;

	private int _editorWeatherOverrideIndex = 0;

	private int _currentEnvironmentWeatherIndex = 0;

	private int _targetEnvironmentWeatherIndex = 0;

	private float _changeWeatherTimer;

	private bool _hasRequestedSkyTextureUpdate;

	private float _accumulatedDeltaTime;

	private int _moonPhase;

	private int _previousFluidFXId = -1;

	public Vector3 FluidHorizonPosition;

	public Vector3 FluidHorizonScale;

	private static readonly Vector3 MinFluidLightColor = new Vector3(0.4f);

	public FogMode ActiveFogMode = FogMode.Dynamic;

	public float FogDepthStart;

	public float FogDepthFalloff;

	private const float FogLerpFactor = 0.05f;

	private const float MinFogLength = 48f;

	private Vector3 _tempColor;

	private Vector4 _tempColorAlpha;

	private Vector4 _skyTopGradientColor;

	private Vector4 _skyBottomGradientColor;

	private Vector4 _sunsetColor;

	private Vector3 _sunlightColor = Vector3.One;

	private Vector3 _sunColor;

	private Vector4 _moonColor;

	private Vector4 _sunGlowColor;

	private Vector4 _moonGlowColor;

	public float StarsOpacity;

	private float _starsTransitionOpacity;

	private float _moonTransitionOpacity;

	private bool _transitionClouds;

	private bool _transitionMoon;

	private bool _transitionStars;

	public float SunHeight = 2f;

	private float _sunlightDampingMultiplier;

	private float _sunScale;

	private float _moonScale;

	private float _daylightDuration;

	private float _halfDaylightDuration;

	private float _inverseAllDaylightDay;

	private float _nightDuration;

	private float _halfNightDuration;

	private float _inverseAllNightDay;

	public Quaternion SkyRotation = Quaternion.Identity;

	private Vector3 _colorFilter;

	private Vector3 _waterTintColor;

	private float _cloudOffset = 0f;

	private float _cloudsTransitionOpacity;

	private Vector3 _fogColor;

	private float _fogHeightFalloff;

	private float _fogDensity;

	private ParticleSystemProxy _particleSystemProxy;

	private ParticleSystemProxy _fluidParticleSystemProxy;

	private ParticleSystemProxy _fluidEnvironmentParticleSystemProxy;

	private string _currentFluidParticleSystemId;

	private string _currentFluidEnvironmentParticleSystemId;

	private bool _resetFluidEnvironmentParticleSystem = false;

	private float _particleSystemPositionOffsetMultiplier;

	private Vector3 _lastCameraPosition;

	private bool _transitionScreenEffect;

	private float _screenEffectTransitionOpacity;

	public int CurrentWeatherIndex => (_editorWeatherOverrideIndex != 0) ? _editorWeatherOverrideIndex : _currentEnvironmentWeatherIndex;

	public int NextWeatherIndex => _targetEnvironmentWeatherIndex;

	public ClientWeather CurrentWeather => _gameInstance.ServerSettings.Weathers[CurrentWeatherIndex];

	public ClientWeather NextWeather => _gameInstance.ServerSettings.Weathers[_targetEnvironmentWeatherIndex];

	public bool IsChangingWeather => _editorWeatherOverrideIndex == 0 && _targetEnvironmentWeatherIndex != 0;

	public float NextWeatherProgress => 1f - _changeWeatherTimer * 0.1f;

	public int CurrentEnvironmentIndex { get; private set; } = 0;


	public ClientWorldEnvironment CurrentEnvironment => _gameInstance.ServerSettings.Environments[CurrentEnvironmentIndex];

	public float SunLight { get; private set; }

	public Vector3 SunlightColor => _sunlightColor;

	public Vector3 SunColor => _sunColor;

	public Vector4 MoonColor => _moonColor;

	public Vector4 SunGlowColor => _sunGlowColor;

	public Vector4 MoonGlowColor => _moonGlowColor;

	public float SunScale => _sunScale;

	public float MoonScale => _moonScale;

	public Vector3 ColorFilter => _colorFilter;

	public FluidFX FluidFX { get; private set; }

	public int FluidFXIndex { get; private set; } = 0;


	public bool IsUnderWater => FluidFXIndex != 0;

	public float FluidHeight { get; private set; }

	public Vector3 FluidBlockLightColor { get; private set; }

	public Vector3 WaterTintColor => _waterTintColor;

	public Vector3 NormalizedSunPosition { get; private set; }

	public Vector4 SkyTopGradientColor => _skyTopGradientColor;

	public Vector4 SkyBottomGradientColor => _skyBottomGradientColor;

	public Vector4 SunsetColor => _sunsetColor;

	public float SunAngle { get; private set; }

	public float CloudsTransitionOpacity => _cloudsTransitionOpacity;

	public Vector3 FogColor => _fogColor;

	public float LerpFogStart { get; private set; }

	public float LerpFogEnd { get; private set; }

	public float FogHeightFalloff => _fogHeightFalloff;

	public float FogDensity => _fogDensity;

	public WeatherModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		SkyRenderer = new SkyRenderer(_gameInstance.Engine);
		OnDaylightPortionChanged();
	}

	protected override void DoDispose()
	{
		SkyRenderer.Dispose();
	}

	public override void Initialize()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		SkyRenderer.Initialize();
		if (_gameInstance.HashesByServerAssetPath.TryGetValue("Sky/Sun.png", out var value))
		{
			SkyRenderer.LoadSunTexture(value);
		}
	}

	public void UpdateMoonPhase()
	{
		DateTime gameTime = _gameInstance.TimeModule.GameTime;
		int num = (gameTime.DayOfYear - 1) % TotalMoonPhases;
		int moonPhase = _moonPhase;
		if (gameTime.Hour < 12)
		{
			if (num == 0)
			{
				_moonPhase = TotalMoonPhases - 1;
			}
			else
			{
				_moonPhase = num - 1;
			}
		}
		else
		{
			_moonPhase = num;
		}
		if (_moonPhase != moonPhase)
		{
			RequestMoonTextureUpdateFromWeather(CurrentWeather);
		}
	}

	public void Update(float deltaTime)
	{
		//IL_0692: Unknown result type (might be due to invalid IL or missing references)
		//IL_0697: Unknown result type (might be due to invalid IL or missing references)
		//IL_0699: Unknown result type (might be due to invalid IL or missing references)
		//IL_069b: Unknown result type (might be due to invalid IL or missing references)
		//IL_069d: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b0: Expected I4, but got Unknown
		_accumulatedDeltaTime += deltaTime;
		if (_accumulatedDeltaTime < 1f / 60f)
		{
			return;
		}
		Vector3 position = _gameInstance.CameraModule.Controller.Position;
		int num = (int)System.Math.Floor(position.X);
		int num2 = (int)System.Math.Floor(position.Y);
		int num3 = (int)System.Math.Floor(position.Z);
		int block = _gameInstance.MapModule.GetBlock(num, num2, num3, 0);
		bool flag = FluidFXIndex != 0;
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
		if (clientBlockType.FluidBlockId != block)
		{
			clientBlockType = _gameInstance.MapModule.ClientBlockTypes[clientBlockType.FluidBlockId];
		}
		int fluidFXIndex = clientBlockType.FluidFXIndex;
		if (FluidFXIndex != fluidFXIndex)
		{
			FluidFXIndex = fluidFXIndex;
			FluidFX = _gameInstance.ServerSettings.FluidFXs[FluidFXIndex];
			flag = FluidFXIndex != 0;
			if (flag)
			{
				int i;
				for (i = num2; _gameInstance.MapModule.GetBlock(num, i, num3, 0) != 0; i++)
				{
				}
				FluidHeight = i;
			}
		}
		Vector3 position2 = _gameInstance.LocalPlayer.Position;
		int num4 = (int)System.Math.Floor(position2.X);
		int worldY = (int)System.Math.Floor(position2.Y);
		int num5 = (int)System.Math.Floor(position2.Z);
		int num6 = num4 >> 5;
		int num7 = num5 >> 5;
		ChunkColumn chunkColumn = _gameInstance.MapModule.GetChunkColumn(num6, num7);
		if (chunkColumn != null)
		{
			int chunkX = num4 - num6 * 32;
			int chunkZ = num5 - num7 * 32;
			ushort environmentId = ChunkHelper.GetEnvironmentId(chunkColumn.Environments, chunkX, chunkZ, worldY);
			if (CurrentEnvironmentIndex == 0 || CurrentEnvironmentIndex != environmentId)
			{
				CurrentEnvironmentIndex = environmentId;
			}
		}
		else
		{
			CurrentEnvironmentIndex = 0;
		}
		if (IsChangingWeather)
		{
			_changeWeatherTimer = System.Math.Max(0f, _changeWeatherTimer - 1f / 60f);
			if (NextWeatherProgress == 1f)
			{
				_currentEnvironmentWeatherIndex = _targetEnvironmentWeatherIndex;
				_targetEnvironmentWeatherIndex = 0;
				UpdateEnvironmentWeather();
			}
		}
		TimeModule timeModule = _gameInstance.TimeModule;
		float gameDayProgressInHours = timeModule.GameDayProgressInHours;
		if (gameDayProgressInHours < _halfNightDuration)
		{
			SunAngle = MathHelper.WrapAngle((gameDayProgressInHours * _inverseAllNightDay - _halfNightDuration * _inverseAllNightDay) * ((float)System.Math.PI * 2f));
		}
		else if (gameDayProgressInHours > 24f - _halfNightDuration)
		{
			SunAngle = MathHelper.WrapAngle((gameDayProgressInHours * _inverseAllNightDay - (24f + _halfNightDuration) * _inverseAllNightDay) * ((float)System.Math.PI * 2f));
		}
		else
		{
			SunAngle = MathHelper.WrapAngle((gameDayProgressInHours * _inverseAllDaylightDay - (12f - _halfDaylightDuration) * _inverseAllDaylightDay) * ((float)System.Math.PI * 2f));
		}
		Vector3 normalizedSunPosition = Vector3.Transform(new Vector3((float)System.Math.Cos(SunAngle), (float)System.Math.Sin(SunAngle) * SunHeight, (float)System.Math.Sin(SunAngle)), SkyRotation);
		normalizedSunPosition.Normalize();
		NormalizedSunPosition = normalizedSunPosition;
		ClientWeather currentWeather = CurrentWeather;
		UpdateTimeValue(out _sunlightDampingMultiplier, gameDayProgressInHours, currentWeather.SunlightDampingMultipliers);
		UpdateColor(out _sunlightColor, gameDayProgressInHours, currentWeather.SunlightColors);
		UpdateColor(out _sunColor, gameDayProgressInHours, currentWeather.SunColors);
		UpdateColor(out _moonColor, gameDayProgressInHours, currentWeather.MoonColors);
		UpdateColor(out _sunGlowColor, gameDayProgressInHours, currentWeather.SunGlowColors);
		UpdateColor(out _moonGlowColor, gameDayProgressInHours, currentWeather.MoonGlowColors);
		UpdateTimeValue(out _sunScale, gameDayProgressInHours, currentWeather.SunScales);
		UpdateTimeValue(out _moonScale, gameDayProgressInHours, currentWeather.MoonScales);
		UpdateColor(out _colorFilter, gameDayProgressInHours, currentWeather.ColorFilters);
		UpdateColor(out _gameInstance.ScreenEffectStoreModule.WeatherScreenEffectRenderer.Color, gameDayProgressInHours, currentWeather.ScreenEffectColors);
		UpdateColor(out _skyTopGradientColor, gameDayProgressInHours, currentWeather.SkyTopColors);
		UpdateColor(out _skyBottomGradientColor, gameDayProgressInHours, currentWeather.SkyBottomColors);
		UpdateColor(out _sunsetColor, gameDayProgressInHours, currentWeather.SkySunsetColors);
		StarsOpacity = 1f;
		UpdateTimeValue(out _fogHeightFalloff, gameDayProgressInHours, currentWeather.FogHeightFalloffs);
		UpdateTimeValue(out _fogDensity, gameDayProgressInHours, currentWeather.FogDensities);
		_cloudOffset = ((timeModule.IsServerTimePaused && !timeModule.IsEditorTimeOverrideActive) ? ((_cloudOffset + _accumulatedDeltaTime * ((float)TimeModule.SecondsPerGameDay / 86400f)) % 1f) : (gameDayProgressInHours % 1f));
		for (int j = 0; j < currentWeather.Clouds.Length; j++)
		{
			UpdateColor(out SkyRenderer.CloudColors[j], gameDayProgressInHours, currentWeather.Clouds[j].Colors);
			UpdateTimeValue(out var _, gameDayProgressInHours, currentWeather.Clouds[j].Speeds);
			int num8 = 0;
			Tuple<float, float>[] speeds = currentWeather.Clouds[j].Speeds;
			foreach (Tuple<float, float> tuple in speeds)
			{
				if (tuple.Item1 < gameDayProgressInHours)
				{
					num8 = (int)tuple.Item2;
				}
			}
			SkyRenderer.CloudOffsets[j] = (float)num8 * _cloudOffset;
		}
		UpdateColor(out _waterTintColor, gameDayProgressInHours, currentWeather.WaterTints);
		if (flag)
		{
			Vector4 lightColorAtBlockPosition = _gameInstance.MapModule.GetLightColorAtBlockPosition(num, num2, num3);
			FluidBlockLightColor = Vector3.Max(MinFluidLightColor, Vector3.Lerp(FluidBlockLightColor, new Vector3(lightColorAtBlockPosition.X, lightColorAtBlockPosition.Y, lightColorAtBlockPosition.Z), 0.01f));
			FluidFog fogMode = FluidFX.FogMode;
			FluidFog val = fogMode;
			switch ((int)val)
			{
			case 2:
				_fogColor = new Vector3(_waterTintColor.X * FluidBlockLightColor.X, _waterTintColor.Y * FluidBlockLightColor.Y, _waterTintColor.Z * FluidBlockLightColor.Z);
				break;
			case 1:
				_fogColor = new Vector3((float)(int)(byte)FluidFX.FogColor.Red / 255f * FluidBlockLightColor.X, (float)(int)(byte)FluidFX.FogColor.Green / 255f * FluidBlockLightColor.Y, (float)(int)(byte)FluidFX.FogColor.Blue / 255f * FluidBlockLightColor.Z);
				break;
			case 0:
				_fogColor = new Vector3((float)(int)(byte)FluidFX.FogColor.Red / 255f, (float)(int)(byte)FluidFX.FogColor.Green / 255f, (float)(int)(byte)FluidFX.FogColor.Blue / 255f);
				break;
			}
			FogDepthStart = FluidFX.FogDepthStart;
			FogDepthFalloff = FluidFX.FogDepthFalloff;
			if (_currentFluidParticleSystemId != FluidFX.Particle?.SystemId)
			{
				_currentFluidParticleSystemId = FluidFX.Particle?.SystemId;
				if (_fluidParticleSystemProxy != null)
				{
					_fluidParticleSystemProxy.Expire();
					_fluidParticleSystemProxy = null;
				}
				if (_currentFluidParticleSystemId != null && _gameInstance.ParticleSystemStoreModule.TrySpawnSystem(_currentFluidParticleSystemId, out _fluidParticleSystemProxy, isLocalPlayer: false, isTracked: true))
				{
					if (FluidFX.Particle.Color_ != null)
					{
						_fluidParticleSystemProxy.DefaultColor = UInt32Color.FromRGBA((byte)FluidFX.Particle.Color_.Red, (byte)FluidFX.Particle.Color_.Green, (byte)FluidFX.Particle.Color_.Blue, byte.MaxValue);
					}
					_fluidParticleSystemProxy.Scale = FluidFX.Particle.Scale;
				}
			}
			if (_previousEnvironmentIndex != CurrentEnvironmentIndex || FluidFXIndex != _previousFluidFXId || _resetFluidEnvironmentParticleSystem)
			{
				if (CurrentEnvironment.FluidParticles.TryGetValue(FluidFXIndex, out var value2))
				{
					if (value2.SystemId != _currentFluidEnvironmentParticleSystemId)
					{
						_currentFluidEnvironmentParticleSystemId = value2.SystemId;
						if (_fluidEnvironmentParticleSystemProxy != null)
						{
							_fluidEnvironmentParticleSystemProxy.Expire();
							_fluidEnvironmentParticleSystemProxy = null;
						}
						if (_currentFluidEnvironmentParticleSystemId != null && _gameInstance.ParticleSystemStoreModule.TrySpawnSystem(_currentFluidEnvironmentParticleSystemId, out _fluidEnvironmentParticleSystemProxy, isLocalPlayer: false, isTracked: true))
						{
							if (value2.Color_ != null)
							{
								_fluidEnvironmentParticleSystemProxy.DefaultColor = UInt32Color.FromRGBA((byte)value2.Color_.Red, (byte)value2.Color_.Green, (byte)value2.Color_.Blue, byte.MaxValue);
							}
							_fluidEnvironmentParticleSystemProxy.Scale = value2.Scale;
						}
					}
				}
				else if (_fluidEnvironmentParticleSystemProxy != null)
				{
					_fluidEnvironmentParticleSystemProxy.Expire();
					_fluidEnvironmentParticleSystemProxy = null;
					_currentFluidEnvironmentParticleSystemId = null;
				}
				_resetFluidEnvironmentParticleSystem = false;
			}
		}
		else
		{
			UpdateColor(out _fogColor, gameDayProgressInHours, currentWeather.FogColors);
			FogDepthFalloff = 0f;
			if (_fluidParticleSystemProxy != null)
			{
				_fluidParticleSystemProxy.Expire();
				_fluidParticleSystemProxy = null;
				_currentFluidParticleSystemId = null;
			}
			if (_fluidEnvironmentParticleSystemProxy != null)
			{
				_fluidEnvironmentParticleSystemProxy.Expire();
				_fluidEnvironmentParticleSystemProxy = null;
				_currentFluidEnvironmentParticleSystemId = null;
			}
		}
		float value3 = (flag ? FluidFX.FogDistance.Near : currentWeather.Fog.Near);
		LerpFogStart = MathHelper.Min(0f, value3);
		float value4 = (flag ? FluidFX.FogDistance.Far : currentWeather.Fog.Far);
		float value5 = ((_gameInstance.WeatherModule.ActiveFogMode == FogMode.Static) ? ((float)_gameInstance.App.Settings.ViewDistance) : _gameInstance.MapModule.EffectiveViewDistance);
		float num9 = MathHelper.Min(value4, MathHelper.Max(value5, 48f));
		LerpFogEnd = ((num9 <= LerpFogEnd) ? num9 : MathHelper.Lerp(LerpFogEnd, num9, 0.05f));
		if (IsChangingWeather)
		{
			ClientWeather nextWeather = NextWeather;
			UpdateTimeValue(out var value6, gameDayProgressInHours, nextWeather.SunlightDampingMultipliers);
			_sunlightDampingMultiplier = MathHelper.Lerp(_sunlightDampingMultiplier, value6, NextWeatherProgress);
			UpdateColor(out _tempColor, gameDayProgressInHours, nextWeather.SunlightColors);
			Vector3.Lerp(ref _sunlightColor, ref _tempColor, NextWeatherProgress, out _sunlightColor);
			UpdateColor(out _tempColor, gameDayProgressInHours, nextWeather.SunColors);
			Vector3.Lerp(ref _sunColor, ref _tempColor, NextWeatherProgress, out _sunColor);
			UpdateColor(out _tempColorAlpha, gameDayProgressInHours, nextWeather.MoonColors);
			Vector4.Lerp(ref _moonColor, ref _tempColorAlpha, NextWeatherProgress, out _moonColor);
			UpdateColor(out _tempColorAlpha, gameDayProgressInHours, nextWeather.SunGlowColors);
			Vector4.Lerp(ref _sunGlowColor, ref _tempColorAlpha, NextWeatherProgress, out _sunGlowColor);
			UpdateColor(out _tempColorAlpha, gameDayProgressInHours, nextWeather.MoonGlowColors);
			Vector4.Lerp(ref _moonGlowColor, ref _tempColorAlpha, NextWeatherProgress, out _moonGlowColor);
			UpdateTimeValue(out value6, gameDayProgressInHours, nextWeather.SunScales);
			_sunScale = MathHelper.Lerp(_sunScale, value6, NextWeatherProgress);
			UpdateTimeValue(out value6, gameDayProgressInHours, nextWeather.MoonScales);
			_moonScale = MathHelper.Lerp(_moonScale, value6, NextWeatherProgress);
			UpdateColor(out _tempColor, gameDayProgressInHours, nextWeather.ColorFilters);
			Vector3.Lerp(ref _colorFilter, ref _tempColor, NextWeatherProgress, out _colorFilter);
			UpdateColor(out _tempColorAlpha, gameDayProgressInHours, nextWeather.ScreenEffectColors);
			Vector4.Lerp(ref _gameInstance.ScreenEffectStoreModule.WeatherScreenEffectRenderer.Color, ref _tempColorAlpha, NextWeatherProgress, out _gameInstance.ScreenEffectStoreModule.WeatherScreenEffectRenderer.Color);
			UpdateColor(out _tempColorAlpha, gameDayProgressInHours, nextWeather.SkyTopColors);
			Vector4.Lerp(ref _skyTopGradientColor, ref _tempColorAlpha, NextWeatherProgress, out _skyTopGradientColor);
			UpdateColor(out _tempColorAlpha, gameDayProgressInHours, nextWeather.SkyBottomColors);
			Vector4.Lerp(ref _skyBottomGradientColor, ref _tempColorAlpha, NextWeatherProgress, out _skyBottomGradientColor);
			UpdateColor(out _tempColorAlpha, gameDayProgressInHours, nextWeather.SkySunsetColors);
			Vector4.Lerp(ref _sunsetColor, ref _tempColorAlpha, NextWeatherProgress, out _sunsetColor);
			UpdateColor(out _tempColor, gameDayProgressInHours, nextWeather.WaterTints);
			Vector3.Lerp(ref _waterTintColor, ref _tempColor, NextWeatherProgress, out _waterTintColor);
			UpdateTimeValue(out value6, gameDayProgressInHours, nextWeather.FogHeightFalloffs);
			_fogHeightFalloff = MathHelper.Lerp(_fogHeightFalloff, value6, NextWeatherProgress);
			UpdateTimeValue(out value6, gameDayProgressInHours, nextWeather.FogDensities);
			_fogDensity = MathHelper.Lerp(_fogDensity, value6, NextWeatherProgress);
			for (int l = 0; l < nextWeather.Clouds.Length; l++)
			{
				UpdateColor(out _tempColorAlpha, gameDayProgressInHours, nextWeather.Clouds[l].Colors);
				Vector4.Lerp(ref SkyRenderer.CloudColors[l], ref _tempColorAlpha, NextWeatherProgress, out SkyRenderer.CloudColors[l]);
			}
			float num10 = 0f;
			if (NextWeatherProgress < 0.25f)
			{
				num10 = 1f - NextWeatherProgress * 4f;
			}
			_cloudsTransitionOpacity = (_transitionClouds ? num10 : 1f);
			_moonTransitionOpacity = (_transitionMoon ? num10 : 1f);
			_starsTransitionOpacity = (_transitionStars ? num10 : 1f);
			_screenEffectTransitionOpacity = (_transitionScreenEffect ? num10 : 1f);
			if (NextWeatherProgress >= 0.75f && !_hasRequestedSkyTextureUpdate)
			{
				_hasRequestedSkyTextureUpdate = true;
				RequestTextureUpdateFromWeather(nextWeather);
				SetWeatherParticles(nextWeather.Particle);
			}
			if (!flag)
			{
				UpdateColor(out _tempColor, gameDayProgressInHours, nextWeather.FogColors);
				Vector3.Lerp(ref _fogColor, ref _tempColor, NextWeatherProgress, out _fogColor);
				float near = nextWeather.Fog.Near;
				float value7 = MathHelper.Min(0f, near);
				LerpFogStart = MathHelper.Lerp(LerpFogStart, value7, NextWeatherProgress);
				float far = nextWeather.Fog.Far;
				float value8 = MathHelper.Min(far, MathHelper.Max(value5, 48f));
				LerpFogEnd = MathHelper.Lerp(LerpFogEnd, value8, NextWeatherProgress);
			}
		}
		SunLight = MathHelper.Clamp((float)(System.Math.Sin(SunAngle) * 2.0 + 0.2) * _sunlightDampingMultiplier, 0.125f, _sunlightDampingMultiplier);
		if (flag)
		{
			FluidHorizonPosition = new Vector3(position.X, FluidHeight / 2f, position.Z);
			FluidHorizonScale = new Vector3(LerpFogEnd, FluidHeight, LerpFogEnd);
		}
		if (!SkyRenderer.IsCloudsTextureLoading)
		{
			_cloudsTransitionOpacity = MathHelper.Lerp(_cloudsTransitionOpacity, 1f, 0.01f);
		}
		for (int m = 0; m < currentWeather.Clouds.Length; m++)
		{
			SkyRenderer.CloudColors[m].W *= _cloudsTransitionOpacity;
		}
		if (0f - NormalizedSunPosition.Y < 0.2f)
		{
			_moonColor.W *= (0f - NormalizedSunPosition.Y) / 0.2f;
		}
		if (!SkyRenderer.IsMoonTextureLoading)
		{
			_moonTransitionOpacity = MathHelper.Lerp(_moonTransitionOpacity, 1f, 0.01f);
		}
		_moonColor.W *= _moonTransitionOpacity;
		if (!SkyRenderer.IsStarsTextureLoading)
		{
			_starsTransitionOpacity = MathHelper.Lerp(_starsTransitionOpacity, 1f, 0.01f);
		}
		StarsOpacity *= _starsTransitionOpacity;
		if (_gameInstance.ScreenEffectStoreModule.EntityScreenEffects.Count > 0)
		{
			_screenEffectTransitionOpacity = MathHelper.Lerp(_screenEffectTransitionOpacity, 0f, 0.01f);
		}
		else if (!_gameInstance.ScreenEffectStoreModule.WeatherScreenEffectRenderer.IsScreenEffectTextureLoading)
		{
			_screenEffectTransitionOpacity = MathHelper.Lerp(_screenEffectTransitionOpacity, 1f, 0.01f);
		}
		_gameInstance.ScreenEffectStoreModule.WeatherScreenEffectRenderer.Color.W *= _screenEffectTransitionOpacity;
		if (_particleSystemProxy != null)
		{
			_particleSystemProxy.Position = position + (position - _lastCameraPosition) * _particleSystemPositionOffsetMultiplier;
			_particleSystemProxy.Rotation = Quaternion.CreateFromYawPitchRoll(_gameInstance.CameraModule.Controller.Rotation.Yaw, 0f, 0f);
			_lastCameraPosition = position;
		}
		if (_fluidParticleSystemProxy != null)
		{
			_fluidParticleSystemProxy.Position = position;
			_fluidParticleSystemProxy.Rotation = Quaternion.CreateFromYawPitchRoll(_gameInstance.CameraModule.Controller.Rotation.Yaw, 0f, 0f);
		}
		if (_fluidEnvironmentParticleSystemProxy != null)
		{
			_fluidEnvironmentParticleSystemProxy.Position = position;
			_fluidEnvironmentParticleSystemProxy.Rotation = Quaternion.CreateFromYawPitchRoll(_gameInstance.CameraModule.Controller.Rotation.Yaw, 0f, 0f);
		}
		_accumulatedDeltaTime = 0f;
		_previousFluidFXId = FluidFXIndex;
		_previousEnvironmentIndex = CurrentEnvironmentIndex;
	}

	private float GetProgress(float startTime, float endTime, float dayTime)
	{
		if (startTime > endTime)
		{
			endTime += 24f;
			if (dayTime < startTime)
			{
				dayTime += 24f;
			}
		}
		return (dayTime - startTime) / (endTime - startTime);
	}

	private void UpdateTimeValue(out float value, float dayTime, Tuple<float, float>[] list)
	{
		if (list.Length == 1)
		{
			value = list[0].Item2;
			return;
		}
		int num = list.Length - 1;
		for (int i = 0; i < list.Length; i++)
		{
			if (list[i].Item1 <= dayTime)
			{
				num = i;
			}
		}
		Tuple<float, float> tuple = list[num];
		int num2 = (num + 1) % list.Length;
		Tuple<float, float> tuple2 = list[num2];
		value = MathHelper.Lerp(tuple.Item2, tuple2.Item2, GetProgress(tuple.Item1, tuple2.Item1, dayTime));
	}

	private void UpdateColor(out Vector3 color, float dayTime, Tuple<float, Color>[] list)
	{
		if (list.Length == 1)
		{
			Tuple<float, Color> tuple = list[0];
			color.X = (float)(int)(byte)tuple.Item2.Red / 255f;
			color.Y = (float)(int)(byte)tuple.Item2.Green / 255f;
			color.Z = (float)(int)(byte)tuple.Item2.Blue / 255f;
			return;
		}
		int num = list.Length - 1;
		for (int i = 0; i < list.Length; i++)
		{
			if (list[i].Item1 <= dayTime)
			{
				num = i;
			}
		}
		Tuple<float, Color> tuple2 = list[num];
		int num2 = (num + 1) % list.Length;
		Tuple<float, Color> tuple3 = list[num2];
		byte r = (byte)tuple2.Item2.Red;
		byte g = (byte)tuple2.Item2.Green;
		byte b = (byte)tuple2.Item2.Blue;
		ColorHsva color2 = ColorHsva.FromRgba(r, g, b, byte.MaxValue);
		byte r2 = (byte)tuple3.Item2.Red;
		byte g2 = (byte)tuple3.Item2.Green;
		byte b2 = (byte)tuple3.Item2.Blue;
		ColorHsva color3 = ColorHsva.FromRgba(r2, g2, b2, byte.MaxValue);
		ColorHsva.Lerp(color2, color3, GetProgress(tuple2.Item1, tuple3.Item1, dayTime)).ToRgba(out color.X, out color.Y, out color.Z, out float _);
	}

	private void UpdateColor(out Vector4 color, float dayTime, Tuple<float, ColorAlpha>[] list)
	{
		if (list.Length == 1)
		{
			Tuple<float, ColorAlpha> tuple = list[0];
			color.X = (float)(int)(byte)tuple.Item2.Red / 255f;
			color.Y = (float)(int)(byte)tuple.Item2.Green / 255f;
			color.Z = (float)(int)(byte)tuple.Item2.Blue / 255f;
			color.W = (float)(int)(byte)tuple.Item2.Alpha / 255f;
			return;
		}
		int num = list.Length - 1;
		for (int i = 0; i < list.Length; i++)
		{
			if (list[i].Item1 <= dayTime)
			{
				num = i;
			}
		}
		Tuple<float, ColorAlpha> tuple2 = list[num];
		int num2 = (num + 1) % list.Length;
		Tuple<float, ColorAlpha> tuple3 = list[num2];
		byte a = (byte)tuple2.Item2.Alpha;
		byte r = (byte)tuple2.Item2.Red;
		byte g = (byte)tuple2.Item2.Green;
		byte b = (byte)tuple2.Item2.Blue;
		ColorHsva color2 = ColorHsva.FromRgba(r, g, b, a);
		byte a2 = (byte)tuple3.Item2.Alpha;
		byte r2 = (byte)tuple3.Item2.Red;
		byte g2 = (byte)tuple3.Item2.Green;
		byte b2 = (byte)tuple3.Item2.Blue;
		ColorHsva color3 = ColorHsva.FromRgba(r2, g2, b2, a2);
		ColorHsva.Lerp(color2, color3, GetProgress(tuple2.Item1, tuple3.Item1, dayTime)).ToRgba(out color.X, out color.Y, out color.Z, out color.W);
	}

	public void UpdateEnvironmentWeather(bool fromForecast = false)
	{
		if (_targetEnvironmentWeatherIndex != 0 || (_currentEnvironmentWeatherIndex != 0 && _serverWeatherIndex == _currentEnvironmentWeatherIndex))
		{
			return;
		}
		float gameDayProgressInHours = _gameInstance.TimeModule.GameDayProgressInHours;
		if (_currentEnvironmentWeatherIndex == 0 || _editorWeatherOverrideIndex != 0)
		{
			_currentEnvironmentWeatherIndex = _serverWeatherIndex;
			if (_editorWeatherOverrideIndex == 0)
			{
				ClientWeather currentWeather = CurrentWeather;
				RequestTextureUpdateFromWeather(currentWeather);
				SetWeatherParticles(currentWeather.Particle);
				_hasRequestedSkyTextureUpdate = true;
			}
			return;
		}
		_targetEnvironmentWeatherIndex = _serverWeatherIndex;
		_changeWeatherTimer = 10f;
		_hasRequestedSkyTextureUpdate = false;
		ClientWeather clientWeather = _gameInstance.ServerSettings.Weathers[_currentEnvironmentWeatherIndex];
		ClientWeather nextWeather = NextWeather;
		_transitionClouds = nextWeather.Clouds.Length != clientWeather.Clouds.Length;
		for (int i = 0; i < nextWeather.Clouds.Length; i++)
		{
			_transitionClouds = _transitionClouds || clientWeather.Clouds[i].Texture != nextWeather.Clouds[i].Texture;
			if (fromForecast)
			{
				continue;
			}
			int num = 0;
			Tuple<float, float>[] speeds = clientWeather.Clouds[i].Speeds;
			foreach (Tuple<float, float> tuple in speeds)
			{
				if (tuple.Item1 < gameDayProgressInHours)
				{
					num = (int)tuple.Item2;
				}
			}
			int num2 = 0;
			Tuple<float, float>[] speeds2 = nextWeather.Clouds[i].Speeds;
			foreach (Tuple<float, float> tuple2 in speeds2)
			{
				if (tuple2.Item1 < gameDayProgressInHours)
				{
					num2 = (int)tuple2.Item2;
				}
			}
			_transitionClouds = _transitionClouds || num != num2;
			if (_transitionClouds)
			{
				break;
			}
		}
		_transitionMoon = false;
		if (clientWeather.Moons.TryGetValue(_moonPhase, out var value) && nextWeather.Moons.TryGetValue(_moonPhase, out var value2))
		{
			_transitionMoon = value != value2;
		}
		_transitionStars = clientWeather.Stars != nextWeather.Stars;
		_transitionScreenEffect = clientWeather.ScreenEffect != nextWeather.ScreenEffect;
		SetWeatherParticles(null);
	}

	public void SetServerWeather(int weatherIndex)
	{
		_serverWeatherIndex = weatherIndex;
		UpdateEnvironmentWeather(fromForecast: true);
	}

	public void SetEditorWeatherOverride(int weatherIndex)
	{
		_editorWeatherOverrideIndex = weatherIndex;
		ClientWeather currentWeather = CurrentWeather;
		RequestTextureUpdateFromWeather(currentWeather);
		SetWeatherParticles(currentWeather.Particle);
	}

	private void SetWeatherParticles(WeatherParticle particle)
	{
		if (_particleSystemProxy != null)
		{
			_particleSystemProxy.Expire();
			_particleSystemProxy = null;
		}
		if (particle == null || particle.SystemId == null)
		{
			return;
		}
		if (_gameInstance.ParticleSystemStoreModule.TrySpawnSystem(particle.SystemId, out _particleSystemProxy, isLocalPlayer: false, isTracked: true))
		{
			if (particle.Color_ != null)
			{
				_particleSystemProxy.DefaultColor = UInt32Color.FromRGBA((byte)particle.Color_.Red, (byte)particle.Color_.Green, (byte)particle.Color_.Blue, byte.MaxValue);
			}
			_particleSystemProxy.Scale = ((particle.Scale != 0f) ? particle.Scale : 1f);
			_particleSystemProxy.IsOvergroundOnly = particle.IsOvergroundOnly;
		}
		_particleSystemPositionOffsetMultiplier = particle.PositionOffsetMultiplier;
	}

	public void ResetParticleSystems()
	{
		SetWeatherParticles(CurrentWeather.Particle);
		_currentFluidParticleSystemId = null;
		_currentFluidEnvironmentParticleSystemId = null;
		_resetFluidEnvironmentParticleSystem = true;
	}

	public void OnFluidFXChanged()
	{
		FluidFXIndex = 0;
		_currentFluidParticleSystemId = null;
	}

	public void OnDaylightPortionChanged()
	{
		_daylightDuration = (float)(int)DaylightPortion * 24f * 0.01f;
		_halfDaylightDuration = _daylightDuration * 0.5f;
		_inverseAllDaylightDay = 1f / (_daylightDuration * 2f);
		_nightDuration = 24f - _daylightDuration;
		_halfNightDuration = _nightDuration * 0.5f;
		_inverseAllNightDay = 1f / (_nightDuration * 2f);
	}

	public void OnEnvironmentCollectionChanged()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		CurrentEnvironmentIndex = 0;
		_previousEnvironmentIndex = 0;
		_currentFluidEnvironmentParticleSystemId = null;
	}

	public void OnWeatherCollectionChanged()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		SetWeatherParticles(CurrentWeather.Particle);
		RequestTextureUpdateFromWeather(CurrentWeather, forceUpdate: true);
	}

	public void RequestTextureUpdateFromWeather(ClientWeather weather, bool forceUpdate = false)
	{
		RequestScreenEffectTextureUpdateFromWeather(weather, forceUpdate);
		RequestStarsTextureUpdateFromWeather(weather, forceUpdate);
		RequestMoonTextureUpdateFromWeather(weather, forceUpdate);
		RequestCloudsTextureUpdateFromWeather(weather, forceUpdate);
	}

	public void RequestScreenEffectTextureUpdateFromWeather(ClientWeather weather, bool forceUpdate = false)
	{
		string value = null;
		if (weather.ScreenEffect != null && !_gameInstance.HashesByServerAssetPath.TryGetValue(weather.ScreenEffect, out value))
		{
			_gameInstance.App.DevTools.Error("Missing weather screen effect asset " + weather.ScreenEffect + " for weather " + weather.Id);
		}
		_gameInstance.ScreenEffectStoreModule.WeatherScreenEffectRenderer.RequestTextureUpdate(value, forceUpdate);
	}

	public void RequestStarsTextureUpdateFromWeather(ClientWeather weather, bool forceUpdate = false)
	{
		string value = null;
		if (weather.Stars != null && !_gameInstance.HashesByServerAssetPath.TryGetValue(weather.Stars, out value))
		{
			_gameInstance.App.DevTools.Error("Missing stars asset " + weather.Stars + " for weather " + weather.Id);
		}
		SkyRenderer.RequestStarsTextureUpdate(value, forceUpdate);
	}

	public void RequestMoonTextureUpdateFromWeather(ClientWeather weather, bool forceUpdate = false)
	{
		string value = null;
		if (weather.Moons.TryGetValue(_moonPhase, out var value2) && !_gameInstance.HashesByServerAssetPath.TryGetValue(value2, out value))
		{
			_gameInstance.App.DevTools.Error("Missing weather moon asset " + value2 + " for weather " + weather.Id);
		}
		SkyRenderer.RequestMoonTextureUpdate(value, forceUpdate);
	}

	public void RequestCloudsTextureUpdateFromWeather(ClientWeather weather, bool forceUpdate = false)
	{
		string[] array = new string[4];
		for (int i = 0; i < 4; i++)
		{
			if (weather.Clouds[i].Texture != null && !_gameInstance.HashesByServerAssetPath.TryGetValue(weather.Clouds[i].Texture, out array[i]))
			{
				_gameInstance.App.DevTools.Error("Missing weather cloud asset " + weather.Clouds[i].Texture + " for weather " + weather.Id);
			}
		}
		SkyRenderer.RequestCloudsTextureUpdate(array, forceUpdate);
	}
}
