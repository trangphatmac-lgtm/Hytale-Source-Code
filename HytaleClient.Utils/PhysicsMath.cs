using System;
using HytaleClient.InGame.Modules.Entities.Projectile;
using HytaleClient.Math;

namespace HytaleClient.Utils;

internal static class PhysicsMath
{
	public const float GRAVITY_ACCELERATION = 32f;

	public const float DensityAir = 1.2f;

	public const float DensityWater = 998f;

	public const float AIR_DENSITY = 0.001225f;

	public const float HeadingDirection = 1f;

	public static float GetAcceleration(float velocity, float terminalVelocity)
	{
		float num = System.Math.Abs(velocity / terminalVelocity);
		return 32f * (1f - num * num * num);
	}

	public static float GetTerminalVelocity(float mass, float density, float areaMillimetersSquared, float dragCoefficient)
	{
		float num = mass * 1000f;
		float num2 = areaMillimetersSquared * 1000000f;
		float num3 = 64f * num / (density * num2 * dragCoefficient);
		return (float)System.Math.Sqrt(num3);
	}

	public static float ComputeProjectedArea(float x, float y, float z, BoundingBox box)
	{
		float num = 0f;
		Vector3 size = box.GetSize();
		if (x != 0f)
		{
			num += System.Math.Abs(x) * size.Z * size.Y;
		}
		if (y != 0f)
		{
			num += System.Math.Abs(y) * size.Z * size.X;
		}
		if (z != 0f)
		{
			num += System.Math.Abs(z) * size.X * size.Y;
		}
		return num;
	}

	public static float ComputeProjectedArea(Vector3 direction, BoundingBox box)
	{
		return ComputeProjectedArea(direction.X, direction.Y, direction.Z, box);
	}

	public static float VolumeOfIntersection(BoundingBox a, Vector3 posA, BoundingBox b, float posBX, float posBY, float posBZ)
	{
		posBX -= posA.X;
		posBY -= posA.Y;
		posBZ -= posA.Z;
		return LengthOfIntersection(a.Min.X, a.Max.X, posBX + b.Min.X, posBX + b.Max.X) * LengthOfIntersection(a.Min.Y, a.Max.Y, posBY + b.Min.Y, posBY + b.Max.Y) * LengthOfIntersection(a.Min.Z, a.Max.Z, posBZ + b.Min.Z, posBZ + b.Max.Z);
	}

	public static float LengthOfIntersection(float aMin, float aMax, float bMin, float bMax)
	{
		float num = System.Math.Max(aMin, bMin);
		float num2 = System.Math.Min(aMax, bMax);
		return System.Math.Max(0f, num2 - num);
	}

	public static float HeadingFromDirection(float x, float z)
	{
		return 1f * TrigMathUtil.Atan2(0f - x, 0f - z);
	}

	public static float NormalizeAngle(float rad)
	{
		rad %= (float)System.Math.PI * 2f;
		if (rad < 0f)
		{
			rad += (float)System.Math.PI * 2f;
		}
		return rad;
	}

	public static float NormalizeTurnAngle(float rad)
	{
		rad = NormalizeAngle(rad);
		if (rad >= (float)System.Math.PI)
		{
			rad -= (float)System.Math.PI * 2f;
		}
		return rad;
	}

	public static float PitchFromDirection(float x, float y, float z)
	{
		return TrigMathUtil.Atan2(y, System.Math.Sqrt(x * x + z * z));
	}

	public static Vector3 VectorFromAngles(float heading, float pitch, ref Vector3 outDirection)
	{
		float num = PitchX(pitch);
		outDirection.Y = PitchY(pitch);
		outDirection.X = HeadingX(heading) * num;
		outDirection.Z = HeadingZ(heading) * num;
		return outDirection;
	}

	public static float PitchX(float pitch)
	{
		return TrigMathUtil.Cos(pitch);
	}

	public static float PitchY(float pitch)
	{
		return TrigMathUtil.Sin(pitch);
	}

	public static float HeadingX(float heading)
	{
		return 0f - TrigMathUtil.Sin(1f * heading);
	}

	public static float HeadingZ(float heading)
	{
		return 0f - TrigMathUtil.Cos(1f * heading);
	}

	public static float ComputeDragCoefficient(float terminalSpeed, float area, float mass, float gravity)
	{
		return mass * gravity / (area * terminalSpeed * terminalSpeed);
	}
}
