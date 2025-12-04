using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class BasicCraftingPanel : WindowPanel
{
	private class CraftableItem
	{
		public string ItemId;

		public string Name;

		public int ItemLevel;

		public string OutputItemId;

		public int OutputQuantity;

		public string Set;

		public int CraftableAmount;

		public List<CraftingIngredient> Ingredients;
	}

	private class CraftingIngredient
	{
		public int Needs;

		public int Has;

		public string ItemId;

		public string ResourceTypeId;
	}

	private Label _titleLabel;

	private Label _categoryLabel;

	private TextField _searchField;

	private ItemGrid _itemList;

	private TabNavigation _categoryTabs;

	private ClientCraftingCategory[] _categories;

	private string _selectedItem;

	private Label _itemName;

	private ItemPreviewComponent _itemPreview;

	private Group _itemRecipePanel;

	private Group _itemInfoPanel;

	private TextButton _craft1Button;

	private TextButton _craft10Button;

	private TextButton _craftAllButton;

	private CraftableItem[] _craftableItems;

	private ProgressBar _progressBar;

	private ItemGrid.ItemGridStyle _itemGridStyle;

	private PatchStyle _slotBackground;

	private PatchStyle _slotSelectedBackground;

	private PatchStyle _slotUncraftableBackground;

	private PatchStyle _slotUncraftableSelectedBackground;

	private PatchStyle _slotSelectedOverlay;

	private Tuple<string, string> _previousSelectedCatgeory;

	public BasicCraftingPanel(InGameView inGameView, Element parent = null)
		: base(inGameView, parent)
	{
	}

	public void Build()
	{
		Clear();
		if (Interface.HasMarkupError)
		{
			return;
		}
		Interface.TryGetDocument("InGame/Pages/Inventory/BasicCraftingPanel.ui", out var document);
		_itemGridStyle = document.ResolveNamedValue<ItemGrid.ItemGridStyle>(Desktop.Provider, "ItemGridStyle");
		_slotBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotBackground");
		_slotSelectedBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotSelectedBackground");
		_slotUncraftableBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotUncraftableBackground");
		_slotUncraftableSelectedBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotUncraftableSelectedBackground");
		_slotSelectedOverlay = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotSelectedOverlay");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_titleLabel = uIFragment.Get<Label>("TitleLabel");
		_categoryLabel = uIFragment.Get<Label>("CategoryLabel");
		_itemPreview = uIFragment.Get<ItemPreviewComponent>("ItemPreview");
		_searchField = uIFragment.Get<Group>("Search").Find<TextField>("SearchField");
		_searchField.ValueChanged = delegate
		{
			BuildItemList();
			SetupItemList();
		};
		_categoryTabs = uIFragment.Get<TabNavigation>("Categories");
		_categoryTabs.SelectedTabChanged = delegate
		{
			_previousSelectedCatgeory = Tuple.Create(Extensions.Value<string>((IEnumerable<JToken>)_inventoryWindow.WindowData["id"]), _categoryTabs.SelectedTab);
			OnCategoryChanged();
			_selectedItem = null;
			_searchField.Value = "";
			_itemList.SetScroll(0, 0);
			BuildItemList();
			UpdateItemInfo();
		};
		_itemList = uIFragment.Get<ItemGrid>("Items");
		_itemList.Style.SlotBackground = _slotBackground;
		_itemList.Style.ItemStackMouseDownSound = null;
		_itemList.AreItemsDraggable = false;
		_itemList.RenderItemQualityBackground = false;
		_itemList.Slots = new ItemGridSlot[0];
		_itemList.SlotClicking = delegate(int slotIndex, int button)
		{
			string id = _itemList.Slots[slotIndex].ItemStack.Id;
			if (Interface.App.InGame.Instance.Chat.IsOpen && Desktop.IsShiftKeyDown)
			{
				_inGameView.ChatComponent.InsertItemTag(id);
			}
			else
			{
				_selectedItem = id;
				BuildItemList();
				UpdateItemInfo();
			}
		};
		_progressBar = uIFragment.Get<ProgressBar>("ProgressBar");
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
			Craft();
		};
		_itemName = uIFragment.Get<Group>("ItemName").Find<Label>("PanelTitle");
		_itemRecipePanel = uIFragment.Get<Group>("ItemRecipe");
		_itemInfoPanel = uIFragment.Get<Group>("ItemInfo");
		uIFragment.Get<TextButton>("RecipesButton").Activating = delegate
		{
			RecipeCataloguePopup recipeCataloguePopup = _inGameView.InventoryPage.RecipeCataloguePopup;
			JToken obj = _inventoryWindow.WindowData["id"];
			recipeCataloguePopup.SetupSelectedBench((obj != null) ? obj.ToObject<string>() : null);
			Desktop.SetLayer(2, recipeCataloguePopup);
		};
	}

	protected override void OnUnmounted()
	{
		if (_inGameView.InventoryPage.RecipeCataloguePopup.IsMounted)
		{
			Desktop.ClearLayer(2);
		}
	}

	protected override void Setup()
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Invalid comparison between Unknown and I4
		_titleLabel.Text = Desktop.Provider.GetText(((string)_inventoryWindow.WindowData["name"]) ?? "");
		if (_inGameView.InventoryPage.IsFieldcraft && (int)_inGameView.InGame.Instance.GameMode == 0)
		{
			_categories = _inGameView.InventoryPage.FieldcraftCategories.ToArray();
		}
		else
		{
			JArray val = _inventoryWindow.WindowData["categories"].ToObject<JArray>();
			_categories = new ClientCraftingCategory[((JContainer)val).Count];
			for (int i = 0; i < _categories.Length; i++)
			{
				_categories[i] = new ClientCraftingCategory
				{
					Id = val[i][(object)"id"].ToObject<string>(),
					Icon = val[i][(object)"icon"].ToObject<string>()
				};
			}
		}
		_searchField.Value = "";
		TabNavigation.Tab[] array = new TabNavigation.Tab[_categories.Length];
		for (int j = 0; j < array.Length; j++)
		{
			TabNavigation.Tab tab = new TabNavigation.Tab
			{
				Id = _categories[j].Id
			};
			if (_inGameView.TryMountAssetTexture(_categories[j].Icon, out var textureArea))
			{
				tab.Icon = new PatchStyle(textureArea);
			}
			array[j] = tab;
		}
		_categoryTabs.Visible = _categories.Length > 1;
		_categoryTabs.Tabs = array;
		if (_previousSelectedCatgeory != null && _previousSelectedCatgeory.Item1 == Extensions.Value<string>((IEnumerable<JToken>)_inventoryWindow.WindowData["id"]) && _categories.Any((ClientCraftingCategory cat) => cat.Id == _previousSelectedCatgeory.Item2))
		{
			_categoryTabs.SelectedTab = _previousSelectedCatgeory.Item2;
		}
		else
		{
			_categoryTabs.SelectedTab = _categories[0].Id;
			_previousSelectedCatgeory = Tuple.Create(Extensions.Value<string>((IEnumerable<JToken>)_inventoryWindow.WindowData["id"]), _categoryTabs.SelectedTab);
		}
		_selectedItem = null;
		OnCategoryChanged();
		BuildItemList();
		UpdateItemInfo();
	}

	private void Craft(int quantity = -1)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		if (quantity == -1)
		{
			quantity = _craftableItems.First((CraftableItem item) => item.ItemId == _selectedItem).CraftableAmount;
		}
		InventoryPage inventoryPage = _inGameView.InventoryPage;
		int id = _inventoryWindow.Id;
		JObject val = new JObject();
		val.Add("itemId", JToken.op_Implicit(_selectedItem));
		val.Add("quantity", JToken.op_Implicit(quantity));
		inventoryPage.SendWindowAction(id, "craftItem", val);
	}

	public void OnSetStacks()
	{
		BuildItemList();
		if (_selectedItem != null)
		{
			UpdateItemInfo();
		}
	}

	private void OnCategoryChanged()
	{
		_categoryLabel.Text = _categoryTabs.SelectedTab;
		_categoryLabel.Parent.Layout();
	}

	public void BuildItemList()
	{
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Invalid comparison between Unknown and I4
		string text = _inventoryWindow.WindowData["id"].ToObject<string>();
		List<CraftableItem> list = new List<CraftableItem>();
		Dictionary<string, ClientItemBase>.KeyCollection keys = _inGameView.Items.Keys;
		string text2 = _searchField.Value.Trim().ToLower();
		string[] array = ((text2 != "") ? (from w in text2.Split(new char[1] { ' ' })
			select w.Trim() into w
			where w != ""
			select w).ToArray() : null);
		foreach (string item in keys)
		{
			ClientItemBase clientItemBase = _inGameView.Items[item];
			if (clientItemBase.Recipe == null || clientItemBase.Recipe.BenchRequirement == null)
			{
				continue;
			}
			bool flag = false;
			ClientItemCraftingRecipe.ClientBenchRequirement[] benchRequirement = clientItemBase.Recipe.BenchRequirement;
			foreach (ClientItemCraftingRecipe.ClientBenchRequirement clientBenchRequirement in benchRequirement)
			{
				if (clientBenchRequirement.Id == text && (int)clientBenchRequirement.Type == 0)
				{
					if (array != null)
					{
						string[] array2 = (from w in text2.Split(new char[1] { ' ' })
							select w.Trim() into w
							where w != ""
							select w).ToArray();
						string text3 = Desktop.Provider.GetText("items." + clientItemBase.Id + ".name").ToLowerInvariant();
						flag = true;
						string[] array3 = array2;
						foreach (string value in array3)
						{
							if (!text3.Contains(value))
							{
								flag = false;
								break;
							}
						}
						break;
					}
					if (_categoryTabs.SelectedTab == "All")
					{
						flag = true;
						break;
					}
					if (clientBenchRequirement.Categories != null)
					{
						string[] categories = clientBenchRequirement.Categories;
						foreach (string text4 in categories)
						{
							if (text4 == _categoryTabs.SelectedTab)
							{
								flag = true;
								break;
							}
						}
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (flag && TryGetCraftableItem(clientItemBase, out var craftableItem))
			{
				list.Add(craftableItem);
			}
		}
		_craftableItems = (from item in list
			orderby item.CraftableAmount > 0 descending, item.ItemLevel, item.Set, item.Name
			select item).ToArray();
		if (_selectedItem == null && _craftableItems.Length != 0)
		{
			_selectedItem = _craftableItems[0].ItemId;
		}
		SetupItemList();
	}

	private bool TryGetCraftableItem(ClientItemBase item, out CraftableItem craftableItem)
	{
		if (item.Recipe == null || item.Recipe.Output == null || item.Recipe.Output.Length == 0 || (item.Recipe.KnowledgeRequired && !_inGameView.InventoryPage.KnownCraftingRecipes.ContainsKey(item.Id)))
		{
			craftableItem = null;
			return false;
		}
		List<CraftingIngredient> list = new List<CraftingIngredient>();
		int num = int.MaxValue;
		if (item.Recipe.Input != null)
		{
			ClientItemCraftingRecipe.ClientCraftingMaterial[] input = item.Recipe.Input;
			foreach (ClientItemCraftingRecipe.ClientCraftingMaterial clientCraftingMaterial in input)
			{
				if (clientCraftingMaterial.Quantity > 0)
				{
					CraftingIngredient item2 = new CraftingIngredient
					{
						Needs = clientCraftingMaterial.Quantity,
						Has = CountMaterial(clientCraftingMaterial),
						ItemId = clientCraftingMaterial.ItemId,
						ResourceTypeId = clientCraftingMaterial.ResourceTypeId
					};
					int num2 = (int)System.Math.Floor((float)CountMaterial(clientCraftingMaterial) / (float)clientCraftingMaterial.Quantity);
					if (num == -1 || num2 < num)
					{
						num = num2;
					}
					list.Add(item2);
				}
			}
		}
		if (list.Count == 0)
		{
			num = 1;
		}
		craftableItem = new CraftableItem
		{
			ItemId = item.Id,
			Name = Desktop.Provider.GetText("items." + item.Id + ".name"),
			ItemLevel = item.ItemLevel,
			OutputItemId = item.Recipe.Output[0].ItemId,
			OutputQuantity = item.Recipe.Output[0].Quantity,
			Set = item.Set,
			CraftableAmount = num,
			Ingredients = list
		};
		return true;
	}

	private void SetupItemList()
	{
		CraftableItem[] craftableItems = _craftableItems;
		_itemList.Slots = new ItemGridSlot[craftableItems.Length];
		for (int i = 0; i < craftableItems.Length; i++)
		{
			CraftableItem craftableItem = craftableItems[i];
			_itemList.Slots[i] = new ItemGridSlot(new ClientItemStack(craftableItem.ItemId, craftableItem.OutputQuantity))
			{
				IsItemIncompatible = (craftableItem.CraftableAmount == 0),
				Background = ((_selectedItem != craftableItem.ItemId) ? _slotUncraftableBackground : ((craftableItem.CraftableAmount == 0) ? _slotUncraftableSelectedBackground : _slotSelectedBackground)),
				Overlay = ((_selectedItem == craftableItem.ItemId) ? _slotSelectedOverlay : null),
				IsActivatable = (_selectedItem != craftableItem.ItemId)
			};
		}
		_itemList.Layout();
	}

	private void UpdateItemInfo()
	{
		_itemRecipePanel.Clear();
		if (_selectedItem != null && _inGameView.Items.TryGetValue(_selectedItem, out var value) && TryGetCraftableItem(value, out var craftableItem))
		{
			Interface.TryGetDocument("InGame/Pages/Inventory/BasicCraftingIngredient.ui", out var document);
			PatchStyle patchStyle = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotInvalidBackground");
			PatchStyle patchStyle2 = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotValidBackground");
			UInt32Color uInt32Color = document.ResolveNamedValue<UInt32Color>(Interface, "ValidColor");
			UInt32Color uInt32Color2 = document.ResolveNamedValue<UInt32Color>(Interface, "InvalidColor");
			ItemGrid.ItemGridStyle itemGridStyle = _itemGridStyle.Clone();
			itemGridStyle.SlotBackground = null;
			for (int i = 0; i < craftableItem.Ingredients.Count; i++)
			{
				CraftingIngredient craftingIngredient = craftableItem.Ingredients[i];
				UIFragment uIFragment = document.Instantiate(Desktop, _itemRecipePanel);
				uIFragment.Get<Group>("Container").Anchor.Top = itemGridStyle.SlotSize * i + itemGridStyle.SlotSpacing * i;
				uIFragment.Get<Group>("SlotBackground").Background = ((craftingIngredient.Has >= craftingIngredient.Needs) ? patchStyle2 : patchStyle);
				uIFragment.Get<Label>("NameLabel").Text = Desktop.Provider.GetText((craftingIngredient.ItemId != null) ? ("items." + craftingIngredient.ItemId + ".name") : ("resourceTypes." + craftingIngredient.ResourceTypeId + ".name"));
				Label label = uIFragment.Get<Label>("QuantityLabel");
				label.Style = label.Style.Clone();
				label.Style.TextColor = ((craftingIngredient.Has >= craftingIngredient.Needs) ? uInt32Color : uInt32Color2);
				label.Text = $"{craftingIngredient.Has}/{craftingIngredient.Needs}";
			}
			ItemGrid itemGrid = new ItemGrid(Desktop, _itemRecipePanel);
			itemGrid.SlotsPerRow = 1;
			itemGrid.Style = itemGridStyle;
			itemGrid.RenderItemQualityBackground = false;
			itemGrid.AreItemsDraggable = false;
			itemGrid.Slots = new ItemGridSlot[craftableItem.Ingredients.Count];
			ItemGrid itemGrid2 = itemGrid;
			for (int j = 0; j < craftableItem.Ingredients.Count; j++)
			{
				CraftingIngredient craftingIngredient2 = craftableItem.Ingredients[j];
				if (craftingIngredient2.ItemId == null)
				{
					PatchStyle icon = null;
					if (_inGameView.InventoryPage.ResourceTypes.TryGetValue(craftingIngredient2.ResourceTypeId, out var value2) && _inGameView.TryMountAssetTexture(value2.Icon, out var textureArea))
					{
						icon = new PatchStyle(textureArea);
					}
					itemGrid2.Slots[j] = new ItemGridSlot
					{
						Name = Desktop.Provider.GetText("ui.items.resourceTypeTooltip.name", new Dictionary<string, string> { 
						{
							"name",
							Desktop.Provider.GetText("resourceTypes." + craftingIngredient2.ResourceTypeId + ".name")
						} }),
						Description = Desktop.Provider.GetText("resourceTypes." + craftingIngredient2.ResourceTypeId + ".description", null, returnFallback: false),
						Icon = icon
					};
				}
				else
				{
					itemGrid2.Slots[j] = new ItemGridSlot(new ClientItemStack(craftingIngredient2.ItemId));
				}
			}
			itemGrid2.Layout();
			_itemRecipePanel.Layout();
			_itemName.Text = Desktop.Provider.GetText("items." + _selectedItem + ".name");
			_craft1Button.Disabled = craftableItem.CraftableAmount < 1;
			_craft10Button.Disabled = craftableItem.CraftableAmount < 10;
			_craftAllButton.Disabled = craftableItem.CraftableAmount < 1;
		}
		else
		{
			_itemName.Text = "";
			_craft1Button.Disabled = true;
			_craft10Button.Disabled = true;
			_craftAllButton.Disabled = true;
		}
		UpdateItemPreview();
		_itemInfoPanel.Layout();
	}

	public void UpdateItemPreview()
	{
		if (_selectedItem != null && Desktop.GetLayer(2) == null)
		{
			_itemPreview.SetItemId(_selectedItem);
		}
		else
		{
			_itemPreview.SetItemId(null);
		}
	}

	private int CountMaterial(ClientItemCraftingRecipe.ClientCraftingMaterial material)
	{
		return (material.ResourceTypeId != null) ? CountResourceType(material.ResourceTypeId) : CountItem(material.ItemId);
	}

	private int CountItem(string itemId)
	{
		int num = 0;
		ClientItemStack[] storageStacks = _inGameView.StorageStacks;
		foreach (ClientItemStack clientItemStack in storageStacks)
		{
			if (clientItemStack?.Id == itemId)
			{
				num += clientItemStack.Quantity;
			}
		}
		ClientItemStack[] hotbarStacks = _inGameView.HotbarStacks;
		foreach (ClientItemStack clientItemStack2 in hotbarStacks)
		{
			if (clientItemStack2?.Id == itemId)
			{
				num += clientItemStack2.Quantity;
			}
		}
		return num;
	}

	private int CountResourceType(string resourceTypeId)
	{
		int num = 0;
		ClientItemStack[] storageStacks = _inGameView.StorageStacks;
		foreach (ClientItemStack clientItemStack in storageStacks)
		{
			if (clientItemStack == null || !_inGameView.Items.TryGetValue(clientItemStack.Id, out var value) || value.ResourceTypes == null)
			{
				continue;
			}
			ClientItemResourceType[] resourceTypes = value.ResourceTypes;
			foreach (ClientItemResourceType clientItemResourceType in resourceTypes)
			{
				if (clientItemResourceType.Id == resourceTypeId)
				{
					num += clientItemResourceType.Quantity * clientItemStack.Quantity;
				}
			}
		}
		ClientItemStack[] hotbarStacks = _inGameView.HotbarStacks;
		foreach (ClientItemStack clientItemStack2 in hotbarStacks)
		{
			if (clientItemStack2 == null || !_inGameView.Items.TryGetValue(clientItemStack2.Id, out var value2) || value2.ResourceTypes == null)
			{
				continue;
			}
			ClientItemResourceType[] resourceTypes2 = value2.ResourceTypes;
			foreach (ClientItemResourceType clientItemResourceType2 in resourceTypes2)
			{
				if (clientItemResourceType2.Id == resourceTypeId)
				{
					num += clientItemResourceType2.Quantity * clientItemStack2.Quantity;
				}
			}
		}
		return num;
	}

	public void ResetState()
	{
		_previousSelectedCatgeory = null;
	}
}
