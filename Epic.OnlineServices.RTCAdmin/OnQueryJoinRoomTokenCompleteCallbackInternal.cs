using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCAdmin;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnQueryJoinRoomTokenCompleteCallbackInternal(ref QueryJoinRoomTokenCompleteCallbackInfoInternal data);
