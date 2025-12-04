using HytaleClient.Data.Items;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class ContainerPanel : WindowPanel
{
	private Label _titleLabel;

	private ItemGrid _itemGrid;

	private AutosortTypeDropdown _autosortTypeDropdown;

	private SortType _autosortType;

	public ContainerPanel(InGameView inGameView, Element parent = null)
		: base(inGameView, parent)
	{
	}

	public void Build()
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		Clear();
		Interface.TryGetDocument("InGame/Pages/Inventory/ContainerPanel.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		uIFragment.Get<TextButton>("AutosortButton").Activating = SortItems;
		Group parent = uIFragment.Get<Group>("AutosortTypeDropdownContainer");
		_autosortTypeDropdown = new AutosortTypeDropdown(Desktop, parent)
		{
			SortType = _autosortType,
			SortTypeChanged = SortItems
		};
		_autosortTypeDropdown.Build();
		_titleLabel = uIFragment.Get<Label>("TitleLabel");
		_itemGrid = uIFragment.Get<ItemGrid>("ItemGrid");
		_itemGrid.Slots = new ItemGridSlot[_inGameView.DefaultItemSlotsPerRow];
		_itemGrid.SlotClicking = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryClick(_inventoryWindow.Id, slotIndex, button);
		};
		_itemGrid.SlotDoubleClicking = delegate(int slotIndex)
		{
			_inGameView.HandleInventoryDoubleClick(_inventoryWindow.Id, slotIndex);
		};
		_itemGrid.Dropped = delegate(int targetSlotIndex, Element sourceItemGrid, ItemGrid.ItemDragData dragData)
		{
			_inGameView.HandleInventoryDragEnd(_itemGrid, _inventoryWindow.Id, targetSlotIndex, sourceItemGrid, dragData);
		};
		_itemGrid.DragCancelled = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryDropItem(_inventoryWindow.Id, slotIndex, button);
		};
		_itemGrid.SlotMouseEntered = delegate(int slotIndex)
		{
			_inGameView.HandleItemSlotMouseEntered(_inventoryWindow.Id, slotIndex);
		};
		_itemGrid.SlotMouseExited = delegate(int slotIndex)
		{
			_inGameView.HandleItemSlotMouseExited(_inventoryWindow.Id, slotIndex);
		};
		uIFragment.Get<TextButton>("TakeAllButton").Activating = delegate
		{
			_inGameView.InGame.SendTakeAllItemStacksPacket(_inventoryWindow.Id);
		};
	}

	protected override void Setup()
	{
		_titleLabel.Text = Desktop.Provider.GetText(((string)_inventoryWindow.WindowData["name"]) ?? "");
		_itemGrid.Slots = new ItemGridSlot[_inventoryWindow.Inventory.Length];
		_itemGrid.InventorySectionId = _inventoryWindow.Id;
		SetupGrid();
	}

	protected override void Update()
	{
		if (_inventoryWindow.Inventory != null)
		{
			SetupGrid();
		}
	}

	public void SetSortType(SortType type)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		_autosortType = type;
	}

	private void SortItems()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		JObject val = new JObject();
		val.Add("sortType", JToken.op_Implicit(((object)(SortType)(ref _autosortTypeDropdown.SortType)).ToString()));
		JObject val2 = val;
		_inGameView.InGame.SendSendWindowActionPacket(_inventoryWindow.Id, "sortItems", ((object)val2).ToString());
	}

	public void SetupGrid()
	{
		for (int i = 0; i < _inventoryWindow.Inventory.Length; i++)
		{
			ClientItemStack clientItemStack = _inventoryWindow.Inventory[i];
			string activeItemFilter = _inGameView.InventoryPage.StoragePanel.ActiveItemFilter;
			bool flag = false;
			if (activeItemFilter != null)
			{
				ClientItemBase value;
				if (clientItemStack == null)
				{
					flag = true;
				}
				else if (_inGameView.Items.TryGetValue(clientItemStack.Id, out value))
				{
					switch (activeItemFilter)
					{
					case "Weapon":
						if (value.Weapon == null)
						{
							flag = true;
						}
						break;
					case "Armor":
						if (value.Armor == null)
						{
							flag = true;
						}
						break;
					case "Material":
						if (value.BlockId == 0)
						{
							flag = true;
						}
						break;
					}
				}
				else
				{
					flag = true;
				}
			}
			if (clientItemStack == null && !flag)
			{
				_itemGrid.Slots[i] = null;
				continue;
			}
			_itemGrid.Slots[i] = new ItemGridSlot
			{
				ItemStack = clientItemStack,
				IsItemIncompatible = flag
			};
		}
		_itemGrid.Layout();
	}
}
