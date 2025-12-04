using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Lobby;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnRTCRoomConnectionChangedCallbackInternal(ref RTCRoomConnectionChangedCallbackInfoInternal data);
