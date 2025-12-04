using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Connect;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnCreateUserCallbackInternal(ref CreateUserCallbackInfoInternal data);
