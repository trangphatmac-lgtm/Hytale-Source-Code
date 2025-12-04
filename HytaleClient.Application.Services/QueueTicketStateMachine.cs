using System;
using Hypixel.ProtoPlus;
using HytaleClient.Auth.Proto.Protocol;
using HytaleClient.Core;
using NLog;

namespace HytaleClient.Application.Services;

internal class QueueTicketStateMachine
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly App _app;

	private readonly Engine _engine;

	public string Key;

	public sbyte[] Extra;

	private bool _ticketAcknowledgedPending;

	private long _ticketStartedProcessing;

	public string Game { get; private set; }

	public string Status { get; private set; }

	public long EstimatedTimeMillis { get; private set; }

	public string Ticket { get; private set; }

	public QueueTicketStateMachine(App app)
	{
		_app = app;
		_engine = app.Engine;
		ResetState();
	}

	public bool TryQueue(string key, sbyte[] extra, out string ticket)
	{
		ticket = null;
		long timestamp = GetTimestamp();
		if (Key == key && AreEqual(Extra, extra) && _ticketStartedProcessing + 5000 > timestamp)
		{
			if (Logger.IsInfoEnabled)
			{
				Logger.Info("Disregarding queue attempt because I'm already queued for {0} and {1}, and we only most recently tried to queue at {2}. Current time {3}", new object[4] { key, extra, _ticketStartedProcessing, timestamp });
			}
			return false;
		}
		if (Ticket == null || (Ticket != null && _ticketAcknowledgedPending && _ticketAcknowledgedPending && _ticketStartedProcessing + 5000 < timestamp) || (Ticket != null && !_ticketAcknowledgedPending))
		{
			string text2 = (Ticket = GenerateNewTicket());
			ticket = text2;
			_ticketStartedProcessing = timestamp;
			_ticketAcknowledgedPending = true;
			Key = key;
			Extra = extra;
			_engine.RunOnMainThread(_engine, delegate
			{
				_app.Interface.OnServicesQueueJoined(key);
			}, allowCallFromMainThread: true);
			return true;
		}
		if (Logger.IsInfoEnabled)
		{
			Logger.Info("Disregarding queue attempt because didn't match secondary condition. Current state {0} input {1} and {2}. Current time {3}", new object[4] { this, key, extra, timestamp });
		}
		return false;
	}

	public void HandleConnectionClose()
	{
		LeaveQueueUI();
	}

	public void HandleConnectionOpen()
	{
		if (Ticket == null)
		{
		}
	}

	private void ResetState()
	{
		Ticket = null;
		_ticketAcknowledgedPending = false;
		_ticketStartedProcessing = 0L;
		Game = null;
		Status = null;
		EstimatedTimeMillis = 0L;
	}

	public void OnLeaveQueueConfirm()
	{
		ResetState();
		LeaveQueueUI();
	}

	private void ShowQueueErrorUI(string cause)
	{
		_engine.RunOnMainThread(_engine, delegate
		{
			_app.Interface.OnServicesQueueError(cause);
		});
	}

	private void UpdateQueueStatusUI(string status)
	{
		_engine.RunOnMainThread(_engine, delegate
		{
			_app.Interface.OnServicesQueueStatusUpdate(status);
		});
	}

	private void LeaveQueueUI()
	{
		_engine.RunOnMainThread(_engine, delegate
		{
			_app.Interface.OnServicesQueueLeft();
		});
	}

	private void JoinQueueUI(string game)
	{
		_engine.RunOnMainThread(_engine, delegate
		{
			_app.Interface.OnServicesQueueJoined(game);
		});
	}

	public void HandleResponse(ClientServerQueueReply reply)
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Expected O, but got Unknown
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Expected O, but got Unknown
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Expected O, but got Unknown
		ProtoSerializable val = ServerQueueUtil.ReadResponseFrom(reply);
		if (Ticket == null)
		{
			Logger.Info<ClientServerQueueReply, ProtoSerializable>("Got ticket response when we don't actually have our own ticket set: {0} {1}", reply, val);
			return;
		}
		if (Ticket != reply.Ticket)
		{
			Logger.Info("Got ticket response with mismatching ticket! Our ticket is {0} but received ticket {1}: {2} {3}", new object[4] { Ticket, reply.Ticket, reply, val });
			return;
		}
		if (((object)val).GetType() == typeof(ClientServerQueueFailure))
		{
			ClientServerQueueFailure val2 = (ClientServerQueueFailure)val;
			Logger.Info<ProtoSerializable>("Got queue failure response from server {0}", val);
			Ticket = null;
			ResetState();
			ShowQueueErrorUI(((object)(FailureCause)(ref val2.Cause)).ToString());
			return;
		}
		if (((object)val).GetType() == typeof(ClientServerQueueStatus))
		{
			Logger.Info<ProtoSerializable>("Received status update for queue {0}", val);
			ClientServerQueueStatus val3 = (ClientServerQueueStatus)val;
			if (!_ticketAcknowledgedPending)
			{
				Logger.Info<string, string>("Got status update to completed ticket {0}: {1}", Ticket, val3.Message);
				return;
			}
			Status = val3.Message;
			UpdateQueueStatusUI(val3.Message);
			return;
		}
		if (((object)val).GetType() == typeof(ClientServerQueueTicket))
		{
			Logger.Info<ProtoSerializable>("Received ticket from server for queue {0}", val);
			ClientServerQueueTicket val4 = (ClientServerQueueTicket)val;
			Game = val4.Game;
			EstimatedTimeMillis = val4.EstimatedTimeMillis;
			if (_ticketAcknowledgedPending)
			{
				JoinQueueUI(Game);
			}
			return;
		}
		if (((object)val).GetType() == typeof(ClientServerQueueWorldTransfer))
		{
			Logger.Info<ProtoSerializable>("Received world transfer notice {0}", val);
			ResetState();
			LeaveQueueUI();
			return;
		}
		if (((object)val).GetType() == typeof(ClientServerQueueFinal))
		{
			Logger.Info<ProtoSerializable>("Got queue final response! Time to join with {0}", val);
			ClientServerQueueFinal val5 = (ClientServerQueueFinal)val;
			ResetState();
			LeaveQueueUI();
			ConnectToServer(val5.Ip, val5.Port);
			return;
		}
		throw new Exception("Illegal response from server queue: " + (object)val);
	}

	private void ConnectToServer(string hostname, int port)
	{
		_engine.RunOnMainThread(_engine, delegate
		{
			_app.Interface.OnServicesQueueLeft();
			if (_app.Stage == App.AppStage.GameLoading)
			{
				_app.GameLoading.Abort();
			}
			_app.GameLoading.Open(hostname, port, AppMainMenu.MainMenuPage.Home);
			_app.Interface.FadeIn();
		});
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}, {4}: {5}, {6}: {7}, {8}: {9}, {10}: {11}, {12}: {13}, {14}: {15}", "Key", Key, "Extra", Extra, "_ticketAcknowledgedPending", _ticketAcknowledgedPending, "_ticketStartedProcessing", _ticketStartedProcessing, "Game", Game, "Status", Status, "EstimatedTimeMillis", EstimatedTimeMillis, "Ticket", Ticket);
	}

	private static string GenerateNewTicket()
	{
		return "T" + Guid.NewGuid();
	}

	private static long GetTimestamp()
	{
		return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
	}

	private static bool AreEqual(sbyte[] a, sbyte[] b)
	{
		if (a == b)
		{
			return true;
		}
		if (a == null || b == null)
		{
			return false;
		}
		return HaveSameContents(a, b);
	}

	private static bool HaveSameContents(sbyte[] a, sbyte[] b)
	{
		int num = a.Length;
		if (num != b.Length)
		{
			return false;
		}
		while (num != 0)
		{
			num--;
			if (a[num] != b[num])
			{
				return false;
			}
		}
		return true;
	}
}
