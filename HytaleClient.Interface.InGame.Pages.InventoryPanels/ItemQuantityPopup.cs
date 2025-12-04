using System;
using HytaleClient.Data.Items;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class ItemQuantityPopup : Element
{
	private readonly InGameView _inGameView;

	private int _maxQuantity;

	private int _quantity;

	private NumberField _numberField;

	private Slider _slider;

	private Group _container;

	private ItemGrid _itemIcon;

	private Action<int> _callback;

	public ItemQuantityPopup(InGameView inGame)
		: base(inGame.Desktop, null)
	{
		_inGameView = inGame;
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("InGame/Pages/Inventory/ItemQuantityPopup.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_container = uIFragment.Get<Group>("Container");
		_itemIcon = uIFragment.Get<ItemGrid>("ItemIcon");
		_itemIcon.AreItemsDraggable = false;
		_itemIcon.Slots = new ItemGridSlot[1];
		_itemIcon.Style.SlotBackground = null;
		uIFragment.Get<Button>("ConfirmButton").Activating = Validate;
		uIFragment.Get<Button>("CancelButton").Activating = Dismiss;
		_numberField = uIFragment.Get<NumberField>("NumberField");
		_numberField.ValueChanged = OnNumberFieldChanged;
		_slider = uIFragment.Get<Slider>("Slider");
		_slider.ValueChanged = OnSliderChanged;
	}

	protected override void OnUnmounted()
	{
		_callback = null;
	}

	public override Element HitTest(Point position)
	{
		return base.HitTest(position) ?? this;
	}

	protected internal override void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Invalid comparison between Unknown and I4
		base.OnKeyDown(keycode, repeat);
		if ((int)keycode != 1073741905)
		{
			if ((int)keycode == 1073741906 && _quantity < _maxQuantity)
			{
				_quantity++;
				_slider.Value = _quantity;
				_slider.Layout();
				_numberField.Value = _quantity;
				_itemIcon.Slots[0].ItemStack.Quantity = _quantity;
			}
		}
		else if (_quantity > 0)
		{
			_quantity--;
			_slider.Value = _quantity;
			_slider.Layout();
			_numberField.Value = _quantity;
			_itemIcon.Slots[0].ItemStack.Quantity = _quantity;
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if (!_container.RectangleAfterPadding.Contains(Desktop.MousePosition))
		{
			Dismiss();
		}
	}

	private void OnNumberFieldChanged()
	{
		if (_numberField.HasValidValue)
		{
			_slider.Value = (int)_numberField.Value;
			_slider.Layout();
			_quantity = _slider.Value;
			_itemIcon.Slots[0].ItemStack.Quantity = _quantity;
		}
	}

	private void OnSliderChanged()
	{
		_numberField.Value = _slider.Value;
		_quantity = _slider.Value;
		_itemIcon.Slots[0].ItemStack.Quantity = _quantity;
	}

	protected internal override void Validate()
	{
		_callback(_quantity);
		Desktop.SetTransientLayer(null);
	}

	protected internal override void Dismiss()
	{
		_callback(0);
		Desktop.SetTransientLayer(null);
	}

	public void Open(Point slotPosition, int maxQuantity, int startingQuantity, string itemId, Action<int> callback)
	{
		_callback = callback;
		_quantity = startingQuantity;
		_maxQuantity = maxQuantity;
		_slider.Max = maxQuantity;
		_slider.Value = startingQuantity;
		_numberField.Format.MaxValue = maxQuantity;
		_numberField.Value = startingQuantity;
		_itemIcon.SetItemStacks(new ClientItemStack[1]
		{
			new ClientItemStack(itemId, startingQuantity)
		});
		Anchor.Left = slotPosition.X - _container.Anchor.Width / 2;
		Anchor.Top = slotPosition.Y + _container.Padding.Top - _container.Anchor.Height - 8;
		Desktop.SetTransientLayer(this);
	}
}
