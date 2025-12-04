using System;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

public static class ServerBlockIterator
{
	public delegate bool BlockIteratorProcedurePlus1<in T>(int x, int y, int z, float px, float py, float pz, float qx, float qy, float qz, T obj1);

	private static class FastMath
	{
		public const float TwoPower52 = 4.5035996E+15f;

		public const float RoundingError = 1E-15f;

		public static bool Eq(float a, float b)
		{
			return Abs(a - b) < 1.0000000036274937E-15;
		}

		public static bool SEq(float a, float b)
		{
			return a <= b + 1E-15f;
		}

		public static bool GEq(float a, float b)
		{
			return a >= b - 1E-15f;
		}

		public static double Abs(float x)
		{
			return ((double)x < 0.0) ? (0f - x) : x;
		}

		public static int FastFloor(float x)
		{
			if (x >= 4.5035996E+15f || x <= -4.5035996E+15f)
			{
				return (int)x;
			}
			int num = (int)x;
			if (x < 0f && (float)num != x)
			{
				num--;
			}
			if (num == 0)
			{
				return (int)(x * (float)num);
			}
			return num;
		}
	}

	public static bool Iterate<T>(float sx, float sy, float sz, float dx, float dy, float dz, float maxDistance, BlockIteratorProcedurePlus1<T> procedure, T obj1)
	{
		CheckParameters(sx, sy, sz, dx, dy, dz);
		return Iterate0(sx, sy, sz, dx, dy, dz, maxDistance, procedure, obj1);
	}

	private static bool Iterate0<T>(float sx, float sy, float sz, float dx, float dy, float dz, float maxDistance, BlockIteratorProcedurePlus1<T> procedure, T obj1)
	{
		maxDistance /= (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
		int num = FastMath.FastFloor(sx);
		int num2 = FastMath.FastFloor(sy);
		int num3 = FastMath.FastFloor(sz);
		float num4 = sx - (float)num;
		float num5 = sy - (float)num2;
		float num6 = sz - (float)num3;
		float num7 = 0f;
		while (num7 <= maxDistance)
		{
			float num8 = Intersection(num4, num5, num6, dx, dy, dz);
			float num9 = num4 + num8 * dx;
			float num10 = num5 + num8 * dy;
			float num11 = num6 + num8 * dz;
			if (!procedure(num, num2, num3, num4, num5, num6, num9, num10, num11, obj1))
			{
				return false;
			}
			if (dx < 0f && FastMath.SEq(num9, 0f))
			{
				num9 += 1f;
				num--;
			}
			else if (dx > 0f && FastMath.GEq(num9, 1f))
			{
				num9 -= 1f;
				num++;
			}
			if (dy < 0f && FastMath.SEq(num10, 0f))
			{
				num10 += 1f;
				num2--;
			}
			else if (dy > 0f && FastMath.GEq(num10, 1f))
			{
				num10 -= 1f;
				num2++;
			}
			if (dz < 0f && FastMath.SEq(num11, 0f))
			{
				num11 += 1f;
				num3--;
			}
			else if (dz > 0f && FastMath.GEq(num11, 1f))
			{
				num11 -= 1f;
				num3++;
			}
			num7 += num8;
			num4 = num9;
			num5 = num10;
			num6 = num11;
		}
		return true;
	}

	private static void CheckParameters(float sx, float sy, float sz, float dx, float dy, float dz)
	{
		if (IsNonValidNumber(sx))
		{
			throw new ArgumentException("sx is a non-valid number! Given: " + sx);
		}
		if (IsNonValidNumber(sy))
		{
			throw new ArgumentException("sy is a non-valid number! Given: " + sy);
		}
		if (IsNonValidNumber(sz))
		{
			throw new ArgumentException("sz is a non-valid number! Given: " + sz);
		}
		if (IsNonValidNumber(dx))
		{
			throw new ArgumentException("dx is a non-valid number! Given: " + dx);
		}
		if (IsNonValidNumber(dy))
		{
			throw new ArgumentException("dy is a non-valid number! Given: " + dy);
		}
		if (IsNonValidNumber(dz))
		{
			throw new ArgumentException("dz is a non-valid number! Given: " + dz);
		}
		if (IsZeroDirection(dx, dy, dz))
		{
			throw new ArgumentException("Direction is ZERO! Given: (" + dx + ", " + dy + ", " + dz + ")");
		}
	}

	public static bool IsNonValidNumber(float d)
	{
		return float.IsNaN(d) || float.IsInfinity(d);
	}

	public static bool IsZeroDirection(float dx, float dy, float dz)
	{
		return FastMath.Eq(dx, 0f) && FastMath.Eq(dy, 0f) && FastMath.Eq(dz, 0f);
	}

	private static float Intersection(float px, float py, float pz, float dx, float dy, float dz)
	{
		float num = 0f;
		if (dx < 0f)
		{
			float num2 = (0f - px) / dx;
			float a = pz + dz * num2;
			float a2 = py + dy * num2;
			if (num2 > num && FastMath.GEq(a, 0f) && FastMath.SEq(a, 1f) && FastMath.GEq(a2, 0f) && FastMath.SEq(a2, 1f))
			{
				num = num2;
			}
		}
		else if (dx > 0f)
		{
			float num2 = (1f - px) / dx;
			float a = pz + dz * num2;
			float a2 = py + dy * num2;
			if (num2 > num && FastMath.GEq(a, 0f) && FastMath.SEq(a, 1f) && FastMath.GEq(a2, 0f) && FastMath.SEq(a2, 1f))
			{
				num = num2;
			}
		}
		if (dy < 0f)
		{
			float num2 = (0f - py) / dy;
			float a = px + dx * num2;
			float a2 = pz + dz * num2;
			if (num2 > num && FastMath.GEq(a, 0f) && FastMath.SEq(a, 1f) && FastMath.GEq(a2, 0f) && FastMath.SEq(a2, 1f))
			{
				num = num2;
			}
		}
		else if (dy > 0f)
		{
			float num2 = (1f - py) / dy;
			float a = px + dx * num2;
			float a2 = pz + dz * num2;
			if (num2 > num && FastMath.GEq(a, 0f) && FastMath.SEq(a, 1f) && FastMath.GEq(a2, 0f) && FastMath.SEq(a2, 1f))
			{
				num = num2;
			}
		}
		if (dz < 0f)
		{
			float num2 = (0f - pz) / dz;
			float a = px + dx * num2;
			float a2 = py + dy * num2;
			if (num2 > num && FastMath.GEq(a, 0f) && FastMath.SEq(a, 1f) && FastMath.GEq(a2, 0f) && FastMath.SEq(a2, 1f))
			{
				num = num2;
			}
		}
		else if (dz > 0f)
		{
			float num2 = (1f - pz) / dz;
			float a = px + dx * num2;
			float a2 = py + dy * num2;
			if (num2 > num && FastMath.GEq(a, 0f) && FastMath.SEq(a, 1f) && FastMath.GEq(a2, 0f) && FastMath.SEq(a2, 1f))
			{
				num = num2;
			}
		}
		return num;
	}
}
