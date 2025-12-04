using HytaleClient.Math;

namespace HytaleClient.Graphics.Particles;

internal struct ParticleAttractor
{
	public Vector3 Position;

	public float TrailPositionMultiplier;

	public Vector3 RadialAxis;

	public Vector3 DampingMultiplier;

	public float Radius;

	public float RadialAcceleration;

	public float RadialTangentAcceleration;

	public float RadialImpulse;

	public float RadialTangentImpulse;

	public Vector3 LinearAcceleration;

	public Vector3 LinearImpulse;

	public void Apply(Vector3 position, Vector3 offsetPosition, ref Vector3 acceleration, ref Vector3 impulse)
	{
		offsetPosition = Position + offsetPosition * TrailPositionMultiplier;
		float num = 0f;
		Vector3 vector = Vector3.Zero;
		if (RadialAxis != Vector3.Zero)
		{
			Vector3 vector2 = RadialAxis * Vector3.Dot(RadialAxis, position - offsetPosition);
			vector = offsetPosition + vector2;
			num = Vector3.Distance(vector, position);
		}
		else
		{
			num = Vector3.Distance(offsetPosition, position);
		}
		if (Radius != 0f && num > Radius)
		{
			return;
		}
		acceleration *= DampingMultiplier;
		if (num != 0f && (RadialAcceleration != 0f || RadialTangentAcceleration != 0f || RadialImpulse != 0f || RadialTangentImpulse != 0f))
		{
			Vector3 zero = Vector3.Zero;
			Vector3 zero2 = Vector3.Zero;
			if (RadialAxis != Vector3.Zero)
			{
				zero = Vector3.Normalize(vector - position);
				zero2 = Vector3.Cross(RadialAxis, zero);
			}
			else
			{
				zero = Vector3.Normalize(offsetPosition - position);
				Quaternion rotation = Quaternion.CreateFromVectors(Vector3.Forward, offsetPosition - position);
				Vector3 vector3 = Vector3.Transform(Vector3.UnitY, rotation);
				vector3.Normalize();
				zero2 = Vector3.Cross(vector3, zero);
			}
			if (zero2 != Vector3.Zero)
			{
				zero2.Normalize();
			}
			acceleration += zero * (0f - RadialAcceleration) + zero2 * RadialTangentAcceleration;
			impulse += zero * (0f - RadialImpulse) + zero2 * RadialTangentImpulse;
		}
		acceleration += LinearAcceleration;
		impulse += LinearImpulse;
	}
}
