#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.InGame.Modules.BuilderTools.Tools;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.Interface.InGame;

[UIMarkupElement]
internal class ItemGrid : Element
{
	public class ItemDragData
	{
		public readonly int PressedMouseButton;

		public readonly int ItemGridIndex;

		public readonly ClientItemStack ItemStack;

		public readonly int? InventorySectionId;

		public readonly int SlotId;

		public ItemDragData(int pressedMouseButton, int itemGridIndex, ClientItemStack itemStack, int? inventorySectionId, int slotId)
		{
			PressedMouseButton = pressedMouseButton;
			ItemGridIndex = itemGridIndex;
			ItemStack = itemStack;
			InventorySectionId = inventorySectionId;
			SlotId = slotId;
		}
	}

	[UIMarkupData]
	public class ItemGridStyle
	{
		public int SlotSize;

		public int SlotIconSize;

		public int SlotSpacing;

		public PatchStyle DurabilityBarBackground;

		public UIPath DurabilityBar;

		public Anchor DurabilityBarAnchor;

		public UInt32Color DurabilityBarColorStart;

		public UInt32Color DurabilityBarColorEnd;

		public PatchStyle SlotBackground;

		public PatchStyle QuantityPopupSlotOverlay;

		public PatchStyle BrokenSlotBackgroundOverlay;

		public PatchStyle BrokenSlotIconOverlay;

		public PatchStyle DefaultItemIcon;

		public SoundStyle ItemStackMouseDownSound;

		public SoundStyle ItemStackActivateSound;

		public SoundStyle ItemStackMovedSound;

		public ItemGridStyle Clone()
		{
			return new ItemGridStyle
			{
				SlotSize = SlotSize,
				SlotIconSize = SlotIconSize,
				SlotSpacing = SlotSpacing,
				DurabilityBar = DurabilityBar,
				DurabilityBarBackground = DurabilityBarBackground,
				DurabilityBarAnchor = DurabilityBarAnchor,
				DurabilityBarColorStart = DurabilityBarColorStart,
				DurabilityBarColorEnd = DurabilityBarColorEnd,
				SlotBackground = SlotBackground,
				QuantityPopupSlotOverlay = QuantityPopupSlotOverlay,
				BrokenSlotBackgroundOverlay = BrokenSlotBackgroundOverlay,
				BrokenSlotIconOverlay = BrokenSlotIconOverlay,
				DefaultItemIcon = DefaultItemIcon
			};
		}
	}

	private const byte DraggingItemStackOpacity = 125;

	private const byte IncompatibleItemStackOpacity = 38;

	private const float DraggingItemStackOpacityFloat = 25f / 51f;

	private const float IncompatibleItemStackOpacityFloat = 0.14901961f;

	private static readonly UInt32Color DraggingItemStackColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 125);

	private static readonly UInt32Color IncompatibleItemStackColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 38);

	private static readonly UInt32Color BrokenItemStackColor = UInt32Color.FromRGBA(190, 190, 190, byte.MaxValue);

	private static readonly UInt32Color IncompatibleItemStackSlotColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 120);

	private static readonly UInt32Color InfoPanePrimaryTextColor = UInt32Color.FromRGBA(170, 180, 190, byte.MaxValue);

	private readonly InGameView _inGameView;

	public Action<int, int> SlotClicking;

	public Action<int> SlotDoubleClicking;

	public Action<int, Element, ItemDragData> Dropped;

	public Action<int, int> DragCancelled;

	public Action<int> SlotMouseEntered;

	public Action<int> SlotMouseExited;

	public Action<int, Element, ItemDragData> SlotMouseDragCompleted;

	public Action<int, int> SlotMouseDragExited;

	public Func<int, bool> SlotMouseDown;

	[UIMarkupProperty]
	public int SlotsPerRow;

	[UIMarkupProperty]
	public bool ShowScrollbar;

	[UIMarkupProperty]
	public ItemGridStyle Style;

	[UIMarkupProperty]
	public bool RenderItemQualityBackground = true;

	[UIMarkupProperty]
	public ItemGridInfoDisplayMode InfoDisplay = (ItemGridInfoDisplayMode)0;

	[UIMarkupProperty]
	public int AdjacentInfoPaneGridWidth = 2;

	[UIMarkupProperty]
	public bool AreItemsDraggable = true;

	[UIMarkupProperty]
	public int? InventorySectionId;

	[UIMarkupProperty]
	public ItemGridSlot[] Slots = new ItemGridSlot[0];

	[UIMarkupProperty]
	public bool AllowMaxStackDraggableItems;

	private ItemTooltipLayer _tooltip;

	private bool _wasDragging;

	private Point _mouseDownPosition;

	private int _mouseDownSlotIndex = -1;

	private int _mouseOverSlotIndex = -1;

	private int _dragSlotIndex = -1;

	private int _pressedMouseButton = -1;

	private bool _isMouseDraggingOver;

	private int _slotIndexForDoubleClick = -1;

	private bool _isQuantityPopupOpen;

	private int _quantityPopupSlotIndex = -1;

	private Font _regularFont;

	private Font _boldFont;

	private TexturePatch _durabilityBarBackgroundTexture;

	private TextureArea _durabilityBarTexture;

	private TexturePatch _slotBackgroundPatch;

	private TexturePatch _quantityPopupSlotOverlayPatch;

	private TexturePatch _defaultItemIconPatch;

	private TexturePatch _brokenSlotBackgroundOverlayPatch;

	private TexturePatch _brokenSlotIconOverlayPatch;

	private TexturePatch _blockSlotBackgroundPatch;

	private TexturePatch _infoPaneBackgroundPatch;

	private readonly Dictionary<int, TexturePatch> _slotBackgroundPatches = new Dictionary<int, TexturePatch>();

	private readonly Dictionary<int, TexturePatch> _specialSlotBackgroundPatches = new Dictionary<int, TexturePatch>();

	[UIMarkupProperty]
	public ScrollbarStyle ScrollbarStyle
	{
		set
		{
			_scrollbarStyle = value;
		}
	}

	[UIMarkupProperty]
	public ClientItemStack[] ItemStacks
	{
		set
		{
			Slots = new ItemGridSlot[value.Length];
			SetItemStacks(value);
		}
	}

	public ItemGrid(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
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
		_tooltip = new ItemTooltipLayer(_inGameView)
		{
			ShowDelay = 0.1f
		};
	}

	protected override void OnUnmounted()
	{
		_quantityPopupSlotIndex = -1;
		if (_mouseOverSlotIndex != -1)
		{
			SlotMouseExited?.Invoke(_mouseDownSlotIndex);
			_mouseDownSlotIndex = -1;
		}
		if (_dragSlotIndex != -1)
		{
			Desktop.CancelMouseDrag();
		}
		else if (_isQuantityPopupOpen)
		{
			_isQuantityPopupOpen = false;
			_quantityPopupSlotIndex = -1;
			_inGameView.SetupDragAndDropItem(null);
			Desktop.SetTransientLayer(null);
		}
	}

	protected override void ApplyStyles()
	{
		_regularFont = Desktop.Provider.GetFontFamily("Default").RegularFont;
		_boldFont = Desktop.Provider.GetFontFamily("Default").BoldFont;
		_blockSlotBackgroundPatch = Desktop.MakeTexturePatch((Desktop.Provider is CustomUIProvider) ? new PatchStyle("Common/SlotBlock.png") : new PatchStyle("InGame/Pages/Inventory/SlotBlock.png"));
		_infoPaneBackgroundPatch = Desktop.MakeTexturePatch((Desktop.Provider is CustomUIProvider) ? new PatchStyle("Common/SlotInfoPane.png") : new PatchStyle("InGame/Pages/Inventory/SlotInfoPane.png"));
		_slotBackgroundPatches.Clear();
		ClientItemQuality[] itemQualities = _inGameView.InGame.Instance.ServerSettings.ItemQualities;
		for (int i = 0; i < itemQualities.Length; i++)
		{
			ClientItemQuality clientItemQuality = itemQualities[i];
			if (_inGameView.TryMountAssetTexture(clientItemQuality.SlotTexture, out var textureArea))
			{
				_slotBackgroundPatches[i] = Desktop.MakeTexturePatch(new PatchStyle(textureArea));
			}
			else
			{
				_slotBackgroundPatches[i] = Desktop.MakeTexturePatch(new PatchStyle(Desktop.Provider.MissingTexture));
			}
			if (_inGameView.TryMountAssetTexture(clientItemQuality.SpecialSlotTexture, out var textureArea2))
			{
				_specialSlotBackgroundPatches[i] = Desktop.MakeTexturePatch(new PatchStyle(textureArea2));
			}
			else
			{
				_specialSlotBackgroundPatches[i] = Desktop.MakeTexturePatch(new PatchStyle(Desktop.Provider.MissingTexture));
			}
		}
		_slotBackgroundPatch = ((Style.SlotBackground != null) ? Desktop.MakeTexturePatch(Style.SlotBackground) : null);
		_quantityPopupSlotOverlayPatch = ((Style.QuantityPopupSlotOverlay != null) ? Desktop.MakeTexturePatch(Style.QuantityPopupSlotOverlay) : null);
		_brokenSlotBackgroundOverlayPatch = ((Style.BrokenSlotBackgroundOverlay != null) ? Desktop.MakeTexturePatch(Style.BrokenSlotBackgroundOverlay) : null);
		_brokenSlotIconOverlayPatch = ((Style.BrokenSlotIconOverlay != null) ? Desktop.MakeTexturePatch(Style.BrokenSlotIconOverlay) : null);
		_defaultItemIconPatch = ((Style.DefaultItemIcon != null) ? Desktop.MakeTexturePatch(Style.DefaultItemIcon) : null);
		_durabilityBarBackgroundTexture = ((Style.DurabilityBarBackground != null) ? Desktop.MakeTexturePatch(Style.DurabilityBarBackground) : null);
		_durabilityBarTexture = ((Style.DurabilityBar != null) ? Desktop.Provider.MakeTextureArea(Style.DurabilityBar.Value) : null);
		ItemGridSlot[] slots = Slots;
		for (int j = 0; j < slots.Length; j++)
		{
			slots[j]?.ApplyStyles(_inGameView, Desktop);
		}
		base.ApplyStyles();
	}

	public override Point ComputeScaledMinSize(int? maxWidth, int? maxHeight)
	{
		Point zero = Point.Zero;
		int num = Desktop.ScaleRound(_scrollbarStyle.Size + _scrollbarStyle.Spacing);
		int itemsPerRow = GetItemsPerRow();
		if (Anchor.Height.HasValue)
		{
			zero.Y = Desktop.ScaleRound(Anchor.Height.Value);
		}
		else
		{
			int num2 = (int)System.Math.Ceiling((float)Slots.Length / (float)itemsPerRow);
			zero.Y = Desktop.ScaleRound(Style.SlotSize * num2 + Style.SlotSpacing * (num2 - 1));
			if (_layoutMode == LayoutMode.TopScrolling || _layoutMode == LayoutMode.BottomScrolling)
			{
				zero.X += num;
			}
			if (maxHeight.HasValue)
			{
				zero.Y = System.Math.Min(zero.Y, maxHeight.Value);
			}
		}
		if (Anchor.Width.HasValue)
		{
			zero.X = Desktop.ScaleRound(Anchor.Width.Value);
		}
		else
		{
			zero.X = Desktop.ScaleRound(Style.SlotSize * itemsPerRow + Style.SlotSpacing * (itemsPerRow - 1));
			if (_layoutMode == LayoutMode.LeftScrolling || _layoutMode == LayoutMode.RightScrolling)
			{
				zero.Y += num;
			}
			if (maxWidth.HasValue)
			{
				zero.X = System.Math.Min(zero.X, maxWidth.Value);
			}
		}
		if (Padding.Horizontal.HasValue)
		{
			zero.X += Desktop.ScaleRound(Padding.Horizontal.Value);
		}
		if (Padding.Vertical.HasValue)
		{
			zero.Y += Desktop.ScaleRound(Padding.Vertical.Value);
		}
		return zero;
	}

	protected override void LayoutSelf()
	{
		if (ShowScrollbar)
		{
			int num = (int)System.Math.Ceiling((float)Slots.Length / (float)GetItemsPerRow());
			ContentHeight = num * Style.SlotSize + (num - 1) * Style.SlotSpacing;
		}
		else
		{
			ContentHeight = null;
		}
		RefreshMouseOver(forceTooltipRefresh: true);
	}

	public override Element HitTest(Point position)
	{
		Debug.Assert(base.IsMounted);
		return _anchoredRectangle.Contains(position) ? this : null;
	}

	public int GetItemsPerRow()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		if ((int)InfoDisplay != 1)
		{
			return SlotsPerRow;
		}
		if (SlotsPerRow < 3)
		{
			throw new Exception("ItemGrid.SlotsPerRow cannot be less than 3 when using InfoDisplayMode.Adjacent");
		}
		if (SlotsPerRow % 3 == 0)
		{
			return SlotsPerRow / 3;
		}
		return (SlotsPerRow > 10) ? (SlotsPerRow / 4) : (SlotsPerRow / 2);
	}

	private int GetSlotIndexAtMousePosition()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Invalid comparison between Unknown and I4
		int itemsPerRow = GetItemsPerRow();
		float num = Desktop.ScaleNoRound(Style.SlotSize) + Desktop.ScaleNoRound(Style.SlotSpacing);
		float num2 = (((int)InfoDisplay == 1) ? (num * (float)itemsPerRow) : num);
		int num3 = (int)((float)(Desktop.MousePosition.X - _rectangleAfterPadding.X) / num2);
		if (num3 >= itemsPerRow)
		{
			return -1;
		}
		int num4 = (int)((float)(Desktop.MousePosition.Y - _rectangleAfterPadding.Y + base.ScaledScrollOffset.Y) / num);
		int num5 = num4 * itemsPerRow + num3;
		if (num5 >= Slots.Length || num5 < 0)
		{
			return -1;
		}
		return num5;
	}

	private Point GetSlotCoordinatesByIndex(int index)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		int itemsPerRow = GetItemsPerRow();
		int num = index % itemsPerRow;
		if (index > 0 && (int)InfoDisplay == 1)
		{
			num *= itemsPerRow;
		}
		int y = index / itemsPerRow;
		return new Point(num, y);
	}

	private Point GetSlotCenterPointByIndex(int index)
	{
		Point slotCoordinatesByIndex = GetSlotCoordinatesByIndex(index);
		int slotSize = Style.SlotSize;
		int slotIconSize = Style.SlotIconSize;
		int slotSpacing = Style.SlotSpacing;
		int num = Desktop.ScaleRound((slotSize - slotIconSize) / 2);
		int num2 = Desktop.ScaleRound(slotIconSize);
		int x = _rectangleAfterPadding.X + Desktop.ScaleRound(slotCoordinatesByIndex.X * (slotSize + slotSpacing)) + num + num2 / 2;
		int y = _rectangleAfterPadding.Y + Desktop.ScaleRound(slotCoordinatesByIndex.Y * (slotSize + slotSpacing)) + num + num2 / 2 - _scaledScrollOffset.Y;
		return new Point(x, y);
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		_wasDragging = false;
		int slotIndexAtMousePosition = GetSlotIndexAtMousePosition();
		if (SlotMouseDown != null && !SlotMouseDown(slotIndexAtMousePosition))
		{
			return;
		}
		_mouseDownSlotIndex = slotIndexAtMousePosition;
		_pressedMouseButton = evt.Button;
		if (_mouseDownSlotIndex != -1)
		{
			_mouseDownPosition = Desktop.MousePosition;
			if (Style.ItemStackMouseDownSound != null && Slots[slotIndexAtMousePosition]?.ItemStack != null)
			{
				Desktop.Provider.PlaySound(Style.ItemStackMouseDownSound);
			}
			if ((long)evt.Button == 2 && Slots[slotIndexAtMousePosition]?.ItemStack != null)
			{
				_inGameView.checkForSettingBrush(Slots[slotIndexAtMousePosition]?.ItemStack.Id);
			}
		}
	}

	protected internal override void OnMouseDragMove()
	{
		RefreshMouseOver();
	}

	protected override void OnMouseMove()
	{
		RefreshMouseOver();
		if (!AreItemsDraggable || !base.CapturedMouseButton.HasValue || _mouseDownSlotIndex == -1 || Desktop.IsMouseDragging || _wasDragging || ((long)_pressedMouseButton == 2 && _inGameView.canSetActiveBrushMaterial()))
		{
			return;
		}
		float num = new Vector2(Desktop.MousePosition.X - _mouseDownPosition.X, Desktop.MousePosition.Y - _mouseDownPosition.Y).Length();
		if (!(num < 3f))
		{
			ItemGridSlot itemGridSlot = Slots[_mouseDownSlotIndex];
			if (itemGridSlot?.ItemStack != null)
			{
				_wasDragging = true;
				_dragSlotIndex = _mouseDownSlotIndex;
				ClientItemStack itemStack = Slots[_mouseDownSlotIndex].ItemStack;
				ClientItemBase clientItemBase = _inGameView.Items[itemStack.Id];
				int quantity = (AllowMaxStackDraggableItems ? clientItemBase.MaxStack : itemStack.Quantity);
				ClientItemStack itemStack2 = new ClientItemStack(itemStack.Id, quantity)
				{
					Metadata = itemStack.Metadata
				};
				_tooltip.Stop();
				ItemDragData itemDragData = new ItemDragData(_pressedMouseButton, _mouseDownSlotIndex, itemStack2, InventorySectionId, itemGridSlot.InventorySlotIndex ?? _mouseDownSlotIndex);
				_inGameView.SetupDragAndDropItem(itemDragData);
				Desktop.StartMouseDrag(itemDragData, this);
			}
		}
	}

	protected override void OnMouseEnter()
	{
		RefreshMouseOver();
	}

	protected override void OnMouseLeave()
	{
		_slotIndexForDoubleClick = -1;
		RefreshMouseOver();
	}

	protected internal override void OnMouseDrop(object data, Element draggedElement, out bool accepted)
	{
		int slotIndex = GetSlotIndexAtMousePosition();
		accepted = slotIndex != -1 && data is ItemDragData;
		if (!accepted)
		{
			return;
		}
		ItemDragData itemDragData = (ItemDragData)data;
		bool flag = (long)itemDragData.PressedMouseButton == 3 && Desktop.IsShiftKeyDown;
		bool flag2 = draggedElement == this && slotIndex == itemDragData.ItemGridIndex;
		if (flag && itemDragData.ItemStack.Quantity > 1 && !flag2)
		{
			Point slotCenterPointByIndex = GetSlotCenterPointByIndex(slotIndex);
			slotCenterPointByIndex.X = Desktop.UnscaleRound(slotCenterPointByIndex.X);
			slotCenterPointByIndex.Y = Desktop.UnscaleRound(slotCenterPointByIndex.Y) - Style.SlotSize / 2;
			_quantityPopupSlotIndex = slotIndex;
			_isQuantityPopupOpen = true;
			int startingQuantity = 1;
			if (itemDragData.ItemStack.Quantity > 1)
			{
				startingQuantity = (int)System.Math.Floor((float)itemDragData.ItemStack.Quantity / 2f);
			}
			_inGameView.ItemQuantityPopup.Open(slotCenterPointByIndex, itemDragData.ItemStack.Quantity, startingQuantity, itemDragData.ItemStack.Id, delegate(int quantity)
			{
				_isQuantityPopupOpen = false;
				_quantityPopupSlotIndex = -1;
				_inGameView.SetupDragAndDropItem(null);
				if (quantity != 0)
				{
					itemDragData.ItemStack.Quantity = quantity;
					Dropped?.Invoke(slotIndex, draggedElement, itemDragData);
				}
			});
			_inGameView.SetupCursorFloatingItem();
			return;
		}
		if (Desktop.IsShiftKeyDown)
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
		Dropped?.Invoke(slotIndex, draggedElement, itemDragData);
	}

	protected internal override void OnMouseDragCancel(object data)
	{
		int dragSlotIndex = _dragSlotIndex;
		_dragSlotIndex = -1;
		_inGameView.SetupDragAndDropItem(null);
		DragCancelled?.Invoke(dragSlotIndex, _pressedMouseButton);
	}

	protected internal override void OnMouseDragComplete(Element element, object data)
	{
		SlotMouseDragCompleted?.Invoke(_dragSlotIndex, element, (ItemDragData)data);
		_dragSlotIndex = -1;
	}

	protected internal override void OnMouseDragEnter(object data, Element sourceElement)
	{
		_isMouseDraggingOver = true;
	}

	protected internal override void OnMouseDragExit(object data, Element sourceElement)
	{
		_isMouseDraggingOver = false;
		SlotMouseDragExited?.Invoke(_dragSlotIndex, _mouseOverSlotIndex);
	}

	public void SetItemStacks(ClientItemStack[] itemStacks, int slotOffset = 0)
	{
		for (int i = 0; i < Slots.Length; i++)
		{
			int num = i + slotOffset;
			if (num >= itemStacks.Length)
			{
				break;
			}
			ClientItemStack clientItemStack = itemStacks[num];
			if (clientItemStack != null)
			{
				BuilderTool builderTool = _inGameView.Items[clientItemStack.Id]?.BuilderTool;
				Slots[i] = new ItemGridSlot
				{
					ItemStack = clientItemStack,
					InventorySlotIndex = i + slotOffset,
					Name = Desktop.Provider.GetText((builderTool != null) ? ("builderTools.tools." + builderTool.Id + ".name") : ("items." + clientItemStack.Id + ".name")),
					Description = Desktop.Provider.GetText((builderTool != null) ? ("builderTools.tools." + builderTool.Id + ".description") : ("items." + clientItemStack.Id + ".description"))
				};
			}
			else
			{
				Slots[i] = null;
			}
		}
	}

	public void RefreshMouseOver(bool forceTooltipRefresh = false)
	{
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		int mouseOverSlotIndex = _mouseOverSlotIndex;
		_mouseOverSlotIndex = (((base.IsHovered || _isMouseDraggingOver) && _inGameView.Items != null) ? GetSlotIndexAtMousePosition() : (-1));
		if (mouseOverSlotIndex != _mouseOverSlotIndex && _mouseOverSlotIndex > 0 && Slots[_mouseOverSlotIndex]?.ItemStack != null)
		{
			_inGameView.InGame.Instance.AudioModule.PlayLocalSoundEvent("UI_BUTTONSLIGHT_HOVER");
		}
		if (_mouseOverSlotIndex != mouseOverSlotIndex)
		{
			if (_mouseOverSlotIndex != -1)
			{
				if (mouseOverSlotIndex != -1)
				{
					SlotMouseExited?.Invoke(mouseOverSlotIndex);
				}
				SlotMouseEntered?.Invoke(_mouseOverSlotIndex);
			}
			else
			{
				SlotMouseExited?.Invoke(mouseOverSlotIndex);
			}
		}
		if ((int)InfoDisplay != 0 || (!forceTooltipRefresh && _mouseOverSlotIndex == mouseOverSlotIndex) || _isMouseDraggingOver)
		{
			return;
		}
		if (_mouseOverSlotIndex != -1)
		{
			ItemGridSlot itemGridSlot = Slots[_mouseOverSlotIndex];
			Point slotCenterPointByIndex = GetSlotCenterPointByIndex(_mouseOverSlotIndex);
			if (itemGridSlot?.Name != null)
			{
				_tooltip.UpdateTooltip(slotCenterPointByIndex, null, itemGridSlot.Name, itemGridSlot.Description, itemGridSlot.ItemStack?.Id);
				_tooltip.Start();
			}
			else if (itemGridSlot?.ItemStack != null)
			{
				_tooltip.UpdateTooltip(slotCenterPointByIndex, itemGridSlot.ItemStack);
				_tooltip.Start();
			}
			else
			{
				_tooltip.Stop();
			}
		}
		else
		{
			_tooltip.Stop();
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if (((long)evt.Button == 2 && _inGameView.canSetActiveBrushMaterial()) || !(!_wasDragging && activate) || _dragSlotIndex != -1)
		{
			return;
		}
		int slotIndexAtMousePosition = GetSlotIndexAtMousePosition();
		if (slotIndexAtMousePosition != -1 && slotIndexAtMousePosition == _mouseDownSlotIndex)
		{
			if (SlotDoubleClicking != null && (long)evt.Button == 1 && evt.Clicks == 2 && _slotIndexForDoubleClick == slotIndexAtMousePosition)
			{
				SlotDoubleClicking(slotIndexAtMousePosition);
				return;
			}
			_slotIndexForDoubleClick = slotIndexAtMousePosition;
			ItemGridSlot itemGridSlot = Slots[slotIndexAtMousePosition];
			if (itemGridSlot == null || itemGridSlot.IsActivatable)
			{
				if (Slots[slotIndexAtMousePosition]?.ItemStack != null && Style.ItemStackActivateSound != null)
				{
					Desktop.Provider.PlaySound(Style.ItemStackActivateSound);
				}
				SlotClicking?.Invoke(slotIndexAtMousePosition, evt.Button);
			}
		}
		else
		{
			_slotIndexForDoubleClick = -1;
		}
	}

	protected override void PrepareForDrawSelf()
	{
		//IL_0c85: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c8b: Invalid comparison between Unknown and I4
		//IL_09a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a6: Invalid comparison between Unknown and I4
		base.PrepareForDrawSelf();
		int slotSize = Style.SlotSize;
		int slotIconSize = Style.SlotIconSize;
		int slotSpacing = Style.SlotSpacing;
		int num = Desktop.ScaleRound((float)(slotSize - slotIconSize) / 2f);
		int num2 = Desktop.ScaleRound(slotIconSize);
		int num3 = Desktop.ScaleRound(slotSize);
		Anchor durabilityBarAnchor = Style.DurabilityBarAnchor;
		ColorHsva color = ColorHsva.FromUInt32Color(Style.DurabilityBarColorStart);
		ColorHsva color2 = ColorHsva.FromUInt32Color(Style.DurabilityBarColorEnd);
		if (ShowScrollbar)
		{
			Desktop.Batcher2D.PushScissor(_rectangleAfterPadding);
		}
		for (int i = 0; i < Slots.Length; i++)
		{
			Point slotCoordinatesByIndex = GetSlotCoordinatesByIndex(i);
			int num4 = _rectangleAfterPadding.Y + Desktop.ScaleRound(slotCoordinatesByIndex.Y * (slotSize + slotSpacing)) - _scaledScrollOffset.Y;
			if (num4 > _rectangleAfterPadding.Bottom)
			{
				break;
			}
			int num5 = num4 + num3;
			if (num5 < _rectangleAfterPadding.Top)
			{
				continue;
			}
			Rectangle destRect = new Rectangle(_rectangleAfterPadding.X + Desktop.ScaleRound(slotCoordinatesByIndex.X * (slotSize + slotSpacing)), num4, num3, num3);
			ItemGridSlot itemGridSlot = Slots[i];
			if (itemGridSlot?.ItemStack != null)
			{
				if (itemGridSlot.ItemIcon != null && _inGameView.Items.TryGetValue(itemGridSlot.ItemStack.Id, out var value))
				{
					if (itemGridSlot.Background != null)
					{
						Desktop.Batcher2D.RequestDrawPatch(itemGridSlot.BackgroundPatch, destRect, Desktop.Scale, Slots[i].IsItemIncompatible ? IncompatibleItemStackSlotColor : UInt32Color.White);
					}
					else if (_slotBackgroundPatch != null)
					{
						Desktop.Batcher2D.RequestDrawPatch(_slotBackgroundPatch, destRect, Desktop.Scale, Slots[i].IsItemIncompatible ? IncompatibleItemStackSlotColor : UInt32Color.White);
					}
					if (RenderItemQualityBackground && !itemGridSlot.SkipItemQualityBackground)
					{
						if (_inGameView.InGame.Instance.ServerSettings.ItemQualities[value.QualityIndex].RenderSpecialSlot && (value.Consumable || (value.Utility != null && value.Utility.Usable)))
						{
							Desktop.Batcher2D.RequestDrawPatch(_specialSlotBackgroundPatches[value.QualityIndex], destRect, Desktop.Scale, Slots[i].IsItemIncompatible ? IncompatibleItemStackSlotColor : UInt32Color.White);
						}
						else if (value.BlockId == 0)
						{
							Desktop.Batcher2D.RequestDrawPatch(_slotBackgroundPatches[value.QualityIndex], destRect, Desktop.Scale, Slots[i].IsItemIncompatible ? IncompatibleItemStackSlotColor : UInt32Color.White);
						}
						else
						{
							Desktop.Batcher2D.RequestDrawPatch(_blockSlotBackgroundPatch, destRect, Desktop.Scale, Slots[i].IsItemIncompatible ? IncompatibleItemStackSlotColor : UInt32Color.White);
						}
					}
					if (itemGridSlot.ItemStack != null && itemGridSlot.ItemStack.MaxDurability > 0.0 && itemGridSlot.ItemStack.Durability < 0.0001)
					{
						Desktop.Batcher2D.RequestDrawPatch(_brokenSlotBackgroundOverlayPatch, destRect, Desktop.Scale, itemGridSlot.IsItemIncompatible ? IncompatibleItemStackSlotColor : UInt32Color.White);
					}
				}
				else
				{
					if (itemGridSlot.Background != null)
					{
						Desktop.Batcher2D.RequestDrawPatch(itemGridSlot.BackgroundPatch, destRect, Desktop.Scale, Slots[i].IsItemIncompatible ? IncompatibleItemStackSlotColor : UInt32Color.White);
					}
					else if (_slotBackgroundPatch != null)
					{
						Desktop.Batcher2D.RequestDrawPatch(_slotBackgroundPatch, destRect, Desktop.Scale, Slots[i].IsItemIncompatible ? IncompatibleItemStackSlotColor : UInt32Color.White);
					}
					if (RenderItemQualityBackground && !itemGridSlot.SkipItemQualityBackground)
					{
						Desktop.Batcher2D.RequestDrawPatch(_slotBackgroundPatches[0], destRect, Desktop.Scale, Slots[i].IsItemIncompatible ? IncompatibleItemStackSlotColor : UInt32Color.White);
					}
					int y = _rectangleAfterPadding.Y + Desktop.ScaleRound(slotCoordinatesByIndex.Y * (slotSize + slotSpacing)) + num - _scaledScrollOffset.Y;
					Rectangle destRect2 = new Rectangle(_rectangleAfterPadding.X + Desktop.ScaleRound(slotCoordinatesByIndex.X * (slotSize + slotSpacing)) + num, y, num2, num2);
					Desktop.Batcher2D.RequestDrawTexture(_defaultItemIconPatch.TextureArea.Texture, _defaultItemIconPatch.TextureArea.Rectangle, destRect2, Slots[i].IsItemIncompatible ? IncompatibleItemStackSlotColor : ((i == _dragSlotIndex) ? DraggingItemStackColor : UInt32Color.White));
				}
				if (_durabilityBarTexture != null)
				{
					ClientItemStack itemStack = itemGridSlot.ItemStack;
					if (itemStack.MaxDurability >= 0.0 && itemStack.Durability > 0.0001 && itemStack.Durability < itemStack.MaxDurability)
					{
						float num6 = (float)(itemStack.Durability / itemStack.MaxDurability);
						Rectangle destRect3 = new Rectangle(destRect.X + Desktop.ScaleRound(durabilityBarAnchor.Left.Value), destRect.Bottom - Desktop.ScaleRound(durabilityBarAnchor.Bottom.Value), Desktop.ScaleRound(durabilityBarAnchor.Width.Value), Desktop.ScaleRound(durabilityBarAnchor.Height.Value));
						ColorHsva colorHsva = ColorHsva.Lerp(color, color2, num6);
						if (itemGridSlot.IsItemIncompatible)
						{
							colorHsva.A = 0.14901961f;
						}
						else if (i == _dragSlotIndex)
						{
							colorHsva.A = 25f / 51f;
						}
						Desktop.Batcher2D.RequestDrawPatch(_durabilityBarBackgroundTexture, destRect3, Desktop.Scale, itemGridSlot.IsItemIncompatible ? IncompatibleItemStackColor : UInt32Color.White);
						destRect3.Width = Desktop.ScaleRound((float)durabilityBarAnchor.Width.Value * num6);
						Rectangle sourceRect = new Rectangle(_durabilityBarTexture.Rectangle.X, _durabilityBarTexture.Rectangle.Y, (int)((float)_durabilityBarTexture.Rectangle.Width * num6), _durabilityBarTexture.Rectangle.Height);
						Desktop.Batcher2D.RequestDrawTexture(_durabilityBarTexture.Texture, sourceRect, destRect3, colorHsva.ToUInt32Color());
					}
				}
			}
			else
			{
				UInt32Color uInt32Color = ((itemGridSlot != null && itemGridSlot.IsItemIncompatible) ? IncompatibleItemStackSlotColor : UInt32Color.White);
				if (itemGridSlot?.Background != null)
				{
					Desktop.Batcher2D.RequestDrawPatch(itemGridSlot.BackgroundPatch, destRect, Desktop.Scale, uInt32Color);
				}
				else if (_slotBackgroundPatch != null)
				{
					Desktop.Batcher2D.RequestDrawPatch(_slotBackgroundPatch, destRect, Desktop.Scale, uInt32Color);
				}
				if (_quantityPopupSlotIndex == i)
				{
					Desktop.Batcher2D.RequestDrawPatch(_quantityPopupSlotOverlayPatch, destRect, Desktop.Scale, uInt32Color);
				}
				if (itemGridSlot?.Icon != null)
				{
					int y2 = _rectangleAfterPadding.Y + Desktop.ScaleRound(slotCoordinatesByIndex.Y * (slotSize + slotSpacing)) + num - _scaledScrollOffset.Y;
					Rectangle destRect4 = new Rectangle(_rectangleAfterPadding.X + Desktop.ScaleRound(slotCoordinatesByIndex.X * (slotSize + slotSpacing)) + num, y2, num2, num2);
					Desktop.Batcher2D.RequestDrawTexture(Slots[i].IconTextureArea.Texture, Slots[i].IconTextureArea.Rectangle, destRect4, uInt32Color);
				}
			}
			if ((int)InfoDisplay == 1)
			{
				Rectangle destRect5 = new Rectangle(_rectangleAfterPadding.X + Desktop.ScaleRound(slotCoordinatesByIndex.X * (slotSize + slotSpacing) + slotSpacing - 1) + num3, num4, (SlotsPerRow / GetItemsPerRow() - 1) * (num3 + 1), num3);
				Desktop.Batcher2D.RequestDrawPatch(_infoPaneBackgroundPatch, destRect5, Desktop.Scale, UInt32Color.White);
			}
		}
		for (int j = 0; j < Slots.Length; j++)
		{
			ItemGridSlot itemGridSlot2 = Slots[j];
			if (itemGridSlot2?.ItemIcon != null)
			{
				Point slotCoordinatesByIndex2 = GetSlotCoordinatesByIndex(j);
				int num7 = _rectangleAfterPadding.Y + Desktop.ScaleRound(slotCoordinatesByIndex2.Y * (slotSize + slotSpacing)) + num - _scaledScrollOffset.Y;
				if (num7 > _rectangleAfterPadding.Bottom)
				{
					break;
				}
				int num8 = num7 + num3;
				if (num8 >= _rectangleAfterPadding.Top)
				{
					UInt32Color color3 = (itemGridSlot2.IsItemIncompatible ? IncompatibleItemStackColor : ((j == _dragSlotIndex) ? DraggingItemStackColor : ((itemGridSlot2.ItemStack == null || !(itemGridSlot2.ItemStack.MaxDurability > 0.0) || !(itemGridSlot2.ItemStack.Durability < 0.0001)) ? UInt32Color.White : BrokenItemStackColor)));
					Rectangle destRect6 = new Rectangle(_rectangleAfterPadding.X + Desktop.ScaleRound(slotCoordinatesByIndex2.X * (slotSize + slotSpacing)) + num, num7, num2, num2);
					Desktop.Batcher2D.RequestDrawTexture(itemGridSlot2.ItemIcon.Texture, itemGridSlot2.ItemIcon.Rectangle, destRect6, color3);
				}
			}
		}
		for (int k = 0; k < Slots.Length; k++)
		{
			if (Slots[k]?.ItemStack == null)
			{
				continue;
			}
			Point slotCoordinatesByIndex3 = GetSlotCoordinatesByIndex(k);
			int num9 = _rectangleAfterPadding.Y + Desktop.ScaleRound(slotCoordinatesByIndex3.Y * (slotSize + slotSpacing)) + num - _scaledScrollOffset.Y;
			if (num9 > _rectangleAfterPadding.Bottom)
			{
				break;
			}
			int num10 = num9 + num3;
			if (num10 >= _rectangleAfterPadding.Top)
			{
				if ((int)InfoDisplay == 1)
				{
					float x = (float)(_rectangleAfterPadding.X + Desktop.ScaleRound(slotCoordinatesByIndex3.X * (slotSize + slotSpacing)) + num3) + 15f;
					float y3 = (float)(num10 - num - num3) + 27f * Desktop.Scale;
					Desktop.Batcher2D.RequestDrawText(_regularFont, 14f * Desktop.Scale, Slots[k].Name, new Vector3(x, y3, 0f), InfoPanePrimaryTextColor);
				}
				if (Slots[k].ItemStack.Quantity > 1)
				{
					string text = Slots[k].ItemStack.Quantity.ToString();
					int num11 = _rectangleAfterPadding.X + Desktop.ScaleRound(slotCoordinatesByIndex3.X * (slotSize + slotSpacing)) + num + num2;
					num11 -= Desktop.ScaleRound(_boldFont.CalculateTextWidth(text) * 16f / (float)_boldFont.BaseSize);
					float y4 = (float)(num10 - num) - 26f * Desktop.Scale;
					Desktop.Batcher2D.RequestDrawText(_boldFont, 16f * Desktop.Scale, text, new Vector3(num11, y4, 0f), Slots[k].IsItemIncompatible ? IncompatibleItemStackColor : UInt32Color.White);
				}
			}
		}
		for (int l = 0; l < Slots.Length; l++)
		{
			ItemGridSlot itemGridSlot3 = Slots[l];
			if (itemGridSlot3 == null || (itemGridSlot3?.Overlay == null && _brokenSlotIconOverlayPatch == null))
			{
				continue;
			}
			Point slotCoordinatesByIndex4 = GetSlotCoordinatesByIndex(l);
			int num12 = _rectangleAfterPadding.Y + Desktop.ScaleRound(slotCoordinatesByIndex4.Y * (slotSize + slotSpacing)) - _scaledScrollOffset.Y;
			if (num12 > _rectangleAfterPadding.Bottom)
			{
				break;
			}
			int num13 = num12 + num3;
			if (num13 >= _rectangleAfterPadding.Top)
			{
				Rectangle destRect7 = new Rectangle(_rectangleAfterPadding.X + Desktop.ScaleRound(slotCoordinatesByIndex4.X * (slotSize + slotSpacing)), num12, num3, num3);
				if (itemGridSlot3.ItemStack != null && itemGridSlot3.ItemStack.MaxDurability > 0.0 && itemGridSlot3.ItemStack.Durability < 0.0001)
				{
					Desktop.Batcher2D.RequestDrawPatch(_brokenSlotIconOverlayPatch, destRect7, Desktop.Scale, itemGridSlot3.IsItemIncompatible ? IncompatibleItemStackSlotColor : UInt32Color.White);
				}
				if (itemGridSlot3.Overlay != null)
				{
					Desktop.Batcher2D.RequestDrawPatch(itemGridSlot3.OverlayPatch, destRect7, Desktop.Scale);
				}
			}
		}
		if (ShowScrollbar)
		{
			Desktop.Batcher2D.PopScissor();
		}
	}
}
