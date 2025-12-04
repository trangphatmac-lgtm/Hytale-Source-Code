using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCData;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnUpdateSendingCallbackInternal(ref UpdateSendingCallbackInfoInternal data);
