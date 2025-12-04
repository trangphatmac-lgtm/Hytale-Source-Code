using System;

namespace HytaleClient.Graphics;

public delegate void glTexImage1D(GL target, int level, int internalFormat, int width, int border, GL format, GL type, IntPtr pixels);
