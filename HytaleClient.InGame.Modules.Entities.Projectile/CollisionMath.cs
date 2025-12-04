using System;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class CollisionMath
{
	public const float MovementThreshold = 1E-05f;

	public const float MovementThresholdSquared = 9.9999994E-11f;

	public const float Extent = 1E-05f;

	public const int Disjoint = 0;

	public const int TouchX = 1;

	public const int TouchY = 2;

	public const int TouchZ = 4;

	public const int TouchAny = 7;

	public const int OverlapX = 8;

	public const int OverlapY = 16;

	public const int OverlapZ = 32;

	public const int OverlapAny = 56;

	public const int OverlapAll = 56;

	public static bool IsDisjoint(int code)
	{
		return code == 0;
	}

	public static bool IsOverlapping(int code)
	{
		return code == 56;
	}

	public static bool IsTouching(int code)
	{
		return (code & 7) != 0;
	}

	public static bool IntersectSweptAABBs(Vector3 posP, Vector3 vP, BoundingBox p, Vector3 posQ, BoundingBox q, ref Vector2 minMax)
	{
		return IntersectSweptAABBs(posP, vP, p, posQ.X, posQ.Y, posQ.Z, q, ref minMax);
	}

	public static bool IntersectSweptAABBs(Vector3 posP, Vector3 vP, BoundingBox p, float qx, float qy, float qz, BoundingBox q, ref Vector2 minMax)
	{
		return IntersectVectorAABB(posP, vP, qx, qy, qz, q.MinkowskiSum(p), ref minMax);
	}

	public static bool IntersectVectorAABB(Vector3 pos, Vector3 vec, float x, float y, float z, BoundingBox box, ref Vector2 minMax)
	{
		return IntersectRayAABB(pos, vec, x, y, z, box, ref minMax) && minMax.X <= 1f;
	}

	public static bool IntersectRayAABB(Vector3 pos, Vector3 ray, float x, float y, float z, BoundingBox box, ref Vector2 minMax)
	{
		minMax.X = 0f;
		minMax.Y = float.MaxValue;
		Vector3 min = box.Min;
		Vector3 max = box.Max;
		return Intersect1D(pos.X, ray.X, x + min.X, x + max.X, ref minMax) && Intersect1D(pos.Y, ray.Y, y + min.Y, y + max.Y, ref minMax) && Intersect1D(pos.Z, ray.Z, z + min.Z, z + max.Z, ref minMax) && minMax.X >= 0f;
	}

	public static bool Intersect1D(float p, float s, float min, float max, ref Vector2 minMax)
	{
		if (System.Math.Abs(s) < 1E-05f)
		{
			return !(p < min) && !(p > max);
		}
		float num = (min - p) / s;
		float num2 = (max - p) / s;
		if (num2 >= num)
		{
			if (num > minMax.X)
			{
				minMax.X = num;
			}
			if (num2 < minMax.Y)
			{
				minMax.Y = num2;
			}
		}
		else
		{
			if (num2 > minMax.X)
			{
				minMax.X = num2;
			}
			if (num < minMax.Y)
			{
				minMax.Y = num;
			}
		}
		return minMax.X <= minMax.Y;
	}

	public static int IntersectAABBs(float px, float py, float pz, BoundingBox bbP, float qx, float qy, float qz, BoundingBox bbQ)
	{
		int num = Intersect1D(px, bbP.Min.X, bbP.Max.X, qx, bbQ.Min.X, bbQ.Max.X);
		if (num == 0)
		{
			return 0;
		}
		num &= 9;
		int num2 = Intersect1D(py, bbP.Min.Y, bbP.Max.Y, qy, bbQ.Min.Y, bbQ.Max.Y);
		if (num2 == 0)
		{
			return 0;
		}
		num2 &= 0x12;
		int num3 = Intersect1D(pz, bbP.Min.Z, bbP.Max.Z, qz, bbQ.Min.Z, bbQ.Max.Z);
		if (num3 == 0)
		{
			return 0;
		}
		num3 &= 0x24;
		return num | num2 | num3;
	}

	public static int Intersect1D(float p, float pMin, float pMax, float q, float qMin, float qMax, float thickness = 1E-05f)
	{
		double num = q - p;
		double num2 = (double)(pMin - qMax) - num;
		if (num2 > (double)thickness)
		{
			return 0;
		}
		if (num2 > (double)(0f - thickness))
		{
			return 7;
		}
		num2 = (double)(qMin - pMax) + num;
		if (num2 > (double)thickness)
		{
			return 0;
		}
		if (num2 > (double)(0f - thickness))
		{
			return 7;
		}
		return 56;
	}

	public static bool IsBelowMovementThreshold(Vector3 v)
	{
		return v.LengthSquared() < 9.9999994E-11f;
	}
}
