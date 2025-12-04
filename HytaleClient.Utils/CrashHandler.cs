using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NLog;
using SDL2;

namespace HytaleClient.Utils;

internal static class CrashHandler
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public static bool IsCrashing;

	private static bool _isHooked;

	public static void Hook()
	{
		if (!_isHooked)
		{
			_isHooked = true;
			AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
			{
				Crash(e.ExceptionObject as Exception);
			};
		}
	}

	private static void Crash(Exception crashException)
	{
		if (!IsCrashing)
		{
			IsCrashing = true;
			Exception ex = crashException;
			StringBuilder stringBuilder = new StringBuilder();
			while (ex != null)
			{
				stringBuilder.AppendLine(ex.ToString());
				stringBuilder.AppendLine("--------------------");
				ex = ex.InnerException;
			}
			Logger.Error<StringBuilder>(stringBuilder);
			StringBuilder stringBuilder2 = new StringBuilder("A critical error occured:");
			stringBuilder2.AppendLine();
			stringBuilder2.AppendLine(crashException.Message);
			stringBuilder2.AppendLine();
			if (LogWriter.LogPath != null)
			{
				stringBuilder2.AppendLine("A log was saved at:");
				stringBuilder2.AppendLine(LogWriter.LogPath);
			}
			SDL.SDL_ShowSimpleMessageBox((SDL_MessageBoxFlags)16, "Hytale has crashed", stringBuilder2.ToString(), IntPtr.Zero);
			if (LogWriter.LogPath != null)
			{
				Process.Start(Path.GetDirectoryName(LogWriter.LogPath));
			}
			Environment.Exit(1);
		}
	}
}
