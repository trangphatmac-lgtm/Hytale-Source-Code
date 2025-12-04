using System;
using System.Collections.Generic;
using HytaleClient.Data.Items;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class StructuralCraftingPanel : WindowPanel
{
	private Label _titleLabel;

	private ItemGrid _inputItemGrid;

	private ItemGrid _optionsItemGrid;

	private Group _categoryPreview;

	private Label _itemName;

	private ItemPreviewComponent _itemPreview;

	private TextButton _craft1Button;

	private TextButton _craft10Button;

	private TextButton _craftAllButton;

	private PatchStyle _slotBackground;

	private PatchStyle _slotSelectedBackground;

	private PatchStyle _slotSelectedOverlay;

	private PatchStyle _inputSlotIcon;

	private int _selectedSlot = 0;

	private PatchStyle[] _optionIcons;

	private string[] _optionIds;

	public bool[] CompatibleSlots { get; private set; }

	public StructuralCraftingPanel(InGameView inGameView, Element parent = null)
		: base(inGameView, parent)
	{
	}

	public void Build()
	{
		Clear();
		if (!Interface.HasMarkupError)
		{
			Interface.TryGetDocument("InGame/Pages/Inventory/StructuralCraftingPanel.ui", out var document);
			_slotBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotBackground");
			_slotSelectedBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotSelectedBackground");
			_slotSelectedOverlay = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotSelectedOverlay");
			_inputSlotIcon = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "InputSlotIcon");
			UIFragment uIFragment = document.Instantiate(Desktop, this);
			_titleLabel = uIFragment.Get<Label>("TitleLabel");
			_itemPreview = uIFragment.Get<ItemPreviewComponent>("ItemPreview");
			_categoryPreview = uIFragment.Get<Group>("CategoryPreview");
			Interface.TryGetDocument("Common.ui", out var _);
			_inputItemGrid = new ItemGrid(Desktop, uIFragment.Get<Group>("InputContainer"))
			{
				SlotsPerRow = 1,
				RenderItemQualityBackground = false,
				Slots = new ItemGridSlot[1],
				SlotMouseEntered = OnInputSlotMouseEntered,
				SlotMouseExited = OnInputSlotMouseExited
			};
			_inputItemGrid.Style = _inGameView.DefaultItemGridStyle.Clone();
			_inputItemGrid.Style.SlotBackground = null;
			_inputItemGrid.SlotClicking = delegate(int slotIndex, int button)
			{
				_inGameView.HandleInventoryClick(_inventoryWindow.Id, slotIndex, button);
			};
			_inputItemGrid.SlotDoubleClicking = delegate(int slotIndex)
			{
				_inGameView.HandleInventoryDoubleClick(_inventoryWindow.Id, slotIndex);
			};
			_inputItemGrid.Dropped = delegate(int targetSlotIndex, Element sourceItemGrid, ItemGrid.ItemDragData dragData)
			{
				_inGameView.HandleInventoryDragEnd(_inputItemGrid, _inventoryWindow.Id, targetSlotIndex, sourceItemGrid, dragData);
			};
			_inputItemGrid.DragCancelled = delegate(int slotIndex, int button)
			{
				_inGameView.HandleInventoryDropItem(_inventoryWindow.Id, slotIndex, button);
			};
			_optionsItemGrid = new ItemGrid(Desktop, uIFragment.Get<Group>("OptionsContainer"))
			{
				SlotsPerRow = 4,
				Slots = new ItemGridSlot[12],
				AreItemsDraggable = false,
				Style = _inGameView.DefaultItemGridStyle,
				SlotClicking = delegate(int slotIndex, int button)
				{
					//IL_003b: Unknown result type (might be due to invalid IL or missing references)
					//IL_0040: Unknown result type (might be due to invalid IL or missing references)
					//IL_0057: Expected O, but got Unknown
					_selectedSlot = slotIndex;
					Update();
					Layout();
					InventoryPage inventoryPage = _inGameView.InventoryPage;
					int id = _inventoryWindow.Id;
					JObject val = new JObject();
					val.Add("slot", JToken.op_Implicit(slotIndex));
					inventoryPage.SendWindowAction(id, "select", val);
				}
			};
			_craft1Button = uIFragment.Get<TextButton>("Craft1Button");
			_craft1Button.Activating = delegate
			{
				Craft(1);
			};
			_craft10Button = uIFragment.Get<TextButton>("Craft10Button");
			_craft10Button.Activating = delegate
			{
				Craft(10);
			};
			_craftAllButton = uIFragment.Get<TextButton>("CraftAllButton");
			_craftAllButton.Activating = delegate
			{
				Craft(GetCraftableQuantity());
			};
			_itemName = uIFragment.Get<Group>("ItemName").Find<Label>("PanelTitle");
			uIFragment.Get<TextButton>("RecipesButton").Activating = delegate
			{
				RecipeCataloguePopup recipeCataloguePopup = _inGameView.InventoryPage.RecipeCataloguePopup;
				JToken obj = _inventoryWindow.WindowData["id"];
				recipeCataloguePopup.SetupSelectedBench((obj != null) ? obj.ToObject<string>() : null);
				Desktop.SetLayer(2, recipeCataloguePopup);
			};
		}
	}

	protected override void Setup()
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		_titleLabel.Text = Desktop.Provider.GetText(((string)_inventoryWindow.WindowData["name"]) ?? "");
		_inputItemGrid.InventorySectionId = _inventoryWindow.Id;
		_inputItemGrid.Slots = new ItemGridSlot[1];
		JArray val = (JArray)_inventoryWindow.WindowData["options"];
		if (((JContainer)val).Count > 16)
		{
			throw new Exception("Amount of input slots cannot exceed 16");
		}
		_optionIcons = new PatchStyle[((JContainer)val).Count];
		_optionIds = new string[((JContainer)val).Count];
		for (int i = 0; i < _optionIcons.Length; i++)
		{
			string text = (string)val[i][(object)"icon"];
			if (text != null && _inGameView.TryMountAssetTexture(text, out var textureArea))
			{
				_optionIcons[i] = new PatchStyle(textureArea);
			}
			else
			{
				_optionIcons[i] = null;
			}
			_optionIds[i] = (string)val[i][(object)"id"];
		}
		_optionsItemGrid.Slots = new ItemGridSlot[_optionIcons.Length];
		int num = (int)System.Math.Ceiling((float)_optionIcons.Length / (float)_optionsItemGrid.SlotsPerRow);
		int num2 = ((_optionIcons.Length > _optionsItemGrid.SlotsPerRow) ? _optionsItemGrid.SlotsPerRow : _optionIcons.Length);
		_optionsItemGrid.Parent.Anchor.Height = _optionsItemGrid.Style.SlotSize * num + _optionsItemGrid.Style.SlotSpacing * (num - 1);
		_optionsItemGrid.Parent.Anchor.Width = _optionsItemGrid.Style.SlotSize * num2 + _optionsItemGrid.Style.SlotSpacing * (num2 - 1);
		Update();
	}

	protected override void Update()
	{
		if (_inventoryWindow.Inventory[0] == null)
		{
			_inputItemGrid.Slots[0] = new ItemGridSlot
			{
				Icon = _inputSlotIcon
			};
		}
		else
		{
			_inputItemGrid.Slots[0] = new ItemGridSlot(_inventoryWindow.Inventory[0]);
		}
		_inputItemGrid.Layout();
		bool flag = false;
		for (int i = 0; i < _optionsItemGrid.Slots.Length; i++)
		{
			ClientItemStack clientItemStack = _inventoryWindow.Inventory[i + 1];
			ItemGridSlot itemGridSlot;
			if (clientItemStack != null)
			{
				itemGridSlot = (_optionsItemGrid.Slots[i] = new ItemGridSlot(clientItemStack));
				flag = true;
			}
			else
			{
				ItemGridSlot[] slots = _optionsItemGrid.Slots;
				int num = i;
				ItemGridSlot obj = new ItemGridSlot
				{
					Icon = _optionIcons[i]
				};
				ItemGridSlot itemGridSlot2 = obj;
				slots[num] = obj;
				itemGridSlot = itemGridSlot2;
			}
			if (i != _selectedSlot)
			{
				if (itemGridSlot.Icon == null)
				{
					itemGridSlot.Background = _slotBackground;
				}
			}
			else
			{
				itemGridSlot.Background = _slotSelectedBackground;
				itemGridSlot.Overlay = _slotSelectedOverlay;
			}
		}
		_optionsItemGrid.Layout();
		UpdateItemPreview();
		string text = _inventoryWindow.Inventory[_selectedSlot + 1]?.Id;
		_itemName.Text = ((text == null) ? Desktop.Provider.GetText("items." + (string)_inGameView.InventoryWindow.WindowData["blockItemId"] + ".bench.options." + _optionIds[_selectedSlot] + ".name") : Desktop.Provider.GetText("items." + text + ".name"));
		_itemName.Layout();
		if (flag && _inventoryWindow.Inventory[0] != null && text == null && _optionIcons[_selectedSlot] != null)
		{
			_categoryPreview.Background = _optionIcons[_selectedSlot];
		}
		else
		{
			_categoryPreview.Background = null;
		}
		_categoryPreview.Layout();
		int craftableQuantity = GetCraftableQuantity();
		_craft1Button.Disabled = craftableQuantity < 1;
		_craft1Button.Layout();
		_craft10Button.Disabled = craftableQuantity < 10;
		_craft10Button.Layout();
		_craftAllButton.Text = Desktop.Provider.GetText("ui.windows.crafting.craftX", new Dictionary<string, string> { 
		{
			"count",
			Desktop.Provider.FormatNumber(craftableQuantity)
		} });
		_craftAllButton.Disabled = craftableQuantity < 1;
		_craftAllButton.Layout();
	}

	public void UpdateItemPreview()
	{
		string text = _inventoryWindow.Inventory[_selectedSlot + 1]?.Id;
		if (text != null && Desktop.GetLayer(2) == null)
		{
			_itemPreview.SetItemId(text);
		}
		else
		{
			_itemPreview.SetItemId(null);
		}
	}

	private void OnInputSlotMouseEntered(int slotIndex)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		_inGameView.HandleItemSlotMouseEntered(_inventoryWindow.Id, slotIndex);
		if (Desktop.IsMouseDragging)
		{
			return;
		}
		JArray val = (JArray)_inventoryWindow.WindowData["inventoryHints"];
		if (val == null)
		{
			return;
		}
		CompatibleSlots = new bool[_inGameView.StorageStacks.Length + _inGameView.HotbarStacks.Length];
		foreach (JToken item in val)
		{
			CompatibleSlots[(int)item] = true;
		}
		_inGameView.InventoryPage.StoragePanel.UpdateGrid();
		_inGameView.HotbarComponent.SetupGrid();
	}

	private void OnInputSlotMouseExited(int slotIndex)
	{
		_inGameView.HandleItemSlotMouseExited(_inventoryWindow.Id, slotIndex);
		_inGameView.InventoryPage.StoragePanel.UpdateGrid();
		_inGameView.HotbarComponent.SetupGrid();
	}

	public void OnSetStacks()
	{
		Update();
	}

	private void Craft(int quantity)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		InventoryPage inventoryPage = _inGameView.InventoryPage;
		int id = _inventoryWindow.Id;
		JObject val = new JObject();
		val.Add("quantity", JToken.op_Implicit(quantity));
		inventoryPage.SendWindowAction(id, "craftItem", val);
	}

	private int GetCraftableQuantity()
	{
		string text = _inventoryWindow.Inventory[_selectedSlot + 1]?.Id;
		if (text == null || !_inGameView.Items.TryGetValue(text, out var value))
		{
			return 0;
		}
		ClientItemCraftingRecipe.ClientCraftingMaterial clientCraftingMaterial = value.Recipe.Input[0];
		if (clientCraftingMaterial.Quantity == 0)
		{
			return 0;
		}
		int num = ((clientCraftingMaterial.ResourceTypeId != null) ? CountResourceType(clientCraftingMaterial.ResourceTypeId) : CountItem(clientCraftingMaterial.ItemId));
		return (int)System.Math.Floor((float)num / (float)clientCraftingMaterial.Quantity);
	}

	private int CountItem(string itemId)
	{
		ClientItemStack clientItemStack = _inventoryWindow.Inventory[0];
		return (clientItemStack?.Id == itemId) ? clientItemStack.Quantity : 0;
	}

	private int CountResourceType(string resourceTypeId)
	{
		ClientItemStack clientItemStack = _inventoryWindow.Inventory[0];
		if (clientItemStack == null || !_inGameView.Items.TryGetValue(clientItemStack.Id, out var value) || value.ResourceTypes == null)
		{
			return 0;
		}
		ClientItemResourceType[] resourceTypes = value.ResourceTypes;
		foreach (ClientItemResourceType clientItemResourceType in resourceTypes)
		{
			if (clientItemResourceType.Id == resourceTypeId)
			{
				return clientItemResourceType.Quantity * clientItemStack.Quantity;
			}
		}
		return 0;
	}

	public void ResetState()
	{
		_selectedSlot = 0;
		_inputItemGrid.Slots = new ItemGridSlot[0];
		_optionsItemGrid.Slots = new ItemGridSlot[0];
		_itemPreview.SetItemId(null);
		_itemName.Text = "";
		_optionIcons = null;
		_optionIds = null;
		_craft1Button.Disabled = true;
		_craft10Button.Disabled = true;
		_craftAllButton.Disabled = true;
	}
}
