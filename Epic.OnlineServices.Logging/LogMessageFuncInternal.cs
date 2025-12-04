using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Logging;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void LogMessageFuncInternal(ref LogMessageInternal message);
