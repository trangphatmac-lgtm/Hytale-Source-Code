#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Hypixel.ProtoPlus;
using HytaleClient.Application;
using HytaleClient.Interface.Messages;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using SDL2;
using Utf8Json;

namespace HytaleClient.InGame;

internal class Chat
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly GameInstance _gameInstance;

	private readonly List<FormattedMessage> _beforePlayingMessages = new List<FormattedMessage>();

	private const double DiagnosticMessageRate = 5.0;

	private readonly Stopwatch _lastMessage = Stopwatch.StartNew();

	private readonly Stopwatch _lastSuccessfulMessage = Stopwatch.StartNew();

	private double _diagnosticMessageScore = 5.0;

	private uint _skippedDiagnosticMessageCount;

	private bool _hasLoggedDiagnosticMessage = true;

	public bool IsOpen { get; private set; }

	public Chat(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
	}

	public void TryOpen(SDL_Keycode? keyCodeTrigger = null, bool isCommand = false)
	{
		Debug.Assert(!IsOpen);
		if (_gameInstance.App.InGame.CurrentOverlay == AppInGame.InGameOverlay.None && !_gameInstance.App.Interface.InGameView.HasFocusedElement && _gameInstance.App.Interface.Desktop.GetInteractiveLayer() == _gameInstance.App.Interface.InGameView)
		{
			IsOpen = true;
			_gameInstance.App.Interface.InGameView.OnChatOpened(keyCodeTrigger, isCommand);
			_gameInstance.App.InGame.OnChatOpenChanged();
		}
	}

	public void Close()
	{
		Debug.Assert(IsOpen);
		IsOpen = false;
		_gameInstance.App.Interface.InGameView.OnChatClosed();
		_gameInstance.App.InGame.OnChatOpenChanged();
	}

	public void Log(string message)
	{
		if (!ThreadHelper.IsMainThread())
		{
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				Log(message);
			});
		}
		else
		{
			AddMessage(message, "fff");
		}
	}

	public void Error(string message)
	{
		if (!ThreadHelper.IsMainThread())
		{
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				Error(message);
			});
			return;
		}
		Logger.Info(message);
		if (!_gameInstance.App.Settings.DiagnosticMode)
		{
			return;
		}
		long num = _lastMessage.ElapsedMilliseconds / 1000;
		_lastMessage.Restart();
		_diagnosticMessageScore += (double)num * 5.0;
		if (_diagnosticMessageScore > 5.0)
		{
			_diagnosticMessageScore = 5.0;
		}
		else if (_diagnosticMessageScore < -5.0)
		{
			_diagnosticMessageScore = -5.0;
		}
		if (_diagnosticMessageScore < 1.0)
		{
			if (_hasLoggedDiagnosticMessage)
			{
				AddMessage("[Warning] Diagnostic message rate limit reached!", "f55");
			}
			_hasLoggedDiagnosticMessage = false;
		}
		else
		{
			_hasLoggedDiagnosticMessage = true;
			_lastSuccessfulMessage.Restart();
			if (_skippedDiagnosticMessageCount != 0)
			{
				AddMessage($"[Warning] {_skippedDiagnosticMessageCount} skipped diagnostic messages!", "f55");
			}
			_skippedDiagnosticMessageCount = 0u;
			AddMessage(message, "f55");
		}
		_diagnosticMessageScore -= 1.0;
	}

	public void NotifyPlayerOfSkippedDiagnosticMessages()
	{
		if (_gameInstance.App.Settings.DiagnosticMode && _lastSuccessfulMessage.ElapsedMilliseconds >= 10000)
		{
			_lastSuccessfulMessage.Restart();
			if (_skippedDiagnosticMessageCount != 0)
			{
				AddMessage($"[Warning] {_skippedDiagnosticMessageCount} skipped diagnostic messages!", "f55");
			}
			_skippedDiagnosticMessageCount = 0u;
		}
	}

	public void HandleBeforePlayingMessages()
	{
		foreach (FormattedMessage beforePlayingMessage in _beforePlayingMessages)
		{
			_gameInstance.App.Interface.InGameView.ChatComponent.OnReceiveMessage(beforePlayingMessage);
		}
		_beforePlayingMessages.Clear();
	}

	private void AddMessage(string messageId, string color)
	{
		AddMessage(new FormattedMessage
		{
			MessageId = messageId,
			Color = color
		});
	}

	private void AddMessage(FormattedMessage formattedMessage)
	{
		if (_gameInstance.IsPlaying)
		{
			_gameInstance.App.Interface.InGameView.ChatComponent.OnReceiveMessage(formattedMessage);
		}
		else
		{
			_beforePlayingMessages.Add(formattedMessage);
		}
	}

	public void AddJsonMessage(string message)
	{
		FormattedMessage formattedMessage;
		try
		{
			formattedMessage = JsonSerializer.Deserialize<FormattedMessage>(message);
		}
		catch (Exception ex)
		{
			_gameInstance.Chat.Log("Failed to parse chat message!");
			Logger.Error<Exception>(ex);
			return;
		}
		AddMessage(formattedMessage);
	}

	public void AddBsonMessage(sbyte[] encodedMessage)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		FormattedMessage formattedMessage;
		try
		{
			using MemoryStream memoryStream = new MemoryStream((byte[])(object)encodedMessage);
			BsonDataReader val = new BsonDataReader((Stream)memoryStream);
			try
			{
				JsonSerializer val2 = JsonSerializer.Create();
				formattedMessage = val2.Deserialize<FormattedMessage>((JsonReader)(object)val);
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			Error("Failed to chat parse message!");
			Logger.Error<Exception>(ex);
			return;
		}
		AddMessage(formattedMessage);
	}

	public void SendMessage(string message)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		_gameInstance.Connection.SendPacket((ProtoPacket)new ChatMessage(message));
	}

	public void SendCommand(string command, params object[] args)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		if (args.Length != 0)
		{
			command = command + " " + string.Join(" ", args);
		}
		_gameInstance.Connection.SendPacket((ProtoPacket)new ChatMessage("/" + command));
	}
}
