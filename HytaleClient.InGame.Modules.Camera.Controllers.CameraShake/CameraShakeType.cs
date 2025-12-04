using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Camera.Controllers.CameraShake;

public class CameraShakeType
{
	public class Vec3Noise
	{
		private readonly Noise1 _noiseX;

		private readonly Noise1 _noiseY;

		private readonly Noise1 _noiseZ;

		public Vec3Noise(Noise1 noiseX, Noise1 noiseY, Noise1 noiseZ)
		{
			_noiseX = noiseX;
			_noiseY = noiseY;
			_noiseZ = noiseZ;
		}

		public Vector3 Eval(int seed, float t)
		{
			float x = _noiseX.Eval(seed, t);
			float y = _noiseY.Eval(seed, t);
			float z = _noiseZ.Eval(seed, t);
			return new Vector3(x, y, z);
		}
	}

	public static readonly CameraShakeType None = new CameraShakeType(0f, 0f, continuous: true, null, null, new OffsetNoise(), new RotationNoise());

	public readonly float Duration;

	public readonly float StartTime;

	public readonly float EaseIn;

	public readonly float EaseOut;

	public readonly bool Continuous;

	public readonly Easing.EasingType EaseInType;

	public readonly Easing.EasingType EaseOutType;

	public readonly Vec3Noise Offset;

	public readonly Vec3Noise Rotation;

	public CameraShakeType(CameraShakeConfig config)
		: this(config.Duration, config.StartTime, config.Continuous, config.EaseIn, config.EaseOut, config.Offset, config.Rotation)
	{
	}

	public CameraShakeType(float duration, float startTime, bool continuous, EasingConfig easeIn, EasingConfig easeOut, OffsetNoise offset, RotationNoise rotation)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		Duration = duration;
		StartTime = startTime;
		Continuous = continuous;
		EaseIn = easeIn?.Time ?? 0f;
		EaseOut = easeOut?.Time ?? 0f;
		EaseInType = ((easeIn != null) ? ((Easing.EasingType)easeIn.Type) : Easing.EasingType.Linear);
		EaseOutType = ((easeOut != null) ? ((Easing.EasingType)easeOut.Type) : Easing.EasingType.Linear);
		Offset = CreateVecNoise(offset.X, offset.Y, offset.Z);
		Rotation = CreateVecNoise(rotation.Pitch, rotation.Yaw, rotation.Roll);
	}

	private static Vec3Noise CreateVecNoise(NoiseConfig[] x, NoiseConfig[] y, NoiseConfig[] z)
	{
		Noise1 noiseX = Noise1Combiner.Summed(Noise1Helper.CreateNoises(x));
		Noise1 noiseY = Noise1Combiner.Summed(Noise1Helper.CreateNoises(y));
		Noise1 noiseZ = Noise1Combiner.Summed(Noise1Helper.CreateNoises(z));
		return new Vec3Noise(noiseX, noiseY, noiseZ);
	}
}
