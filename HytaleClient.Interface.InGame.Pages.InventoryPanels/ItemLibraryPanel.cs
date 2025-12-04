using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Data.Items;
using HytaleClient.Interface.InGame.Hud;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class ItemLibraryPanel : Panel
{
	private TabNavigation _primaryCategoryTabs;

	private TabNavigation _secondaryCategoryTabs;

	private Tuple<string, string> _selectedTabs;

	private ItemGridInfoDisplayMode _displayMode = (ItemGridInfoDisplayMode)0;

	private Dictionary<string, string> _idKeywordsMapping;

	public ItemGrid ItemGrid { get; private set; }

	public TextField SearchField { get; private set; }

	public string HoveredItemId { get; private set; }

	public ItemLibraryPanel(InGameView inGameView, Element parent = null)
		: base(inGameView, parent)
	{
	}//IL_0002: Unknown result type (might be due to invalid IL or missing references)


	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Pages/Inventory/ItemLibraryPanel.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_primaryCategoryTabs = uIFragment.Get<TabNavigation>("PrimaryCategoryTabs");
		_primaryCategoryTabs.SelectedTabChanged = delegate
		{
			SearchField.Value = "";
			SetSelectedTabs(_primaryCategoryTabs.SelectedTab);
			UpdateItemLibrary();
			SetupSecondaryCategoryTabs();
			_secondaryCategoryTabs.Parent.Layout();
		};
		_secondaryCategoryTabs = uIFragment.Get<TabNavigation>("SecondaryCategoryTabs");
		_secondaryCategoryTabs.SelectedTabChanged = delegate
		{
			SearchField.Value = "";
			SetSelectedTabs(_primaryCategoryTabs.SelectedTab, _secondaryCategoryTabs.SelectedTab);
			UpdateItemLibrary();
		};
		SearchField = uIFragment.Get<Group>("Search").Find<TextField>("SearchField");
		SearchField.ValueChanged = UpdateItemLibrary;
		ItemGrid = uIFragment.Get<ItemGrid>("ItemGrid");
		ItemGrid.AllowMaxStackDraggableItems = true;
		ItemGrid.Slots = new ItemGridSlot[0];
		ItemGrid.SlotMouseEntered = delegate(int slotIndex)
		{
			ClientItemStack clientItemStack3 = ItemGrid.Slots[slotIndex]?.ItemStack;
			if (clientItemStack3 != null)
			{
				HoveredItemId = clientItemStack3.Id;
			}
		};
		ItemGrid.SlotMouseExited = delegate
		{
			HoveredItemId = null;
		};
		ItemGrid.SlotClicking = delegate(int slotIndex, int button)
		{
			ClientItemStack clientItemStack2 = ItemGrid.Slots[slotIndex]?.ItemStack;
			if (clientItemStack2 != null)
			{
				string id2 = clientItemStack2.Id;
				if (Interface.App.InGame.Instance.Chat.IsOpen && Desktop.IsShiftKeyDown)
				{
					_inGameView.ChatComponent.InsertItemTag(id2);
				}
				else if (Desktop.IsShiftKeyDown)
				{
					ClientItemBase clientItemBase2 = _inGameView.Items[id2];
					int quantity2 = 1;
					if ((long)button == 1)
					{
						quantity2 = clientItemBase2.MaxStack;
					}
					else if ((long)button == 2)
					{
						quantity2 = (int)System.Math.Floor((float)clientItemBase2.MaxStack / 2f);
					}
					ClientItemStack itemStack4 = new ClientItemStack(clientItemBase2.Id, quantity2);
					_inGameView.InGame.SendSmartGiveCreativeItemPacket(itemStack4, (SmartMoveType)1);
				}
			}
		};
		ItemGrid.SlotDoubleClicking = delegate(int slotIndex)
		{
			ClientItemStack clientItemStack = ItemGrid.Slots[slotIndex]?.ItemStack;
			if (clientItemStack != null)
			{
				string id = clientItemStack.Id;
				ClientItemBase clientItemBase = _inGameView.Items[id];
				ClientItemStack itemStack3 = new ClientItemStack(clientItemBase.Id);
				_inGameView.InGame.SendSmartGiveCreativeItemPacket(itemStack3, (SmartMoveType)0);
			}
		};
		ItemGrid.Dropped = delegate(int targetSlotIndex, Element sourceItemGrid, ItemGrid.ItemDragData dragData)
		{
			if (dragData.InventorySectionId.HasValue)
			{
				ClientItemStack itemStack;
				if (!(sourceItemGrid is ItemGrid itemGrid))
				{
					if (!(sourceItemGrid is BaseItemSlotSelector baseItemSlotSelector))
					{
						return;
					}
					itemStack = baseItemSlotSelector.GetItemStack(dragData.ItemGridIndex);
				}
				else
				{
					itemStack = itemGrid.Slots[dragData.ItemGridIndex].ItemStack;
				}
				int quantity = itemStack.Quantity - dragData.ItemStack.Quantity;
				ClientItemStack itemStack2 = new ClientItemStack(itemStack.Id, quantity)
				{
					Metadata = itemStack.Metadata
				};
				_inGameView.ClearSlotHighlight();
				Interface.App.InGame.Instance.BuilderToolsModule.ClearConfiguringTool();
				_inGameView.InGame.SendSetCreativeItemPacket(dragData.InventorySectionId.Value, dragData.SlotId, itemStack2, overwrite: true);
			}
		};
		if (_inGameView.Items != null)
		{
			UpdateItemLibrary();
		}
		ClientItemCategory[] itemCategories = Interface.App.InGame.ItemCategories;
		if (itemCategories != null && itemCategories.Length != 0)
		{
			if (_selectedTabs == null)
			{
				SetSelectedTabs(Interface.App.InGame.ItemCategories[0].Id);
			}
			SetupPrimaryCategoryTabs();
			SetupSecondaryCategoryTabs();
		}
	}

	public void ResetState()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		_selectedTabs = null;
		_idKeywordsMapping = null;
		_displayMode = (ItemGridInfoDisplayMode)0;
		_primaryCategoryTabs.Tabs = new TabNavigation.Tab[0];
		_secondaryCategoryTabs.Tabs = new TabNavigation.Tab[0];
		SearchField.Value = "";
	}

	protected override void OnMounted()
	{
		UpdateItemLibrary();
	}

	protected override void OnUnmounted()
	{
		HoveredItemId = null;
	}

	private void SetSelectedTabs(string primaryTab, string secondaryTab = null)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		ClientItemCategory clientItemCategory = Interface.App.InGame.ItemCategories.First((ClientItemCategory c) => c.Id == primaryTab);
		ClientItemCategory clientItemCategory2 = ((secondaryTab != null) ? clientItemCategory.Children.First((ClientItemCategory c) => c.Id == secondaryTab) : clientItemCategory.Children[0]);
		_selectedTabs = Tuple.Create(primaryTab, clientItemCategory2.Id);
		_displayMode = clientItemCategory2.InfoDisplayMode;
	}

	public void EnsureValidCategorySelected()
	{
		if (_selectedTabs == null)
		{
			SetSelectedTabs(Interface.App.InGame.ItemCategories[0].Id);
			return;
		}
		ClientItemCategory[] itemCategories = Interface.App.InGame.ItemCategories;
		foreach (ClientItemCategory clientItemCategory in itemCategories)
		{
			if (clientItemCategory.Id != _selectedTabs.Item1)
			{
				continue;
			}
			ClientItemCategory[] children = clientItemCategory.Children;
			foreach (ClientItemCategory clientItemCategory2 in children)
			{
				if (!(clientItemCategory2.Id != _selectedTabs.Item2))
				{
					return;
				}
			}
			break;
		}
		SetSelectedTabs(Interface.App.InGame.ItemCategories[0].Id);
	}

	public void SetupCategories()
	{
		SetupPrimaryCategoryTabs();
		SetupSecondaryCategoryTabs();
		if (base.IsMounted)
		{
			_primaryCategoryTabs.Layout();
			_secondaryCategoryTabs.Parent.Layout();
		}
	}

	public void OnItemsUpdated()
	{
		_idKeywordsMapping = new Dictionary<string, string>(_inGameView.Items.Count);
		foreach (ClientItemBase value in _inGameView.Items.Values)
		{
			string text = Interface.GetText("items." + value.Id + ".name", null, returnFallback: false);
			if (text != null)
			{
				_idKeywordsMapping[value.Id] = text.ToLowerInvariant() + " " + value.Id.ToLowerInvariant();
			}
		}
	}

	private void SetupPrimaryCategoryTabs()
	{
		ClientItemCategory[] itemCategories = Interface.App.InGame.ItemCategories;
		TabNavigation.Tab[] array = new TabNavigation.Tab[itemCategories.Length];
		for (int i = 0; i < itemCategories.Length; i++)
		{
			ClientItemCategory clientItemCategory = itemCategories[i];
			array[i] = new TabNavigation.Tab
			{
				Id = clientItemCategory.Id
			};
			if (_inGameView.TryMountAssetTexture(clientItemCategory.Icon, out var textureArea))
			{
				array[i].Icon = new PatchStyle(textureArea);
			}
			if (_inGameView.TryMountAssetTexture(clientItemCategory.Icon.Replace(".png", "Active.png"), out var textureArea2))
			{
				array[i].IconSelected = new PatchStyle(textureArea2);
			}
		}
		_primaryCategoryTabs.Tabs = array;
		_primaryCategoryTabs.SelectedTab = _selectedTabs.Item1;
	}

	public void UpdateItemLibrary()
	{
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		string value = _selectedTabs?.Item1 + "." + _selectedTabs?.Item2;
		List<ClientItemBase> list = new List<ClientItemBase>();
		string text = SearchField.Value.Trim().ToLowerInvariant();
		if (text != "")
		{
			string[] array = (from w in text.Split(new char[1] { ' ' })
				select w.Trim() into w
				where w != ""
				select w).ToArray();
			foreach (KeyValuePair<string, string> item in _idKeywordsMapping)
			{
				bool flag = true;
				string[] array2 = array;
				foreach (string value2 in array2)
				{
					if (!item.Value.Contains(value2))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					list.Add(_inGameView.Items[item.Key]);
				}
			}
			ItemGrid.InfoDisplay = (ItemGridInfoDisplayMode)0;
		}
		else
		{
			foreach (KeyValuePair<string, ClientItemBase> item2 in _inGameView.Items)
			{
				if (item2.Value.Categories != null && item2.Value.Categories.Contains(value))
				{
					list.Add(item2.Value);
				}
			}
			ItemGrid.InfoDisplay = _displayMode;
		}
		list = (from item in list
			orderby item.Set, item.Id
			select item).ToList();
		int itemsPerRow = ItemGrid.GetItemsPerRow();
		int num = ((list.Count > 5 * itemsPerRow) ? (list.Count + itemsPerRow - list.Count % itemsPerRow) : (5 * itemsPerRow));
		ItemGrid.Slots = new ItemGridSlot[num];
		ItemGrid.SetItemStacks(list.Select((ClientItemBase item) => new ClientItemStack(item.Id)).ToArray());
		ItemGrid.SetScroll(0, 0);
		if (ItemGrid.IsMounted)
		{
			ItemGrid.Layout();
		}
	}

	private void SetupSecondaryCategoryTabs()
	{
		ClientItemCategory[] children = Interface.App.InGame.ItemCategories.First((ClientItemCategory c) => c.Id == _selectedTabs.Item1).Children;
		TabNavigation.Tab[] array = new TabNavigation.Tab[children.Length];
		for (int i = 0; i < children.Length; i++)
		{
			array[i] = new TabNavigation.Tab
			{
				Id = children[i].Id
			};
			if (_inGameView.TryMountAssetTexture(children[i].Icon, out var textureArea))
			{
				array[i].Icon = new PatchStyle(textureArea);
			}
		}
		_secondaryCategoryTabs.Tabs = array;
		_secondaryCategoryTabs.SelectedTab = _selectedTabs.Item2;
	}
}
