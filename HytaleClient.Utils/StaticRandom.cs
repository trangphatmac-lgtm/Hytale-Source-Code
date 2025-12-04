namespace HytaleClient.Utils;

internal static class StaticRandom
{
	public static float NextFloat(long seed)
	{
		return (float)(seed & 0xFFFFFF) / 16777216f;
	}

	public static float NextFloat(long seed, float min, float max)
	{
		return min + NextFloat(seed) * (max - min);
	}
}
