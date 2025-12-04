using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class ChangeActiveSlotInteraction : ClientInteraction
{
	public ChangeActiveSlotInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		if (!firstRun)
		{
			context.State.State = (InteractionState)0;
			return;
		}
		context.InstanceStore.OriginalSlot = gameInstance.InventoryModule.HotbarActiveSlot;
		int num;
		if (Interaction.TargetSlot == int.MinValue)
		{
			num = context.MetaStore.TargetSlot.Value;
		}
		else
		{
			if (gameInstance.InventoryModule.HotbarActiveSlot == Interaction.TargetSlot)
			{
				context.State.State = (InteractionState)0;
				return;
			}
			num = Interaction.TargetSlot;
			context.MetaStore.TargetSlot = num;
		}
		gameInstance.InventoryModule.SetActiveHotbarSlot(num, triggerInteraction: false);
		if (context.MetaStore.DisableSlotFork)
		{
			if (context.ServerData == null)
			{
				context.State.State = (InteractionState)4;
			}
			else
			{
				context.State.State = context.ServerData.State;
			}
			return;
		}
		InteractionContext interactionContext = InteractionContext.ForInteraction(gameInstance, gameInstance.InventoryModule, (InteractionType)14);
		if (interactionContext.GetRootInteractionId(gameInstance, (InteractionType)14, out var id))
		{
			if (Interaction.TargetSlot != int.MinValue)
			{
				interactionContext.MetaStore.TargetSlot = num;
			}
			context.Fork((InteractionType)14, interactionContext, id);
		}
		context.State.State = (InteractionState)0;
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
		int num;
		if (Interaction.TargetSlot == int.MinValue)
		{
			num = context.MetaStore.TargetSlot.Value;
		}
		else
		{
			num = Interaction.TargetSlot;
			context.MetaStore.TargetSlot = num;
		}
		if (gameInstance.InventoryModule.HotbarActiveSlot == num)
		{
			gameInstance.InventoryModule.SetActiveHotbarSlot(context.InstanceStore.OriginalSlot.Value, triggerInteraction: false);
		}
	}

	protected override void MatchServer0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Tick0(gameInstance, clickType, hasAnyButtonClick, firstRun: true, 0f, type, context);
	}
}
