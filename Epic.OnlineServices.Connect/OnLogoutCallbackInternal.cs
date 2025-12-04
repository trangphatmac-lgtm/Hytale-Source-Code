using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Connect;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnLogoutCallbackInternal(ref LogoutCallbackInfoInternal data);
