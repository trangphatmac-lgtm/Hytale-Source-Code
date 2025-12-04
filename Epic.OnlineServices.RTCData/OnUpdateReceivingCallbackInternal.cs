using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCData;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnUpdateReceivingCallbackInternal(ref UpdateReceivingCallbackInfoInternal data);
