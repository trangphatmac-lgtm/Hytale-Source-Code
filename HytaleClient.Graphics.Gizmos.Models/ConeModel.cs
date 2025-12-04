using System;

namespace HytaleClient.Graphics.Gizmos.Models;

public class ConeModel
{
	public static PrimitiveModelData BuildModelData(float radius, float height, int segments)
	{
		float[] array = new float[3 * segments * 8];
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < segments; j++)
			{
				int num = (j + i * segments) * 8;
				switch (i)
				{
				case 0:
					array[num] = 0f;
					array[num + 1] = (0f - height) / 2f;
					array[num + 2] = 0f;
					break;
				case 1:
				{
					float num2 = (float)j / (float)(segments - 1) * ((float)System.Math.PI * 2f);
					array[num] = radius * (float)System.Math.Cos(num2);
					array[num + 1] = (0f - height) / 2f;
					array[num + 2] = radius * (float)System.Math.Sin(num2);
					break;
				}
				case 2:
					array[num] = 0f;
					array[num + 1] = height / 2f;
					array[num + 2] = 0f;
					break;
				}
				array[num + 3] = 0f;
				array[num + 4] = 0f;
				array[num + 5] = 0f;
				array[num + 6] = 0f;
				array[num + 7] = 0f;
			}
		}
		return new PrimitiveModelData(array, PrimitiveModelData.MakeRadialIndices(segments, 3));
	}

	public static bool[,,] BuildVoxelData(int radiusX, int height, int radiusZ)
	{
		bool[,,] array = new bool[radiusX * 2 + 1, height + 1, radiusZ * 2 + 1];
		float num = 0.41f;
		float num2 = (float)radiusX + num;
		float num3 = (float)radiusZ + num;
		for (int num4 = height; num4 >= 0; num4--)
		{
			double num5 = 1.0 - (double)num4 / (double)height;
			double num6 = (double)num2 * num5;
			int num7;
			int num8 = -(num7 = (int)num6);
			for (int i = num8; i <= num7; i++)
			{
				double d = 1.0 - (double)(i * i) / (num6 * num6);
				double num9 = System.Math.Sqrt(d) * (double)num3 * num5;
				int num10;
				int num11 = -(num10 = (int)(double.IsNaN(num9) ? 0.0 : num9));
				for (int j = num11; j <= num10; j++)
				{
					array[i + radiusX, num4, j + radiusZ] = true;
				}
			}
		}
		return array;
	}

	public static bool[,,] BuildInvertedVoxelData(int radiusX, int height, int radiusZ)
	{
		bool[,,] array = new bool[radiusX * 2 + 1, height + 1, radiusZ * 2 + 1];
		float num = 0.41f;
		float num2 = (float)radiusX + num;
		float num3 = (float)radiusZ + num;
		for (int num4 = height; num4 >= 0; num4--)
		{
			double num5 = (double)num4 / (double)height;
			double num6 = (double)num2 * num5;
			double num7 = 1.0 / (num6 * num6);
			double num8 = (double)num3 * num5;
			int num9;
			int num10 = -(num9 = (int)num6);
			for (int i = num10; i <= num9; i++)
			{
				double num11 = System.Math.Sqrt(1.0 - (double)(i * i) * num7) * num8;
				int num12;
				int num13 = -(num12 = (int)(double.IsNaN(num11) ? 0.0 : num11));
				for (int j = num13; j <= num12; j++)
				{
					array[i + radiusX, num4, j + radiusZ] = true;
				}
			}
		}
		return array;
	}
}
