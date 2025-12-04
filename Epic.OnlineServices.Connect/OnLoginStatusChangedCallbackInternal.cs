using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Connect;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnLoginStatusChangedCallbackInternal(ref LoginStatusChangedCallbackInfoInternal data);
