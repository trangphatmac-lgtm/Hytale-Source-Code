using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Connect;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnQueryExternalAccountMappingsCallbackInternal(ref QueryExternalAccountMappingsCallbackInfoInternal data);
