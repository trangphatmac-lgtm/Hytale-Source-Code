#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Audio;
using HytaleClient.Core;
using HytaleClient.Data.Audio;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Wwise;

namespace HytaleClient.InGame.Modules.Audio;

internal class AudioModule : Module
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const float AudioTimeStep = 1f / 30f;

	private float _accumulatedDeltaTime;

	public uint CurrentEffectSoundEventIndex = 0u;

	private int _currentEffectPlaybackId = -1;

	private AudioDevice _audio;

	private int _currentFrameId = 0;

	public AudioModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_audio = _gameInstance.Engine.Audio;
		_audio.RefreshBanks();
	}

	protected override void DoDispose()
	{
	}

	public void PrepareSoundBanks(out Dictionary<string, WwiseResource> upcomingWwiseIds)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		upcomingWwiseIds = new Dictionary<string, WwiseResource>();
		if (_gameInstance.HashesByServerAssetPath.TryGetValue("SoundBanks/Wwise_IDs.h", out var value))
		{
			try
			{
				WwiseHeaderParser.Parse(AssetManager.GetAssetLocalPathUsingHash(value), out upcomingWwiseIds);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to load wwise header file.");
			}
		}
	}

	public void SetupSoundBanks(Dictionary<string, WwiseResource> upcomingWwiseIds)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_audio.ResourceManager.SetupWwiseIds(upcomingWwiseIds);
		if (_gameInstance.HashesByServerAssetPath.TryGetValue("SoundBanks/Windows/Init.bnk", out var value))
		{
			_audio.ResourceManager.FilePathsByFileName["Init.bnk"] = AssetManager.GetAssetLocalPathUsingHash(value);
		}
		if (_gameInstance.HashesByServerAssetPath.TryGetValue("SoundBanks/Windows/Master.bnk", out var value2))
		{
			_audio.ResourceManager.FilePathsByFileName["Master.bnk"] = AssetManager.GetAssetLocalPathUsingHash(value2);
		}
		if (_gameInstance.HashesByServerAssetPath.TryGetValue("SoundBanks/Windows/Music.bnk", out var value3))
		{
			_audio.ResourceManager.FilePathsByFileName["Music.bnk"] = AssetManager.GetAssetLocalPathUsingHash(value3);
		}
		foreach (KeyValuePair<string, string> item in _gameInstance.HashesByServerAssetPath)
		{
			if (item.Key.StartsWith("SoundBanks/Windows/") && item.Key.EndsWith(".wem"))
			{
				string key = item.Key.Substring("SoundBanks/Windows/".Length);
				_audio.ResourceManager.FilePathsByFileName[key] = AssetManager.GetAssetLocalPathUsingHash(item.Value);
			}
		}
		_audio.RefreshBanks();
	}

	public bool TryRegisterSoundObject(Vector3 position, Vector3 orientation, ref AudioDevice.SoundObjectReference soundObjectReference, bool hasUniqueEvent = false)
	{
		return _audio.TryRegisterSoundObject(position, orientation, ref soundObjectReference, hasSingleEvent: false, hasUniqueEvent);
	}

	public void UnregisterSoundObject(ref AudioDevice.SoundObjectReference soundObjectReference)
	{
		_audio.UnregisterSoundObject(ref soundObjectReference);
	}

	public bool TryPlayLocalBlockSoundEvent(int blockSoundSetIndex, BlockSoundEvent blockSoundEvent, ref int playbackId)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (blockSoundSetIndex >= _gameInstance.ServerSettings.BlockSoundSets.Length || blockSoundSetIndex < 0)
		{
			return false;
		}
		if (_gameInstance.ServerSettings.BlockSoundSets[blockSoundSetIndex].SoundEventIndices.TryGetValue(blockSoundEvent, out var value))
		{
			uint networkWwiseId = ResourceManager.GetNetworkWwiseId(value);
			if (networkWwiseId != 0)
			{
				if (playbackId != -1)
				{
					_gameInstance.AudioModule.ActionOnEvent(playbackId, (AkActionOnEventType)3);
				}
				playbackId = _gameInstance.AudioModule.PlayLocalSoundEvent(networkWwiseId);
				return true;
			}
		}
		return false;
	}

	public int PlayLocalSoundEvent(string soundEventId)
	{
		if (_audio.ResourceManager.WwiseEventIds.TryGetValue(soundEventId, out var value))
		{
			return PlayLocalSoundEvent(value);
		}
		Logger.Warn("Could not load sound: {0}", soundEventId);
		return -1;
	}

	public void PlaySoundEvent(string soundEventId, Vector3 position, Vector3 orientation)
	{
		if (_audio.ResourceManager.WwiseEventIds.TryGetValue(soundEventId, out var value))
		{
			PlaySoundEvent(value, position, orientation);
		}
		else
		{
			Logger.Warn("Could not load sound: {0}", soundEventId);
		}
	}

	public bool TryPlayBlockSoundEvent(int blockSoundSetIndex, BlockSoundEvent blockSoundEvent, Vector3 worldPosition, Vector3 orientation)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (blockSoundSetIndex >= _gameInstance.ServerSettings.BlockSoundSets.Length || blockSoundSetIndex < 0)
		{
			return false;
		}
		if (_gameInstance.ServerSettings.BlockSoundSets[blockSoundSetIndex].SoundEventIndices.TryGetValue(blockSoundEvent, out var value))
		{
			uint networkWwiseId = ResourceManager.GetNetworkWwiseId(value);
			if (networkWwiseId != 0)
			{
				_gameInstance.AudioModule.PlaySoundEvent(networkWwiseId, worldPosition, orientation);
				return true;
			}
		}
		return false;
	}

	public int PlayLocalSoundEvent(uint soundEventIndex)
	{
		if (soundEventIndex == 0)
		{
			return -1;
		}
		return _audio.PostEvent(soundEventIndex, AudioDevice.PlayerSoundObjectReference);
	}

	public void PlaySoundEvent(uint soundEventIndex, Vector3 position, Vector3 orientation)
	{
		if (soundEventIndex != 0)
		{
			AudioDevice.SoundObjectReference soundObjectReference = default(AudioDevice.SoundObjectReference);
			if (_audio.TryRegisterSoundObject(position, orientation, ref soundObjectReference, hasSingleEvent: true))
			{
				_audio.PostEvent(soundEventIndex, soundObjectReference);
			}
		}
	}

	public void PlaySoundEvent(uint soundEventIndex, Vector3 position, Vector3 orientation, ref AudioDevice.SoundEventReference soundEventReference)
	{
		if (soundEventIndex != 0)
		{
			AudioDevice.SoundObjectReference soundObjectReference = default(AudioDevice.SoundObjectReference);
			if (_audio.TryRegisterSoundObject(position, orientation, ref soundObjectReference, hasSingleEvent: true))
			{
				soundEventReference.PlaybackId = _audio.PostEvent(soundEventIndex, soundObjectReference);
			}
		}
	}

	public void PlaySoundEvent(uint soundEventIndex, AudioDevice.SoundObjectReference soundObjectId, ref AudioDevice.SoundEventReference soundEventReference)
	{
		if (soundEventIndex != 0)
		{
			int playbackId = _audio.PostEvent(soundEventIndex, soundObjectId);
			soundEventReference.PlaybackId = playbackId;
		}
	}

	public void ActionOnEvent(ref AudioDevice.SoundEventReference soundEventReference, AkActionOnEventType actionType)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		Debug.Assert(soundEventReference.PlaybackId != -1);
		_audio.ActionOnEvent(soundEventReference.PlaybackId, actionType, 0, (AkCurveInterpolation)4);
		soundEventReference.PlaybackId = -1;
	}

	public void ActionOnEvent(int playbackId, AkActionOnEventType actionType)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		Debug.Assert(playbackId != -1);
		_audio.ActionOnEvent(playbackId, actionType, 0, (AkCurveInterpolation)4);
	}

	public void OnSoundEffectCollectionChanged()
	{
		CurrentEffectSoundEventIndex = 0u;
	}

	public void SetWorldSoundEffect(uint effectIndex)
	{
		if (effectIndex != CurrentEffectSoundEventIndex)
		{
			CurrentEffectSoundEventIndex = effectIndex;
			if (_currentEffectPlaybackId != -1)
			{
				_audio.ActionOnEvent(_currentEffectPlaybackId, (AkActionOnEventType)0, 0, (AkCurveInterpolation)4);
			}
			if (CurrentEffectSoundEventIndex == 0)
			{
				_currentEffectPlaybackId = -1;
			}
			else
			{
				_currentEffectPlaybackId = PlayLocalSoundEvent(CurrentEffectSoundEventIndex);
			}
		}
	}

	public void Update(float deltaTime)
	{
		_accumulatedDeltaTime += deltaTime;
		if (_accumulatedDeltaTime < 1f / 30f)
		{
			return;
		}
		_accumulatedDeltaTime = 0f;
		ICameraController controller = _gameInstance.CameraModule.Controller;
		Vector3 position = controller.Position;
		_audio.SetListenerPosition(position, controller.Rotation);
		EntityStoreModule entityStoreModule = _gameInstance.EntityStoreModule;
		AudioDevice.SoundObjectReference[] soundObjectReferences = entityStoreModule.GetSoundObjectReferences();
		BoundingSphere[] boundingVolumes = entityStoreModule.GetBoundingVolumes();
		Vector3[] orientations = entityStoreModule.GetOrientations();
		int entitiesCount = entityStoreModule.GetEntitiesCount();
		bool flag = _currentFrameId % 2 == 0;
		bool flag2 = !flag && _currentFrameId % 4 - 1 == 0;
		bool flag3 = !flag && !flag2 && _currentFrameId % 8 - 3 == 0;
		for (int i = entityStoreModule.PlayerEntityLocalId + 1; i < entitiesCount; i++)
		{
			Vector3 center = boundingVolumes[i].Center;
			float num = Vector3.DistanceSquared(position, center);
			bool flag4 = false;
			if (num < 2304f || ((num < 9408f) ? flag : ((!(num < 25600f)) ? flag3 : flag2)))
			{
				_audio.SetPosition(soundObjectReferences[i], center, orientations[i]);
			}
		}
		_currentFrameId++;
	}
}
