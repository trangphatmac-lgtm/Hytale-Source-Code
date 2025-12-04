using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class CooldownConditionInteraction : SimpleInstantInteraction
{
	public CooldownConditionInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		if (CheckCooldown(gameInstance, Interaction.CooldownId))
		{
			context.State.State = (InteractionState)3;
		}
		else
		{
			context.State.State = (InteractionState)0;
		}
	}

	protected bool CheckCooldown(GameInstance gameInstance, string id)
	{
		return gameInstance.InteractionModule.GetCooldown(id)?.HasCooldown(deduct: false) ?? false;
	}
}
