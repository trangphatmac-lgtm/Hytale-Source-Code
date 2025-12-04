using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class ForceProviderStandardState
{
	public float DisplacedMass;

	public float DragCoefficient;

	public float Gravity;

	public Vector3 NextTickVelocity = default(Vector3);

	public Vector3 ExternalVelocity = default(Vector3);

	public Vector3 ExternalAcceleration = default(Vector3);

	public Vector3 ExternalForce = default(Vector3);

	public Vector3 ExternalImpulse = default(Vector3);

	public ForceProviderStandardState()
	{
		NextTickVelocity = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
	}

	public void ConvertToForces(float dt, float mass)
	{
		ExternalForce += ExternalAcceleration * (1f / mass);
		ExternalForce += ExternalImpulse * (1f / dt);
		ExternalAcceleration = Vector3.Zero;
		ExternalImpulse = Vector3.Zero;
	}

	public void UpdateVelocity(ref Vector3 velocity)
	{
		if (NextTickVelocity.X < float.MaxValue)
		{
			velocity = NextTickVelocity;
			NextTickVelocity = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		}
		velocity += ExternalVelocity;
		ExternalVelocity = Vector3.Zero;
	}

	public void Clear()
	{
		ExternalForce = Vector3.Zero;
	}
}
