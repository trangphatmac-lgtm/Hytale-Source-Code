using HytaleClient.Math;

namespace HytaleClient.Graphics.Particles;

internal class ParticleSpawnerSettings
{
	public enum Shape
	{
		Sphere,
		Cube,
		Circle,
		FullCube
	}

	public struct InitialVelocity
	{
		public float Yaw;

		public float Pitch;

		public float Speed;

		public InitialVelocity(float yaw, float pitch, float speed)
		{
			Yaw = yaw;
			Pitch = pitch;
			Speed = speed;
		}
	}

	public struct UVMotionParams
	{
		public string TexturePath;

		public int TextureId;

		public bool AddRandomUVOffset;

		public Vector2 Speed;

		public float Strength;

		public UVMotionCurveType StrengthCurveType;

		public float Scale;
	}

	public enum UVMotionCurveType
	{
		Constant,
		IncreaseLinear,
		IncreaseQuartIn,
		IncreaseQuartInOut,
		IncreaseQuartOut,
		DecreaseLinear,
		DecreaseQuartIn,
		DecreaseQuartInOut,
		DecreaseQuartOut
	}

	public struct IntersectionHighlightParams
	{
		public float Threshold;

		public Vector3 Color;
	}

	public const int DefaultMaxConcurrentParticles = 512;

	public static readonly Vector2 DefaultParticleLifeSpan = new Vector2(2f, 2f);

	public static readonly Vector2 DefaultSpawnRate = Vector2.One;

	public ParticleSettings ParticleSettings;

	public FXSystem.RenderMode RenderMode = FXSystem.RenderMode.BlendLinear;

	public ParticleFXSystem.ParticleRotationInfluence RotationInfluence = ParticleFXSystem.ParticleRotationInfluence.None;

	public ParticleFXSystem.ParticleCollisionBlockType ParticleCollisionBlockType = ParticleFXSystem.ParticleCollisionBlockType.None;

	public ParticleFXSystem.ParticleCollisionAction ParticleCollisionAction = ParticleFXSystem.ParticleCollisionAction.Expire;

	public ParticleFXSystem.ParticleRotationInfluence ParticleCollisionRotationInfluence = ParticleFXSystem.ParticleRotationInfluence.None;

	public float LightInfluence = 0f;

	public float TrailSpawnerPositionMultiplier;

	public float TrailSpawnerRotationMultiplier;

	public bool ParticleRotateWithSpawner = false;

	public bool LinearFiltering = false;

	public bool IsLowRes = false;

	public UVMotionParams UVMotion;

	public IntersectionHighlightParams IntersectionHighlight;

	public float CameraOffset;

	public float VelocityStretchMultiplier;

	public float LifeSpan = 0f;

	public Point TotalParticles = new Point(-1, -1);

	public int MaxConcurrentParticles = 512;

	public Vector2 ParticleLifeSpan = DefaultParticleLifeSpan;

	public Vector2 SpawnRate = DefaultSpawnRate;

	public bool SpawnBurst = false;

	public Vector2 WaveDelay = Vector2.Zero;

	public InitialVelocity InitialVelocityMin;

	public InitialVelocity InitialVelocityMax;

	public Shape EmitShape = Shape.Sphere;

	public Vector3 EmitOffsetMin;

	public Vector3 EmitOffsetMax;

	public bool UseEmitDirection;

	public ParticleAttractor[] Attractors = new ParticleAttractor[0];
}
