using System.Runtime.InteropServices;

namespace Epic.OnlineServices.P2P;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnPeerConnectionInterruptedCallbackInternal(ref OnPeerConnectionInterruptedInfoInternal data);
