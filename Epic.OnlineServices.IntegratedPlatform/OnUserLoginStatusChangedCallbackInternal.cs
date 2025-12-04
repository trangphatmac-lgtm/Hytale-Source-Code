using System.Runtime.InteropServices;

namespace Epic.OnlineServices.IntegratedPlatform;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnUserLoginStatusChangedCallbackInternal(ref UserLoginStatusChangedCallbackInfoInternal data);
