using System;
using System.Collections.Generic;
using HytaleClient.Data.Items;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

[UIMarkupElement]
internal class BlockSelector : InputElement<string>
{
	[UIMarkupData]
	public class BlockSelectorStyle
	{
		public ItemGrid.ItemGridStyle ItemGridStyle = new ItemGrid.ItemGridStyle();

		public PatchStyle SlotDropIcon;

		public PatchStyle SlotDeleteIcon;

		public PatchStyle SlotHoverOverlay;
	}

	private readonly InGameView _inGameView;

	private ItemGrid _itemGrid;

	private int _capacity;

	private int _dropSlotIndex;

	private bool _isSwapping;

	[UIMarkupProperty]
	public BlockSelectorStyle Style = new BlockSelectorStyle();

	private string _value = "";

	[UIMarkupProperty]
	public int Capacity
	{
		get
		{
			return _capacity;
		}
		set
		{
			_itemGrid.SlotsPerRow = value;
			_capacity = value;
			InitialiseSlots();
		}
	}

	public override string Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;
			RebuildSlots();
		}
	}

	public BlockSelector(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		IUIProvider provider = desktop.Provider;
		IUIProvider iUIProvider = provider;
		if (!(iUIProvider is CustomUIProvider customUIProvider))
		{
			if (!(iUIProvider is Interface @interface))
			{
				throw new Exception("IUIProvider must be of type CustomUIProvider or Interface");
			}
			_inGameView = @interface.InGameView;
		}
		else
		{
			_inGameView = customUIProvider.Interface.InGameView;
		}
		_itemGrid = new ItemGrid(Desktop, this)
		{
			SlotMouseEntered = SlotMouseEntered,
			SlotMouseExited = SlotMouseExited,
			SlotMouseDragCompleted = delegate(int dragSlotIndex, Element element, ItemGrid.ItemDragData dragData)
			{
				SlotMouseDragCompleted(dragSlotIndex);
			},
			SlotMouseDragExited = SlotMouseDragExited,
			Dropped = Dropped,
			SlotClicking = SlotClicking
		};
		InitialiseSlots();
	}

	private void InitialiseSlots()
	{
		_itemGrid.Slots = new ItemGridSlot[Capacity];
		for (int i = 0; i < Capacity; i++)
		{
			_itemGrid.Slots[i] = new ItemGridSlot();
		}
	}

	public override Point ComputeScaledMinSize(int? maxWidth, int? maxHeight)
	{
		ApplyStyles();
		return base.ComputeScaledMinSize(maxWidth, maxHeight);
	}

	protected override void ApplyStyles()
	{
		_itemGrid.Style = Style.ItemGridStyle;
		base.ApplyStyles();
	}

	protected override void OnMounted()
	{
		UpdateDropSlot();
	}

	private void SlotMouseEntered(int slotIndex)
	{
		ItemGridSlot itemGridSlot = _itemGrid.Slots[slotIndex];
		if (Desktop.IsMouseDragging && itemGridSlot.ItemStack == null)
		{
			_itemGrid.Slots[_dropSlotIndex].Overlay = Style.SlotHoverOverlay;
		}
		else if (itemGridSlot.ItemStack != null)
		{
			itemGridSlot.Overlay = (Desktop.IsMouseDragging ? Style.SlotHoverOverlay : Style.SlotDeleteIcon);
		}
		_itemGrid.Layout();
	}

	private void SlotMouseExited(int slotIndex)
	{
		if (_dropSlotIndex > -1)
		{
			_itemGrid.Slots[_dropSlotIndex].Overlay = null;
		}
		if (slotIndex > -1)
		{
			_itemGrid.Slots[slotIndex].Overlay = null;
		}
		_itemGrid.Layout();
	}

	private void SlotMouseDragCompleted(int dragSlotIndex)
	{
		if (_isSwapping)
		{
			_isSwapping = false;
			return;
		}
		for (int i = dragSlotIndex; i < _itemGrid.Slots.Length - 1; i++)
		{
			_itemGrid.Slots[i].ItemStack = _itemGrid.Slots[i + 1].ItemStack;
			_itemGrid.Slots[i + 1].ItemStack = null;
			if (_itemGrid.Slots[i].ItemStack == null)
			{
				break;
			}
		}
		ItemGridSlot[] slots = _itemGrid.Slots;
		foreach (ItemGridSlot itemGridSlot in slots)
		{
			itemGridSlot.Overlay = null;
		}
		UpdateDropSlot();
		UpdateValueFromSlots();
	}

	private void SlotMouseDragExited(int dragSlotIndex, int mouseOverSlotIndex)
	{
		ItemGridSlot[] slots = _itemGrid.Slots;
		foreach (ItemGridSlot itemGridSlot in slots)
		{
			itemGridSlot.Overlay = null;
		}
	}

	private void Dropped(int slotIndex, Element element, ItemGrid.ItemDragData dragData)
	{
		ClientItemBase clientItemBase = _inGameView.Items[dragData.ItemStack.Id];
		if (clientItemBase.BlockId == 0)
		{
			return;
		}
		if (_itemGrid.Slots[slotIndex].ItemStack != null)
		{
			if (element == _itemGrid)
			{
				_itemGrid.Slots[dragData.ItemGridIndex].ItemStack = _itemGrid.Slots[slotIndex].ItemStack;
				_isSwapping = true;
			}
			_itemGrid.Slots[slotIndex].ItemStack = dragData.ItemStack;
		}
		else
		{
			_itemGrid.Slots[_dropSlotIndex].ItemStack = dragData.ItemStack;
		}
		UpdateDropSlot();
		UpdateValueFromSlots();
	}

	private void SlotClicking(int index, int button)
	{
		EmptySlot(index);
	}

	public void EmptySlot(int index)
	{
		if (_dropSlotIndex != index && _itemGrid.Slots[index].ItemStack != null)
		{
			_itemGrid.Slots[index].ItemStack = null;
			UpdateValueFromSlots();
			RebuildSlots();
		}
	}

	public void Reset()
	{
		Value = "";
		ValueChanged?.Invoke();
	}

	private void UpdateValueFromSlots()
	{
		List<string> list = new List<string>();
		ItemGridSlot[] slots = _itemGrid.Slots;
		foreach (ItemGridSlot itemGridSlot in slots)
		{
			if (itemGridSlot.ItemStack != null)
			{
				list.Add(itemGridSlot.ItemStack.Id);
			}
		}
		_value = string.Join(",", list);
		ValueChanged?.Invoke();
	}

	private void RebuildSlots()
	{
		InitialiseSlots();
		if (_value == null)
		{
			return;
		}
		string[] array = _value.Split(new char[1] { ',' });
		for (int i = 0; i < array.Length && i < _itemGrid.Slots.Length; i++)
		{
			string key = array[i].Split(new char[1] { '%' })[^1];
			if (_inGameView.Items.TryGetValue(key, out var value))
			{
				_itemGrid.Slots[i] = new ItemGridSlot(new ClientItemStack(value.Id));
			}
		}
		UpdateDropSlot();
	}

	private void UpdateDropSlot()
	{
		ItemGridSlot[] slots = _itemGrid.Slots;
		foreach (ItemGridSlot itemGridSlot in slots)
		{
			itemGridSlot.Icon = null;
		}
		int dropSlotIndex = -1;
		for (int j = 0; j < _itemGrid.Slots.Length; j++)
		{
			ItemGridSlot itemGridSlot2 = _itemGrid.Slots[j];
			if (itemGridSlot2.ItemStack == null)
			{
				itemGridSlot2.Icon = Style.SlotDropIcon;
				dropSlotIndex = j;
				_itemGrid.Slots[j] = itemGridSlot2;
				break;
			}
		}
		_dropSlotIndex = dropSlotIndex;
		_itemGrid.Layout();
	}
}
