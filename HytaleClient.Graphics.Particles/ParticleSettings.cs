using HytaleClient.Math;

namespace HytaleClient.Graphics.Particles;

internal class ParticleSettings
{
	public enum ScaleRatioConstraint
	{
		OneToOne,
		Preserved,
		None
	}

	public enum UVOptions
	{
		None,
		RandomFlipU,
		RandomFlipV,
		RandomFlipUV,
		FlipU,
		FlipV,
		FlipUV
	}

	public enum SoftParticles
	{
		Enable,
		Disable,
		Require
	}

	public struct RangeKeyframe
	{
		public byte Time;

		public byte Min;

		public byte Max;
	}

	public struct ScaleKeyframe
	{
		public byte Time;

		public Vector2 Min;

		public Vector2 Max;
	}

	public struct RotationKeyframe
	{
		public byte Time;

		public Vector3 Min;

		public Vector3 Max;
	}

	public struct ColorKeyframe
	{
		public byte Time;

		public UInt32Color Color;
	}

	public struct OpacityKeyframe
	{
		public byte Time;

		public float Opacity;
	}

	public const int DefaultFrameIndex = 0;

	public static readonly Vector2 DefaultScale = Vector2.One;

	public static readonly Quaternion DefaultRotation = Quaternion.Identity;

	public static readonly UInt32Color DefaultColor = UInt32Color.White;

	public const float DefaultOpacity = 1f;

	public const bool DefaultRandomTextureInverse = false;

	public string Id;

	public string TexturePath;

	public UShortVector2 FrameSize;

	public Rectangle ImageLocation;

	public SoftParticles SoftParticlesOption = SoftParticles.Enable;

	public float SoftParticlesFadeFactor = 1f;

	public bool UseSpriteBlending;

	public UVOptions UVOption;

	public ScaleRatioConstraint ScaleRatio = ScaleRatioConstraint.OneToOne;

	public ScaleKeyframe[] ScaleKeyframes;

	public RotationKeyframe[] RotationKeyframes;

	public RangeKeyframe[] TextureIndexKeyFrames;

	public ColorKeyframe[] ColorKeyframes;

	public OpacityKeyframe[] OpacityKeyframes;

	public byte ScaleKeyFrameCount;

	public byte RotationKeyFrameCount;

	public byte TextureKeyFrameCount;

	public byte ColorKeyFrameCount;

	public byte OpacityKeyFrameCount;
}
