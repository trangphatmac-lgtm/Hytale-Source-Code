using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Ecom;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnCheckoutCallbackInternal(ref CheckoutCallbackInfoInternal data);
