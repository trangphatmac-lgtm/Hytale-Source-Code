using System;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Gizmos.Models;

public class PrimitiveModelData
{
	public const int VertexSize = 8;

	public readonly float[] Vertices;

	public readonly ushort[] Indices;

	public PrimitiveModelData(float[] vertices, ushort[] indices)
	{
		Vertices = vertices;
		Indices = indices;
	}

	public static ushort[] MakeRadialIndices(int segments, int rings)
	{
		int num = 0;
		ushort[] array = new ushort[segments * rings * 6];
		for (int i = 0; i < rings - 1; i++)
		{
			for (int j = 0; j < segments - 1; j++)
			{
				array[num++] = (ushort)(i * segments + j);
				array[num++] = (ushort)((i + 1) * segments + j);
				array[num++] = (ushort)((i + 1) * segments + j + 1);
				array[num++] = (ushort)((i + 1) * segments + j + 1);
				array[num++] = (ushort)(i * segments + j + 1);
				array[num++] = (ushort)(i * segments + j);
			}
		}
		return array;
	}

	public void OffsetVertices(Vector3 offset)
	{
		for (int i = 0; i < Vertices.Length - 1; i += 8)
		{
			Vertices[i] += offset.X;
			Vertices[i + 1] += offset.Y;
			Vertices[i + 2] += offset.Z;
		}
	}

	public static PrimitiveModelData CombineData(PrimitiveModelData model1, PrimitiveModelData model2)
	{
		float[] vertices = model1.Vertices;
		float[] vertices2 = model2.Vertices;
		float[] array = new float[vertices.Length + vertices2.Length];
		Array.Copy(vertices, array, vertices.Length);
		Array.Copy(vertices2, 0, array, vertices.Length, vertices2.Length);
		ushort[] indices = model1.Indices;
		ushort[] indices2 = model2.Indices;
		ushort[] array2 = new ushort[indices.Length + indices2.Length];
		Array.Copy(indices, array2, indices.Length);
		Array.Copy(indices2, 0, array2, indices.Length, indices2.Length);
		ushort num = 0;
		for (int i = 0; i < indices.Length; i++)
		{
			if (num == 0 || array2[i] > num)
			{
				num = array2[i];
			}
		}
		num++;
		for (int j = indices.Length; j < indices.Length + indices2.Length; j++)
		{
			array2[j] += num;
		}
		return new PrimitiveModelData(array, array2);
	}
}
