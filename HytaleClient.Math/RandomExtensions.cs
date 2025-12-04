using System;

namespace HytaleClient.Math;

internal static class RandomExtensions
{
	public static float NextFloat(this Random random, float min, float max)
	{
		return (float)((double)min + random.NextDouble() * (double)(max - min));
	}
}
