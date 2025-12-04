using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Sanctions;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnQueryActivePlayerSanctionsCallbackInternal(ref QueryActivePlayerSanctionsCallbackInfoInternal data);
