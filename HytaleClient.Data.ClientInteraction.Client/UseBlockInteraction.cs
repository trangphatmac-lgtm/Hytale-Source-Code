using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class UseBlockInteraction : SimpleBlockInteraction
{
	public UseBlockInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void InteractWithBlock(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context, BlockPosition targetBlockHit, int blockId)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (gameInstance.MapModule.ClientBlockTypes[blockId].Interactions.TryGetValue(type, out var value))
		{
			context.State.State = (InteractionState)0;
			context.Execute(gameInstance.InteractionModule.RootInteractions[value]);
		}
		else
		{
			context.State.State = (InteractionState)3;
		}
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
