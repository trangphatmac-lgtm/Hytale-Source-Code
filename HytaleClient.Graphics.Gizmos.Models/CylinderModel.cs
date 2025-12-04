using System;

namespace HytaleClient.Graphics.Gizmos.Models;

public class CylinderModel
{
	public static PrimitiveModelData BuildModelData(float radius, float height, int segments)
	{
		float[] array = new float[4 * segments * 8];
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < segments; j++)
			{
				int num = (j + i * segments) * 8;
				float num2 = (float)j / (float)(segments - 1) * ((float)System.Math.PI * 2f);
				switch (i)
				{
				case 0:
					array[num] = 0f;
					array[num + 1] = (0f - height) / 2f;
					array[num + 2] = 0f;
					break;
				case 1:
					array[num] = (float)((double)radius * System.Math.Cos(num2));
					array[num + 1] = (0f - height) / 2f;
					array[num + 2] = (float)((double)radius * System.Math.Sin(num2));
					break;
				case 2:
					array[num] = (float)((double)radius * System.Math.Cos(num2));
					array[num + 1] = height / 2f;
					array[num + 2] = (float)((double)radius * System.Math.Sin(num2));
					break;
				case 3:
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
		return new PrimitiveModelData(array, PrimitiveModelData.MakeRadialIndices(segments, 4));
	}

	public static PrimitiveModelData BuildHollowModelData(float radius, float height, int segments)
	{
		float[] array = new float[2 * segments * 8];
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < segments; j++)
			{
				int num = (j + i * segments) * 8;
				float num2 = (float)j / (float)(segments - 1) * ((float)System.Math.PI * 2f);
				array[num] = (float)((double)radius * System.Math.Cos(num2));
				array[num + 1] = ((i == 0) ? ((0f - height) / 2f) : (height / 2f));
				array[num + 2] = (float)((double)radius * System.Math.Sin(num2));
				array[num + 3] = 0f;
				array[num + 4] = 0f;
				array[num + 5] = 0f;
				array[num + 6] = 0f;
				array[num + 7] = 0f;
			}
		}
		return new PrimitiveModelData(array, PrimitiveModelData.MakeRadialIndices(segments, 2));
	}

	public static bool[,,] BuildVoxelData(int radiusX, int height, int radiusZ)
	{
		bool[,,] array = new bool[radiusX * 2 + 1, height + 1, radiusZ * 2 + 1];
		float num = 0.41f;
		float num2 = (float)radiusX + num;
		float num3 = (float)radiusZ + num;
		double num4 = 1.0 / (double)(num2 * num2);
		for (int i = -radiusX; i <= radiusX; i++)
		{
			double d = 1.0 - (double)(i * i) * num4;
			double num5 = System.Math.Sqrt(d) * (double)num3;
			int num6;
			int num7 = -(num6 = (int)num5);
			for (int j = num7; j <= num6; j++)
			{
				for (int num8 = height; num8 >= 0; num8--)
				{
					array[i + radiusX, num8, j + radiusZ] = true;
				}
			}
		}
		return array;
	}
}
