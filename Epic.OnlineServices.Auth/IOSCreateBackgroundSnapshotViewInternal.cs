using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Auth;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate IntPtr IOSCreateBackgroundSnapshotViewInternal(IntPtr context);
