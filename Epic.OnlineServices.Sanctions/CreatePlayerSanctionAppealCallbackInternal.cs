using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Sanctions;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void CreatePlayerSanctionAppealCallbackInternal(ref CreatePlayerSanctionAppealCallbackInfoInternal data);
