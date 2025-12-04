using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HytaleClient.Data.ClientInteraction;
using HytaleClient.Data.Items;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Interaction;

internal class InteractionChain : InteractionModule.ChainSyncStorage
{
	public class CallState
	{
		public readonly ClientRootInteraction RootInteraction;

		public readonly int OperationCounter;

		public CallState(ClientRootInteraction rootInteraction, int operationCounter)
		{
			RootInteraction = rootInteraction;
			OperationCounter = operationCounter;
		}
	}

	public class TempChain : InteractionModule.ChainSyncStorage
	{
		public readonly Dictionary<ulong, TempChain> TempForkedChainData = new Dictionary<ulong, TempChain>();

		public readonly List<InteractionSyncData> TempSyncData = new List<InteractionSyncData>();

		public InteractionState ServerState { get; set; } = (InteractionState)4;


		public TempChain GetTempForkedChain(ForkedChainId chainId)
		{
			ulong key = ForkedIdToIndex(chainId);
			if (!TempForkedChainData.TryGetValue(key, out var value))
			{
				value = new TempChain();
				TempForkedChainData.Add(key, value);
			}
			return value;
		}

		public void PutInteractionSyncData(int index, InteractionSyncData data)
		{
			if (index < TempSyncData.Count)
			{
				TempSyncData[index] = data;
				return;
			}
			if (index == TempSyncData.Count)
			{
				TempSyncData.Add(data);
				return;
			}
			throw new Exception($"Temp sync data send out of order: {index} {TempSyncData.Count}");
		}

		public void SyncFork(GameInstance gameInstance, SyncInteractionChain packet)
		{
			ForkedChainId forkedId = packet.ForkedId;
			while (forkedId.ForkedId != null)
			{
				forkedId = forkedId.ForkedId;
			}
			TempChain tempForkedChain = GetTempForkedChain(forkedId);
			gameInstance.InteractionModule.Sync(tempForkedChain, packet);
		}
	}

	public readonly InteractionType Type;

	public readonly InteractionChainData ChainData;

	public int ChainId;

	public readonly ForkedChainId ForkedChainId;

	public readonly ForkedChainId BaseForkedChainId;

	public bool Predicted;

	public bool Desync;

	public readonly InteractionContext Context;

	public int ServerCompleteIndex;

	public readonly Dictionary<ulong, InteractionChain> ForkedChains = new Dictionary<ulong, InteractionChain>();

	private readonly Dictionary<ulong, TempChain> _tempForkedChainData = new Dictionary<ulong, TempChain>();

	private readonly Dictionary<ulong, ulong> _forkedChainsMap = new Dictionary<ulong, ulong>();

	public readonly List<InteractionChain> NewForks = new List<InteractionChain>();

	public ClientRootInteraction InitialRootInteraction;

	public ClientRootInteraction RootInteraction;

	public int OperationCounter;

	private readonly List<CallState> _callStack = new List<CallState>();

	public bool ServerCancelled = false;

	public int OperationIndex;

	private int _operationIndexOffset;

	private readonly List<InteractionEntry> _interactions = new List<InteractionEntry>();

	private readonly List<InteractionSyncData> _tempSyncData = new List<InteractionSyncData>();

	public bool ServerAck = false;

	public InteractionEntry PreviousInteractionEntry;

	private int _tempSyncDataOffset;

	public readonly Stopwatch Time = new Stopwatch();

	public readonly Stopwatch WaitingForServerFinished = new Stopwatch();

	public readonly Stopwatch WaitingForClientFinished = new Stopwatch();

	public InteractionState ClientState = (InteractionState)4;

	public InteractionState FinalState = (InteractionState)0;

	public Action OnCompletion;

	public readonly int InitialSlot;

	public readonly ClientItemStack InitialItem;

	public bool SentInitialState;

	public float TimeShift;

	private bool _firstRun = true;

	internal bool SkipChainOnClick;

	public InteractionState ServerState { get; set; } = (InteractionState)4;


	public bool HasTempSyncData => _tempSyncData.Count > 0;

	public InteractionChain(InteractionType type, InteractionContext context, InteractionChainData chainData, ClientRootInteraction rootInteraction, int initialSlot, ClientItemStack initialItem, Action onCompletion)
		: this(null, null, type, context, chainData, rootInteraction, initialSlot, initialItem, onCompletion)
	{
	}//IL_0003: Unknown result type (might be due to invalid IL or missing references)


	public InteractionChain(ForkedChainId forkedChainId, ForkedChainId baseForkedChainId, InteractionType type, InteractionContext context, InteractionChainData chainData, ClientRootInteraction rootInteraction, int initialSlot, ClientItemStack initialItem, Action onCompletion)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		Type = type;
		ChainData = chainData;
		OnCompletion = onCompletion;
		ForkedChainId = forkedChainId;
		BaseForkedChainId = baseForkedChainId;
		InitialRootInteraction = (RootInteraction = rootInteraction);
		Context = context;
		InitialSlot = initialSlot;
		InitialItem = initialItem;
	}

	public void UpdateClientState(GameInstance gameInstance, InteractionModule.ClickType clickType)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Invalid comparison between Unknown and I4
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		if ((int)ClientState != 4)
		{
			return;
		}
		InteractionEntry ret;
		if (OperationCounter >= RootInteraction.Operations.Length)
		{
			ClientState = FinalState;
		}
		else if (GetInteraction(OperationIndex, out ret))
		{
			InteractionState state = ret.State.State;
			InteractionState val = state;
			if ((int)val == 0 || (int)val == 4)
			{
				ClientState = (InteractionState)4;
			}
			else
			{
				ClientState = (InteractionState)3;
			}
		}
	}

	public void RemoveForksForEntry(InteractionModule module, int entry)
	{
		if (ForkedChains.Count == 0)
		{
			return;
		}
		List<ulong> list = (from e in ForkedChains
			where (int)(e.Key >> 32) == entry && e.Value.Predicted
			select e.Key).ToList();
		foreach (ulong item in list)
		{
			module.RevertChain(ForkedChains[item], 0);
			ForkedChains.Remove(item);
		}
	}

	public void NextOperationIndex()
	{
		OperationIndex++;
	}

	public bool FindForkedChain(GameInstance gameInstance, ForkedChainId chainId, InteractionChainData data, out InteractionChain ret)
	{
		ulong key = ForkedIdToIndex(chainId);
		if (_forkedChainsMap.TryGetValue(key, out var value))
		{
			key = value;
		}
		if (ForkedChains.TryGetValue(key, out ret) || data == null)
		{
			return ret != null;
		}
		if (!GetInteraction(chainId.EntryIndex, out var ret2))
		{
			return false;
		}
		int rootInteraction = ret2.State.RootInteraction;
		int operationCounter = ret2.State.OperationCounter;
		ClientRootInteraction clientRootInteraction = gameInstance.InteractionModule.RootInteractions[rootInteraction];
		ClientRootInteraction.Operation operation = clientRootInteraction.Operations[operationCounter];
		if (operation is ClientRootInteraction.InteractionWrapper interactionWrapper)
		{
			ClientInteraction interaction = interactionWrapper.GetInteraction(gameInstance.InteractionModule);
			Context.InitEntry(this, ret2, gameInstance);
			ret = interaction.MapForkChain(Context, data);
			Context.DeinitEntry(this, ret2, gameInstance);
			if (ret != null)
			{
				_forkedChainsMap.Add(key, ForkedIdToIndex(ret.BaseForkedChainId));
				return true;
			}
		}
		return false;
	}

	public bool GetForkedChain(ForkedChainId chainId, out InteractionChain ret)
	{
		ulong key = ForkedIdToIndex(chainId);
		if (_forkedChainsMap.TryGetValue(key, out var value))
		{
			key = value;
		}
		return ForkedChains.TryGetValue(key, out ret);
	}

	public bool RemoveTempForkedChain(ForkedChainId chainId, out TempChain ret)
	{
		ulong key = ForkedIdToIndex(chainId);
		if (_forkedChainsMap.TryGetValue(key, out var value))
		{
			key = value;
		}
		if (_tempForkedChainData.TryGetValue(key, out ret))
		{
			return _tempForkedChainData.Remove(key);
		}
		return false;
	}

	public TempChain GetTempForkedChain(ForkedChainId chainId)
	{
		ulong key = ForkedIdToIndex(chainId);
		if (!_tempForkedChainData.TryGetValue(key, out var value))
		{
			value = new TempChain();
			_tempForkedChainData.Add(key, value);
		}
		return value;
	}

	internal void PutForkedChain(ForkedChainId chainId, InteractionChain val)
	{
		NewForks.Add(val);
		ForkedChains.Add(ForkedIdToIndex(chainId), val);
	}

	internal void RemoveForkedChain(InteractionModule module, ForkedChainId chainId)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		ulong key = ForkedIdToIndex(chainId);
		if (ForkedChains.TryGetValue(key, out var value))
		{
			module.RevertChain(value, 0);
			value.ClientState = (InteractionState)3;
			value.ServerState = (InteractionState)3;
		}
		ForkedChains.Remove(key);
	}

	public bool GetInteraction(int index, out InteractionEntry ret)
	{
		index -= _operationIndexOffset;
		if (index < 0 || index >= _interactions.Count)
		{
			ret = null;
			return false;
		}
		ret = _interactions[index];
		return ret != null;
	}

	public InteractionEntry GetOrCreateInteractionEntry(int index)
	{
		int num = index - _operationIndexOffset;
		InteractionEntry interactionEntry = ((num < _interactions.Count) ? _interactions[num] : null);
		if (interactionEntry == null)
		{
			if (num != _interactions.Count)
			{
				throw new Exception($"Trying to add interaction entry at a weird location: {num} {_interactions.Count}");
			}
			interactionEntry = new InteractionEntry(index, OperationCounter, RootInteraction.Index);
			_interactions.Add(interactionEntry);
		}
		return interactionEntry;
	}

	public void ShiftInteractionEntryOffset(int amount)
	{
		_interactions.Clear();
		if (_operationIndexOffset > 0)
		{
			_operationIndexOffset -= amount;
		}
	}

	public void RemoveInteractionEntry(int index)
	{
		int num = index - _operationIndexOffset;
		if (num != 0)
		{
			throw new Exception("Trying to remove out of order");
		}
		PreviousInteractionEntry = _interactions[num];
		_interactions.RemoveAt(num);
		_operationIndexOffset++;
	}

	public void PutInteractionSyncData(int index, InteractionSyncData data)
	{
		index -= _tempSyncDataOffset;
		if (index < _tempSyncData.Count)
		{
			_tempSyncData[index] = data;
			return;
		}
		if (index == _tempSyncData.Count)
		{
			_tempSyncData.Add(data);
			return;
		}
		throw new Exception($"Temp sync data send out of order: {index} {_tempSyncData.Count}");
	}

	public void SyncFork(GameInstance gameInstance, SyncInteractionChain packet)
	{
		ForkedChainId forkedId = packet.ForkedId;
		while (forkedId.ForkedId != null)
		{
			forkedId = forkedId.ForkedId;
		}
		if (FindForkedChain(gameInstance, forkedId, packet.Data, out var ret))
		{
			gameInstance.InteractionModule.Sync(ret, packet);
			return;
		}
		if (packet.OverrideRootInteraction != int.MinValue && packet.ForkedId != null)
		{
			gameInstance.InteractionModule.Handle(packet);
			return;
		}
		TempChain tempForkedChain = GetTempForkedChain(forkedId);
		gameInstance.InteractionModule.Sync(tempForkedChain, packet);
	}

	public InteractionSyncData RemoveInteractionSyncData(int index)
	{
		index -= _tempSyncDataOffset;
		if (index != 0)
		{
			return null;
		}
		if (_tempSyncData.Count == 0)
		{
			return null;
		}
		InteractionSyncData val = _tempSyncData[index];
		if (val != null)
		{
			_tempSyncData.RemoveAt(index);
			_tempSyncDataOffset++;
		}
		return val;
	}

	public InteractionSyncData GetInteractionSyncData(int index)
	{
		index -= _tempSyncDataOffset;
		if (index != 0)
		{
			return null;
		}
		if (_tempSyncData.Count == 0)
		{
			return null;
		}
		return _tempSyncData[index];
	}

	public void UpdateSyncPosition(int index)
	{
		if (_tempSyncDataOffset == index)
		{
			_tempSyncDataOffset = index + 1;
		}
		else if (index > _tempSyncDataOffset)
		{
			throw new Exception($"Temp sync data send out of order: {index} {_tempSyncData.Count}");
		}
	}

	public void CopyTempFrom(TempChain tempChain)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		ServerState = tempChain.ServerState;
		_tempSyncData.AddRange(tempChain.TempSyncData);
		foreach (KeyValuePair<ulong, TempChain> tempForkedChainDatum in tempChain.TempForkedChainData)
		{
			_tempForkedChainData.Add(tempForkedChainDatum.Key, tempForkedChainDatum.Value);
		}
	}

	public int GetCallDepth()
	{
		return _callStack.Count;
	}

	public void PushRoot(ClientRootInteraction nextInteraction)
	{
		CallState callState = new CallState(RootInteraction, OperationCounter);
		_callStack.Add(callState);
		_interactions[OperationIndex - _operationIndexOffset].EnteredCallState = callState;
		OperationCounter = 0;
		RootInteraction = nextInteraction;
	}

	public void PopRoot()
	{
		CallState callState = _callStack[_callStack.Count - 1];
		_callStack.RemoveAt(_callStack.Count - 1);
		RootInteraction = callState.RootInteraction;
		OperationCounter = callState.OperationCounter + 1;
	}

	public bool ConsumeFirstRun()
	{
		bool firstRun = _firstRun;
		_firstRun = false;
		return firstRun;
	}

	public bool ConsumeDesync()
	{
		bool desync = Desync;
		Desync = false;
		return desync;
	}

	public void ClearIncompleteSyncData()
	{
		int num = _tempSyncData.FindIndex((InteractionSyncData v) => (int)v.State == 4);
		if (num != -1)
		{
			_tempSyncData.RemoveRange(num, _tempSyncData.Count - num);
		}
	}

	public void ClearSyncData()
	{
		_tempSyncData.Clear();
	}

	internal int LowestInteractionIndex()
	{
		return _interactions.Select((InteractionEntry v) => v.Index).Min();
	}

	public void ClearInteractions()
	{
		_interactions.Clear();
		_operationIndexOffset = 0;
	}

	public override string ToString()
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		return string.Format("{0}: {1}, {2}: {3}, {4}: {5}, {6}: {7}, {8}: {9}, {10}: {11}", "Time", Time.Elapsed.TotalSeconds, "ChainData", ChainData, "WaitingForServerFinished", WaitingForServerFinished.Elapsed.TotalSeconds, "TotalSeconds", WaitingForClientFinished, "ClientState", ClientState, "ServerState", ServerState);
	}

	public static ulong ForkedIdToIndex(ForkedChainId chainId)
	{
		return (ulong)(((long)chainId.EntryIndex << 32) | (chainId.SubIndex & 0xFFFFFFFFu));
	}
}
