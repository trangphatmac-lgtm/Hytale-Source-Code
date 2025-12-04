using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Auth;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnLoginStatusChangedCallbackInternal(ref LoginStatusChangedCallbackInfoInternal data);
