using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCData;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnDataReceivedCallbackInternal(ref DataReceivedCallbackInfoInternal data);
