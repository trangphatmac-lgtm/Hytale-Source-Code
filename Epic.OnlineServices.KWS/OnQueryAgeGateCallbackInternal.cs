using System.Runtime.InteropServices;

namespace Epic.OnlineServices.KWS;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnQueryAgeGateCallbackInternal(ref QueryAgeGateCallbackInfoInternal data);
