using System;

namespace HytaleClient.Graphics;

public delegate void glGetSynciv(IntPtr sync, GL pname, IntPtr bufSize, IntPtr length, out IntPtr values);
