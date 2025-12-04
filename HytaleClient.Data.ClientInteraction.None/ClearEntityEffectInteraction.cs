using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class ClearEntityEffectInteraction : SimpleInstantInteraction
{
	public ClearEntityEffectInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		context.InstanceStore.PredictedEffect = gameInstance.LocalPlayer.PredictedRemoveEffect(Interaction.EffectId);
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
		gameInstance.LocalPlayer.CancelPrediction(context.InstanceStore.PredictedEffect);
	}
}
