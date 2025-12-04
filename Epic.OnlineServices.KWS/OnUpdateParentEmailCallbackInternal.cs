using System.Runtime.InteropServices;

namespace Epic.OnlineServices.KWS;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnUpdateParentEmailCallbackInternal(ref UpdateParentEmailCallbackInfoInternal data);
