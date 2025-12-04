using System.Collections.Generic;
using System.Linq;
using HytaleClient.Application;
using HytaleClient.Data.Items;
using HytaleClient.InGame.Modules;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame;

internal class HotbarComponent : InterfaceComponent
{
	public readonly InGameView InGameView;

	private ItemGrid _itemGrid;

	private ItemGrid _utilityItemGrid;

	private ItemGrid _consumableItemGrid;

	private Group _consumableSlotPointer;

	private Group _consumableSlotContainer;

	private Group _utilitySlotPointer;

	private Group _utilitySlotContainer;

	private Group _background;

	private Group _container;

	private Element _activeSlotOverlay;

	private Element _activeSlotSwitchableOverlay;

	private int _slotCount;

	private readonly List<Label> _hotkeyLabels = new List<Label>();

	private Label _activeItemNameLabel;

	private int _activeHotbarSlot;

	private string _activeHotbarItemId;

	private float _activeHotbarItemNameLabelTime;

	private int _highlightedHotbarSlot = -1;

	private PatchStyle _highlightedSlotOverlay;

	private int _activeUtilitySlot;

	private int _activeConsumableSlot;

	private int _inventoryOpenedContainerMargin;

	private int _inventoryClosedContainerMargin;

	public HotbarComponent(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
		InGameView = inGameView;
	}

	public void Build()
	{
		Clear();
		_hotkeyLabels.Clear();
		_slotCount = 0;
		_highlightedSlotOverlay = new PatchStyle("InGame/Pages/Inventory/SlotHighlight.png");
		Interface.TryGetDocument("InGame/Hud/Hotbar.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_inventoryClosedContainerMargin = document.ResolveNamedValue<int>(Desktop.Provider, "InventoryClosedContainerMargin");
		_inventoryOpenedContainerMargin = document.ResolveNamedValue<int>(Desktop.Provider, "InventoryOpenedContainerMargin");
		_container = uIFragment.Get<Group>("Container");
		_background = uIFragment.Get<Group>("Background");
		_itemGrid = uIFragment.Get<ItemGrid>("ItemGrid");
		_itemGrid.Slots = new ItemGridSlot[InGameView.DefaultItemSlotsPerRow];
		_itemGrid.InventorySectionId = -1;
		_itemGrid.SlotClicking = delegate(int slotIndex, int button)
		{
			InGameView.HandleInventoryClick(-1, slotIndex, button);
		};
		_itemGrid.SlotDoubleClicking = delegate(int slotIndex)
		{
			InGameView.HandleInventoryDoubleClick(-1, slotIndex);
		};
		_itemGrid.Dropped = delegate(int targetSlotIndex, Element sourceItemGrid, ItemGrid.ItemDragData dragData)
		{
			InGameView.HandleInventoryDragEnd(_itemGrid, -1, targetSlotIndex, sourceItemGrid, dragData);
		};
		_itemGrid.DragCancelled = delegate(int slotIndex, int button)
		{
			InGameView.HandleInventoryDropItem(-1, slotIndex, button);
		};
		_itemGrid.SlotMouseEntered = delegate(int slotIndex)
		{
			InGameView.HandleItemSlotMouseEntered(-1, slotIndex);
		};
		_itemGrid.SlotMouseExited = delegate(int slotIndex)
		{
			InGameView.HandleItemSlotMouseExited(-1, slotIndex);
		};
		_utilitySlotContainer = uIFragment.Get<Group>("UtilitySlotContainer");
		_utilitySlotPointer = uIFragment.Get<Group>("UtilitySlotPointer");
		_utilityItemGrid = uIFragment.Get<ItemGrid>("UtilitySlotItemGrid");
		_utilityItemGrid.InventorySectionId = -5;
		_utilityItemGrid.Slots = new ItemGridSlot[1];
		_consumableSlotContainer = uIFragment.Get<Group>("ConsumableSlotContainer");
		_consumableSlotPointer = uIFragment.Get<Group>("ConsumableSlotPointer");
		_consumableItemGrid = uIFragment.Get<ItemGrid>("ConsumableSlotItemGrid");
		_consumableItemGrid.InventorySectionId = -6;
		_consumableItemGrid.Slots = new ItemGridSlot[1];
		_activeSlotOverlay = uIFragment.Get<Group>("ActiveSlotOverlay");
		_activeSlotSwitchableOverlay = uIFragment.Get<Group>("ActiveSlotSwitchableOverlay");
		int num = _activeHotbarSlot * (_itemGrid.Style.SlotSize + _itemGrid.Style.SlotSpacing) + _itemGrid.Padding.Horizontal.GetValueOrDefault();
		int num2 = (int)(((float)_activeSlotOverlay.Anchor.Width.Value - (float)_itemGrid.Style.SlotSize) / 2f);
		_activeSlotOverlay.Anchor.Left = num - num2;
		_activeSlotSwitchableOverlay.Anchor.Left = num - num2;
		_activeItemNameLabel = uIFragment.Get<Label>("ActiveItemNameLabel");
		UpdateActiveHotbarSlotOverlay();
		if (InGameView.HotbarStacks != null)
		{
			SetupGrid();
		}
		if (base.IsMounted)
		{
			UpdateBackgroundVisibility();
			UpdateInputBindings();
		}
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
		UpdateInputBindings();
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	private void Animate(float deltaTime)
	{
		if (_activeHotbarItemNameLabelTime != 0f)
		{
			_activeHotbarItemNameLabelTime -= deltaTime;
			if (_activeHotbarItemNameLabelTime <= 0f)
			{
				_activeHotbarItemNameLabelTime = 0f;
				_activeItemNameLabel.Visible = false;
			}
		}
	}

	public void OnSetStacks()
	{
		SetupGrid();
		UpdateActiveItemNameLabel();
	}

	public void OnToggleItemSlotSelector()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		_itemGrid.InfoDisplay = (ItemGridInfoDisplayMode)((InGameView.InGame.ActiveItemSelector != 0) ? 2 : 0);
		_utilitySlotPointer.Visible = InGameView.InGame.ActiveItemSelector == AppInGame.ItemSelector.Utility;
		_utilitySlotPointer.Layout(_utilitySlotPointer.Parent.RectangleAfterPadding);
		_utilitySlotContainer.Find<Group>("Highlight").Visible = InGameView.InGame.ActiveItemSelector == AppInGame.ItemSelector.Utility;
		_utilitySlotContainer.Layout();
		_consumableSlotPointer.Visible = InGameView.InGame.ActiveItemSelector == AppInGame.ItemSelector.Consumable;
		_consumableSlotPointer.Layout(_consumableSlotContainer.Parent.RectangleAfterPadding);
		_consumableSlotContainer.Find<Group>("Highlight").Visible = InGameView.InGame.ActiveItemSelector == AppInGame.ItemSelector.Consumable;
		_consumableSlotContainer.Layout();
	}

	public void OnToggleInventoryOpen()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		bool flag = (int)InGameView.InGame.CurrentPage != 1 && (int)InGameView.InGame.CurrentPage != 2;
		Group utilitySlotContainer = _utilitySlotContainer;
		bool visible = (_consumableSlotContainer.Visible = flag);
		utilitySlotContainer.Visible = visible;
		if (_container.IsMounted)
		{
			_container.Layout();
		}
		if (_utilitySlotContainer.IsMounted)
		{
			_utilitySlotContainer.Layout(_utilitySlotContainer.RectangleAfterPadding);
			_consumableSlotContainer.Layout(_consumableSlotContainer.RectangleAfterPadding);
		}
		UpdateBackgroundVisibility();
	}

	private void UpdateInputBindings()
	{
		_utilitySlotContainer.Find<Label>("InputBinding").Text = InGameView.Interface.App.Settings.InputBindings.ShowUtilitySlotSelector.BoundInputLabel;
		if (_utilitySlotContainer.IsMounted)
		{
			_utilitySlotContainer.Layout();
		}
		_consumableSlotContainer.Find<Label>("InputBinding").Text = InGameView.Interface.App.Settings.InputBindings.ShowConsumableSlotSelector.BoundInputLabel;
		if (_consumableSlotContainer.IsMounted)
		{
			_consumableSlotContainer.Layout();
		}
	}

	public void UpdateBackgroundVisibility()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Invalid comparison between Unknown and I4
		_background.Visible = (int)InGameView.InGame.Instance.GameMode == 0 && (int)InGameView.InGame.CurrentPage != 1 && (int)InGameView.InGame.CurrentPage != 2;
		if (_background.IsMounted)
		{
			_background.Layout(_background.Parent.RectangleAfterPadding);
		}
	}

	public void OnPageChanged()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Invalid comparison between Unknown and I4
		_itemGrid.AreItemsDraggable = (int)Interface.App.InGame.CurrentPage == 2 || (int)Interface.App.InGame.CurrentPage == 1 || Interface.App.InGame.IsToolsSettingsModalOpened;
	}

	public void OnItemIconsUpdated()
	{
		if (InGameView.HotbarStacks != null)
		{
			SetupGrid();
		}
	}

	public void SetupGrid()
	{
		if (_slotCount != InGameView.HotbarStacks.Length)
		{
			foreach (Label hotkeyLabel in _hotkeyLabels)
			{
				hotkeyLabel.Parent.Remove(hotkeyLabel);
			}
			_hotkeyLabels.Clear();
			Interface.TryGetDocument("InGame/Hud/HotKey.ui", out var document);
			for (int i = 0; i < InGameView.HotbarStacks.Length; i++)
			{
				Label label = (Label)document.Instantiate(Desktop, _container).RootElements[0];
				label.Anchor.Left = i * (_itemGrid.Style.SlotSize + _itemGrid.Style.SlotSpacing) + 4;
				label.Text = ((i != 9) ? (i + 1) : 0).ToString();
				_hotkeyLabels.Add(label);
			}
			_slotCount = InGameView.HotbarStacks.Length;
			_container.Layout();
		}
		_itemGrid.Slots = new ItemGridSlot[InGameView.HotbarStacks.Length];
		for (int j = 0; j < InGameView.HotbarStacks.Length; j++)
		{
			ClientItemStack clientItemStack = InGameView.HotbarStacks[j];
			if (clientItemStack == null)
			{
				continue;
			}
			bool isItemIncompatible = false;
			if (InGameView.InventoryPage.StoragePanel.ActiveItemFilter != null)
			{
				if (InGameView.Items.TryGetValue(clientItemStack.Id, out var value))
				{
					switch (InGameView.InventoryPage.StoragePanel.ActiveItemFilter)
					{
					case "Weapon":
						if (value.Weapon == null)
						{
							isItemIncompatible = true;
						}
						break;
					case "Armor":
						if (value.Armor == null)
						{
							isItemIncompatible = true;
						}
						break;
					case "Material":
						if (value.BlockId == 0)
						{
							isItemIncompatible = true;
						}
						break;
					}
				}
				else
				{
					isItemIncompatible = true;
				}
			}
			else if (!Desktop.IsMouseDragging)
			{
				InGameView.Items.TryGetValue(clientItemStack.Id, out var value2);
				isItemIncompatible = !InGameView.IsItemValid(j, value2, InventorySectionType.Hotbar);
			}
			_itemGrid.Slots[j] = new ItemGridSlot(clientItemStack)
			{
				IsItemIncompatible = isItemIncompatible
			};
		}
		if (_highlightedHotbarSlot >= 0 && _itemGrid.Slots[_highlightedHotbarSlot] != null)
		{
			ApplySlotHighlight(_highlightedHotbarSlot);
		}
		_utilityItemGrid.Slots[0] = ((_activeUtilitySlot == -1) ? null : new ItemGridSlot(InGameView.UtilityStacks[_activeUtilitySlot]));
		_utilityItemGrid.Layout();
		_consumableItemGrid.Slots[0] = ((_activeConsumableSlot == -1) ? null : new ItemGridSlot(InGameView.ConsumableStacks[_activeConsumableSlot]));
		_consumableItemGrid.Layout();
		_itemGrid.Layout();
	}

	public void OnSetActiveHotbarSlot(int slot)
	{
		_activeHotbarSlot = slot;
		if (!Interface.HasMarkupError)
		{
			UpdateActiveHotbarSlotOverlay();
			if (_activeSlotOverlay.IsMounted)
			{
				_activeSlotOverlay.Layout(_activeSlotOverlay.Parent.AnchoredRectangle);
			}
			if (_activeSlotSwitchableOverlay.IsMounted)
			{
				_activeSlotSwitchableOverlay.Layout(_activeSlotSwitchableOverlay.Parent.AnchoredRectangle);
			}
		}
		UpdateActiveItemNameLabel();
	}

	private void UpdateActiveHotbarSlotOverlay()
	{
		bool flag = _activeHotbarSlot != -1;
		ClientItemStack clientItemStack = ((!flag) ? null : InGameView?.GetHotbarItem(_activeHotbarSlot));
		bool flag2 = clientItemStack != null && IsGroupedBlock(clientItemStack);
		if (flag)
		{
			_activeSlotOverlay.Visible = !flag2;
			_activeSlotSwitchableOverlay.Visible = flag2;
		}
		else
		{
			_activeSlotOverlay.Visible = false;
			_activeSlotSwitchableOverlay.Visible = false;
		}
		int num = _activeHotbarSlot * (_itemGrid.Style.SlotSize + _itemGrid.Style.SlotSpacing) + _itemGrid.Padding.Left.GetValueOrDefault();
		int num2 = (int)(((float)_activeSlotOverlay.Anchor.Width.Value - (float)_itemGrid.Style.SlotSize) / 2f);
		_activeSlotOverlay.Anchor.Left = num - num2;
		_activeSlotSwitchableOverlay.Anchor.Left = _activeSlotOverlay.Anchor.Left;
	}

	private bool IsGroupedBlock(ClientItemStack item)
	{
		string id = item.Id;
		return InGameView.InGame.Instance.ServerSettings.BlockGroups.Values.Any((BlockGroup group) => group.Names.Contains(id));
	}

	public void OnSetActiveUtilitySlot(int slot)
	{
		_activeUtilitySlot = slot;
		_utilityItemGrid.Slots[0] = ((_activeUtilitySlot == -1) ? null : new ItemGridSlot(InGameView.UtilityStacks[_activeUtilitySlot]));
		_utilityItemGrid.Layout();
	}

	public void OnSetActiveConsumableSlot(int slot)
	{
		_activeConsumableSlot = slot;
		_consumableItemGrid.Slots[0] = ((_activeConsumableSlot == -1) ? null : new ItemGridSlot(InGameView.ConsumableStacks[_activeConsumableSlot]));
		_consumableItemGrid.Layout();
	}

	public void HighlightSlot(int slot)
	{
		if (_highlightedHotbarSlot != slot && _itemGrid.Slots[slot] != null)
		{
			ApplySlotHighlight(slot);
			if (_itemGrid.IsMounted)
			{
				_itemGrid.Layout();
			}
			_highlightedHotbarSlot = slot;
		}
	}

	private void ApplySlotHighlight(int slot)
	{
		_itemGrid.Slots[slot].Overlay = _highlightedSlotOverlay;
	}

	public void ClearSlotHighlight()
	{
		if (_highlightedHotbarSlot != -1 && _itemGrid.Slots[_highlightedHotbarSlot] != null)
		{
			_itemGrid.Slots[_highlightedHotbarSlot].Overlay = null;
			if (_itemGrid.IsMounted)
			{
				_itemGrid.Layout();
			}
			_highlightedHotbarSlot = -1;
		}
	}

	public void UpdateActiveItemNameLabel()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Invalid comparison between Unknown and I4
		ClientItemStack hotbarItem = InGameView.GetHotbarItem(_activeHotbarSlot);
		if ((int)InGameView.InGame.CurrentPage == 2 || (int)InGameView.InGame.CurrentPage == 1)
		{
			_activeHotbarItemId = hotbarItem?.Id;
			_activeItemNameLabel.Visible = false;
			_activeHotbarItemNameLabelTime = 0f;
		}
		else if (hotbarItem?.Id != _activeHotbarItemId)
		{
			_activeHotbarItemId = hotbarItem?.Id;
			if (_activeHotbarItemId == null)
			{
				_activeItemNameLabel.Visible = false;
				_activeHotbarItemNameLabelTime = 0f;
				return;
			}
			_activeItemNameLabel.Text = Desktop.Provider.GetText("items." + _activeHotbarItemId + ".name");
			_activeItemNameLabel.Visible = true;
			_activeItemNameLabel.Layout(_activeItemNameLabel.Parent.RectangleAfterPadding);
			_activeHotbarItemNameLabelTime = 1.25f;
		}
	}

	public void ResetState()
	{
		_itemGrid.AreItemsDraggable = false;
		_itemGrid.Slots = new ItemGridSlot[InGameView.DefaultItemSlotsPerRow];
		_activeHotbarItemNameLabelTime = 0f;
		_activeItemNameLabel.Visible = false;
		_activeHotbarSlot = 0;
		UpdateActiveHotbarSlotOverlay();
		_activeUtilitySlot = -1;
		_utilityItemGrid.Slots[0] = null;
		_activeConsumableSlot = -1;
		_consumableItemGrid.Slots[0] = null;
		_highlightedHotbarSlot = -1;
	}
}
