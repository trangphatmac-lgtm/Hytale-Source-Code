using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTC;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnParticipantStatusChangedCallbackInternal(ref ParticipantStatusChangedCallbackInfoInternal data);
