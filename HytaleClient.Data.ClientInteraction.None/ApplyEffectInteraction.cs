using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;
using NLog;

namespace HytaleClient.Data.ClientInteraction.None;

internal class ApplyEffectInteraction : SimpleInstantInteraction
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public ApplyEffectInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (ClientInteraction.TryGetEntity(gameInstance, context, Interaction.EntityTarget, out var entity))
		{
			context.InstanceStore.PredictedEffect = entity.PredictedAddEffect(Interaction.EffectId);
		}
		else
		{
			Logger.Error($"Entity does not exist for ApplyEffectInteraction in {type}, ID: {Id}");
		}
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if (ClientInteraction.TryGetEntity(gameInstance, context, Interaction.EntityTarget, out var entity))
		{
			entity.CancelPrediction(context.InstanceStore.PredictedEffect);
		}
		else
		{
			Logger.Error($"Entity does not exist for ApplyEffectInteraction in {type}, ID: {Id}");
		}
	}
}
