#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HytaleClient.Application;
using HytaleClient.Application.Services;
using HytaleClient.Auth.Proto.Protocol;
using HytaleClient.Core;
using HytaleClient.Interface.DevTools;
using HytaleClient.Interface.InGame;
using HytaleClient.Interface.MainMenu;
using HytaleClient.Interface.Services;
using HytaleClient.Interface.Settings;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Interface;

internal class Interface : BaseInterface
{
	private class InterfaceEventHandler
	{
		public Disposable DisposeGate;

		public Delegate Callback;
	}

	private class EngineEventHandler
	{
		public Delegate Callback;
	}

	public new static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const float ReferenceHeight = 1080f;

	public readonly App App;

	public readonly string MainMenuBackgroundImagePath;

	public readonly StartupView StartupView;

	public readonly MainMenuView MainMenuView;

	public readonly GameLoadingView GameLoadingView;

	public readonly DisconnectionView DisconnectionView;

	public readonly InGameView InGameView;

	public readonly CustomUIProvider InGameCustomUIProvider;

	public readonly DevToolsLayer DevToolsLayer;

	public readonly DevToolsNotificationPanel DevToolsNotificationPanel;

	public readonly ModalDialog ModalDialog;

	private InterfaceComponent _currentView;

	public readonly QueueStatus QueueStatus;

	public readonly SettingsComponent SettingsComponent;

	public readonly SocialBar SocialBar;

	private readonly Dictionary<string, InterfaceEventHandler> _handlersForInterfaceEvents = new Dictionary<string, InterfaceEventHandler>();

	private readonly Dictionary<string, EngineEventHandler> _handlersForEngineEvents = new Dictionary<string, EngineEventHandler>();

	public string QueueTicketName { get; private set; }

	public string QueueTicketStatus { get; private set; }

	public HytaleServices.ServiceState ServiceState { get; private set; } = HytaleServices.ServiceState.Disconnected;


	public Interface(App app, string resourcePath, bool isDevModeEnabled)
		: base(app.Engine, app.Fonts, app.CoUIManager, resourcePath, isDevModeEnabled)
	{
		App = app;
		RegisterServicesLifecycleEvents();
		Regex bgRegex = new Regex("^Zone[0-9]+\\.png$");
		string[] array = (from f in Directory.GetFiles(Path.Combine(Paths.GameData, "Backgrounds"))
			where bgRegex.IsMatch(Path.GetFileName(f))
			select f).ToArray();
		MainMenuBackgroundImagePath = array[new Random().Next(array.Length)];
		ModalDialog = new ModalDialog(this);
		SettingsComponent = new SettingsComponent(this, null);
		StartupView = new StartupView(this);
		MainMenuView = new MainMenuView(this);
		GameLoadingView = new GameLoadingView(this);
		DisconnectionView = new DisconnectionView(this);
		InGameCustomUIProvider = new CustomUIProvider(this);
		InGameView = new InGameView(this);
		DevToolsLayer = new DevToolsLayer(this);
		DevToolsNotificationPanel = new DevToolsNotificationPanel(this);
		SocialBar = new SocialBar(this);
		QueueStatus = new QueueStatus(this);
	}

	protected override void DoDispose()
	{
		InGameCustomUIProvider.Dispose();
		base.DoDispose();
	}

	protected override void Build()
	{
		ModalDialog.Build();
		SettingsComponent.Build();
		QueueStatus.Build();
		SocialBar.Build();
		MainMenuView.Build();
		GameLoadingView.Build();
		DisconnectionView.Build();
		InGameView.Build();
		DevToolsLayer.Build();
		DevToolsNotificationPanel.Build();
	}

	protected override void SetDrawOutlines(bool draw)
	{
		base.SetDrawOutlines(draw);
		InGameView.CustomHud.OnChangeDrawOutlines();
		InGameView.CustomPage.OnChangeDrawOutlines();
	}

	protected override float GetScale()
	{
		return (float)Engine.Window.Viewport.Height / 1080f;
	}

	protected override void LoadTextures(bool use2x)
	{
		base.LoadTextures(use2x);
		if (InGameView.IsMounted)
		{
			InGameCustomUIProvider.LoadTextures(use2x);
		}
	}

	public new void OnWindowSizeChanged()
	{
		base.OnWindowSizeChanged();
		SettingsComponent.OnWindowSizeChanged();
	}

	public void OnAppStageChanged()
	{
		InterfaceComponent interfaceComponent = App.Stage switch
		{
			App.AppStage.Startup => StartupView, 
			App.AppStage.MainMenu => MainMenuView, 
			App.AppStage.GameLoading => GameLoadingView, 
			App.AppStage.InGame => InGameView, 
			App.AppStage.Disconnection => DisconnectionView, 
			_ => throw new NotSupportedException(), 
		};
		if (_currentView != interfaceComponent)
		{
			DevToolsNotificationPanel.Parent?.Remove(DevToolsNotificationPanel);
			interfaceComponent.Add(DevToolsNotificationPanel);
			Desktop.ClearAllLayers();
			Desktop.SetLayer(0, interfaceComponent);
			_currentView = interfaceComponent;
			QueueStatus.Parent?.Remove(QueueStatus);
			_currentView.Add(QueueStatus);
			_currentView.Layout();
		}
	}

	public void OnServicesInitialized()
	{
		ServiceState = HytaleServices.ServiceState.Connected;
		SocialBar.UpdateServiceInformation();
		MainMenuView.MinigamesPage.OnGamesUpdated();
		MainMenuView.SharedSinglePlayerPage.OnWorldsUpdated();
		QueueStatus.Update();
	}

	public void OnServicesStateChanged(HytaleServices.ServiceState state)
	{
		ServiceState = state;
		SocialBar.UpdateServiceInformation();
	}

	public void OnServicesUserStateChanged(string uuid, ClientUserState state)
	{
		SocialBar.UpdateServiceInformation();
	}

	public void OnServicesFriendsAdded(string uuid)
	{
		SocialBar.UpdateServiceInformation();
	}

	public void OnServicesFriendsRemoved(string uuid)
	{
		SocialBar.UpdateServiceInformation();
	}

	public void OnServicesQueueError(string causeId)
	{
		OnServicesQueueLeft();
		if (ModalDialog.IsMounted)
		{
			Logger.Warn("Skipped modal dialog for queue error {0} since another modal was already opened.", causeId);
			return;
		}
		Debug.Assert(MainMenuView.IsMounted);
		ModalDialog.Setup(new ModalDialog.DialogSetup
		{
			Title = "ui.socialMenu.queue",
			Text = "ui.socialMenu.queueErrors." + causeId,
			Cancellable = false
		});
		Desktop.SetLayer(4, ModalDialog);
	}

	public void OnServicesQueueLeft()
	{
		QueueTicketName = null;
		QueueTicketStatus = null;
		QueueStatus.Update();
	}

	public void OnServicesQueueJoined(string joinKey)
	{
		string queueTicketName = joinKey;
		foreach (ClientGameWrapper game in App.HytaleServices.Games)
		{
			if (game.JoinKey == joinKey)
			{
				queueTicketName = game.DefaultName;
				break;
			}
		}
		QueueTicketName = queueTicketName;
		QueueStatus.Update();
	}

	public void OnServicesQueueStatusUpdate(string status)
	{
		if (QueueTicketName != null)
		{
			QueueTicketStatus = status;
			QueueStatus.Update();
		}
	}

	public void RegisterForEvent(string name, Disposable disposeGate, Action callback)
	{
		_handlersForInterfaceEvents.Add(name, new InterfaceEventHandler
		{
			DisposeGate = disposeGate,
			Callback = callback
		});
	}

	public void RegisterForEvent<T>(string name, Disposable disposeGate, Action<T> callback)
	{
		_handlersForInterfaceEvents.Add(name, new InterfaceEventHandler
		{
			DisposeGate = disposeGate,
			Callback = callback
		});
	}

	public void RegisterForEvent<T1, T2>(string name, Disposable disposeGate, Action<T1, T2> callback)
	{
		_handlersForInterfaceEvents.Add(name, new InterfaceEventHandler
		{
			DisposeGate = disposeGate,
			Callback = callback
		});
	}

	public void RegisterForEvent<T1, T2, T3>(string name, Disposable disposeGate, Action<T1, T2, T3> callback)
	{
		_handlersForInterfaceEvents.Add(name, new InterfaceEventHandler
		{
			DisposeGate = disposeGate,
			Callback = callback
		});
	}

	public void UnregisterFromEvent(string name)
	{
		_handlersForInterfaceEvents.Remove(name);
	}

	public void TriggerEvent(string name, object data1 = null, object data2 = null, object data3 = null, object data4 = null, object data5 = null, object data6 = null)
	{
		if (!_handlersForEngineEvents.TryGetValue(name, out var value))
		{
			Logger.Warn("No interface-side handler for engine event: {0}", name);
			return;
		}
		Delegate callback = value.Callback;
		switch (callback.Method.GetParameters().Length)
		{
		case 0:
			callback.DynamicInvoke();
			break;
		case 1:
			callback.DynamicInvoke(data1);
			break;
		case 2:
			callback.DynamicInvoke(data1, data2);
			break;
		case 3:
			callback.DynamicInvoke(data1, data2, data3);
			break;
		case 4:
			callback.DynamicInvoke(data1, data2, data3, data4);
			break;
		case 5:
			callback.DynamicInvoke(data1, data2, data3, data4, data5);
			break;
		case 6:
			callback.DynamicInvoke(data1, data2, data3, data4, data5, data6);
			break;
		default:
			throw new NotSupportedException();
		}
	}

	public void RegisterForEventFromEngine(string name, Action callback)
	{
		_handlersForEngineEvents.Add(name, new EngineEventHandler
		{
			Callback = callback
		});
	}

	public void RegisterForEventFromEngine<T>(string name, Action<T> callback)
	{
		_handlersForEngineEvents.Add(name, new EngineEventHandler
		{
			Callback = callback
		});
	}

	public void RegisterForEventFromEngine<T1, T2>(string name, Action<T1, T2> callback)
	{
		_handlersForEngineEvents.Add(name, new EngineEventHandler
		{
			Callback = callback
		});
	}

	public void RegisterForEventFromEngine<T1, T2, T3>(string name, Action<T1, T2, T3> callback)
	{
		_handlersForEngineEvents.Add(name, new EngineEventHandler
		{
			Callback = callback
		});
	}

	public void RegisterForEventFromEngine<T1, T2, T3, T4>(string name, Action<T1, T2, T3, T4> callback)
	{
		_handlersForEngineEvents.Add(name, new EngineEventHandler
		{
			Callback = callback
		});
	}

	public void RegisterForEventFromEngine<T1, T2, T3, T4, T5>(string name, Action<T1, T2, T3, T4, T5> callback)
	{
		_handlersForEngineEvents.Add(name, new EngineEventHandler
		{
			Callback = callback
		});
	}

	public void RegisterForEventFromEngine<T1, T2, T3, T4, T5, T6>(string name, Action<T1, T2, T3, T4, T5, T6> callback)
	{
		_handlersForEngineEvents.Add(name, new EngineEventHandler
		{
			Callback = callback
		});
	}

	public void TriggerEventFromInterface(string name, object data1 = null, object data2 = null, object data3 = null)
	{
		if (!_handlersForInterfaceEvents.TryGetValue(name, out var value))
		{
			Logger.Warn("No engine-side handler for engine event: {0}", name);
		}
		else if (!value.DisposeGate.Disposed)
		{
			Delegate callback = value.Callback;
			switch (callback.Method.GetParameters().Length)
			{
			case 0:
				callback.DynamicInvoke();
				break;
			case 1:
				callback.DynamicInvoke(data1);
				break;
			case 2:
				callback.DynamicInvoke(data1, data2);
				break;
			case 3:
				callback.DynamicInvoke(data1, data2, data3);
				break;
			default:
				throw new NotSupportedException();
			}
		}
	}

	private void RegisterServicesLifecycleEvents()
	{
		RegisterForEvent<string>("services.sendFriendRequestByUsername", Engine, OnSendFriendRequestByUsername);
		RegisterForEvent("services.answerFriendRequest", Engine, delegate(string userId, bool accept)
		{
			App.HytaleServices.AnswerFriendRequest(new Guid(userId), accept);
		});
		RegisterForEvent("services.removeFriend", Engine, delegate(string userId)
		{
			App.HytaleServices.RemoveFriend(new Guid(userId));
		});
		RegisterForEvent("services.sendMessage", Engine, delegate(string userId, string message)
		{
			App.HytaleServices.SendMessage(new Guid(userId), message);
		});
		RegisterForEvent("services.sendPartyMessage", Engine, delegate(string message)
		{
			App.HytaleServices.SendPartyMessage(message);
		});
		RegisterForEvent("services.disbandParty", Engine, delegate
		{
			App.HytaleServices.DisbandParty();
		});
		RegisterForEvent("services.answerPartyInvitation", Engine, delegate(string partyId, bool accept)
		{
			App.HytaleServices.AnswerPartyInvite(partyId, accept);
		});
		RegisterForEvent("services.leaveParty", Engine, delegate
		{
			App.HytaleServices.LeaveParty();
		});
		RegisterForEvent("services.removeMemberFromParty", Engine, delegate(string userId)
		{
			App.HytaleServices.RemoveMemberFromParty(new Guid(userId));
		});
		RegisterForEvent("services.createParty", Engine, delegate
		{
			App.HytaleServices.CreateParty();
		});
		RegisterForEvent("services.answerPartyInvite", Engine, delegate(string partyId, bool accept)
		{
			App.HytaleServices.AnswerPartyInvite(partyId, accept);
		});
		RegisterForEvent("services.inviteUserToParty", Engine, delegate(string userId)
		{
			App.HytaleServices.InviteUserToParty(new Guid(userId));
		});
		RegisterForEvent("services.makeUserPartyLeader", Engine, delegate(string userId)
		{
			App.HytaleServices.MakeUserPartyLeader(new Guid(userId));
		});
		RegisterForEvent("services.toggleUserBlocked", Engine, delegate(string userId, bool blocked)
		{
			App.HytaleServices.ToggleUserBlocked(new Guid(userId), blocked);
		});
		RegisterForEvent("services.leaveGameQueue", Engine, OnLeaveQueue);
	}

	private void UnregisterServicesLifecycleEvents()
	{
		UnregisterFromEvent("services.sendFriendRequestByUsername");
		UnregisterFromEvent("services.answerFriendRequest");
		UnregisterFromEvent("services.removeFriend");
		UnregisterFromEvent("services.sendMessage");
		UnregisterFromEvent("services.sendPartyMessage");
		UnregisterFromEvent("services.disbandParty");
		UnregisterFromEvent("services.answerPartyInvitation");
		UnregisterFromEvent("services.leaveParty");
		UnregisterFromEvent("services.removeMemberFromParty");
		UnregisterFromEvent("services.createParty");
		UnregisterFromEvent("services.answerPartyInvite");
		UnregisterFromEvent("services.inviteUserToParty");
		UnregisterFromEvent("services.makeUserPartyLeader");
		UnregisterFromEvent("services.toggleUserBlocked");
		UnregisterFromEvent("services.leaveGameQueue");
	}

	private void OnSendFriendRequestByUsername(string username)
	{
		App.HytaleServices.SendFriendRequestByUsername(username, delegate(ClientFailureNotification err)
		{
			Engine.RunOnMainThread(this, delegate
			{
				TriggerEvent("services.sendFriendRequestByUsername.reply", username, err.CauseLocalizable);
			});
		}, delegate
		{
			Engine.RunOnMainThread(this, delegate
			{
				TriggerEvent("services.sendFriendRequestByUsername.reply", username);
			});
		});
	}

	private void OnLeaveQueue()
	{
		App.HytaleServices.LeaveGameQueue(delegate(ClientFailureNotification err)
		{
			Logger.Warn<ClientFailureNotification>("Failed to leave queue with error {0}", err);
			Engine.RunOnMainThread(this, delegate
			{
				App.Interface.TriggerEvent("services.queue.error", err.CauseLocalizable);
			});
		}, delegate(ClientSuccessNotification success)
		{
			Logger.Info("Successfully left queue with token {0}", success.Token);
			Engine.RunOnMainThread(this, delegate
			{
				TriggerEvent("services.queue.left");
			});
		});
	}
}
