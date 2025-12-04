using System;
using HytaleClient.Application;
using HytaleClient.Data.EntityStats;
using HytaleClient.Data.Items;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class CharacterPanel : Panel
{
	private Label _nameLabel;

	private CharacterPreviewComponent _previewContainer;

	private ItemGrid _itemGridLeft;

	private ItemGrid _itemGridRight;

	private ItemGrid _itemGridBottom;

	private Group _specialSlotBackdrop;

	private Label _statsHealth;

	private Label _statsHealthGain;

	private Label _statsMana;

	private Label _statsManaGain;

	private Label _statsArmorRating;

	private Label _statsMeleePower;

	private Label _statsRangedPower;

	private Label _statsMagicPower;

	private Label _utilitySlotInputBinding;

	private Label _consumableSlotInputBinding;

	private PatchStyle _specialSlotBackground;

	private PatchStyle _specialSlotCompatibleBackground;

	private int _activeUtilitySlot = -1;

	private int _activeConsumableSlot = -1;

	private AppInGame.ItemSelector _compatibleItemSelectorItemInteraction;

	public Group Panel { get; private set; }

	public CharacterPanel(InGameView inGameView, Element parent = null)
		: base(inGameView, parent)
	{
	}

	protected override void OnMounted()
	{
		_nameLabel.Text = _inGameView.Interface.App.AuthManager.Settings.Username;
		UpdateCharacterVisibility(doLayout: false);
		UpdateInputBindings();
		UpdateGrid();
		UpdateCompatibleSlotHighlight();
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Pages/Inventory/CharacterPanel.ui", out var document);
		_specialSlotBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SpecialSlotBackground");
		_specialSlotCompatibleBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SpecialSlotCompatibleBackground");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		Panel = uIFragment.Get<Group>("Panel");
		_nameLabel = uIFragment.Get<Label>("NameLabel");
		_previewContainer = uIFragment.Get<CharacterPreviewComponent>("PreviewContainer");
		_statsHealth = uIFragment.Get<Label>("StatsHealth");
		_statsHealthGain = uIFragment.Get<Label>("StatsHealthGain");
		_statsMana = uIFragment.Get<Label>("StatsMana");
		_statsManaGain = uIFragment.Get<Label>("StatsManaGain");
		_statsArmorRating = uIFragment.Get<Label>("StatsArmorRating");
		_statsMeleePower = uIFragment.Get<Label>("StatsMeleePower");
		_statsRangedPower = uIFragment.Get<Label>("StatsRangedPower");
		_statsMagicPower = uIFragment.Get<Label>("StatsMagicPower");
		_utilitySlotInputBinding = uIFragment.Get<Label>("UtilitySlotInputBinding");
		_consumableSlotInputBinding = uIFragment.Get<Label>("ConsumableSlotInputBinding");
		_specialSlotBackdrop = uIFragment.Get<Group>("SpecialSlotBackdrop");
		_itemGridLeft = uIFragment.Get<ItemGrid>("ItemGridLeft");
		_itemGridLeft.Slots = new ItemGridSlot[3];
		_itemGridLeft.InventorySectionId = -3;
		_itemGridLeft.SlotClicking = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryClick(-3, TranslateArmorSlotIndex(slotIndex, 0), button);
		};
		_itemGridLeft.Dropped = delegate(int targetSlotIndex, Element sourceItemGrid, ItemGrid.ItemDragData dragData)
		{
			_inGameView.HandleInventoryDragEnd(_itemGridLeft, -3, TranslateArmorSlotIndex(targetSlotIndex, 0), sourceItemGrid, dragData);
		};
		_itemGridLeft.DragCancelled = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryDropItem(-3, TranslateArmorSlotIndex(slotIndex, 0), button);
		};
		_itemGridLeft.SlotMouseEntered = delegate(int slotIndex)
		{
			if (slotIndex == 2)
			{
				OnSpecialSlotMouseEntered(-5, slotIndex);
			}
			else
			{
				OnArmorSlotMouseEntered(slotIndex);
			}
		};
		_itemGridLeft.SlotMouseExited = delegate(int slotIndex)
		{
			if (slotIndex == 2)
			{
				OnSpecialSlotMouseExited(-5);
			}
			else
			{
				OnArmorSlotMouseExited(slotIndex);
			}
		};
		_itemGridRight = uIFragment.Get<ItemGrid>("ItemGridRight");
		_itemGridRight.Slots = new ItemGridSlot[3];
		_itemGridRight.InventorySectionId = -3;
		_itemGridRight.SlotClicking = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryClick(-3, TranslateArmorSlotIndex(slotIndex, 2), button);
		};
		_itemGridRight.Dropped = delegate(int targetSlotIndex, Element sourceItemGrid, ItemGrid.ItemDragData dragData)
		{
			_inGameView.HandleInventoryDragEnd(_itemGridRight, -3, TranslateArmorSlotIndex(targetSlotIndex, 2), sourceItemGrid, dragData);
		};
		_itemGridRight.DragCancelled = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryDropItem(-3, TranslateArmorSlotIndex(slotIndex, 2), button);
		};
		_itemGridRight.SlotMouseEntered = delegate(int slotIndex)
		{
			if (slotIndex == 2)
			{
				OnSpecialSlotMouseEntered(-6, slotIndex);
			}
			else
			{
				OnArmorSlotMouseEntered(slotIndex + 2);
			}
		};
		_itemGridRight.SlotMouseExited = delegate(int slotIndex)
		{
			if (slotIndex == 2)
			{
				OnSpecialSlotMouseExited(-6);
			}
			else
			{
				OnArmorSlotMouseExited(slotIndex + 2);
			}
		};
		_itemGridBottom = uIFragment.Get<ItemGrid>("ItemGridBottom");
		_itemGridBottom.InventorySectionId = -3;
		_itemGridBottom.Slots = new ItemGridSlot[3];
		for (int i = 0; i < _itemGridBottom.Slots.Length; i++)
		{
			_itemGridBottom.Slots[i] = new ItemGridSlot
			{
				Icon = new PatchStyle("InGame/Pages/Inventory/RingSlotIconSpecial.png")
			};
		}
		int ringSlotOffset = 5;
		_itemGridBottom.SlotClicking = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryClick(-3, slotIndex + ringSlotOffset, button);
		};
		_itemGridBottom.Dropped = delegate(int targetSlotIndex, Element sourceItemGrid, ItemGrid.ItemDragData dragData)
		{
			_inGameView.HandleInventoryDragEnd(_itemGridBottom, -3, targetSlotIndex + ringSlotOffset, sourceItemGrid, dragData);
		};
		_itemGridBottom.DragCancelled = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryDropItem(-3, slotIndex + ringSlotOffset, button);
		};
		_itemGridBottom.SlotMouseEntered = delegate(int slotIndex)
		{
			OnArmorSlotMouseEntered(slotIndex + ringSlotOffset);
		};
		_itemGridBottom.SlotMouseExited = delegate(int slotIndex)
		{
			OnArmorSlotMouseExited(slotIndex + ringSlotOffset);
		};
		if (_inGameView.ArmorStacks != null)
		{
			UpdateGrid();
		}
		if (base.IsMounted)
		{
			UpdateCharacterVisibility(doLayout: false);
			UpdateInputBindings(doLayout: false);
		}
		static int TranslateArmorSlotIndex(int index, int offset)
		{
			return ((index >= 2) ? (index + 3) : index) + offset;
		}
	}

	private void OnSpecialSlotMouseEntered(int sectionId, int slotIndex)
	{
		_inGameView.HandleItemSlotMouseEntered(sectionId, 0);
		_inGameView.InventoryPage.StoragePanel.UpdateGrid();
		_inGameView.HotbarComponent.SetupGrid();
		ClientItemStack clientItemStack = _inGameView.ItemDragData?.ItemStack;
		if (clientItemStack != null)
		{
			if (!_inGameView.Items.TryGetValue(clientItemStack.Id, out var value))
			{
				return;
			}
			if (sectionId == -5)
			{
				if (value.Utility == null || !value.Utility.Usable)
				{
					return;
				}
			}
			else if (!value.Consumable)
			{
				return;
			}
		}
		ItemSlotSelectorPopover itemSelectorPopover = _inGameView.InventoryPage.ItemSelectorPopover;
		float scale = Desktop.Scale;
		ItemGrid itemGrid = ((sectionId == -5) ? _itemGridLeft : _itemGridRight);
		float num = (float)slotIndex + 0.5f;
		float num2 = (float)(-_rectangleAfterPadding.Left) / scale + (float)itemGrid.RectangleAfterPadding.Left / scale - (float)itemSelectorPopover.Anchor.Width.Value / 2f + (float)(itemGrid.Style.SlotSize + itemGrid.Style.SlotSpacing) / 2f;
		float num3 = (float)(-_rectangleAfterPadding.Top + itemGrid.RectangleAfterPadding.Top) / scale + (float)(itemGrid.Style.SlotSize + itemGrid.Style.SlotSpacing) * num - (float)itemSelectorPopover.Anchor.Height.Value / 2f;
		int activeSlot = ((sectionId == -5) ? _activeUtilitySlot : _activeConsumableSlot);
		itemSelectorPopover.Setup(sectionId, activeSlot, (int)System.Math.Round(num2), (int)System.Math.Round(num3));
		itemSelectorPopover.Visible = true;
		itemSelectorPopover.Layout(_rectangleAfterPadding);
		UpdateCompatibleSlotHighlight();
	}

	private void OnSpecialSlotMouseExited(int sectionId)
	{
		_inGameView.HandleItemSlotMouseExited(sectionId, 0);
	}

	public void OnSetActiveUtilitySlot(int activeSlot)
	{
		_activeUtilitySlot = activeSlot;
		if (base.IsMounted)
		{
			UpdateSelectorSlots();
			ItemSlotSelectorPopover itemSelectorPopover = _inGameView.InventoryPage.ItemSelectorPopover;
			if (itemSelectorPopover.InventorySectionId == -5 && itemSelectorPopover.IsMounted)
			{
				itemSelectorPopover.SelectedSlot = activeSlot + 1;
			}
		}
	}

	public void OnSetActiveConsumableSlot(int activeSlot)
	{
		_activeConsumableSlot = activeSlot;
		if (base.IsMounted)
		{
			UpdateSelectorSlots();
			ItemSlotSelectorPopover itemSelectorPopover = _inGameView.InventoryPage.ItemSelectorPopover;
			if (itemSelectorPopover.InventorySectionId == -6 && itemSelectorPopover.IsMounted)
			{
				itemSelectorPopover.SelectedSlot = activeSlot + 1;
			}
		}
	}

	private void OnArmorSlotMouseEntered(int slotIndex)
	{
		if (!Desktop.IsMouseDragging)
		{
			_inGameView.HandleItemSlotMouseEntered(-3, slotIndex);
			_inGameView.InventoryPage.StoragePanel.UpdateGrid();
			_inGameView.HotbarComponent.SetupGrid();
		}
	}

	private void OnArmorSlotMouseExited(int slotIndex)
	{
		_inGameView.HandleItemSlotMouseExited(-3, slotIndex);
		_inGameView.InventoryPage.StoragePanel.UpdateGrid();
		_inGameView.HotbarComponent.SetupGrid();
	}

	public void UpdateGrid()
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i <= 4; i++)
		{
			ItemGrid itemGrid = ((i > 1) ? _itemGridRight : _itemGridLeft);
			int num = ((i > 1) ? (i - 2) : i);
			itemGrid.Slots[num] = new ItemGridSlot(_inGameView.ArmorStacks[i])
			{
				InventorySlotIndex = i
			};
			if (_inGameView.ArmorStacks[i] == null)
			{
				ItemGridSlot[] slots = itemGrid.Slots;
				ItemGridSlot itemGridSlot = new ItemGridSlot();
				ItemArmorSlot val = (ItemArmorSlot)i;
				itemGridSlot.Icon = new PatchStyle("InGame/Pages/Inventory/ArmorSlotIcon" + ((object)(ItemArmorSlot)(ref val)).ToString() + ".png");
				slots[num] = itemGridSlot;
			}
		}
		int num2 = 5;
		for (int j = 0; j < _itemGridBottom.Slots.Length; j++)
		{
			_itemGridBottom.Slots[j] = new ItemGridSlot(_inGameView.ArmorStacks[j + 5])
			{
				InventorySlotIndex = j + num2
			};
			if (_inGameView.ArmorStacks[j + num2] == null)
			{
				_itemGridBottom.Slots[j] = new ItemGridSlot
				{
					Icon = new PatchStyle("InGame/Pages/Inventory/RingSlotIconSpecial.png")
				};
			}
		}
		_itemGridLeft.Layout();
		_itemGridRight.Layout();
		_itemGridBottom.Layout();
		UpdateSelectorSlots();
		ItemSlotSelectorPopover itemSelectorPopover = _inGameView.InventoryPage.ItemSelectorPopover;
		if (itemSelectorPopover.IsMounted)
		{
			itemSelectorPopover.SetItemStacks(_inGameView.GetItemStacks(itemSelectorPopover.InventorySectionId));
		}
	}

	public void UpdateCompatibleSlotHighlight(bool updateGrid = true)
	{
		AppInGame.ItemSelector compatibleItemSelectorItemInteraction = _compatibleItemSelectorItemInteraction;
		if (_inGameView.InventoryPage.ItemSelectorPopover.IsMounted)
		{
			_compatibleItemSelectorItemInteraction = ((_inGameView.InventoryPage.ItemSelectorPopover.InventorySectionId == -5) ? AppInGame.ItemSelector.Utility : AppInGame.ItemSelector.Consumable);
		}
		else
		{
			ClientItemStack clientItemStack = _inGameView.ItemDragData?.ItemStack;
			if (clientItemStack == null && _inGameView.HoveredItemSlot != null)
			{
				ClientItemStack[] itemStacks = _inGameView.GetItemStacks(_inGameView.HoveredItemSlot.InventorySectionId);
				clientItemStack = itemStacks[_inGameView.HoveredItemSlot.SlotId];
			}
			if (clientItemStack != null && _inGameView.Items.TryGetValue(clientItemStack.Id, out var value))
			{
				if (value.Consumable)
				{
					_compatibleItemSelectorItemInteraction = AppInGame.ItemSelector.Consumable;
				}
				else if (value.Utility != null && value.Utility.Usable)
				{
					_compatibleItemSelectorItemInteraction = AppInGame.ItemSelector.Utility;
				}
				else
				{
					_compatibleItemSelectorItemInteraction = AppInGame.ItemSelector.None;
				}
			}
			else
			{
				_compatibleItemSelectorItemInteraction = AppInGame.ItemSelector.None;
			}
		}
		if (updateGrid && compatibleItemSelectorItemInteraction != _compatibleItemSelectorItemInteraction)
		{
			UpdateSelectorSlots();
			SetupSpecialSlotBackdrop();
		}
	}

	private void SetupSpecialSlotBackdrop()
	{
		if (!_inGameView.InventoryPage.ItemSelectorPopover.IsMounted)
		{
			_specialSlotBackdrop.Visible = false;
			return;
		}
		ItemGrid itemGrid = ((_compatibleItemSelectorItemInteraction == AppInGame.ItemSelector.Utility) ? _itemGridLeft : _itemGridRight);
		float scale = Desktop.Scale;
		int num = itemGrid.Style.SlotSize + itemGrid.Style.SlotSpacing;
		int num2 = _specialSlotBackdrop.Anchor.Width.Value / 2;
		int num3 = _specialSlotBackdrop.Anchor.Height.Value / 2;
		float num4 = (float)(itemGrid.RectangleAfterPadding.Left - _specialSlotBackdrop.Parent.RectangleAfterPadding.Left) / scale + (float)(num / 2) - (float)num2;
		double a = (double)((float)(itemGrid.RectangleAfterPadding.Top - _specialSlotBackdrop.Parent.RectangleAfterPadding.Top) / scale) + (double)num * 2.5 - (double)num3;
		_specialSlotBackdrop.Anchor.Left = (int)System.Math.Round(num4);
		_specialSlotBackdrop.Anchor.Top = (int)System.Math.Round(a);
		_specialSlotBackdrop.Visible = true;
		_specialSlotBackdrop.Layout(_specialSlotBackdrop.Parent.RectangleAfterPadding);
	}

	private void UpdateSelectorSlots()
	{
		if (_activeConsumableSlot > -1 && _inGameView.ConsumableStacks[_activeConsumableSlot] != null)
		{
			_itemGridRight.Slots[2] = new ItemGridSlot(_inGameView.ConsumableStacks[_activeConsumableSlot])
			{
				SkipItemQualityBackground = true,
				Background = ((_compatibleItemSelectorItemInteraction == AppInGame.ItemSelector.Consumable) ? _specialSlotCompatibleBackground : _specialSlotBackground)
			};
		}
		else
		{
			_itemGridRight.Slots[2] = new ItemGridSlot
			{
				Icon = new PatchStyle("InGame/Pages/Inventory/ConsumableSlotIcon.png"),
				Background = ((_compatibleItemSelectorItemInteraction == AppInGame.ItemSelector.Consumable) ? _specialSlotCompatibleBackground : _specialSlotBackground)
			};
		}
		if (_activeUtilitySlot > -1 && _inGameView.UtilityStacks[_activeUtilitySlot] != null)
		{
			_itemGridLeft.Slots[2] = new ItemGridSlot(_inGameView.UtilityStacks[_activeUtilitySlot])
			{
				SkipItemQualityBackground = true,
				Background = ((_compatibleItemSelectorItemInteraction == AppInGame.ItemSelector.Utility) ? _specialSlotCompatibleBackground : _specialSlotBackground)
			};
		}
		else
		{
			_itemGridLeft.Slots[2] = new ItemGridSlot
			{
				Icon = new PatchStyle("InGame/Pages/Inventory/UtilitySlotIcon.png"),
				Background = ((_compatibleItemSelectorItemInteraction == AppInGame.ItemSelector.Utility) ? _specialSlotCompatibleBackground : _specialSlotBackground)
			};
		}
		_itemGridRight.Layout();
		_itemGridLeft.Layout();
	}

	public void OnHealthChanged(ClientEntityStatValue health)
	{
		_statsHealth.Text = $"{MathHelper.Round(health.Value)}/{MathHelper.Round(health.Max)}";
		_statsHealth.Layout();
	}

	public void OnManaChanged(ClientEntityStatValue mana)
	{
		_statsMana.Text = $"{MathHelper.Round(mana.Value)}/{MathHelper.Round(mana.Max)}";
		_statsMana.Layout();
	}

	private void UpdateInputBindings(bool doLayout = true)
	{
		_utilitySlotInputBinding.Text = _inGameView.Interface.App.Settings.InputBindings.ShowUtilitySlotSelector.BoundInputLabel;
		if (doLayout)
		{
			_utilitySlotInputBinding.Layout();
		}
		_consumableSlotInputBinding.Text = _inGameView.Interface.App.Settings.InputBindings.ShowConsumableSlotSelector.BoundInputLabel;
		if (doLayout)
		{
			_consumableSlotInputBinding.Layout();
		}
	}

	public void UpdateCharacterVisibility(bool doLayout = true)
	{
		_previewContainer.Visible = Desktop.GetLayer(2) == null;
		if (_previewContainer.Visible && doLayout)
		{
			_previewContainer.Layout(_previewContainer.Parent.RectangleAfterPadding);
		}
	}

	public void OnItemSlotSelectorClosed()
	{
		UpdateCompatibleSlotHighlight(updateGrid: false);
		UpdateSelectorSlots();
		_specialSlotBackdrop.Visible = false;
		_itemGridBottom.RefreshMouseOver();
		_inGameView.InventoryPage.StoragePanel.UpdateGrid();
		_inGameView.HotbarComponent.SetupGrid();
	}
}
