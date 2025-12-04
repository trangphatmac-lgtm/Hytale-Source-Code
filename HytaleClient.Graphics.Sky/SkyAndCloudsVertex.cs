using System.Runtime.InteropServices;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Sky;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32)]
internal struct SkyAndCloudsVertex
{
	public static readonly int Size = Marshal.SizeOf(typeof(SkyAndCloudsVertex));

	public Vector3 Position;

	public Vector2 TextureCoordinates;
}
