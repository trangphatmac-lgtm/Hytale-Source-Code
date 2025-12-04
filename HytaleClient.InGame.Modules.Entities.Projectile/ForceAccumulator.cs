using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

public class ForceAccumulator
{
	public float Speed;

	public Vector3 Force = default(Vector3);

	public Vector3 ResistanceForceLimit = default(Vector3);

	public void Initialize(PhysicsBodyState state, float mass, float timeStep)
	{
		Force = Vector3.Zero;
		Speed = state.Velocity.Length();
		ResistanceForceLimit = state.Velocity * ((0f - mass) / timeStep);
	}

	public void ComputeResultingForce(PhysicsBodyState state, bool onGround, ForceProvider[] forceProviders, float mass, float timeStep)
	{
		Initialize(state, mass, timeStep);
		foreach (ForceProvider forceProvider in forceProviders)
		{
			forceProvider.Update(state, this, onGround);
		}
	}
}
