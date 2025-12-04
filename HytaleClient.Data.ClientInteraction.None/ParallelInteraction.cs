using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class ParallelInteraction : ClientInteraction
{
	public ParallelInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		context.Execute(gameInstance.InteractionModule.RootInteractions[Interaction.ChainingNext[0]]);
		for (int i = 1; i < Interaction.ChainingNext.Length; i++)
		{
			int rootInteractionId = Interaction.ChainingNext[i];
			context.InstanceStore.ForkedChain = context.Fork(context.Duplicate(), rootInteractionId);
		}
		context.State.State = (InteractionState)0;
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
	}

	protected override void MatchServer0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Tick0(gameInstance, InteractionModule.ClickType.Single, hasAnyButtonClick: false, firstRun: true, 0f, type, context);
	}
}
