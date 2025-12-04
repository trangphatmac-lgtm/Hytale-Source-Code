#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using SDL2;

namespace HytaleClient.Utils;

internal static class LogWriter
{
	private class HytaleLogListener : TraceListener
	{
		public override void Write(string message)
		{
			_logger.Info(message);
		}

		public override void WriteLine(string message)
		{
			_logger.Info(message);
		}

		public override void Fail(string message, string detailMessage)
		{
			//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0100: Unknown result type (might be due to invalid IL or missing references)
			//IL_0102: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_012d: Unknown result type (might be due to invalid IL or missing references)
			//IL_012f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0139: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			if (BuildInfo.Platform == Platform.Windows)
			{
				base.Fail(message, detailMessage);
				return;
			}
			StackTrace stackTrace = new StackTrace(fNeedFileInfo: true);
			string text = "";
			if (!string.IsNullOrEmpty(message))
			{
				text += message;
				if (!string.IsNullOrEmpty(detailMessage))
				{
					text = text + ": '" + detailMessage + "'";
				}
				text += "\n";
			}
			else if (!string.IsNullOrEmpty(detailMessage))
			{
				text += detailMessage;
				text += "\n";
			}
			text += stackTrace.ToString();
			SDL_MessageBoxData val = default(SDL_MessageBoxData);
			val.flags = (SDL_MessageBoxFlags)16;
			val.title = "Fail";
			val.message = text;
			val.numbuttons = 2;
			val.buttons = (SDL_MessageBoxButtonData[])(object)new SDL_MessageBoxButtonData[2]
			{
				new SDL_MessageBoxButtonData
				{
					flags = (SDL_MessageBoxButtonFlags)2,
					buttonid = 0,
					text = "Ignore"
				},
				new SDL_MessageBoxButtonData
				{
					flags = (SDL_MessageBoxButtonFlags)1,
					buttonid = 1,
					text = "Abort"
				}
			};
			SDL_MessageBoxData val2 = val;
			int num = default(int);
			SDL.SDL_ShowMessageBox(ref val2, ref num);
			if (num == 1)
			{
				throw new Exception(text);
			}
			_logger.Error("Fail: " + text);
		}
	}

	private static Logger _logger;

	private const int MaxLogs = 10;

	private static readonly string LogsFolderPath;

	public static string LogPath { get; private set; }

	static LogWriter()
	{
		LogsFolderPath = Path.Combine(Paths.UserData, "Logs");
	}

	public static void Start()
	{
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Expected O, but got Unknown
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Expected O, but got Unknown
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Expected O, but got Unknown
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Expected O, but got Unknown
		if (!Debugger.IsAttached)
		{
			Trace.Listeners.Add(new ConsoleTraceListener());
		}
		string text = OptionsHelper.LogFileOverride;
		if (text == null)
		{
			if (!Directory.Exists(LogsFolderPath))
			{
				Directory.CreateDirectory(LogsFolderPath);
			}
			else
			{
				CleanLogDirectory(LogsFolderPath);
			}
			text = Paths.EnsureUniqueFilename(Path.Combine(LogsFolderPath, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")), OptionsHelper.LaunchEditor ? "_editor.log" : "_client.log");
		}
		else if (File.Exists(text))
		{
			Console.WriteLine("Specified log file already exists at '" + text + "', deleting");
			File.Delete(text);
		}
		Console.WriteLine("Set log path to " + text);
		try
		{
			Trace.Listeners.Add(new HytaleLogListener());
			Trace.AutoFlush = true;
			Layout layout = Layout.FromString("${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring,Data:separator=\n}");
			LoggingConfiguration val = new LoggingConfiguration();
			val.AddRule(LogLevel.Info, LogLevel.Fatal, (Target)new ConsoleTarget("logconsole")
			{
				Encoding = Encoding.UTF8,
				Layout = layout
			}, "*");
			val.AddRule(LogLevel.Debug, LogLevel.Fatal, (Target)new FileTarget("logfile")
			{
				FileName = Layout.op_Implicit(text),
				Encoding = Encoding.UTF8,
				Layout = layout,
				KeepFileOpen = true,
				OpenFileCacheTimeout = 30
			}, "*");
			if (Debugger.IsAttached)
			{
				val.AddRule(LogLevel.Debug, LogLevel.Fatal, (Target)new DebuggerTarget("debugger")
				{
					Layout = layout
				}, "*");
			}
			LogManager.Configuration = val;
			_logger = LogManager.GetCurrentClassLogger();
		}
		catch (Exception ex)
		{
			string text2 = "Failed to setup logging.";
			string text3 = "Could not setup log at " + text + ".\n" + ex.Message;
			SDL.SDL_ShowSimpleMessageBox((SDL_MessageBoxFlags)16, text2, text3, IntPtr.Zero);
			return;
		}
		Debug.WriteLine("Log started.");
		LogPath = text;
	}

	private static void CleanLogDirectory(string dir)
	{
		try
		{
			string[] files = Directory.GetFiles(dir);
			if (files.Length >= 10)
			{
				Array.Sort(files, (IComparer<string>?)StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true));
				for (int i = 0; i <= files.Length - 10; i++)
				{
					File.Delete(files[i]);
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
