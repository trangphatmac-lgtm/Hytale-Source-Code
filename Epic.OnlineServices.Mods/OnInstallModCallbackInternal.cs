using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Mods;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnInstallModCallbackInternal(ref InstallModCallbackInfoInternal data);
