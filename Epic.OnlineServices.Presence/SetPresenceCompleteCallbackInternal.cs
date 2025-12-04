using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Presence;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void SetPresenceCompleteCallbackInternal(ref SetPresenceCallbackInfoInternal data);
