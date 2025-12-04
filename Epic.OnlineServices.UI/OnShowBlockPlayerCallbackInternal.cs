using System.Runtime.InteropServices;

namespace Epic.OnlineServices.UI;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnShowBlockPlayerCallbackInternal(ref OnShowBlockPlayerCallbackInfoInternal data);
