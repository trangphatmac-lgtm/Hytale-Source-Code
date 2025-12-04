using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class ResetCooldownInteraction : SimpleInstantInteraction
{
	public ResetCooldownInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		string cooldownId = null;
		float cooldownTime = 0f;
		float[] chargeTimes = null;
		bool interruptRecharge = false;
		if (Interaction.Cooldown != null)
		{
			cooldownId = Interaction.Cooldown.CooldownId;
			cooldownTime = Interaction.Cooldown.Cooldown;
			chargeTimes = Interaction.Cooldown.ChargeTimes;
			interruptRecharge = Interaction.Cooldown.InterruptRecharge;
		}
		ResetCooldown(context, gameInstance, cooldownId, cooldownTime, chargeTimes, interruptRecharge);
	}

	protected void ResetCooldown(InteractionContext context, GameInstance gameInstance, string cooldownId, float cooldownTime, float[] chargeTimes, bool interruptRecharge0)
	{
		float num = 0.35f;
		float[] array = InteractionModule.DefaultChargeTimes;
		bool interruptRecharge = false;
		if (cooldownId == null)
		{
			RootInteraction rootInteraction = context.Chain.RootInteraction.RootInteraction;
			InteractionCooldown cooldown = rootInteraction.Cooldown;
			if (cooldown != null)
			{
				cooldownId = cooldown.CooldownId;
				if (cooldown.Cooldown > 0f)
				{
					num = cooldown.Cooldown;
				}
				if (cooldown.InterruptRecharge)
				{
					interruptRecharge = true;
				}
				if (cooldown.ChargeTimes != null && cooldown.ChargeTimes.Length != 0)
				{
					array = cooldown.ChargeTimes;
				}
			}
			if (cooldownId == null)
			{
				cooldownId = rootInteraction.Id;
			}
		}
		Cooldown cooldown2 = gameInstance.InteractionModule.GetCooldown(cooldownId);
		if (cooldown2 != null)
		{
			num = cooldown2.GetCooldown();
			array = cooldown2.GetCharges();
			interruptRecharge = cooldown2.InterruptRecharge();
		}
		if (cooldownTime > 0f)
		{
			num = cooldownTime;
		}
		if (chargeTimes != null && chargeTimes.Length != 0)
		{
			array = chargeTimes;
		}
		if (interruptRecharge0)
		{
			interruptRecharge = true;
		}
		Cooldown cooldown3 = gameInstance.InteractionModule.GetCooldown(cooldownId, num, array, force: true, interruptRecharge);
		cooldown3.SetCooldownMax(num);
		cooldown3.SetCharges(array);
		cooldown3.ResetCooldown();
		cooldown3.ResetCharges();
	}
}
