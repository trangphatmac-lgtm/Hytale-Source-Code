using System.Runtime.InteropServices;
using HytaleClient.Math;

namespace HytaleClient.Graphics.BlockyModels;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32)]
internal struct ModelVertex
{
	public static readonly int Size = Marshal.SizeOf(typeof(ModelVertex));

	public uint NodeIndex;

	public uint AtlasIndexAndShadingModeAndGradienId;

	public Vector3 Position;

	public Vector2 TextureCoordinates;
}
