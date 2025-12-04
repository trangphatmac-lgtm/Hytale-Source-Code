#define DEBUG
using System.Collections.Generic;
using System.Diagnostics;
using Hypixel.ProtoPlus;
using HytaleClient.Data.EntityStats;
using HytaleClient.Data.Items;
using HytaleClient.Data.UserSettings;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using SDL2;

namespace HytaleClient.InGame.Modules;

internal class InventoryModule : Module
{
	public const float TimeToDropStack = 0.5f;

	public const int InactiveSlotIndex = -1;

	private ClientItemStack[] _storageInventory;

	public ClientItemStack[] _armorInventory;

	public ClientItemStack[] HotbarInventory;

	public ClientItemStack[] UtilityInventory;

	public ClientItemStack[] ConsumableInventory;

	public ClientItemStack[] ToolsInventory;

	public int HotbarActiveSlot = -1;

	private float _dropBindingHeldTick = 0f;

	private bool _hasDroppedStack = false;

	private bool _usingToolsItem = false;

	public int UtilityActiveSlot { get; private set; } = -1;


	public int ConsumableActiveSlot { get; private set; } = -1;


	public int ToolsActiveSlot { get; private set; } = -1;


	public InventoryModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_gameInstance.App.Interface.RegisterForEvent("game.selectActiveUtilitySlot", _gameInstance, delegate(int slot)
		{
			SetActiveUtilitySlot(slot);
		});
		_gameInstance.App.Interface.RegisterForEvent("game.selectActiveConsumableSlot", _gameInstance, delegate(int slot)
		{
			SetActiveConsumableSlot(slot);
		});
		_gameInstance.App.Interface.RegisterForEvent("game.useConsumableSlot", _gameInstance, delegate(int slot)
		{
			SetActiveConsumableSlot(slot, sendPacket: true, doInteraction: true);
		});
	}

	protected override void DoDispose()
	{
		_gameInstance.App.Interface.UnregisterFromEvent("game.selectActiveUtilitySlot");
		_gameInstance.App.Interface.UnregisterFromEvent("game.selectActiveConsumableSlot");
		_gameInstance.App.Interface.UnregisterFromEvent("game.useConsumableSlot");
	}

	public void Update(float deltaTime)
	{
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Invalid comparison between Unknown and I4
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Invalid comparison between Unknown and I4
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Invalid comparison between Unknown and I4
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		if (_gameInstance.Input.IsBindingHeld(inputBindings.DropItem))
		{
			_dropBindingHeldTick += deltaTime;
			if (_dropBindingHeldTick >= 0.5f && !_hasDroppedStack && GetHotbarItem(HotbarActiveSlot) != null)
			{
				DropItem(dropWholeStack: true);
				_hasDroppedStack = true;
			}
		}
		else
		{
			if (_dropBindingHeldTick > 0f && !_hasDroppedStack && GetHotbarItem(HotbarActiveSlot) != null)
			{
				DropItem(dropWholeStack: false);
			}
			_hasDroppedStack = false;
			_dropBindingHeldTick = 0f;
		}
		if ((int)_gameInstance.GameMode == 1 && _gameInstance.Input.IsShiftHeld())
		{
			for (sbyte b = 0; b < 10; b++)
			{
				if (_gameInstance.Input.ConsumeKey((SDL_Scancode)(30 + b)))
				{
					_gameInstance.Connection.SendPacket((ProtoPacket)new LoadHotbar(b));
					_gameInstance.AudioModule.PlayLocalSoundEvent("UI_HOTBAR_UP");
					break;
				}
			}
		}
		if ((int)_gameInstance.GameMode == 1 && _gameInstance.Input.ConsumeBinding(_gameInstance.App.Settings.InputBindings.SelectBlockFromSet))
		{
			TrySelectBlockFromSet();
		}
		if (_gameInstance.Input.IsAltHeld() && _gameInstance.Input.ConsumeKey((SDL_Scancode)10))
		{
			_gameInstance.SetGameMode((GameMode)((int)_gameInstance.GameMode != 1), executeCommand: true);
		}
	}

	private void TrySelectBlockFromSet()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		_gameInstance.Connection.SendPacket((ProtoPacket)new SwitchHotbarBlockSet());
	}

	private void DropItem(bool dropWholeStack)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Expected O, but got Unknown
		if (_gameInstance.InteractionModule.ApplyRules(null, new InteractionChainData(), (InteractionType)0, null) && !UsingToolsItem() && _gameInstance.InteractionModule.ApplyRules(null, new InteractionChainData(), (InteractionType)0, null))
		{
			ClientItemStack hotbarItem = GetHotbarItem(HotbarActiveSlot);
			int num = ((!dropWholeStack) ? 1 : hotbarItem.Quantity);
			Item val = new Item(hotbarItem.Id, num, hotbarItem.Durability, hotbarItem.MaxDurability, false, (sbyte[])(object)ProtoHelper.SerializeBson(hotbarItem.Metadata));
			_gameInstance.Connection.SendPacket((ProtoPacket)new DropItemStack(new InventoryPosition(-1, HotbarActiveSlot, val)));
			HotbarInventory[HotbarActiveSlot].Quantity -= num;
			if (HotbarInventory[HotbarActiveSlot].Quantity == 0)
			{
				HotbarInventory[HotbarActiveSlot] = null;
				ChangeCharacterItem(null, GetUtilityItem(UtilityActiveSlot)?.Id, ItemChangeType.Dropped);
			}
			UpdateAll();
		}
	}

	public void SetInventory(UpdatePlayerInventory inventory)
	{
		//IL_0405: Unknown result type (might be due to invalid IL or missing references)
		Debug.Assert(ThreadHelper.IsMainThread());
		if (inventory.Storage != null)
		{
			_storageInventory = new ClientItemStack[inventory.Storage.Capacity];
			foreach (KeyValuePair<int, Item> item in inventory.Storage.Items)
			{
				_storageInventory[item.Key] = new ClientItemStack(item.Value);
			}
		}
		if (inventory.Armor != null)
		{
			_armorInventory = new ClientItemStack[inventory.Armor.Capacity];
			string[] array = new string[inventory.Armor.Capacity];
			foreach (KeyValuePair<int, Item> item2 in inventory.Armor.Items)
			{
				_armorInventory[item2.Key] = new ClientItemStack(item2.Value);
				array[item2.Key] = item2.Value.ItemId;
			}
			_gameInstance.LocalPlayer?.SetCharacterModel(null, array);
		}
		if (inventory.Hotbar != null)
		{
			HotbarInventory = new ClientItemStack[inventory.Hotbar.Capacity];
			foreach (KeyValuePair<int, Item> item3 in inventory.Hotbar.Items)
			{
				HotbarInventory[item3.Key] = new ClientItemStack(item3.Value);
			}
			if (HotbarActiveSlot >= inventory.Hotbar.Capacity)
			{
				SetActiveHotbarSlot(inventory.Hotbar.Capacity - 1);
			}
		}
		if (inventory.Utility != null)
		{
			UtilityInventory = new ClientItemStack[inventory.Utility.Capacity];
			foreach (KeyValuePair<int, Item> item4 in inventory.Utility.Items)
			{
				UtilityInventory[item4.Key] = new ClientItemStack(item4.Value);
			}
		}
		if (inventory.Consumable != null)
		{
			ConsumableInventory = new ClientItemStack[inventory.Consumable.Capacity];
			foreach (KeyValuePair<int, Item> item5 in inventory.Consumable.Items)
			{
				ConsumableInventory[item5.Key] = new ClientItemStack(item5.Value);
			}
		}
		if (inventory.Tools != null)
		{
			ToolsInventory = new ClientItemStack[inventory.Tools.Capacity];
			foreach (KeyValuePair<int, Item> item6 in inventory.Tools.Items)
			{
				ToolsInventory[item6.Key] = new ClientItemStack(item6.Value);
			}
		}
		string newItemId = GetActiveItem()?.Id;
		string newSecondaryItemId = GetUtilityItem(UtilityActiveSlot)?.Id;
		ChangeCharacterItem(newItemId, newSecondaryItemId);
		UpdateAll();
		_gameInstance.App.Interface.InGameView.StatusEffectsHudComponent?.UpdateTrinketsBuffs();
		_gameInstance.App.Interface.InGameView.AbilitiesHudComponent?.ShowOrHideHud();
		_gameInstance.App.Interface.TriggerEvent("inventory.setAutosortType", inventory.SortType_);
	}

	public void UpdateAll()
	{
		_gameInstance.App.Interface.TriggerEvent("inventory.setAll", _storageInventory, _armorInventory, HotbarInventory, UtilityInventory, ConsumableInventory, ToolsInventory);
		_gameInstance.App.Interface.InGameView.AbilitiesHudComponent.OnSignatureEnergyStatChanged(_gameInstance.LocalPlayer.GetEntityStat(DefaultEntityStats.SignatureEnergy));
	}

	public ClientItemStack GetStorageItem(int slot)
	{
		ClientItemStack[] storageInventory = _storageInventory;
		return (storageInventory != null) ? storageInventory[slot] : null;
	}

	public ClientItemStack GetArmorItem(int slot)
	{
		ClientItemStack[] armorInventory = _armorInventory;
		return (armorInventory != null) ? armorInventory[slot] : null;
	}

	public int GetActiveInventorySectionType()
	{
		return UsingToolsItem() ? (-8) : (-1);
	}

	public int GetActiveSlot()
	{
		return UsingToolsItem() ? ToolsActiveSlot : HotbarActiveSlot;
	}

	public ClientItemStack GetActiveItem()
	{
		if (UsingToolsItem())
		{
			return GetActiveToolsItem();
		}
		return GetActiveHotbarItem();
	}

	public int GetActiveHotbarSlot()
	{
		return HotbarActiveSlot;
	}

	public ClientItemStack GetActiveHotbarItem()
	{
		return GetHotbarItem(HotbarActiveSlot);
	}

	public ClientItemStack GetHotbarItem(int slot)
	{
		if (HotbarInventory == null || slot == -1)
		{
			return null;
		}
		return HotbarInventory[slot];
	}

	public ClientItemStack GetUtilityItem(int slot)
	{
		if (UtilityInventory == null || slot == -1)
		{
			return null;
		}
		return UtilityInventory[slot];
	}

	public ClientItemStack GetConsumableItem(int slot)
	{
		if (ConsumableInventory == null || slot == -1)
		{
			return null;
		}
		return ConsumableInventory[slot];
	}

	public ClientItemStack[] GetToolItemStacks()
	{
		return ToolsInventory;
	}

	public ClientItemStack GetToolItemStack(int id)
	{
		return ToolsInventory[id];
	}

	public bool UsingToolsItem()
	{
		return _usingToolsItem;
	}

	public void SetUsingToolsItem(bool value)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		BuilderToolAction val = (BuilderToolAction)(value ? 5 : 6);
		_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolGeneralAction(val));
		_usingToolsItem = value;
	}

	public int GetActiveToolsSlot()
	{
		return ToolsActiveSlot;
	}

	public ClientItemStack GetActiveToolsItem()
	{
		return GetToolsItem(ToolsActiveSlot);
	}

	public ClientItemStack GetToolsItem(int slot)
	{
		if (ToolsInventory == null || slot == -1)
		{
			return null;
		}
		return ToolsInventory[slot];
	}

	public void SetHotbarItem(int slot, ClientItemStack itemStack)
	{
		if (HotbarInventory != null)
		{
			HotbarInventory[slot] = itemStack;
			UpdateAll();
		}
	}

	public int GetHotbarCapacity()
	{
		return HotbarInventory.Length;
	}

	public int GetStorageCapacity()
	{
		return _storageInventory.Length;
	}

	public void ScrollHotbarSlot(bool positive)
	{
		if (HotbarInventory != null && !_usingToolsItem)
		{
			int num = HotbarActiveSlot + (positive ? 1 : (-1));
			if (num < 0)
			{
				num = HotbarInventory.Length - 1;
			}
			else if (num >= HotbarInventory.Length)
			{
				num = 0;
			}
			SetActiveHotbarSlot(num);
		}
	}

	public void SetActiveHotbarSlot(int slot, bool triggerInteraction = true)
	{
		if (HotbarInventory == null || slot >= HotbarInventory.Length)
		{
			return;
		}
		if (triggerInteraction)
		{
			_gameInstance.InteractionModule.ConsumeInteractionType(null, (InteractionType)15, slot);
			return;
		}
		if (_usingToolsItem)
		{
			SetUsingToolsItem(value: false);
		}
		else if (slot == HotbarActiveSlot)
		{
			return;
		}
		HotbarActiveSlot = slot;
		_gameInstance.App.Interface.TriggerEvent("game.setActiveHotbarSlot", HotbarActiveSlot);
		if (_gameInstance.LocalPlayer != null)
		{
			ChangeCharacterItem(GetHotbarItem(HotbarActiveSlot)?.Id, GetUtilityItem(UtilityActiveSlot)?.Id, ItemChangeType.SlotChanged);
			_gameInstance.LocalPlayer.ClearFirstPersonItemWiggle();
			_gameInstance.LocalPlayer.FinishAction();
		}
	}

	public void SetActiveUtilitySlot(int slot, bool sendPacket = true)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		if (slot != UtilityActiveSlot && (UtilityInventory == null || slot < UtilityInventory.Length))
		{
			UtilityActiveSlot = slot;
			_gameInstance.App.Interface.TriggerEvent("game.setActiveUtilitySlot", UtilityActiveSlot);
			if (sendPacket)
			{
				_gameInstance.Connection.SendPacket((ProtoPacket)new SetActiveSlot(-5, UtilityActiveSlot));
			}
			_gameInstance.App.Interface.InGameView.AbilitiesHudComponent?.ShowOrHideHud();
			if (_gameInstance.LocalPlayer != null)
			{
				ChangeCharacterItem(GetHotbarItem(HotbarActiveSlot)?.Id, GetUtilityItem(UtilityActiveSlot)?.Id, ItemChangeType.SlotChanged);
				_gameInstance.LocalPlayer.ClearFirstPersonItemWiggle();
				_gameInstance.LocalPlayer.FinishAction();
			}
		}
	}

	public void SetActiveConsumableSlot(int slot, bool sendPacket = true, bool doInteraction = false)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		if (ConsumableInventory != null && slot >= ConsumableInventory.Length)
		{
			return;
		}
		if (slot != ConsumableActiveSlot)
		{
			ConsumableActiveSlot = slot;
			_gameInstance.App.Interface.TriggerEvent("game.setActiveConsumableSlot", ConsumableActiveSlot);
			if (sendPacket)
			{
				_gameInstance.Connection.SendPacket((ProtoPacket)new SetActiveSlot(-6, ConsumableActiveSlot));
			}
		}
		if (doInteraction)
		{
			ClientItemStack consumableItem = GetConsumableItem(ConsumableActiveSlot);
			_gameInstance.LocalPlayer.ConsumableItem = _gameInstance.ItemLibraryModule.GetItem(consumableItem?.Id);
			_gameInstance.LocalPlayer.SetCharacterItemConsumable();
			if (!_gameInstance.InteractionModule.StartChain((InteractionType)6, InteractionModule.ClickType.Single, OnCompletion))
			{
				_gameInstance.LocalPlayer.RestoreCharacterItem();
			}
		}
		void OnCompletion()
		{
			_gameInstance.LocalPlayer.RestoreCharacterItem();
		}
	}

	public void SetActiveToolsSlot(int slot, bool sendPacket = true, bool useTool = true)
	{
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		if (ToolsInventory == null)
		{
			return;
		}
		if (!_usingToolsItem)
		{
			if (useTool)
			{
				SetUsingToolsItem(value: true);
			}
		}
		else if (slot == ToolsActiveSlot)
		{
			return;
		}
		if (slot >= ToolsInventory.Length)
		{
			return;
		}
		if (useTool)
		{
			_gameInstance.App.Interface.InGameView.ClearSlotHighlight();
			_gameInstance.App.Interface.TriggerEvent("game.setActiveHotbarSlot", -1);
			_gameInstance.App.InGame.Instance.BuilderToolsModule.ClearConfiguringTool();
		}
		if (ToolsActiveSlot != slot)
		{
			ToolsActiveSlot = slot;
			if (sendPacket)
			{
				_gameInstance.Connection.SendPacket((ProtoPacket)new SetActiveSlot(-8, ToolsActiveSlot));
			}
		}
		_gameInstance.App.Interface.TriggerEvent("game.setActiveToolsSlot", ToolsActiveSlot);
		ClientItemStack toolsItem = GetToolsItem(ToolsActiveSlot);
		_gameInstance.BuilderToolsModule.TrySelectActiveTool(-8, slot, toolsItem);
		if (_gameInstance.LocalPlayer != null)
		{
			ChangeCharacterItem(toolsItem?.Id, GetUtilityItem(UtilityActiveSlot)?.Id, ItemChangeType.SlotChanged);
			_gameInstance.LocalPlayer.ClearFirstPersonItemWiggle();
			_gameInstance.LocalPlayer.FinishAction();
		}
	}

	private void ChangeCharacterItem(string newItemId, string newSecondaryItemId = null, ItemChangeType changeType = ItemChangeType.Other)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Invalid comparison between Unknown and I4
		if (_gameInstance.LocalPlayer != null)
		{
			_gameInstance.LocalPlayer.ChangeCharacterItem(newItemId, newSecondaryItemId);
			_gameInstance.LocalPlayer.UpdateLight();
			if (!_gameInstance.BuilderToolsModule.TrySelectActiveTool() || (int)_gameInstance.GameMode != 1)
			{
				_gameInstance.App.Interface.InGameView.ClearSlotHighlight();
			}
			_gameInstance.App.Interface.InGameView.ToolsSettingsPage.OnPlayerCharacterItemChanged(changeType);
			_gameInstance.App.Interface.InGameView.OnPlayerCharacterItemChanged(changeType);
		}
	}

	public void AddAndSelectHotbarItem(string itemId)
	{
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Invalid comparison between Unknown and I4
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Expected O, but got Unknown
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Expected O, but got Unknown
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Expected O, but got Unknown
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Expected O, but got Unknown
		//IL_00e5: Expected O, but got Unknown
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Expected O, but got Unknown
		int num = -1;
		for (int i = 0; i < HotbarInventory.Length; i++)
		{
			ClientItemStack hotbarItem = GetHotbarItem(i);
			if (hotbarItem == null)
			{
				if (num == -1)
				{
					num = i;
				}
			}
			else if (hotbarItem.Id == itemId)
			{
				SetActiveHotbarSlot(i);
				return;
			}
		}
		if (num != -1)
		{
			SetActiveHotbarSlot(num);
		}
		else
		{
			num = HotbarActiveSlot;
		}
		for (int j = 0; j < _storageInventory.Length; j++)
		{
			ClientItemStack storageItem = GetStorageItem(j);
			if (storageItem != null && storageItem.Id == itemId)
			{
				_gameInstance.Connection.SendPacket((ProtoPacket)new MoveItemStack(new InventoryPosition(-2, j, storageItem.ToItemPacket(includeMetadata: true)), new InventoryPosition(-1, num, GetHotbarItem(num)?.ToItemPacket(includeMetadata: true))));
				return;
			}
		}
		if ((int)_gameInstance.GameMode == 1)
		{
			ClientItemBase item = _gameInstance.ItemLibraryModule.GetItem(itemId);
			if (item != null)
			{
				double durability = item.Durability;
				_gameInstance.Connection.SendPacket((ProtoPacket)new SetCreativeItem(new InventoryPosition(-1, num, new Item(itemId, 1, durability, durability, false, (sbyte[])null)), false));
			}
		}
	}
}
