using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Sessions;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnSessionInviteAcceptedCallbackInternal(ref SessionInviteAcceptedCallbackInfoInternal data);
