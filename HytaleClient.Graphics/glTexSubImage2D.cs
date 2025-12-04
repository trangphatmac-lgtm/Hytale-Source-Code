using System;

namespace HytaleClient.Graphics;

public delegate void glTexSubImage2D(GL target, int level, int xoffset, int yoffset, int width, int height, GL format, GL type, IntPtr pixels);
