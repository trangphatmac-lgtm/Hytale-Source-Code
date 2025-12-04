using System.Runtime.InteropServices;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Map;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32)]
internal struct ChunkVertex
{
	public static readonly int Size = Marshal.SizeOf(typeof(ChunkVertex));

	public ShortVector3 PositionPacked;

	public ushort DoubleSidedAndBlockId;

	public UShortVector2 TextureCoordinates;

	public UShortVector2 MaskTextureCoordinates;

	public uint NormalAndNodeIndex;

	public uint TintColorAndEffectAndShadingMode;

	public uint GlowColorAndSunlight;

	public uint UseBillboard;
}
