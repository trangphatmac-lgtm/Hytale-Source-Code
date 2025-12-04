using System.Runtime.InteropServices;

namespace Epic.OnlineServices.PlayerDataStorage;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate ReadResult OnReadFileDataCallbackInternal(ref ReadFileDataCallbackInfoInternal data);
