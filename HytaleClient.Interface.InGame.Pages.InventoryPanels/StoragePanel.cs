using HytaleClient.Data.Items;
using HytaleClient.InGame.Modules;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class StoragePanel : Panel
{
	private ItemGrid _itemGrid;

	private TabNavigation _filter;

	private AutosortTypeDropdown _autosortTypeDropdown;

	private int _highlightedSlot = -1;

	private PatchStyle _highlightedSlotOverlay;

	private SortType _autosortType;

	public string ActiveItemFilter => _filter.SelectedTab;

	public int Offset { get; private set; }

	public StoragePanel(InGameView inGameView, Element parent = null)
		: base(inGameView, parent)
	{
	}

	public void Build()
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		Clear();
		_highlightedSlotOverlay = new PatchStyle("InGame/Pages/Inventory/SlotHighlight.png");
		Interface.TryGetDocument("InGame/Pages/Inventory/StoragePanel.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		Offset = document.ResolveNamedValue<int>(Interface, "Offset");
		Group parent = uIFragment.Get<Group>("AutosortTypeDropdownContainer");
		_autosortTypeDropdown = new AutosortTypeDropdown(Desktop, parent)
		{
			SortType = _autosortType,
			SortTypeChanged = SortItems
		};
		_autosortTypeDropdown.Build();
		_filter = uIFragment.Get<TabNavigation>("Filter");
		_filter.SelectedTabChanged = delegate
		{
			UpdateGrid();
			_inGameView.HotbarComponent.SetupGrid();
			if (_inGameView.InventoryPage.ContainerPanel.IsMounted)
			{
				_inGameView.InventoryPage.ContainerPanel.SetupGrid();
			}
		};
		uIFragment.Get<TextButton>("AutosortButton").Activating = SortItems;
		_itemGrid = uIFragment.Get<ItemGrid>("ItemGrid");
		_itemGrid.Slots = new ItemGridSlot[4 * _inGameView.DefaultItemSlotsPerRow];
		_itemGrid.InventorySectionId = -2;
		_itemGrid.SlotClicking = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryClick(-2, slotIndex, button);
		};
		_itemGrid.SlotDoubleClicking = delegate(int slotIndex)
		{
			_inGameView.HandleInventoryDoubleClick(-2, slotIndex);
		};
		_itemGrid.Dropped = delegate(int targetSlotIndex, Element sourceItemGrid, ItemGrid.ItemDragData dragData)
		{
			_inGameView.HandleInventoryDragEnd(_itemGrid, -2, targetSlotIndex, sourceItemGrid, dragData);
		};
		_itemGrid.DragCancelled = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryDropItem(-2, slotIndex, button);
		};
		_itemGrid.SlotMouseEntered = delegate(int slotIndex)
		{
			_inGameView.HandleItemSlotMouseEntered(-2, slotIndex);
		};
		_itemGrid.SlotMouseExited = delegate(int slotIndex)
		{
			_inGameView.HandleItemSlotMouseExited(-2, slotIndex);
		};
		if (_inGameView.StorageStacks != null)
		{
			UpdateGrid();
		}
	}

	protected override void OnUnmounted()
	{
		_filter.SelectedTab = null;
	}

	protected override void OnMounted()
	{
		UpdateGrid();
	}

	public void SetSortType(SortType type)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		_autosortType = type;
	}

	private void SortItems()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		_inGameView.InGame.SendSortInventoryPacket(_autosortTypeDropdown.SortType);
	}

	public void HighlightSlot(int slot)
	{
		if (_highlightedSlot != slot && _itemGrid.Slots[slot] != null)
		{
			ApplySlotHighlight(slot);
			if (_itemGrid.IsMounted)
			{
				_itemGrid.Layout();
			}
			_highlightedSlot = slot;
		}
	}

	private void ApplySlotHighlight(int slot)
	{
		_itemGrid.Slots[slot].Overlay = _highlightedSlotOverlay;
	}

	public void ClearSlotHighlight()
	{
		if (_highlightedSlot != -1 && _itemGrid.Slots[_highlightedSlot] != null)
		{
			_itemGrid.Slots[_highlightedSlot].Overlay = null;
			if (_itemGrid.IsMounted)
			{
				_itemGrid.Layout();
			}
			_highlightedSlot = -1;
		}
	}

	public void UpdateGrid()
	{
		_itemGrid.Slots = new ItemGridSlot[_inGameView.StorageStacks.Length];
		for (int i = 0; i < _inGameView.StorageStacks.Length; i++)
		{
			ClientItemStack clientItemStack = _inGameView.StorageStacks[i];
			bool flag = false;
			if (ActiveItemFilter != null)
			{
				ClientItemBase value;
				if (clientItemStack == null)
				{
					flag = true;
				}
				else if (_inGameView.Items.TryGetValue(clientItemStack.Id, out value))
				{
					switch (ActiveItemFilter)
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
			else if (!Desktop.IsMouseDragging)
			{
				if (clientItemStack == null || !_inGameView.Items.TryGetValue(clientItemStack.Id, out var value2))
				{
					value2 = null;
				}
				flag = !_inGameView.IsItemValid(i, value2, InventorySectionType.Storage);
			}
			if (clientItemStack != null || flag)
			{
				_itemGrid.Slots[i] = new ItemGridSlot
				{
					ItemStack = clientItemStack,
					IsItemIncompatible = flag
				};
			}
		}
		if (_highlightedSlot >= 0 && _itemGrid.Slots[_highlightedSlot] != null)
		{
			ApplySlotHighlight(_highlightedSlot);
		}
		if (_itemGrid.IsMounted)
		{
			_itemGrid.Layout();
		}
	}
}
