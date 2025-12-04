using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCAudio;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnUpdateReceivingCallbackInternal(ref UpdateReceivingCallbackInfoInternal data);
