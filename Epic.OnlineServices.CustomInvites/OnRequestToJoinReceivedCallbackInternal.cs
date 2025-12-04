using System.Runtime.InteropServices;

namespace Epic.OnlineServices.CustomInvites;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnRequestToJoinReceivedCallbackInternal(ref RequestToJoinReceivedCallbackInfoInternal data);
