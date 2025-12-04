using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class EffectConditionInteraction : SimpleInstantInteraction
{
	public EffectConditionInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Invalid comparison between Unknown and I4
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		if (!ClientInteraction.TryGetEntity(gameInstance, context, Interaction.EntityTarget, out var entity))
		{
			return;
		}
		for (int i = 0; i < Interaction.EntityEffects.Length; i++)
		{
			Match match_ = Interaction.Match_;
			Match val = match_;
			if ((int)val != 0)
			{
				if ((int)val == 1 && entity.HasEffect(Interaction.EntityEffects[i]))
				{
					context.State.State = (InteractionState)3;
					break;
				}
			}
			else if (!entity.HasEffect(Interaction.EntityEffects[i]))
			{
				context.State.State = (InteractionState)3;
				break;
			}
		}
	}
}
