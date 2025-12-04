using System.Runtime.InteropServices;

namespace Epic.OnlineServices.UserInfo;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnQueryUserInfoCallbackInternal(ref QueryUserInfoCallbackInfoInternal data);
