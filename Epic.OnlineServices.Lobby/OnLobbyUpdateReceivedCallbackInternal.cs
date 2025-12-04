using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Lobby;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnLobbyUpdateReceivedCallbackInternal(ref LobbyUpdateReceivedCallbackInfoInternal data);
