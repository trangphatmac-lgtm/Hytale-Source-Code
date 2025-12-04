using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Lobby;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnQueryInvitesCallbackInternal(ref QueryInvitesCallbackInfoInternal data);
