#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Hypixel.ProtoPlus;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Networking;
using HytaleClient.Interface.Messages;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json;

namespace HytaleClient.AssetEditor;

internal class AssetEditorAppMainMenu
{
	public enum ConnectionStages
	{
		Connecting,
		Authenticating
	}

	public class Server
	{
		public string Name;

		public string Hostname;

		public int Port;

		public DateTime DateLastJoined;
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private static readonly Comparison<Server> ServerSortComparison = (Server a, Server b) => b.DateLastJoined.CompareTo(a.DateLastJoined);

	private readonly AssetEditorApp _app;

	private AssetEditorPacketHandler _packetHandler;

	private ConnectionToServer _connection;

	public string ServerDisconnectReason;

	private string _previousServerHostname;

	private int _previousServerPort;

	public string AssetPathToOpen;

	public AssetIdReference AssetIdToOpen;

	private List<Server> _servers = new List<Server>();

	public bool IsConnectingToServer => _connection != null;

	public ConnectionToServer Connection => _connection;

	public string ConnectionErrorMessage { get; private set; }

	public bool DisplayDisconnectPopup { get; private set; }

	public ConnectionStages ConnectionStage { get; private set; }

	public IReadOnlyList<Server> Servers => _servers;

	private string ServersFilePath => Path.Combine(Paths.UserData, "Servers.json");

	public AssetEditorAppMainMenu(AssetEditorApp app)
	{
		_app = app;
		LoadServers();
	}

	private void LoadServers()
	{
		try
		{
			string text = File.ReadAllText(ServersFilePath, Encoding.UTF8);
			_servers = JsonConvert.DeserializeObject<List<Server>>(text);
		}
		catch (FileNotFoundException)
		{
			_servers.Clear();
		}
		catch (Exception ex2)
		{
			Logger.Error(ex2, "Failed to load server list.");
			_servers.Clear();
		}
		_servers.Sort(ServerSortComparison);
		if (_app.Stage == AssetEditorApp.AppStage.MainMenu)
		{
			_app.Interface.MainMenuView.BuildServerList();
		}
	}

	private bool TrySaveServers()
	{
		string serversFilePath = ServersFilePath;
		try
		{
			string contents = JsonConvert.SerializeObject((object)Servers, (Formatting)1);
			File.WriteAllText(serversFilePath + ".new", contents);
			if (File.Exists(serversFilePath))
			{
				File.Replace(serversFilePath + ".new", serversFilePath, serversFilePath + ".bak");
			}
			else
			{
				File.Move(serversFilePath + ".new", serversFilePath);
			}
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to save server list to " + serversFilePath);
			return false;
		}
		return true;
	}

	public void Open()
	{
		_app.SetStage(AssetEditorApp.AppStage.MainMenu);
	}

	public void OpenWithDisconnectPopup(string hostname, int port)
	{
		DisplayDisconnectPopup = true;
		_previousServerHostname = hostname;
		_previousServerPort = port;
		_app.Interface.MainMenuView.UpdateDisconnectPopup();
		_app.SetStage(AssetEditorApp.AppStage.MainMenu);
	}

	public void CloseDisconnectPopup()
	{
		DisplayDisconnectPopup = false;
		_app.Interface.MainMenuView.UpdateDisconnectPopup();
	}

	public void Reconnect()
	{
		ConnectToServer(_previousServerHostname, _previousServerPort);
	}

	public void ConnectToServer(string host, int port)
	{
		Debug.Assert(_connection == null);
		Logger.Info<string, int>("Connecting to server {0}:{1}", host, port);
		ConnectionErrorMessage = null;
		ConnectionStage = ConnectionStages.Connecting;
		_connection = new ConnectionToServer(_app.Engine, host, port, OnConnected, OnDisconnected);
		_app.Interface.MainMenuView.UpdateConnectionStatus();
	}

	public void ConnectToServer(int index)
	{
		Server server = Servers[index];
		server.DateLastJoined = DateTime.Now;
		TrySaveServers();
		ConnectToServer(server.Hostname, server.Port);
	}

	private void OnConnected(Exception exception)
	{
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Expected O, but got Unknown
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Invalid comparison between Unknown and I4
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Expected O, but got Unknown
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Expected O, but got Unknown
		if (exception != null)
		{
			ConnectionErrorMessage = "ui.assetEditor.mainMenu.connection.error.failedToEstablishConnection";
			_connection.OnDisconnected = null;
			_connection = null;
			_packetHandler?.Dispose();
			_packetHandler = null;
			_app.Interface.MainMenuView.UpdateConnectionStatus();
			return;
		}
		Logger.Info("Connection established!");
		ConnectionStage = ConnectionStages.Authenticating;
		_packetHandler = new AssetEditorPacketHandler(_app, _connection);
		_connection.OnPacketReceived = _packetHandler.Receive;
		ConnectionMode val = (ConnectionMode)6;
		if (_app.AuthManager.Settings.IsInsecure)
		{
			val = (ConnectionMode)7;
		}
		_connection.SendPacket((ProtoPacket)new Connect("f4c63561b2d2f5120b4c81ad1b8544e396088277d88f650aea892b6f0cb113f", 1643968234458L, val, _app.Settings.Language ?? Language.SystemLanguage));
		if ((int)val == 7)
		{
			_connection.SendPacket((ProtoPacket)new SetUsername(_app.AuthManager.Settings.Username));
		}
		else
		{
			if (_app.AuthManager.CertPathBytes == null)
			{
				throw new Exception("Attempted to execute an online-mode handshake while not authenticated!");
			}
			Auth1 packet = new Auth1((sbyte[])(object)_app.AuthManager.CertPathBytes);
			_connection.SendPacket((ProtoPacket)(object)packet);
		}
		_app.Interface.MainMenuView.UpdateConnectionStatus();
	}

	public void OnAuthenticated()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		Logger.Info("User is fully authenticated!");
		ConnectionToServer connection = _connection;
		AssetEditorPacketHandler packetHandler = _packetHandler;
		_connection.OnDisconnected = null;
		_connection = null;
		_packetHandler = null;
		string assetPathToOpen = AssetPathToOpen;
		AssetIdReference assetIdToOpen = AssetIdToOpen;
		_app.Interface.MainMenuView.UpdateConnectionStatus();
		_app.Editor.OpenAssetEditor(connection, packetHandler);
		if (assetPathToOpen != null)
		{
			_app.Editor.OpenAsset(assetPathToOpen);
		}
		else if (assetIdToOpen.Type != null)
		{
			_app.Editor.OpenAsset(assetIdToOpen);
		}
	}

	private void OnDisconnected(Exception exception)
	{
		if (_connection != null)
		{
			Logger.Error(exception, "Disconnected from server");
			_connection.OnDisconnected = null;
			_connection = null;
			_packetHandler?.Dispose();
			_packetHandler = null;
			if (ServerDisconnectReason != null)
			{
				ConnectionErrorMessage = ServerDisconnectReason;
			}
			else if (exception != null)
			{
				ConnectionErrorMessage = "ui.assetEditor.mainMenu.connection.error.failedAuthentication";
			}
			_app.Interface.MainMenuView.UpdateConnectionStatus();
		}
	}

	public void CancelConnection()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		if (_connection != null)
		{
			_connection.OnDisconnected = null;
			_connection.SendPacketImmediate((ProtoPacket)new Disconnect("Player abort", (DisconnectType)0));
			_connection.Close();
			_connection = null;
			_packetHandler?.Dispose();
			_packetHandler = null;
			_app.Interface.MainMenuView.UpdateConnectionStatus();
		}
	}

	public void CleanUp()
	{
		_servers.Sort(ServerSortComparison);
		AssetIdToOpen = AssetIdReference.None;
		AssetPathToOpen = null;
		ServerDisconnectReason = null;
		DisplayDisconnectPopup = false;
		_previousServerHostname = null;
		_previousServerPort = 0;
		ConnectionErrorMessage = null;
		CancelConnection();
	}

	private void SaveServers()
	{
		if (!TrySaveServers())
		{
			_app.Interface.Notifications.AddNotification((AssetEditorPopupNotificationType)2, FormattedMessage.FromMessageId("ui.assetEditor.mainMenu.errors.failedToSaveServerList"));
		}
	}

	public void AddServer(Server server)
	{
		_servers.Add(server);
		_servers.Sort(ServerSortComparison);
		SaveServers();
		_app.Interface.MainMenuView.BuildServerList();
	}

	public void UpdateServer(int index, string name, string hostname, int port)
	{
		Server server = Servers[index];
		server.Name = name;
		server.Hostname = hostname;
		server.Port = port;
		_servers.Sort(ServerSortComparison);
		SaveServers();
		_app.Interface.MainMenuView.BuildServerList();
	}

	public void RemoveServer(int index)
	{
		_servers.RemoveAt(index);
		SaveServers();
		_app.Interface.MainMenuView.BuildServerList();
	}
}
