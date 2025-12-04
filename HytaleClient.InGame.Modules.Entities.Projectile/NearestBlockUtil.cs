using System;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class NearestBlockUtil
{
	public class IterationElement
	{
		public int OffsetX;

		public int OffsetY;

		public int OffsetZ;

		public Func<float, float> X;

		public Func<float, float> Y;

		public Func<float, float> Z;

		public IterationElement(int offsetX, int offsetY, int offsetZ, Func<float, float> x, Func<float, float> y, Func<float, float> z)
		{
			OffsetX = offsetX;
			OffsetY = offsetY;
			OffsetZ = offsetZ;
			X = x;
			Y = y;
			Z = z;
		}
	}

	public static readonly IterationElement[] DefaultElements = new IterationElement[6]
	{
		new IterationElement(-1, 0, 0, (float x) => 0f, (float y) => y, (float z) => z),
		new IterationElement(1, 0, 0, (float x) => 1f, (float y) => y, (float z) => z),
		new IterationElement(0, -1, 0, (float x) => x, (float y) => 0f, (float z) => z),
		new IterationElement(0, 1, 0, (float x) => x, (float y) => 1f, (float z) => z),
		new IterationElement(0, 0, -1, (float x) => x, (float y) => y, (float z) => 0f),
		new IterationElement(0, 0, 1, (float x) => x, (float y) => y, (float z) => 1f)
	};

	public static IntVector3? FindNearestBlock<T>(Vector3 position, Func<IntVector3, T, bool> validBlock, T t)
	{
		return FindNearestBlock(DefaultElements, position.X, position.Y, position.Z, validBlock, t);
	}

	public static IntVector3? FindNearestBlock<T>(IterationElement[] elements, Vector3 position, Func<IntVector3, T, bool> validBlock, T t)
	{
		return FindNearestBlock(elements, position.X, position.Y, position.Z, validBlock, t);
	}

	public static IntVector3? FindNearestBlock<T>(float x, float y, float z, Func<IntVector3, T, bool> validBlock, T t)
	{
		return FindNearestBlock(DefaultElements, x, y, z, validBlock, t);
	}

	public static IntVector3? FindNearestBlock<T>(IterationElement[] elements, float x, float y, float z, Func<IntVector3, T, bool> validBlock, T t)
	{
		int num = (int)System.Math.Floor(x);
		int num2 = (int)System.Math.Floor(y);
		int num3 = (int)System.Math.Floor(z);
		float num4 = x % 1f;
		float num5 = y % 1f;
		float num6 = z % 1f;
		IntVector3? result = null;
		float num7 = float.PositiveInfinity;
		foreach (IterationElement iterationElement in elements)
		{
			float num8 = num4 - iterationElement.X(num4);
			float num9 = num5 - iterationElement.Y(num5);
			float num10 = num6 - iterationElement.Z(num6);
			float num11 = num8 * num8 + num9 * num9 + num10 * num10;
			IntVector3 intVector = new IntVector3(num + iterationElement.OffsetX, num2 + iterationElement.OffsetY, num3 + iterationElement.OffsetZ);
			if (num11 < num7 && validBlock(intVector, t))
			{
				num7 = num11;
				if (!result.HasValue)
				{
					result = default(IntVector3);
				}
				result = intVector;
			}
		}
		return result;
	}
}
