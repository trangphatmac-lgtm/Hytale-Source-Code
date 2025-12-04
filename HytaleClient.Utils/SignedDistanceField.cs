using System;

namespace HytaleClient.Utils;

internal class SignedDistanceField
{
	private const int OutputZeroValue = 127;

	public static byte[] Generate(byte[] input, int inputWidth, int inputHeight, int scaleDownFactor, int outputSpread, int outputBorder, out int outputWidth, out int outputHeight)
	{
		int num = outputSpread * scaleDownFactor;
		outputWidth = inputWidth / scaleDownFactor + outputSpread * 2 + outputBorder * 2;
		outputHeight = inputHeight / scaleDownFactor + outputSpread * 2 + outputBorder * 2;
		byte[] array = new byte[outputWidth * outputHeight * 4];
		for (int i = 0; i < outputHeight; i++)
		{
			for (int j = 0; j < outputWidth; j++)
			{
				byte b = byte.MaxValue;
				if (j > outputBorder && j < outputWidth - outputBorder && i > outputBorder && i < outputHeight - outputBorder)
				{
					int num2 = j * scaleDownFactor + scaleDownFactor / 2 - num;
					int num3 = i * scaleDownFactor + scaleDownFactor / 2 - num;
					int num4 = 0;
					if (num2 >= 0 && num2 < inputWidth && num3 >= 0 && num3 < inputHeight)
					{
						num4 = input[num3 * inputWidth + num2];
					}
					int num5 = num;
					int num6 = System.Math.Max(0, num2 - num5);
					int num7 = System.Math.Min(inputWidth - 1, num2 + num5);
					int num8 = System.Math.Max(0, num3 - num5);
					int num9 = System.Math.Min(inputHeight - 1, num3 + num5);
					int num10 = num5 * num5;
					for (int k = num8; k <= num9; k++)
					{
						for (int l = num6; l <= num7; l++)
						{
							if (num4 != input[k * inputWidth + l])
							{
								int num11 = (num2 - l) * (num2 - l) + (num3 - k) * (num3 - k);
								if (num11 < num10)
								{
									num10 = num11;
								}
							}
						}
					}
					float val = (float)System.Math.Sqrt(num10);
					float num12 = (float)((num4 != 1) ? 1 : (-1)) * System.Math.Min(val, num);
					b = (byte)(127f + num12 * (float)((num12 < 0f) ? 127 : 128) / (float)num);
				}
				int num13 = 4 * (i * outputWidth + j);
				array[num13] = b;
				array[num13 + 1] = b;
				array[num13 + 2] = b;
				array[num13 + 3] = byte.MaxValue;
			}
		}
		return array;
	}
}
