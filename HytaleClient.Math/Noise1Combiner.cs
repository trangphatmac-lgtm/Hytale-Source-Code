namespace HytaleClient.Math;

public static class Noise1Combiner
{
	public class Sum : Noise1
	{
		private readonly Noise1[] _noise;

		public Sum(Noise1[] noise)
		{
			_noise = noise;
		}

		public float Eval(int seed, float t)
		{
			float num = 0f;
			Noise1[] noise = _noise;
			foreach (Noise1 noise2 in noise)
			{
				num += noise2.Eval(seed, t);
			}
			return num;
		}
	}

	public static Noise1 Summed(Noise1[] noises)
	{
		if (noises == null || noises.Length == 0)
		{
			return Noise1Source.Zero;
		}
		return new Sum(noises);
	}
}
