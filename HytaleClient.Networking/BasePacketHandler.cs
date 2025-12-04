#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Hypixel.ProtoPlus;
using HytaleClient.Core;
using HytaleClient.Interface.Messages;
using HytaleClient.Net.Protocol;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Networking;

internal abstract class BasePacketHandler : Disposable
{
	private class PendingCallback
	{
		public Action<FailureReply, ProtoPacket> Callback;

		public Disposable Disposable;
	}

	private ConcurrentDictionary<int, PendingCallback> _pendingCallbacks = new ConcurrentDictionary<int, PendingCallback>();

	private int _lastCallbackToken;

	private DateTime _lastCallbackWarning;

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly Engine _engine;

	protected readonly ConnectionToServer _connection;

	private readonly BlockingCollection<ProtoPacket> _packets = new BlockingCollection<ProtoPacket>();

	protected readonly Thread _thread;

	private readonly CancellationTokenSource _threadCancellationTokenSource = new CancellationTokenSource();

	private readonly CancellationToken _threadCancellationToken;

	public bool IsOnThread => ThreadHelper.IsOnThread(_thread);

	public int AddPendingCallback<T>(Disposable disposable, Action<FailureReply, T> callback) where T : ProtoPacket
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected O, but got Unknown
		if (_pendingCallbacks.Count > 1000)
		{
			if ((DateTime.Now - _lastCallbackWarning).TotalSeconds > 5.0)
			{
				_lastCallbackWarning = DateTime.Now;
				Logger.Warn("There are currently more than 1000 pending packet callbacks. Removing oldest callback...");
			}
			int num = _pendingCallbacks.Keys.First();
			_pendingCallbacks.TryRemove(num, out var value);
			value.Callback(new FailureReply(num, BsonHelper.ToBson(JToken.FromObject((object)FormattedMessage.FromMessageId("ui.general.callback.cancelled")))), null);
		}
		int num2 = Interlocked.Add(ref _lastCallbackToken, 1);
		_pendingCallbacks[num2] = new PendingCallback
		{
			Callback = delegate(FailureReply err, ProtoPacket res)
			{
				callback(err, (T)(object)res);
			},
			Disposable = disposable
		};
		return num2;
	}

	protected void CallPendingCallback(int token, ProtoPacket responsePacket, FailureReply failurePacket)
	{
		if (_pendingCallbacks.TryRemove(token, out var value) && !value.Disposable.Disposed)
		{
			value.Callback?.Invoke(failurePacket, responsePacket);
		}
	}

	protected BasePacketHandler(Engine engine, ConnectionToServer connection)
	{
		_engine = engine;
		_connection = connection;
		_threadCancellationToken = _threadCancellationTokenSource.Token;
		_thread = new Thread(ProcessPacketsThreadStart)
		{
			Name = "BackgroundPacketHandler",
			IsBackground = true
		};
		_thread.Start();
	}

	protected override void DoDispose()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_threadCancellationTokenSource.Cancel();
		_thread.Join();
		_threadCancellationTokenSource.Dispose();
	}

	public void Receive(byte[] buffer, int payloadLength)
	{
		ProtoBinaryReader val = ProtoBinaryReader.Create(buffer, payloadLength);
		try
		{
			Receive(PacketReader.ReadPacket(val));
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private void Receive(ProtoPacket packet)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		if (packet.GetId() == 1)
		{
			string reason = ((Disconnect)packet).Reason;
			SetDisconnectReason(reason);
			return;
		}
		if (packet.GetId() == 2)
		{
			Ping val = (Ping)packet;
			DateTime utcNow = DateTime.UtcNow;
			ProcessPingPacket(val);
			_connection.SendPacket((ProtoPacket)new Pong(val.Id, TimeHelper.DateTimeToInstantData(utcNow), (PongType)0, (short)_packets.Count));
		}
		_packets.Add(packet, _threadCancellationToken);
	}

	private void ProcessPacketsThreadStart()
	{
		Debug.Assert(ThreadHelper.IsOnThread(_thread));
		Stopwatch stopwatch = Stopwatch.StartNew();
		ProtoPacket val = null;
		try
		{
			while (true)
			{
				CancellationToken threadCancellationToken = _threadCancellationToken;
				if (!threadCancellationToken.IsCancellationRequested)
				{
					try
					{
						val = _packets.Take(_threadCancellationToken);
					}
					catch (OperationCanceledException)
					{
						break;
					}
					stopwatch.Restart();
					ProcessPacket(val);
					ref ConnectionToServer.PacketStat reference = ref _connection.PacketStats[val.GetId()];
					if (reference.Name == null)
					{
						reference.Name = ((object)val).GetType().Name;
					}
					reference.AddReceivedTime(stopwatch.ElapsedTicks);
					continue;
				}
				break;
			}
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			Exception e = ex3;
			Logger.Error(e, "Exception when handling packet {0} {1}:", new object[2]
			{
				(val != null) ? new int?(val.GetId()) : null,
				((object)val)?.GetType().Name
			});
			string reason = $"Exception when handling packet {val.GetId()} {((object)val).GetType().Name}: {e.Message}";
			_engine.RunOnMainThread(_engine, delegate
			{
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0039: Expected O, but got Unknown
				SetDisconnectReason(reason);
				_connection.SendPacketImmediate((ProtoPacket)new Disconnect(reason, (DisconnectType)1));
				_connection.Close();
				_connection.OnDisconnected(e);
			}, allowCallFromMainThread: true);
		}
	}

	protected virtual void SetDisconnectReason(string reason)
	{
	}

	protected virtual void ProcessPingPacket(Ping packet)
	{
	}

	protected abstract void ProcessPacket(ProtoPacket packet);
}
