using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Auth;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnVerifyIdTokenCallbackInternal(ref VerifyIdTokenCallbackInfoInternal data);
