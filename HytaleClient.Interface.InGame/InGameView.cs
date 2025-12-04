#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HytaleClient.Application;
using HytaleClient.Core;
using HytaleClient.Data.Entities;
using HytaleClient.Data.EntityStats;
using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.InGame.Modules;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Interface.InGame.Hud;
using HytaleClient.Interface.InGame.Hud.Abilities;
using HytaleClient.Interface.InGame.Hud.StatusEffects;
using HytaleClient.Interface.InGame.Overlays;
using HytaleClient.Interface.InGame.Pages;
using HytaleClient.Interface.InGame.Pages.InventoryPanels;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.Interface.InGame;

internal class InGameView : InterfaceComponent
{
	public class ClientInventoryPosition
	{
		public int InventorySectionId;

		public int SlotId;
	}

	internal readonly AppInGame InGame;

	public int NextPreviewId = 100;

	public readonly EntityUIContainer EntityUIContainer;

	public readonly Group HudContainer;

	public readonly Group PageContainer;

	public readonly Group ToolsSettingsPageContainer;

	public readonly CustomHud CustomHud;

	public readonly UtilitySlotSelector UtilitySlotSelector;

	public readonly ConsumableSlotSelector ConsumableSlotSelector;

	public readonly BuilderToolsMaterialSlotSelector BuilderToolsMaterialSlotSelector;

	public readonly HotbarComponent HotbarComponent;

	public readonly StaminaPanelComponent StaminaPanelComponent;

	public readonly AmmoIndicator AmmoIndicator;

	public readonly HealthComponent HealthComponent;

	public readonly OxygenComponent OxygenComponent;

	public readonly NotificationFeedComponent NotificationFeedComponent;

	public readonly KillFeedComponent KillFeedComponent;

	public readonly ReticleComponent ReticleComponent;

	public readonly StatusEffectsHudComponent StatusEffectsHudComponent;

	public readonly AbilitiesHudComponent AbilitiesHudComponent;

	public readonly InputBindingsComponent InputBindingsComponent;

	public readonly CompassComponent CompassComponent;

	public readonly ObjectivePanelComponent ObjectivePanelComponent;

	public readonly EventTitleComponent EventTitleComponent;

	public readonly ChatComponent ChatComponent;

	public readonly PlayerListComponent PlayerListComponent;

	public readonly BuilderToolsLegend BuilderToolsLegend;

	public readonly SpeedometerComponent SpeedometerComponent;

	public readonly MovementIndicator MovementIndicator;

	public readonly DebugStressComponent DebugStressComponent;

	public readonly InventoryPage InventoryPage;

	public readonly MapPage MapPage;

	public readonly ToolsSettingsPage ToolsSettingsPage;

	public readonly MediaBrowserPage MediaBrowserPage;

	public readonly CustomPage CustomPage;

	public readonly InGameMenuOverlay InGameMenuOverlay;

	public readonly MachinimaEditorOverlay MachinimaEditorOverlay;

	public readonly ConfirmQuitOverlay ConfirmQuitToDesktopOverlay;

	public readonly ItemQuantityPopup ItemQuantityPopup;

	private readonly HashSet<string> _mountedTextureAssetPaths = new HashSet<string>();

	private readonly Dictionary<string, TextureArea> _mountedAssetTextureAreasByLocalPath = new Dictionary<string, TextureArea>();

	public readonly Dictionary<Guid, PacketHandler.PlayerListPlayer> Players = new Dictionary<Guid, PacketHandler.PlayerListPlayer>();

	private HudComponent[] _visibleHudComponents = (HudComponent[])(object)new HudComponent[0];

	private readonly MarkupErrorOverlay _customUIMarkupErrorOverlay;

	private bool _hasCustomUIMarkupError;

	public bool Wielding;

	public PacketHandler.InventoryWindow InventoryWindow;

	private ClientItemStack _visibleDragStack;

	private FloatingItemComponent _cursorFloatingItem;

	private bool _isDropItemBindingDown;

	private float _dropBindingHeldTick;

	private bool _hasDroppedStack;

	public Dictionary<string, ClientItemBase> Items { get; private set; }

	public string Motd { get; private set; }

	public string ServerName { get; private set; }

	public int MaxPlayers { get; private set; }

	public bool HasFocusedElement => Desktop.FocusedElement != null && (Desktop.FocusedElement != CustomPage || CustomPage.HasPageDesktopFocusedElement);

	public ClientInventoryPosition HoveredItemSlot { get; private set; }

	public ClientInventoryPosition HighlightedItemSlot { get; private set; }

	public ClientItemStack[] StorageStacks { get; private set; }

	public ClientItemStack[] ArmorStacks { get; private set; }

	public ClientItemStack[] HotbarStacks { get; private set; }

	public ClientItemStack[] UtilityStacks { get; private set; }

	public ClientItemStack[] ConsumableStacks { get; private set; }

	public ItemGrid.ItemDragData ItemDragData { get; private set; }

	public int DefaultItemSlotsPerRow { get; private set; }

	public ItemGrid.ItemGridStyle DefaultItemGridStyle { get; private set; }

	public Texture ItemIconAtlasTexture { get; private set; }

	public InGameView(Interface @interface)
		: base(@interface, null)
	{
		InGame = Interface.App.InGame;
		_customUIMarkupErrorOverlay = new CustomMarkupErrorOverlay(this);
		EntityUIContainer = new EntityUIContainer(Desktop, this);
		HudContainer = new Group(Desktop, this);
		PageContainer = new Group(Desktop, this);
		ToolsSettingsPageContainer = new Group(Desktop, this);
		CustomHud = new CustomHud(this);
		UtilitySlotSelector = new UtilitySlotSelector(this);
		ConsumableSlotSelector = new ConsumableSlotSelector(this);
		BuilderToolsMaterialSlotSelector = new BuilderToolsMaterialSlotSelector(this);
		HotbarComponent = new HotbarComponent(this);
		StaminaPanelComponent = new StaminaPanelComponent(this);
		AmmoIndicator = new AmmoIndicator(this);
		HealthComponent = new HealthComponent(this);
		OxygenComponent = new OxygenComponent(this);
		NotificationFeedComponent = new NotificationFeedComponent(this);
		KillFeedComponent = new KillFeedComponent(this);
		ReticleComponent = new ReticleComponent(this);
		StatusEffectsHudComponent = new StatusEffectsHudComponent(this);
		AbilitiesHudComponent = new AbilitiesHudComponent(this);
		InputBindingsComponent = new InputBindingsComponent(this);
		CompassComponent = new CompassComponent(this);
		BuilderToolsLegend = new BuilderToolsLegend(this);
		SpeedometerComponent = new SpeedometerComponent(this);
		MovementIndicator = new MovementIndicator(this);
		ObjectivePanelComponent = new ObjectivePanelComponent(this);
		EventTitleComponent = new EventTitleComponent(this);
		ChatComponent = new ChatComponent(this);
		PlayerListComponent = new PlayerListComponent(this);
		InventoryPage = new InventoryPage(this);
		MapPage = new MapPage(this);
		ToolsSettingsPage = new ToolsSettingsPage(this);
		MediaBrowserPage = new MediaBrowserPage(this);
		CustomPage = new CustomPage(this);
		InGameMenuOverlay = new InGameMenuOverlay(this);
		MachinimaEditorOverlay = new MachinimaEditorOverlay(this);
		ConfirmQuitToDesktopOverlay = new ConfirmQuitOverlay(this);
		ItemQuantityPopup = new ItemQuantityPopup(this);
		DebugStressComponent = new DebugStressComponent(this);
		Interface.RegisterForEventFromEngine<Dictionary<string, ClientItemBase>>("items.initialized", OnItemsInitialized);
		Interface.RegisterForEventFromEngine<Dictionary<string, ClientItemBase>>("items.added", OnItemsAdded);
		Interface.RegisterForEventFromEngine<string[]>("items.removed", OnItemsRemoved);
		Interface.RegisterForEventFromEngine<ClientItemStack[], ClientItemStack[], ClientItemStack[], ClientItemStack[], ClientItemStack[]>("inventory.setAll", OnInventorySetAll);
		Interface.RegisterForEventFromEngine<PacketHandler.InventoryWindow>("inventory.windows.open", OnWindowOpen);
		Interface.RegisterForEventFromEngine<PacketHandler.InventoryWindow>("inventory.windows.update", OnWindowUpdate);
		Interface.RegisterForEventFromEngine<int>("inventory.windows.close", OnWindowClose);
		Interface.RegisterForEventFromEngine<SortType>("inventory.setAutosortType", OnSetAutosortType);
		Interface.RegisterForEventFromEngine<int>("game.setActiveHotbarSlot", OnSetActiveHotbarSlot);
		Interface.RegisterForEventFromEngine<int>("game.setActiveUtilitySlot", OnSetActiveUtilitySlot);
		Interface.RegisterForEventFromEngine<int>("game.setActiveConsumableSlot", OnSetActiveConsumableSlot);
		Interface.RegisterForEventFromEngine<int>("game.setActiveToolsSlot", OnSetActiveToolsSlot);
		Interface.RegisterForEventFromEngine<int, ClientEntityStatValue, float?>("game.entityStats.set", OnStatChanged);
		Interface.RegisterForEventFromEngine("inventory.dropItemBindingDown", OnDropItemBindingDown);
		Interface.RegisterForEventFromEngine("inventory.dropItemBindingUp", OnDropItemBindingUp);
		Interface.RegisterForEventFromEngine<string[]>("assets.updated", OnAssetsUpdated);
		Interface.RegisterForEventFromEngine<HudComponent[]>("hud.visibleComponentsUpdated", OnVisibleHudComponentsUpdated);
		Interface.RegisterForEventFromEngine("settings.inputBindingsUpdated", InputBindingsUpdated);
	}

	public void Build()
	{
		Interface.TryGetDocument("InGame/Common.ui", out var document);
		DefaultItemSlotsPerRow = document.ResolveNamedValue<int>(Interface, "DefaultItemSlotsPerRow");
		DefaultItemGridStyle = document.ResolveNamedValue<ItemGrid.ItemGridStyle>(Interface, "DefaultItemGridStyle");
		_cursorFloatingItem = new FloatingItemComponent(this, null);
		SetupCursorFloatingItem();
		EntityUIContainer.Build();
		CustomHud.Build();
		UtilitySlotSelector.Build();
		ConsumableSlotSelector.Build();
		BuilderToolsMaterialSlotSelector.Build();
		HotbarComponent.Build();
		StaminaPanelComponent.Build();
		AmmoIndicator.Build();
		HealthComponent.Build();
		OxygenComponent.Build();
		NotificationFeedComponent.Build();
		KillFeedComponent.Build();
		ReticleComponent.Build();
		StatusEffectsHudComponent.Build();
		AbilitiesHudComponent.Build();
		InputBindingsComponent.Build();
		ObjectivePanelComponent.Build();
		EventTitleComponent.Build();
		ChatComponent.Build();
		PlayerListComponent.Build();
		BuilderToolsLegend.Build();
		SpeedometerComponent.Build();
		MovementIndicator.Build();
		UpdateHudWidgetVisibility(doLayout: false);
		InGameMenuOverlay.Build();
		MachinimaEditorOverlay.Build();
		ConfirmQuitToDesktopOverlay.Build();
		InventoryPage.Build();
		MapPage.Build();
		ToolsSettingsPage.Build();
		MediaBrowserPage.Build();
		CustomPage.Build();
		ItemQuantityPopup.Build();
		DebugStressComponent.Visible = false;
	}

	protected override void OnMounted()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		if (_hasCustomUIMarkupError)
		{
			Desktop.SetLayer(1, _customUIMarkupErrorOverlay);
		}
		if ((int)InGame.Instance.GameMode == 1)
		{
			BuilderToolsLegend.UpdateInputHints();
			if (InGame.Instance.BuilderToolsModule.TrySelectHotbarActiveTool())
			{
				UpdateBuilderToolsLegendVisibility();
			}
		}
	}

	public void DisplayMarkupError(string message, TextParserSpan span)
	{
		_hasCustomUIMarkupError = true;
		_customUIMarkupErrorOverlay.Setup(message, span);
		if (base.IsMounted)
		{
			if (_customUIMarkupErrorOverlay.IsMounted)
			{
				_customUIMarkupErrorOverlay.Layout();
			}
			else
			{
				Desktop.SetLayer(1, _customUIMarkupErrorOverlay);
			}
		}
	}

	public void ClearMarkupError()
	{
		if (_hasCustomUIMarkupError)
		{
			_hasCustomUIMarkupError = false;
			_customUIMarkupErrorOverlay.Clear();
			if (_customUIMarkupErrorOverlay.IsMounted)
			{
				Desktop.ClearLayer(1);
			}
		}
	}

	protected override void OnUnmounted()
	{
		Debug.Assert(_mountedAssetTextureAreasByLocalPath.Count == 0);
		Interface.InGameCustomUIProvider.DisposeTextures();
		ItemIconAtlasTexture?.Dispose();
		ItemIconAtlasTexture = null;
		if (_isDropItemBindingDown)
		{
			_isDropItemBindingDown = false;
			Desktop.UnregisterAnimationCallback(CheckForItemsToDrop);
		}
	}

	public void RegisterEntityUIDrawTasks(ref Matrix transformationMatrix, Entity entity, float distanceToCamera)
	{
		EntityUIContainer.RegisterDrawTasksForEntity(ref transformationMatrix, entity, distanceToCamera);
	}

	protected internal override void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Invalid comparison between Unknown and I4
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		base.OnKeyDown(keycode, repeat);
		if (InGame.CurrentOverlay != 0)
		{
			return;
		}
		Page currentPage = InGame.CurrentPage;
		Page val = currentPage;
		if (val - 1 > 1)
		{
			if ((int)val == 6)
			{
				MapPage.OnKeyDown(keycode, repeat);
			}
		}
		else
		{
			InventoryPage.OnKeyDown(keycode, repeat);
		}
	}

	protected internal override void OnKeyUp(SDL_Keycode keycode)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (InGame.CurrentOverlay == AppInGame.InGameOverlay.None && (int)InGame.CurrentPage == 6)
		{
			MapPage.OnKeyUp(keycode);
		}
	}

	public void OnCharacterControllerTicked(ClientMovementStates movementStates)
	{
		MovementIndicator.Update(movementStates);
	}

	protected internal override void Dismiss()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		if (InGame.Instance.Chat.IsOpen)
		{
			InGame.Instance.Chat.Close();
			return;
		}
		if (InGame.CurrentOverlay == AppInGame.InGameOverlay.None && (int)InGame.CurrentPage == 0 && !PlayerListComponent.Visible && !InGame.IsToolsSettingsModalOpened)
		{
			InGame.SetCurrentOverlay(AppInGame.InGameOverlay.InGameMenu);
			return;
		}
		InGame.TryClosePageOrOverlay();
		InGame.CloseToolsSettingsModal();
	}

	public void OnReset(bool isStayingConnected)
	{
		if (Interface.HasMarkupError)
		{
			return;
		}
		foreach (TextureArea value in _mountedAssetTextureAreasByLocalPath.Values)
		{
			value.Texture.Dispose();
		}
		_mountedAssetTextureAreasByLocalPath.Clear();
		_mountedTextureAssetPaths.Clear();
		ItemIconAtlasTexture?.Dispose();
		ItemIconAtlasTexture = null;
		PageContainer.Clear();
		CustomHud.ResetState();
		UtilitySlotSelector.ResetState();
		ConsumableSlotSelector.ResetState();
		BuilderToolsMaterialSlotSelector.ResetState();
		ChatComponent.ResetState();
		KillFeedComponent.ResetState();
		StaminaPanelComponent.ResetState();
		AmmoIndicator.ResetState();
		HealthComponent.ResetState();
		OxygenComponent.ResetState();
		NotificationFeedComponent.ResetState();
		ReticleComponent.ResetState(isStayingConnected);
		InputBindingsComponent.ResetState();
		CompassComponent.ResetState();
		ObjectivePanelComponent.ResetState();
		EventTitleComponent.ResetState();
		HotbarComponent.ResetState();
		ChatComponent.ResetState();
		PlayerListComponent.ResetState();
		BuilderToolsLegend.ResetState();
		Motd = null;
		ServerName = null;
		MaxPlayers = 0;
		Players.Clear();
		if (PlayerListComponent.IsMounted)
		{
			PlayerListComponent.UpdateList();
			PlayerListComponent.UpdateServerDetails();
		}
		_visibleHudComponents = (HudComponent[])(object)new HudComponent[0];
		UpdateHudWidgetVisibility(doLayout: false);
		Items = null;
		NextPreviewId = 100;
		HoveredItemSlot = null;
		StorageStacks = null;
		ArmorStacks = null;
		HotbarStacks = null;
		UtilityStacks = null;
		ConsumableStacks = null;
		InventoryWindow = null;
		if (Desktop.IsMouseDragging && ItemDragData != null)
		{
			Desktop.CancelMouseDrag();
		}
		SetupDragAndDropItem(null);
		InventoryPage.ResetState();
		MediaBrowserPage.ResetState();
		CustomPage.ResetState();
		if (Desktop.GetLayer(3) != null)
		{
			Desktop.ClearLayer(3);
		}
		if (base.IsMounted && isStayingConnected)
		{
			Layout();
		}
	}

	public void PlayPageCloseSound(Page closedPage)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		if ((int)closedPage != 2)
		{
			if ((int)closedPage == 6)
			{
				Interface.PlaySound(MapPage.CloseSound);
			}
		}
		else
		{
			Interface.PlaySound(InventoryPage.CloseSound);
		}
	}

	public void PlayPageOpenSound(Page openedPage)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		if ((int)openedPage != 2)
		{
			if ((int)openedPage == 6)
			{
				Interface.PlaySound(MapPage.OpenSound);
			}
		}
		else
		{
			Interface.PlaySound(InventoryPage.OpenSound);
		}
	}

	public void CloseToolsSettingsModal()
	{
		ToolsSettingsPageContainer.Clear();
		ReparentChat(doLayout: false);
		UpdateHudWidgetVisibility();
		InputBindingsComponent.UpdateBindings();
		HotbarComponent.OnPageChanged();
		Layout();
	}

	public void OpenToolsSettingsPage()
	{
		ToolsSettingsPageContainer.Clear();
		ToolsSettingsPageContainer.Add(ToolsSettingsPage);
		ReparentChat(doLayout: false);
		UpdateHudWidgetVisibility();
		InputBindingsComponent.UpdateBindings();
		HotbarComponent.OnPageChanged();
		InventoryPage.SelectionCommandsPanel.Visible = false;
		InventoryPage.Layout();
		InventoryPage.BuilderToolPanel.Visible = false;
		InventoryPage.BuilderToolPanel.Layout();
		Layout();
	}

	public void OnPageChanged()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected I4, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Invalid comparison between Unknown and I4
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Invalid comparison between Unknown and I4
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Invalid comparison between Unknown and I4
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Invalid comparison between Unknown and I4
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Invalid comparison between Unknown and I4
		if (InventoryWindow != null)
		{
			CloseAllInventoryWindows();
		}
		InterfaceComponent child = null;
		Page currentPage = InGame.CurrentPage;
		Page val = currentPage;
		switch (val - 1)
		{
		case 0:
			child = InventoryPage;
			break;
		case 1:
			child = InventoryPage;
			break;
		case 6:
			child = CustomPage;
			break;
		case 5:
			child = MapPage;
			break;
		case 2:
			child = MediaBrowserPage;
			break;
		case 4:
			child = ToolsSettingsPage;
			break;
		}
		PageContainer.Clear();
		if ((int)InGame.CurrentPage > 0)
		{
			PageContainer.Add(child);
		}
		ReparentChat(doLayout: false);
		if ((int)InGame.CurrentPage == 2)
		{
			InGame.SendOpenInventoryPacket();
			PacketHandler.InventoryWindow obj = new PacketHandler.InventoryWindow
			{
				Id = 0,
				WindowType = (WindowType)1
			};
			JObject val2 = new JObject();
			val2.Add("id", JToken.op_Implicit("Fieldcraft"));
			val2.Add("name", JToken.op_Implicit("ui.windows.fieldcraft"));
			val2.Add("type", JToken.op_Implicit(0));
			obj.WindowData = val2;
			InventoryWindow = obj;
			InventoryPage.IsFieldcraft = true;
			InventoryPage.SetupWindows();
		}
		else if ((int)InGame.CurrentPage == 7)
		{
			Desktop.FocusElement(CustomPage);
		}
		if (PlayerListComponent.IsMounted)
		{
			InGame.SetPlayerListVisible(visible: false);
		}
		if ((int)InGame.Instance.GameMode == 1 && ((int)InGame.CurrentPage == 0 || (int)InGame.CurrentPage == 2))
		{
			InGame.Instance.BuilderToolsModule.TrySelectActiveTool();
		}
		UpdateHudWidgetVisibility();
		InputBindingsComponent.UpdateBindings();
		HotbarComponent.OnPageChanged();
		Layout();
	}

	public void OnOverlayChanged()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Invalid comparison between Unknown and I4
		Element layer = null;
		switch (InGame.CurrentOverlay)
		{
		case AppInGame.InGameOverlay.InGameMenu:
			layer = InGameMenuOverlay;
			break;
		case AppInGame.InGameOverlay.MachinimaEditor:
			layer = MachinimaEditorOverlay;
			break;
		case AppInGame.InGameOverlay.ConfirmQuit:
			layer = ConfirmQuitToDesktopOverlay;
			break;
		}
		if (Desktop.GetLayer(3) != null)
		{
			Desktop.ClearLayer(3);
		}
		if (InGame.CurrentOverlay != 0)
		{
			Desktop.SetLayer(3, layer);
		}
		else if ((int)InGame.CurrentPage == 7)
		{
			Desktop.FocusElement(CustomPage);
		}
		UpdateReticleVisibility(doLayout: true);
		UpdateBuilderToolsLegendVisibility(doLayout: true);
		if (PlayerListComponent.IsMounted)
		{
			InGame.SetPlayerListVisible(visible: false);
		}
	}

	public void TryClosePageOrOverlayWithInputBinding()
	{
		if (!HasFocusedElement)
		{
			InGame.TryClosePageOrOverlay();
		}
	}

	private void OnVisibleHudComponentsUpdated(HudComponent[] components)
	{
		_visibleHudComponents = components;
		UpdateHudWidgetVisibility();
	}

	private void InputBindingsUpdated()
	{
		if (base.IsMounted)
		{
			BuilderToolsLegend.UpdateInputHints(doClear: true, doLayout: true);
		}
	}

	public void UpdateBuilderToolsLegendVisibility(bool doLayout = false)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		BuilderToolsLegend.Visible = _visibleHudComponents.Contains((HudComponent)12) && InGame.IsHudVisible && Interface.App.Stage == App.AppStage.InGame && (int)InGame.Instance.GameMode == 1 && InGame.Instance.BuilderToolsModule.HasActiveBrush && Interface.App.Settings.BuilderToolsSettings.DisplayLegend && (int)InGame.CurrentPage == 0 && InGame.CurrentOverlay == AppInGame.InGameOverlay.None;
		if (BuilderToolsLegend.IsMounted && doLayout)
		{
			BuilderToolsLegend.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void UpdateSpeedometerVisibility(bool doLayout = false)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		SpeedometerComponent.Visible = _visibleHudComponents.Contains((HudComponent)13) && InGame.IsHudVisible && Interface.App.Stage == App.AppStage.InGame && (int)InGame.CurrentPage == 0 && InGame.CurrentOverlay == AppInGame.InGameOverlay.None && SpeedometerComponent.Enabled;
		if (SpeedometerComponent.IsMounted && doLayout)
		{
			SpeedometerComponent.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void UpdateMovementIndicatorVisibility(bool doLayout = false)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		MovementIndicator.Visible = _visibleHudComponents.Contains((HudComponent)14) && InGame.IsHudVisible && (int)InGame.CurrentPage == 0 && InGame.CurrentOverlay == AppInGame.InGameOverlay.None;
		if (MovementIndicator.IsMounted && doLayout)
		{
			MovementIndicator.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void UpdateReticleVisibility(bool doLayout = false)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		ReticleComponent.Visible = _visibleHudComponents.Contains((HudComponent)2) && InGame.IsHudVisible && ReticleComponent.IsReticleVisible && (int)InGame.CurrentPage == 0 && InGame.CurrentOverlay == AppInGame.InGameOverlay.None;
		if (ReticleComponent.IsMounted && doLayout)
		{
			ReticleComponent.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void OnPlayerListVisibilityChanged()
	{
		UpdatePlayerListVisibility(doLayout: true);
	}

	public void UpdatePlayerListVisibility(bool doLayout = false)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		PlayerListComponent.Visible = _visibleHudComponents.Contains((HudComponent)8) && InGame.IsHudVisible && InGame.IsPlayerListVisible && (int)InGame.CurrentPage == 0 && InGame.CurrentOverlay == AppInGame.InGameOverlay.None;
		if (PlayerListComponent.IsMounted && doLayout)
		{
			PlayerListComponent.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void UpdateUtilitySlotSelectorVisibility(bool doLayout = false)
	{
		UtilitySlotSelector.Visible = _visibleHudComponents.Contains((HudComponent)15) && InGame.IsHudVisible && InGame.ActiveItemSelector == AppInGame.ItemSelector.Utility;
		if (UtilitySlotSelector.IsMounted && doLayout)
		{
			UtilitySlotSelector.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void UpdateConsumableSlotSelectorVisibility(bool doLayout = false)
	{
		ConsumableSlotSelector.Visible = _visibleHudComponents.Contains((HudComponent)16) && InGame.IsHudVisible && InGame.ActiveItemSelector == AppInGame.ItemSelector.Consumable;
		if (ConsumableSlotSelector.IsMounted && doLayout)
		{
			ConsumableSlotSelector.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void UpdateBuilderToolsMaterialSlotSelectorVisibility(bool doLayout = false)
	{
		BuilderToolsMaterialSlotSelector.Visible = _visibleHudComponents.Contains((HudComponent)17) && InGame.IsHudVisible && InGame.ActiveItemSelector == AppInGame.ItemSelector.BuilderToolsMaterial;
		if (BuilderToolsMaterialSlotSelector.IsMounted && doLayout)
		{
			BuilderToolsMaterialSlotSelector.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void UpdateObjectivePanelVisibility(bool doLayout = false)
	{
		ObjectivePanelComponent.Visible = _visibleHudComponents.Contains((HudComponent)11) && InGame.IsHudVisible && ObjectivePanelComponent.HasObjectives;
		if (ObjectivePanelComponent.IsMounted && doLayout)
		{
			ObjectivePanelComponent.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void UpdateStaminaPanelVisibility(bool doLayout = false)
	{
		StaminaPanelComponent.Visible = _visibleHudComponents.Contains((HudComponent)18) && InGame.IsHudVisible && StaminaPanelComponent.ShouldDisplay;
		if (StaminaPanelComponent.IsMounted && doLayout)
		{
			StaminaPanelComponent.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void UpdateAmmoIndicatorVisibility(bool doLayout = false)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		AmmoIndicator.Visible = _visibleHudComponents.Contains((HudComponent)19) && InGame.IsHudVisible && InGame.Instance != null && (int)InGame.Instance.GameMode == 0 && InGame.Instance.LocalPlayer != null && InGame.Instance.LocalPlayer.ShouldDisplayHudForEntityStat(DefaultEntityStats.Ammo);
		if (AmmoIndicator.IsMounted && doLayout)
		{
			AmmoIndicator.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void UpdateHealthVisibility(bool doLayout = false)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		HealthComponent.Visible = _visibleHudComponents.Contains((HudComponent)20) && InGame.IsHudVisible && InGame.Instance != null && (int)InGame.Instance.GameMode == 0;
		if (HealthComponent.IsMounted && doLayout)
		{
			HealthComponent.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void UpdateOxygenVisibility(bool doLayout = false)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		OxygenComponent.Visible = _visibleHudComponents.Contains((HudComponent)21) && InGame.IsHudVisible && InGame.Instance != null && (int)InGame.Instance.GameMode == 0 && OxygenComponent.Display;
		if (OxygenComponent.IsMounted && doLayout)
		{
			OxygenComponent.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void UpdateStatusEffectHudVisibility(bool doLayout = false)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		StatusEffectsHudComponent.Visible = InGame.IsHudVisible && InGame.Instance != null && (int)InGame.Instance.GameMode == 0;
		if (StatusEffectsHudComponent.IsMounted && doLayout)
		{
			StatusEffectsHudComponent.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	public void UpdateAbilitiesHudVisibility(bool doLayout = false)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		AbilitiesHudComponent.Visible = InGame.IsHudVisible && InGame.Instance != null && (int)InGame.Instance.GameMode == 0;
		if (AbilitiesHudComponent.IsMounted && doLayout)
		{
			AbilitiesHudComponent.Layout(HudContainer.RectangleAfterPadding);
		}
	}

	private void UpdateHudWidgetVisibility(bool doLayout = true)
	{
		bool isHudVisible = InGame.IsHudVisible;
		EntityUIContainer.Visible = isHudVisible;
		HotbarComponent.Visible = HotbarComponent.Parent != HudContainer || (_visibleHudComponents.Contains((HudComponent)0) && isHudVisible);
		UpdateUtilitySlotSelectorVisibility();
		UpdateConsumableSlotSelectorVisibility();
		UpdateBuilderToolsMaterialSlotSelectorVisibility();
		NotificationFeedComponent.Visible = _visibleHudComponents.Contains((HudComponent)5) && isHudVisible;
		KillFeedComponent.Visible = _visibleHudComponents.Contains((HudComponent)6) && isHudVisible;
		InputBindingsComponent.Visible = _visibleHudComponents.Contains((HudComponent)7) && isHudVisible;
		CompassComponent.Visible = _visibleHudComponents.Contains((HudComponent)10) && isHudVisible;
		UpdateReticleVisibility();
		UpdateBuilderToolsLegendVisibility();
		UpdateSpeedometerVisibility();
		UpdateMovementIndicatorVisibility();
		UpdateObjectivePanelVisibility();
		UpdatePlayerListVisibility();
		UpdateStaminaPanelVisibility();
		UpdateAmmoIndicatorVisibility();
		UpdateHealthVisibility();
		UpdateOxygenVisibility();
		UpdateStatusEffectHudVisibility();
		UpdateAbilitiesHudVisibility();
		ChatComponent.Visible = _visibleHudComponents.Contains((HudComponent)3);
		if (EntityUIContainer.IsMounted && doLayout)
		{
			EntityUIContainer.Layout(_rectangleAfterPadding);
		}
		if (HudContainer.IsMounted && doLayout)
		{
			HudContainer.Layout(_rectangleAfterPadding);
		}
		if (HotbarComponent.IsMounted && HotbarComponent.Parent != HudContainer)
		{
			HotbarComponent.Layout(HotbarComponent.Parent.RectangleAfterPadding);
		}
		if (InputBindingsComponent.IsMounted && InputBindingsComponent.Parent != HudContainer)
		{
			InputBindingsComponent.Layout(HotbarComponent.Parent.RectangleAfterPadding);
		}
	}

	public void UpdateDebugStressVisibility()
	{
		DebugStressComponent.Visible = !DebugStressComponent.Visible;
		UpdateHudWidgetVisibility();
	}

	public void OnActiveItemSelectorChanged()
	{
		UpdateUtilitySlotSelectorVisibility(doLayout: true);
		UpdateConsumableSlotSelectorVisibility(doLayout: true);
		UpdateBuilderToolsMaterialSlotSelectorVisibility(doLayout: true);
		HotbarComponent.OnToggleItemSlotSelector();
	}

	public void OnActiveBuilderToolSelected(bool hasActiveBrush, int? favoriteMaterialsCount)
	{
		HighlightSlot(-1, InGame.Instance.InventoryModule.HotbarActiveSlot);
		if (hasActiveBrush && favoriteMaterialsCount > 0)
		{
			BuilderToolsMaterialSlotSelector.SetItemStacks(InGame.Instance.BuilderToolsModule.ActiveTool.BrushData.GetFavoriteMaterialStacks());
		}
	}

	public void OnHudVisibilityChanged()
	{
		UpdateHudWidgetVisibility();
		ChatComponent.OnHudVisibilityChanged();
	}

	public void OnChatOpened(SDL_Keycode? keyCodeTrigger, bool isCommand)
	{
		ReparentChat();
		ChatComponent.OnOpened(keyCodeTrigger, isCommand);
	}

	public void OnChatClosed()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		ReparentChat();
		ChatComponent.OnClosed();
		if ((int)InGame.CurrentPage == 7)
		{
			Desktop.FocusElement(CustomPage);
		}
	}

	private void ReparentChat(bool doLayout = true)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if ((int)InGame.CurrentPage == 0 || !ChatComponent.IsOpen())
		{
			if (ChatComponent.Parent != HudContainer)
			{
				ChatComponent.Parent?.Remove(ChatComponent);
				HudContainer.Add(ChatComponent, PlayerListComponent);
				if (doLayout)
				{
					HudContainer.Layout();
				}
			}
		}
		else if (ChatComponent.Parent != PageContainer)
		{
			ChatComponent.Parent?.Remove(ChatComponent);
			PageContainer.Add(ChatComponent);
			if (doLayout)
			{
				PageContainer.Layout();
			}
		}
	}

	public void OnGameModeChanged()
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Invalid comparison between Unknown and I4
		InventoryPage.SetupWindows();
		HotbarComponent.UpdateBackgroundVisibility();
		UpdateBuilderToolsLegendVisibility(doLayout: true);
		UpdateStaminaPanelVisibility(doLayout: true);
		UpdateAmmoIndicatorVisibility(doLayout: true);
		UpdateHealthVisibility(doLayout: true);
		UpdateOxygenVisibility(doLayout: true);
		UpdateAbilitiesHudVisibility(doLayout: true);
		UpdateStatusEffectHudVisibility(doLayout: true);
		if ((int)InGame.Instance.GameMode == 0)
		{
			ClearSlotHighlight();
		}
	}

	private void OnItemsInitialized(Dictionary<string, ClientItemBase> items)
	{
		Items = items;
		InventoryPage.OnItemsUpdated();
	}

	private void OnItemsAdded(Dictionary<string, ClientItemBase> items)
	{
		foreach (ClientItemBase value in items.Values)
		{
			Items[value.Id] = value;
		}
		InventoryPage.OnItemsUpdated();
	}

	private void OnItemsRemoved(string[] itemIds)
	{
		foreach (string key in itemIds)
		{
			Items.Remove(key);
		}
		InventoryPage.OnItemsUpdated();
		UpdateAmmoIndicatorVisibility(doLayout: true);
	}

	public void OnItemIconsUpdated(Texture texture)
	{
		ItemIconAtlasTexture?.Dispose();
		ItemIconAtlasTexture = texture;
		SetupCursorFloatingItem();
		HotbarComponent.OnItemIconsUpdated();
		InventoryPage.OnItemIconsUpdated();
	}

	public void OnServerInfoUpdate(string serverName, string motd, int maxPlayers)
	{
		ServerName = serverName;
		Motd = motd;
		MaxPlayers = maxPlayers;
		if (PlayerListComponent.IsMounted)
		{
			PlayerListComponent.UpdateServerDetails();
		}
	}

	public void OnPlayerListAdd(PacketHandler.PlayerListPlayer[] players)
	{
		foreach (PacketHandler.PlayerListPlayer playerListPlayer in players)
		{
			Players[playerListPlayer.Uuid] = playerListPlayer;
		}
		if (PlayerListComponent.IsMounted)
		{
			PlayerListComponent.UpdateList();
		}
	}

	public void OnPlayerListRemove(Guid[] players)
	{
		foreach (Guid key in players)
		{
			Players.Remove(key);
		}
		if (PlayerListComponent.IsMounted)
		{
			PlayerListComponent.UpdateList();
		}
	}

	public void OnPlayerListUpdate(Dictionary<Guid, int> players)
	{
		foreach (KeyValuePair<Guid, int> player in players)
		{
			if (Players.TryGetValue(player.Key, out var value))
			{
				value.Ping = player.Value;
			}
		}
		if (PlayerListComponent.IsMounted)
		{
			PlayerListComponent.UpdateList();
		}
	}

	public void OnPlayerListClear()
	{
		Players.Clear();
		if (PlayerListComponent.IsMounted)
		{
			PlayerListComponent.UpdateList();
		}
	}

	private void OnAssetsUpdated(string[] assetNames)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		foreach (string text in assetNames)
		{
			if (_mountedTextureAssetPaths.Contains(text))
			{
				flag = true;
			}
			if (text.StartsWith("UI/Custom/"))
			{
				if (text.EndsWith(".ui"))
				{
					flag2 = true;
				}
				else if (text.EndsWith(".png"))
				{
					flag3 = true;
				}
			}
		}
		if (flag)
		{
			ReloadAssetTextures();
		}
		if (flag2)
		{
			ClearMarkupError();
			try
			{
				Interface.InGameCustomUIProvider.LoadDocuments();
			}
			catch (TextParser.TextParserException ex)
			{
				Interface.InGameCustomUIProvider.ClearDocuments();
				if (!Interface.App.Settings.DiagnosticMode)
				{
					DisconnectWithError("Failed to load updated CustomUI documents", ex);
					return;
				}
				DisplayMarkupError(ex.RawMessage, ex.Span);
			}
			catch (Exception exception)
			{
				Interface.InGameCustomUIProvider.ClearDocuments();
				DisconnectWithError("Failed to load updated CustomUI documents", exception);
				return;
			}
		}
		if (flag3)
		{
			try
			{
				Interface.InGameCustomUIProvider.LoadTextures(Desktop.Scale > 1f);
			}
			catch (Exception exception2)
			{
				DisconnectWithError("Failed to load updated CustomUI textures", exception2);
			}
		}
	}

	public void OnStatChanged(int stat, ClientEntityStatValue value, float? previousValue)
	{
		if (stat == DefaultEntityStats.Health)
		{
			HealthComponent.OnStatChanged(value);
			InventoryPage.CharacterPanel.OnHealthChanged(value);
		}
		else if (stat == DefaultEntityStats.Mana)
		{
			InventoryPage.CharacterPanel.OnManaChanged(value);
		}
		else if (stat == DefaultEntityStats.Stamina)
		{
			StaminaPanelComponent.OnStatChanged(value, previousValue);
		}
		else if (stat == DefaultEntityStats.SignatureEnergy)
		{
			AbilitiesHudComponent.OnSignatureEnergyStatChanged(value);
		}
		else if (stat == DefaultEntityStats.Ammo)
		{
			AmmoIndicator.OnAmmoChanged(value);
		}
		else if (stat == DefaultEntityStats.Oxygen)
		{
			OxygenComponent.OnStatChanged(value);
		}
	}

	public void OnEffectAdded(int effectIndex)
	{
		StatusEffectsHudComponent.OnEffectAdded(effectIndex);
		StaminaPanelComponent.OnEffectAdded(effectIndex);
	}

	public void OnEffectRemoved(int effectIndex)
	{
		StatusEffectsHudComponent.OnEffectRemoved(effectIndex);
		StaminaPanelComponent.OnEffectRemoved(effectIndex);
		AbilitiesHudComponent.OnEffectRemoved(effectIndex);
	}

	public void OnReticleServerEvent(int eventIndex)
	{
		ReticleComponent.OnServerEvent(eventIndex);
	}

	public void OnReticlesUpdated()
	{
		ReticleComponent.UpdateReticleImage();
	}

	private bool TryGetServerProvidedAssetHash(string assetPath, out string hash)
	{
		hash = null;
		return InGame.Instance != null && InGame.Instance.HashesByServerAssetPath.TryGetValue(assetPath, out hash);
	}

	public bool TryMountAssetTexture(string assetPath, out TextureArea textureArea)
	{
		if (string.IsNullOrEmpty(assetPath))
		{
			textureArea = null;
			return false;
		}
		bool flag = Desktop.Scale > 1f;
		bool flag2 = false;
		int scale = 1;
		string hash = null;
		if (flag)
		{
			string text = assetPath.Substring(0, assetPath.Length - ".png".Length) + "@2x.png";
			flag2 = TryGetServerProvidedAssetHash(text, out hash);
			if (flag2)
			{
				scale = 2;
				assetPath = text;
			}
		}
		if (!flag2)
		{
			flag2 = TryGetServerProvidedAssetHash(assetPath, out hash);
		}
		if (!flag2 && !flag)
		{
			string text2 = assetPath.Substring(0, assetPath.Length - ".png".Length) + "@2x.png";
			flag2 = TryGetServerProvidedAssetHash(text2, out hash);
			if (flag2)
			{
				scale = 2;
				assetPath = text2;
			}
		}
		if (!flag2)
		{
			textureArea = null;
			return false;
		}
		string assetLocalPathUsingHash = AssetManager.GetAssetLocalPathUsingHash(hash);
		if (_mountedAssetTextureAreasByLocalPath.TryGetValue(assetLocalPathUsingHash, out textureArea))
		{
			return true;
		}
		textureArea = ExternalTextureLoader.FromPath(assetLocalPathUsingHash);
		textureArea.Scale = scale;
		_mountedAssetTextureAreasByLocalPath.Add(assetLocalPathUsingHash, textureArea);
		_mountedTextureAssetPaths.Add(assetPath);
		return true;
	}

	public void ReloadAssetTextures()
	{
		foreach (TextureArea value in _mountedAssetTextureAreasByLocalPath.Values)
		{
			value.Texture.Dispose();
		}
		_mountedAssetTextureAreasByLocalPath.Clear();
		_mountedTextureAssetPaths.Clear();
		NotificationFeedComponent.RebuildFeed();
		KillFeedComponent.RebuildFeed();
		ReticleComponent.UpdateReticleImage();
		CompassComponent.OnAssetsUpdated();
		InventoryPage.ItemLibraryPanel.SetupCategories();
	}

	internal void DisconnectWithError(string reason, Exception exception)
	{
		App.AppStage stage = Interface.App.Stage;
		App.AppStage appStage = stage;
		if ((uint)(appStage - 3) <= 1u)
		{
			InGame.Instance.DisconnectWithReason(reason, exception);
		}
		else
		{
			Debug.Fail($"Unexpected game stage: {Interface.App.Stage}");
		}
	}

	public void OpenAssetEditorMissingPathDialog()
	{
		InGame.SetCurrentOverlay(AppInGame.InGameOverlay.InGameMenu);
		InGameMenuOverlay.ShowSettings();
	}

	private void OnWindowOpen(PacketHandler.InventoryWindow window)
	{
		if (InventoryWindow?.Id != window.Id)
		{
			InventoryWindow = window;
			InventoryPage.IsFieldcraft = false;
			if (InventoryPage.IsMounted)
			{
				InventoryPage.SetupWindows();
			}
		}
	}

	private void OnWindowUpdate(PacketHandler.InventoryWindow window)
	{
		if (InventoryWindow != null && InventoryWindow.Id == window.Id)
		{
			InventoryWindow.WindowData = window.WindowData;
			InventoryWindow.Inventory = window.Inventory;
			if (InventoryPage.IsMounted)
			{
				InventoryPage.UpdateWindows();
			}
		}
	}

	private void OnWindowClose(int windowId)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Invalid comparison between Unknown and I4
		if (InventoryWindow != null && InventoryWindow.Id == windowId)
		{
			InventoryWindow = null;
			if ((int)InGame.CurrentPage == 2 || (int)InGame.CurrentPage == 1)
			{
				InGame.TryClosePage();
			}
		}
	}

	public void CloseAllInventoryWindows()
	{
		if (InventoryWindow != null)
		{
			InGame.SendCloseWindowPacket(InventoryWindow.Id);
			InventoryWindow = null;
		}
	}

	private void OnDropItemBindingDown()
	{
		if (!_isDropItemBindingDown)
		{
			_isDropItemBindingDown = true;
			Desktop.RegisterAnimationCallback(CheckForItemsToDrop);
		}
	}

	private void OnDropItemBindingUp()
	{
		if (_isDropItemBindingDown)
		{
			_isDropItemBindingDown = false;
			Desktop.UnregisterAnimationCallback(CheckForItemsToDrop);
			CheckForItemsToDrop(0f);
		}
	}

	private void CheckForItemsToDrop(float dt)
	{
		if (HoveredItemSlot == null || GetItemStacks(HoveredItemSlot.InventorySectionId)[HoveredItemSlot.SlotId] == null)
		{
			return;
		}
		if (_isDropItemBindingDown)
		{
			_dropBindingHeldTick += dt;
			if (_dropBindingHeldTick >= 0.5f && !_hasDroppedStack)
			{
				DropItemStack(HoveredItemSlot.InventorySectionId, HoveredItemSlot.SlotId);
				_hasDroppedStack = true;
			}
		}
		else
		{
			if (_dropBindingHeldTick > 0f && !_hasDroppedStack)
			{
				DropItemStack(HoveredItemSlot.InventorySectionId, HoveredItemSlot.SlotId, 1);
			}
			_hasDroppedStack = false;
			_dropBindingHeldTick = 0f;
		}
	}

	private void LayoutCursorFloatingItem(float deltaTime)
	{
		int num = -DefaultItemGridStyle.SlotSize / 4;
		if (Desktop.IsShiftKeyDown)
		{
			if (ItemDragData.ItemStack.Quantity > 1)
			{
				_visibleDragStack.Quantity = (int)System.Math.Floor((float)ItemDragData.ItemStack.Quantity / 2f);
			}
			else
			{
				_visibleDragStack.Quantity = 1;
			}
		}
		else if ((long)ItemDragData.PressedMouseButton == 3)
		{
			_visibleDragStack.Quantity = 1;
		}
		else
		{
			_visibleDragStack.Quantity = ItemDragData.ItemStack.Quantity;
		}
		_cursorFloatingItem.Anchor.Left = Desktop.UnscaleRound(Desktop.MousePosition.X) + num;
		_cursorFloatingItem.Anchor.Top = Desktop.UnscaleRound(Desktop.MousePosition.Y) + num;
		_cursorFloatingItem.Layout(_rectangleAfterPadding);
		_cursorFloatingItem.ShowDropIcon = InventoryPage.IsItemAtPositionDroppable(Desktop.MousePosition);
	}

	private void OnSetActiveHotbarSlot(int activeSlot)
	{
		InGame.SetActiveItemSelector(AppInGame.ItemSelector.None);
		HotbarComponent.OnSetActiveHotbarSlot(activeSlot);
		AbilitiesHudComponent.ShowOrHideHud();
		ReticleComponent.OnSetActiveSlot(activeSlot);
	}

	private void OnSetActiveUtilitySlot(int activeSlot)
	{
		UtilitySlotSelector.SelectedSlot = activeSlot + 1;
		HotbarComponent.OnSetActiveUtilitySlot(activeSlot);
		InventoryPage.CharacterPanel.OnSetActiveUtilitySlot(activeSlot);
	}

	private void OnSetActiveConsumableSlot(int activeSlot)
	{
		ConsumableSlotSelector.SelectedSlot = activeSlot + 1;
		HotbarComponent.OnSetActiveConsumableSlot(activeSlot);
		InventoryPage.CharacterPanel.OnSetActiveConsumableSlot(activeSlot);
	}

	private void OnSetActiveToolsSlot(int activeSlot)
	{
	}

	public void OnPlayerCharacterItemChanged(ItemChangeType changeType)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Invalid comparison between Unknown and I4
		switch (changeType)
		{
		case ItemChangeType.Dropped:
			AbilitiesHudComponent?.OnSignatureEnergyStatChanged(null);
			break;
		case ItemChangeType.SlotChanged:
			AbilitiesHudComponent?.OnSignatureEnergyStatChanged(InGame.Instance.LocalPlayer?.GetEntityStat(DefaultEntityStats.SignatureEnergy));
			break;
		}
		if ((int)InGame.Instance.GameMode == 1)
		{
			UpdateBuilderToolsLegendVisibility(doLayout: true);
		}
		UpdateStaminaPanelVisibility(doLayout: true);
		UpdateAmmoIndicatorVisibility(doLayout: true);
		ReticleComponent.UpdateReticleImage();
	}

	private void OnSetAutosortType(SortType sortType)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		InventoryPage.ContainerPanel.SetSortType(sortType);
		InventoryPage.StoragePanel.SetSortType(sortType);
	}

	private void OnInventorySetAll(ClientItemStack[] storageStacks, ClientItemStack[] armorStacks, ClientItemStack[] hotbarStacks, ClientItemStack[] utilityStacks, ClientItemStack[] consumableStacks)
	{
		StorageStacks = storageStacks;
		HotbarStacks = hotbarStacks;
		ArmorStacks = armorStacks;
		UtilityStacks = utilityStacks;
		ConsumableStacks = consumableStacks;
		SetupCursorFloatingItem();
		InventoryPage.OnSetStacks();
		HotbarComponent.OnSetStacks();
		ReticleComponent.OnSetStacks();
		UtilitySlotSelector.SetItemStacks(UtilityStacks);
		ConsumableSlotSelector.SetItemStacks(ConsumableStacks);
		UpdateAmmoIndicatorVisibility(doLayout: true);
	}

	public void SetupDragAndDropItem(ItemGrid.ItemDragData itemDragData)
	{
		ItemDragData = itemDragData;
		_visibleDragStack = ((itemDragData == null) ? null : new ClientItemStack(itemDragData.ItemStack.Id, itemDragData.ItemStack.Quantity));
		SetupCursorFloatingItem();
	}

	public void SetupCursorFloatingItem()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		bool flag = (int)InGame.CurrentPage != 0 || InGame.IsToolsSettingsModalOpened;
		if (ItemDragData != null && flag && !ItemQuantityPopup.IsMounted)
		{
			if (_cursorFloatingItem.Parent == null)
			{
				Add(_cursorFloatingItem);
				Desktop.RegisterAnimationCallback(LayoutCursorFloatingItem);
			}
			_cursorFloatingItem.Slot = new ItemGridSlot
			{
				ItemStack = _visibleDragStack
			};
			LayoutCursorFloatingItem(0f);
		}
		else if (_cursorFloatingItem.Parent != null)
		{
			Remove(_cursorFloatingItem);
			Desktop.UnregisterAnimationCallback(LayoutCursorFloatingItem);
		}
		if (InventoryPage.CharacterPanel.IsMounted)
		{
			InventoryPage.CharacterPanel.UpdateCompatibleSlotHighlight();
		}
	}

	public TextureArea GetTextureAreaForItemIcon(string icon)
	{
		Dictionary<string, ClientIcon> itemIcons = InGame.Instance.ItemLibraryModule.ItemIcons;
		if (icon == null || !itemIcons.TryGetValue(icon, out var value))
		{
			return null;
		}
		return new TextureArea(ItemIconAtlasTexture, value.X, value.Y, value.Size, value.Size, 1);
	}

	public ClientItemStack[] GetItemStacks(int sectionId)
	{
		switch (sectionId)
		{
		case -2:
			return StorageStacks;
		case -1:
			return HotbarStacks;
		case -3:
			return ArmorStacks;
		case -5:
			return UtilityStacks;
		case -6:
			return ConsumableStacks;
		default:
		{
			PacketHandler.InventoryWindow inventoryWindow = InventoryWindow;
			if (inventoryWindow != null && inventoryWindow.Id == sectionId)
			{
				return InventoryWindow.Inventory;
			}
			throw new Exception("Invalid inventory section id: " + sectionId);
		}
		}
	}

	public ClientItemStack GetHotbarItem(int slot)
	{
		if (HotbarStacks == null || slot == -1)
		{
			return null;
		}
		return HotbarStacks[slot];
	}

	public ClientItemStack GetUtilityItem(int slot)
	{
		if (UtilityStacks == null || slot == -1)
		{
			return null;
		}
		return UtilityStacks[slot];
	}

	public ClientItemStack GetConsumableItem(int slot)
	{
		if (ConsumableStacks == null || slot == -1)
		{
			return null;
		}
		return ConsumableStacks[slot];
	}

	public bool checkForSettingBrush(string itemStackName)
	{
		if (canSetActiveBrushMaterial())
		{
			InGame.Instance.BuilderToolsModule.setActiveBrushMaterial(itemStackName, Desktop.IsShiftKeyDown, Desktop.IsAltKeyDown);
			return true;
		}
		return false;
	}

	public bool canSetActiveBrushMaterial()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		return (int)InGame.Instance.GameMode == 1 && InGame.Instance.BuilderToolsModule.HasActiveBrush;
	}

	public void HandleInventoryClick(int sectionId, int slotIndex, int button)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Invalid comparison between Unknown and I4
		if ((int)Interface.App.InGame.CurrentPage != 2 && (int)Interface.App.InGame.CurrentPage != 1 && (long)button != 1)
		{
			return;
		}
		ClientItemStack clientItemStack = GetItemStacks(sectionId)[slotIndex];
		if (clientItemStack == null)
		{
			ClearSlotHighlight();
			Interface.App.InGame.Instance.BuilderToolsModule.ClearConfiguringTool();
		}
		else
		{
			if (canSetActiveBrushMaterial() && (long)button == 2)
			{
				return;
			}
			if (Desktop.IsShiftKeyDown)
			{
				if (Interface.App.InGame.Instance.Chat.IsOpen)
				{
					ChatComponent.InsertItemTag(clientItemStack.Id);
				}
				else
				{
					ClientItemStack itemStack = clientItemStack;
					if ((long)button != 1)
					{
						int quantity = 1;
						if (clientItemStack.Quantity > 1 && (long)button == 2)
						{
							quantity = (int)System.Math.Floor((float)clientItemStack.Quantity / 2f);
						}
						itemStack = new ClientItemStack(clientItemStack.Id, quantity)
						{
							Metadata = clientItemStack.Metadata
						};
					}
					Interface.App.InGame.SendSmartMoveItemStackPacket(itemStack, sectionId, slotIndex, (SmartMoveType)1);
				}
			}
			if ((long)button == 1)
			{
				if ((int)InGame.Instance.GameMode == 1 && (sectionId == -2 || sectionId == -1) && Interface.App.InGame.Instance.BuilderToolsModule.TryConfigureTool(sectionId, slotIndex, clientItemStack))
				{
					HighlightSlot(sectionId, slotIndex);
				}
				if (sectionId == -1)
				{
					Interface.App.InGame.Instance.InventoryModule.SetActiveHotbarSlot(slotIndex);
				}
			}
		}
	}

	public void HandleInventoryDoubleClick(int sectionId, int slotIndex)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		if ((int)Interface.App.InGame.CurrentPage == 2 || (int)Interface.App.InGame.CurrentPage == 1)
		{
			ClientItemStack clientItemStack = GetItemStacks(sectionId)[slotIndex];
			if (clientItemStack != null)
			{
				Interface.App.InGame.SendSmartMoveItemStackPacket(clientItemStack, sectionId, slotIndex, (SmartMoveType)0);
			}
		}
	}

	public bool HandleHotbarLoad(sbyte hotbarIndex)
	{
		return Interface.App.InGame.HandleHotbarLoad(hotbarIndex);
	}

	public void HandleInventoryDragEnd(Element targetElement, int targetSectionId, int targetSlotIndex, Element sourceElement, ItemGrid.ItemDragData dragData)
	{
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Invalid comparison between Unknown and I4
		if (sourceElement == InventoryPage.ItemLibraryPanel.ItemGrid)
		{
			bool flag = GetItemStacks(targetSectionId)[targetSlotIndex] != null;
			PlayItemMovedSound(targetElement);
			InGame.SendSetCreativeItemPacket(targetSectionId, targetSlotIndex, dragData.ItemStack);
			if (flag)
			{
				ClearSlotHighlight();
				if (Interface.App.InGame.Instance.BuilderToolsModule.TryConfigureTool(targetSectionId, targetSlotIndex, dragData.ItemStack))
				{
					HighlightSlot(targetSectionId, targetSlotIndex);
				}
			}
		}
		else if (dragData.InventorySectionId.HasValue)
		{
			PlayItemMovedSound(targetElement);
			if ((int)InGame.Instance.GameMode == 1 && (long)dragData.PressedMouseButton == 2)
			{
				InGame.SendSetCreativeItemPacket(targetSectionId, targetSlotIndex, dragData.ItemStack);
			}
			else
			{
				MoveItemStack(dragData.InventorySectionId.Value, dragData.SlotId, targetSectionId, targetSlotIndex, dragData.ItemStack);
			}
			ClearSlotHighlight();
			Interface.App.InGame.Instance.BuilderToolsModule.ClearConfiguringTool();
		}
	}

	private void PlayItemMovedSound(Element targetElement)
	{
		if (!(targetElement is ItemGrid itemGrid))
		{
			if (targetElement is ItemSlotSelectorPopover { ItemMovedSound: not null } itemSlotSelectorPopover)
			{
				Desktop.Provider.PlaySound(itemSlotSelectorPopover.ItemMovedSound);
			}
		}
		else if (itemGrid.Style.ItemStackMovedSound != null)
		{
			Desktop.Provider.PlaySound(itemGrid.Style.ItemStackMovedSound);
		}
	}

	public void MoveItemStack(int sourceSectionId, int sourceSlotIndex, int targetSectionId, int targetSlotIndex, ClientItemStack itemStack = null)
	{
		if (itemStack == null)
		{
			itemStack = GetItemStacks(sourceSectionId)[sourceSlotIndex];
		}
		InGame.SendMoveItemStackPacket(itemStack, sourceSectionId, sourceSlotIndex, targetSectionId, targetSlotIndex);
	}

	public void HandleInventoryDropItem(int sectionId, int slotIndex, int button)
	{
		if ((InventoryPage.IsMounted || Interface.App.InGame.IsToolsSettingsModalOpened) && InventoryPage.IsItemAtPositionDroppable(Desktop.MousePosition))
		{
			DropItemStack(sectionId, slotIndex, ((long)button == 3) ? 1 : (-1));
			ClearSlotHighlight();
			Interface.App.InGame.Instance.BuilderToolsModule.ClearConfiguringTool();
		}
	}

	public void HandleItemSlotMouseEntered(int sectionId, int slotIndex)
	{
		_dropBindingHeldTick = 0f;
		_hasDroppedStack = false;
		HoveredItemSlot = new ClientInventoryPosition
		{
			InventorySectionId = sectionId,
			SlotId = slotIndex
		};
		if (InventoryPage.CharacterPanel.IsMounted)
		{
			InventoryPage.CharacterPanel.UpdateCompatibleSlotHighlight();
		}
	}

	public void HandleItemSlotMouseExited(int sectionId, int slotIndex)
	{
		ClientInventoryPosition hoveredItemSlot = HoveredItemSlot;
		if (hoveredItemSlot == null || hoveredItemSlot.SlotId != slotIndex)
		{
			return;
		}
		ClientInventoryPosition hoveredItemSlot2 = HoveredItemSlot;
		if (hoveredItemSlot2 != null && hoveredItemSlot2.InventorySectionId == sectionId)
		{
			HoveredItemSlot = null;
			if (InventoryPage.CharacterPanel.IsMounted)
			{
				InventoryPage.CharacterPanel.UpdateCompatibleSlotHighlight();
			}
		}
	}

	private void DropItemStack(int sectionId, int slotIndex, int quantity = -1)
	{
		ClientItemStack clientItemStack = GetItemStacks(sectionId)[slotIndex];
		ClientItemStack itemStack = new ClientItemStack(clientItemStack.Id, (quantity > -1) ? quantity : clientItemStack.Quantity)
		{
			Metadata = clientItemStack.Metadata
		};
		InGame.SendDropItemStackPacket(itemStack, sectionId, slotIndex);
	}

	public void HighlightSlot(int sectionId, int slotIndex)
	{
		InventoryPage.StoragePanel.ClearSlotHighlight();
		HotbarComponent.ClearSlotHighlight();
		if (HighlightedItemSlot != null && HighlightedItemSlot.InventorySectionId == sectionId && HighlightedItemSlot.SlotId == slotIndex)
		{
			HighlightedItemSlot = null;
		}
		switch ((InventorySectionType)sectionId)
		{
		case InventorySectionType.Storage:
			InventoryPage.StoragePanel.HighlightSlot(slotIndex);
			return;
		case InventorySectionType.Hotbar:
			HotbarComponent.HighlightSlot(slotIndex);
			return;
		}
		HighlightedItemSlot = new ClientInventoryPosition
		{
			InventorySectionId = sectionId,
			SlotId = slotIndex
		};
	}

	public void ClearSlotHighlight()
	{
		InventoryPage.StoragePanel.ClearSlotHighlight();
		HotbarComponent.ClearSlotHighlight();
		HighlightedItemSlot = null;
	}

	internal bool IsItemValid(int index, ClientItemBase item, InventorySectionType inventorySectionType)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected I4, but got Unknown
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Invalid comparison between Unknown and I4
		Debug.Assert(inventorySectionType == InventorySectionType.Storage || inventorySectionType == InventorySectionType.Hotbar);
		ClientInventoryPosition hoveredItemSlot = HoveredItemSlot;
		if (hoveredItemSlot == null)
		{
			return true;
		}
		if (inventorySectionType == InventorySectionType.Storage)
		{
			index += HotbarStacks.Length;
		}
		if (hoveredItemSlot.InventorySectionId >= 0 && InventoryWindow != null)
		{
			WindowType windowType = InventoryWindow.WindowType;
			WindowType val = windowType;
			switch (val - 2)
			{
			case 2:
			{
				if (item == null)
				{
					return false;
				}
				if (hoveredItemSlot.SlotId >= InventoryPage.ProcessingPanel.FuelSlotCount)
				{
					return InventoryPage.ProcessingPanel.CompatibleInputSlots[index];
				}
				string text = (string)InventoryWindow.WindowData["fuel"][(object)hoveredItemSlot.SlotId][(object)"resourceTypeId"];
				if (item.ResourceTypes == null)
				{
					return false;
				}
				ClientItemResourceType[] resourceTypes = item.ResourceTypes;
				foreach (ClientItemResourceType clientItemResourceType in resourceTypes)
				{
					if (clientItemResourceType.Id == text)
					{
						return true;
					}
				}
				return false;
			}
			case 1:
				if (item == null)
				{
					return false;
				}
				return InventoryPage.StructuralCraftingPanel.CompatibleSlots[index];
			case 0:
			{
				if (item == null)
				{
					return false;
				}
				HashSet<int>[] inventoryHints = InventoryPage.DiagramCraftingPanel.InventoryHints;
				if (inventoryHints[hoveredItemSlot.SlotId] == null || InventoryWindow.Inventory[hoveredItemSlot.SlotId] != null)
				{
					return true;
				}
				return inventoryHints[hoveredItemSlot.SlotId].Contains(index);
			}
			}
		}
		else
		{
			if (hoveredItemSlot.InventorySectionId == -3)
			{
				if (ArmorStacks[hoveredItemSlot.SlotId] != null)
				{
					return true;
				}
				if (item == null)
				{
					return false;
				}
				return item.Armor != null && (int)item.Armor.ArmorSlot == hoveredItemSlot.SlotId;
			}
			if (hoveredItemSlot.InventorySectionId == -5)
			{
				if (item == null)
				{
					return false;
				}
				return item.Utility != null && item.Utility.Usable;
			}
			if (hoveredItemSlot.InventorySectionId == -6)
			{
				return item?.Consumable ?? false;
			}
		}
		return true;
	}
}
