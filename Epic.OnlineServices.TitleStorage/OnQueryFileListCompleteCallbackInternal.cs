using System.Runtime.InteropServices;

namespace Epic.OnlineServices.TitleStorage;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnQueryFileListCompleteCallbackInternal(ref QueryFileListCallbackInfoInternal data);
