using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Sessions;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void SessionSearchOnFindCallbackInternal(ref SessionSearchFindCallbackInfoInternal data);
