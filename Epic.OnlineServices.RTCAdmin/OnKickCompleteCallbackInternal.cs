using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCAdmin;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnKickCompleteCallbackInternal(ref KickCompleteCallbackInfoInternal data);
