using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCAudio;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnAudioInputStateCallbackInternal(ref AudioInputStateCallbackInfoInternal data);
