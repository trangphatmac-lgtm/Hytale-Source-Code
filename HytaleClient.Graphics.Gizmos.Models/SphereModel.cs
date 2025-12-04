using System;

namespace HytaleClient.Graphics.Gizmos.Models;

public class SphereModel
{
	public static PrimitiveModelData BuildModelData(float radius, float height, int segments, int rings, float depth = 0f)
	{
		float[] array = new float[rings * segments * 8];
		if (depth == 0f)
		{
			depth = radius;
		}
		for (int i = 0; i < rings; i++)
		{
			float num = (float)i / (float)(rings - 1) * (float)System.Math.PI;
			for (int j = 0; j < segments; j++)
			{
				int num2 = (j + i * segments) * 8;
				float num3 = (1f - (float)j / (float)(segments - 1)) * ((float)System.Math.PI * 2f);
				array[num2] = (float)((double)radius * System.Math.Sin(num) * System.Math.Cos(num3));
				array[num2 + 1] = (float)((double)(height / 2f) * System.Math.Cos(num));
				array[num2 + 2] = (float)((double)depth * System.Math.Sin(num) * System.Math.Sin(num3));
				array[num2 + 3] = 0f;
				array[num2 + 4] = 0f;
				array[num2 + 5] = 0f;
				array[num2 + 6] = 0f;
				array[num2 + 7] = 0f;
			}
		}
		return new PrimitiveModelData(array, PrimitiveModelData.MakeRadialIndices(segments, rings));
	}

	public static bool[,,] BuildVoxelData(int radiusX, int radiusY, int radiusZ)
	{
		bool[,,] array = new bool[radiusX * 2 + 1, radiusY * 2 + 1, radiusZ * 2 + 1];
		float num = 0.41f;
		float num2 = (float)radiusX + num;
		float num3 = (float)radiusY + num;
		float num4 = (float)radiusZ + num;
		double num5 = 1.0 / (double)(num2 * num2);
		double num6 = 1.0 / (double)(num3 * num3);
		for (int i = -radiusX; i <= radiusX; i++)
		{
			double num7 = 1.0 - (double)(i * i) * num5;
			double num8 = System.Math.Sqrt(num7) * (double)num3;
			int num9;
			int num10 = -(num9 = (int)num8);
			for (int num11 = num9; num11 >= num10; num11--)
			{
				double num12 = System.Math.Sqrt(num7 - (double)(num11 * num11) * num6) * (double)num4;
				int num13;
				int num14 = -(num13 = (int)num12);
				for (int j = num14; j <= num13; j++)
				{
					array[i + radiusX, num11 + radiusY, j + radiusZ] = true;
				}
			}
		}
		return array;
	}
}
