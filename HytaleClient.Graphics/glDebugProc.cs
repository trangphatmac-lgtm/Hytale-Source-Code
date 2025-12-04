using System;

namespace HytaleClient.Graphics;

public delegate void glDebugProc(GL source, GL type, uint id, GL severity, int length, IntPtr message, IntPtr userParam);
