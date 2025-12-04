using System.Runtime.InteropServices;
using Epic.OnlineServices.AntiCheatCommon;

namespace Epic.OnlineServices.AntiCheatServer;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnClientAuthStatusChangedCallbackInternal(ref OnClientAuthStatusChangedCallbackInfoInternal data);
