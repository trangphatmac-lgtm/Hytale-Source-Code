#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Data.Items;
using HytaleClient.Interface.InGame.Hud;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class ItemSlotSelectorPopover : BaseItemSlotSelector
{
	private int _dragSlotIndex = -1;

	private int _mouseDownSlotIndex = -1;

	private int _pressedMouseButton;

	private Point _mouseDownPosition;

	private bool _wasDragging;

	private SoundStyle _mouseDownSound;

	public SoundStyle ItemMovedSound;

	public int InventorySectionId { get; private set; }

	public ItemSlotSelectorPopover(InGameView inGameView, Element parent)
		: base(inGameView, parent, enableEmptySlot: true)
	{
		_quantityFontSize = 16f;
	}

	public void Build()
	{
		Interface.TryGetDocument("InGame/Pages/Inventory/ItemSlotSelectorPopover.ui", out var document);
		Build(document);
		Anchor = _containerBackgroundAnchor;
		document.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "MouseDownSound", out _mouseDownSound);
		document.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "ItemMovedSound", out ItemMovedSound);
	}

	protected override void OnUnmounted()
	{
		base.OnUnmounted();
		_mouseDownSlotIndex = -1;
		if (_dragSlotIndex != -1)
		{
			int dragSlotIndex = _dragSlotIndex;
			_dragSlotIndex = -1;
			_inGameView.SetupDragAndDropItem(null);
			OnDragCancel(dragSlotIndex, _pressedMouseButton);
			Desktop.ClearMouseDrag();
		}
		_inGameView.InventoryPage.CharacterPanel.OnItemSlotSelectorClosed();
	}

	public void Setup(int sectionId, int activeSlot, int x, int y)
	{
		InventorySectionId = sectionId;
		Anchor.Left = x;
		Anchor.Top = y;
		SelectedSlot = activeSlot + 1;
		_hoveredSlot = SelectedSlot;
		SetItemStacks(_inGameView.GetItemStacks(sectionId));
	}

	private void OnDragCancel(int slotIndex, int button)
	{
		_inGameView.HandleInventoryDropItem(InventorySectionId, slotIndex, button);
		if (!base.IsMounted || HitTest(Desktop.MousePosition) == null)
		{
			base.Visible = false;
		}
	}

	protected internal override void OnMouseDrop(object data, Element sourceElement, out bool accepted)
	{
		int num = _hoveredSlot - 1;
		accepted = num != -1 && data is ItemGrid.ItemDragData;
		if (!accepted)
		{
			return;
		}
		ItemGrid.ItemDragData itemDragData = (ItemGrid.ItemDragData)data;
		if ((long)itemDragData.PressedMouseButton == 1 && Desktop.IsShiftKeyDown)
		{
			if (itemDragData.ItemStack.Quantity > 1)
			{
				itemDragData.ItemStack.Quantity = (int)System.Math.Floor((float)itemDragData.ItemStack.Quantity / 2f);
			}
		}
		else if ((long)itemDragData.PressedMouseButton == 3)
		{
			itemDragData.ItemStack.Quantity = 1;
		}
		_inGameView.SetupDragAndDropItem(null);
		_inGameView.HandleInventoryDragEnd(this, InventorySectionId, num, sourceElement, itemDragData);
	}

	protected internal override void OnMouseDragCancel(object data)
	{
		int dragSlotIndex = _dragSlotIndex;
		_dragSlotIndex = -1;
		_inGameView.SetupDragAndDropItem(null);
		OnDragCancel(dragSlotIndex, _pressedMouseButton);
	}

	protected internal override void OnMouseDragComplete(Element element, object data)
	{
		_dragSlotIndex = -1;
		if (HitTest(Desktop.MousePosition) == null)
		{
			base.Visible = false;
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if (!_wasDragging && activate && _dragSlotIndex == -1)
		{
			int num = _hoveredSlot - 1;
			if (num != -1 && num == _mouseDownSlotIndex)
			{
				_inGameView.HandleInventoryClick(InventorySectionId, num, evt.Button);
			}
		}
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		_mouseDownSlotIndex = _hoveredSlot - 1;
		_pressedMouseButton = evt.Button;
		_wasDragging = false;
		if (_mouseDownSlotIndex != -1)
		{
			_mouseDownPosition = Desktop.MousePosition;
		}
		if (_mouseDownSound != null)
		{
			Desktop.Provider.PlaySound(_mouseDownSound);
		}
	}

	protected internal override void OnMouseDragMove()
	{
		RefreshHoveredSlot();
	}

	protected override void OnMouseMove()
	{
		base.OnMouseMove();
		if (!base.CapturedMouseButton.HasValue || _mouseDownSlotIndex == -1 || Desktop.IsMouseDragging || _wasDragging)
		{
			return;
		}
		float num = new Vector2(Desktop.MousePosition.X - _mouseDownPosition.X, Desktop.MousePosition.Y - _mouseDownPosition.Y).Length();
		if (num >= 3f)
		{
			ClientItemStack clientItemStack = _itemStacks[_mouseDownSlotIndex];
			if (clientItemStack != null)
			{
				_wasDragging = true;
				_dragSlotIndex = _mouseDownSlotIndex;
				ClientItemStack itemStack = new ClientItemStack(clientItemStack.Id, clientItemStack.Quantity)
				{
					Metadata = clientItemStack.Metadata
				};
				ItemGrid.ItemDragData itemDragData = new ItemGrid.ItemDragData(_pressedMouseButton, _mouseDownSlotIndex, itemStack, InventorySectionId, _mouseDownSlotIndex);
				_inGameView.SetupDragAndDropItem(itemDragData);
				Desktop.StartMouseDrag(itemDragData, this);
			}
		}
	}

	protected override void OnMouseLeave()
	{
		if (_dragSlotIndex == -1)
		{
			base.Visible = false;
		}
	}

	protected internal override void OnMouseDragExit(object data, Element sourceElement)
	{
		OnMouseLeave();
	}

	public override Element HitTest(Point position)
	{
		Debug.Assert(base.IsMounted);
		float num = (float)base.AnchoredRectangle.Width / 2f;
		Point point = new Point(position.X - base.AnchoredRectangle.Left - base.AnchoredRectangle.Width / 2, position.Y - base.AnchoredRectangle.Top - base.AnchoredRectangle.Height / 2);
		if ((double)(point.X * point.X) + (double)(point.Y * point.Y) > (double)(num * num))
		{
			return null;
		}
		return base.HitTest(position) ?? this;
	}
}
