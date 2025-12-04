using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class SimpleBlockInteraction : SimpleInstantInteraction
{
	public SimpleBlockInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		if (context.MetaStore.TargetBlockRaw == null)
		{
			context.State.State = (InteractionState)3;
			return;
		}
		BlockPosition targetBlock = context.MetaStore.TargetBlock;
		context.State.BlockPosition_ = context.MetaStore.TargetBlockRaw;
		int block = gameInstance.MapModule.GetBlock(targetBlock.X, targetBlock.Y, targetBlock.Z, 1);
		if (block == 1 || block == 0)
		{
			context.State.State = (InteractionState)3;
		}
		else
		{
			InteractWithBlock(gameInstance, clickType, hasAnyButtonClick, type, context, context.State.BlockPosition_, block);
		}
	}

	protected virtual void InteractWithBlock(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context, BlockPosition targetBlockHit, int blockId)
	{
	}
}
