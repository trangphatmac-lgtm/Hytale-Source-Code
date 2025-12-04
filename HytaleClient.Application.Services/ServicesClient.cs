using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hypixel.ProtoPlus;
using HytaleClient.Auth.Proto;
using HytaleClient.AuthHandshake.Proto;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Application.Services;

internal class ServicesClient
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly App _app;

	private readonly string _host;

	private readonly int _port;

	private readonly string _uri;

	private WebSocket _webSocket;

	private bool _connecting;

	private Action _onDisconnected;

	private CancellationTokenSource _connectionCancellationTokenSource;

	private ServicesPacketHandler _packetHandler;

	private int _reconnectBackoff;

	private bool _connectionClosed;

	public ServicesAuthState AuthState { get; private set; }

	public ServicesClient(App app, string host, int port, bool secure, string path, ServicesPacketHandler handler, Action onDisconnected)
	{
		_app = app;
		_host = host;
		_port = port;
		_onDisconnected = onDisconnected;
		_packetHandler = handler;
		string text = (secure ? "wss" : "ws");
		_uri = $"{text}://{_host}:{_port}/{path}";
		if (OptionsHelper.DisableServices)
		{
			Logger.Info("Refusing to connect to services since they have been disabled");
		}
		else if (!_app.AuthManager.Settings.IsInsecure)
		{
			Connect();
		}
		else
		{
			Logger.Info("Refusing to connect to services while IsInsecure is true!");
		}
	}

	private void Connect()
	{
		_connecting = true;
		_app.Engine.RunOnMainThread(_app.Engine, delegate
		{
			_app.Interface.OnServicesStateChanged(HytaleServices.ServiceState.Connecting);
		}, allowCallFromMainThread: true);
		AuthState = new ServicesAuthState(_app.AuthManager, this);
		CancellationTokenSource cancellation = (_connectionCancellationTokenSource = new CancellationTokenSource());
		_webSocket = new WebSocket(_uri, delegate
		{
			_reconnectBackoff = 0;
			_connecting = false;
			_app.Engine.RunOnMainThread(_app.Engine, delegate
			{
				_app.Interface.OnServicesStateChanged(HytaleServices.ServiceState.Authenticating);
			});
			Logger.Info("Connected successfully to services websocket at {0}", _uri);
		}, delegate(byte[] bytes, WebSocket socket)
		{
			try
			{
				ProtoBinaryReader val = ProtoBinaryReader.Create(bytes);
				try
				{
					ProtoPacket val2 = (AuthState.Authed ? PacketReader.ReadPacket(val) : PacketReader.ReadPacket(val));
					Logger.Info<ProtoPacket>("Received packet from websocket channel: {0}", val2);
					_packetHandler.Receive(val2, this);
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			catch
			{
				Logger.Info<byte[], WebSocket>("Failed to read packet from websocket: {0}, {0}", bytes, socket);
				Close();
			}
		}, delegate(Exception exception, WebSocket socket)
		{
			_connecting = false;
			_onDisconnected();
			if (!_connectionClosed)
			{
				_reconnectBackoff++;
				if (_reconnectBackoff > 10)
				{
					_reconnectBackoff = 10;
				}
				Logger.Info<string, int>("Delaying reconnect to {0} by {1} seconds...", _uri, _reconnectBackoff - 1);
				Task.Delay(1000 * (_reconnectBackoff - 1), cancellation.Token).ContinueWith(delegate(Task task)
				{
					if (!task.IsCanceled && !_connectionClosed)
					{
						Logger.Info("Reconnecting to {0}.", _uri);
						Connect();
					}
				}, cancellation.Token);
				if (exception != null)
				{
					Logger.Error(exception, "Got exception from websocket with message {0} and exception:", new object[1] { exception.Message });
				}
				else
				{
					Logger.Info("Got socket closed");
				}
			}
		});
		Logger.Info<ServicesEndpoint, string>("Connecting to websocket at {0}: {1}", OptionsHelper.Endpoint, _uri);
		_webSocket.ConnectAsync();
	}

	public bool IsConnected()
	{
		return _webSocket != null && _webSocket.IsAlive();
	}

	public void Write(ProtoPacket packet)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		if (packet == null)
		{
			throw new ArgumentException("Packet can't be null");
		}
		if (_connecting || _webSocket == null || !_webSocket.IsAlive())
		{
			Logger.Warn<ProtoPacket, bool, bool?>("Attempted to write packet out {0} while connecting or not connected: {1} and websocket state: {2}", packet, _connecting, _webSocket?.IsAlive());
			return;
		}
		byte[] array;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			ProtoBinaryWriter val = new ProtoBinaryWriter((Stream)memoryStream);
			try
			{
				packet.WritePacket(val);
				array = memoryStream.ToArray();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		try
		{
			_webSocket.SendMessageAsync(array);
			Logger.Info<ProtoPacket, WebSocket, int>("Sent packet {0} to {1} with payload length {2}", packet, _webSocket, array.Length);
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to send packet {0} to {1} with payload length {2}", new object[3] { packet, _webSocket, array.Length });
		}
	}

	public void Close()
	{
		Logger.Info<ServicesEndpoint, string>("Closing websocket for {0}: {1}", OptionsHelper.Endpoint, _uri);
		_connectionClosed = true;
		_packetHandler?.Dispose();
		_connectionCancellationTokenSource?.Cancel();
		if (_connecting || IsConnected())
		{
			_webSocket.Close();
		}
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}, {4}: {5}, {6}: {7}, {8}: {9}", "_host", _host, "_port", _port, "_webSocket", _webSocket, "_connecting", _connecting, "AuthState", AuthState);
	}
}
