using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Data.Items;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class DiagramCraftingPanel : WindowPanel
{
	private Label _titleLabel;

	private Label _categoryLabel;

	private TabNavigation _categoryTabs;

	private Group _itemCategoriesContainer;

	private Group _itemDiagramGroup;

	private PatchStyle _itemCategoryButtonPatchStyle;

	private PatchStyle _itemCategoryButtonActivePatchStyle;

	private Anchor _itemCategoryButtonAnchor;

	private ItemGrid _inputItemGrid;

	private ItemGrid _outputItemGrid;

	private Label _nameLabel;

	private Label _descriptionLabel;

	private Label _weaponDamageLabel;

	private Label _weaponDpsLabel;

	private Label _weaponSpeedLabel;

	private Label _weaponTypeLabel;

	private Label _weaponRatingLabel;

	private ProgressBar _progressBar;

	private TextButton _craftButton;

	private ClientCraftingCategory[] _categories;

	private Tuple<string, string> _selectedCategory;

	private int? _hoveredInputSlot;

	private PatchStyle _validItemGridSlotBackground;

	private PatchStyle _invalidItemGridSlotBackground;

	private PatchStyle _activeItemGridSlotBackground;

	private PatchStyle _lockedSlotIcon;

	private PatchStyle _unlockedSlotIcon;

	private Tuple<string, string, string> _previousSelectedCatgeory;

	public HashSet<int>[] InventoryHints { get; private set; }

	private ClientCraftingItemCategory SelectedItemCategory => _categories.First((ClientCraftingCategory category) => category.Id == _selectedCategory.Item1).ItemCategories.First((ClientCraftingItemCategory category) => category.Id == _selectedCategory.Item2);

	public DiagramCraftingPanel(InGameView inGameView, Element parent = null)
		: base(inGameView, parent)
	{
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Pages/Inventory/DiagramCraftingPanel.ui", out var document);
		_activeItemGridSlotBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotPatchActive");
		_validItemGridSlotBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotPatchValid");
		_invalidItemGridSlotBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotPatchInvalid");
		_lockedSlotIcon = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotLockedIcon");
		_unlockedSlotIcon = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SlotUnlockedIcon");
		_itemCategoryButtonPatchStyle = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "ItemCategoryButtonBackground");
		_itemCategoryButtonActivePatchStyle = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "ItemCategoryButtonActiveBackground");
		_itemCategoryButtonAnchor = document.ResolveNamedValue<Anchor>(Desktop.Provider, "ItemCategoryButtonAnchor");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_titleLabel = uIFragment.Get<Label>("TitleLabel");
		_categoryLabel = uIFragment.Get<Label>("CategoryLabel");
		_categoryTabs = uIFragment.Get<TabNavigation>("Categories");
		_categoryTabs.SelectedTabChanged = delegate
		{
			ClientCraftingCategory clientCraftingCategory = _categories.First((ClientCraftingCategory c) => c.Id == _categoryTabs.SelectedTab);
			SelectCategory(clientCraftingCategory.Id, clientCraftingCategory.ItemCategories[0].Id);
		};
		_itemCategoriesContainer = uIFragment.Get<Group>("ItemCategories");
		_itemDiagramGroup = uIFragment.Get<Group>("ItemDiagram");
		_craftButton = uIFragment.Get<TextButton>("CraftButton");
		_craftButton.Activating = delegate
		{
			_inGameView.InventoryPage.SendWindowAction(_inventoryWindow.Id, "craftItem", null);
		};
		_progressBar = uIFragment.Get<ProgressBar>("ProgressBar");
		_nameLabel = uIFragment.Get<Label>("PanelTitle");
		_descriptionLabel = uIFragment.Get<Label>("Description");
		_weaponDamageLabel = uIFragment.Get<Label>("WeaponDamage");
		_weaponDpsLabel = uIFragment.Get<Label>("WeaponDPS");
		_weaponSpeedLabel = uIFragment.Get<Label>("WeaponSpeed");
		_weaponTypeLabel = uIFragment.Get<Label>("WeaponType");
		_weaponRatingLabel = uIFragment.Get<Label>("WeaponRating");
		_inputItemGrid = uIFragment.Get<ItemGrid>("InputItemGrid");
		_inputItemGrid.Slots = new ItemGridSlot[0];
		_inputItemGrid.SlotMouseEntered = delegate(int index)
		{
			_inGameView.HandleItemSlotMouseEntered(_inventoryWindow.Id, index);
			_hoveredInputSlot = index;
			_inGameView.InventoryPage.StoragePanel.UpdateGrid();
			_inGameView.HotbarComponent.SetupGrid();
			UpdateDiagramState();
		};
		_inputItemGrid.SlotMouseExited = delegate(int index)
		{
			_inGameView.HandleItemSlotMouseExited(_inventoryWindow.Id, index);
			_hoveredInputSlot = null;
			_inGameView.InventoryPage.StoragePanel.UpdateGrid();
			_inGameView.HotbarComponent.SetupGrid();
			UpdateDiagramState();
		};
		_outputItemGrid = uIFragment.Get<ItemGrid>("OutputItemGrid");
		_outputItemGrid.Slots = new ItemGridSlot[1];
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

	protected override void Update()
	{
		ClientCraftingItemCategory selectedItemCategory = SelectedItemCategory;
		JArray val = _inventoryWindow.WindowData["slots"].ToObject<JArray>();
		int num = -1;
		bool flag = false;
		_inputItemGrid.Slots = new ItemGridSlot[1 + selectedItemCategory.Slots];
		for (int i = 0; i < _inputItemGrid.Slots.Length; i++)
		{
			ClientItemStack clientItemStack = _inventoryWindow.Inventory[i];
			if (clientItemStack != null)
			{
				num = i;
				if (i < ((JContainer)val).Count)
				{
					JToken val2 = val[i];
					JToken obj = val2[(object)"requiredAmount"];
					int? num2 = ((obj != null) ? new int?(obj.ToObject<int>()) : null);
					if (!num2.HasValue || num2.GetValueOrDefault() == -1 || !(num2 <= clientItemStack.Quantity))
					{
						flag = true;
					}
					_inputItemGrid.Slots[i] = new ItemGridSlot
					{
						ItemStack = clientItemStack,
						Background = ((num2.HasValue && num2.GetValueOrDefault() != -1 && num2 <= clientItemStack.Quantity) ? _validItemGridSlotBackground : _invalidItemGridSlotBackground)
					};
				}
			}
			else if (num + 1 == i && !flag)
			{
				_inputItemGrid.Slots[i] = new ItemGridSlot
				{
					Icon = _unlockedSlotIcon,
					Background = _activeItemGridSlotBackground
				};
			}
			else
			{
				_inputItemGrid.Slots[i] = new ItemGridSlot
				{
					Icon = _lockedSlotIcon
				};
			}
		}
		_inputItemGrid.Anchor.Width = _inputItemGrid.Slots.Length * _inputItemGrid.Style.SlotSize + (_inputItemGrid.Slots.Length - 1) * _inputItemGrid.Style.SlotSpacing;
		InventoryHints = new HashSet<int>[_inputItemGrid.Slots.Length];
		ClientItemStack clientItemStack2 = _inventoryWindow.Inventory[_inventoryWindow.Inventory.Length - 1];
		ClientItemBase value = null;
		if (clientItemStack2 != null && (clientItemStack2.Id == "Unknown" || _inGameView.Items.TryGetValue(clientItemStack2.Id, out value)))
		{
			if (clientItemStack2.Id == "Unknown")
			{
				_nameLabel.Text = "???";
				_descriptionLabel.Text = "";
				_outputItemGrid.RenderItemQualityBackground = false;
				_outputItemGrid.Slots[0] = new ItemGridSlot
				{
					Name = "???",
					ItemStack = new ClientItemStack("???")
				};
			}
			else
			{
				_nameLabel.Text = Desktop.Provider.GetText("items." + value.Id + ".name");
				_descriptionLabel.Text = Desktop.Provider.GetText("items." + value.Id + ".description", null, returnFallback: false) ?? "";
				_outputItemGrid.Slots[0] = new ItemGridSlot(clientItemStack2);
				_outputItemGrid.RenderItemQualityBackground = true;
				_outputItemGrid.Layout();
			}
		}
		else
		{
			_nameLabel.Text = "";
			_descriptionLabel.Text = "";
			_outputItemGrid.Slots[0] = null;
		}
		if (value != null && value.Weapon != null && clientItemStack2.Id != "Unknown")
		{
			_weaponDamageLabel.Text = "7-13";
			_weaponDpsLabel.Text = "(9.8 DPS)";
			_weaponSpeedLabel.Text = "Very Slow";
			_weaponTypeLabel.Text = "2-Hand Sword";
			_weaponRatingLabel.Text = "15/15";
		}
		else
		{
			_weaponDamageLabel.Text = "";
			_weaponDpsLabel.Text = "";
			_weaponSpeedLabel.Text = "";
			_weaponTypeLabel.Text = "";
			_weaponRatingLabel.Text = "";
		}
		bool flag2 = false;
		JToken val3 = default(JToken);
		if (_inventoryWindow.WindowData.TryGetValue("progress", ref val3) && val3.ToObject<float>() < 1f)
		{
			flag2 = true;
			_progressBar.Value = val3.ToObject<float>();
		}
		else
		{
			_progressBar.Value = 0f;
		}
		bool flag3 = true;
		for (int j = 0; j < ((((JContainer)val).Count == 1) ? 1 : (((JContainer)val).Count - 1)); j++)
		{
			JObject val4 = val[j].ToObject<JObject>();
			InventoryHints[j] = new HashSet<int>();
			JToken obj2 = val4["inventoryHints"];
			JArray val5 = ((obj2 != null) ? obj2.ToObject<JArray>() : null);
			if (val5 != null)
			{
				foreach (JToken item in val5)
				{
					InventoryHints[j].Add(item.ToObject<int>());
				}
			}
			JToken obj3 = val4["requiredAmount"];
			int? num3 = ((obj3 != null) ? new int?(obj3.ToObject<int>()) : null);
			if (num3.HasValue && num3.GetValueOrDefault() != -1 && (_inventoryWindow.Inventory[j] == null || num3 > _inventoryWindow.Inventory[j].Quantity))
			{
				flag3 = false;
			}
		}
		_craftButton.Disabled = flag2 || !flag3 || _outputItemGrid.Slots[0] == null;
		UpdateDiagramState();
		Layout();
		if (_inputItemGrid.IsHovered)
		{
			_inGameView.InventoryPage.StoragePanel.UpdateGrid();
			_inGameView.HotbarComponent.SetupGrid();
		}
	}

	protected override void Setup()
	{
		_titleLabel.Text = Desktop.Provider.GetText(((string)_inventoryWindow.WindowData["name"]) ?? "");
		JArray val = _inventoryWindow.WindowData["categories"].ToObject<JArray>();
		_categories = new ClientCraftingCategory[((JContainer)val).Count];
		for (int i = 0; i < _categories.Length; i++)
		{
			JArray val2 = val[i][(object)"itemCategories"].ToObject<JArray>();
			_categories[i] = new ClientCraftingCategory
			{
				Id = val[i][(object)"id"].ToObject<string>(),
				Icon = val[i][(object)"icon"].ToObject<string>(),
				ItemCategories = new ClientCraftingItemCategory[((JContainer)val2).Count]
			};
			for (int j = 0; j < ((JContainer)val2).Count; j++)
			{
				_categories[i].ItemCategories[j] = new ClientCraftingItemCategory
				{
					Id = val2[j][(object)"id"].ToObject<string>(),
					Icon = val2[j][(object)"icon"].ToObject<string>(),
					Diagram = val2[j][(object)"diagram"].ToObject<string>(),
					Slots = val2[j][(object)"slots"].ToObject<int>(),
					SpecialSlot = val2[j][(object)"specialSlot"].ToObject<bool>()
				};
			}
		}
		_inputItemGrid.InventorySectionId = _inventoryWindow.Id;
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
		if (_previousSelectedCatgeory != null && _previousSelectedCatgeory.Item1 == Extensions.Value<string>((IEnumerable<JToken>)_inventoryWindow.WindowData["id"]) && _categories.Any((ClientCraftingCategory cat) => cat.Id == _previousSelectedCatgeory.Item2) && _categories.First((ClientCraftingCategory cat) => cat.Id == _previousSelectedCatgeory.Item2).ItemCategories.Any((ClientCraftingItemCategory cat) => cat.Id == _previousSelectedCatgeory.Item3))
		{
			SelectCategory(_previousSelectedCatgeory.Item2, _previousSelectedCatgeory.Item3);
			return;
		}
		_previousSelectedCatgeory = Tuple.Create(Extensions.Value<string>((IEnumerable<JToken>)_inventoryWindow.WindowData["id"]), _categories[0].Id, _categories[0].ItemCategories[0].Id);
		SelectCategory(_categories[0].Id, _categories[0].ItemCategories[0].Id);
	}

	private void BuildCategoryButtons()
	{
		_categoryLabel.Text = _selectedCategory.Item1;
		_categoryLabel.Parent.Layout();
		_itemCategoriesContainer.Clear();
		TabNavigation.Tab[] array = new TabNavigation.Tab[_categories.Length];
		for (int i = 0; i < _categories.Length; i++)
		{
			ClientCraftingCategory category = _categories[i];
			TabNavigation.Tab tab = new TabNavigation.Tab
			{
				Id = category.Id
			};
			if (_inGameView.TryMountAssetTexture(category.Icon, out var textureArea))
			{
				tab.Icon = new PatchStyle(textureArea);
			}
			array[i] = tab;
			if (!(_selectedCategory.Item1 == category.Id))
			{
				continue;
			}
			ClientCraftingItemCategory[] itemCategories = category.ItemCategories;
			foreach (ClientCraftingItemCategory itemCategory in itemCategories)
			{
				Button parent = new Button(Desktop, _itemCategoriesContainer)
				{
					Background = ((_selectedCategory.Item2 == itemCategory.Id) ? _itemCategoryButtonActivePatchStyle : _itemCategoryButtonPatchStyle),
					Anchor = _itemCategoryButtonAnchor,
					Activating = delegate
					{
						SelectCategory(category.Id, itemCategory.Id);
					}
				};
				if (_inGameView.TryMountAssetTexture(itemCategory.Icon, out var textureArea2))
				{
					new Group(Desktop, parent)
					{
						Background = new PatchStyle(textureArea2),
						Anchor = new Anchor
						{
							Width = _itemCategoryButtonAnchor.Height,
							Height = _itemCategoryButtonAnchor.Height
						}
					};
				}
			}
		}
		_categoryTabs.Tabs = array;
		_categoryTabs.SelectedTab = _selectedCategory.Item1;
		_categoryTabs.Layout();
		_itemCategoriesContainer.Layout();
	}

	private void SelectCategory(string categoryId, string itemCategoryId)
	{
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected O, but got Unknown
		_previousSelectedCatgeory = Tuple.Create(Extensions.Value<string>((IEnumerable<JToken>)_inventoryWindow.WindowData["id"]), categoryId, itemCategoryId);
		_selectedCategory = Tuple.Create(categoryId, itemCategoryId);
		BuildCategoryButtons();
		_itemDiagramGroup.Clear();
		for (int i = 0; i < 3; i++)
		{
			new Group(Desktop, _itemDiagramGroup)
			{
				Anchor = new Anchor
				{
					Width = 415,
					Height = 145
				},
				Background = new PatchStyle($"InGame/Pages/Inventory/Diagram/Sword_{i + 1}_Default.png")
			};
		}
		Update();
		InventoryPage inventoryPage = _inGameView.InventoryPage;
		int id = _inventoryWindow.Id;
		JObject val = new JObject();
		val.Add("category", JToken.op_Implicit(categoryId));
		val.Add("itemCategory", JToken.op_Implicit(itemCategoryId));
		inventoryPage.SendWindowAction(id, "updateCategory", val);
	}

	private void UpdateDiagramState()
	{
		for (int i = 0; i < 3; i++)
		{
			string arg = "Default";
			if (_inputItemGrid.Slots.Length > i && _inputItemGrid.Slots[i].Background == _validItemGridSlotBackground)
			{
				arg = "Valid";
			}
			else if (_hoveredInputSlot == i)
			{
				arg = "Hovered";
			}
			Element element = _itemDiagramGroup.Children[i];
			element.Background = new PatchStyle($"InGame/Pages/Inventory/Diagram/Sword_{i + 1}_{arg}.png");
			element.Layout();
		}
	}

	public void OnSetStacks()
	{
		Update();
	}

	public void ResetState()
	{
		_previousSelectedCatgeory = null;
	}
}
