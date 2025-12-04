using System;
using System.Collections.Concurrent;
using HytaleClient.Interface.DevTools;
using NLog;

namespace HytaleClient.Application;

internal class DevTools
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly App _app;

	public volatile bool IsDiagnosticsModeEnabled;

	private readonly ConcurrentQueue<Tuple<DevToolsOverlay.MessageType, string>> _messageQueue = new ConcurrentQueue<Tuple<DevToolsOverlay.MessageType, string>>();

	public bool IsOpen { get; private set; }

	public DevTools(App app)
	{
		_app = app;
		IsDiagnosticsModeEnabled = _app.Settings.DiagnosticMode;
	}

	public void Info(string message)
	{
		Logger.Info(message);
		if (IsDiagnosticsModeEnabled)
		{
			_messageQueue.Enqueue(Tuple.Create(DevToolsOverlay.MessageType.Info, message));
		}
	}

	public void Error(string message)
	{
		Logger.Error(message);
		if (IsDiagnosticsModeEnabled)
		{
			_messageQueue.Enqueue(Tuple.Create(DevToolsOverlay.MessageType.Error, message));
		}
	}

	public void Warn(string message)
	{
		Logger.Warn(message);
		if (IsDiagnosticsModeEnabled)
		{
			_messageQueue.Enqueue(Tuple.Create(DevToolsOverlay.MessageType.Warning, message));
		}
	}

	public void HandleMessageQueue()
	{
		if (_messageQueue.IsEmpty)
		{
			return;
		}
		DevToolsOverlay devTools = _app.Interface.DevToolsLayer.DevTools;
		DevToolsNotificationPanel devToolsNotificationPanel = _app.Interface.DevToolsNotificationPanel;
		int num = 0;
		int num2 = 0;
		Tuple<DevToolsOverlay.MessageType, string> result;
		while (_messageQueue.TryDequeue(out result))
		{
			if (IsDiagnosticsModeEnabled)
			{
				devTools.AddConsoleMessage(result.Item1, result.Item2);
				switch (result.Item1)
				{
				case DevToolsOverlay.MessageType.Warning:
					num++;
					break;
				case DevToolsOverlay.MessageType.Error:
					num2++;
					break;
				}
			}
		}
		if (!IsOpen && (_app.Stage != App.AppStage.InGame || _app.InGame.IsHudVisible) && (num > 0 || num2 > 0))
		{
			if (num2 > 0)
			{
				devToolsNotificationPanel.AddUnreadError(num2);
			}
			if (num > 0)
			{
				devToolsNotificationPanel.AddUnreadWarning(num);
			}
			if (devToolsNotificationPanel.IsMounted)
			{
				devToolsNotificationPanel.Layout();
			}
		}
		devTools.LayoutLog();
	}

	public void Open()
	{
		IsOpen = true;
		_app.Interface.Desktop.SetLayer(5, _app.Interface.DevToolsLayer);
		_app.Interface.DevToolsNotificationPanel.ClearUnread();
		if (_app.Stage == App.AppStage.InGame)
		{
			_app.InGame.UpdateInputStates();
		}
	}

	public void Close()
	{
		IsOpen = false;
		_app.Interface.Desktop.ClearLayer(5);
		if (_app.Stage == App.AppStage.InGame)
		{
			_app.InGame.UpdateInputStates();
		}
	}

	public void ClearNotifications()
	{
		_app.Interface.DevToolsNotificationPanel.ClearUnread();
	}
}
