using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Sessions;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnJoinSessionAcceptedCallbackInternal(ref JoinSessionAcceptedCallbackInfoInternal data);
