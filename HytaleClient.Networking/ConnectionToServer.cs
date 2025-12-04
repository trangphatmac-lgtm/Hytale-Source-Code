#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Hypixel.ProtoPlus;
using HytaleClient.Core;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Networking;

internal class ConnectionToServer
{
	private class SocketBufferStream
	{
		public int Offset;

		public byte[] Buffer;
	}

	public struct PacketStat
	{
		public string Name;

		public long ReceivedCount;

		public long ReceivedTotalElapsed;

		public long SentCount;

		public long SentTotalSize;

		public void AddReceivedTime(long elapsed)
		{
			ReceivedCount++;
			ReceivedTotalElapsed += elapsed;
		}

		public void AddSentSize(long size)
		{
			SentCount++;
			SentTotalSize += size;
		}
	}

	private const int MaxSendInterval = 30;

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly Engine _engine;

	public readonly string Hostname;

	public readonly int Port;

	private readonly Action<Exception> _onConnected;

	public Action<byte[], int> OnPacketReceived;

	public Action<Exception> OnDisconnected;

	public int SentPacketLength;

	public int ReceivedPacketLength;

	public ReferralConnect Referral;

	private Socket _socket;

	private Thread _connectAndReceiveThread;

	private Thread _sendThread;

	private ConcurrentQueue<ProtoPacket> _sendQueue = new ConcurrentQueue<ProtoPacket>();

	private AutoResetEvent _sendTriggerEvent = new AutoResetEvent(initialState: true);

	public PacketStat[] PacketStats = new PacketStat[243];

	public ConnectionToServer(Engine engine, string hostname, int port, Action<Exception> onConnected, Action<Exception> onDisconnected)
	{
		ConnectionToServer connectionToServer = this;
		_engine = engine;
		Hostname = hostname;
		Port = port;
		_onConnected = onConnected;
		OnDisconnected = onDisconnected;
		_connectAndReceiveThread = new Thread((ThreadStart)delegate
		{
			connectionToServer.ConnectAndReceiveThreadStart(hostname, port);
		})
		{
			Name = "ConnectAndReceive",
			IsBackground = true
		};
		_connectAndReceiveThread.Start();
		_sendThread = new Thread(SendPackets)
		{
			Name = "SocketSend",
			IsBackground = true
		};
		_sendThread.Start();
	}

	private void ConnectAndReceiveThreadStart(string hostname, int port)
	{
		try
		{
			IPAddress[] hostAddresses = Dns.GetHostAddresses(hostname);
			if (hostAddresses.Length == 0)
			{
				throw new Exception($"Failed to find any addresses for: {hostname}:{port}");
			}
			Exception ex = null;
			IPAddress[] array = hostAddresses;
			foreach (IPAddress iPAddress in array)
			{
				try
				{
					_socket = new Socket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
					{
						NoDelay = true,
						LingerState = new LingerOption(enable: true, 1)
					};
					_socket.Connect(iPAddress, port);
				}
				catch (SocketException ex2)
				{
					ex = ex2;
					_socket?.Dispose();
					_socket = null;
					continue;
				}
				break;
			}
			if (_socket == null)
			{
				throw ex ?? new Exception("Failed to find host address for $" + hostname);
			}
		}
		catch (ThreadAbortException)
		{
			return;
		}
		catch (Exception ex4)
		{
			Exception ex5 = ex4;
			Exception exception = ex5;
			Logger.Error(exception, "Failed to connect to {0}:{1}", new object[2] { hostname, port });
			if (_onConnected != null)
			{
				_engine.RunOnMainThread(_engine, delegate
				{
					_onConnected?.Invoke(exception);
				});
			}
			return;
		}
		if (_onConnected != null)
		{
			_engine.RunOnMainThread(_engine, delegate
			{
				_onConnected?.Invoke(null);
			});
		}
		byte[] array2 = new byte[65557];
		int num = 0;
		byte[] array3 = new byte[array2.Length + 4];
		SocketBufferStream socketBufferStream = new SocketBufferStream();
		socketBufferStream.Buffer = new byte[4];
		SocketBufferStream socketBufferStream2 = socketBufferStream;
		SocketBufferStream socketBufferStream3 = new SocketBufferStream();
		while (true)
		{
			int num2 = 0;
			try
			{
				num2 = _socket.Receive(array3);
			}
			catch (SocketException)
			{
			}
			if (num2 == 0)
			{
				break;
			}
			int num3 = 0;
			while (num3 < num2)
			{
				int num4 = 0;
				int num5 = 0;
				if (socketBufferStream3.Buffer == null)
				{
					num4 = System.Math.Min(num2 - num3, 4 - socketBufferStream2.Offset);
					Buffer.BlockCopy(array3, num3, socketBufferStream2.Buffer, socketBufferStream2.Offset, num4);
					socketBufferStream2.Offset += num4;
					if (socketBufferStream2.Offset == 4)
					{
						num = BitConverter.ToInt32(socketBufferStream2.Buffer, 0);
						socketBufferStream2.Offset = 0;
						Interlocked.Add(ref ReceivedPacketLength, num);
						if (num <= array2.Length)
						{
							socketBufferStream3.Buffer = array2;
						}
						else
						{
							Logger.Warn<int, int>("Received a packet with a payload of {0} bytes (bigger than {1}). If this happens often, the default size should be adjusted", num, array2.Length);
							socketBufferStream3.Buffer = new byte[num];
						}
					}
				}
				num3 += num4;
				if (socketBufferStream3.Buffer != null)
				{
					num5 = System.Math.Min(num2 - num3, num - socketBufferStream3.Offset);
					Buffer.BlockCopy(array3, num3, socketBufferStream3.Buffer, socketBufferStream3.Offset, num5);
					socketBufferStream3.Offset += num5;
					if (socketBufferStream3.Offset == num)
					{
						try
						{
							OnPacketReceived(socketBufferStream3.Buffer, num);
						}
						catch (Exception innerException)
						{
							Exception wrapperException2 = new Exception("Failed to deserialize packet: ", innerException);
							if (OnDisconnected != null)
							{
								_engine.RunOnMainThread(_engine, delegate
								{
									OnDisconnected?.Invoke(wrapperException2);
								});
							}
							return;
						}
						socketBufferStream3.Buffer = null;
						socketBufferStream3.Offset = 0;
					}
				}
				num3 += num5;
			}
		}
		Exception wrapperException = new Exception("Failed to receive frame from server (zero bytes received)");
		if (OnDisconnected != null)
		{
			_engine.RunOnMainThread(_engine, delegate
			{
				OnDisconnected?.Invoke(wrapperException);
			});
		}
	}

	private void SendPackets()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		int num = 0;
		Stopwatch stopwatch = new Stopwatch();
		MemoryStream memoryStream = new MemoryStream();
		ProtoBinaryWriter val = new ProtoBinaryWriter((Stream)memoryStream);
		while (true)
		{
			stopwatch.Restart();
			ProtoPacket result;
			while (_sendQueue.TryDequeue(out result))
			{
				int num2 = (int)memoryStream.Position;
				((BinaryWriter)(object)val).Write(0);
				result.WritePacket(val);
				int num3 = (int)memoryStream.Position;
				int num4 = num3 - num2 - 4;
				if (num4 <= 0)
				{
					throw new ArgumentException("Packet length can't be 0");
				}
				memoryStream.Position = num2;
				((BinaryWriter)(object)val).Write(num4);
				memoryStream.Position = num3;
				int num5 = num4 + 4;
				num += num5;
				ref PacketStat reference = ref PacketStats[result.GetId()];
				if (reference.Name == null)
				{
					reference.Name = ((object)result).GetType().Name;
				}
				reference.AddSentSize(num4);
			}
			if (num > 0)
			{
				try
				{
					Interlocked.Add(ref SentPacketLength, num);
					_socket.Send(memoryStream.GetBuffer(), num, SocketFlags.None);
					num = 0;
				}
				catch (SocketException ex)
				{
					Logger.Error<SocketException>(ex);
				}
			}
			memoryStream.SetLength(0L);
			int num6 = 30 - (int)stopwatch.ElapsedMilliseconds;
			if (num6 > 0)
			{
				_sendTriggerEvent.WaitOne(num6);
			}
		}
	}

	public void Close()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (_connectAndReceiveThread != null)
		{
			_connectAndReceiveThread.Abort();
			_connectAndReceiveThread = null;
		}
		if (_sendThread != null)
		{
			_sendThread.Abort();
			_sendThread = null;
		}
		_socket?.Close();
		_socket = null;
	}

	public void TriggerSend()
	{
		_sendTriggerEvent.Set();
	}

	public void SendPacket(ProtoPacket packet)
	{
		if (packet == null)
		{
			throw new ArgumentException("Packet can't be null");
		}
		_sendQueue.Enqueue(packet);
	}

	public void SendPacketImmediate(ProtoPacket packet)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		if (packet == null)
		{
			throw new ArgumentException("Packet can't be null");
		}
		if (_socket == null || !_socket.Connected)
		{
			return;
		}
		using MemoryStream memoryStream = new MemoryStream();
		ProtoBinaryWriter val = new ProtoBinaryWriter((Stream)memoryStream);
		try
		{
			((BinaryWriter)(object)val).Write(0);
			packet.WritePacket(val);
			if (memoryStream.Length <= 4)
			{
				throw new ArgumentException("Packet length can't be 0");
			}
			((BinaryWriter)(object)val).Seek(0, SeekOrigin.Begin);
			long num = memoryStream.Length - 4;
			((BinaryWriter)(object)val).Write((int)num);
			((BinaryWriter)(object)val).Seek(0, SeekOrigin.End);
			byte[] array = memoryStream.ToArray();
			try
			{
				if (_socket != null && _socket.Connected)
				{
					Interlocked.Add(ref SentPacketLength, array.Length);
					_socket.Send(array, array.Length, SocketFlags.None);
					ref PacketStat reference = ref PacketStats[packet.GetId()];
					if (reference.Name == null)
					{
						reference.Name = ((object)packet).GetType().Name;
					}
					reference.AddSentSize(num);
				}
			}
			catch (SocketException ex)
			{
				SocketException ex2 = ex;
				SocketException exception = ex2;
				_engine.RunOnMainThread(_engine, delegate
				{
					OnDisconnected?.Invoke(exception);
				}, allowCallFromMainThread: true);
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void ResetPacketStats()
	{
		PacketStats = new PacketStat[243];
	}
}
