using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Platform;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void ReleaseMemoryFunc(IntPtr pointer);
