#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HytaleClient.AssetEditor.Data;
using HytaleClient.Data.Characters;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.AssetEditor;

internal class AssetEditorAppStartup
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly AssetEditorApp _app;

	private readonly CancellationTokenSource _startupLoadingCancelTokenSource = new CancellationTokenSource();

	public AssetEditorAppStartup(AssetEditorApp app)
	{
		_app = app;
	}

	public void StartFromCosmeticsEditor()
	{
		Load(delegate
		{
			_app.Editor.OpenCosmeticsEditor();
		});
	}

	public void StartFromMainMenu()
	{
		Load(delegate
		{
			_app.SetStage(AssetEditorApp.AppStage.MainMenu);
			_app.Engine.Window.Show();
		});
	}

	public void StartFromCosmeticsEditorWithPath(string assetFile)
	{
		Load(delegate
		{
			_app.Editor.OpenCosmeticsEditor();
			_app.Editor.OpenAsset(assetFile);
		});
	}

	public void StartFromCosmeticsEditorWithId(string assetType, string assetId)
	{
		Load(delegate
		{
			_app.Editor.OpenCosmeticsEditor();
			_app.Editor.OpenAsset(new AssetIdReference(assetType, assetId));
		});
	}

	public void StartFromAssetEditor(string address)
	{
		Load(delegate
		{
			_app.SetStage(AssetEditorApp.AppStage.MainMenu);
			if (!_app.MainMenu.IsConnectingToServer)
			{
				if (HostnameHelper.TryParseHostname(address, 5520, out var host, out var port, out var error))
				{
					_app.MainMenu.ConnectToServer(host, port);
				}
				else
				{
					Logger.Warn<string, string>("Invalid address '{0}': {1}", address, error);
				}
			}
		});
	}

	public void StartFromAssetEditorWithPath(string address, string assetFile)
	{
		_app.MainMenu.AssetPathToOpen = assetFile;
		StartFromAssetEditor(address);
	}

	public void StartFromAssetEditorWithId(string address, string assetType, string assetId)
	{
		_app.MainMenu.AssetIdToOpen = new AssetIdReference(assetType, assetId);
		StartFromAssetEditor(address);
	}

	private async void Load(Action onLoaded)
	{
		Debug.Assert(_app.Stage == AssetEditorApp.AppStage.Initial);
		_app.SetStage(AssetEditorApp.AppStage.Startup);
		ManualResetEventSlim fadeInDoneEvent = new ManualResetEventSlim(initialState: false);
		_app.Interface.FadeIn(delegate
		{
			fadeInDoneEvent.Set();
		}, longFade: true);
		_app.ResetElapsedTime();
		byte[][] upcomingHairGradientAtlasPixels = null;
		await Task.WhenAll(Task.Run(delegate
		{
			_app.Fonts.LoadFonts(_app.Engine.Graphics);
		}), Task.Run(delegate
		{
			bool textureAtlasNeedsUpdate = true;
			CharacterPartStore characterPartStore = _app.CharacterPartStore;
			characterPartStore.LoadAssets(new HashSet<string>(), ref textureAtlasNeedsUpdate, _startupLoadingCancelTokenSource.Token);
			characterPartStore.PrepareGradientAtlas(out upcomingHairGradientAtlasPixels);
		}));
		Logger.Info("Background startup loading done.");
		if (_startupLoadingCancelTokenSource.IsCancellationRequested)
		{
			return;
		}
		fadeInDoneEvent.Wait();
		_app.Engine.RunOnMainThread(_app.Engine, delegate
		{
			if (!_startupLoadingCancelTokenSource.IsCancellationRequested)
			{
				_app.Fonts.BuildFontTextures();
				_app.CharacterPartStore.BuildGradientTexture(upcomingHairGradientAtlasPixels);
				_app.Interface.SetLanguageAndLoad(_app.Settings.Language);
				_app.Interface.LoadAndBuild();
				onLoaded();
			}
		});
	}

	public void CleanUp()
	{
		_startupLoadingCancelTokenSource.Cancel();
	}
}
