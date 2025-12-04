#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HytaleClient.Data.Items;
using HytaleClient.Interface.InGame.Pages.InventoryPanels;
using HytaleClient.Interface.InGame.Pages.InventoryPanels.SelectionCommands;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.Interface.InGame.Pages;

internal class InventoryPage : InterfaceComponent
{
	public readonly InGameView InGameView;

	public bool IsFieldcraft;

	private int _hotbarHeight;

	private int _panelSpacing;

	public readonly ProcessingPanel ProcessingPanel;

	public readonly DiagramCraftingPanel DiagramCraftingPanel;

	public readonly ContainerPanel ContainerPanel;

	public readonly BasicCraftingPanel BasicCraftingPanel;

	public readonly StructuralCraftingPanel StructuralCraftingPanel;

	public readonly BlockInfoPanel BlockInfoPanel;

	public readonly CharacterPanel CharacterPanel;

	public readonly StoragePanel StoragePanel;

	public readonly ItemLibraryPanel ItemLibraryPanel;

	public readonly RecipeCataloguePopup RecipeCataloguePopup;

	public readonly BuilderToolPanel BuilderToolPanel;

	public readonly SelectionCommandsPanel SelectionCommandsPanel;

	public readonly ItemSlotSelectorPopover ItemSelectorPopover;

	public SoundStyle OpenSound { get; private set; }

	public SoundStyle CloseSound { get; private set; }

	public ClientCraftingCategory[] FieldcraftCategories { get; private set; }

	public Dictionary<string, ClientItemCraftingRecipe> KnownCraftingRecipes { get; } = new Dictionary<string, ClientItemCraftingRecipe>();


	public Dictionary<string, ClientResourceType> ResourceTypes { get; private set; } = new Dictionary<string, ClientResourceType>();


	public InventoryPage(InGameView inGameView)
		: base(inGameView.Interface, null)
	{
		InGameView = inGameView;
		ItemSelectorPopover = new ItemSlotSelectorPopover(inGameView, null);
		ContainerPanel = new ContainerPanel(InGameView)
		{
			Visible = false
		};
		BasicCraftingPanel = new BasicCraftingPanel(InGameView)
		{
			Visible = false
		};
		StructuralCraftingPanel = new StructuralCraftingPanel(InGameView)
		{
			Visible = false
		};
		ProcessingPanel = new ProcessingPanel(InGameView)
		{
			Visible = false
		};
		DiagramCraftingPanel = new DiagramCraftingPanel(InGameView)
		{
			Visible = false
		};
		BlockInfoPanel = new BlockInfoPanel(InGameView)
		{
			Visible = false
		};
		CharacterPanel = new CharacterPanel(InGameView);
		ItemLibraryPanel = new ItemLibraryPanel(InGameView)
		{
			Visible = false
		};
		StoragePanel = new StoragePanel(InGameView);
		BuilderToolPanel = new BuilderToolPanel(InGameView)
		{
			Visible = false
		};
		BuilderToolPanel.Visible = false;
		SelectionCommandsPanel = new SelectionCommandsPanel(InGameView)
		{
			Visible = false
		};
		SelectionCommandsPanel.Visible = false;
		RecipeCataloguePopup = new RecipeCataloguePopup(inGameView, null);
		Interface.RegisterForEventFromEngine<ClientCraftingCategory[]>("fieldcraftCategories.initialized", OnFieldcraftCategoriesInitialized);
		Interface.RegisterForEventFromEngine<ClientCraftingCategory[]>("fieldcraftCategories.added", OnFieldcraftCategoriesAdded);
		Interface.RegisterForEventFromEngine<PacketHandler.ClientKnownRecipe[]>("crafting.knownRecipesUpdated", OnKnownRecipesUpdated);
		Interface.RegisterForEventFromEngine<Dictionary<string, ClientResourceType>>("resourceTypes.initialized", OnResourceTypesInitialized);
		Interface.RegisterForEventFromEngine<Dictionary<string, ClientResourceType>>("resourceTypes.added", OnResourceTypesAdded);
		Interface.RegisterForEventFromEngine<string[]>("resourceTypes.removed", OnResourceTypesRemoved);
	}

	public void Build()
	{
		Find<Group>("CenterPanel")?.Clear();
		Find<Group>("Root")?.Clear();
		Clear();
		Interface.TryGetDocument("InGame/Pages/Inventory/InventoryPage.ui", out var document);
		OpenSound = document.ResolveNamedValue<SoundStyle>(Desktop.Provider, "OpenSound");
		CloseSound = document.ResolveNamedValue<SoundStyle>(Desktop.Provider, "CloseSound");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_hotbarHeight = document.ResolveNamedValue<int>(Interface, "HotbarHeight");
		_panelSpacing = document.ResolveNamedValue<int>(Interface, "PanelSpacing");
		Group group = uIFragment.Get<Group>("Root");
		BlockInfoPanel.Build();
		group.Add(BlockInfoPanel);
		CharacterPanel.Build();
		group.Add(CharacterPanel, 0);
		BuilderToolPanel.Build();
		group.Add(BuilderToolPanel);
		SelectionCommandsPanel.Build();
		group.Add(SelectionCommandsPanel);
		Group group2 = uIFragment.Get<Group>("CenterPanel");
		ContainerPanel.Build();
		group2.Add(ContainerPanel);
		BasicCraftingPanel.Build();
		group2.Add(BasicCraftingPanel);
		StructuralCraftingPanel.Build();
		group2.Add(StructuralCraftingPanel);
		DiagramCraftingPanel.Build();
		group2.Add(DiagramCraftingPanel);
		ProcessingPanel.Build();
		group2.Add(ProcessingPanel);
		ItemLibraryPanel.Build();
		group2.Add(ItemLibraryPanel);
		StoragePanel.Build();
		group2.Add(StoragePanel);
		RecipeCataloguePopup.Build();
		ItemSelectorPopover.Visible = false;
		ItemSelectorPopover.Build();
		Add(ItemSelectorPopover);
		if (base.IsMounted)
		{
			InGameView.HotbarComponent.OnToggleInventoryOpen();
			Add(InGameView.HotbarComponent);
			Add(InGameView.InputBindingsComponent);
			SetupWindows();
		}
	}

	protected override void OnMounted()
	{
		base.OnMounted();
		InGameView.InGame.SetSceneBlurEnabled(enabled: true);
		InGameView.HotbarComponent.UpdateActiveItemNameLabel();
		InGameView.HotbarComponent.OnToggleInventoryOpen();
		InGameView.HudContainer.Remove(InGameView.HotbarComponent);
		InGameView.HudContainer.Remove(InGameView.InputBindingsComponent);
		Add(InGameView.HotbarComponent);
		Add(InGameView.InputBindingsComponent);
		SetupWindows();
		Layout();
		InGameView.SetupCursorFloatingItem();
	}

	protected override void OnUnmounted()
	{
		base.OnUnmounted();
		InGameView.InGame.SetSceneBlurEnabled(enabled: false);
		Remove(InGameView.HotbarComponent);
		Remove(InGameView.InputBindingsComponent);
		InGameView.HotbarComponent.UpdateActiveItemNameLabel();
		InGameView.HotbarComponent.OnToggleInventoryOpen();
		InGameView.HudContainer.Add(InGameView.HotbarComponent);
		InGameView.HudContainer.Add(InGameView.InputBindingsComponent);
		InGameView.HudContainer.Layout();
		if (InGameView.ItemDragData != null)
		{
			InGameView.SetupDragAndDropItem(null);
		}
	}

	public override Element HitTest(Point position)
	{
		Debug.Assert(base.IsMounted);
		if (!_anchoredRectangle.Contains(position))
		{
			return null;
		}
		return base.HitTest(position) ?? this;
	}

	protected internal override void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Invalid comparison between Unknown and I4
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Invalid comparison between Unknown and I4
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Invalid comparison between Unknown and I4
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Invalid comparison between Unknown and I4
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Invalid comparison between Unknown and I4
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Invalid comparison between Unknown and I4
		base.OnKeyDown(keycode, repeat);
		if (Desktop.IsShiftKeyDown)
		{
			for (sbyte b = 0; b < 9; b++)
			{
				if ((int)keycode == 49 + b)
				{
					if (InGameView.HandleHotbarLoad(b))
					{
						return;
					}
					break;
				}
			}
			if ((int)keycode == 48 && InGameView.HandleHotbarLoad(9))
			{
				return;
			}
		}
		if ((int)keycode >= 48 && (int)keycode <= 57)
		{
			InGameView.ClientInventoryPosition hoveredItemSlot = InGameView.HoveredItemSlot;
			if (hoveredItemSlot != null)
			{
				ClientItemStack clientItemStack = InGameView.GetItemStacks(hoveredItemSlot.InventorySectionId)[hoveredItemSlot.SlotId];
				if (clientItemStack != null)
				{
					int targetSlotIndex = (((int)keycode == 48) ? 9 : (keycode - 49));
					InGameView.MoveItemStack(hoveredItemSlot.InventorySectionId, hoveredItemSlot.SlotId, -1, targetSlotIndex, clientItemStack);
				}
			}
		}
		else
		{
			if (!ItemLibraryPanel.IsMounted)
			{
				return;
			}
			if (Desktop.IsShortcutKeyDown && (int)keycode == 102)
			{
				Desktop.FocusElement(ItemLibraryPanel.SearchField);
			}
			else
			{
				if (!Desktop.IsShiftKeyDown || (int)keycode != 104)
				{
					return;
				}
				if (ItemLibraryPanel.HoveredItemId != null)
				{
					InGameView.InGame.OpenAssetIdInAssetEditor("Item", ItemLibraryPanel.HoveredItemId);
					return;
				}
				InGameView.ClientInventoryPosition hoveredItemSlot2 = InGameView.HoveredItemSlot;
				if (hoveredItemSlot2 != null)
				{
					ClientItemStack clientItemStack2 = InGameView.GetItemStacks(hoveredItemSlot2.InventorySectionId)[hoveredItemSlot2.SlotId];
					if (clientItemStack2 != null)
					{
						InGameView.InGame.OpenAssetIdInAssetEditor("Item", clientItemStack2.Id);
					}
				}
			}
		}
	}

	public void OnSetStacks()
	{
		if (base.IsMounted)
		{
			StoragePanel.UpdateGrid();
			CharacterPanel.UpdateGrid();
			if (BasicCraftingPanel.Visible)
			{
				BasicCraftingPanel.OnSetStacks();
			}
			if (StructuralCraftingPanel.Visible)
			{
				StructuralCraftingPanel.OnSetStacks();
			}
			if (DiagramCraftingPanel.Visible)
			{
				DiagramCraftingPanel.OnSetStacks();
			}
			if (ProcessingPanel.Visible)
			{
				ProcessingPanel.OnSetStacks();
			}
		}
	}

	public void OnItemsUpdated()
	{
		ItemLibraryPanel.OnItemsUpdated();
		if (base.IsMounted)
		{
			if (ItemLibraryPanel.Visible)
			{
				ItemLibraryPanel.UpdateItemLibrary();
			}
			OnSetStacks();
		}
	}

	public void OnItemIconsUpdated()
	{
		if (base.IsMounted && InGameView.Items != null)
		{
			if (ItemLibraryPanel.Visible)
			{
				ItemLibraryPanel.UpdateItemLibrary();
			}
			OnSetStacks();
		}
	}

	private void OnFieldcraftCategoriesInitialized(ClientCraftingCategory[] categories)
	{
		FieldcraftCategories = categories;
	}

	private void OnFieldcraftCategoriesAdded(ClientCraftingCategory[] categories)
	{
		Dictionary<string, ClientCraftingCategory> dictionary = new Dictionary<string, ClientCraftingCategory>();
		ClientCraftingCategory[] fieldcraftCategories = FieldcraftCategories;
		foreach (ClientCraftingCategory clientCraftingCategory in fieldcraftCategories)
		{
			dictionary[clientCraftingCategory.Id] = clientCraftingCategory;
		}
		foreach (ClientCraftingCategory clientCraftingCategory2 in categories)
		{
			dictionary[clientCraftingCategory2.Id] = clientCraftingCategory2;
		}
		FieldcraftCategories = dictionary.Values.ToArray();
		if (BasicCraftingPanel.IsMounted && IsFieldcraft)
		{
			BasicCraftingPanel.RefreshWindow();
		}
	}

	private void OnKnownRecipesUpdated(PacketHandler.ClientKnownRecipe[] knownRecipes)
	{
		KnownCraftingRecipes.Clear();
		foreach (PacketHandler.ClientKnownRecipe clientKnownRecipe in knownRecipes)
		{
			KnownCraftingRecipes[clientKnownRecipe.ItemId] = clientKnownRecipe.Recipe;
		}
		if (BasicCraftingPanel.IsMounted)
		{
			BasicCraftingPanel.BuildItemList();
		}
	}

	private void OnResourceTypesInitialized(Dictionary<string, ClientResourceType> resourceTypes)
	{
		ResourceTypes = resourceTypes;
	}

	private void OnResourceTypesAdded(Dictionary<string, ClientResourceType> resourceTypes)
	{
		foreach (KeyValuePair<string, ClientResourceType> resourceType in resourceTypes)
		{
			ResourceTypes[resourceType.Key] = resourceType.Value;
		}
		OnSetStacks();
	}

	private void OnResourceTypesRemoved(string[] resourceTypes)
	{
		foreach (string key in resourceTypes)
		{
			ResourceTypes.Remove(key);
		}
		OnSetStacks();
	}

	public bool IsItemAtPositionDroppable(Point pos)
	{
		return !CharacterPanel.Panel.AnchoredRectangle.Contains(pos) && !StoragePanel.AnchoredRectangle.Contains(pos) && (GetCurrentContextPanel() == null || !GetCurrentContextPanel().AnchoredRectangle.Contains(pos)) && (!BuilderToolPanel.Visible || !BuilderToolPanel.Panel.AnchoredRectangle.Contains(pos)) && (!SelectionCommandsPanel.Visible || !SelectionCommandsPanel.Panel.AnchoredRectangle.Contains(pos)) && (!BlockInfoPanel.Visible || !BlockInfoPanel.Panel.AnchoredRectangle.Contains(pos)) && (!InGameView.InGame.IsToolsSettingsModalOpened || !InGameView.ToolsSettingsPage.ContainPosition(pos));
	}

	private HytaleClient.Interface.InGame.Pages.InventoryPanels.Panel GetCurrentContextPanel()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected I4, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Invalid comparison between Unknown and I4
		if (InGameView.InventoryWindow == null)
		{
			return null;
		}
		WindowType windowType = InGameView.InventoryWindow.WindowType;
		WindowType val = windowType;
		return (int)val switch
		{
			1 => (IsFieldcraft && (int)InGameView.InGame.Instance.GameMode == 1) ? ((HytaleClient.Interface.InGame.Pages.InventoryPanels.Panel)ItemLibraryPanel) : ((HytaleClient.Interface.InGame.Pages.InventoryPanels.Panel)BasicCraftingPanel), 
			4 => ProcessingPanel, 
			3 => StructuralCraftingPanel, 
			2 => DiagramCraftingPanel, 
			0 => ContainerPanel, 
			_ => null, 
		};
	}

	public void SetupWindows()
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected I4, but got Unknown
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Invalid comparison between Unknown and I4
		PacketHandler.InventoryWindow inventoryWindow = InGameView.InventoryWindow;
		ContainerPanel.Visible = false;
		BasicCraftingPanel.Visible = false;
		StructuralCraftingPanel.Visible = false;
		ProcessingPanel.Visible = false;
		DiagramCraftingPanel.Visible = false;
		ItemLibraryPanel.Visible = false;
		BuilderToolPanel.Visible = false;
		BlockInfoPanel.Visible = !IsFieldcraft;
		BlockInfoPanel.Update();
		if (inventoryWindow == null)
		{
			return;
		}
		WindowType windowType = inventoryWindow.WindowType;
		WindowType val = windowType;
		switch ((int)val)
		{
		case 1:
			if (IsFieldcraft && (int)InGameView.InGame.Instance.GameMode == 1)
			{
				ItemLibraryPanel.Visible = true;
				break;
			}
			BuilderToolPanel.Visible = false;
			BasicCraftingPanel.Visible = true;
			BasicCraftingPanel.SetupWindow(inventoryWindow);
			break;
		case 4:
			ProcessingPanel.Visible = true;
			ProcessingPanel.SetupWindow(inventoryWindow);
			break;
		case 2:
			DiagramCraftingPanel.Visible = true;
			DiagramCraftingPanel.SetupWindow(inventoryWindow);
			break;
		case 3:
			StructuralCraftingPanel.Visible = true;
			StructuralCraftingPanel.SetupWindow(inventoryWindow);
			break;
		case 0:
			ContainerPanel.Visible = true;
			ContainerPanel.SetupWindow(inventoryWindow);
			break;
		}
		Layout();
	}

	protected override void AfterChildrenLayout()
	{
		if (InGameView.InventoryWindow == null)
		{
			return;
		}
		int num = Desktop.UnscaleRound(StoragePanel.Find<Group>("Panel").AnchoredRectangle.Height);
		int num2 = Desktop.UnscaleRound(GetCurrentContextPanel().Find<Group>("Panel").AnchoredRectangle.Height);
		int num3 = num + num2 + _panelSpacing + StoragePanel.Offset - _hotbarHeight;
		Group group = CharacterPanel.Find<Group>("Panel");
		int num4 = Desktop.UnscaleRound(group.AnchoredRectangle.Height);
		int num5 = System.Math.Max(num3 - num4 + _hotbarHeight, _hotbarHeight);
		if (group.Anchor.Bottom != num5)
		{
			group.Anchor.Bottom = num5;
			group.Layout();
		}
		if (BlockInfoPanel.Visible)
		{
			Group group2 = BlockInfoPanel.Find<Group>("Panel");
			int num6 = Desktop.UnscaleRound(group2.AnchoredRectangle.Height);
			int num7 = num3 + _hotbarHeight - num6;
			if (group2.Anchor.Bottom != num7)
			{
				group2.Anchor.Bottom = num7;
				group2.Layout();
			}
		}
		if (BuilderToolPanel.Visible)
		{
			Group group3 = BuilderToolPanel.Find<Group>("Panel");
			int num8 = Desktop.UnscaleRound(group3.AnchoredRectangle.Height);
			int num9 = num3 + _hotbarHeight - num8;
			if (group3.Anchor.Bottom != num9)
			{
				group3.Anchor.Bottom = num9;
				group3.Layout();
			}
		}
		if (SelectionCommandsPanel.Visible)
		{
			Group group4 = SelectionCommandsPanel.Find<Group>("Panel");
			int num10 = Desktop.UnscaleRound(group4.AnchoredRectangle.Height);
			int num11 = num3 + _hotbarHeight - num10;
			if (group4.Anchor.Bottom != num11)
			{
				group4.Anchor.Bottom = num11;
				group4.Layout();
			}
		}
	}

	public void UpdateWindows()
	{
		if (InGameView.InventoryWindow != null && GetCurrentContextPanel() is WindowPanel windowPanel)
		{
			windowPanel.UpdateWindow(InGameView.InventoryWindow);
		}
	}

	public void ResetState()
	{
		ItemLibraryPanel.ResetState();
		BasicCraftingPanel.ResetState();
		StructuralCraftingPanel.ResetState();
		DiagramCraftingPanel.ResetState();
		FieldcraftCategories = null;
		KnownCraftingRecipes.Clear();
		ResourceTypes.Clear();
	}

	public void SendWindowAction(int windowId, string action, JObject data)
	{
		InGameView.InGame.SendSendWindowActionPacket(windowId, action, ((object)data)?.ToString());
	}
}
