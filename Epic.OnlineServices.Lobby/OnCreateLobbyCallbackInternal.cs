using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Lobby;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnCreateLobbyCallbackInternal(ref CreateLobbyCallbackInfoInternal data);
