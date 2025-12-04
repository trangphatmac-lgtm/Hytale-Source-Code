using System;

namespace HytaleClient.Graphics;

public delegate void glReadPixels(int x, int y, int width, int height, GL format, GL type, IntPtr pixels);
