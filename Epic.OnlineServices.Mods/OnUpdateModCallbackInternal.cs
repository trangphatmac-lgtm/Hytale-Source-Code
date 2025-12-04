using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Mods;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnUpdateModCallbackInternal(ref UpdateModCallbackInfoInternal data);
