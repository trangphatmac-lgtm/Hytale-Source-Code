using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class ChangeBlockInteraction : SimpleBlockInteraction
{
	public ChangeBlockInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void InteractWithBlock(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context, BlockPosition targetBlockHit, int blockId)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		if (Interaction.BlockChanges.TryGetValue(blockId, out var value))
		{
			context.InstanceStore.OldBlockId = blockId;
			context.InstanceStore.ExpectedBlockId = value;
			gameInstance.MapModule.SetClientBlock(targetBlockHit.X, targetBlockHit.Y, targetBlockHit.Z, value);
		}
		else
		{
			context.State.State = (InteractionState)3;
		}
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		base.Revert0(gameInstance, type, context);
		InteractionSyncData state = context.State;
		int oldBlockId = context.InstanceStore.OldBlockId;
		int expectedBlockId = context.InstanceStore.ExpectedBlockId;
		if (state.BlockPosition_ != null && oldBlockId != int.MaxValue && gameInstance.MapModule.GetBlock(state.BlockPosition_.X, state.BlockPosition_.Y, state.BlockPosition_.Z, int.MaxValue) == expectedBlockId)
		{
			gameInstance.MapModule.SetClientBlock(state.BlockPosition_.X, state.BlockPosition_.Y, state.BlockPosition_.Z, oldBlockId);
		}
	}
}
