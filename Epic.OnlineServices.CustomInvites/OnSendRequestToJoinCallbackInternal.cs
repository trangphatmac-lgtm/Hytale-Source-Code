using System.Runtime.InteropServices;

namespace Epic.OnlineServices.CustomInvites;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnSendRequestToJoinCallbackInternal(ref SendRequestToJoinCallbackInfoInternal data);
