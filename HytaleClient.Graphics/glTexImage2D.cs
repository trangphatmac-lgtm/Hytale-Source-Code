using System;

namespace HytaleClient.Graphics;

public delegate void glTexImage2D(GL target, int level, int internalFormat, int width, int height, int border, GL format, GL type, IntPtr pixels);
