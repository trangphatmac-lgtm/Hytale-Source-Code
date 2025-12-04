#define DEBUG
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HytaleClient.Application.Auth;
using HytaleClient.AssetEditor.Backends;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.Characters;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.Interface.CoherentUI;
using HytaleClient.Interface.CoherentUI.Internals;
using HytaleClient.Math;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.AssetEditor;

internal class AssetEditorApp : Disposable
{
	public enum AppStage
	{
		Initial,
		Startup,
		MainMenu,
		Editor,
		Exited
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly Engine Engine;

	public readonly AuthManager AuthManager;

	public readonly CoUIManager CoUIManager;

	public readonly AssetEditorInterface Interface;

	public readonly FontManager Fonts;

	public readonly CharacterPartStore CharacterPartStore;

	public readonly AssetEditorAppStartup Startup;

	public readonly AssetEditorAppEditor Editor;

	public readonly AssetEditorAppMainMenu MainMenu;

	private readonly IpcClient _ipcClient;

	private JObject _withheldOpenEditorIpcMessage;

	private bool _resetElapsedTime = false;

	private readonly JsonSerializerSettings _settingsSerializerSettings = new JsonSerializerSettings();

	private readonly string _settingsPath = Path.Combine(Paths.UserData, "Settings.json");

	private readonly object _settingsSaveLock = new object();

	private int _lastSavedSettingsCounter;

	private int _settingsSaveCounter;

	public AppStage Stage { get; private set; }

	public float CpuTime { get; private set; }

	public AssetEditorSettings Settings { get; private set; }

	public AssetEditorApp()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		LoadSettings();
		Window.WindowState initialState = (Settings.Fullscreen ? Window.WindowState.Fullscreen : (Settings.Maximized ? Window.WindowState.Maximized : Window.WindowState.Normal));
		string path = Path.Combine(Paths.EditorData, (BuildInfo.Platform == Platform.MacOS) ? "Icon-256.png" : "Icon-64.png");
		Window.WindowSettings windowSettings = new Window.WindowSettings
		{
			Title = "Hytale Asset Editor",
			Icon = new Image(File.ReadAllBytes(path)),
			InitialSize = new Point(1280, 720),
			MinimumSize = new Point(50, 50),
			Borderless = (Settings.UseBorderlessForFullscreen && Settings.Fullscreen),
			InitialState = initialState,
			Resizable = true
		};
		Engine = new Engine(windowSettings, allowBatcher2dToGrow: true);
		Engine.Profiling.Initialize(1);
		Engine.Graphics.SetVSyncEnabled(enabled: true);
		Engine.Window.Show();
		AuthManager = new AuthManager();
		CoUIManager = new CoUIManager(Engine, new CoUIFileHandler());
		Fonts = new FontManager();
		CharacterPartStore = new CharacterPartStore(Engine.Graphics.GL);
		Interface = new AssetEditorInterface(this, OptionsHelper.IsUiDevEnabled);
		Startup = new AssetEditorAppStartup(this);
		MainMenu = new AssetEditorAppMainMenu(this);
		Editor = new AssetEditorAppEditor(this);
		_ipcClient = IpcClient.CreateReadWriteClient(OnReceiveIpcMessage);
	}

	protected override void DoDispose()
	{
		Debug.Assert(Stage == AppStage.Exited);
		SaveSettingsBlocking();
		_ipcClient.Dispose();
		CharacterPartStore.Dispose();
		Interface.Dispose();
		Fonts.Dispose();
		CoUIManager.Dispose();
		Engine.Dispose();
	}

	internal void SetStage(AppStage newStage)
	{
		Debug.Assert(newStage != Stage);
		Debug.Assert(ThreadHelper.IsMainThread());
		Logger.Info<AppStage, AppStage>("Changing from Stage {from} to {to}", Stage, newStage);
		switch (Stage)
		{
		case AppStage.Startup:
			Startup.CleanUp();
			break;
		case AppStage.Editor:
			Editor.CleanUp();
			break;
		case AppStage.MainMenu:
			MainMenu.CleanUp();
			break;
		}
		Stage = newStage;
		if (newStage != AppStage.Exited)
		{
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
			if (Stage == AppStage.MainMenu && _withheldOpenEditorIpcMessage != null)
			{
				HandleWithheldOpenEditorIpcMessage(raiseWindow: true);
			}
			Engine.Graphics.GPUProgramStore.ResetProgramUniforms();
			ResetElapsedTime();
			Interface.OnAppStageChanged();
		}
		else
		{
			SDL.SDL_HideWindow(Engine.Window.Handle);
		}
	}

	public void Exit()
	{
		SetStage(AppStage.Exited);
	}

	private void OnReceiveIpcMessage(string messageType, JObject data)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		if (!(messageType != "OpenEditor"))
		{
			Engine.RunOnMainThread(this, delegate
			{
				HandleOpenEditorIpcMessage(data, raiseWindow: true);
			});
		}
	}

	private void HandleOpenEditorIpcMessage(JObject data, bool raiseWindow)
	{
		if (Stage == AppStage.Exited)
		{
			return;
		}
		if (Stage == AppStage.Startup)
		{
			_withheldOpenEditorIpcMessage = data;
			return;
		}
		bool flag = false;
		JToken val = default(JToken);
		if (data.TryGetValue("Cosmetics", ref val))
		{
			flag = (bool)val;
		}
		AssetIdReference assetIdReference = AssetIdReference.None;
		string text = null;
		JToken val2 = default(JToken);
		JToken val3 = default(JToken);
		if (data.TryGetValue("AssetPath", ref val2))
		{
			text = (string)val2;
		}
		else if (data.TryGetValue("AssetId", ref val3))
		{
			assetIdReference = new AssetIdReference((string)data["AssetType"], (string)val3);
		}
		if (flag)
		{
			if (Stage == AppStage.MainMenu)
			{
				if (MainMenu.IsConnectingToServer)
				{
					MainMenu.CancelConnection();
				}
				Editor.OpenCosmeticsEditor();
				if (text != null)
				{
					Editor.OpenAsset(text);
				}
				else if (assetIdReference.Id != null)
				{
					Editor.OpenAsset(assetIdReference);
				}
			}
			else if (Stage == AppStage.Editor)
			{
				if (Editor.Backend is LocalAssetEditorBackend)
				{
					if (text != null)
					{
						Editor.OpenAsset(text);
					}
					else if (assetIdReference.Id != null)
					{
						Editor.OpenAsset(assetIdReference);
					}
				}
				else
				{
					_withheldOpenEditorIpcMessage = data;
					Editor.ShowIpcServerConnectionPrompt(Interface.GetText("ui.assetEditor.cosmeticEditor"));
				}
			}
		}
		else
		{
			string text2 = (string)data["Hostname"];
			int num = (int)data["Port"];
			if (Stage == AppStage.MainMenu)
			{
				if (MainMenu.IsConnectingToServer)
				{
					if (MainMenu.Connection.Hostname != text2 || MainMenu.Connection.Port != num)
					{
						MainMenu.CancelConnection();
						MainMenu.ConnectToServer(text2, num);
					}
				}
				else
				{
					MainMenu.ConnectToServer(text2, num);
				}
				if (text != null)
				{
					MainMenu.AssetPathToOpen = text;
					MainMenu.AssetIdToOpen = AssetIdReference.None;
				}
				else if (assetIdReference.Id != null)
				{
					MainMenu.AssetPathToOpen = null;
					MainMenu.AssetIdToOpen = assetIdReference;
				}
			}
			else if (Stage == AppStage.Editor)
			{
				if (Editor.Backend is ServerAssetEditorBackend serverAssetEditorBackend && serverAssetEditorBackend.Hostname == text2 && serverAssetEditorBackend.Port == num)
				{
					if (text != null)
					{
						Editor.OpenAsset(text);
					}
					else if (assetIdReference.Id != null)
					{
						Editor.OpenAsset(assetIdReference);
					}
				}
				else
				{
					_withheldOpenEditorIpcMessage = data;
					Editor.ShowIpcServerConnectionPrompt((string)data["Name"]);
				}
			}
		}
		if (raiseWindow)
		{
			Engine.Window.Raise();
		}
	}

	public void HandleWithheldOpenEditorIpcMessage(bool raiseWindow)
	{
		JObject withheldOpenEditorIpcMessage = _withheldOpenEditorIpcMessage;
		_withheldOpenEditorIpcMessage = null;
		HandleOpenEditorIpcMessage(withheldOpenEditorIpcMessage, raiseWindow);
	}

	public void ResetElapsedTime()
	{
		_resetElapsedTime = true;
	}

	public void RunLoop()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Invalid comparison between Unknown and I4
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Invalid comparison between Unknown and I4
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Invalid comparison between Unknown and I4
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Invalid comparison between Unknown and I4
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Invalid comparison between Unknown and I4
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Expected I4, but got Unknown
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Invalid comparison between Unknown and I4
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Invalid comparison between Unknown and I4
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Invalid comparison between Unknown and I4
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
							Interface.OnWindowFocusChanged();
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
									SaveSettings();
								}
							}
							break;
						}
						case 0:
						case 2:
							flag = true;
							break;
						}
					}
					else
					{
						Logger.Info("Received SDL_QUIT event");
						if (Stage != AppStage.Editor || !Interface.AssetEditor.CheckHasUnexportedChanges(quit: true, Exit))
						{
							Exit();
						}
					}
				}
				else if ((val2 - 768 <= 1 || (int)val2 == 771 || val2 - 1024 <= 3) && Engine.Window.IsFocused)
				{
					OnUserInput(val);
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
			CoUIManager.Update();
			Engine.Temp_ProcessQueuedActions();
			if (Stage == AppStage.Exited)
			{
				break;
			}
			float deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
			stopwatch.Restart();
			if (_resetElapsedTime)
			{
				deltaTime = 0f;
				_resetElapsedTime = false;
			}
			Fonts.BuildMissingGlyphs();
			if (Stage == AppStage.Editor)
			{
				Editor.GameTime.OnNewFrame(deltaTime);
			}
			Interface.Update(deltaTime);
			Interface.PrepareForDraw();
			GLFunctions gL = Engine.Graphics.GL;
			gL.Viewport(Engine.Window.Viewport);
			RenderTarget.BindHardwareFramebuffer();
			gL.ClearColor(0f, 0f, 0f, 1f);
			gL.Clear((GL)17664u);
			gL.Disable(GL.DEPTH_TEST);
			gL.Enable(GL.BLEND);
			gL.ActiveTexture(GL.TEXTURE0);
			gL.BlendFunc(GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
			Interface.Draw();
			gL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
			SDL.SDL_GL_SwapWindow(Engine.Window.Handle);
			int num = (Engine.Window.IsFocused ? 60 : 30);
			float num2 = 1f / (float)num;
			CpuTime = (float)stopwatch.Elapsed.TotalSeconds;
			if (CpuTime < num2)
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
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Invalid comparison between Unknown and I4
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		if ((int)@event.type == 768 && (int)@event.key.keysym.sym == 1073741890)
		{
			bool forceReset = (@event.key.keysym.mod & 3) > 0;
			Engine.Graphics.GPUProgramStore.ResetPrograms(forceReset);
			Logger.Info("Shaders have been reloaded.");
		}
		if (CoUIManager.FocusedWebView == null)
		{
			Interface.OnUserInput(@event);
			return;
		}
		WebView webView = CoUIManager.FocusedWebView;
		SDL_Event evtCopy = @event;
		CoUIManager.RunInThread(delegate
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			CoUIViewInputForwarder.OnUserInput(webView, evtCopy, Engine.Window);
		});
	}

	private void OnWindowSizeChanged()
	{
		Interface.OnWindowSizeChanged();
		int width = Engine.Window.Viewport.Width;
		int height = Engine.Window.Viewport.Height;
		Engine.Graphics.RTStore.Resize(width, height);
	}

	public void ApplySettings(AssetEditorSettings newSettings)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		AssetEditorSettings settings = Settings;
		Settings = newSettings;
		SaveSettings();
		if (settings.Language != newSettings.Language)
		{
			Interface.SetLanguageAndLoad(newSettings.Language);
			Interface.LoadAndBuild();
			if (Stage == AppStage.Editor)
			{
				Editor.Backend.OnLanguageChanged();
			}
		}
		if (settings.Fullscreen != newSettings.Fullscreen || settings.UseBorderlessForFullscreen != newSettings.UseBorderlessForFullscreen)
		{
			Window.WindowState windowState = Engine.Window.GetState();
			if (windowState == Window.WindowState.Fullscreen)
			{
				windowState = Window.WindowState.Normal;
			}
			Engine.Window.SetState(newSettings.Fullscreen ? Window.WindowState.Fullscreen : windowState, newSettings.Fullscreen && newSettings.UseBorderlessForFullscreen, recalculateZoom: false);
		}
		if (settings.AssetsPath != newSettings.AssetsPath && Stage == AppStage.Editor)
		{
			Editor.OnAssetEditorPathChanged();
		}
		if (settings.DisplayDefaultAssetPathWarning != newSettings.DisplayDefaultAssetPathWarning && Stage == AppStage.Editor)
		{
			Interface.AssetEditor.UpdateAssetPathWarning(newSettings.DisplayDefaultAssetPathWarning);
		}
	}

	private string GetSettingsJsonString()
	{
		return JsonConvert.SerializeObject((object)Settings, (Formatting)1, _settingsSerializerSettings);
	}

	public void SaveSettings()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_settingsSaveCounter++;
		int version = _settingsSaveCounter;
		string jsonString = GetSettingsJsonString();
		Task.Run(delegate
		{
			SaveSettingsToFile(jsonString, version);
		}).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to save asset editor settings");
			}
		});
	}

	private void SaveSettingsBlocking()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_settingsSaveCounter++;
		int settingsSaveCounter = _settingsSaveCounter;
		string settingsJsonString = GetSettingsJsonString();
		SaveSettingsToFile(settingsJsonString, settingsSaveCounter);
	}

	private void SaveSettingsToFile(string data, int count)
	{
		lock (_settingsSaveLock)
		{
			if (_lastSavedSettingsCounter <= count)
			{
				File.WriteAllText(_settingsPath + ".new", data);
				if (File.Exists(_settingsPath))
				{
					File.Replace(_settingsPath + ".new", _settingsPath, _settingsPath + ".bak");
				}
				else
				{
					File.Move(_settingsPath + ".new", _settingsPath);
				}
				_lastSavedSettingsCounter = count;
			}
		}
	}

	private void LoadSettings()
	{
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Expected O, but got Unknown
		Logger.Info("Loading settings...");
		Settings = new AssetEditorSettings();
		if (!File.Exists(_settingsPath))
		{
			Logger.Info("Settings file does not exist. Initializing...");
			Settings.Initialize();
			SaveSettings();
			return;
		}
		JObject val;
		try
		{
			val = JObject.Parse(File.ReadAllText(_settingsPath));
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to load asset editor settings json");
			Settings.Initialize();
			SaveSettings();
			return;
		}
		int num = 1;
		JToken val2 = default(JToken);
		if (val.TryGetValue("FormatVersion", ref val2))
		{
			num = (int)val2;
		}
		int num2 = 1;
		bool flag = num2 > num;
		if (flag)
		{
			AssetEditorSettings.Migrate(val, num);
			Logger.Info<int, int>("Migrated settings from format version {0} to {1}", num, num2);
		}
		try
		{
			JsonSerializer val3 = JsonSerializer.CreateDefault(_settingsSerializerSettings);
			JTokenReader val4 = new JTokenReader((JToken)(object)val);
			try
			{
				val3.Populate((JsonReader)(object)val4, (object)Settings);
			}
			finally
			{
				((IDisposable)val4)?.Dispose();
			}
		}
		catch (Exception ex2)
		{
			Logger.Error(ex2, "Failed to convert JSON object to settings object");
			Settings.Initialize();
			SaveSettings();
			return;
		}
		Settings.FormatVersion = num2;
		Settings.Initialize();
		if (flag)
		{
			SaveSettings();
		}
	}
}
