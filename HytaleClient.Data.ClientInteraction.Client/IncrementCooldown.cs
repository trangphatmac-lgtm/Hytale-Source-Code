using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class IncrementCooldown : SimpleInstantInteraction
{
	public IncrementCooldown(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		string cooldownId = Interaction.CooldownId;
		if (string.IsNullOrEmpty(cooldownId))
		{
			InteractionCooldown cooldown = context.Chain.RootInteraction.RootInteraction.Cooldown;
			if (cooldown != null)
			{
				cooldownId = cooldown.CooldownId;
			}
		}
		ProcessCooldown(gameInstance, cooldownId);
		context.State.State = (InteractionState)0;
	}

	protected void ProcessCooldown(GameInstance gameInstance, string id)
	{
		Cooldown cooldown = gameInstance.InteractionModule.GetCooldown(id);
		if (cooldown != null)
		{
			if (Interaction.CooldownIncrementTime != 0f)
			{
				cooldown.IncreaseTime(Interaction.CooldownIncrementTime);
			}
			if (Interaction.CooldownIncrementCharge != 0)
			{
				cooldown.ReplenishCharge(Interaction.CooldownIncrementCharge, Interaction.CooldownIncrementInterrupt);
			}
			if (Interaction.CooldownIncrementChargeTime != 0f)
			{
				cooldown.IncreaseChargeTime(Interaction.CooldownIncrementChargeTime);
			}
		}
	}
}
