using System;
using System.Runtime.InteropServices;

namespace HytaleClient.Utils;

internal class WindowsUtils
{
	private static class NativeMethods
	{
		[DllImport("shell32.dll")]
		internal static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appID);
	}

	public const int ResultOk = 0;

	public static int SetApplicationUserModelId(string appId)
	{
		Version version = Environment.OSVersion.Version;
		if (version.Major > 6 || (version.Major == 6 && version.Minor >= 1))
		{
			return NativeMethods.SetCurrentProcessExplicitAppUserModelID(appId);
		}
		return -1;
	}
}
