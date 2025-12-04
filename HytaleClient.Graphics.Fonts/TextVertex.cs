using System.Runtime.InteropServices;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Fonts;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32)]
internal struct TextVertex
{
	public static readonly int Size = Marshal.SizeOf(typeof(TextVertex));

	public Vector3 Position;

	public Vector2 TextureCoordinates;

	public uint FillColor;

	public uint OutlineColor;
}
