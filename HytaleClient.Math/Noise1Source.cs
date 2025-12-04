using System;
using HytaleClient.Protocol;

namespace HytaleClient.Math;

public static class Noise1Source
{
	public class Constant : Noise1
	{
		private readonly float _value;

		public Constant(float value)
		{
			_value = value;
		}

		public float Eval(int seed, float t)
		{
			return _value;
		}
	}

	public class Perlin : Noise1
	{
		private static readonly int LENGTH = 32;

		private static readonly int MASK = LENGTH - 1;

		private static readonly float[] NOISE = CreateNoiseArray(LENGTH, -1f, 1f, new System.Random());

		private Func<float, float> _interpolation;

		public Perlin(Func<float, float> interpolation)
		{
			_interpolation = interpolation;
		}

		public float Eval(int seed, float t)
		{
			int num = Noise1Helper.Floor(t);
			int num2 = Noise1Helper.Hash(seed, num);
			int num3 = Noise1Helper.Hash(seed, num + 1);
			float num4 = t - (float)num;
			float value = NOISE[num2 & MASK] * num4;
			float value2 = NOISE[num3 & MASK] * (num4 - 1f);
			float num5 = MathHelper.Lerp(value, value2, _interpolation(num4));
			return MathHelper.Clamp(num5 * 2f, -1f, 1f);
		}

		private static float[] CreateNoiseArray(int length, float min, float max, System.Random rnd)
		{
			float[] array = new float[length];
			float num = 1f;
			float num2 = 0f;
			for (int i = 0; i < length; i++)
			{
				float val = (array[i] = rnd.NextFloat(0f, 1f));
				num = System.Math.Min(num, val);
				num2 = System.Math.Max(num2, val);
			}
			float num3 = max - min;
			float num4 = num2 - num;
			for (int j = 0; j < length; j++)
			{
				float num5 = (array[j] - num) / num4;
				array[j] = min + num5 * num3;
			}
			return array;
		}
	}

	public class Random : Noise1
	{
		public float Eval(int seed, float t)
		{
			int x = Noise1Helper.ToInt(t);
			return Noise1Helper.Rand(seed, x);
		}
	}

	public class Sine : Noise1
	{
		public float Eval(int seed, float t)
		{
			return MathHelper.Sin(t);
		}
	}

	public class Cosine : Noise1
	{
		public float Eval(int seed, float t)
		{
			return MathHelper.Cos(t);
		}
	}

	public static readonly Func<float, float> LinearInterpolation = (float t) => t;

	public static readonly Func<float, float> HermiteInterpolation = (float t) => t * t * (3f - 2f * t);

	public static readonly Func<float, float> QuinticInterpolation = (float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

	public static readonly Constant Zero = new Constant(0f);

	public static readonly Constant One = new Constant(1f);

	public static readonly Perlin PerlinLinear = new Perlin(LinearInterpolation);

	public static readonly Perlin PerlinHermite = new Perlin(HermiteInterpolation);

	public static readonly Perlin PerlinQuintic = new Perlin(HermiteInterpolation);

	public static readonly Random Rand = new Random();

	public static readonly Sine Sin = new Sine();

	public static readonly Cosine Cos = new Cosine();

	public static Noise1 GetSource(NoiseType type)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected I4, but got Unknown
		return (int)type switch
		{
			0 => Sin, 
			1 => Cos, 
			2 => PerlinLinear, 
			3 => PerlinHermite, 
			4 => PerlinQuintic, 
			5 => Rand, 
			_ => Zero, 
		};
	}
}
