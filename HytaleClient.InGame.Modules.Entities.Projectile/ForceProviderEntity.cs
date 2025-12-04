using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class ForceProviderEntity : ForceProviderStandard
{
	protected Entity _entity;

	public ForceProviderStandardState ForceProviderStandardState;

	public float Density = 700f;

	public ForceProviderEntity(Entity entity)
	{
		_entity = entity;
	}

	public override ForceProviderStandardState GetForceProviderStandardState()
	{
		return ForceProviderStandardState;
	}

	public override float GetMass(float volume)
	{
		return volume * GetDensity();
	}

	public override float GetVolume()
	{
		return _entity.Hitbox.GetVolume();
	}

	public override float GetProjectedArea(PhysicsBodyState bodyState, float speed)
	{
		float num = PhysicsMath.ComputeProjectedArea(bodyState.Velocity, _entity.Hitbox);
		return (num == 0f) ? 0f : (num / speed);
	}

	public override float GetDensity()
	{
		return Density;
	}

	public override float GetFrictionCoefficient()
	{
		return 0f;
	}
}
