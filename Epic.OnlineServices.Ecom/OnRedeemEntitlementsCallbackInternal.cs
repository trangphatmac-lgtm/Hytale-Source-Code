using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Ecom;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnRedeemEntitlementsCallbackInternal(ref RedeemEntitlementsCallbackInfoInternal data);
