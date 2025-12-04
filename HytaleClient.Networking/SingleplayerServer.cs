#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using HytaleClient.Application;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Networking;

internal class SingleplayerServer
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly Process Process;

	public readonly int Port;

	private readonly App _app;

	private Action<string, float> _onProgress;

	private Action _onReady;

	private readonly Action _onShutdown;

	private bool _isDumping;

	private bool _isStopping;

	public volatile string ShutdownMessage = null;

	private Stopwatch _lastProgress = Stopwatch.StartNew();

	public SingleplayerServer(App app, string directoryName, Action<string, float> onProgress, Action onReady, Action onShutdown)
	{
		_app = app;
		_onProgress = onProgress;
		_onReady = onReady;
		_onShutdown = onShutdown;
		string text = Paths.TrimBackslash(Path.Combine(Paths.Saves, directoryName));
		string text2 = Paths.TrimBackslash(Paths.BuiltInAssets);
		string text3 = Paths.TrimBackslash(Paths.Server);
		Port = FindFreeTcpPort();
		string text4 = Path.Combine(Paths.UserData, "Server");
		Directory.CreateDirectory(text4);
		List<string> list = new List<string>
		{
			"-jar \"" + text3 + "\"",
			$"--client-pid {Process.GetCurrentProcess().Id}",
			$"--bind {Port}",
			"--assets=\"" + text2 + "\"",
			"--singleplayer",
			$"--ownerUuid=\"{_app.AuthManager.GetPlayerUuid()}\"",
			"--owner=\"" + _app.Username + "\"",
			"--universe=\"" + text + "\""
		};
		if (!_app.AuthManager.Settings.IsInsecure)
		{
			string text5 = Path.Combine(text4, ".spPrivateKey");
			string text6 = Path.Combine(text4, ".spCert");
			_app.AuthManager.WritePemDataSp(text5, text6);
			list.Add("--spkey=\"" + text5 + "\"");
			list.Add("--spcert=\"" + text6 + "\"");
		}
		list.AddRange(OptionsHelper.CustomServerArgumentList);
		Logger.Info<string, List<string>>("Starting server: {0} {1}", Paths.Java, list);
		Process = Process.Start(new ProcessStartInfo
		{
			FileName = Paths.Java,
			Arguments = string.Join(" ", list),
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			RedirectStandardInput = true,
			WorkingDirectory = text4
		});
		Debug.Assert(Process != null, "Process != null");
		Process.Exited += OnServerProcessExit;
		Process.EnableRaisingEvents = true;
		Process.OutputDataReceived += OnOutputDataReceived;
		Process.ErrorDataReceived += OnErrorDataReceived;
		Process.BeginOutputReadLine();
		Process.BeginErrorReadLine();
		AppDomain.CurrentDomain.ProcessExit += OnClientProcessExit;
	}

	private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
	{
		string data = e.Data;
		if (data == null)
		{
			return;
		}
		if (data.StartsWith("-=|"))
		{
			string[] array = data.Split(new char[1] { '|' });
			string phase = array[1];
			_lastProgress.Restart();
			if (phase == "Shutdown")
			{
				ShutdownMessage = array[2];
				return;
			}
			float progress = float.Parse(array[2], CultureInfo.InvariantCulture);
			_app.Engine.RunOnMainThread(_app.Engine, delegate
			{
				_onProgress?.Invoke(phase, progress);
			});
		}
		else if (data.Contains(">> Singleplayer Ready <<") && !_isStopping)
		{
			Logger.Info("Singleplayer server is ready. Starting to connect...");
			_app.Engine.RunOnMainThread(_app.Engine, delegate
			{
				_onReady?.Invoke();
			});
		}
	}

	private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
	{
		string data = e.Data;
		if (data != null)
		{
			if (data.Contains("Failed to shutdown correctly dumping!"))
			{
				_isDumping = true;
			}
			Logger.Warn("ERROR - {0}", data);
		}
	}

	public void Close()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (!_isStopping && !Process.HasExited)
		{
			StopServer();
		}
		_onProgress = null;
		_onReady = null;
	}

	private void OnClientProcessExit(object sender, EventArgs eventArgs)
	{
		Logger.Info("Client process exit");
		if (!_isStopping && !Process.HasExited)
		{
			StopServer();
		}
	}

	private void OnServerProcessExit(object sender, EventArgs eventArgs)
	{
		Debug.Assert(Process.HasExited);
		Logger.Info("Server process exited with code {0}", Process.ExitCode);
		AppDomain.CurrentDomain.ProcessExit -= OnClientProcessExit;
		AffinityHelper.SetupDefaultAffinity();
		_app.Engine.RunOnMainThread(_app.Engine, _onShutdown, allowCallFromMainThread: true);
	}

	private void StopServer()
	{
		Debug.Assert(!_isStopping, "StopServer method already got called");
		Logger.Info("Stopping server...");
		_isStopping = true;
		AppDomain.CurrentDomain.ProcessExit -= OnClientProcessExit;
		Process.StandardInput.WriteLine("stop");
		_lastProgress.Restart();
		ThreadPool.QueueUserWorkItem(delegate
		{
			bool flag = false;
			int num = 10000;
			while (!Process.WaitForExit(1000))
			{
				if (_lastProgress.ElapsedMilliseconds > num)
				{
					if (flag)
					{
						Logger.Warn("Failed to stop server cleanly!");
						Process.Kill();
						return;
					}
					flag = true;
					Process.StandardInput.WriteLine("dump");
					_app.Engine.RunOnMainThread(_app.Engine, delegate
					{
						_onProgress?.Invoke("Server seems frozen. Dumping", 0f);
					});
					num = 20000;
					_lastProgress.Restart();
				}
			}
			Logger.Info("Stopped server, exit code: {0}", Process.ExitCode);
		});
	}

	private static int FindFreeTcpPort()
	{
		TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);
		tcpListener.Start();
		int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
		tcpListener.Stop();
		return port;
	}
}
