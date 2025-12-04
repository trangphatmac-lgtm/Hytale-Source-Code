using System;
using System.Collections.Generic;
using HytaleClient.Data.EntityStats;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;
using NLog;

namespace HytaleClient.Data.ClientInteraction.Server;

internal class ChangeStatInteraction : SimpleInstantInteraction
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public ChangeStatInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Invalid comparison between Unknown and I4
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Invalid comparison between Unknown and I4
		if (ClientInteraction.TryGetEntity(gameInstance, context, Interaction.EntityTarget, out var entity))
		{
			EntityStatUpdate[] array = (EntityStatUpdate[])(object)new EntityStatUpdate[Interaction.StatModifiers.Count];
			int num = 0;
			foreach (KeyValuePair<int, float> statModifier in Interaction.StatModifiers)
			{
				int key = statModifier.Key;
				float num2 = statModifier.Value;
				if ((int)Interaction.ValueType_ == 0)
				{
					ClientEntityStatValue entityStat = entity.GetEntityStat(key);
					if (entityStat == null)
					{
						num++;
						continue;
					}
					num2 = num2 * (entityStat.Max - entityStat.Min) / 100f;
				}
				if (num2 == 0f)
				{
					continue;
				}
				ChangeStatBehaviour changeStatBehaviour_ = Interaction.ChangeStatBehaviour_;
				ChangeStatBehaviour val = changeStatBehaviour_;
				if ((int)val != 0)
				{
					if ((int)val != 1)
					{
						throw new ArgumentOutOfRangeException();
					}
					array[num++] = entity.SetStatValue(key, num2);
				}
				else
				{
					array[num++] = entity.AddStatValue(key, num2);
				}
			}
			context.InstanceStore.PredictedStats = array;
		}
		else
		{
			Logger.Error($"Entity does not exist for ChangeStatInteraction in {type}, ID: {Id}");
		}
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
		if (context.InstanceStore.PredictedStats == null)
		{
			return;
		}
		int num = 0;
		foreach (KeyValuePair<int, float> statModifier in Interaction.StatModifiers)
		{
			EntityStatUpdate val = context.InstanceStore.PredictedStats[num++];
			if (val != null)
			{
				gameInstance.LocalPlayer.CancelStatPrediction(statModifier.Key, val);
			}
		}
	}
}
