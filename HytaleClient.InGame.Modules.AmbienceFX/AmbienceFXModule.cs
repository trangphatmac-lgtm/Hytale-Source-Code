#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Data.AmbienceFX;
using HytaleClient.Data.Map;
using HytaleClient.Data.Weather;
using HytaleClient.InGame.Commands;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using Wwise;

namespace HytaleClient.InGame.Modules.AmbienceFX;

internal class AmbienceFXModule : Module
{
	private struct AmbientBedTask
	{
		public uint AmbientBedSoundEventIndex;

		public int AmbienceFXIndex;
	}

	private readonly Dictionary<int, int> _ambientBedPlaybacksByAmbienceFXIndices = new Dictionary<int, int>();

	private const float AnalyzeEnvironmentDelay = 60f;

	private const float AnalyzeEnvironmentSquareDistance = 9f;

	private const int ZoneRadius = 8;

	private const int ZoneRadiusHeight = 6;

	private const int ZoneThreshold = 3;

	private const int BlocksToAnalyze = 3072;

	public const int EmptyAmbienceId = 0;

	private readonly Dictionary<string, int> _ambienceFXIndicesByIds = new Dictionary<string, int>();

	private const int AllMusicAmbienceDefaultSize = 4;

	private const int AllMusicAmbienceGrow = 2;

	private const int AmbienceTasksDefaultSize = 25;

	private const int AmbienceTasksGrowth = 10;

	private readonly HashSet<int> _allActiveAmbienceFXIndices = new HashSet<int>();

	private int _soundTaskCount = 0;

	private AmbienceFXSoundSettings[] _soundTasks = new AmbienceFXSoundSettings[25];

	private int _ambientBedTaskCount = 0;

	private AmbientBedTask[] _ambientBedTasks = new AmbientBedTask[25];

	private int[] _allMusicAmbienceFXIndices = new int[4];

	private int[] _allSoundEffectAmbienceFXIndices = new int[4];

	private int _currentSoundEffectPlaybackId = -1;

	private readonly Random _random = new Random();

	private float _lastAnalyzeEnvironmentTime = 60f;

	private Vector3 _currentPlayerPosition;

	private Vector3 _lastPlayerPosition = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

	private float _accumulatedDeltaTime;

	private readonly BlockEnvironmentStats _environmentStats = new BlockEnvironmentStats(3072);

	private int _altitude;

	private bool _hasRoof;

	private bool _hasFloor;

	private int _wallsCount;

	private int _sunLightLevel;

	private int _torchLightLevel;

	private int _globalLightLevel;

	private const int MusicFadeDuration = 5000;

	private int _currentMusicPlaybackId;

	public AmbienceFXSettings[] AmbienceFXs { get; private set; }

	public int MusicAmbienceFXIndex { get; private set; } = 0;


	public uint CurrentSoundEffectSoundEventIndex { get; private set; }

	public uint CurrentMusicSoundEventIndex { get; private set; }

	private void StopAmbientBeds()
	{
		foreach (int value in _ambientBedPlaybacksByAmbienceFXIndices.Values)
		{
			_gameInstance.AudioModule.ActionOnEvent(value, (AkActionOnEventType)0);
		}
		_ambientBedPlaybacksByAmbienceFXIndices.Clear();
	}

	private void PlayAmbientBeds()
	{
		for (int i = 0; i < _ambientBedTaskCount; i++)
		{
			ref AmbientBedTask reference = ref _ambientBedTasks[i];
			int value = _gameInstance.AudioModule.PlayLocalSoundEvent(reference.AmbientBedSoundEventIndex);
			_ambientBedPlaybacksByAmbienceFXIndices[reference.AmbienceFXIndex] = value;
		}
		List<int> list = new List<int>();
		foreach (int key2 in _ambientBedPlaybacksByAmbienceFXIndices.Keys)
		{
			if (!_allActiveAmbienceFXIndices.Contains(key2))
			{
				list.Add(key2);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			int key = list[j];
			_gameInstance.AudioModule.ActionOnEvent(_ambientBedPlaybacksByAmbienceFXIndices[key], (AkActionOnEventType)0);
			_ambientBedPlaybacksByAmbienceFXIndices.Remove(key);
		}
	}

	public AmbienceFXModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		SetupAmbienceDebug();
	}

	protected override void DoDispose()
	{
		StopCurrentMusic();
		StopAmbientBeds();
	}

	public void Update(float deltaTime)
	{
		_accumulatedDeltaTime += deltaTime;
		if (!(_accumulatedDeltaTime < 1f / 60f))
		{
			_currentPlayerPosition = _gameInstance.LocalPlayer.Position;
			_lastAnalyzeEnvironmentTime += _accumulatedDeltaTime;
			if (_lastAnalyzeEnvironmentTime > 60f || Vector3.DistanceSquared(_currentPlayerPosition, _lastPlayerPosition) > 9f)
			{
				_lastAnalyzeEnvironmentTime = 0f;
				_lastPlayerPosition = _currentPlayerPosition;
				AnalyzeEnvironment();
			}
			UpdateActiveAmbienceFXs();
			Play3DSounds();
			_accumulatedDeltaTime = 0f;
		}
	}

	public void PrepareAmbienceFXs(AmbienceFX[] networkAmbienceFXs, out AmbienceFXSettings[] upcomingAmbienceFXs)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		upcomingAmbienceFXs = new AmbienceFXSettings[networkAmbienceFXs.Length];
		List<AmbienceFXSoundSettings> validSoundSettings = new List<AmbienceFXSoundSettings>();
		for (int i = 0; i < networkAmbienceFXs.Length; i++)
		{
			AmbienceFXSettings clientAmbienceFX = new AmbienceFXSettings();
			AmbienceFX networkAmbienceFX = networkAmbienceFXs[i];
			AmbienceFXProtocolInitializer.Initialize(networkAmbienceFX, ref clientAmbienceFX, validSoundSettings);
			upcomingAmbienceFXs[i] = clientAmbienceFX;
		}
	}

	public void SetupAmbienceFXs(AmbienceFX[] _networkAmbienceFXs, AmbienceFXSettings[] upcomingSettings)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		AmbienceFXs = upcomingSettings;
		for (int i = 0; i < AmbienceFXs.Length; i++)
		{
			_ambienceFXIndicesByIds[AmbienceFXs[i].Id] = i;
		}
	}

	public void OnAmbienceFXChanged()
	{
		MusicAmbienceFXIndex = 0;
		StopCurrentMusic();
		StopAmbientBeds();
	}

	private void AnalyzeEnvironment()
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Invalid comparison between Unknown and I4
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Invalid comparison between Unknown and I4
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Invalid comparison between Unknown and I4
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Invalid comparison between Unknown and I4
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Invalid comparison between Unknown and I4
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Invalid comparison between Unknown and I4
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Invalid comparison between Unknown and I4
		int num = (int)System.Math.Floor(_currentPlayerPosition.X);
		int num2 = (int)System.Math.Floor(_currentPlayerPosition.Y);
		int num3 = (int)System.Math.Floor(_currentPlayerPosition.Z);
		_altitude = num2;
		_hasRoof = false;
		for (int i = num2 + 2; i < num2 + 32; i++)
		{
			int block = _gameInstance.MapModule.GetBlock(num, i, num3, 0);
			ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
			if ((int)clientBlockType.CollisionMaterial == 1 && (int)clientBlockType.Opacity <= 0)
			{
				_hasRoof = true;
				break;
			}
		}
		_hasFloor = false;
		for (int num4 = num2 - 1; num4 > num2 - 4; num4--)
		{
			int block2 = _gameInstance.MapModule.GetBlock(num, num4, num3, 0);
			if ((int)_gameInstance.MapModule.ClientBlockTypes[block2].CollisionMaterial == 1)
			{
				_hasFloor = true;
				break;
			}
		}
		int num5 = 0;
		for (int j = num + 1; j < num + 24; j++)
		{
			int block3 = _gameInstance.MapModule.GetBlock(j, num2 + 2, num3, 0);
			if ((int)_gameInstance.MapModule.ClientBlockTypes[block3].CollisionMaterial == 1)
			{
				num5++;
				break;
			}
		}
		for (int num6 = num - 1; num6 > num - 24; num6--)
		{
			int block4 = _gameInstance.MapModule.GetBlock(num6, num2 + 2, num3, 0);
			if ((int)_gameInstance.MapModule.ClientBlockTypes[block4].CollisionMaterial == 1)
			{
				num5++;
				break;
			}
		}
		for (int k = num3 + 1; k < num3 + 24; k++)
		{
			int block5 = _gameInstance.MapModule.GetBlock(num, num2 + 2, k, 0);
			if ((int)_gameInstance.MapModule.ClientBlockTypes[block5].CollisionMaterial == 1)
			{
				num5++;
				break;
			}
		}
		for (int num7 = num3 - 1; num7 > num3 - 24; num7--)
		{
			int block6 = _gameInstance.MapModule.GetBlock(num, num2 + 2, num7, 0);
			if ((int)_gameInstance.MapModule.ClientBlockTypes[block6].CollisionMaterial == 1)
			{
				num5++;
				break;
			}
		}
		_wallsCount = num5;
		int worldChunkX = num >> 5;
		int y = num2 >> 5;
		int worldChunkZ = num3 >> 5;
		Chunk chunk = _gameInstance.MapModule.GetChunkColumn(worldChunkX, worldChunkZ)?.GetChunk(y);
		if (chunk?.Data.BorderedLightAmounts != null)
		{
			int indexInChunk = ChunkHelper.IndexOfWorldBlockInChunk(num, num2, num3);
			int num8 = ChunkHelper.IndexOfBlockInBorderedChunk(indexInChunk, 0, 0, 0);
			ushort num9 = chunk.Data.BorderedLightAmounts[num8];
			_sunLightLevel = (num9 >> 12) & 0xF;
			_torchLightLevel = ((num9 >> 8) & 0xF) | ((num9 >> 4) & 0xF) | (num9 & 0xF);
		}
		else
		{
			_sunLightLevel = 0;
			_torchLightLevel = 0;
		}
		_globalLightLevel = (int)MathHelper.Max(_sunLightLevel, _torchLightLevel);
		_environmentStats.TotalStats = 0;
		for (int l = 0; l < 16; l++)
		{
			for (int m = 0; m < 16; m++)
			{
				for (int n = 0; n < 12; n++)
				{
					int num10 = num + (l - 8) * 3 + _random.Next() % 3;
					int num11 = num2 + (n - 6) * 3 + _random.Next() % 3;
					int num12 = num3 + (m - 8) * 3 + _random.Next() % 3;
					int block7 = _gameInstance.MapModule.GetBlock(num10, num11, num12, 0);
					AddToStats(_gameInstance.MapModule.ClientBlockTypes[block7].BlockSoundSetIndex, num10, num11, num12);
				}
			}
		}
	}

	private void UpdateActiveAmbienceFXs()
	{
		int musicAmbienceFXIndex = MusicAmbienceFXIndex;
		MusicAmbienceFXIndex = 0;
		int num = 0;
		uint currentSoundEffectSoundEventIndex = CurrentSoundEffectSoundEventIndex;
		CurrentSoundEffectSoundEventIndex = 0u;
		int num2 = 0;
		_ambientBedTaskCount = 0;
		_soundTaskCount = 0;
		_allActiveAmbienceFXIndices.Clear();
		int currentEnvironmentIndex = _gameInstance.WeatherModule.CurrentEnvironmentIndex;
		int weatherIndex = ((_gameInstance.WeatherModule.IsChangingWeather && _gameInstance.WeatherModule.NextWeatherProgress > 0.75f) ? _gameInstance.WeatherModule.NextWeatherIndex : _gameInstance.WeatherModule.CurrentWeatherIndex);
		int fluidFXIndex = _gameInstance.WeatherModule.FluidFXIndex;
		for (int i = 0; i < AmbienceFXs.Length; i++)
		{
			AmbienceFXSettings ambienceFXSettings = AmbienceFXs[i];
			if (ambienceFXSettings.Conditions != null && !IsValidAmbience(ambienceFXSettings.Conditions, currentEnvironmentIndex, weatherIndex, fluidFXIndex, out var _))
			{
				continue;
			}
			if (ambienceFXSettings.AmbientBedSoundEventIndex != 0 && !_ambientBedPlaybacksByAmbienceFXIndices.ContainsKey(i))
			{
				ArrayUtils.GrowArrayIfNecessary(ref _ambientBedTasks, _ambientBedTaskCount, 10);
				_ambientBedTasks[_ambientBedTaskCount].AmbientBedSoundEventIndex = ambienceFXSettings.AmbientBedSoundEventIndex;
				_ambientBedTasks[_ambientBedTaskCount].AmbienceFXIndex = i;
				_ambientBedTaskCount++;
			}
			if (ambienceFXSettings.Sounds != null && ambienceFXSettings.Sounds.Length != 0)
			{
				ArrayUtils.GrowArrayIfNecessary(ref _soundTasks, _soundTaskCount, 10);
				for (int j = 0; j < ambienceFXSettings.Sounds.Length; j++)
				{
					_soundTasks[_soundTaskCount] = ambienceFXSettings.Sounds[j];
					_soundTaskCount++;
				}
			}
			if (ambienceFXSettings.MusicSoundEventIndex != 0)
			{
				if (MusicAmbienceFXIndex == 0)
				{
					MusicAmbienceFXIndex = i;
				}
				if (num >= _allMusicAmbienceFXIndices.Length)
				{
					Array.Resize(ref _allMusicAmbienceFXIndices, num + 2);
				}
				_allMusicAmbienceFXIndices[num] = i;
				num++;
			}
			if (ambienceFXSettings.EffectSoundEventIndex != 0)
			{
				if (CurrentSoundEffectSoundEventIndex == 0)
				{
					CurrentSoundEffectSoundEventIndex = ambienceFXSettings.EffectSoundEventIndex;
				}
				_allSoundEffectAmbienceFXIndices[num2] = i;
				num2++;
			}
			_allActiveAmbienceFXIndices.Add(i);
		}
		if (musicAmbienceFXIndex != MusicAmbienceFXIndex)
		{
			if (MusicAmbienceFXIndex == 0)
			{
				_gameInstance.App.DevTools.Error("No music ambienceFX found!");
			}
			else if (num > 1)
			{
				string[] array = new string[num];
				for (int k = 0; k < num; k++)
				{
					array[k] = AmbienceFXs[_allMusicAmbienceFXIndices[k]].Id;
				}
				_gameInstance.App.DevTools.Error("Trying to set several music ambienceFX at the same time: " + string.Join(", ", array) + ".");
			}
			CurrentMusicSoundEventIndex = AmbienceFXs[MusicAmbienceFXIndex].MusicSoundEventIndex;
			if (CurrentMusicSoundEventIndex != AmbienceFXs[musicAmbienceFXIndex].MusicSoundEventIndex)
			{
				StopCurrentMusic();
				_currentMusicPlaybackId = _gameInstance.AudioModule.PlayLocalSoundEvent(CurrentMusicSoundEventIndex);
			}
		}
		if (currentSoundEffectSoundEventIndex != CurrentSoundEffectSoundEventIndex)
		{
			_gameInstance.AudioModule.SetWorldSoundEffect(CurrentSoundEffectSoundEventIndex);
			if (num2 > 1)
			{
				string[] array2 = new string[num2];
				for (int l = 0; l < num2; l++)
				{
					array2[l] = AmbienceFXs[_allSoundEffectAmbienceFXIndices[l]].Id;
				}
				_gameInstance.App.DevTools.Error("Trying to set several sound effects at the same time: " + string.Join(", ", array2) + ".");
			}
		}
		PlayAmbientBeds();
	}

	private void Play3DSounds()
	{
		for (int i = 0; i < _soundTaskCount; i++)
		{
			AmbienceFXSoundSettings ambienceFXSoundSettings = _soundTasks[i];
			if (ambienceFXSoundSettings.LastTime == 0f)
			{
				ambienceFXSoundSettings.NextTime = (float)_random.Next() % (ambienceFXSoundSettings.Frequency.Min + 1f);
				ambienceFXSoundSettings.LastTime = 1f;
				continue;
			}
			ambienceFXSoundSettings.NextTime -= 1f / 60f;
			if (!(ambienceFXSoundSettings.NextTime > 0f))
			{
				ambienceFXSoundSettings.NextTime = _random.NextFloat(ambienceFXSoundSettings.Frequency.Min, ambienceFXSoundSettings.Frequency.Max);
				if (ambienceFXSoundSettings.Play3D != AmbienceFXSoundSettings.AmbienceFXSoundPlay3D.No)
				{
					_gameInstance.AudioModule.PlaySoundEvent(ambienceFXSoundSettings.SoundEventIndex, GetSoundPosition(ambienceFXSoundSettings), Vector3.Zero);
					continue;
				}
				int playbackId = _gameInstance.AudioModule.PlayLocalSoundEvent(ambienceFXSoundSettings.SoundEventIndex);
				_gameInstance.AudioModule.ActionOnEvent(playbackId, (AkActionOnEventType)3);
			}
		}
	}

	private Vector3 GetSoundPosition(AmbienceFXSoundSettings sound)
	{
		Vector3 result = _currentPlayerPosition;
		if (sound.BlockSoundSetIndex != 0)
		{
			for (int i = 0; i < _environmentStats.TotalStats; i++)
			{
				ref int reference = ref _environmentStats.BlockSoundSetIndices[i];
				if (sound.BlockSoundSetIndex != reference)
				{
					continue;
				}
				if (sound.Play3D == AmbienceFXSoundSettings.AmbienceFXSoundPlay3D.LocationName)
				{
					result = _environmentStats.GetClosestBlock(i, _currentPlayerPosition);
				}
				ref BlockEnvironmentStats.BlockStats reference2 = ref _environmentStats.Stats[i];
				switch (sound.Altitude)
				{
				case AmbienceFXSoundSettings.AmbienceFXAltitude.Highest:
					result.Y = reference2.HighestAltitude;
					break;
				case AmbienceFXSoundSettings.AmbienceFXAltitude.Lowest:
					result.Y = reference2.LowestAltitude;
					break;
				case AmbienceFXSoundSettings.AmbienceFXAltitude.Random:
					switch (_random.Next(3))
					{
					case 1:
						result.Y = reference2.HighestAltitude;
						break;
					case 2:
						result.Y = reference2.LowestAltitude;
						break;
					}
					break;
				}
			}
		}
		int num = sound.Radius.Max - sound.Radius.Min;
		if (num == 0)
		{
			return result;
		}
		return new Vector3(result.X + (float)((_random.Next() % num + sound.Radius.Min) * (_random.Next() % 2 * 2 - 1)), result.Y + ((float)(_random.Next() % num) * 0.5f + (float)sound.Radius.Min * 0.5f) * (float)(_random.Next() % 2 * 2 - 1), result.Z + (float)((_random.Next() % num + sound.Radius.Min) * (_random.Next() % 2 * 2 - 1)));
	}

	private bool IsValidAmbience(AmbienceFXConditionSettings conditions, int environmentIndex, int weatherIndex, int fluidFxIndex, out string debug, bool debugEnabled = false)
	{
		debug = null;
		if (_wallsCount < conditions.Walls.Min)
		{
			if (debugEnabled)
			{
				debug = $"Not enough walls: min={conditions.Walls.Min}";
			}
			return false;
		}
		if (_wallsCount > conditions.Walls.Max)
		{
			if (debugEnabled)
			{
				debug = $"Too many walls: max={conditions.Walls.Max}";
			}
			return false;
		}
		if (conditions.Roof && !_hasRoof)
		{
			if (debugEnabled)
			{
				debug = "Roof is wrong";
			}
			return false;
		}
		if (conditions.Floor && !_hasFloor)
		{
			if (debugEnabled)
			{
				debug = "Floor is wrong";
			}
			return false;
		}
		if (_altitude < conditions.Altitude.Min)
		{
			if (debugEnabled)
			{
				debug = $"Altitude too low: min={conditions.Altitude.Min}";
			}
			return false;
		}
		if (_altitude > conditions.Altitude.Max)
		{
			if (debugEnabled)
			{
				debug = $"Altitude too high: max={conditions.Altitude.Max}";
			}
			return false;
		}
		float gameDayProgressInHours = _gameInstance.TimeModule.GameDayProgressInHours;
		if (conditions.DayTime.Min < conditions.DayTime.Max)
		{
			if (gameDayProgressInHours < conditions.DayTime.Min || gameDayProgressInHours > conditions.DayTime.Max)
			{
				if (debugEnabled)
				{
					debug = $"Not in right time range: {conditions.DayTime.Min}-{conditions.DayTime.Max}";
				}
				return false;
			}
		}
		else if (gameDayProgressInHours < conditions.DayTime.Min && gameDayProgressInHours > conditions.DayTime.Max)
		{
			if (debugEnabled)
			{
				debug = $"Not in right time range: {conditions.DayTime.Min}/{conditions.DayTime.Max}";
			}
			return false;
		}
		if (_sunLightLevel < conditions.SunLightLevel.Min)
		{
			if (debugEnabled)
			{
				debug = $"Not enough sunlight: min={conditions.SunLightLevel.Min}";
			}
			return false;
		}
		if (_sunLightLevel > conditions.SunLightLevel.Max)
		{
			if (debugEnabled)
			{
				debug = $"Too much sunlight: max={conditions.SunLightLevel.Max}";
			}
			return false;
		}
		if (_torchLightLevel < conditions.TorchLightLevel.Min)
		{
			if (debugEnabled)
			{
				debug = $"Not enough torchlight: min={conditions.TorchLightLevel.Min}";
			}
			return false;
		}
		if (_torchLightLevel > conditions.TorchLightLevel.Max)
		{
			if (debugEnabled)
			{
				debug = $"Too much torchlight: max={conditions.TorchLightLevel.Max}";
			}
			return false;
		}
		if (_globalLightLevel < conditions.GlobalLightLevel.Min)
		{
			if (debugEnabled)
			{
				debug = $"Not enough global light: min={conditions.GlobalLightLevel.Min}";
			}
			return false;
		}
		if (_globalLightLevel > conditions.GlobalLightLevel.Max)
		{
			if (debugEnabled)
			{
				debug = $"Too much global light: max={conditions.GlobalLightLevel.Max}";
			}
			return false;
		}
		if (conditions.EnvironmentIndices != null)
		{
			bool flag = false;
			for (int i = 0; i < conditions.EnvironmentIndices.Length; i++)
			{
				if (conditions.EnvironmentIndices[i] == environmentIndex)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				if (debugEnabled)
				{
					string[] array = new string[conditions.EnvironmentIndices.Length];
					for (int j = 0; j < conditions.EnvironmentIndices.Length; j++)
					{
						array[j] = _gameInstance.ServerSettings.Environments[conditions.EnvironmentIndices[j]].Id;
					}
					debug = "Environment invalid: " + _gameInstance.WeatherModule.CurrentEnvironment.Id + " not in [ " + string.Join(", ", array) + " ]";
				}
				return false;
			}
		}
		if (conditions.WeatherIndices != null)
		{
			bool flag2 = false;
			for (int k = 0; k < conditions.WeatherIndices.Length; k++)
			{
				if (conditions.WeatherIndices[k] == weatherIndex)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				if (debugEnabled)
				{
					ClientWeather clientWeather = _gameInstance.ServerSettings.Weathers[weatherIndex];
					string[] array2 = new string[conditions.WeatherIndices.Length];
					for (int l = 0; l < conditions.WeatherIndices.Length; l++)
					{
						array2[l] = _gameInstance.ServerSettings.Weathers[conditions.WeatherIndices[l]].Id;
					}
					debug = "Weather invalid: " + clientWeather.Id + " not in [ " + string.Join(", ", array2) + " ]";
				}
				return false;
			}
		}
		if (conditions.FluidFXIndices != null)
		{
			bool flag3 = conditions.FluidFXIndices.Length == 0 && fluidFxIndex == 0;
			for (int m = 0; m < conditions.FluidFXIndices.Length; m++)
			{
				if (conditions.FluidFXIndices[m] == fluidFxIndex)
				{
					flag3 = true;
					break;
				}
			}
			if (!flag3)
			{
				if (debugEnabled)
				{
					debug = "FluidFX invalid: " + _gameInstance.WeatherModule.FluidFX.Id;
				}
				return false;
			}
		}
		if (conditions.SurroundingBlockSoundSets != null)
		{
			for (int n = 0; n < conditions.SurroundingBlockSoundSets.Length; n++)
			{
				AmbienceFXConditionSettings.AmbienceFXBlockSoundSet ambienceFXBlockSoundSet = conditions.SurroundingBlockSoundSets[n];
				float min = ambienceFXBlockSoundSet.Percent.Min;
				float max = ambienceFXBlockSoundSet.Percent.Max;
				if (!IsActive(ambienceFXBlockSoundSet.BlockSoundSetIndex, min, max))
				{
					if (debugEnabled)
					{
						BlockSoundSet arg = _gameInstance.ServerSettings.BlockSoundSets[ambienceFXBlockSoundSet.BlockSoundSetIndex];
						debug = $"Required environment not meeting criteria: {arg} min={min}, max={max}";
					}
					return false;
				}
			}
		}
		return true;
	}

	public bool IsActive(int blockSoundSetIndex, float min, float max)
	{
		for (int i = 0; i < _environmentStats.TotalStats; i++)
		{
			if (_environmentStats.BlockSoundSetIndices[i] == blockSoundSetIndex)
			{
				ref BlockEnvironmentStats.BlockStats reference = ref _environmentStats.Stats[i];
				return reference.Percent >= min && reference.Percent <= max;
			}
		}
		return min == 0f;
	}

	private void AddToStats(int blockSoundSetIndex, int x, int y, int z)
	{
		for (int i = 0; i < _environmentStats.TotalStats; i++)
		{
			if (blockSoundSetIndex == _environmentStats.BlockSoundSetIndices[i])
			{
				_environmentStats.Add(i, x, y, z);
				return;
			}
		}
		_environmentStats.Initialize(blockSoundSetIndex, x, y, z);
	}

	private void SetupAmbienceDebug()
	{
		_gameInstance.RegisterCommand("getAmbience", GetAmbienceCommand);
		_gameInstance.RegisterCommand("getAmbienceEnv", GetAmbienceEnvCommand);
		_gameInstance.RegisterCommand("checkAmbience", CheckAmbienceCommand);
	}

	[Usage("getAmbience", new string[] { })]
	[Description("Dumps current ambience information")]
	private void GetAmbienceCommand(string[] args)
	{
		if (args.Length != 0)
		{
			throw new InvalidCommandUsage();
		}
		_gameInstance.Chat.Log("Music:");
		if (!_gameInstance.Engine.Audio.ResourceManager.DebugWwiseIds.TryGetValue(CurrentMusicSoundEventIndex, out var value))
		{
			value = "None";
		}
		_gameInstance.Chat.Log(AmbienceFXs[MusicAmbienceFXIndex].Id + " - " + value);
		_gameInstance.Chat.Log("Ambient Beds:");
		foreach (int key in _ambientBedPlaybacksByAmbienceFXIndices.Keys)
		{
			AmbienceFXSettings ambienceFXSettings = AmbienceFXs[key];
			if (!_gameInstance.Engine.Audio.ResourceManager.DebugWwiseIds.TryGetValue(ambienceFXSettings.AmbientBedSoundEventIndex, out var value2))
			{
				value2 = "Not found";
			}
			_gameInstance.Chat.Log(ambienceFXSettings.Id + " - " + value2);
		}
		List<string> list = new List<string>();
		foreach (int allActiveAmbienceFXIndex in _allActiveAmbienceFXIndices)
		{
			AmbienceFXSettings ambienceFXSettings2 = AmbienceFXs[allActiveAmbienceFXIndex];
			if (ambienceFXSettings2.Sounds != null)
			{
				list.Add(ambienceFXSettings2.Id);
			}
		}
		_gameInstance.Chat.Log("Sounds:");
		_gameInstance.Chat.Log(string.Join(", ", list));
	}

	[Usage("getAmbienceEnv", new string[] { })]
	[Description("Prints ambience environment data to chat")]
	private void GetAmbienceEnvCommand(string[] args)
	{
		if (args.Length != 0)
		{
			throw new InvalidCommandUsage();
		}
		_gameInstance.Chat.Log("Environment analysis:");
		_gameInstance.Chat.Log($"Altitude ={_altitude}, HasFloor={_hasFloor}, HasRoof={_hasRoof}, WallsCount={_wallsCount}, " + $"DayTime={_gameInstance.TimeModule.GameDayProgressInHours}, SunLight={_sunLightLevel}, TorchLight={_torchLightLevel}, GlobalLight={_globalLightLevel}");
		_gameInstance.Chat.Log($"{3072} blocks analyzed in {48}x{48}x{36} cuboid => {_environmentStats.TotalStats} block environment types found");
		for (int i = 0; i < _environmentStats.TotalStats; i++)
		{
			_gameInstance.Chat.Log(_environmentStats.GetDebugData(i, _gameInstance.ServerSettings.BlockSoundSets[_environmentStats.BlockSoundSetIndices[i]].Id));
		}
	}

	[Usage("checkAmbience", new string[] { "[ambienceName]" })]
	[Description("Dumps information about an ambience by name")]
	private void CheckAmbienceCommand(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0];
		if (!_ambienceFXIndicesByIds.TryGetValue(text, out var value))
		{
			_gameInstance.Chat.Log("Ambience \"" + text + "\" not found...");
			return;
		}
		AmbienceFXSettings ambienceFXSettings = AmbienceFXs[value];
		int currentEnvironmentIndex = _gameInstance.WeatherModule.CurrentEnvironmentIndex;
		int weatherIndex = ((_gameInstance.WeatherModule.IsChangingWeather && _gameInstance.WeatherModule.NextWeatherProgress > 0.75f) ? _gameInstance.WeatherModule.NextWeatherIndex : _gameInstance.WeatherModule.CurrentWeatherIndex);
		int fluidFXIndex = _gameInstance.WeatherModule.FluidFXIndex;
		if (ambienceFXSettings.Conditions != null && !IsValidAmbience(ambienceFXSettings.Conditions, currentEnvironmentIndex, weatherIndex, fluidFXIndex, out var debug, debugEnabled: true))
		{
			_gameInstance.Chat.Log("Ambience \"" + text + "\" can't be triggered because: " + debug);
			return;
		}
		_gameInstance.Chat.Log("Ambience \"" + text + "\" can be triggered!");
		if (ambienceFXSettings.MusicSoundEventIndex == 0)
		{
			_gameInstance.Chat.Log("Notice this ambience doesn't have any music to play.");
		}
		else
		{
			if (!_gameInstance.Engine.Audio.ResourceManager.DebugWwiseIds.TryGetValue(ambienceFXSettings.MusicSoundEventIndex, out var value2))
			{
				value2 = "Not found";
			}
			_gameInstance.Chat.Log("This ambience should play music : " + value2);
		}
		if (ambienceFXSettings.AmbientBedSoundEventIndex == 0)
		{
			_gameInstance.Chat.Log("Notice this ambience doesn't have any ambient bed to play.");
			return;
		}
		if (!_gameInstance.Engine.Audio.ResourceManager.DebugWwiseIds.TryGetValue(ambienceFXSettings.AmbientBedSoundEventIndex, out var value3))
		{
			value3 = "Not found";
		}
		_gameInstance.Chat.Log("This ambience should play ambient bed: " + value3);
	}

	private void StopCurrentMusic()
	{
		if (_currentMusicPlaybackId != -1)
		{
			_gameInstance.Engine.Audio.ActionOnEvent(_currentMusicPlaybackId, (AkActionOnEventType)0, 5000, (AkCurveInterpolation)0);
			_currentMusicPlaybackId = -1;
		}
	}
}
