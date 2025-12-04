using System;
using System.Runtime.InteropServices;
using HytaleClient.Math;

namespace HytaleClient.Utils;

public static class WindowsDPIHelper
{
	private enum PROCESS_DPI_AWARENESS
	{
		Process_DPI_Unaware,
		Process_System_DPI_Aware,
		Process_Per_Monitor_DPI_Aware
	}

	private enum DPI_AWARENESS_CONTEXT
	{
		DPI_AWARENESS_CONTEXT_UNAWARE = 16,
		DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = 17,
		DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE = 18,
		DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = 34
	}

	private enum DpiType
	{
		Effective,
		Angular,
		Raw
	}

	public const int ReferenceDPI = 96;

	private const int MONITOR_DEFAULTTONEAREST = 2;

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetProcessDpiAwarenessContext(int dpiFlag);

	[DllImport("SHCore.dll", SetLastError = true)]
	private static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

	[DllImport("user32.dll")]
	private static extern bool SetProcessDPIAware();

	public static bool TryEnableDpiAwareness()
	{
		try
		{
			return SetProcessDpiAwarenessContext(34);
		}
		catch
		{
		}
		try
		{
			return SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.Process_Per_Monitor_DPI_Aware);
		}
		catch
		{
		}
		try
		{
			return SetProcessDPIAware();
		}
		catch
		{
		}
		return false;
	}

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(IntPtr hwnd);

	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromPoint([In] Point pt, [In] uint dwFlags);

	[DllImport("shcore.dll")]
	private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, out uint dpiX, out uint dpiY);

	public static bool TryGetDpiForWindow(IntPtr hwnd, out uint dpi)
	{
		try
		{
			dpi = GetDpiForWindow(hwnd);
			return true;
		}
		catch
		{
		}
		try
		{
			IntPtr hmonitor = MonitorFromPoint(new Point(0, 0), 2u);
			GetDpiForMonitor(hmonitor, DpiType.Effective, out dpi, out var _);
			return dpi != 0;
		}
		catch
		{
		}
		dpi = 0u;
		return false;
	}
}
