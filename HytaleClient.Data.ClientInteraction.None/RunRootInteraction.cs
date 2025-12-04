using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class RunRootInteraction : SimpleInstantInteraction
{
	public RunRootInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		context.Execute(gameInstance.InteractionModule.RootInteractions[Interaction.RootInteraction]);
		context.State.State = (InteractionState)0;
	}

	protected override void MatchServer0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		context.State.State = context.ServerData.State;
		if ((int)context.State.State == 0)
		{
			context.Execute(gameInstance.InteractionModule.RootInteractions[context.ServerData.EnteredRootInteraction]);
		}
		base.MatchServer0(gameInstance, clickType, hasAnyButtonClick, type, context);
	}
}
