using System.Runtime.InteropServices;

namespace Epic.OnlineServices.UI;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnHideFriendsCallbackInternal(ref HideFriendsCallbackInfoInternal data);
