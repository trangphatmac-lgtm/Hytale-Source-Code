using System.Runtime.InteropServices;

namespace Epic.OnlineServices.TitleStorage;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate ReadResult OnReadFileDataCallbackInternal(ref ReadFileDataCallbackInfoInternal data);
