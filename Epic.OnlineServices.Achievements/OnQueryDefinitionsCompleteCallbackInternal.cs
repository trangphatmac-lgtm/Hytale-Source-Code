using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Achievements;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnQueryDefinitionsCompleteCallbackInternal(ref OnQueryDefinitionsCompleteCallbackInfoInternal data);
