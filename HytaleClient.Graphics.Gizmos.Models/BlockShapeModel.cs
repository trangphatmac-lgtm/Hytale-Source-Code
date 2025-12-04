using HytaleClient.Graphics.Map;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Gizmos.Models;

public class BlockShapeModel
{
	public static PrimitiveModelData BuildModelData(bool[,,] blockData, Vector3 positionOffset)
	{
		int length = blockData.GetLength(0);
		int length2 = blockData.GetLength(1);
		int length3 = blockData.GetLength(2);
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < length; i++)
		{
			for (int j = 0; j < length2; j++)
			{
				for (int k = 0; k < length3; k++)
				{
					if (!blockData[i, j, k])
					{
						continue;
					}
					for (int l = 0; l < 6; l++)
					{
						Vector3 normal = ChunkGeometryBuilder.AdjacentBlockOffsetsBySide[l].Normal;
						int num3 = i + (int)normal.X;
						int num4 = j + (int)normal.Y;
						int num5 = k + (int)normal.Z;
						if (num3 < 0 || num3 >= length || num4 < 0 || num4 >= length2 || num5 < 0 || num5 >= length3 || !blockData[num3, num4, num5])
						{
							num += 32;
							num2 += 6;
						}
					}
				}
			}
		}
		float[] array = new float[num];
		ushort[] array2 = new ushort[num2];
		int num6 = 0;
		int num7 = 0;
		for (int m = 0; m < length; m++)
		{
			for (int n = 0; n < length2; n++)
			{
				for (int num8 = 0; num8 < length3; num8++)
				{
					if (!blockData[m, n, num8])
					{
						continue;
					}
					for (int num9 = 0; num9 < 6; num9++)
					{
						Vector3 normal2 = ChunkGeometryBuilder.AdjacentBlockOffsetsBySide[num9].Normal;
						int num10 = m + (int)normal2.X;
						int num11 = n + (int)normal2.Y;
						int num12 = num8 + (int)normal2.Z;
						if (num10 < 0 || num10 >= length || num11 < 0 || num11 >= length2 || num12 < 0 || num12 >= length3 || !blockData[num10, num11, num12])
						{
							for (int num13 = 0; num13 < 4; num13++)
							{
								int num14 = num6 * 8 + num13 * 8;
								array[num14] = ChunkGeometryBuilder.CornersPerSide[num9][num13].X + (float)m + positionOffset.X;
								array[num14 + 1] = ChunkGeometryBuilder.CornersPerSide[num9][num13].Y + (float)n + positionOffset.Y;
								array[num14 + 2] = ChunkGeometryBuilder.CornersPerSide[num9][num13].Z + (float)num8 + positionOffset.Z;
								array[num14 + 3] = 0f;
								array[num14 + 4] = 0f;
								array[num14 + 5] = 0f;
								array[num14 + 6] = 0f;
								array[num14 + 7] = 0f;
							}
							array2[num7] = (ushort)num6;
							array2[num7 + 1] = (ushort)(num6 + 1);
							array2[num7 + 2] = (ushort)(num6 + 2);
							array2[num7 + 3] = (ushort)num6;
							array2[num7 + 4] = (ushort)(num6 + 2);
							array2[num7 + 5] = (ushort)(num6 + 3);
							num6 += 4;
							num7 += 6;
						}
					}
				}
			}
		}
		return new PrimitiveModelData(array, array2);
	}
}
