using System.Runtime.InteropServices;
using HytaleClient.Protocol;

namespace HytaleClient.Math;

public static class Noise1Helper
{
	[StructLayout(LayoutKind.Explicit)]
	private struct Float2Int
	{
		[FieldOffset(0)]
		public float f;

		[FieldOffset(0)]
		public int i;
	}

	private static readonly Noise1[] EMPTY_ARRAY = new Noise1[0];

	public const int RandomSeed = 0;

	public const int XPrime = 1619;

	public const int YPrime = 31337;

	public const int HashPrime = 60493;

	public const float Int2FloatDenom = 2.1474836E+09f;

	public static int Floor(float value)
	{
		int num = (int)value;
		return (value < 0f) ? (num - 1) : num;
	}

	public static int Hash(int seed, int x = 31337)
	{
		int num = seed ^ (1619 * x);
		num = num * num * num * 60493;
		return (num >> 13) ^ num;
	}

	public static float Rand(int seed, int x)
	{
		int num = seed ^ (1619 * x);
		float value = (float)(num * num * num * 60493) / 2.1474836E+09f;
		return MathHelper.Clamp(value, -1f, 1f);
	}

	public static int ToInt(float value)
	{
		Float2Int float2Int = default(Float2Int);
		float2Int.f = value;
		return float2Int.i;
	}

	public static Noise1 CreateNoise(NoiseConfig config)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (config == null || config.Frequency == 0f || config.Amplitude == 0f)
		{
			return Noise1Source.Zero;
		}
		Noise1 source = Noise1Source.GetSource(config.Type);
		source = Noise1Modifier.Seeded(source, config.Seed);
		source = Noise1Modifier.Clamped(source, config.Clamp);
		return Noise1Modifier.Scaled(source, config.Frequency, config.Amplitude);
	}

	public static Noise1[] CreateNoises(NoiseConfig[] configs)
	{
		if (configs == null || configs.Length == 0)
		{
			return EMPTY_ARRAY;
		}
		bool flag = false;
		Noise1[] array = new Noise1[configs.Length];
		for (int i = 0; i < configs.Length; i++)
		{
			NoiseConfig config = configs[i];
			flag |= (array[i] = CreateNoise(config)) != Noise1Source.Zero;
		}
		if (!flag)
		{
			return EMPTY_ARRAY;
		}
		return array;
	}
}
