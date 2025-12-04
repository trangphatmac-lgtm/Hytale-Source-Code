#define DEBUG
using System;
using System.Diagnostics;
using System.Net.Sockets;
using Hypixel.ProtoPlus;
using HytaleClient.Interface;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Sentry;
using Sentry.Protocol;

namespace HytaleClient.Application;

internal class AppGameLoading
{
	public enum GameLoadingStage
	{
		Initial,
		WaitingForServerToShutdown,
		BootingServer,
		Connecting,
		Loading,
		Aborted,
		Complete
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly App _app;

	public GameLoadingStage LoadingStage { get; private set; }

	public ConnectionToServer Connection { get; private set; }

	public AppGameLoading(App app)
	{
		_app = app;
	}

	public void Open(string singleplayerWorldName)
	{
		SentrySdk.ConfigureScope((Action<Scope>)delegate(Scope o)
		{
			BaseScopeExtensions.UnsetTag((BaseScope)(object)o, "Server");
			BaseScopeExtensions.SetTag((BaseScope)(object)o, "World", singleplayerWorldName);
		});
		LoadingStage = GameLoadingStage.Initial;
		_app.SetSingleplayerWorldName(singleplayerWorldName);
		_app.SetStage(App.AppStage.GameLoading);
		_app.MainMenu.SetPageToReturnTo(AppMainMenu.MainMenuPage.Adventure);
		if (_app.ShuttingDownSingleplayerServer != null)
		{
			_app.Interface.GameLoadingView.SetStatus("Waiting for server to finish shutting down...", 0f);
			SetStage(GameLoadingStage.WaitingForServerToShutdown);
		}
		else
		{
			StartSingleplayerServer();
		}
	}

	public void Open(string hostname, int port, AppMainMenu.MainMenuPage? returnPage = null)
	{
		SentrySdk.ConfigureScope((Action<Scope>)delegate(Scope o)
		{
			BaseScopeExtensions.UnsetTag((BaseScope)(object)o, "World");
			BaseScopeExtensions.SetTag((BaseScope)(object)o, "Server", $"{hostname}:{port}");
		});
		_app.DevTools.Info($"Connecting to server {hostname}:{port}");
		LoadingStage = GameLoadingStage.Initial;
		Connection = new ConnectionToServer(_app.Engine, hostname, port, OnConnected, OnDisconnectedWithError);
		_app.SetStage(App.AppStage.GameLoading);
		if (returnPage.HasValue)
		{
			_app.MainMenu.SetPageToReturnTo(returnPage.Value);
		}
		SetStage(GameLoadingStage.Connecting);
		_app.Interface.GameLoadingView.SetStatus("Connecting...", 0f);
	}

	internal void CleanUp()
	{
		if (LoadingStage == GameLoadingStage.Complete)
		{
			Debug.Assert(Connection == null);
		}
		else
		{
			if (Connection != null)
			{
				Connection.OnDisconnected = null;
				Connection.Close();
				Connection = null;
			}
			if (_app.SingleplayerServer != null)
			{
				_app.SingleplayerServer.Close();
				_app.OnSinglePlayerServerShuttingDown();
			}
			_app.InGame.DisposeAndClearInstance();
			if (_app.Interface.FadeState == BaseInterface.InterfaceFadeState.FadingOut || _app.Interface.FadeState == BaseInterface.InterfaceFadeState.FadedOut)
			{
				_app.Interface.FadeIn();
			}
		}
		SentrySdk.ConfigureScope((Action<Scope>)delegate(Scope o)
		{
			BaseScopeExtensions.UnsetTag((BaseScope)(object)o, "World");
			BaseScopeExtensions.UnsetTag((BaseScope)(object)o, "Server");
		});
	}

	public void AssertStage(GameLoadingStage stage)
	{
		if (LoadingStage == stage)
		{
			return;
		}
		throw new Exception($"Loading stage is {LoadingStage} but expected {stage}");
	}

	public void AssertStage(GameLoadingStage stage1, GameLoadingStage stage2)
	{
		if (LoadingStage == stage1 || LoadingStage == stage2)
		{
			return;
		}
		throw new Exception($"Loading stage is {LoadingStage} but expected {stage1} or {stage2}");
	}

	public void SetStage(GameLoadingStage stage)
	{
		Logger.Info<GameLoadingStage, GameLoadingStage>("Changing from loading stage {from} to {to}", LoadingStage, stage);
		LoadingStage = stage;
		if (LoadingStage == GameLoadingStage.Complete)
		{
			Connection = null;
		}
	}

	public void StartSingleplayerServer()
	{
		AssertStage(GameLoadingStage.Initial, GameLoadingStage.WaitingForServerToShutdown);
		SetStage(GameLoadingStage.BootingServer);
		Debug.Assert(_app.SingleplayerServer == null);
		Debug.Assert(_app.ShuttingDownSingleplayerServer == null);
		_app.Interface.GameLoadingView.SetStatus("Booting server...", 0f);
		SingleplayerServer server = null;
		try
		{
			server = new SingleplayerServer(_app, _app.SingleplayerWorldName, OnSingleplayerServerProgress, OnSingleplayerServerReady, delegate
			{
				_app.OnSingleplayerServerShutdown(server);
				if (LoadingStage == GameLoadingStage.BootingServer)
				{
					OnDisconnectedWithError(new Exception("Server failed to boot."));
				}
			});
			_app.SetSingleplayerServer(server);
		}
		catch (Exception ex)
		{
			Logger.Error<Exception>(ex);
			Abort();
		}
		void OnSingleplayerServerProgress(string message, float progress)
		{
			if (LoadingStage != GameLoadingStage.Aborted)
			{
				_app.Interface.GameLoadingView.SetStatus("Booting server... (" + message + ")", progress);
			}
		}
		void OnSingleplayerServerReady()
		{
			Debug.Assert(ThreadHelper.IsMainThread());
			if (LoadingStage != GameLoadingStage.Aborted)
			{
				AssertStage(GameLoadingStage.BootingServer);
				SetStage(GameLoadingStage.Connecting);
				DevTools devTools = _app.DevTools;
				int port = _app.SingleplayerServer.Port;
				devTools.Info("Connecting to singleplayer server on port " + port);
				Connection = new ConnectionToServer(_app.Engine, "127.0.0.1", _app.SingleplayerServer.Port, OnConnected, OnDisconnectedWithError);
			}
		}
	}

	private void OnConnected(Exception exception)
	{
		if (exception != null)
		{
			string hostname = Connection.Hostname;
			int port = Connection.Port;
			Logger.Error(exception, "Failed to connect");
			Connection = null;
			if (_app.SingleplayerServer != null)
			{
				_app.Disconnection.SetReason(_app.SingleplayerServer.ShutdownMessage);
			}
			if (_app.Disconnection.Reason == null && exception is SocketException)
			{
				_app.Disconnection.SetReason(_app.Interface.GetText("ui.disconnection.errors.noConnectectionEstablished"));
			}
			_app.Disconnection.Open(exception.Message, hostname, port);
		}
		else
		{
			SetStage(GameLoadingStage.Loading);
			_app.InGame.CreateInstance(Connection);
		}
	}

	private void OnDisconnectedWithError(Exception exception)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		Debug.Assert(Connection != null || _app.SingleplayerWorldName != null);
		if (LoadingStage != GameLoadingStage.Aborted)
		{
			Logger.Info("Disconnected during loading with error:");
			Logger.Error<Exception>(exception);
			SetStage(GameLoadingStage.Aborted);
			if (_app.SingleplayerWorldName != null)
			{
				_app.Disconnection.Open(exception.Message);
			}
			else
			{
				_app.Disconnection.Open(exception.Message, Connection.Hostname, Connection.Port);
			}
		}
	}

	public void Abort()
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		Debug.Assert(ThreadHelper.IsMainThread());
		if (LoadingStage != GameLoadingStage.Complete && _app.Stage == App.AppStage.GameLoading)
		{
			if (Connection != null)
			{
				Connection.OnDisconnected = null;
				Connection.SendPacketImmediate((ProtoPacket)new Disconnect("Player abort", (DisconnectType)0));
				Connection.Close();
				Connection = null;
			}
			if (_app.SingleplayerServer != null)
			{
				_app.SingleplayerServer.Close();
				_app.OnSinglePlayerServerShuttingDown();
			}
			_app.InGame.DisposeAndClearInstance();
			SetStage(GameLoadingStage.Aborted);
			_app.MainMenu.Open(_app.MainMenu.CurrentPage);
		}
	}
}
