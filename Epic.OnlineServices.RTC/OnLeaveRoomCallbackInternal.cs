using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTC;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnLeaveRoomCallbackInternal(ref LeaveRoomCallbackInfoInternal data);
