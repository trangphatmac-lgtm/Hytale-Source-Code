using System.Runtime.InteropServices;

namespace Epic.OnlineServices.PlayerDataStorage;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnFileTransferProgressCallbackInternal(ref FileTransferProgressCallbackInfoInternal data);
