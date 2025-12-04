using System;
using HytaleClient.Math;

namespace HytaleClient.Utils;

internal class BilinearFilter
{
	private static float CubicHermite(float A, float B, float C, float D, float t)
	{
		float num = (0f - A) / 2f + 3f * B / 2f - 3f * C / 2f + D / 2f;
		float num2 = A - 5f * B / 2f + 2f * C - D / 2f;
		float num3 = (0f - A) / 2f + C / 2f;
		return num * t * t * t + num2 * t * t + num3 * t + B;
	}

	public static byte[] ApplyFilter(byte[] pixels, int originalSize, int scaledSize)
	{
		byte[] array = new byte[scaledSize * scaledSize * 4];
		float num = (float)(originalSize - 1) / (float)scaledSize;
		int num2 = 0;
		byte[][] array2 = new byte[16][];
		for (int i = 0; i < 16; i++)
		{
			array2[i] = new byte[4];
		}
		for (int j = 0; j < scaledSize; j++)
		{
			float num3 = j / (scaledSize - 1);
			for (int k = 0; k < scaledSize; k++)
			{
				float num4 = k / (scaledSize - 1);
				int num5 = (int)(num * (float)k);
				int num6 = (int)(num * (float)j);
				int index = (num6 * originalSize + num5) * 4;
				float num7 = num4 * (float)originalSize - 0.5f;
				float t = num7 - (float)System.Math.Floor(num7);
				float num8 = num3 * (float)originalSize - 0.5f;
				float t2 = num8 - (float)System.Math.Floor(num8);
				GetColorByte(pixels, index, originalSize, -1, -1, ref array2[0]);
				GetColorByte(pixels, index, originalSize, 0, -1, ref array2[1]);
				GetColorByte(pixels, index, originalSize, 1, -1, ref array2[2]);
				GetColorByte(pixels, index, originalSize, 2, -1, ref array2[3]);
				GetColorByte(pixels, index, originalSize, -1, 0, ref array2[4]);
				GetColorByte(pixels, index, originalSize, 0, 0, ref array2[5]);
				GetColorByte(pixels, index, originalSize, 1, 0, ref array2[6]);
				GetColorByte(pixels, index, originalSize, 2, 0, ref array2[7]);
				GetColorByte(pixels, index, originalSize, -1, 1, ref array2[8]);
				GetColorByte(pixels, index, originalSize, 0, 1, ref array2[9]);
				GetColorByte(pixels, index, originalSize, 1, 1, ref array2[10]);
				GetColorByte(pixels, index, originalSize, 2, 1, ref array2[11]);
				GetColorByte(pixels, index, originalSize, -1, 2, ref array2[12]);
				GetColorByte(pixels, index, originalSize, 0, 2, ref array2[13]);
				GetColorByte(pixels, index, originalSize, 1, 2, ref array2[14]);
				GetColorByte(pixels, index, originalSize, 2, 2, ref array2[15]);
				for (int l = 0; l < 4; l++)
				{
					float a = CubicHermite((int)array2[0][l], (int)array2[1][l], (int)array2[2][l], (int)array2[3][l], t);
					float b = CubicHermite((int)array2[4][l], (int)array2[5][l], (int)array2[6][l], (int)array2[7][l], t);
					float c = CubicHermite((int)array2[8][l], (int)array2[9][l], (int)array2[10][l], (int)array2[11][l], t);
					float d = CubicHermite((int)array2[12][l], (int)array2[13][l], (int)array2[14][l], (int)array2[15][l], t);
					float value = CubicHermite(a, b, c, d, t2);
					value = MathHelper.Clamp(value, 0f, 255f);
					array[num2 + l] = (byte)value;
				}
				num2 += 4;
			}
		}
		return array;
	}

	private static void GetColorByte(byte[] pixels, int index, int originalSize, int x, int y, ref byte[] colorBytes)
	{
		int num = index + originalSize * 4 * y + 4 * x;
		int max = originalSize * originalSize * 4 - 1;
		colorBytes[0] = pixels[MathHelper.Clamp(num, 0, max)];
		colorBytes[1] = pixels[MathHelper.Clamp(num + 1, 0, max)];
		colorBytes[2] = pixels[MathHelper.Clamp(num + 2, 0, max)];
		colorBytes[3] = pixels[MathHelper.Clamp(num + 3, 0, max)];
	}
}
