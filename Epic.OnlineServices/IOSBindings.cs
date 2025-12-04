using System;
using System.Runtime.InteropServices;
using Epic.OnlineServices.Auth;

namespace Epic.OnlineServices;

public static class IOSBindings
{
	[DllImport("EOSSDK-Win64-Shipping.dll")]
	internal static extern void EOS_Auth_Login(IntPtr handle, ref IOSLoginOptionsInternal options, IntPtr clientData, OnLoginCallbackInternal completionDelegate);
}
