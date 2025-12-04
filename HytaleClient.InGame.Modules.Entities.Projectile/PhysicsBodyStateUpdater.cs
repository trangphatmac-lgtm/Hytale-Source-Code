using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class PhysicsBodyStateUpdater
{
	protected const float MinVelocity = 1E-06f;

	protected Vector3 _acceleration = default(Vector3);

	protected readonly ForceAccumulator _accumulator = new ForceAccumulator();

	public virtual void Update(PhysicsBodyState before, PhysicsBodyState after, float mass, float dt, bool onGround, ForceProvider[] forceProvider)
	{
		ComputeAcceleration(before, onGround, forceProvider, mass, dt);
		UpdatePositionBeforeVelocity(before, after, dt);
		UpdateAndClampVelocity(before, after, dt);
	}

	protected static void UpdatePositionBeforeVelocity(PhysicsBodyState before, PhysicsBodyState after, float dt)
	{
		after.Position = before.Position + before.Velocity * dt;
	}

	protected static void UpdatePositionAfterVelocity(PhysicsBodyState before, PhysicsBodyState after, float dt)
	{
		after.Position = before.Position + after.Velocity * dt;
	}

	protected void UpdateAndClampVelocity(PhysicsBodyState before, PhysicsBodyState after, float dt)
	{
		UpdateVelocity(before, after, dt);
		after.Velocity = after.Velocity.ClipToZero(1E-06f);
	}

	protected void UpdateVelocity(PhysicsBodyState before, PhysicsBodyState after, float dt)
	{
		after.Velocity = before.Velocity + _acceleration * dt;
	}

	protected void ComputeAcceleration(float mass)
	{
		_acceleration = _accumulator.Force * (1f / mass);
	}

	protected void ComputeAcceleration(PhysicsBodyState state, bool onGround, ForceProvider[] forceProviders, float mass, float timeStep)
	{
		_accumulator.ComputeResultingForce(state, onGround, forceProviders, mass, timeStep);
		ComputeAcceleration(mass);
	}

	protected void AssignAcceleration(PhysicsBodyState state)
	{
		state.Velocity = _acceleration;
	}

	protected void AddAcceleration(PhysicsBodyState state, float scale)
	{
		state.Velocity += _acceleration * scale;
	}

	protected void AddAcceleration(PhysicsBodyState state)
	{
		state.Velocity += _acceleration;
	}

	protected void ConvertAccelerationToVelocity(PhysicsBodyState before, PhysicsBodyState after, float scale)
	{
		after.Velocity = after.Velocity * scale + before.Velocity;
		after.Velocity = after.Velocity.ClipToZero(1E-06f);
	}
}
