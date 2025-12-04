using System.Runtime.InteropServices;
using Epic.OnlineServices.AntiCheatCommon;

namespace Epic.OnlineServices.AntiCheatClient;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnPeerAuthStatusChangedCallbackInternal(ref OnClientAuthStatusChangedCallbackInfoInternal data);
