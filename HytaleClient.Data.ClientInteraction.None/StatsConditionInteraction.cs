using System.Collections.Generic;
using HytaleClient.Data.EntityStats;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class StatsConditionInteraction : SimpleInstantInteraction
{
	public StatsConditionInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		bool flag = true;
		foreach (KeyValuePair<int, float> cost in Interaction.Costs)
		{
			ClientEntityStatValue entityStat = gameInstance.LocalPlayer.GetEntityStat(cost.Key);
			if (entityStat != null)
			{
				float value = GetValue(entityStat);
				if (Interaction.LessThan)
				{
					if (value >= cost.Value)
					{
						flag = false;
						break;
					}
				}
				else if (value < cost.Value && !CanOverdraw(value, entityStat.Min))
				{
					flag = false;
					break;
				}
				continue;
			}
			flag = false;
			break;
		}
		if (!flag)
		{
			context.State.State = (InteractionState)3;
		}
	}

	protected float GetValue(ClientEntityStatValue stat)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		return ((int)Interaction.ValueType_ == 1) ? stat.Value : (stat.AsPercentage() * 100f);
	}

	protected bool CanOverdraw(float value, float min)
	{
		return Interaction.Lenient && value > 0f && min < 0f;
	}
}
