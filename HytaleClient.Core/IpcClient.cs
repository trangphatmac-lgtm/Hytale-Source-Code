using System;
using System.Threading;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Core;

public class IpcClient : Disposable
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const string MessagePrefix = "ipc:";

	private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	private readonly Action<string, JObject> _commandReceived;

	private Thread _thread;

	private IpcClient()
	{
	}

	private IpcClient(Action<string, JObject> commandReceived)
	{
		_commandReceived = commandReceived;
		_thread = new Thread(ProcessInputThreadStart);
		_thread.IsBackground = true;
		_thread.Name = "IpcClient";
		_thread.Start();
	}

	private void ProcessInputThreadStart()
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		CancellationToken token = _cancellationTokenSource.Token;
		while (!token.IsCancellationRequested)
		{
			string text = Console.In.ReadLine();
			if (text == null)
			{
				break;
			}
			text = text.Trim();
			if (text.StartsWith("ipc:"))
			{
				string arg;
				JObject arg2;
				try
				{
					string text2 = text.Substring("ipc:".Length).Trim();
					JObject val = JObject.Parse(text2);
					arg = (string)val["Command"];
					arg2 = ((!val.ContainsKey("Data")) ? ((JObject)null) : ((JObject)val["Data"]));
				}
				catch (Exception ex)
				{
					Logger.Warn(ex, "Failed to parse ipc command {0}", new object[1] { text });
					continue;
				}
				try
				{
					_commandReceived(arg, arg2);
				}
				catch (Exception ex2)
				{
					Logger.Error(ex2, "IPC message handler threw an exception");
				}
			}
		}
	}

	public void SendCommand(string command, JObject data)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		JObject val = new JObject();
		val.Add("Command", JToken.op_Implicit(command));
		val.Add("Data", (JToken)(object)data);
		JObject val2 = val;
		Console.Out.WriteLine("ipc:" + ((JToken)val2).ToString((Formatting)0, Array.Empty<JsonConverter>()));
	}

	protected override void DoDispose()
	{
		_cancellationTokenSource.Cancel();
		_cancellationTokenSource.Dispose();
		_thread?.Interrupt();
	}

	public static IpcClient CreateWriteOnlyClient()
	{
		return new IpcClient();
	}

	public static IpcClient CreateReadWriteClient(Action<string, JObject> commandReceived)
	{
		return new IpcClient(commandReceived);
	}
}
