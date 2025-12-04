#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Hypixel.ProtoPlus;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.InGame;
using HytaleClient.Interface.InGame.Pages.InventoryPanels;
using HytaleClient.Math;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Application;

internal class AppInGame
{
	public enum InGameOverlay
	{
		None,
		InGameMenu,
		MachinimaEditor,
		ConfirmQuit
	}

	public enum ItemSelector
	{
		None,
		Utility,
		Consumable,
		BuilderToolsMaterial
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly App _app;

	private CustomPageLifetime _customPageLifetime;

	private bool _isCapturingWorldPreviewBeforeExit;

	private bool _hasPreparedToExit;

	private bool _isExitingApplication;

	private GLBuffer _worldPreviewScreenshotPixelBuffer;

	private IntPtr _worldPreviewScreenshotFenceSync;

	private GLTexture _worldPreviewTexture;

	private GLFramebuffer _worldPreviewFramebuffer;

	private const int WorldPreviewColorChannels = 4;

	private const int WorldPreviewOutputHeight = 360;

	private int _worldPreviewOutputWidth;

	public GameInstance Instance { get; private set; }

	public InGameOverlay CurrentOverlay { get; private set; }

	public Page CurrentPage { get; private set; }

	public bool IsToolsSettingsModalOpened { get; private set; } = false;


	public bool WasCurrentPageOpenedViaInteractionBinding { get; private set; }

	public bool IsHudVisible { get; private set; } = true;


	public bool IsFirstPersonViewVisible { get; private set; } = true;


	public bool IsPlayerListVisible { get; private set; }

	public string ServerName { get; set; }

	public ItemSelector ActiveItemSelector { get; private set; } = ItemSelector.None;


	public bool HasUnclosablePage => (int)CurrentPage == 7 && (int)_customPageLifetime == 0;

	public ClientItemCategory[] ItemCategories { get; private set; }

	public AppInGame(App app)
	{
		_app = app;
	}

	internal void CreateInstance(ConnectionToServer connection)
	{
		Instance = new GameInstance(_app, connection);
	}

	internal void DisposeAndClearInstance()
	{
		Instance?.Dispose();
		Instance = null;
	}

	public void Reset(bool isStayingConnected)
	{
		CurrentOverlay = InGameOverlay.None;
		CurrentPage = (Page)0;
		IsHudVisible = true;
		IsFirstPersonViewVisible = true;
		IsPlayerListVisible = false;
		ActiveItemSelector = ItemSelector.None;
		ResetInventoryState();
		_app.Interface.InGameView.OnReset(isStayingConnected);
	}

	public void Open()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Invalid comparison between Unknown and I4
		Debug.Assert(_app.Stage == App.AppStage.MainMenu || _app.Stage == App.AppStage.GameLoading);
		Instance.Connection.OnDisconnected = OnDisconnectedWithError;
		_app.SetStage(App.AppStage.InGame);
		if ((int)CurrentPage > 0)
		{
			_app.Interface.InGameView.OnPageChanged();
		}
		UpdateInputStates();
	}

	internal void CleanUp()
	{
		PrepareToExit();
		_app.CoUIManager.SetFocusedWebView(null);
		_app.Interface.Desktop.IsFocused = true;
		Reset(isStayingConnected: false);
		DisposeAndClearInstance();
		_isCapturingWorldPreviewBeforeExit = false;
		_hasPreparedToExit = false;
	}

	internal void OnNewFrame(float deltaTime)
	{
		Engine engine = _app.Engine;
		GLFunctions gL = engine.Graphics.GL;
		if (Instance.Disposed)
		{
			_app.Interface.PrepareForDraw();
			gL.Disable(GL.DEPTH_TEST);
			gL.BlendFunc(GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
			gL.Viewport(engine.Window.Viewport);
			_app.Interface.Draw();
			if (_worldPreviewScreenshotFenceSync != IntPtr.Zero)
			{
				FetchWorldPreviewScreenshot(_isExitingApplication);
			}
			if (_isExitingApplication)
			{
				_app.Exit();
			}
			else
			{
				_app.MainMenu.Open(AppMainMenu.MainMenuPage.Home);
			}
			return;
		}
		Instance.Input.UpdateBindings();
		deltaTime *= Instance.TimeDilationModifier;
		Instance.ProfilingModule.SetDrawCallStats(gL.DrawCallsCount, gL.DrawnVertices / 3);
		gL.ResetDrawCallStats();
		engine.Profiling.SwapMeasureBuffers();
		engine.Profiling.StartMeasure(0);
		engine.Profiling.StartMeasure(2);
		Instance.OnNewFrame(deltaTime, engine.Window.GetState() != Window.WindowState.Minimized);
		engine.Profiling.StopMeasure(2);
		engine.Profiling.StartMeasure(42);
		gL.ClearColor(0f, 0f, 0f, 1f);
		gL.Clear((GL)16640u);
		if (Instance.IsReadyToDraw)
		{
			gL.Enable(GL.DEPTH_TEST);
			gL.DepthMask(write: true);
			Instance.DrawScene();
			gL.Disable(GL.BLEND);
			gL.Viewport(engine.Window.Viewport);
			Instance.DrawPostEffect();
			if (_isCapturingWorldPreviewBeforeExit)
			{
				CaptureWorldPreviewScreenshot();
			}
		}
		gL.Disable(GL.DEPTH_TEST);
		gL.Enable(GL.BLEND);
		Instance.DrawAfterPostEffect();
		engine.Profiling.StopMeasure(42);
		engine.Profiling.StopMeasure(0);
		gL.UseProgram(engine.Graphics.GPUProgramStore.BasicProgram);
		gL.BlendFunc(GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
		_app.Interface.Draw();
		gL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
		gL.Enable(GL.DEPTH_TEST);
		gL.Clear(GL.DEPTH_BUFFER_BIT);
		Instance.DrawAfterInterface();
		Instance.Input.EndUserInput();
		if (_isCapturingWorldPreviewBeforeExit)
		{
			PrepareToExit();
		}
	}

	private void PrepareToExit()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		if (_hasPreparedToExit)
		{
			return;
		}
		_hasPreparedToExit = true;
		Instance.Connection.SendPacketImmediate((ProtoPacket)new Disconnect("Player leave", (DisconnectType)0));
		Instance.Connection.Close();
		if (_app.SingleplayerServer != null)
		{
			if (!_app.SingleplayerServer.Process.HasExited)
			{
				_app.SingleplayerServer.Close();
			}
			_app.OnSinglePlayerServerShuttingDown();
		}
		Instance.Dispose();
		_app.Engine.Window.SetMouseLock(enabled: false);
	}

	public void UpdateInputStates(bool skipResetKeys = false)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Invalid comparison between Unknown and I4
		if (IsToolsSettingsModalOpened || (int)CurrentPage != 0 || CurrentOverlay != 0 || Instance.Chat.IsOpen || _app.DevTools.IsOpen || _app.Interface.InGameView.UtilitySlotSelector.IsMounted || _app.Interface.InGameView.ConsumableSlotSelector.IsMounted || _app.Interface.InGameView.BuilderToolsMaterialSlotSelector.IsMounted)
		{
			bool flag = (int)CurrentPage == 0 && (_app.Interface.InGameView.UtilitySlotSelector.IsMounted || _app.Interface.InGameView.ConsumableSlotSelector.IsMounted || _app.Interface.InGameView.BuilderToolsMaterialSlotSelector.IsMounted);
			_app.Engine.Window.SetMouseLock(flag);
			Instance.Input.MouseInputDisabled = flag;
			if (((int)CurrentPage == 6 || flag) && CurrentOverlay == InGameOverlay.None && !Instance.Chat.IsOpen)
			{
				Instance.Input.KeyInputDisabled = false;
			}
			else
			{
				Instance.Input.KeyInputDisabled = true;
				if (!skipResetKeys)
				{
					Instance.Input.ResetKeys();
				}
			}
			Instance.Input.ResetMouseButtons();
			bool flag2 = CurrentOverlay == InGameOverlay.MachinimaEditor;
			_app.CoUIManager.SetFocusedWebView(flag2 ? Instance.EditorWebViewModule.WebView : null);
			_app.Interface.Desktop.IsFocused = !flag2;
		}
		else
		{
			Instance.Input.MouseInputDisabled = false;
			_app.Engine.Window.SetMouseLock(enabled: true);
			Instance.Input.KeyInputDisabled = false;
			Instance.Input.ResetMouseButtons();
			if (!skipResetKeys)
			{
				Instance.Input.ResetKeys();
			}
			_app.CoUIManager.SetFocusedWebView(null);
			_app.Interface.Desktop.IsFocused = false;
		}
	}

	public void RequestExit(bool exitApplication = false)
	{
		_isExitingApplication = exitApplication;
		if (_app.SingleplayerServer != null && Instance.IsPlaying)
		{
			_isCapturingWorldPreviewBeforeExit = true;
		}
		else if (exitApplication)
		{
			_app.Exit();
		}
		else
		{
			_app.MainMenu.Open(AppMainMenu.MainMenuPage.Home);
		}
	}

	private void OnDisconnectedWithError(Exception exception)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (!_hasPreparedToExit && _app.Stage == App.AppStage.InGame)
		{
			Logger.Info("Disconnected with error:");
			Logger.Error<Exception>(exception);
			_app.MainMenu.SetPageToReturnTo(AppMainMenu.MainMenuPage.Home);
			_app.Disconnection.Open(exception.Message, Instance.Connection.Hostname, Instance.Connection.Port);
		}
	}

	public byte[] GetAsset(string name)
	{
		if (!Instance.HashesByServerAssetPath.TryGetValue(name, out var value))
		{
			return null;
		}
		return AssetManager.GetAssetUsingHash(value);
	}

	public void OnChatOpenChanged()
	{
		UpdateInputStates();
	}

	public void TryClosePageOrOverlay()
	{
		if (CurrentOverlay != 0)
		{
			SetCurrentOverlay(InGameOverlay.None);
		}
		else
		{
			TryClosePage();
		}
	}

	public void TryClosePage()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		Page currentPage = CurrentPage;
		Page val = currentPage;
		if ((int)val == 0)
		{
			return;
		}
		if ((int)val != 2)
		{
			if ((int)val == 7)
			{
				if ((int)_customPageLifetime == 0)
				{
					SetCurrentOverlay(InGameOverlay.InGameMenu);
					return;
				}
				_app.Interface.InGameView.CustomPage.StartLoading();
				Instance.Connection.SendPacket((ProtoPacket)new CustomPageEvent((CustomPageEventType)2, (sbyte[])null));
			}
			else
			{
				SetCurrentPage((Page)0, wasOpenedWithInteractionBinding: false, playSound: true);
			}
		}
		else
		{
			SetCurrentPage((Page)0, wasOpenedWithInteractionBinding: false, playSound: true);
		}
	}

	public void SetCurrentPage(Page page, bool wasOpenedWithInteractionBinding = false, bool playSound = false)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Invalid comparison between Unknown and I4
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Invalid comparison between Unknown and I4
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Invalid comparison between Unknown and I4
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Invalid comparison between Unknown and I4
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Invalid comparison between Unknown and I4
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		if ((int)page == 7)
		{
			throw new Exception("OpenCustomPage must be used for Page of type Custom");
		}
		if ((int)page == 3 && Instance.ImmersiveScreenModule.ActiveWebScreen == null)
		{
			return;
		}
		if ((int)page == 6 || (int)page == 7)
		{
			CloseToolsSettingsModal();
		}
		if ((int)CurrentPage == 7)
		{
			Instance.Connection.SendPacket((ProtoPacket)new CustomPageEvent((CustomPageEventType)0, (sbyte[])null));
		}
		Page currentPage = CurrentPage;
		CurrentPage = page;
		WasCurrentPageOpenedViaInteractionBinding = wasOpenedWithInteractionBinding;
		if (_app.Stage == App.AppStage.InGame)
		{
			if (playSound)
			{
				if ((int)CurrentPage == 0)
				{
					_app.Interface.InGameView.PlayPageCloseSound(currentPage);
				}
				else
				{
					_app.Interface.InGameView.PlayPageOpenSound(CurrentPage);
				}
			}
			_app.Interface.InGameView.OnPageChanged();
			UpdateInputStates((int)CurrentPage == 0 && (int)currentPage == 6);
		}
		if (ActiveItemSelector != 0)
		{
			SetActiveItemSelector(ItemSelector.None);
		}
	}

	public void CloseToolsSettingsModal()
	{
		IsToolsSettingsModalOpened = false;
		if (_app.Stage == App.AppStage.InGame)
		{
			_app.Interface.InGameView.CloseToolsSettingsModal();
			UpdateInputStates();
		}
	}

	public void OpenToolsSettingsPage()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		IsToolsSettingsModalOpened = true;
		if (_app.Stage == App.AppStage.InGame)
		{
			_app.Interface.InGameView.OpenToolsSettingsPage();
			UpdateInputStates();
			if ((int)CurrentPage == 6)
			{
				SetCurrentPage((Page)0);
			}
		}
	}

	public void ToogleToolById(string toolId)
	{
		if (_app.InGame.Instance.InventoryModule.GetActiveToolsItem()?.Id == toolId && IsToolsSettingsModalOpened)
		{
			CloseToolsSettingsModal();
			return;
		}
		_app.Interface.InGameView.ToolsSettingsPage.SelectToolById(toolId);
		OpenToolsSettingsPage();
	}

	public void OpenOrUpdateCustomPage(CustomPage packet)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Invalid comparison between Unknown and I4
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Invalid comparison between Unknown and I4
		Instance.Connection.SendPacket((ProtoPacket)new CustomPageEvent((CustomPageEventType)0, (sbyte[])null));
		_customPageLifetime = packet.Lifetime;
		WasCurrentPageOpenedViaInteractionBinding = (int)packet.Lifetime == 2;
		_app.Interface.InGameView.CustomPage.Apply(packet);
		if ((int)CurrentPage != 7)
		{
			CurrentPage = (Page)7;
			if (_app.Stage == App.AppStage.InGame)
			{
				_app.Interface.InGameView.OnPageChanged();
				UpdateInputStates();
			}
		}
	}

	public void UpdateCustomHud(CustomHud packet)
	{
		_app.Interface.InGameView.CustomHud.Apply(packet);
	}

	public void SetActiveItemSelector(ItemSelector itemSelector)
	{
		ActiveItemSelector = itemSelector;
		_app.Interface.InGameView.OnActiveItemSelectorChanged();
		UpdateInputStates(skipResetKeys: true);
	}

	public void SetCurrentOverlay(InGameOverlay overlay)
	{
		CurrentOverlay = overlay;
		_app.Interface.InGameView.OnOverlayChanged();
		UpdateInputStates();
		if (ActiveItemSelector != 0)
		{
			SetActiveItemSelector(ItemSelector.None);
		}
	}

	public void SwitchHudVisibility()
	{
		if (IsHudVisible)
		{
			IsHudVisible = false;
			_app.DevTools.ClearNotifications();
		}
		else if (IsFirstPersonViewVisible)
		{
			IsFirstPersonViewVisible = false;
		}
		else
		{
			bool isHudVisible = (IsFirstPersonViewVisible = true);
			IsHudVisible = isHudVisible;
		}
		_app.Interface.InGameView.OnHudVisibilityChanged();
	}

	public void SetPlayerListVisible(bool visible)
	{
		IsPlayerListVisible = visible;
		_app.Interface.InGameView.OnPlayerListVisibilityChanged();
	}

	public void SetSceneBlurEnabled(bool enabled)
	{
		Instance.PostEffectRenderer.UseBlur(enabled);
	}

	public void SendCustomPageData(JObject data)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		Instance.Connection.SendPacket((ProtoPacket)new CustomPageEvent((CustomPageEventType)1, BsonHelper.ToBson((JToken)(object)data)));
	}

	public void SendChatMessageOrExecuteCommand(string message)
	{
		if (message.StartsWith("."))
		{
			Instance.ExecuteCommand(message);
		}
		else
		{
			Instance.Chat.SendMessage(message);
		}
	}

	public void OpenAssetEditor()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		OpenEditor(new JObject());
	}

	public void OpenAssetPathInAssetEditor(string assetPath)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		JObject val = new JObject();
		val.Add("AssetPath", JToken.op_Implicit(assetPath));
		OpenEditor(val);
	}

	public void OpenAssetIdInAssetEditor(string assetType, string assetId)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		JObject val = new JObject();
		val.Add("AssetType", JToken.op_Implicit(assetType));
		val.Add("AssetId", JToken.op_Implicit(assetId));
		OpenEditor(val);
	}

	private void OpenEditor(JObject data)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		data["Name"] = JToken.op_Implicit(ServerName ?? $"{Instance.Connection.Hostname}:{Instance.Connection.Port}");
		data["Hostname"] = JToken.op_Implicit(Instance.Connection.Hostname);
		data["Port"] = JToken.op_Implicit(Instance.Connection.Port);
		Instance.Connection.SendPacket((ProtoPacket)new AssetEditorInitialize());
		_app.Ipc.SendCommand("OpenEditor", data);
	}

	private void ResetInventoryState()
	{
		ItemCategories = null;
	}

	public void SendSetCreativeItemPacket(int section, int slot, ClientItemStack itemStack, bool overwrite = false)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		Item val = itemStack.ToItemPacket(includeMetadata: false);
		Instance.Connection.SendPacket((ProtoPacket)new SetCreativeItem(new InventoryPosition(section, slot, val), overwrite));
	}

	public void SendSmartGiveCreativeItemPacket(ClientItemStack itemStack, SmartMoveType moveType)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		Item val = itemStack.ToItemPacket(includeMetadata: false);
		Instance.Connection.SendPacket((ProtoPacket)new SmartGiveCreativeItem(val, moveType));
	}

	public void SendMoveItemStackPacket(ClientItemStack itemStack, int sourceSectionId, int sourceSlotId, int targetSectionId, int targetSlotId)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_002b: Expected O, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		Item val = itemStack.ToItemPacket(includeMetadata: false);
		Instance.Connection.SendPacket((ProtoPacket)new MoveItemStack(new InventoryPosition(sourceSectionId, sourceSlotId, val), new InventoryPosition(targetSectionId, targetSlotId, (Item)null)));
	}

	public void SendSmartMoveItemStackPacket(ClientItemStack itemStack, int sourceSectionId, int sourceSlotId, SmartMoveType moveType)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		Item val = itemStack.ToItemPacket(includeMetadata: false);
		Instance.Connection.SendPacket((ProtoPacket)new SmartMoveItemStack(new InventoryPosition(sourceSectionId, sourceSlotId, val), moveType));
	}

	public void SendTakeAllItemStacksPacket(int inventorySectionId)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		Instance.Connection.SendPacket((ProtoPacket)new TakeAllItemStacks(inventorySectionId));
	}

	public void SendDropItemStackPacket(ClientItemStack itemStack, int sectionId, int slotId)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		Item val = itemStack.ToItemPacket(includeMetadata: true);
		Instance.Connection.SendPacket((ProtoPacket)new DropItemStack(new InventoryPosition(sectionId, slotId, val)));
	}

	public void SendOpenInventoryPacket()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		Instance.Connection.SendPacket((ProtoPacket)new OpenInventory());
	}

	public void SendCloseWindowPacket(int id)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		Instance.Connection.SendPacket((ProtoPacket)new CloseWindow(id));
	}

	public void SendSendWindowActionPacket(int windowId, string key, string data)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		Instance.Connection.SendPacket((ProtoPacket)new SendWindowAction(windowId, key, data));
	}

	public void SendSortInventoryPacket(SortType sortType)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		Instance.Connection.SendPacket((ProtoPacket)new SortInventory(sortType));
	}

	public void OnItemCategoriesInitialized(ItemCategory[] categories)
	{
		ItemCategories = new ClientItemCategory[categories.Length];
		for (int i = 0; i < categories.Length; i++)
		{
			ItemCategories[i] = new ClientItemCategory(categories[i]);
		}
		Array.Sort(ItemCategories, (ClientItemCategory a, ClientItemCategory b) => a.Order - b.Order);
		ItemLibraryPanel itemLibraryPanel = _app.Interface.InGameView.InventoryPage.ItemLibraryPanel;
		itemLibraryPanel.EnsureValidCategorySelected();
		if (_app.Stage == App.AppStage.InGame)
		{
			itemLibraryPanel.SetupCategories();
		}
	}

	public bool HandleHotbarLoad(sbyte hotbarIndex)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		GameInstance instance = _app.InGame.Instance;
		if ((int)instance.GameMode == 1)
		{
			instance.Connection.SendPacket((ProtoPacket)new LoadHotbar(hotbarIndex));
			return true;
		}
		return false;
	}

	public void OnItemCategoriesAdded(ItemCategory[] categories)
	{
		Dictionary<string, ClientItemCategory> dictionary = new Dictionary<string, ClientItemCategory>();
		ClientItemCategory[] itemCategories = ItemCategories;
		foreach (ClientItemCategory clientItemCategory in itemCategories)
		{
			dictionary[clientItemCategory.Id] = clientItemCategory;
		}
		foreach (ItemCategory val in categories)
		{
			dictionary[val.Id] = new ClientItemCategory(val);
		}
		ItemCategories = dictionary.Values.ToArray();
		Array.Sort(ItemCategories, (ClientItemCategory a, ClientItemCategory b) => a.Order - b.Order);
		_app.Interface.InGameView.InventoryPage.ItemLibraryPanel.SetupCategories();
	}

	public void OnItemCategoriesRemoved(ItemCategory[] categories)
	{
		string[] array = new string[categories.Length];
		for (int i = 0; i < categories.Length; i++)
		{
			array[i] = categories[i].Id;
		}
		List<ClientItemCategory> list = new List<ClientItemCategory>();
		ClientItemCategory[] itemCategories = ItemCategories;
		foreach (ClientItemCategory clientItemCategory in itemCategories)
		{
			if (!array.Contains(clientItemCategory.Id))
			{
				list.Add(clientItemCategory);
			}
		}
		ItemCategories = list.ToArray();
		_app.Interface.InGameView.InventoryPage.ItemLibraryPanel.SetupCategories();
	}

	private void FetchWorldPreviewScreenshot(bool saveFromMainThread)
	{
		Debug.Assert(_worldPreviewScreenshotFenceSync != IntPtr.Zero);
		Logger.Info("Capturing world preview screenshot...");
		GLFunctions gL = _app.Engine.Graphics.GL;
		gL.GetSynciv(_worldPreviewScreenshotFenceSync, GL.SYNC_STATUS, (IntPtr)8, IntPtr.Zero, out var values);
		if ((int)values != 37145)
		{
			return;
		}
		gL.BindVertexArray(GLVertexArray.None);
		gL.BindBuffer(GLVertexArray.None, GL.PIXEL_PACK_BUFFER, _worldPreviewScreenshotPixelBuffer);
		IntPtr intPtr = gL.MapBufferRange(GL.PIXEL_PACK_BUFFER, IntPtr.Zero, (IntPtr)(_worldPreviewOutputWidth * 360 * 4), GL.ONE);
		byte[] rawPixels = new byte[_worldPreviewOutputWidth * 360 * 4];
		for (int i = 0; i < 360; i++)
		{
			Marshal.Copy(intPtr + i * _worldPreviewOutputWidth * 4, rawPixels, (360 - i - 1) * _worldPreviewOutputWidth * 4, _worldPreviewOutputWidth * 4);
		}
		gL.UnmapBuffer(GL.PIXEL_PACK_BUFFER);
		gL.BindBuffer(GLVertexArray.None, GL.PIXEL_PACK_BUFFER, GLBuffer.None);
		gL.DeleteBuffer(_worldPreviewScreenshotPixelBuffer);
		gL.DeleteSync(_worldPreviewScreenshotFenceSync);
		gL.DeleteFramebuffer(_worldPreviewFramebuffer);
		gL.DeleteTexture(_worldPreviewTexture);
		_worldPreviewScreenshotPixelBuffer = GLBuffer.None;
		_worldPreviewScreenshotFenceSync = IntPtr.Zero;
		int width = _worldPreviewOutputWidth;
		string worldDirectoryName = _app.SingleplayerWorldName;
		string filePath = Path.Combine(Paths.Saves, worldDirectoryName, "preview.png");
		string tmpFilePath = Path.Combine(Paths.Saves, worldDirectoryName, "preview.tmp.png");
		if (saveFromMainThread)
		{
			SaveFile();
			return;
		}
		ThreadPool.QueueUserWorkItem(delegate
		{
			SaveFile();
		});
		void MoveFile()
		{
			File.Delete(filePath);
			File.Move(tmpFilePath, filePath);
			Logger.Info("World preview screenshot capture complete");
			if (_app.Stage == App.AppStage.MainMenu && _app.MainMenu.CurrentPage == AppMainMenu.MainMenuPage.Adventure)
			{
				_app.Interface.MainMenuView.AdventurePage.OnWorldPreviewUpdated(worldDirectoryName);
			}
		}
		void SaveFile()
		{
			try
			{
				new Image(width, 360, rawPixels).SavePNG(tmpFilePath, width, 360, 16711680u, 65280u, 255u, 0u);
				if (saveFromMainThread)
				{
					MoveFile();
				}
				else
				{
					_app.Engine.RunOnMainThread(_app, MoveFile);
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Exception saving world preview:");
			}
		}
	}

	private void CaptureWorldPreviewScreenshot()
	{
		Debug.Assert(_isCapturingWorldPreviewBeforeExit && _worldPreviewScreenshotFenceSync == IntPtr.Zero);
		Rectangle viewport = _app.Engine.Window.Viewport;
		GLFunctions gL = _app.Engine.Graphics.GL;
		_worldPreviewOutputWidth = (int)((float)viewport.Width / (float)viewport.Height * 360f);
		gL.AssertActiveTexture(GL.TEXTURE0);
		_worldPreviewTexture = gL.GenTexture();
		gL.BindTexture(GL.TEXTURE_2D, _worldPreviewTexture);
		gL.TexImage2D(GL.TEXTURE_2D, 0, 32856, _worldPreviewOutputWidth, 360, 0, GL.RGBA, GL.UNSIGNED_BYTE, IntPtr.Zero);
		_worldPreviewFramebuffer = gL.GenFramebuffer();
		gL.BindFramebuffer(GL.DRAW_FRAMEBUFFER, _worldPreviewFramebuffer);
		gL.FramebufferTexture2D(GL.FRAMEBUFFER, GL.COLOR_ATTACHMENT0, GL.TEXTURE_2D, _worldPreviewTexture, 0);
		gL.DrawBuffers(1, new GL[4]
		{
			GL.COLOR_ATTACHMENT0,
			GL.NO_ERROR,
			GL.NO_ERROR,
			GL.NO_ERROR
		});
		GL gL2 = gL.CheckFramebufferStatus(GL.FRAMEBUFFER);
		if (gL2 != GL.FRAMEBUFFER_COMPLETE)
		{
			throw new Exception("Incomplete Framebuffer object, status: " + gL2);
		}
		gL.BlitFramebuffer(0, 0, viewport.Width, viewport.Height, 0, 0, _worldPreviewOutputWidth, 360, GL.COLOR_BUFFER_BIT, GL.LINEAR);
		gL.BindFramebuffer(GL.READ_FRAMEBUFFER, _worldPreviewFramebuffer);
		gL.ReadBuffer(GL.COLOR_ATTACHMENT0);
		gL.BindVertexArray(GLVertexArray.None);
		_worldPreviewScreenshotPixelBuffer = gL.GenBuffer();
		gL.BindBuffer(GLVertexArray.None, GL.PIXEL_PACK_BUFFER, _worldPreviewScreenshotPixelBuffer);
		gL.BufferData(GL.PIXEL_PACK_BUFFER, (IntPtr)(_worldPreviewOutputWidth * 360 * 4), IntPtr.Zero, GL.DYNAMIC_READ);
		gL.ReadPixels(0, 0, _worldPreviewOutputWidth, 360, GL.BGRA, GL.UNSIGNED_BYTE, IntPtr.Zero);
		gL.BindBuffer(GLVertexArray.None, GL.PIXEL_PACK_BUFFER, GLBuffer.None);
		gL.BindFramebuffer(GL.FRAMEBUFFER, GLFramebuffer.None);
		_worldPreviewScreenshotFenceSync = gL.FenceSync(GL.SYNC_GPU_COMMANDS_COMPLETE, GL.NO_ERROR);
		if (_worldPreviewScreenshotFenceSync == IntPtr.Zero)
		{
			throw new Exception("Failed to get fence sync!");
		}
	}
}
