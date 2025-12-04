using System.Runtime.InteropServices;

namespace Epic.OnlineServices.AntiCheatClient;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnClientIntegrityViolatedCallbackInternal(ref OnClientIntegrityViolatedCallbackInfoInternal data);
