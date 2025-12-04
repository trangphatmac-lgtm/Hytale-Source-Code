namespace HytaleClient.Graphics.Gizmos.Models;

public class CubeModel
{
	private static readonly int[] CubeVertices = new int[24]
	{
		-1, -1, 1, 1, -1, 1, 1, 1, 1, -1,
		1, 1, -1, -1, -1, -1, 1, -1, 1, 1,
		-1, 1, -1, -1
	};

	private static readonly ushort[] CubeVertexIndices = new ushort[36]
	{
		0, 1, 2, 0, 2, 3, 4, 5, 6, 4,
		6, 7, 5, 3, 2, 5, 2, 6, 4, 7,
		1, 4, 1, 0, 7, 6, 2, 7, 2, 1,
		4, 0, 3, 4, 3, 5
	};

	public static PrimitiveModelData BuildModelData(float halfWidth, float halfHeight, float halfDepth = 0f)
	{
		if (halfDepth == 0f)
		{
			halfDepth = halfWidth;
		}
		float[] array = new float[64];
		for (int i = 0; i < 8; i++)
		{
			int num = i * 8;
			array[num] = (float)CubeVertices[i * 3] * halfWidth;
			array[num + 1] = (float)CubeVertices[i * 3 + 1] * halfHeight;
			array[num + 2] = (float)CubeVertices[i * 3 + 2] * halfDepth;
			array[num + 3] = 0f;
			array[num + 4] = 0f;
			array[num + 5] = 0f;
			array[num + 6] = 0f;
			array[num + 7] = 0f;
		}
		return new PrimitiveModelData(array, CubeVertexIndices);
	}

	public static bool[,,] BuildVoxelData(int radiusX, int radiusY, int radiusZ)
	{
		bool[,,] array = new bool[radiusX * 2 + 1, radiusY * 2 + 1, radiusZ * 2 + 1];
		for (int i = -radiusX; i <= radiusX; i++)
		{
			for (int j = -radiusZ; j <= radiusZ; j++)
			{
				for (int num = radiusY; num >= -radiusY; num--)
				{
					array[i + radiusX, num + radiusY, j + radiusZ] = true;
				}
			}
		}
		return array;
	}
}
