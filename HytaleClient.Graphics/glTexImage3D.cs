using System;

namespace HytaleClient.Graphics;

public delegate void glTexImage3D(GL target, int level, int internalFormat, int width, int height, int depth, int border, GL format, GL type, IntPtr pixels);
