using System.Runtime.InteropServices;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Map;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct StaticBlockyModelVertex
{
	public byte NodeIndex;

	public Vector3 Position;

	public Vector3 Normal;

	public ShadingMode ShadingMode;

	public uint DoubleSided;

	public Vector2 TextureCoordinates;

	public uint TintColorAndEffect;

	public uint GlowColorAndSunlight;
}
