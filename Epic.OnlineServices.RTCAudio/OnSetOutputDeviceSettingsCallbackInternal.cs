using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCAudio;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnSetOutputDeviceSettingsCallbackInternal(ref OnSetOutputDeviceSettingsCallbackInfoInternal data);
