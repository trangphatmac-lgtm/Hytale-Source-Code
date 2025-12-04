#define DEBUG
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Net.WebSockets.Managed;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Application.Services;

public class WebSocket
{
	private const int ReceiveBufferSize = 1024;

	private const int SendBufferSize = 16;

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly CancellationTokenSource _cancellationTokenSource;

	private readonly CancellationToken _threadCancellationToken;

	private readonly string _uri;

	private readonly Action<WebSocket> _onConnected;

	private readonly Action<byte[], WebSocket> _onMessage;

	private readonly Action<Exception, WebSocket> _onDisconnected;

	private System.Net.WebSockets.WebSocket _clientWebSocket;

	private Thread _thread;

	public WebSocket(string uri, Action<WebSocket> onConnected, Action<byte[], WebSocket> onMessage, Action<Exception, WebSocket> onDisconnected)
	{
		_onConnected = onConnected;
		_onMessage = onMessage;
		_onDisconnected = onDisconnected;
		_uri = uri;
		_cancellationTokenSource = new CancellationTokenSource();
		_threadCancellationToken = _cancellationTokenSource.Token;
	}

	public void Close()
	{
		if (_clientWebSocket != null && _clientWebSocket.State == WebSocketState.Open)
		{
			try
			{
				_clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed the connection", CancellationToken.None).GetAwaiter().GetResult();
				Task.Run(delegate
				{
					_onDisconnected?.Invoke(null, this);
				});
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to close WebSocket:");
			}
		}
		_cancellationTokenSource.Cancel();
		_cancellationTokenSource.Dispose();
		_thread?.Join();
		_clientWebSocket?.Dispose();
	}

	public void SendMessageAsync(byte[] bytes)
	{
		if (_clientWebSocket == null || _clientWebSocket.State != WebSocketState.Open)
		{
			throw new Exception("Connection is not open.");
		}
		_clientWebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, endOfMessage: true, _threadCancellationToken).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted || t.IsCanceled)
			{
				if (t.Exception != null)
				{
					Logger.Error((Exception)t.Exception, "Unable to send message via websocket");
				}
				else
				{
					Logger.Error("Unable to send message via websocket because operation was canceled");
				}
			}
		});
	}

	public bool IsAlive()
	{
		return _clientWebSocket != null && _clientWebSocket.State == WebSocketState.Open;
	}

	public void ConnectAsync()
	{
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
		_clientWebSocket = SystemClientWebSocket.CreateClientWebSocket();
		InitializeWebsocketOptions();
		try
		{
			SystemClientWebSocket.ConnectAsync(_clientWebSocket, new Uri(_uri), CancellationToken.None).ContinueWith(delegate(Task t)
			{
				if (t.IsFaulted || _clientWebSocket.State != WebSocketState.Open)
				{
					Logger.Error("Unable to connect");
					if (t.Exception != null)
					{
						_onDisconnected?.Invoke(t.Exception, this);
					}
					else
					{
						_onDisconnected?.Invoke(null, this);
					}
				}
				else
				{
					OnOpen();
				}
			});
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Error while connecting");
		}
	}

	private void InitializeWebsocketOptions()
	{
		if (SystemClientWebSocket.ManagedWebSocketRequired)
		{
			System.Net.WebSockets.WebSocket clientWebSocket = _clientWebSocket;
			ClientWebSocket val = (ClientWebSocket)(object)((clientWebSocket is ClientWebSocket) ? clientWebSocket : null);
			Logger.Info("Using managed websocket");
			val.Options.KeepAliveInterval = TimeSpan.FromSeconds(1.0);
			val.Options.SetRequestHeader("hytale-services-auth-version", "f4c63561b2d2f5120b4c81ad1b8544e396088277d88f650aea892b6f0cb113f");
			val.Options.SetRequestHeader("hytale-services-auth-compiletime", 1643968234458L.ToString());
			val.Options.SetRequestHeader("hytale-services-client-version", "9d8ae0bf16cd64d040f9b8199273a8d174fe9809e896daca052b49e4ee79f");
			val.Options.SetRequestHeader("hytale-services-client-compiletime", 1551618657161L.ToString());
		}
		else
		{
			ClientWebSocket clientWebSocket2 = _clientWebSocket as ClientWebSocket;
			clientWebSocket2.Options.KeepAliveInterval = TimeSpan.FromSeconds(1.0);
			clientWebSocket2.Options.SetRequestHeader("hytale-services-auth-version", "f4c63561b2d2f5120b4c81ad1b8544e396088277d88f650aea892b6f0cb113f");
			clientWebSocket2.Options.SetRequestHeader("hytale-services-auth-compiletime", 1643968234458L.ToString());
			clientWebSocket2.Options.SetRequestHeader("hytale-services-client-version", "9d8ae0bf16cd64d040f9b8199273a8d174fe9809e896daca052b49e4ee79f");
			clientWebSocket2.Options.SetRequestHeader("hytale-services-client-compiletime", 1551618657161L.ToString());
		}
	}

	private void OnOpen()
	{
		if (_onConnected != null)
		{
			Task.Run(delegate
			{
				_onConnected(this);
			});
		}
		StartListen();
	}

	private void StartListen()
	{
		_thread = new Thread(BackgroundThreadStart)
		{
			Name = "WebsocketThread",
			IsBackground = true
		};
		_thread.Start();
	}

	private void BackgroundThreadStart()
	{
		Debug.Assert(ThreadHelper.IsOnThread(_thread));
		Logger.Info("Websocket starts listening");
		ArraySegment<byte> buffer = System.Net.WebSockets.WebSocket.CreateClientBuffer(1024, 16);
		using (MemoryStream memoryStream = new MemoryStream())
		{
			while (true)
			{
				CancellationToken threadCancellationToken = _threadCancellationToken;
				if (threadCancellationToken.IsCancellationRequested)
				{
					break;
				}
				if (_clientWebSocket.CloseStatus.HasValue || _clientWebSocket.State != WebSocketState.Open)
				{
					Exception arg = ((_clientWebSocket.CloseStatus.HasValue && _clientWebSocket.CloseStatus.Value == WebSocketCloseStatus.NormalClosure) ? null : new Exception($"Closed with CloseStatus {_clientWebSocket.CloseStatus}, reason {_clientWebSocket.CloseStatusDescription}, current state : {_clientWebSocket.State}"));
					_onDisconnected?.Invoke(arg, this);
					break;
				}
				try
				{
					WebSocketReceiveResult result;
					do
					{
						result = _clientWebSocket.ReceiveAsync(buffer, _threadCancellationToken).GetAwaiter().GetResult();
						memoryStream.Write(buffer.Array, buffer.Offset, result.Count);
					}
					while (!result.EndOfMessage);
					memoryStream.Seek(0L, SeekOrigin.Begin);
					switch (result.MessageType)
					{
					case WebSocketMessageType.Close:
						Logger.Info("Close message received");
						_clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
						break;
					case WebSocketMessageType.Binary:
						_onMessage(memoryStream.ToArray(), this);
						break;
					default:
						Logger.Error<WebSocketMessageType, string>("Received illegal message {0} : {1}", result.MessageType, Encoding.UTF8.GetString(memoryStream.ToArray()));
						break;
					}
				}
				catch (OperationCanceledException)
				{
					Logger.Info("Receive had been canceled");
					break;
				}
				catch (Exception ex2)
				{
					Logger.Error(ex2, "Exception during reception");
					_onDisconnected?.Invoke(ex2, this);
					break;
				}
				memoryStream.SetLength(0L);
			}
		}
		Logger.Info("Websocket stopped listening");
	}
}
