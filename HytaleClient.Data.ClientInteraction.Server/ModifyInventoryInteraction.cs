using HytaleClient.Data.Items;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Server;

internal class ModifyInventoryInteraction : SimpleInstantInteraction
{
	public ModifyInventoryInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		if (Interaction.RequiredGameMode != gameInstance.GameMode)
		{
			return;
		}
		if (Interaction.ItemToRemove != null)
		{
		}
		if (context.HeldItem != null && Interaction.AdjustHeldItemQuantity < 0 && context.HeldItem.Quantity == -Interaction.AdjustHeldItemQuantity)
		{
			context.HeldItemContainer[context.HeldItemSlot] = null;
			gameInstance.InventoryModule.UpdateAll();
			context.HeldItem = null;
		}
		ClientItemStack clientItemStack = context.HeldItem?.Clone();
		if (clientItemStack != null)
		{
			clientItemStack.Durability += Interaction.AdjustHeldItemDurability;
			if (clientItemStack.Durability <= 0.0 && clientItemStack.MaxDurability > 0.0)
			{
				clientItemStack = ((Interaction.BrokenItem != null) ? new ClientItemStack(Interaction.BrokenItem) : null);
			}
			context.HeldItemContainer[context.HeldItemSlot] = clientItemStack;
			gameInstance.InventoryModule.UpdateAll();
			context.HeldItem = clientItemStack;
		}
	}
}
