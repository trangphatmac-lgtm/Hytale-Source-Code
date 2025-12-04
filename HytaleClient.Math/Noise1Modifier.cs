using System;
using HytaleClient.Protocol;

namespace HytaleClient.Math;

public static class Noise1Modifier
{
	public class Clamp : Noise1
	{
		private readonly Noise1 _noise;

		private readonly float _min;

		private readonly float _max;

		public Clamp(Noise1 noise, float min, float max)
		{
			_noise = noise;
			_min = min;
			_max = max;
		}

		public float Eval(int seed, float t)
		{
			float value = _noise.Eval(seed, t);
			return MathHelper.Clamp(value, _min, _max);
		}
	}

	public class Normalize : Noise1
	{
		private readonly Noise1 _noise;

		private readonly float _min;

		private readonly float _invRange;

		public Normalize(Noise1 noise, float min, float max)
		{
			_noise = noise;
			_min = min;
			_invRange = 2f / (max - min);
		}

		public float Eval(int seed, float t)
		{
			float num = _noise.Eval(seed, t);
			return -1f + (num - _min) * _invRange;
		}
	}

	public class Scale : Noise1
	{
		private readonly Noise1 _noise;

		private readonly float _frequency;

		private readonly float _amplitude;

		public Scale(Noise1 noise, float frequency, float amplitude)
		{
			_noise = noise;
			_frequency = frequency;
			_amplitude = amplitude;
		}

		public float Eval(int seed, float t)
		{
			return _noise.Eval(seed, t * _frequency) * _amplitude;
		}
	}

	public class Seed : Noise1
	{
		private readonly Noise1 _noise;

		private readonly int _seedOffset;

		public Seed(Noise1 noise, int seedOffset)
		{
			_noise = noise;
			_seedOffset = seedOffset;
		}

		public float Eval(int seed, float t)
		{
			return _noise.Eval(seed + _seedOffset, t);
		}
	}

	public static Noise1 Clamped(Noise1 noise, ClampConfig config)
	{
		if (config == null)
		{
			return noise;
		}
		return Clamped(noise, config.Min, config.Max, config.Normalize);
	}

	public static Noise1 Clamped(Noise1 noise, float min, float max, bool normalize = false)
	{
		if (min >= max)
		{
			return Noise1Source.Zero;
		}
		if (noise == Noise1Source.Zero || (min == -1f && max == 1f))
		{
			return noise;
		}
		noise = new Clamp(noise, min, max);
		if (normalize)
		{
			return Normalized(noise, min, max);
		}
		return noise;
	}

	public static Noise1 Normalized(Noise1 noise, float min, float max)
	{
		if (min == max)
		{
			return Noise1Source.Zero;
		}
		return new Normalize(noise, min, max);
	}

	public static Noise1 Scaled(Noise1 noise, float frequency, float amplitude)
	{
		if (noise == Noise1Source.Zero || (frequency == 1f && amplitude == 1f))
		{
			return noise;
		}
		return new Scale(noise, frequency, amplitude);
	}

	public static Noise1 Seeded(Noise1 noise, int seed)
	{
		if (noise == Noise1Source.Zero)
		{
			return noise;
		}
		if (seed == 0)
		{
			seed = Noise1Helper.Hash(Environment.TickCount);
		}
		return new Seed(noise, seed);
	}
}
