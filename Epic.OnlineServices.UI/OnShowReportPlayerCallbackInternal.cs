using System.Runtime.InteropServices;

namespace Epic.OnlineServices.UI;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnShowReportPlayerCallbackInternal(ref OnShowReportPlayerCallbackInfoInternal data);
