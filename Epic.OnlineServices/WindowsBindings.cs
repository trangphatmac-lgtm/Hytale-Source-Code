using System;
using System.Runtime.InteropServices;
using Epic.OnlineServices.Platform;

namespace Epic.OnlineServices;

public static class WindowsBindings
{
	[DllImport("EOSSDK-Win64-Shipping.dll")]
	internal static extern IntPtr EOS_Platform_Create(ref WindowsOptionsInternal options);
}
