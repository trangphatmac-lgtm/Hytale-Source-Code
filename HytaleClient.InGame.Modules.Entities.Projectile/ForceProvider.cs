namespace HytaleClient.InGame.Modules.Entities.Projectile;

public interface ForceProvider
{
	void Update(PhysicsBodyState state, ForceAccumulator accumulator, bool onGround);
}
