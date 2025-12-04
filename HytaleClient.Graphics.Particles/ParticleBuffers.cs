using HytaleClient.Math;

namespace HytaleClient.Graphics.Particles;

internal struct ParticleBuffers : IFXDataStorage
{
	public struct ParticleRenderData
	{
		public Vector3 Position;

		public Vector3 Velocity;

		public Vector3 AttractorVelocity;

		public Quaternion Rotation;

		public uint Color;

		public byte TargetTextureIndex;

		public byte PrevTargetTextureIndex;

		public ushort TargetTextureBlendProgress;

		public byte BoolData;

		public ushort Seed;
	}

	public struct ParticleSimulationData
	{
		public Vector3 SpawnerPositionAtSpawn;

		public Quaternion RotationOffset;

		public Vector2 ScaleStep;

		public float ScaleNextKeyframeTime;

		public byte ScaleAnimationIndex;

		public Vector3 RotationStep;

		public Vector3 CurrentRotation;

		public float RotationNextKeyframeTime;

		public byte RotationAnimationIndex;

		public float TextureNextKeyframeTime;

		public byte TextureAnimationIndex;
	}

	public struct ParticleLifeData
	{
		public float LifeSpanTimer;

		public float LifeSpan;
	}

	public const int InverseUTextureBitId = 0;

	public const int InverseVTextureBitId = 1;

	public const int CollisionBitId = 2;

	public int Count;

	public ParticleLifeData[] Life;

	public Vector2[] Scale;

	public float[] ScaleRatio;

	public ParticleSimulationData[] Data0;

	public ParticleRenderData[] Data1;

	public void Initialize(int maxParticles)
	{
		Count = maxParticles;
		Life = new ParticleLifeData[maxParticles];
		Scale = new Vector2[maxParticles];
		ScaleRatio = new float[maxParticles];
		Data0 = new ParticleSimulationData[maxParticles];
		Data1 = new ParticleRenderData[maxParticles];
	}

	public void Release()
	{
	}
}
