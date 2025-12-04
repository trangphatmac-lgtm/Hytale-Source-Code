using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Sessions;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnRejectInviteCallbackInternal(ref RejectInviteCallbackInfoInternal data);
