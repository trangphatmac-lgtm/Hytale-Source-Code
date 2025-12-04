using System;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

public static class TrigMathUtil
{
	private static class Riven
	{
		private static readonly int SinBits;

		private static readonly int SinMask;

		private static readonly int SinCount;

		private static readonly float RadFull;

		private static readonly float RadToIndex;

		private static readonly float DegFull;

		private static readonly float DegToIndex;

		private static readonly float[] SIN;

		private static readonly float[] COS;

		static Riven()
		{
			SinBits = 12;
			SinMask = ~(-1 << SinBits);
			SinCount = SinMask + 1;
			RadFull = (float)System.Math.PI * 2f;
			DegFull = 360f;
			RadToIndex = (float)SinCount / RadFull;
			DegToIndex = (float)SinCount / DegFull;
			SIN = new float[SinCount];
			COS = new float[SinCount];
			for (int i = 0; i < SinCount; i++)
			{
				SIN[i] = (float)System.Math.Sin(((float)i + 0.5f) / (float)SinCount * RadFull);
				COS[i] = (float)System.Math.Cos(((float)i + 0.5f) / (float)SinCount * RadFull);
			}
			for (int j = 0; j < 360; j += 90)
			{
				SIN[(int)((float)j * DegToIndex) & SinMask] = (float)System.Math.Sin((double)j * System.Math.PI / 180.0);
				COS[(int)((float)j * DegToIndex) & SinMask] = (float)System.Math.Cos((double)j * System.Math.PI / 180.0);
			}
		}

		public static float Sin(float rad)
		{
			return SIN[(int)(rad * RadToIndex) & SinMask];
		}

		public static float Cos(float rad)
		{
			return COS[(int)(rad * RadToIndex) & SinMask];
		}
	}

	private static class Icecore
	{
		private const int SizeAc = 100000;

		private const int SizeAr = 100001;

		private static readonly float[] ATAN2;

		static Icecore()
		{
			ATAN2 = new float[100001];
			for (int i = 0; i <= 100000; i++)
			{
				double num = (double)i / 100000.0;
				double num2 = 1.0;
				double y = num2 * num;
				float num3 = (float)System.Math.Atan2(y, num2);
				ATAN2[i] = num3;
			}
		}

		public static float Atan2(float y, float x)
		{
			if (y < 0f)
			{
				if (x < 0f)
				{
					if (y < x)
					{
						return 0f - ATAN2[(int)(x / y * 100000f)] - (float)System.Math.PI / 2f;
					}
					return ATAN2[(int)(y / x * 100000f)] - (float)System.Math.PI;
				}
				y = 0f - y;
				if (y > x)
				{
					return ATAN2[(int)(x / y * 100000f)] - (float)System.Math.PI / 2f;
				}
				return 0f - ATAN2[(int)(y / x * 100000f)];
			}
			if (x < 0f)
			{
				x = 0f - x;
				if (y > x)
				{
					return ATAN2[(int)(x / y * 100000f)] + (float)System.Math.PI / 2f;
				}
				return 0f - ATAN2[(int)(y / x * 100000f)] + (float)System.Math.PI;
			}
			if (y > x)
			{
				return 0f - ATAN2[(int)(x / y * 100000f)] + (float)System.Math.PI / 2f;
			}
			return ATAN2[(int)(y / x * 100000f)];
		}
	}

	public const float Pi = (float)System.Math.PI;

	public const float PiHalf = (float)System.Math.PI / 2f;

	public const float PiQuarter = (float)System.Math.PI / 4f;

	public const float Pi2 = (float)System.Math.PI * 2f;

	public const float Pi4 = (float)System.Math.PI * 4f;

	public const float RadToDeg = 180f / (float)System.Math.PI;

	public const float DegToRad = (float)System.Math.PI / 180f;

	public static float Sin(float radians)
	{
		return Riven.Sin(radians);
	}

	public static float Cos(float radians)
	{
		return Riven.Cos(radians);
	}

	public static float Sin(double radians)
	{
		return Riven.Sin((float)radians);
	}

	public static float Cos(double radians)
	{
		return Riven.Cos((float)radians);
	}

	public static float Atan2(float y, float x)
	{
		return Icecore.Atan2(y, x);
	}

	public static float Atan2(double y, double x)
	{
		return Icecore.Atan2((float)y, (float)x);
	}

	public static float Atan(double d)
	{
		return (float)System.Math.Atan(d);
	}

	public static float Asin(double d)
	{
		return (float)System.Math.Asin(d);
	}
}
