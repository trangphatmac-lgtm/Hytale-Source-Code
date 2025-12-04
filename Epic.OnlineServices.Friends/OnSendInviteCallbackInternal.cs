using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Friends;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnSendInviteCallbackInternal(ref SendInviteCallbackInfoInternal data);
