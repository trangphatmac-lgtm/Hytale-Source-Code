using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Lobby;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnKickMemberCallbackInternal(ref KickMemberCallbackInfoInternal data);
