using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class EvaluateChainVariableInteraction : ClientInteraction
{
	private string[] _sortedNextKeys;

	private Dictionary<string, int> _nextIndexes;

	public EvaluateChainVariableInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
		if (interaction.ChainVariableNext != null)
		{
			_sortedNextKeys = interaction.ChainVariableNext.Keys.ToArray();
			Array.Sort(_sortedNextKeys);
			_nextIndexes = new Dictionary<string, int>();
			for (int i = 0; i < _sortedNextKeys.Length; i++)
			{
				_nextIndexes.Add(_sortedNextKeys[i], i);
			}
		}
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		if (firstRun && Interaction.ChainId != null)
		{
			if (ChainingInteraction.NamedSequenceData.TryGetValue(Interaction.ChainId, out var value) && value.Variable != null && _nextIndexes.TryGetValue(value.Variable, out var value2))
			{
				context.State.State = (InteractionState)0;
				context.State.ChainVariableNextIndex = value2;
				context.Jump(context.Labels[value2]);
				value.Variable = null;
			}
			else
			{
				context.State.State = (InteractionState)3;
				context.Jump(context.Labels[_sortedNextKeys.Length]);
			}
		}
	}

	public override void Compile(InteractionModule module, ClientRootInteraction.OperationsBuilder builder)
	{
		if (_sortedNextKeys != null)
		{
			ClientRootInteraction.Label[] array = new ClientRootInteraction.Label[_sortedNextKeys.Length + 1];
			ClientRootInteraction.Label label = builder.CreateUnresolvedLabel();
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = builder.CreateUnresolvedLabel();
			}
			builder.AddOperation(Id, array);
			builder.Jump(label);
			for (int j = 0; j < _sortedNextKeys.Length; j++)
			{
				string key = _sortedNextKeys[j];
				builder.ResolveLabel(array[j]);
				ClientInteraction clientInteraction = module.Interactions[Interaction.ChainVariableNext[key]];
				clientInteraction.Compile(module, builder);
				builder.Jump(label);
			}
			int num = _sortedNextKeys.Length;
			builder.ResolveLabel(array[num]);
			if (Interaction.Failed != int.MinValue)
			{
				ClientInteraction clientInteraction2 = module.Interactions[Interaction.Failed];
				clientInteraction2.Compile(module, builder);
			}
			builder.Jump(label);
			builder.ResolveLabel(label);
		}
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
	}

	protected override void MatchServer0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		context.State.State = context.ServerData.State;
		InteractionState state = context.State.State;
		InteractionState val = state;
		if ((int)val != 0)
		{
			if ((int)val == 3)
			{
				context.Jump(context.Labels[_sortedNextKeys.Length]);
			}
		}
		else
		{
			context.Jump(context.Labels[context.State.ChainVariableNextIndex]);
		}
	}
}
