#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using HytaleClient.Audio.Commands;
using HytaleClient.Core;
using HytaleClient.Math;
using HytaleClient.Utils;
using NLog;
using Wwise;

namespace HytaleClient.Audio;

internal class AudioDevice : Disposable
{
	private struct EventPlayback
	{
		public uint WwisePlaybackId;

		public uint EventId;

		public SoundObjectReference SoundObjectReference;
	}

	public struct OutputDevice
	{
		public uint Id;

		public string Name;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void WwiseErrorDelegate(int errorCode, IntPtr message, int errorLevel, int playingId, int gameObjectId);

	public struct SoundObjectReference
	{
		public int SlotId;

		public uint SoundObjectId;

		private static SoundObjectReference _empty = new SoundObjectReference(0u, -1);

		public static SoundObjectReference Empty => _empty;

		public SoundObjectReference(uint soundObjectSlotId, int slotId)
		{
			SoundObjectId = soundObjectSlotId;
			SlotId = slotId;
		}
	}

	public struct SoundEventReference
	{
		public SoundObjectReference SoundObjectReference;

		public int PlaybackId;

		private static SoundEventReference none = new SoundEventReference(SoundObjectReference.Empty, -1);

		public static SoundEventReference None => none;

		public SoundEventReference(SoundObjectReference soundObjectSlotId, int playbackId)
		{
			SoundObjectReference = soundObjectSlotId;
			PlaybackId = playbackId;
		}
	}

	private SoundObjectMemoryPool _soundObjectMemoryPool;

	private SoundObjectBuffers _soundObjectBuffers;

	private Dictionary<uint, uint> _eventReferenceCountByEventId = new Dictionary<uint, uint>();

	private Dictionary<uint, int> _playbackIdsByWwisePlaybackId = new Dictionary<uint, int>();

	private Dictionary<int, EventPlayback> _currentEventPlaybacksByPlaybackId = new Dictionary<int, EventPlayback>();

	private Dictionary<int, uint> _soundEventIdsByPlaybackIds = new Dictionary<int, uint>();

	public const string InitBank = "Init.bnk";

	public const string MasterBank = "Master.bnk";

	public const string UIBank = "UI.bnk";

	public const string MusicBank = "Music.bnk";

	private const int MaxLiveSoundObjects = 100;

	private const float LiveSoundObjectCullingSquaredDistance = 10000f;

	public const uint DefaultOutputDeviceId = 0u;

	public const int EmptySoundEventIndex = 0;

	public const int EmptySoundObjectId = 0;

	public const int PlayerSoundObjectId = 1;

	public const int NoSlotId = -1;

	public const int PlayerSoundObjectSlotId = 0;

	public static SoundObjectReference PlayerSoundObjectReference = new SoundObjectReference
	{
		SoundObjectId = 1u,
		SlotId = 0
	};

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const int NoPlaybackId = -1;

	private uint _currentOutputDeviceId = 0u;

	private int _outputDeviceCount = 0;

	private OutputDevice[] _outputDevices = new OutputDevice[2];

	private volatile bool _threadAlive = true;

	internal readonly AudioCategoryState[] AudioCategoryStates;

	public readonly ResourceManager ResourceManager;

	private uint _nextSoundObjectId = 2u;

	private int _soundObjectCount = 0;

	private const int SoundObjectDistanceDefaultSize = 1000;

	private const int SoundObjectDistanceGrowth = 500;

	private int[] _sortedSoundObjectSlotIds = new int[1000];

	private float[] _sortedSoundObjectSquaredDistanceToListener = new float[1000];

	private int _nextPlaybackId = 0;

	private readonly ConcurrentQueue<int> _commandIdQueue = new ConcurrentQueue<int>();

	private string _masterVolumeRTPCName;

	private float _masterVolume;

	private Vector3 _currentListenerPosition;

	private CommandMemoryPool _commandMemoryPool;

	private ConcurrentQueue<uint> _stoppedWwisePlaybackIds = new ConcurrentQueue<uint>();

	private EventCallbackFunc _defaultStopEventCallback;

	private static WwiseErrorDelegate ErrorDelegate;

	public int PlaybackCount => _currentEventPlaybacksByPlaybackId.Count;

	public int OutputDeviceCount => _outputDeviceCount;

	public float MasterVolume
	{
		get
		{
			Debug.Assert(ThreadHelper.IsOnThread(Thread));
			return _masterVolume;
		}
		set
		{
			value = MathHelper.Clamp(value, 0f, 100f);
			SetRTPC(_masterVolumeRTPCName, value);
		}
	}

	internal Thread Thread { get; private set; }

	public void RefreshBanks()
	{
		int num = _commandMemoryPool.TakeSlot();
		if (num >= 64)
		{
			_commandMemoryPool.Commands.Data[num].Type = CommandType.RefreshBanks;
			_commandIdQueue.Enqueue(num);
		}
	}

	private void ProcessRefreshBanks()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 1; i < _soundObjectBuffers.Count; i++)
		{
			uint num = _soundObjectBuffers.SoundObjectId[i];
			if (num != 0)
			{
				UnregisterSoundObject(num, i);
			}
		}
		UnloadBanks();
		SoundEngine.ClearCustomPaths();
		foreach (KeyValuePair<string, string> item in ResourceManager.FilePathsByFileName)
		{
			SoundEngine.RegisterCustomPath(item.Key, item.Value);
		}
		LoadBanks();
	}

	public bool TryRegisterSoundObject(Vector3 position, Vector3 orientation, ref SoundObjectReference soundObjectReference, bool hasSingleEvent = false, bool hasUniqueEvent = false)
	{
		soundObjectReference.SlotId = -1;
		soundObjectReference.SoundObjectId = 0u;
		if (hasSingleEvent && Vector3.DistanceSquared(position, _currentListenerPosition) >= 10000f)
		{
			return false;
		}
		int num = _commandMemoryPool.TakeSlot();
		int num2 = _soundObjectMemoryPool.TakeSlot();
		if (num < 64 || num2 < 0)
		{
			return false;
		}
		soundObjectReference.SlotId = num2;
		soundObjectReference.SoundObjectId = _nextSoundObjectId;
		ref CommandBuffers.CommandData reference = ref _commandMemoryPool.Commands.Data[num];
		reference.Type = CommandType.RegisterSoundObject;
		reference.SoundObjectReference = soundObjectReference;
		reference.WorldPosition = position;
		reference.WorldOrientation = orientation;
		byte bitfield = 0;
		if (hasSingleEvent)
		{
			BitUtils.SwitchOnBit(0, ref bitfield);
		}
		if (hasUniqueEvent)
		{
			BitUtils.SwitchOnBit(1, ref bitfield);
		}
		reference.BoolData = bitfield;
		_commandIdQueue.Enqueue(num);
		_nextSoundObjectId++;
		return true;
	}

	private void ProcessRegisterSoundObject(ref CommandBuffers.CommandData registerSoundObject)
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		SoundObjectReference soundObjectReference = registerSoundObject.SoundObjectReference;
		bool flag = Vector3.DistanceSquared(registerSoundObject.WorldPosition, _currentListenerPosition) < 10000f;
		byte bitfield = 0;
		if (BitUtils.IsBitOn(0, registerSoundObject.BoolData))
		{
			BitUtils.SwitchOnBit(0, ref bitfield);
		}
		if (BitUtils.IsBitOn(1, registerSoundObject.BoolData))
		{
			BitUtils.SwitchOnBit(1, ref bitfield);
		}
		GetWwiseOrientations(registerSoundObject.WorldOrientation, ref _soundObjectBuffers.FrontOrientation[soundObjectReference.SlotId], ref _soundObjectBuffers.TopOrientation[soundObjectReference.SlotId]);
		if (flag)
		{
			SoundEngine.RegisterGameObject((ulong)soundObjectReference.SoundObjectId);
			Vector3 vector = registerSoundObject.WorldPosition - _currentListenerPosition;
			Vector3 vector2 = _soundObjectBuffers.FrontOrientation[soundObjectReference.SlotId];
			Vector3 vector3 = _soundObjectBuffers.TopOrientation[soundObjectReference.SlotId];
			SoundEngine.SetPosition((ulong)soundObjectReference.SoundObjectId, vector.X, vector.Y, 0f - vector.Z, vector2.X, vector2.Y, vector2.Z, vector3.X, vector3.Y, vector3.Z);
			BitUtils.SwitchOnBit(2, ref bitfield);
		}
		_soundObjectBuffers.SoundObjectId[soundObjectReference.SlotId] = soundObjectReference.SoundObjectId;
		_soundObjectBuffers.Position[soundObjectReference.SlotId] = registerSoundObject.WorldPosition;
		_soundObjectBuffers.BoolData[soundObjectReference.SlotId] = bitfield;
		_soundObjectBuffers.LastPlaybackId[soundObjectReference.SlotId] = -1;
		_soundObjectCount++;
		ArrayUtils.GrowArrayIfNecessary(ref _sortedSoundObjectSlotIds, _soundObjectCount, 500);
		ArrayUtils.GrowArrayIfNecessary(ref _sortedSoundObjectSquaredDistanceToListener, _soundObjectCount, 500);
	}

	public void UnregisterSoundObject(ref SoundObjectReference soundObjectReference)
	{
		if (soundObjectReference.SlotId != -1)
		{
			int num = _commandMemoryPool.TakeSlot();
			if (num >= 64)
			{
				ref CommandBuffers.CommandData reference = ref _commandMemoryPool.Commands.Data[num];
				reference.Type = CommandType.UnregisterSoundObject;
				reference.SoundObjectReference = soundObjectReference;
				_commandIdQueue.Enqueue(num);
			}
			soundObjectReference = SoundObjectReference.Empty;
		}
	}

	private void ProcessUnregisterSoundObject(ref CommandBuffers.CommandData unRegisterSoundObject)
	{
		Debug.Assert(unRegisterSoundObject.SoundObjectReference.SoundObjectId != 1, "The player sound object should never get unregistered");
		uint num = _soundObjectBuffers.SoundObjectId[unRegisterSoundObject.SoundObjectReference.SlotId];
		if (unRegisterSoundObject.SoundObjectReference.SoundObjectId != num)
		{
			Logger.Warn("Trying to unregister unreferenced soundobject");
		}
		else
		{
			UnregisterSoundObject(unRegisterSoundObject.SoundObjectReference.SoundObjectId, unRegisterSoundObject.SoundObjectReference.SlotId);
		}
	}

	public void SetListenerPosition(Vector3 position, Vector3 orientation)
	{
		int num = _commandMemoryPool.TakeSlot();
		if (num >= 64)
		{
			ref CommandBuffers.CommandData reference = ref _commandMemoryPool.Commands.Data[num];
			reference.Type = CommandType.SetListenerPosition;
			reference.WorldPosition = position;
			reference.WorldOrientation = orientation;
			_commandIdQueue.Enqueue(num);
		}
	}

	private void ProcessSetListenerPosition(ref CommandBuffers.CommandData setListenerPosition)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		_currentListenerPosition = setListenerPosition.WorldPosition;
		Vector3 frontOrientation = Vector3.Zero;
		Vector3 topOrientation = Vector3.Zero;
		GetWwiseOrientations(setListenerPosition.WorldOrientation, ref frontOrientation, ref topOrientation);
		SoundEngine.SetPosition(1uL, 0f, 0f, 0f, frontOrientation.X, frontOrientation.Y, frontOrientation.Z, topOrientation.X, topOrientation.Y, topOrientation.Z);
	}

	public int PostEvent(uint eventId, SoundObjectReference soundObjectReference)
	{
		Debug.Assert(eventId != 0, "Expected valid sound event");
		if (soundObjectReference.SlotId == -1)
		{
			return -1;
		}
		int nextPlaybackId = _nextPlaybackId;
		int num = _commandMemoryPool.TakeSlot();
		if (num >= 64)
		{
			ref CommandBuffers.CommandData reference = ref _commandMemoryPool.Commands.Data[num];
			reference.Type = CommandType.PostEvent;
			reference.SoundObjectReference = soundObjectReference;
			reference.EventId = eventId;
			reference.PlaybackId = nextPlaybackId;
			_commandIdQueue.Enqueue(num);
		}
		_nextPlaybackId++;
		return nextPlaybackId;
	}

	private bool TryPrepareEvent(uint eventId)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		if (!_eventReferenceCountByEventId.TryGetValue(eventId, out var value))
		{
			AKRESULT val = SoundEngine.PrepareEvent((PreparationType)0, eventId);
			if ((int)val != 1)
			{
				if (!ResourceManager.DebugWwiseIds.TryGetValue(eventId, out var value2))
				{
					value2 = eventId.ToString();
				}
				Logger.Warn("Failed to load event: " + value2);
				return false;
			}
			value = 0u;
		}
		value++;
		_eventReferenceCountByEventId[eventId] = value;
		return true;
	}

	private void ProcessPostEvent(ref CommandBuffers.CommandData postEvent)
	{
		uint num = _soundObjectBuffers.SoundObjectId[postEvent.SoundObjectReference.SlotId];
		if (postEvent.SoundObjectReference.SoundObjectId != num)
		{
			Logger.Warn("Trying to post event on unreferenced soundobject");
			return;
		}
		byte bitfield = _soundObjectBuffers.BoolData[postEvent.SoundObjectReference.SlotId];
		if (BitUtils.IsBitOn(1, bitfield))
		{
			_soundObjectBuffers.LastPlaybackId[postEvent.SoundObjectReference.SlotId] = postEvent.PlaybackId;
			_soundEventIdsByPlaybackIds.Add(postEvent.PlaybackId, postEvent.EventId);
		}
		if (BitUtils.IsBitOn(2, bitfield))
		{
			if (!TryPrepareEvent(postEvent.EventId) && BitUtils.IsBitOn(0, _soundObjectBuffers.BoolData[postEvent.SoundObjectReference.SlotId]))
			{
				UnregisterSoundObject(postEvent.SoundObjectReference.SoundObjectId, postEvent.SoundObjectReference.SlotId);
			}
			PostEventToWwise(postEvent.PlaybackId, postEvent.EventId, postEvent.SoundObjectReference);
		}
	}

	private void PostEventToWwise(int playbackId, uint eventId, SoundObjectReference soundObjectReference)
	{
		uint num = SoundEngine.PostEvent(eventId, (ulong)soundObjectReference.SoundObjectId, (AkCallbackType)1, _defaultStopEventCallback);
		if (num != 0)
		{
			_playbackIdsByWwisePlaybackId[num] = playbackId;
			_currentEventPlaybacksByPlaybackId[playbackId] = new EventPlayback
			{
				WwisePlaybackId = num,
				EventId = eventId,
				SoundObjectReference = soundObjectReference
			};
		}
		else
		{
			if (!ResourceManager.DebugWwiseIds.TryGetValue(eventId, out var value))
			{
				value = eventId.ToString();
			}
			Logger.Warn("Failed to play event: " + value);
			RemoveEventReference(eventId);
		}
	}

	public void ActionOnEvent(int playbackId, AkActionOnEventType actionType, int transitionDuration = 0, AkCurveInterpolation fadeCurveType = 4)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if (playbackId != -1)
		{
			int num = _commandMemoryPool.TakeSlot();
			if (num >= 64)
			{
				ref CommandBuffers.CommandData reference = ref _commandMemoryPool.Commands.Data[num];
				reference.Type = CommandType.ActionOnEvent;
				reference.PlaybackId = playbackId;
				reference.ActionType = (byte)actionType;
				reference.TransitionDuration = transitionDuration;
				reference.FadeCurveType = (byte)fadeCurveType;
				_commandIdQueue.Enqueue(num);
			}
		}
	}

	private void ProcessActionOnEvent(ref CommandBuffers.CommandData actionOnEvent)
	{
		if (_currentEventPlaybacksByPlaybackId.TryGetValue(actionOnEvent.PlaybackId, out var value))
		{
			SoundEngine.ExecuteActionOnPlayingID((AkActionOnEventType)actionOnEvent.ActionType, value.WwisePlaybackId, actionOnEvent.TransitionDuration, (AkCurveInterpolation)actionOnEvent.FadeCurveType);
		}
	}

	public void SetPosition(SoundObjectReference soundObjectReference, Vector3 position, Vector3 frontOrientation)
	{
		if (soundObjectReference.SlotId != -1)
		{
			int num = _commandMemoryPool.TakeSlot();
			if (num >= 64)
			{
				ref CommandBuffers.CommandData reference = ref _commandMemoryPool.Commands.Data[num];
				reference.Type = CommandType.SetPosition;
				reference.SoundObjectReference = soundObjectReference;
				reference.WorldPosition = position;
				reference.WorldOrientation = frontOrientation;
				_commandIdQueue.Enqueue(num);
			}
		}
	}

	private void ProcessSetPosition(ref CommandBuffers.CommandData postEvent)
	{
		Debug.Assert(postEvent.SoundObjectReference.SoundObjectId != 1, "The player soundobject should always be at position 0 0 0");
		uint num = _soundObjectBuffers.SoundObjectId[postEvent.SoundObjectReference.SlotId];
		if (postEvent.SoundObjectReference.SoundObjectId != num)
		{
			Logger.Warn("Trying to set position on unreferenced soundobject");
			return;
		}
		_soundObjectBuffers.Position[postEvent.SoundObjectReference.SlotId] = postEvent.WorldPosition;
		GetWwiseOrientations(postEvent.WorldOrientation, ref _soundObjectBuffers.FrontOrientation[postEvent.SoundObjectReference.SlotId], ref _soundObjectBuffers.TopOrientation[postEvent.SoundObjectReference.SlotId]);
	}

	private void ProcessRTPC(ref CommandBuffers.CommandData rtpc)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		SoundEngine.SetRTPC((ulong)rtpc.RTPCId, rtpc.Volume);
	}

	private void RemoveEventReference(uint eventId)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		Debug.Assert(ThreadHelper.IsOnThread(Thread));
		if (_eventReferenceCountByEventId.TryGetValue(eventId, out var value))
		{
			value--;
			if (value == 0)
			{
				SoundEngine.PrepareEvent((PreparationType)1, eventId);
				_eventReferenceCountByEventId.Remove(eventId);
			}
			else
			{
				_eventReferenceCountByEventId[eventId] = value;
			}
		}
	}

	private void ProcessCommand(int commandId)
	{
		ref CommandBuffers.CommandData reference = ref _commandMemoryPool.Commands.Data[commandId];
		switch (reference.Type)
		{
		case CommandType.RefreshBanks:
			ProcessRefreshBanks();
			_commandMemoryPool.ReleaseSlot(commandId);
			break;
		case CommandType.RegisterSoundObject:
			ProcessRegisterSoundObject(ref reference);
			_commandMemoryPool.ReleaseSlot(commandId);
			break;
		case CommandType.UnregisterSoundObject:
			ProcessUnregisterSoundObject(ref reference);
			_commandMemoryPool.ReleaseSlot(commandId);
			break;
		case CommandType.SetListenerPosition:
			ProcessSetListenerPosition(ref reference);
			_commandMemoryPool.ReleaseSlot(commandId);
			break;
		case CommandType.PostEvent:
			ProcessPostEvent(ref reference);
			_commandMemoryPool.ReleaseSlot(commandId);
			break;
		case CommandType.ActionOnEvent:
			ProcessActionOnEvent(ref reference);
			_commandMemoryPool.ReleaseSlot(commandId);
			break;
		case CommandType.SetRTPC:
			ProcessRTPC(ref reference);
			_commandMemoryPool.ReleasePrioritySlot(commandId);
			break;
		case CommandType.SetPosition:
			ProcessSetPosition(ref reference);
			_commandMemoryPool.ReleaseSlot(commandId);
			break;
		default:
			throw new NotImplementedException();
		}
	}

	public AudioDevice(uint outputDeviceId, string masterVolumeRTPCName, float masterVolume, string[] categoryVolumeRTPCs, float[] categoryVolumes, int soundGroupCount)
	{
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Expected O, but got Unknown
		ErrorDelegate = WwiseErrorCallback;
		SoundEngine.SetLocalOutput(Marshal.GetFunctionPointerForDelegate(ErrorDelegate));
		_currentOutputDeviceId = outputDeviceId;
		_masterVolumeRTPCName = masterVolumeRTPCName;
		_masterVolume = masterVolume;
		AudioCategoryStates = new AudioCategoryState[categoryVolumes.Length];
		for (int i = 0; i < AudioCategoryStates.Length; i++)
		{
			AudioCategoryStates[i] = new AudioCategoryState(i, categoryVolumeRTPCs[i], categoryVolumes[i]);
		}
		SoundEngine.Init();
		SoundEngine.SetBasePath(Path.Combine(Paths.BuiltInAssets, "Common/SoundBanks/Windows"));
		_commandMemoryPool = new CommandMemoryPool();
		_commandMemoryPool.Initialize();
		ResourceManager = new ResourceManager();
		_defaultStopEventCallback = (EventCallbackFunc)delegate(int callbackType, IntPtr eventCallbackInfo)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			EventCallbackInfo val = (EventCallbackInfo)Marshal.PtrToStructure(eventCallbackInfo, typeof(EventCallbackInfo));
			_stoppedWwisePlaybackIds.Enqueue(val.PlayingId);
		};
		_soundObjectMemoryPool = new SoundObjectMemoryPool();
		_soundObjectMemoryPool.Initialize();
		_soundObjectBuffers = _soundObjectMemoryPool.SoundObjects;
		Thread = new Thread(AudioDeviceThreadStart)
		{
			Name = "AudioDeviceThread",
			IsBackground = true
		};
		Thread.SetApartmentState(ApartmentState.STA);
		Thread.Start();
	}

	protected override void DoDispose()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_threadAlive = false;
		Thread.Join();
		Thread = null;
		_soundObjectMemoryPool.Release();
		SoundEngine.Term();
	}

	public void SetCategoryVolume(int categoryId, float volume)
	{
		SetRTPC(AudioCategoryStates[categoryId].RtpcName, MathHelper.Clamp(volume, 0f, 100f));
	}

	public void SetRTPC(string rtpcName, float value)
	{
		if (!ResourceManager.WwiseGameParameterIds.TryGetValue(rtpcName, out var value2))
		{
			Logger.Warn("Unknown RTPC {0}", rtpcName);
		}
		else
		{
			SetRTPC(value2, value);
		}
	}

	public void SetRTPC(uint rtcpId, float value)
	{
		int num = _commandMemoryPool.TakePrioritySlot();
		if (num >= 0)
		{
			ref CommandBuffers.CommandData reference = ref _commandMemoryPool.Commands.Data[num];
			reference.Type = CommandType.SetRTPC;
			reference.Volume = value;
			reference.RTPCId = rtcpId;
			_commandIdQueue.Enqueue(num);
		}
	}

	public void ReplaceOutputDevice(uint outputDeviceId)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (outputDeviceId != _currentOutputDeviceId)
		{
			_currentOutputDeviceId = outputDeviceId;
			SoundEngine.ReplaceOutput(_currentOutputDeviceId);
		}
	}

	private void RegisterPlayerSoundObject()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		int num = _soundObjectMemoryPool.TakeSlot();
		Debug.Assert(num == 0, "The player SoundObject is expected to be in SlotId 0");
		_soundObjectBuffers.SoundObjectId[PlayerSoundObjectReference.SlotId] = 1u;
		BitUtils.SwitchOnBit(2, ref _soundObjectBuffers.BoolData[PlayerSoundObjectReference.SlotId]);
		SoundEngine.RegisterGameObject(1uL);
		SoundEngine.SetDefaultListeners(1uL, 1);
	}

	private void AudioDeviceThreadStart()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		LoadBanks();
		SoundEngine.SetRTPC(_masterVolumeRTPCName, _masterVolume);
		for (int i = 0; i < AudioCategoryStates.Length; i++)
		{
			ref AudioCategoryState reference = ref AudioCategoryStates[i];
			SoundEngine.SetRTPC(reference.RtpcName, reference.Volume);
		}
		if (_currentOutputDeviceId != 0)
		{
			SoundEngine.ReplaceOutput(_currentOutputDeviceId);
		}
		RegisterPlayerSoundObject();
		while (_threadAlive)
		{
			int result;
			while (_commandIdQueue.TryDequeue(out result))
			{
				ProcessCommand(result);
			}
			int num = 0;
			for (int j = 1; j < _soundObjectBuffers.Count; j++)
			{
				if (_soundObjectBuffers.SoundObjectId[j] != 0)
				{
					_sortedSoundObjectSlotIds[num] = j;
					_sortedSoundObjectSquaredDistanceToListener[num] = Vector3.DistanceSquared(_soundObjectBuffers.Position[j], _currentListenerPosition);
					num++;
				}
			}
			Debug.Assert(_soundObjectCount == num, "Number of sorted SoundObjects is incorrect");
			Array.Sort(_sortedSoundObjectSquaredDistanceToListener, _sortedSoundObjectSlotIds, 0, _soundObjectCount);
			for (int k = 0; k < _soundObjectCount; k++)
			{
				int num2 = _sortedSoundObjectSlotIds[k];
				float num3 = _sortedSoundObjectSquaredDistanceToListener[k];
				uint num4 = _soundObjectBuffers.SoundObjectId[num2];
				byte bitfield = _soundObjectBuffers.BoolData[num2];
				bool flag = BitUtils.IsBitOn(2, bitfield);
				if (!flag && num3 < 10000f && k < 100)
				{
					SoundEngine.RegisterGameObject((ulong)num4);
					int num5 = _soundObjectBuffers.LastPlaybackId[num2];
					if (num5 != -1 && !_currentEventPlaybacksByPlaybackId.ContainsKey(num5) && _soundEventIdsByPlaybackIds.TryGetValue(num5, out var value) && TryPrepareEvent(value))
					{
						PostEventToWwise(num5, value, new SoundObjectReference(num4, num2));
					}
					BitUtils.SwitchOnBit(2, ref _soundObjectBuffers.BoolData[num2]);
					flag = true;
				}
				else if (flag && (num3 > 10000f || k >= 100))
				{
					SoundEngine.StopAll((ulong)num4);
					SoundEngine.UnRegisterGameObject((ulong)num4);
					BitUtils.SwitchOffBit(2, ref _soundObjectBuffers.BoolData[num2]);
					flag = false;
				}
				if (flag)
				{
					Vector3 vector = _soundObjectBuffers.Position[num2] - _currentListenerPosition;
					Vector3 vector2 = _soundObjectBuffers.FrontOrientation[num2];
					Vector3 vector3 = _soundObjectBuffers.TopOrientation[num2];
					SoundEngine.SetPosition((ulong)num4, vector.X, vector.Y, 0f - vector.Z, vector2.X, vector2.Y, vector2.Z, vector3.X, vector3.Y, vector3.Z);
				}
			}
			SoundEngine.RenderAudio();
			uint result2;
			while (_stoppedWwisePlaybackIds.TryDequeue(out result2))
			{
				if (!_playbackIdsByWwisePlaybackId.TryGetValue(result2, out var value2))
				{
					continue;
				}
				EventPlayback eventPlayback = _currentEventPlaybacksByPlaybackId[value2];
				_currentEventPlaybacksByPlaybackId.Remove(value2);
				_playbackIdsByWwisePlaybackId.Remove(result2);
				int slotId = eventPlayback.SoundObjectReference.SlotId;
				uint soundObjectId = eventPlayback.SoundObjectReference.SoundObjectId;
				if (_soundObjectBuffers.SoundObjectId[slotId] == soundObjectId)
				{
					byte bitfield2 = _soundObjectBuffers.BoolData[slotId];
					if (BitUtils.IsBitOn(0, bitfield2))
					{
						UnregisterSoundObject(soundObjectId, slotId);
					}
				}
				RemoveEventReference(eventPlayback.EventId);
			}
			Thread.Sleep(16);
		}
		UnloadBanks();
	}

	public OutputDevice[] GetOutputDevices()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		Thread thread = new Thread((ThreadStart)delegate
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			uint num = default(uint);
			SoundEngine.GetDeviceCount(ref num);
			ArrayUtils.GrowArrayIfNecessary(ref _outputDevices, (int)num, 0);
			DeviceDescription[] array = (DeviceDescription[])(object)new DeviceDescription[num];
			SoundEngine.GetDevices(array, ref num);
			_outputDeviceCount = (int)num;
			for (int i = 0; i < num; i++)
			{
				ref DeviceDescription reference = ref array[i];
				_outputDevices[i] = new OutputDevice
				{
					Id = reference.DeviceId,
					Name = Marshal.PtrToStringAuto(reference.DeviceName)
				};
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		return _outputDevices;
	}

	private void LoadBanks()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		uint num = default(uint);
		SoundEngine.LoadBank("Init.bnk", ref num);
		uint num2 = default(uint);
		SoundEngine.LoadBank("UI.bnk", ref num2);
		uint num3 = default(uint);
		SoundEngine.LoadBank("Master.bnk", ref num3);
		uint num4 = default(uint);
		SoundEngine.LoadBank("Music.bnk", ref num4);
	}

	private void UnloadBanks()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		foreach (EventPlayback value in _currentEventPlaybacksByPlaybackId.Values)
		{
			RemoveEventReference(value.EventId);
		}
		Debug.Assert(_eventReferenceCountByEventId.Count == 0);
		_playbackIdsByWwisePlaybackId.Clear();
		_currentEventPlaybacksByPlaybackId.Clear();
		SoundEngine.UnloadBank("Music.bnk");
		SoundEngine.UnloadBank("Master.bnk");
		SoundEngine.UnloadBank("UI.bnk");
		SoundEngine.UnloadBank("Init.bnk");
	}

	private void UnregisterSoundObject(uint soundObjectId, int slotId)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		Debug.Assert(ThreadHelper.IsOnThread(Thread));
		if (BitUtils.IsBitOn(2, _soundObjectBuffers.BoolData[slotId]))
		{
			SoundEngine.StopAll((ulong)soundObjectId);
			SoundEngine.UnRegisterGameObject((ulong)soundObjectId);
		}
		int num = _soundObjectBuffers.LastPlaybackId[slotId];
		if (num != -1)
		{
			_soundEventIdsByPlaybackIds.Remove(num);
		}
		_soundObjectBuffers.SoundObjectId[slotId] = 0u;
		_soundObjectMemoryPool.ReleaseSlot(slotId);
		_soundObjectCount--;
	}

	private void GetWwiseOrientations(Vector3 orientation, ref Vector3 frontOrientation, ref Vector3 topOrientation)
	{
		Vector3 value = new Vector3(0f, 0f, 1f);
		Vector3 value2 = new Vector3(0f, 1f, 0f);
		Quaternion.CreateFromYawPitchRoll(0f - orientation.Yaw, 0f - orientation.Pitch, 0f - orientation.Roll, out var result);
		frontOrientation = Vector3.Transform(value, result);
		frontOrientation.Normalize();
		topOrientation = Vector3.Transform(value2, result);
		topOrientation.Normalize();
	}

	private static void WwiseErrorCallback(int errorCode, IntPtr message, int errorLevel, int playingId, int gameObjectId)
	{
		if (message != IntPtr.Zero)
		{
			Logger.Warn<int, string>("Audio: {0} - {1}", errorCode, Marshal.PtrToStringAuto(message));
		}
		else
		{
			Logger.Warn("Audio: {0}", errorCode);
		}
	}
}
