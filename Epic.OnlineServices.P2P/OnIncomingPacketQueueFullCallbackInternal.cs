using System.Runtime.InteropServices;

namespace Epic.OnlineServices.P2P;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnIncomingPacketQueueFullCallbackInternal(ref OnIncomingPacketQueueFullInfoInternal data);
