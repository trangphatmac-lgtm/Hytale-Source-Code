using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Presence;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnJoinGameAcceptedCallbackInternal(ref JoinGameAcceptedCallbackInfoInternal data);
