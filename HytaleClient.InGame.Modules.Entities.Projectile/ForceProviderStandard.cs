using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal abstract class ForceProviderStandard : ForceProvider
{
	protected Vector3 _dragForce = default(Vector3);

	public abstract float GetMass(float volume);

	public abstract float GetVolume();

	public abstract float GetDensity();

	public abstract float GetProjectedArea(PhysicsBodyState bodyState, float speed);

	public abstract float GetFrictionCoefficient();

	public abstract ForceProviderStandardState GetForceProviderStandardState();

	public void Update(PhysicsBodyState bodyState, ForceAccumulator accumulator, bool onGround)
	{
		ForceProviderStandardState forceProviderStandardState = GetForceProviderStandardState();
		Vector3 externalForce = forceProviderStandardState.ExternalForce;
		float y = externalForce.Y;
		accumulator.Force += externalForce;
		float speed = accumulator.Speed;
		float num = forceProviderStandardState.DragCoefficient * GetProjectedArea(bodyState, speed) * speed;
		_dragForce = bodyState.Velocity * (0f - num);
		ClipForce(ref _dragForce, accumulator.ResistanceForceLimit);
		accumulator.Force += _dragForce;
		float num2 = (0f - forceProviderStandardState.Gravity) * GetMass(GetVolume());
		if (onGround)
		{
			float num3 = (num2 + y) * GetFrictionCoefficient();
			if (speed > 0f && num3 > 0f)
			{
				num3 /= speed;
				accumulator.Force.X -= bodyState.Velocity.X * num3;
				accumulator.Force.Z -= bodyState.Velocity.Z * num3;
			}
		}
		else
		{
			accumulator.Force.Y += num2;
		}
		if (forceProviderStandardState.DisplacedMass != 0f)
		{
			accumulator.Force.Y += forceProviderStandardState.DisplacedMass * forceProviderStandardState.Gravity;
		}
	}

	public void ClipForce(ref Vector3 value, Vector3 threshold)
	{
		if (threshold.X < 0f)
		{
			if (value.X < threshold.X)
			{
				value.X = threshold.X;
			}
		}
		else if (value.X > threshold.X)
		{
			value.X = threshold.X;
		}
		if (threshold.Y < 0f)
		{
			if (value.Y < threshold.Y)
			{
				value.Y = threshold.Y;
			}
		}
		else if (value.Y > threshold.Y)
		{
			value.Y = threshold.Y;
		}
		if (threshold.Z < 0f)
		{
			if (value.Z < threshold.Z)
			{
				value.Z = threshold.Z;
			}
		}
		else if (value.Z > threshold.Z)
		{
			value.Z = threshold.Z;
		}
	}
}
