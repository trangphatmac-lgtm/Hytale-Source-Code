using System;
using System.Collections.Generic;
using HytaleClient.Data.ClientInteraction;
using HytaleClient.Data.Items;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Interaction;

internal class InteractionContext
{
	public readonly InventorySectionType HeldItemSectionId;

	public readonly ClientItemStack[] HeldItemContainer;

	public readonly int HeldItemSlot;

	public ClientItemStack HeldItem;

	public readonly ContextMetaStore MetaStore = new ContextMetaStore();

	public ClientRootInteraction.Label[] Labels;

	public string OriginalItemType { get; private set; }

	public bool AllowSkipChainOnClick => Chain.SkipChainOnClick;

	public int OperationCounter
	{
		get
		{
			return Chain.OperationCounter;
		}
		set
		{
			Chain.OperationCounter = value;
		}
	}

	public InteractionSyncData State { get; private set; }

	public InteractionMetaStore InstanceStore { get; private set; }

	public InteractionSyncData ServerData { get; private set; }

	public InteractionChain Chain { get; private set; }

	public InteractionEntry Entry { get; private set; }

	public GameInstance GameInstance { get; private set; }

	public Entity Entity { get; private set; }

	private InteractionContext(Entity runningForEntity, InventorySectionType heldItemSectionId, ClientItemStack[] heldItemContainer, int heldItemSlot, ClientItemStack heldItem)
	{
		Entity = runningForEntity;
		HeldItemSlot = heldItemSlot;
		HeldItem = heldItem;
		HeldItemContainer = heldItemContainer;
		HeldItemSectionId = heldItemSectionId;
		OriginalItemType = heldItem?.Id;
	}

	public InteractionChain Fork(InteractionContext context, int rootInteractionId)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return Fork(Chain.Type, context, rootInteractionId);
	}

	public InteractionChain Fork(InteractionType type, InteractionContext context, int rootInteractionId)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return Fork(type, context, rootInteractionId, Entry.NextForkId(), matchServer: false);
	}

	public InteractionChain ForkPredicted(InteractionChainData data, InteractionType type, InteractionContext context, int rootInteractionId)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return Fork(data, type, context, rootInteractionId, Entry.NextPredictedForkId(), matchServer: true);
	}

	public InteractionChain ForkPredicted(InteractionType type, InteractionContext context, int rootInteractionId)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return Fork(type, context, rootInteractionId, Entry.NextPredictedForkId(), matchServer: true);
	}

	private InteractionChain Fork(InteractionType type, InteractionContext context, int rootInteractionId, int subIndex, bool matchServer)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		InteractionChainData data = new InteractionChainData(Chain.ChainData);
		return Fork(data, type, context, rootInteractionId, subIndex, matchServer);
	}

	private InteractionChain Fork(InteractionChainData data, InteractionType type, InteractionContext context, int rootInteractionId, int subIndex, bool matchServer)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Expected O, but got Unknown
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Expected O, but got Unknown
		//IL_0266: Expected O, but got Unknown
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		if (!context.MetaStore.TargetSlot.HasValue)
		{
			context.MetaStore.TargetSlot = MetaStore.TargetSlot;
		}
		if (context.MetaStore.TargetSlot.HasValue)
		{
			data.TargetSlot = context.MetaStore.TargetSlot.Value;
		}
		if (context.MetaStore.HitLocation.HasValue)
		{
			Vector4 value = context.MetaStore.HitLocation.Value;
			data.HitLocation = new Vector3f(value.X, value.Y, value.Z);
		}
		if (context.MetaStore.HitDetail != null)
		{
			data.HitDetail = context.MetaStore.HitDetail;
		}
		if (context.MetaStore.TargetBlock != null)
		{
			data.BlockPosition_ = context.MetaStore.TargetBlock;
		}
		if (context.MetaStore.TargetEntity != null)
		{
			data.EntityId = context.MetaStore.TargetEntity.NetworkId;
		}
		ClientRootInteraction.Operation operation = GameInstance.InteractionModule.RootInteractions[Entry.State.RootInteraction].Operations[Entry.State.OperationCounter];
		if (matchServer && operation is ClientRootInteraction.InteractionWrapper interactionWrapper)
		{
			ClientInteraction interaction = interactionWrapper.GetInteraction(GameInstance.InteractionModule);
			foreach (KeyValuePair<ulong, InteractionChain> forkedChain in Chain.ForkedChains)
			{
				InteractionChain value2 = forkedChain.Value;
				if (value2.BaseForkedChainId == null)
				{
					continue;
				}
				int entryIndex = value2.BaseForkedChainId.EntryIndex;
				if (entryIndex == Entry.Index)
				{
					InteractionChain interactionChain = interaction.MapForkChain(this, data);
					if (interactionChain != null)
					{
						return interactionChain;
					}
				}
			}
		}
		int chainId = Chain.ChainId;
		ForkedChainId forkedChainId = Chain.ForkedChainId;
		ForkedChainId val = new ForkedChainId(Entry.Index, subIndex, (ForkedChainId)null);
		if (forkedChainId != null)
		{
			ForkedChainId val2 = new ForkedChainId(forkedChainId);
			forkedChainId = val2;
			ForkedChainId val3 = val2;
			while (val3.ForkedId != null)
			{
				val3 = val3.ForkedId;
			}
			val3.ForkedId = val;
		}
		else
		{
			forkedChainId = val;
		}
		ClientRootInteraction rootInteraction = GameInstance.InteractionModule.RootInteractions[rootInteractionId];
		int hotbarActiveSlot = GameInstance.InventoryModule.HotbarActiveSlot;
		ClientItemStack activeHotbarItem = GameInstance.InventoryModule.GetActiveHotbarItem();
		InteractionChain interactionChain2 = new InteractionChain(forkedChainId, val, type, context, data, rootInteraction, hotbarActiveSlot, activeHotbarItem, null);
		interactionChain2.Time.Start();
		interactionChain2.ChainId = chainId;
		interactionChain2.Predicted = true;
		interactionChain2.SkipChainOnClick = AllowSkipChainOnClick;
		if (Chain.RemoveTempForkedChain(val, out var ret))
		{
			interactionChain2.CopyTempFrom(ret);
		}
		Chain.PutForkedChain(val, interactionChain2);
		return interactionChain2;
	}

	public InteractionContext Duplicate()
	{
		InteractionContext interactionContext = new InteractionContext(Entity, HeldItemSectionId, HeldItemContainer, HeldItemSlot, HeldItem);
		interactionContext.MetaStore.CopyFrom(MetaStore);
		return interactionContext;
	}

	public void Jump(ClientRootInteraction.Label label)
	{
		Chain.OperationCounter = label.Index;
	}

	public void Execute(ClientRootInteraction nextInteraction)
	{
		Chain.PushRoot(nextInteraction);
	}

	public void SetTimeShift(float shift)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		Chain.TimeShift = shift;
		if (Chain.ForkedChainId == null)
		{
			GameInstance.InteractionModule.SetGlobalTimeShift(Chain.Type, shift);
		}
	}

	internal void InitEntry(InteractionChain chain, InteractionEntry entry, GameInstance gameInstance)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		Chain = chain;
		Entry = entry;
		GameInstance = gameInstance;
		State = entry.State;
		ServerData = entry.ServerState;
		InstanceStore = entry.InteractionMetaStore;
		Labels = null;
		Chain.SkipChainOnClick |= Chain.RootInteraction.RootInteraction.Settings.TryGetValue(GameInstance.GameMode, out var value) && value.AllowSkipChainOnClick;
	}

	internal void DeinitEntry(InteractionChain chain, InteractionEntry entry, GameInstance gameInstance)
	{
		State = null;
		ServerData = null;
		InstanceStore = null;
		Chain = null;
		Entry = null;
		GameInstance = null;
		Labels = null;
	}

	internal bool GetRootInteractionId(GameInstance gameInstance, InteractionType type, out int id)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		if (Entity is InteractionSource interactionSource && interactionSource.TryGetInteractionId(type, out id))
		{
			return true;
		}
		id = int.MinValue;
		if (OriginalItemType == null)
		{
			return gameInstance.ServerSettings.UnarmedInteractions.TryGetValue(type, out id);
		}
		return (gameInstance.ItemLibraryModule.GetItem(OriginalItemType)?.Interactions?.TryGetValue(type, out id)).GetValueOrDefault();
	}

	public static InteractionContext ForProxy(Entity runningForEntity, InventoryModule inventoryModule, InteractionType type)
	{
		return new InteractionContext(runningForEntity, InventorySectionType.Hotbar, inventoryModule.HotbarInventory, inventoryModule.HotbarActiveSlot, inventoryModule.GetHotbarItem(inventoryModule.HotbarActiveSlot));
	}

	public static InteractionContext ForInteraction(GameInstance gameInstance, InventoryModule inventoryModule, InteractionType type, int? equipSlot = null)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected I4, but got Unknown
		switch ((int)type)
		{
		default:
			switch (type - 22)
			{
			case 2:
				if (!equipSlot.HasValue)
				{
					throw new ArgumentException("Equipped interaction type requires a slot set");
				}
				return new InteractionContext(gameInstance.LocalPlayer, InventorySectionType.Armor, inventoryModule._armorInventory, equipSlot.Value, inventoryModule.GetArmorItem(equipSlot.Value));
			case 0:
				return new InteractionContext(gameInstance.LocalPlayer, InventorySectionType.Hotbar, inventoryModule.HotbarInventory, inventoryModule.HotbarActiveSlot, inventoryModule.GetHotbarItem(inventoryModule.HotbarActiveSlot));
			case 1:
				return new InteractionContext(gameInstance.LocalPlayer, InventorySectionType.Utility, inventoryModule.UtilityInventory, inventoryModule.UtilityActiveSlot, inventoryModule.GetUtilityItem(inventoryModule.UtilityActiveSlot));
			}
			break;
		case 6:
			return new InteractionContext(gameInstance.LocalPlayer, InventorySectionType.Consumable, inventoryModule.ConsumableInventory, inventoryModule.ConsumableActiveSlot, inventoryModule.GetConsumableItem(inventoryModule.ConsumableActiveSlot));
		case 0:
		case 2:
		case 3:
		case 4:
		case 5:
		case 8:
			if (inventoryModule.UsingToolsItem())
			{
				return new InteractionContext(gameInstance.LocalPlayer, InventorySectionType.Tools, inventoryModule.ToolsInventory, inventoryModule.ToolsActiveSlot, inventoryModule.GetToolsItem(inventoryModule.ToolsActiveSlot));
			}
			if (inventoryModule.GetHotbarItem(inventoryModule.HotbarActiveSlot) == null && inventoryModule.GetUtilityItem(inventoryModule.UtilityActiveSlot) != null)
			{
				return new InteractionContext(gameInstance.LocalPlayer, InventorySectionType.Utility, inventoryModule.UtilityInventory, inventoryModule.UtilityActiveSlot, inventoryModule.GetUtilityItem(inventoryModule.UtilityActiveSlot));
			}
			return new InteractionContext(gameInstance.LocalPlayer, InventorySectionType.Hotbar, inventoryModule.HotbarInventory, inventoryModule.HotbarActiveSlot, inventoryModule.GetHotbarItem(inventoryModule.HotbarActiveSlot));
		case 1:
		{
			if (inventoryModule.UsingToolsItem())
			{
				return new InteractionContext(gameInstance.LocalPlayer, InventorySectionType.Tools, inventoryModule.ToolsInventory, inventoryModule.ToolsActiveSlot, inventoryModule.GetToolsItem(inventoryModule.ToolsActiveSlot));
			}
			ClientItemStack hotbarItem = inventoryModule.GetHotbarItem(inventoryModule.HotbarActiveSlot);
			ClientItemStack utilityItem = inventoryModule.GetUtilityItem(inventoryModule.UtilityActiveSlot);
			if (hotbarItem != null)
			{
				if ((gameInstance.ItemLibraryModule.GetItem(hotbarItem.Id)?.Utility?.Compatible).GetValueOrDefault() && utilityItem != null)
				{
					return new InteractionContext(gameInstance.LocalPlayer, InventorySectionType.Utility, inventoryModule.UtilityInventory, inventoryModule.UtilityActiveSlot, inventoryModule.GetUtilityItem(inventoryModule.UtilityActiveSlot));
				}
			}
			else if (utilityItem != null)
			{
				return new InteractionContext(gameInstance.LocalPlayer, InventorySectionType.Utility, inventoryModule.UtilityInventory, inventoryModule.UtilityActiveSlot, inventoryModule.GetUtilityItem(inventoryModule.UtilityActiveSlot));
			}
			break;
		}
		case 7:
			break;
		}
		return new InteractionContext(gameInstance.LocalPlayer, InventorySectionType.Hotbar, inventoryModule.HotbarInventory, inventoryModule.HotbarActiveSlot, inventoryModule.GetHotbarItem(inventoryModule.HotbarActiveSlot));
	}

	public static InteractionContext ForInteraction(Entity entity, InteractionType type)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected I4, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Invalid comparison between Unknown and I4
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Invalid comparison between Unknown and I4
		ClientItemBase clientItemBase = entity.PrimaryItem;
		switch ((int)type)
		{
		default:
			if ((int)type != 22 && (int)type == 23)
			{
				clientItemBase = entity.SecondaryItem;
			}
			break;
		case 6:
			clientItemBase = entity.ConsumableItem ?? clientItemBase;
			break;
		case 1:
			clientItemBase = entity.SecondaryItem ?? clientItemBase;
			break;
		case 0:
		case 2:
		case 3:
		case 4:
		case 5:
			clientItemBase = clientItemBase ?? entity.SecondaryItem;
			break;
		}
		ClientItemStack heldItem = ((clientItemBase != null) ? new ClientItemStack(clientItemBase.Id) : null);
		return new InteractionContext(entity, InventorySectionType.Hotbar, null, 0, heldItem);
	}
}
