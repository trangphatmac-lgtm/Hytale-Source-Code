using System.Runtime.InteropServices;
using HytaleClient.Math;

namespace HytaleClient.Graphics;

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 64)]
internal struct FXVertex
{
	public static readonly int Size = Marshal.SizeOf(typeof(FXVertex));

	public static readonly int ConfigBitShiftQuadType = 0;

	public static readonly int ConfigBitShiftLinearFiltering = 3;

	public static readonly int ConfigBitShiftSoftParticles = 4;

	public static readonly int ConfigBitShiftInvertUTexture = 5;

	public static readonly int ConfigBitShiftInvertVTexture = 6;

	public static readonly int ConfigBitShiftBlendMode = 7;

	public static readonly int ConfigBitShiftIsFirstPerson = 9;

	public static readonly int ConfigBitShiftNextFree = 10;

	public static readonly int ConfigBitShiftDrawId = 16;

	public static readonly uint ConfigBitMaskQuadType = 7u;

	public static readonly uint ConfigBitQuadTypeOriented = 0u;

	public static readonly uint ConfigBitQuadTypeBillboard = 1u;

	public static readonly uint ConfigBitQuadTypeBillboardY = 2u;

	public static readonly uint ConfigBitQuadTypeBillboardVelocity = 3u;

	public static readonly uint ConfigBitQuadTypeVelocity = 4u;

	public static readonly uint ConfigBitMaskBlendMode = 1u;

	public static readonly uint ConfigBitBlendModeLinear = 0u;

	public static readonly uint ConfigBitBlendModeAdd = 1u;

	[FieldOffset(0)]
	public uint Config;

	[FieldOffset(4)]
	public uint TextureInfo;

	[FieldOffset(8)]
	public uint Color;

	[FieldOffset(12)]
	public Vector3 Position;

	[FieldOffset(24)]
	public Vector2 Scale;

	[FieldOffset(32)]
	public Vector3 Velocity;

	[FieldOffset(44)]
	public Vector4 Rotation;

	[FieldOffset(60)]
	public uint SeedAndLifeRatio;

	[FieldOffset(4)]
	public Vector3 TopLeftPosition;

	[FieldOffset(16)]
	public Vector3 BottomLeftPosition;

	[FieldOffset(28)]
	public Vector3 TopRightPosition;

	[FieldOffset(40)]
	public Vector3 BottomRightPosition;

	[FieldOffset(52)]
	public float Length;
}
