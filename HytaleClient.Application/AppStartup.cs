#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HytaleClient.Core;
using HytaleClient.Data.Audio;
using HytaleClient.Data.Characters;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Application;

internal class AppStartup
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly App _app;

	private readonly CancellationTokenSource _startupLoadingCancelTokenSource = new CancellationTokenSource();

	public AppStartup(App app)
	{
		_app = app;
	}

	public void StartFromMainMenu()
	{
		Load(delegate
		{
			_app.MainMenu.Open(AppMainMenu.MainMenuPage.Home);
			_app.Interface.OnAppStageChanged();
		});
	}

	public void StartWithLocalWorld(string worldName)
	{
		Load(delegate
		{
			foreach (AppMainMenu.World world in _app.MainMenu.Worlds)
			{
				if (world.Options.Name == worldName)
				{
					_app.GameLoading.Open(world.Path);
					_app.Interface.FadeIn();
					return;
				}
			}
			_app.MainMenu.Open(AppMainMenu.MainMenuPage.Adventure);
			_app.Interface.MainMenuView.AdventurePage.OnFailedToJoinUnknownWorld();
			_app.Interface.FadeIn();
		});
	}

	public void StartWithServerConnection(string address)
	{
		Load(delegate
		{
			_app.MainMenu.Open(AppMainMenu.MainMenuPage.Servers);
			_app.Interface.MainMenuView.ServersPage.HandleAutoConnectOnStartup(address);
		});
	}

	internal void CleanUp()
	{
		_startupLoadingCancelTokenSource.Cancel();
	}

	private void Load(Action onLoaded)
	{
		Debug.Assert(_app.Stage == App.AppStage.Initial);
		_app.SetupInterface();
		_app.SetStage(App.AppStage.Startup);
		ManualResetEventSlim fadeInDoneEvent = new ManualResetEventSlim(initialState: false);
		_app.Interface.FadeIn(delegate
		{
			fadeInDoneEvent.Set();
		}, longFade: true);
		_app.ResetElapsedTime();
		CancellationToken cancelToken = _startupLoadingCancelTokenSource.Token;
		ThreadPool.QueueUserWorkItem(delegate
		{
			Dictionary<string, WwiseResource> upcomingWwiseIds = null;
			byte[][] upcomingHairGradientAtlasPixels = null;
			Task.WaitAll(Task.Run(delegate
			{
				try
				{
					WwiseHeaderParser.Parse(Path.Combine(Paths.BuiltInAssets, "Common/SoundBanks/Wwise_IDs.h"), out upcomingWwiseIds);
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Failed to load wwise header file.");
				}
			}), Task.Run(delegate
			{
				_app.Fonts.LoadFonts(_app.Engine.Graphics);
			}), Task.Run(delegate
			{
				AssetManager.Initialize(_app.Engine, cancelToken, out var newAssets);
				Logger.Info("AssetManager initialized.");
				HashSet<string> updatedCosmeticsAssets = new HashSet<string>();
				if (!AssetManager.IsAssetsDirectoryImmutable)
				{
					updatedCosmeticsAssets = AssetManager.GetUpdatedAssets(Path.Combine(Paths.BuiltInAssets, "Cosmetics"), Path.Combine(Paths.BuiltInAssets, "CosmeticsAssetsIndex.cache"), cancelToken);
					Logger.Info("Finished getting list of updated cosmetic config files.");
				}
				bool textureAtlasNeedsUpdate = false;
				CharacterPartStore characterPartStore = _app.CharacterPartStore;
				characterPartStore.LoadAssets(updatedCosmeticsAssets, ref textureAtlasNeedsUpdate, cancelToken);
				characterPartStore.PrepareGradientAtlas(out upcomingHairGradientAtlasPixels);
				characterPartStore.LoadModelData(_app.Engine, newAssets, textureAtlasNeedsUpdate);
				Logger.Info("CharacterPartStore initialized.");
				if (!cancelToken.IsCancellationRequested)
				{
					string text = Path.Combine(Paths.BuiltInAssets, "CommonAssetsIndex.cache");
					if (File.Exists(text + ".tmp"))
					{
						File.Delete(text);
						File.Move(text + ".tmp", text);
						File.Delete(text + ".tmp");
					}
				}
			}));
			Logger.Info("Background startup loading done.");
			fadeInDoneEvent.Wait();
			_app.Engine.RunOnMainThread(_app.Engine, delegate
			{
				_app.Engine.Audio.ResourceManager.SetupWwiseIds(upcomingWwiseIds);
				_app.CharacterPartStore.BuildGradientTexture(upcomingHairGradientAtlasPixels);
				_app.Fonts.BuildFontTextures();
				_app.Interface.SetLanguageAndLoad(_app.Settings.Language);
				_app.Interface.LoadAndBuild();
				onLoaded();
			});
		});
	}
}
