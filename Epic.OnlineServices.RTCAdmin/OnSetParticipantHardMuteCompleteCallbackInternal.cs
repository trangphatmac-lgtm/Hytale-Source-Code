using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCAdmin;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnSetParticipantHardMuteCompleteCallbackInternal(ref SetParticipantHardMuteCompleteCallbackInfoInternal data);
