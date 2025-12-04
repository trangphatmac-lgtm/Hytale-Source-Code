namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class PhysicsBodyStateUpdaterSymplecticEuler : PhysicsBodyStateUpdater
{
	public override void Update(PhysicsBodyState before, PhysicsBodyState after, float mass, float dt, bool onGround, ForceProvider[] forceProvider)
	{
		ComputeAcceleration(before, onGround, forceProvider, mass, dt);
		UpdateAndClampVelocity(before, after, dt);
		PhysicsBodyStateUpdater.UpdatePositionAfterVelocity(before, after, dt);
	}
}
