using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class ChainingInteraction : ClientInteraction
{
	public class ChainData
	{
		public readonly Stopwatch TimeSinceLastAttack = new Stopwatch();

		public int LastSequenceIndex = -1;

		public string CurrentFlag;

		public string Variable;
	}

	private readonly Stopwatch _timeSinceLastAttack = new Stopwatch();

	private int _lastSequenceIndex = -1;

	public static Dictionary<string, ChainData> NamedSequenceData = new Dictionary<string, ChainData>();

	private string[] _sortedFlagKags;

	private Dictionary<string, int> _flagIndex;

	public ChainingInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
		if (interaction.Flags != null)
		{
			_sortedFlagKags = interaction.Flags.Keys.ToArray();
			Array.Sort(_sortedFlagKags);
			_flagIndex = new Dictionary<string, int>();
			for (int i = 0; i < _sortedFlagKags.Length; i++)
			{
				_flagIndex.Add(_sortedFlagKags[i], i);
			}
		}
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		if (!firstRun)
		{
			return;
		}
		int num = _lastSequenceIndex;
		Stopwatch timeSinceLastAttack = _timeSinceLastAttack;
		if (Interaction.ChainId != null)
		{
			if (!NamedSequenceData.TryGetValue(Interaction.ChainId, out var value))
			{
				NamedSequenceData.Add(Interaction.ChainId, value = new ChainData());
			}
			num = value.LastSequenceIndex;
			timeSinceLastAttack = value.TimeSinceLastAttack;
		}
		if (Interaction.ChainingAllowance > 0f && timeSinceLastAttack.Elapsed.TotalSeconds * (double)gameInstance.TimeDilationModifier > (double)Interaction.ChainingAllowance)
		{
			num = -1;
			if (Interaction.ChainId != null)
			{
				NamedSequenceData[Interaction.ChainId].CurrentFlag = null;
				NamedSequenceData[Interaction.ChainId].Variable = null;
			}
		}
		bool flag = false;
		if (Interaction.ChainId != null)
		{
			ChainData chainData = NamedSequenceData[Interaction.ChainId];
			if (chainData.CurrentFlag != null && _flagIndex.TryGetValue(chainData.CurrentFlag, out var value2))
			{
				context.State.FlagIndex = value2;
				context.Jump(context.Labels[Interaction.ChainingNext.Length + value2]);
				flag = true;
			}
		}
		num++;
		if (num >= Interaction.ChainingNext.Length)
		{
			num = 0;
		}
		if (!flag)
		{
			context.State.ChainingIndex = num;
			context.Jump(context.Labels[num]);
		}
		context.State.State = (InteractionState)0;
		timeSinceLastAttack.Restart();
		if (Interaction.ChainId != null)
		{
			ChainData chainData2 = NamedSequenceData[Interaction.ChainId];
			chainData2.LastSequenceIndex = num;
			chainData2.CurrentFlag = null;
		}
		else
		{
			_lastSequenceIndex = num;
		}
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
		int lastSequenceIndex = _lastSequenceIndex;
		if (Interaction.ChainId != null)
		{
			if (!NamedSequenceData.TryGetValue(Interaction.ChainId, out var value))
			{
				return;
			}
			lastSequenceIndex = value.LastSequenceIndex;
		}
		lastSequenceIndex--;
		if (lastSequenceIndex < 0)
		{
			lastSequenceIndex = Interaction.ChainingNext.Length - 1;
		}
		if (Interaction.ChainId != null)
		{
			NamedSequenceData[Interaction.ChainId].LastSequenceIndex = lastSequenceIndex;
		}
	}

	protected override void MatchServer0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if ((int)context.ServerData.State != 4)
		{
			throw new Exception("Server in unexpected state");
		}
		Tick0(gameInstance, clickType, hasAnyButtonClick, firstRun: true, 0f, type, context);
	}

	public override void Compile(InteractionModule module, ClientRootInteraction.OperationsBuilder builder)
	{
		int num = Interaction.ChainingNext.Length;
		string[] sortedFlagKags = _sortedFlagKags;
		ClientRootInteraction.Label[] array = new ClientRootInteraction.Label[num + ((sortedFlagKags != null) ? sortedFlagKags.Length : 0)];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = builder.CreateUnresolvedLabel();
		}
		builder.AddOperation(Id, array);
		ClientRootInteraction.Label label = builder.CreateUnresolvedLabel();
		for (int j = 0; j < Interaction.ChainingNext.Length; j++)
		{
			builder.ResolveLabel(array[j]);
			ClientInteraction clientInteraction = module.Interactions[Interaction.ChainingNext[j]];
			clientInteraction.Compile(module, builder);
			builder.Jump(label);
		}
		if (_sortedFlagKags != null)
		{
			for (int k = 0; k < _sortedFlagKags.Length; k++)
			{
				string key = _sortedFlagKags[k];
				builder.ResolveLabel(array[Interaction.ChainingNext.Length + k]);
				ClientInteraction clientInteraction2 = module.Interactions[Interaction.Flags[key]];
				clientInteraction2.Compile(module, builder);
				builder.Jump(label);
			}
		}
		builder.ResolveLabel(label);
	}
}
