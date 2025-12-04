using System.Runtime.InteropServices;
using Epic.OnlineServices.AntiCheatCommon;

namespace Epic.OnlineServices.AntiCheatServer;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnMessageToClientCallbackInternal(ref OnMessageToClientCallbackInfoInternal data);
