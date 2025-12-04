using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Interaction;

internal class InteractionEntry
{
	public readonly int Index;

	public readonly InteractionMetaStore InteractionMetaStore = new InteractionMetaStore();

	public readonly Stopwatch Time = new Stopwatch();

	public float TimeOffset;

	public readonly InteractionSyncData State = new InteractionSyncData
	{
		State = (InteractionState)4
	};

	private InteractionSyncData _serverState;

	public readonly Stopwatch WaitingForSyncData = new Stopwatch();

	public readonly Stopwatch WaitingForServerFinished = new Stopwatch();

	public readonly Stopwatch WaitingForClientFinished = new Stopwatch();

	private int _nextForkId;

	private int _nextPredictedForkId;

	public InteractionChain.CallState EnteredCallState;

	public InteractionSyncData ServerState
	{
		get
		{
			return _serverState;
		}
		set
		{
			_serverState = value;
			if (_serverState != null && (_serverState.OperationCounter != State.OperationCounter || _serverState.RootInteraction != State.RootInteraction))
			{
				throw new Exception($"{Index}: Client/Server desync {State.OperationCounter} != {_serverState.OperationCounter}, {State.RootInteraction} != {_serverState.RootInteraction}");
			}
		}
	}

	public InteractionEntry(int index, int operationCounter, int rootInteraction)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		Index = index;
		State.OperationCounter = operationCounter;
		State.RootInteraction = rootInteraction;
	}

	public int NextForkId()
	{
		return _nextForkId++;
	}

	public int NextPredictedForkId()
	{
		return --_nextPredictedForkId;
	}

	public void SetTimestamp(float shift)
	{
		Time.Start();
		TimeOffset = shift;
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}, {4}: {5}, {6}: {7}, {8}: {9}, {10}: {11}, {12}: {13}", "Time", Time.Elapsed.TotalSeconds, "InteractionMetaStore", InteractionMetaStore, "State", State, "ServerState", ServerState, "WaitingForSyncData", WaitingForSyncData.Elapsed.TotalSeconds, "WaitingForServerFinished", WaitingForServerFinished.Elapsed.TotalSeconds, "WaitingForClientFinished", WaitingForClientFinished.Elapsed.TotalSeconds);
	}

	public int GetClientDataHashCode()
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		float progress = State.Progress;
		State.Progress = (int)progress;
		int num = ((object)State).GetHashCode();
		if (State.ForkCounts != null)
		{
			foreach (KeyValuePair<InteractionType, int> forkCount in State.ForkCounts)
			{
				int num2 = num * 397;
				InteractionType key = forkCount.Key;
				num = num2 ^ ((object)(InteractionType)(ref key)).GetHashCode();
				num = (num * 397) ^ forkCount.Value.GetHashCode();
			}
		}
		State.Progress = progress;
		return num;
	}
}
