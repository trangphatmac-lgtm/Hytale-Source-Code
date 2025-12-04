#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Epic.OnlineServices;
using Hypixel.ProtoPlus;
using HytaleClient.Application.Auth;
using HytaleClient.Application.Services;
using HytaleClient.Application.Services.Api;
using HytaleClient.Common.Memory;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.Characters;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.Interface;
using HytaleClient.Interface.CoherentUI;
using HytaleClient.Interface.CoherentUI.Internals;
using HytaleClient.Interface.UI;
using HytaleClient.Math;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using SDL2;

namespace HytaleClient.Application;

internal class App : Disposable
{
	public enum AppStage
	{
		Initial,
		Startup,
		MainMenu,
		GameLoading,
		InGame,
		Disconnection,
		Exited
	}

	public enum SoundGroupType : byte
	{
		UI,
		MainMenu,
		InGameAssets,
		InGameCustomUI
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly Engine Engine;

	public readonly AuthManager AuthManager;

	public readonly HytaleServices HytaleServices;

	public readonly HytaleServicesApiClient HytaleServicesApi;

	public readonly FontManager Fonts;

	public readonly CoUIManager CoUIManager;

	public readonly DevTools DevTools;

	public readonly EOSPlatformManager EOSPlatform;

	public readonly AppStartup Startup;

	public readonly AppMainMenu MainMenu;

	public readonly AppGameLoading GameLoading;

	public readonly AppInGame InGame;

	public readonly AppDisconnection Disconnection;

	public readonly IpcClient Ipc;

	private bool _resetElapsedTime = false;

	private readonly string ScreenshotsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Hytale Screenshots");

	private bool _wasScreenshotRequested;

	private Point _upcomingScreenshotSize;

	private GLBuffer _screenshotPixelBuffer;

	private IntPtr _screenshotFenceSync;

	public CharacterPartStore CharacterPartStore { get; private set; }

	public HytaleClient.Interface.Interface Interface { get; private set; }

	public AppStage Stage { get; private set; }

	public string Username => AuthManager.Settings.Username;

	public ClientPlayerSkin PlayerSkin { get; private set; }

	public string SingleplayerWorldName { get; private set; }

	public SingleplayerServer SingleplayerServer { get; private set; }

	public SingleplayerServer ShuttingDownSingleplayerServer { get; private set; }

	public float CpuTime { get; private set; }

	public Settings Settings { get; private set; }

	public App()
	{
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		if (OptionsHelper.GenerateUIDocs)
		{
			string fullPath = Path.GetFullPath("../../UIDocs.MediaWiki.txt");
			File.WriteAllText(fullPath, DocGen.GenerateMediaWikiPage());
			Logger.Info("Successfully generated UI documentation into " + fullPath);
			Environment.Exit(0);
		}
		Settings = Settings.Load();
		EOSPlatform = new EOSPlatformManager();
		Result result = EOSPlatform.Initialize();
		if (result != 0)
		{
			Logger.Error($"Failed to initialize EOS Platform: {result}");
		}
		Window.WindowState initialState = (Settings.Fullscreen ? Window.WindowState.Fullscreen : (Settings.Maximized ? Window.WindowState.Maximized : Window.WindowState.Normal));
		string path = Path.Combine(Paths.GameData, (BuildInfo.Platform == Platform.MacOS) ? "Icon-256.png" : "Icon-64.png");
		Window.WindowSettings windowSettings = new Window.WindowSettings
		{
			Title = "Hytale",
			Icon = new Image(File.ReadAllBytes(path)),
			InitialSize = new Point(Settings.ScreenResolution.Width, Settings.ScreenResolution.Height),
			MinimumSize = new Point(Settings.ScreenResolution.Width, Settings.ScreenResolution.Height),
			Borderless = (Settings.UseBorderlessForFullscreen && Settings.Fullscreen),
			InitialState = initialState,
			MinAspectRatio = 1f,
			Resizable = true
		};
		Engine = new Engine(windowSettings);
		Engine.InitializeAudio(Settings.AudioSettings.OutputDeviceId, "MASTERVOLUME", Settings.AudioSettings.MasterVolume, Settings.AudioSettings.GetCategoryRTPCsArray(), Settings.AudioSettings.GetCategoryVolumesArray());
		Engine.SetMouseRelativeModeRaw(Settings.MouseSettings.MouseRawInputMode);
		SDL_Rect val = default(SDL_Rect);
		SDL.SDL_GetDisplayBounds(SDL.SDL_GetWindowDisplayIndex(Engine.Window.Handle), ref val);
		if (!Settings.ScreenResolution.FitsIn(val.w, val.h))
		{
			Settings.ScreenResolution = ScreenResolutions.DefaultScreenResolution;
			Engine.Window.UpdateSize(Settings.ScreenResolution);
			Engine.Window.SetState(Window.WindowState.Normal, borderless: false, recalculateZoom: false);
		}
		Engine.Window.Show();
		Fonts = new FontManager();
		CharacterPartStore = new CharacterPartStore(Engine.Graphics.GL);
		AuthManager = new AuthManager();
		HytaleServices = new HytaleServices(this);
		HytaleServicesApi = new HytaleServicesApiClient();
		CoUIManager = new CoUIManager(Engine, new CoUIGameFileHandler(this));
		Startup = new AppStartup(this);
		MainMenu = new AppMainMenu(this);
		GameLoading = new AppGameLoading(this);
		InGame = new AppInGame(this);
		Disconnection = new AppDisconnection(this);
		DevTools = new DevTools(this);
		Ipc = IpcClient.CreateWriteOnlyClient();
	}

	protected override void DoDispose()
	{
		Debug.Assert(Stage == AppStage.Exited);
		Settings.Save();
		InGame.DisposeAndClearInstance();
		Interface.Dispose();
		Fonts.Dispose();
		CoUIManager.Dispose();
		CharacterPartStore.Dispose();
		HytaleServices.Dispose();
		EOSPlatform?.Dispose();
		Engine.Dispose();
		AssetManager.Shutdown();
		Ipc.Dispose();
	}

	internal void SetStage(AppStage newStage)
	{
		Debug.Assert(newStage != Stage);
		Debug.Assert(ThreadHelper.IsMainThread());
		Logger.Info<AppStage, AppStage>("Changing from Stage {from} to {to}", Stage, newStage);
		if (DevTools.IsOpen)
		{
			DevTools.Close();
		}
		switch (Stage)
		{
		case AppStage.Startup:
			Startup.CleanUp();
			break;
		case AppStage.MainMenu:
			MainMenu.CleanUp();
			break;
		case AppStage.GameLoading:
			GameLoading.CleanUp();
			break;
		case AppStage.InGame:
			InGame.CleanUp();
			break;
		case AppStage.Disconnection:
			Disconnection.CleanUp();
			break;
		}
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
		Stage = newStage;
		Debug.Assert(InGame.Instance == null || newStage == AppStage.InGame);
		if (newStage != AppStage.Exited)
		{
			Engine.Graphics.SetVSyncEnabled(newStage != AppStage.InGame || Settings.VSync);
			Engine.Graphics.GPUProgramStore.ResetProgramUniforms();
			ResetElapsedTime();
			Interface.OnAppStageChanged();
		}
		else
		{
			SDL.SDL_HideWindow(Engine.Window.Handle);
		}
	}

	internal void ReplaceCharacterPartStore(CharacterPartStore newPartStore)
	{
		CharacterPartStore.Dispose();
		CharacterPartStore = newPartStore;
	}

	internal void SetupInterface()
	{
		Debug.Assert(Interface == null);
		Interface = new HytaleClient.Interface.Interface(this, Path.Combine(Paths.GameData, "Interface"), OptionsHelper.IsUiDevEnabled);
	}

	internal void SetPlayerSkin(ClientPlayerSkin playerSkin)
	{
		PlayerSkin = playerSkin;
	}

	internal void SetSingleplayerWorldName(string name)
	{
		SingleplayerWorldName = name;
	}

	internal void SetSingleplayerServer(SingleplayerServer server)
	{
		Debug.Assert(ShuttingDownSingleplayerServer == null);
		SingleplayerServer = server;
	}

	public void OnSinglePlayerServerShuttingDown()
	{
		ShuttingDownSingleplayerServer = SingleplayerServer;
		SingleplayerServer = null;
	}

	public void OnSingleplayerServerShutdown(SingleplayerServer server)
	{
		if (server == ShuttingDownSingleplayerServer)
		{
			ShuttingDownSingleplayerServer = null;
		}
		else if (server == SingleplayerServer)
		{
			SingleplayerServer = null;
		}
	}

	public void Exit()
	{
		if (Stage == AppStage.GameLoading)
		{
			GameLoading.Abort();
		}
		SetStage(AppStage.Exited);
	}

	public void Update()
	{
		EOSPlatform?.Tick();
	}

	public void ResetElapsedTime()
	{
		_resetElapsedTime = true;
	}

	public void RunLoop()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Invalid comparison between Unknown and I4
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Invalid comparison between Unknown and I4
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I4
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Invalid comparison between Unknown and I4
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Invalid comparison between Unknown and I4
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Expected I4, but got Unknown
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Invalid comparison between Unknown and I4
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Invalid comparison between Unknown and I4
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Invalid comparison between Unknown and I4
		Stopwatch stopwatch = Stopwatch.StartNew();
		SDL_Event val = default(SDL_Event);
		while (Stage != AppStage.Exited)
		{
			bool flag = false;
			while (Stage != AppStage.Exited && SDL.SDL_PollEvent(ref val) == 1)
			{
				SDL_EventType type = val.type;
				SDL_EventType val2 = type;
				if ((int)val2 <= 512)
				{
					if ((int)val2 != 256)
					{
						if ((int)val2 != 512)
						{
							continue;
						}
						SDL_WindowEventID windowEvent = val.window.windowEvent;
						SDL_WindowEventID val3 = windowEvent;
						switch (val3 - 4)
						{
						case 8:
						case 9:
							Engine.Window.OnFocusChanged((int)val.window.windowEvent == 12);
							if ((int)val.window.windowEvent == 13)
							{
								Interface.Desktop.ClearInput(clearFocus: false);
							}
							break;
						case 3:
						case 4:
						case 5:
						{
							Window.WindowState state = Engine.Window.GetState();
							if (state != Window.WindowState.Minimized)
							{
								bool flag2 = state == Window.WindowState.Maximized;
								if (Settings.Maximized != flag2)
								{
									Settings.Maximized = flag2;
									Settings.Save();
								}
							}
							OnWindowSizeChanged();
							break;
						}
						case 0:
						case 2:
							flag = true;
							break;
						}
						continue;
					}
					Logger.Info("Received SDL_QUIT event");
					if (Stage != AppStage.InGame)
					{
						Exit();
						return;
					}
					if (InGame.CurrentOverlay != AppInGame.InGameOverlay.ConfirmQuit)
					{
						if (DevTools.IsOpen)
						{
							DevTools.Close();
						}
						InGame.SetCurrentOverlay(AppInGame.InGameOverlay.ConfirmQuit);
					}
				}
				else
				{
					if ((val2 - 768 > 1 && (int)val2 != 771 && val2 - 1024 > 3) || !Engine.Window.IsFocused)
					{
						continue;
					}
					OnUserInput(val);
					WebView webView = CoUIManager.FocusedWebView;
					if (webView != null)
					{
						SDL_Event evtCopy = val;
						CoUIManager.RunInThread(delegate
						{
							//IL_000d: Unknown result type (might be due to invalid IL or missing references)
							CoUIViewInputForwarder.OnUserInput(webView, evtCopy, Engine.Window);
						});
					}
				}
			}
			if (Stage == AppStage.Exited)
			{
				break;
			}
			if (flag)
			{
				Engine.Window.SetupViewport();
				OnWindowSizeChanged();
			}
			EOSPlatform?.Tick();
			CoUIManager.Update();
			Engine.Temp_ProcessQueuedActions();
			if (Stage == AppStage.Exited)
			{
				break;
			}
			if (DevTools.IsDiagnosticsModeEnabled && Interface.HasLoaded)
			{
				DevTools.HandleMessageQueue();
			}
			float deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
			stopwatch.Restart();
			if (_resetElapsedTime)
			{
				deltaTime = 0f;
				_resetElapsedTime = false;
			}
			Fonts.BuildMissingGlyphs();
			Interface.Update(deltaTime);
			switch (Stage)
			{
			case AppStage.MainMenu:
				MainMenu.OnNewFrame(deltaTime);
				break;
			case AppStage.InGame:
				InGame.OnNewFrame(deltaTime);
				break;
			default:
			{
				Interface.PrepareForDraw();
				GLFunctions gL = Engine.Graphics.GL;
				gL.Viewport(Engine.Window.Viewport);
				gL.ClearColor(0f, 0f, 0f, 1f);
				gL.Clear((GL)17664u);
				gL.Enable(GL.BLEND);
				gL.BlendFunc(GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
				Interface.Draw();
				gL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
				break;
			}
			}
			NativeMemory.EndBump();
			HandleScreenshotting();
			SDL.SDL_GL_SwapWindow(Engine.Window.Handle);
			int num = (Engine.Window.IsFocused ? Settings.FpsLimit : 30);
			bool flag3 = Settings.UnlimitedFps && Engine.Window.IsFocused;
			float num2 = 1f / (float)num;
			CpuTime = (float)stopwatch.Elapsed.TotalSeconds;
			if (!flag3 && CpuTime < num2)
			{
				Thread.Sleep((int)(MathHelper.Max(0f, num2 - CpuTime - 0.002f) * 1000f));
				for (float num3 = (float)stopwatch.Elapsed.TotalSeconds; num3 < num2; num3 = (float)stopwatch.Elapsed.TotalSeconds)
				{
					Thread.Sleep(0);
				}
			}
			if (Engine.Window.GetState() == Window.WindowState.Minimized)
			{
				Thread.Sleep(10);
			}
		}
	}

	private void OnUserInput(SDL_Event @event)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Invalid comparison between Unknown and I4
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Invalid comparison between Unknown and I4
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Invalid comparison between Unknown and I4
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Invalid comparison between Unknown and I4
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Invalid comparison between Unknown and I4
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		if ((int)@event.type == 768 && Input.EventMatchesBinding(@event, Settings.InputBindings.OpenDevTools) && Settings.DiagnosticMode)
		{
			if (!DevTools.IsOpen && Interface.Desktop.FocusedElement == null)
			{
				DevTools.Open();
			}
			else if (DevTools.IsOpen)
			{
				DevTools.Close();
			}
			return;
		}
		switch (Stage)
		{
		case AppStage.Startup:
			return;
		case AppStage.MainMenu:
			MainMenu.OnUserInput(@event);
			break;
		case AppStage.InGame:
			if (!InGame.Instance.Disposed)
			{
				InGame.Instance.OnUserInput(@event);
			}
			break;
		}
		SDL_EventType type = @event.type;
		SDL_EventType val = type;
		if ((int)val == 768 || (int)val == 1025)
		{
			if (Input.EventMatchesBinding(@event, Settings.InputBindings.TakeScreenshot))
			{
				ScheduleScreenshot();
			}
			else if (Input.EventMatchesBinding(@event, Settings.InputBindings.ToggleFullscreen))
			{
				ToggleFullscreen();
			}
		}
		if ((int)@event.type == 768 && (int)@event.key.keysym.sym == 1073741890)
		{
			bool forceReset = (@event.key.keysym.mod & 3) > 0;
			Engine.Graphics.GPUProgramStore.ResetPrograms(forceReset);
			InGame.Instance?.Chat.Log("Shaders have been reloaded.");
		}
		if (CoUIManager.FocusedWebView == null)
		{
			Interface.OnUserInput(@event);
		}
	}

	private void OnWindowSizeChanged()
	{
		Interface.OnWindowSizeChanged();
		int width = Engine.Window.Viewport.Width;
		int height = Engine.Window.Viewport.Height;
		switch (Stage)
		{
		case AppStage.MainMenu:
			Engine.Graphics.RTStore.Resize(width, height);
			MainMenu.SceneRenderer.Resize(width, height);
			MainMenu.PostEffectRenderer.Resize(width, height, 1f);
			break;
		case AppStage.GameLoading:
		case AppStage.InGame:
			InGame.Instance?.Resize(width, height);
			break;
		}
	}

	private void ScheduleScreenshot()
	{
		if (_screenshotFenceSync == IntPtr.Zero)
		{
			Interface.ClearFlash();
			_wasScreenshotRequested = true;
		}
	}

	private void HandleScreenshotting()
	{
		GLFunctions gL = Engine.Graphics.GL;
		if (_wasScreenshotRequested)
		{
			Debug.Assert(_screenshotFenceSync == IntPtr.Zero);
			_wasScreenshotRequested = false;
			_upcomingScreenshotSize = new Point(Engine.Window.Viewport.Width, Engine.Window.Viewport.Height);
			gL.BindVertexArray(GLVertexArray.None);
			_screenshotPixelBuffer = gL.GenBuffer();
			gL.BindBuffer(GLVertexArray.None, GL.PIXEL_PACK_BUFFER, _screenshotPixelBuffer);
			gL.BufferData(GL.PIXEL_PACK_BUFFER, (IntPtr)(_upcomingScreenshotSize.X * _upcomingScreenshotSize.Y * 4), IntPtr.Zero, GL.DYNAMIC_READ);
			gL.ReadPixels(0, 0, _upcomingScreenshotSize.X, _upcomingScreenshotSize.Y, GL.BGRA, GL.UNSIGNED_BYTE, IntPtr.Zero);
			gL.BindBuffer(GLVertexArray.None, GL.PIXEL_PACK_BUFFER, GLBuffer.None);
			_screenshotFenceSync = gL.FenceSync(GL.SYNC_GPU_COMMANDS_COMPLETE, GL.NO_ERROR);
			if (_screenshotFenceSync == IntPtr.Zero)
			{
				gL.DeleteBuffer(_screenshotPixelBuffer);
				throw new Exception("Failed to get fence sync!");
			}
			Interface.Flash();
		}
		else
		{
			if (!(_screenshotFenceSync != IntPtr.Zero))
			{
				return;
			}
			gL.GetSynciv(_screenshotFenceSync, GL.SYNC_STATUS, (IntPtr)8, IntPtr.Zero, out var values);
			if ((int)values != 37145)
			{
				return;
			}
			Point size = _upcomingScreenshotSize;
			_upcomingScreenshotSize = Point.Zero;
			gL.BindVertexArray(GLVertexArray.None);
			gL.BindBuffer(GLVertexArray.None, GL.PIXEL_PACK_BUFFER, _screenshotPixelBuffer);
			IntPtr intPtr = gL.MapBufferRange(GL.PIXEL_PACK_BUFFER, IntPtr.Zero, (IntPtr)(size.X * size.Y * 4), GL.ONE);
			byte[] rawPixels = new byte[size.X * size.Y * 4];
			for (int i = 0; i < size.Y; i++)
			{
				Marshal.Copy(intPtr + i * size.X * 4, rawPixels, (size.Y - i - 1) * size.X * 4, size.X * 4);
			}
			gL.UnmapBuffer(GL.PIXEL_PACK_BUFFER);
			gL.BindBuffer(GLVertexArray.None, GL.PIXEL_PACK_BUFFER, GLBuffer.None);
			gL.DeleteBuffer(_screenshotPixelBuffer);
			gL.DeleteSync(_screenshotFenceSync);
			_screenshotPixelBuffer = GLBuffer.None;
			_screenshotFenceSync = IntPtr.Zero;
			ThreadPool.QueueUserWorkItem(delegate
			{
				Directory.CreateDirectory(ScreenshotsPath);
				try
				{
					string tempFileName = Path.GetTempFileName();
					new Image(size.X, size.Y, rawPixels).SavePNG(tempFileName, 16711680u, 65280u, 255u, 0u);
					int num = 0;
					for (int j = 0; j < 100; j++)
					{
						string arg = ((num > 0) ? $"_{num}" : "");
						string destFileName = Path.Combine(ScreenshotsPath, $"Hytale{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{arg}.png");
						num++;
						try
						{
							File.Move(tempFileName, destFileName);
							return;
						}
						catch
						{
						}
					}
					throw new Exception("Could not move temp file to screenshots path");
				}
				catch (Exception ex)
				{
					Logger.Error("Failed to save screenshot: " + ex);
					if (InGame.Instance != null)
					{
						Engine.RunOnMainThread(InGame.Instance, delegate
						{
							InGame.Instance.Chat.Log(Interface.GetText("ui.general.failedToSaveScreenshot"));
						});
					}
				}
			});
		}
	}

	public void ApplyNewSettings(Settings newSettings)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Expected O, but got Unknown
		Settings settings = Settings;
		Settings = newSettings;
		if (settings.Language != newSettings.Language)
		{
			Interface.SetLanguageAndLoad(newSettings.Language);
			Interface.LoadAndBuild();
			if (Stage == AppStage.InGame)
			{
				InGame.Instance.Connection.SendPacket((ProtoPacket)new UpdateLanguage(Settings.Language ?? Language.SystemLanguage));
			}
		}
		if (settings.DynamicUIScaling != newSettings.DynamicUIScaling || settings.StaticUIScale != newSettings.StaticUIScale)
		{
			Interface.OnWindowSizeChanged();
		}
		if (settings.Fullscreen != newSettings.Fullscreen || settings.UseBorderlessForFullscreen != newSettings.UseBorderlessForFullscreen || !settings.ScreenResolution.Equals(newSettings.ScreenResolution))
		{
			ScreenResolution screenResolution = newSettings.ScreenResolution;
			Engine.Window.UpdateSize(screenResolution);
			Window.WindowState windowState = Engine.Window.GetState();
			if (windowState == Window.WindowState.Fullscreen)
			{
				windowState = Window.WindowState.Normal;
			}
			Engine.Window.SetState(newSettings.Fullscreen ? Window.WindowState.Fullscreen : windowState, newSettings.Fullscreen && newSettings.UseBorderlessForFullscreen, !settings.ScreenResolution.Equals(newSettings.ScreenResolution));
		}
		if (settings.MouseSettings.MouseRawInputMode != newSettings.MouseSettings.MouseRawInputMode)
		{
			Engine.SetMouseRelativeModeRaw(Settings.MouseSettings.MouseRawInputMode);
		}
		if (Stage == AppStage.InGame)
		{
			if (settings.VSync != newSettings.VSync)
			{
				Engine.Graphics.SetVSyncEnabled(newSettings.VSync);
			}
			if (settings.ViewDistance != newSettings.ViewDistance)
			{
				InGame.Instance.Connection.SendPacket((ProtoPacket)new ViewRadius(Settings.ViewDistance));
			}
			if (settings.RenderScale != newSettings.RenderScale)
			{
				InGame.Instance.SetResolutionScale((float)newSettings.RenderScale * 0.01f);
			}
			if (settings.ViewBobbingEffect != newSettings.ViewBobbingEffect)
			{
				InGame.Instance.CameraModule.CameraShakeController.Reset();
			}
			if (settings.ViewBobbingIntensity != newSettings.ViewBobbingIntensity)
			{
				InGame.Instance.CameraModule.CameraShakeController.Reset();
			}
			if (settings.CameraShakeEffect != newSettings.CameraShakeEffect)
			{
				InGame.Instance.CameraModule.CameraShakeController.Reset();
			}
			if (settings.FirstPersonCameraShakeIntensity != newSettings.FirstPersonCameraShakeIntensity)
			{
				InGame.Instance.CameraModule.CameraShakeController.Reset();
			}
			if (settings.ThirdPersonCameraShakeIntensity != newSettings.ThirdPersonCameraShakeIntensity)
			{
				InGame.Instance.CameraModule.CameraShakeController.Reset();
			}
			if (settings.InputBindings != newSettings.InputBindings)
			{
				InGame.Instance.Input.SetInputBindings(newSettings.InputBindings);
				InGame.Instance.App.Interface.InGameView.AbilitiesHudComponent?.OnUpdateInputBindings();
			}
			if (settings.BuilderToolsSettings.DisplayLegend != newSettings.BuilderToolsSettings.DisplayLegend)
			{
				InGame.Instance.App.Interface.InGameView.UpdateBuilderToolsLegendVisibility();
			}
		}
		if (settings.AudioSettings.OutputDeviceId != newSettings.AudioSettings.OutputDeviceId)
		{
			Engine.Audio.ReplaceOutputDevice(newSettings.AudioSettings.OutputDeviceId);
		}
		if (settings.AudioSettings.MasterVolume != newSettings.AudioSettings.MasterVolume)
		{
			Engine.Audio.MasterVolume = newSettings.AudioSettings.MasterVolume;
		}
		foreach (KeyValuePair<string, float> categoryVolume in newSettings.AudioSettings.CategoryVolumes)
		{
			if (categoryVolume.Value != settings.AudioSettings.CategoryVolumes[categoryVolume.Key])
			{
				Engine.Audio.SetCategoryVolume((int)Enum.Parse(typeof(AudioSettings.SoundCategory), categoryVolume.Key), categoryVolume.Value);
			}
		}
		if (settings.DiagnosticMode != newSettings.DiagnosticMode)
		{
			if (!newSettings.DiagnosticMode)
			{
				DevTools.ClearNotifications();
			}
			DevTools.IsDiagnosticsModeEnabled = newSettings.DiagnosticMode;
		}
	}

	public void ToggleFullscreen()
	{
		Settings settings = Settings.Clone();
		settings.Fullscreen = !settings.Fullscreen;
		ApplyNewSettings(settings);
		Settings.Save();
	}
}
