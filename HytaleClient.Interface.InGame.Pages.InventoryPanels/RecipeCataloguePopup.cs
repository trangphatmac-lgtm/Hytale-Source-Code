using System;
using System.Collections.Generic;
using HytaleClient.Data.Items;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class RecipeCataloguePopup : InterfaceComponent
{
	private InGameView _inGameView;

	private Group _benchesGroup;

	private Group _benchCategoriesGroup;

	private Group _itemCategoriesGroup;

	private Group _recipesGroup;

	private ItemPreviewComponent _itemPreviewComponent;

	private Label _itemNameLabel;

	private Label _itemDescriptionLabel;

	private Group _ingredientsGroup;

	private Group _categoriesSeparator;

	private Tuple<string, string, string, string> _selection = Tuple.Create<string, string, string, string>(null, null, null, null);

	public RecipeCataloguePopup(InGameView inGameView, Element parent)
		: base(inGameView.Interface, parent)
	{
		_inGameView = inGameView;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Pages/Inventory/RecipeCataloguePopup.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		uIFragment.Get<Button>("CloseButton").Activating = Dismiss;
		_benchesGroup = uIFragment.Get<Group>("Benches");
		_benchCategoriesGroup = uIFragment.Get<Group>("BenchCategories");
		_itemCategoriesGroup = uIFragment.Get<Group>("ItemCategories");
		_recipesGroup = uIFragment.Get<Group>("Recipes");
		_categoriesSeparator = uIFragment.Get<Group>("CategoriesSeparator");
		_itemPreviewComponent = uIFragment.Get<ItemPreviewComponent>("ItemPreview");
		_itemNameLabel = uIFragment.Get<Label>("ItemName");
		_itemDescriptionLabel = uIFragment.Get<Label>("ItemDescription");
		_ingredientsGroup = uIFragment.Get<Group>("Ingredients");
	}

	protected override void OnMounted()
	{
		UpdateLists();
		if (_inGameView.InventoryPage.BlockInfoPanel.IsMounted)
		{
			_inGameView.InventoryPage.BlockInfoPanel.UpdatePreview();
		}
		if (_inGameView.InventoryPage.BasicCraftingPanel.IsMounted)
		{
			_inGameView.InventoryPage.BasicCraftingPanel.UpdateItemPreview();
		}
		if (_inGameView.InventoryPage.StructuralCraftingPanel.IsMounted)
		{
			_inGameView.InventoryPage.StructuralCraftingPanel.UpdateItemPreview();
		}
		if (_inGameView.InventoryPage.CharacterPanel.IsMounted)
		{
			_inGameView.InventoryPage.CharacterPanel.UpdateCharacterVisibility();
		}
	}

	protected override void OnUnmounted()
	{
		if (_inGameView.IsMounted)
		{
			if (_inGameView.InventoryPage.BlockInfoPanel.IsMounted)
			{
				_inGameView.InventoryPage.BlockInfoPanel.UpdatePreview();
			}
			if (_inGameView.InventoryPage.BasicCraftingPanel.IsMounted)
			{
				_inGameView.InventoryPage.BasicCraftingPanel.UpdateItemPreview();
			}
			if (_inGameView.InventoryPage.StructuralCraftingPanel.IsMounted)
			{
				_inGameView.InventoryPage.StructuralCraftingPanel.UpdateItemPreview();
			}
			if (_inGameView.InventoryPage.CharacterPanel.IsMounted)
			{
				_inGameView.InventoryPage.CharacterPanel.UpdateCharacterVisibility();
			}
		}
	}

	protected internal override void Dismiss()
	{
		Desktop.ClearLayer(2);
	}

	public void SetupSelectedBench(string benchId)
	{
		_selection = Tuple.Create<string, string, string, string>(benchId, null, null, null);
	}

	private void UpdateLists()
	{
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Invalid comparison between Unknown and I4
		_benchesGroup.Clear();
		_benchCategoriesGroup.Clear();
		_itemCategoriesGroup.Clear();
		_recipesGroup.Clear();
		Dictionary<string, ClientItemBase> items = _inGameView.Items;
		HashSet<string> hashSet = new HashSet<string>();
		HashSet<string> hashSet2 = new HashSet<string>();
		HashSet<string> hashSet3 = new HashSet<string>();
		Interface.TryGetDocument("InGame/Pages/Inventory/RecipeCatalogueNavigationButton.ui", out var navigationButtonDoc);
		Interface.TryGetDocument("InGame/Pages/Inventory/RecipeCatalogueNavigationButtonSelected.ui", out var navigationButtonSelectedDoc);
		Interface.TryGetDocument("InGame/Pages/Inventory/RecipeCatalogueRecipeButton.ui", out var recipeButtonDoc);
		int recipeButtonItemSlotSize = recipeButtonDoc.ResolveNamedValue<int>(Interface, "ItemSlotSize");
		Button.ButtonStyle recipeButtonStyleSelected = recipeButtonDoc.ResolveNamedValue<Button.ButtonStyle>(Interface, "StyleSelected");
		ItemGrid.ItemGridStyle itemGridStyle = recipeButtonDoc.ResolveNamedValue<ItemGrid.ItemGridStyle>(Interface, "RecipeItemGridStyle");
		foreach (ClientItemBase value in items.Values)
		{
			if (value.Recipe?.BenchRequirement == null || (value.Recipe.KnowledgeRequired && !_inGameView.InventoryPage.KnownCraftingRecipes.ContainsKey(value.Id)))
			{
				continue;
			}
			bool flag = false;
			ClientItemCraftingRecipe.ClientBenchRequirement[] benchRequirement = value.Recipe.BenchRequirement;
			foreach (ClientItemCraftingRecipe.ClientBenchRequirement clientBenchRequirement in benchRequirement)
			{
				if ((int)clientBenchRequirement.Type == 1)
				{
					continue;
				}
				if (_selection.Item1 == null)
				{
					_selection = Tuple.Create<string, string, string, string>(clientBenchRequirement.Id, null, null, null);
				}
				if (!hashSet.Contains(clientBenchRequirement.Id))
				{
					hashSet.Add(clientBenchRequirement.Id);
					MakeTextButton(_benchesGroup, clientBenchRequirement.Id, _selection.Item1 == clientBenchRequirement.Id, clientBenchRequirement.Id, null, null, null);
				}
				if (clientBenchRequirement.Id != _selection.Item1)
				{
					continue;
				}
				if (clientBenchRequirement.Categories == null)
				{
					if (_selection.Item2 == null)
					{
						_selection = Tuple.Create<string, string, string, string>(clientBenchRequirement.Id, "All", null, null);
					}
					if (!hashSet2.Contains("All"))
					{
						hashSet2.Add("All");
						MakeTextButton(_benchCategoriesGroup, "All", _selection.Item2 == "All", clientBenchRequirement.Id, "All", null, null);
					}
					if (!(_selection.Item2 != "All") && !flag)
					{
						if (_selection.Item4 == null)
						{
							_selection = Tuple.Create<string, string, string, string>(clientBenchRequirement.Id, "All", null, value.Id);
						}
						flag = true;
						MakeRecipeButton(value, clientBenchRequirement.Id, "All", null);
					}
					continue;
				}
				string[] categories = clientBenchRequirement.Categories;
				foreach (string text in categories)
				{
					string[] array = text.Split(new char[1] { '.' });
					string text2 = array[0];
					if (_selection.Item2 == null)
					{
						_selection = Tuple.Create<string, string, string, string>(clientBenchRequirement.Id, text2, null, null);
					}
					if (!hashSet2.Contains(text2))
					{
						hashSet2.Add(text2);
						MakeTextButton(_benchCategoriesGroup, text2, _selection.Item2 == text2, clientBenchRequirement.Id, text2, null, null);
					}
					if (text2 != _selection.Item2)
					{
						continue;
					}
					if (array.Length > 1)
					{
						string text3 = array[1];
						if (_selection.Item3 == null)
						{
							_selection = Tuple.Create<string, string, string, string>(clientBenchRequirement.Id, text2, text3, null);
						}
						if (!hashSet3.Contains(text3))
						{
							hashSet3.Add(text3);
							MakeTextButton(_itemCategoriesGroup, text3, _selection.Item3 == text3, clientBenchRequirement.Id, text2, text3, null);
						}
						if (text3 != _selection.Item3)
						{
							continue;
						}
					}
					if (!flag)
					{
						if (_selection.Item4 == null)
						{
							_selection = Tuple.Create(clientBenchRequirement.Id, text2, (array.Length > 1) ? array[1] : null, value.Id);
						}
						flag = true;
						MakeRecipeButton(value, clientBenchRequirement.Id, text2, (array.Length > 1) ? array[1] : null);
					}
				}
			}
		}
		if (_recipesGroup.Children.Count > 0)
		{
			_recipesGroup.Children[_recipesGroup.Children.Count - 1].Anchor.Bottom = 0;
		}
		_categoriesSeparator.Visible = _itemCategoriesGroup.Children.Count > 0;
		UpdateItemPanel();
		void MakeRecipeButton(ClientItemBase item, string bench, string benchCategory, string itemCategory)
		{
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			UIFragment uIFragment = recipeButtonDoc.Instantiate(Desktop, _recipesGroup);
			Button button = uIFragment.Get<Button>("Button");
			button.Activating = delegate
			{
				_selection = Tuple.Create(bench, benchCategory, itemCategory, item.Id);
				UpdateLists();
				Layout();
			};
			if (_selection.Item4 == item.Id)
			{
				button.Style = recipeButtonStyleSelected;
			}
			ItemGrid itemGrid = new ItemGrid(Desktop, button)
			{
				SlotsPerRow = 1,
				InfoDisplay = (ItemGridInfoDisplayMode)2,
				Style = itemGridStyle,
				Slots = new ItemGridSlot[1],
				Anchor = new Anchor
				{
					Width = recipeButtonItemSlotSize,
					Height = recipeButtonItemSlotSize,
					Left = 5
				},
				RenderItemQualityBackground = false
			};
			itemGrid.Slots[0] = new ItemGridSlot
			{
				ItemStack = new ClientItemStack(item.Id)
			};
			Label label = uIFragment.Get<Label>("Name");
			label.Text = Interface.GetText("items." + item.Id + ".name");
			label.Style = label.Style.Clone();
			label.Style.TextColor = _inGameView.InGame.Instance.ServerSettings.ItemQualities[item.QualityIndex].TextColor;
		}
		void MakeTextButton(Element parent, string name, bool isSelected, string bench, string benchCategory, string itemCategory, string itemId)
		{
			if (isSelected)
			{
				navigationButtonSelectedDoc.Instantiate(Desktop, parent).Get<Label>("Label").Text = name;
			}
			else
			{
				TextButton textButton = navigationButtonDoc.Instantiate(Desktop, parent).Get<TextButton>("Button");
				textButton.Text = name;
				textButton.Activating = delegate
				{
					_selection = Tuple.Create(bench, benchCategory, itemCategory, itemId);
					UpdateLists();
					Layout();
				};
			}
		}
	}

	private void UpdateItemPanel()
	{
		ClientItemBase clientItemBase = _inGameView.Items[_selection.Item4];
		_itemNameLabel.Text = Desktop.Provider.GetText("items." + clientItemBase.Id + ".name");
		_itemNameLabel.Style = _itemNameLabel.Style.Clone();
		_itemNameLabel.Style.TextColor = _inGameView.InGame.Instance.ServerSettings.ItemQualities[clientItemBase.QualityIndex].TextColor;
		_itemDescriptionLabel.Text = Desktop.Provider.GetText("items." + clientItemBase.Id + ".description", null, returnFallback: false) ?? "";
		_itemPreviewComponent.SetItemId(clientItemBase.Id);
		_ingredientsGroup.Clear();
		Group root = null;
		Interface.TryGetDocument("InGame/Pages/Inventory/RecipeCatalogueIngredient.ui", out var document);
		Interface.TryGetDocument("InGame/Pages/Inventory/RecipeCatalogueSeparator.ui", out var document2);
		for (int i = 0; i < clientItemBase.Recipe.Input.Length; i++)
		{
			if (i % 2 == 0)
			{
				if (i > 0)
				{
					document2.Instantiate(Desktop, _ingredientsGroup);
				}
				root = new Group(Desktop, _ingredientsGroup)
				{
					LayoutMode = LayoutMode.Center
				};
			}
			ClientItemCraftingRecipe.ClientCraftingMaterial clientCraftingMaterial = clientItemBase.Recipe.Input[i];
			UIFragment uIFragment = document.Instantiate(Desktop, root);
			uIFragment.Get<Label>("Name").Text = ((clientCraftingMaterial.ItemId != null) ? Interface.GetText("items." + clientCraftingMaterial.ItemId + ".name") : Interface.GetText("resourceTypes." + clientCraftingMaterial.ResourceTypeId + ".name"));
			Label label = uIFragment.Get<Label>("Quantity");
			int quantity = clientCraftingMaterial.Quantity;
			label.Text = "x" + quantity;
			ItemGrid itemGrid = new ItemGrid(Desktop, uIFragment.Get<Group>("Container"));
			itemGrid.SlotsPerRow = 1;
			itemGrid.Style = _inGameView.DefaultItemGridStyle.Clone();
			itemGrid.Style.SlotBackground = null;
			itemGrid.Anchor.Width = itemGrid.Style.SlotSize;
			itemGrid.RenderItemQualityBackground = false;
			itemGrid.Slots = new ItemGridSlot[1];
			if (clientCraftingMaterial.ItemId == null)
			{
				PatchStyle icon = null;
				if (_inGameView.InventoryPage.ResourceTypes.TryGetValue(clientCraftingMaterial.ResourceTypeId, out var value) && _inGameView.TryMountAssetTexture(value.Icon, out var textureArea))
				{
					icon = new PatchStyle(textureArea);
				}
				itemGrid.Slots[0] = new ItemGridSlot
				{
					Name = Desktop.Provider.GetText("ui.items.resourceTypeTooltip.name", new Dictionary<string, string> { 
					{
						"name",
						Desktop.Provider.GetText("resourceTypes." + clientCraftingMaterial.ResourceTypeId + ".name")
					} }),
					Description = Desktop.Provider.GetText("resourceTypes." + clientCraftingMaterial.ResourceTypeId + ".description", null, returnFallback: false),
					Icon = icon
				};
			}
			else
			{
				itemGrid.Slots[0] = new ItemGridSlot(new ClientItemStack(clientCraftingMaterial.ItemId));
				itemGrid.Layout();
			}
		}
	}
}
