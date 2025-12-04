using System;

namespace HytaleClient.Graphics;

public delegate void glTexSubImage3D(GL target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, GL format, GL type, IntPtr pixels);
