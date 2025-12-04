using System;
using System.Diagnostics;
using System.IO;
using HytaleClient.AssetEditor;
using HytaleClient.Audio.Commands;
using HytaleClient.Core;
using HytaleClient.Graphics;
using HytaleClient.Utils;
using NLog;
using Sentry;
using Sentry.Protocol;

namespace HytaleClient.Application;

internal class Program
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private static void Main(string[] args)
	{
		if (!Debugger.IsAttached)
		{
			Directory.SetCurrentDirectory(Paths.App);
		}
		Language.Initialize();
		BitUtils.UnitTest();
		MemoryPoolHelper.UnitTest();
		FXMemoryPool.UnitTest();
		ClusteredLighting.UnitTest();
		CommandMemoryPool.UnitTest();
		CommandMemoryPool.CommandBufferUnitTest();
		CommandMemoryPool.StressTest(continuous: false, out var resultLog);
		Console.WriteLine(resultLog);
		if (!OptionsHelper.Setup(args))
		{
			return;
		}
		Paths.Setup();
		LogWriter.Start();
		if (BuildInfo.Platform == Platform.Windows)
		{
			string text = (OptionsHelper.LaunchEditor ? "Editor" : "Game");
			int num = WindowsUtils.SetApplicationUserModelId("HypixelStudios.Hytale." + text);
			if (num != 0)
			{
				Logger.Warn("Failed to set application user model id, result: {0}", num);
			}
		}
		using (new ApplicationMutex())
		{
			SentrySdk.ConfigureScope((Action<Scope>)delegate(Scope o)
			{
				BaseScopeExtensions.SetTag((BaseScope)(object)o, "Build.Platform", BuildInfo.Platform.ToString());
				BaseScopeExtensions.SetTag((BaseScope)(object)o, "Build.Architecture", BuildInfo.Architecture);
				BaseScopeExtensions.SetTag((BaseScope)(object)o, "Build.Configuration", BuildInfo.Configuration);
				BaseScopeExtensions.SetTag((BaseScope)(object)o, "Build.Version", BuildInfo.Version);
				BaseScopeExtensions.SetTag((BaseScope)(object)o, "Build.RevisionId", BuildInfo.RevisionId);
				BaseScopeExtensions.SetTag((BaseScope)(object)o, "Build.BranchName", BuildInfo.BranchName);
			});
			if (!Debugger.IsAttached)
			{
				CrashHandler.Hook();
			}
			BuildInfo.PrintAll();
			GraphicsDevice.TryForceDedicatedNvGraphics();
			ThreadHelper.Initialize();
			AffinityHelper.Setup();
			Engine.Initialize();
			if (OptionsHelper.LaunchEditor)
			{
				StartAssetEditor();
			}
			else
			{
				StartGame();
			}
		}
	}

	private static void StartGame()
	{
		using App app = new App();
		string username = app.Username;
		SentrySdk.ConfigureScope((Action<Scope>)delegate(Scope o)
		{
			BaseScopeExtensions.SetTag((BaseScope)(object)o, "Username", username);
		});
		if (OptionsHelper.ServerAddress != null)
		{
			app.Startup.StartWithServerConnection(OptionsHelper.ServerAddress);
		}
		else if (OptionsHelper.WorldName != null)
		{
			app.Startup.StartWithLocalWorld(OptionsHelper.WorldName);
		}
		else
		{
			app.Startup.StartFromMainMenu();
		}
		app.RunLoop();
	}

	private static void StartAssetEditor()
	{
		using AssetEditorApp assetEditorApp = new AssetEditorApp();
		string username = assetEditorApp.AuthManager.Settings.Username;
		SentrySdk.ConfigureScope((Action<Scope>)delegate(Scope o)
		{
			BaseScopeExtensions.SetTag((BaseScope)(object)o, "Username", username);
		});
		if (OptionsHelper.ServerAddress != null)
		{
			if (OptionsHelper.OpenAssetPath != null)
			{
				assetEditorApp.Startup.StartFromAssetEditorWithPath(OptionsHelper.ServerAddress, OptionsHelper.OpenAssetPath);
			}
			else if (OptionsHelper.OpenAssetType != null && OptionsHelper.OpenAssetId != null)
			{
				assetEditorApp.Startup.StartFromAssetEditorWithId(OptionsHelper.ServerAddress, OptionsHelper.OpenAssetType, OptionsHelper.OpenAssetId);
			}
			else
			{
				assetEditorApp.Startup.StartFromAssetEditor(OptionsHelper.ServerAddress);
			}
		}
		else if (OptionsHelper.OpenAssetPath != null)
		{
			assetEditorApp.Startup.StartFromCosmeticsEditorWithPath(OptionsHelper.OpenAssetPath);
		}
		else if (OptionsHelper.OpenAssetType != null && OptionsHelper.OpenAssetId != null)
		{
			assetEditorApp.Startup.StartFromCosmeticsEditorWithId(OptionsHelper.OpenAssetType, OptionsHelper.OpenAssetId);
		}
		else if (OptionsHelper.OpenCosmetics)
		{
			assetEditorApp.Startup.StartFromCosmeticsEditor();
		}
		else
		{
			assetEditorApp.Startup.StartFromMainMenu();
		}
		assetEditorApp.RunLoop();
	}
}
