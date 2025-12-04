using System;

namespace HytaleClient.Graphics.Gizmos.Models;

internal class BipyramidModel
{
	public static PrimitiveModelData BuildModelData(float radius, float height, int segments)
	{
		float[] array = new float[4 * segments * 8];
		float num = (float)System.Math.Sqrt(2.0) - 1f;
		radius *= 10f * num / 2f - 0.5f;
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < segments; j++)
			{
				int num2 = (j + i * segments) * 8;
				switch (i)
				{
				case 0:
					array[num2] = 0f;
					array[num2 + 1] = (0f - height) / 2f;
					array[num2 + 2] = 0f;
					break;
				case 1:
				{
					float num3 = (float)j / (float)(segments - 1) * ((float)System.Math.PI * 2f) + (float)System.Math.PI / 4f;
					array[num2] = radius * (float)System.Math.Cos(num3);
					array[num2 + 1] = 0f;
					array[num2 + 2] = radius * (float)System.Math.Sin(num3);
					break;
				}
				case 2:
					array[num2] = 0f;
					array[num2 + 1] = height / 2f;
					array[num2 + 2] = 0f;
					break;
				}
				array[num2 + 3] = 0f;
				array[num2 + 4] = 0f;
				array[num2 + 5] = 0f;
				array[num2 + 6] = 0f;
				array[num2 + 7] = 0f;
			}
		}
		return new PrimitiveModelData(array, PrimitiveModelData.MakeRadialIndices(segments, 4));
	}
}
