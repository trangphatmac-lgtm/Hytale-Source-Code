using System.Runtime.InteropServices;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Batcher2D;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 64)]
internal struct Batcher2DVertex
{
	public static readonly int Size = Marshal.SizeOf(typeof(Batcher2DVertex));

	public Vector3 Position;

	public UShortVector2 TextureCoordinates;

	public UShortVector4 Scissor;

	public Vector4 MaskTextureArea;

	public UShortVector4 MaskBounds;

	public UInt32Color FillColor;

	public UInt32Color OutlineColor;

	public byte FillThreshold;

	public byte FillBlurAmount;

	public byte OutlineThreshold;

	public byte OutlineBlurAmount;

	public uint FontId;
}
