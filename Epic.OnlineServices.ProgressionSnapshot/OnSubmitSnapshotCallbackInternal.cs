using System.Runtime.InteropServices;

namespace Epic.OnlineServices.ProgressionSnapshot;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnSubmitSnapshotCallbackInternal(ref SubmitSnapshotCallbackInfoInternal data);
