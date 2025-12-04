using System;
using HytaleClient.Core;
using HytaleClient.Graphics.Map;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Gizmos;

internal class BlockShapeRenderer : Disposable
{
	private readonly GraphicsDevice _graphics;

	private GLVertexArray _vertexArray;

	private GLBuffer _verticesBuffer;

	private GLBuffer _indicesBuffer;

	private Matrix _matrix;

	private int _quadIndicesTotal;

	private int _lineIndicesTotal;

	private float[] _vertices;

	private ushort[] _indices;

	private static readonly sbyte[,] Edges = new sbyte[12, 9]
	{
		{ 0, -1, -1, 0, -1, 0, 0, 0, -1 },
		{ 0, 1, -1, 0, 1, 0, 0, 0, -1 },
		{ -1, 0, -1, -1, 0, 0, 0, 0, -1 },
		{ 1, 0, -1, 1, 0, 0, 0, 0, -1 },
		{ 0, -1, 1, 0, -1, 0, 0, 0, 1 },
		{ 0, 1, 1, 0, 1, 0, 0, 0, 1 },
		{ -1, 0, 1, -1, 0, 0, 0, 0, 1 },
		{ 1, 0, 1, 1, 0, 0, 0, 0, 1 },
		{ -1, 1, 0, -1, 0, 0, 0, 1, 0 },
		{ 1, 1, 0, 1, 0, 0, 0, 1, 0 },
		{ -1, -1, 0, -1, 0, 0, 0, -1, 0 },
		{ 1, -1, 0, 1, 0, 0, 0, -1, 0 }
	};

	private static readonly byte[,] EdgeLines = new byte[12, 6]
	{
		{ 0, 0, 0, 1, 0, 0 },
		{ 0, 1, 0, 1, 1, 0 },
		{ 0, 0, 0, 0, 1, 0 },
		{ 1, 0, 0, 1, 1, 0 },
		{ 0, 0, 1, 1, 0, 1 },
		{ 0, 1, 1, 1, 1, 1 },
		{ 0, 0, 1, 0, 1, 1 },
		{ 1, 0, 1, 1, 1, 1 },
		{ 0, 1, 0, 0, 1, 1 },
		{ 1, 1, 0, 1, 1, 1 },
		{ 0, 0, 0, 0, 0, 1 },
		{ 1, 0, 0, 1, 0, 1 }
	};

	public BlockShapeRenderer(GraphicsDevice graphics, int vertPositionAttrib = -1, int vertTexCoordsAttrib = -1)
	{
		_graphics = graphics;
		GLFunctions gL = graphics.GL;
		_vertexArray = gL.GenVertexArray();
		gL.BindVertexArray(_vertexArray);
		_verticesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		_indicesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		if (vertPositionAttrib != -1)
		{
			gL.EnableVertexAttribArray((uint)vertPositionAttrib);
			gL.VertexAttribPointer((uint)vertPositionAttrib, 3, GL.FLOAT, normalized: false, 32, IntPtr.Zero);
		}
		if (vertTexCoordsAttrib != -1)
		{
			gL.EnableVertexAttribArray((uint)vertTexCoordsAttrib);
			gL.VertexAttribPointer((uint)vertTexCoordsAttrib, 2, GL.FLOAT, normalized: false, 32, (IntPtr)12);
		}
	}

	protected override void DoDispose()
	{
		_graphics.GL.DeleteBuffer(_verticesBuffer);
		_graphics.GL.DeleteBuffer(_indicesBuffer);
		_graphics.GL.DeleteVertexArray(_vertexArray);
	}

	public unsafe void UpdateModelData(bool[,,] blockData, int xOffset, int yOffset, int zOffset)
	{
		int length = blockData.GetLength(0);
		int length2 = blockData.GetLength(1);
		int length3 = blockData.GetLength(2);
		int num = length * length3;
		int num2 = 0;
		int num3 = 0;
		int num4 = length * length2 * length3 * 6;
		ushort[] array = new ushort[num4];
		int num5 = 0;
		bool[] array2 = new bool[3];
		for (short num6 = 0; num6 < length; num6++)
		{
			for (short num7 = 0; num7 < length2; num7++)
			{
				for (short num8 = 0; num8 < length3; num8++)
				{
					if (blockData[num6, num7, num8])
					{
						for (int i = 0; i < 6; i++)
						{
							Vector3 normal = ChunkGeometryBuilder.AdjacentBlockOffsetsBySide[i].Normal;
							int num9 = num6 + (int)normal.X;
							int num10 = num7 + (int)normal.Y;
							int num11 = num8 + (int)normal.Z;
							if (num9 < 0 || num9 >= length || num10 < 0 || num10 >= length2 || num11 < 0 || num11 >= length3 || !blockData[num9, num10, num11])
							{
								num2 += 6;
							}
						}
						for (int j = 0; j < Edges.GetLength(0); j++)
						{
							for (int k = 0; k < 3; k++)
							{
								int num12 = k * 3;
								int num9 = num6 + Edges[j, num12];
								int num10 = num7 + Edges[j, num12 + 1];
								int num11 = num8 + Edges[j, num12 + 2];
								array2[k] = num9 >= 0 && num9 < length && num10 >= 0 && num10 < length2 && num11 >= 0 && num11 < length3 && blockData[num9, num10, num11];
							}
							if ((!array2[0] || !array2[1] || !array2[2]) && (array2[0] || array2[1] || !array2[2]) && (array2[0] || !array2[1] || array2[2]) && ((!array2[0] && !array2[1] && !array2[2]) || ((num6 <= 0 || (j != 2 && j != 6 && j != 8 && j != 10)) && (num7 <= 0 || (j != 0 && j != 4 && j != 10 && j != 11)) && (num8 <= 0 || (j != 0 && j != 1 && j != 2 && j != 3)))))
							{
								num3 += 2;
								if (num5 + 6 >= array.Length)
								{
									Array.Resize(ref array, array.Length * 2);
								}
								array[num5++] = (ushort)(num6 + EdgeLines[j, 0]);
								array[num5++] = (ushort)(num7 + EdgeLines[j, 1]);
								array[num5++] = (ushort)(num8 + EdgeLines[j, 2]);
								array[num5++] = (ushort)(num6 + EdgeLines[j, 3]);
								array[num5++] = (ushort)(num7 + EdgeLines[j, 4]);
								array[num5++] = (ushort)(num8 + EdgeLines[j, 5]);
							}
						}
					}
				}
			}
		}
		_indices = new ushort[num2 + num3];
		_quadIndicesTotal = num2;
		_lineIndicesTotal = num3;
		ushort[] vertArray = new ushort[System.Math.Max(num2 / 4, 32)];
		int vertArrayOffset = 0;
		int num13 = 0;
		ushort vertCount = 1;
		ushort[,,] vertLookup = new ushort[length + 1, length2 + 1, length3 + 1];
		for (ushort num14 = 0; num14 < length; num14++)
		{
			for (ushort num15 = 0; num15 < length2; num15++)
			{
				for (ushort num16 = 0; num16 < length3; num16++)
				{
					if (blockData[num14, num15, num16])
					{
						for (int l = 0; l < 6; l++)
						{
							Vector3 normal2 = ChunkGeometryBuilder.AdjacentBlockOffsetsBySide[l].Normal;
							int num17 = num14 + (int)normal2.X;
							int num18 = num15 + (int)normal2.Y;
							int num19 = num16 + (int)normal2.Z;
							if (num17 < 0 || num17 >= length || num18 < 0 || num18 >= length2 || num19 < 0 || num19 >= length3 || !blockData[num17, num18, num19])
							{
								ushort[] array3 = new ushort[4];
								for (int m = 0; m < 4; m++)
								{
									array3[m] = addVert((ushort)(ChunkGeometryBuilder.CornersPerSide[l][m].X + (float)(int)num14), (ushort)(ChunkGeometryBuilder.CornersPerSide[l][m].Y + (float)(int)num15), (ushort)(ChunkGeometryBuilder.CornersPerSide[l][m].Z + (float)(int)num16));
								}
								_indices[num13++] = array3[0];
								_indices[num13++] = array3[1];
								_indices[num13++] = array3[2];
								_indices[num13++] = array3[0];
								_indices[num13++] = array3[2];
								_indices[num13++] = array3[3];
							}
						}
					}
				}
			}
		}
		for (int n = 0; n < num5; n += 6)
		{
			_indices[num13++] = addVert(array[n], array[n + 1], array[n + 2]);
			_indices[num13++] = addVert(array[n + 3], array[n + 4], array[n + 5]);
		}
		vertCount--;
		if (vertArray.Length != 0)
		{
			_vertices = new float[vertCount * 8];
			for (int num20 = 0; num20 < vertCount; num20++)
			{
				int num21 = num20 * 3;
				int num22 = num20 * 8;
				_vertices[num22] = vertArray[num21] + xOffset;
				_vertices[num22 + 1] = vertArray[num21 + 1] + yOffset;
				_vertices[num22 + 2] = vertArray[num21 + 2] + zOffset;
			}
			GLFunctions gL = _graphics.GL;
			gL.BindVertexArray(_vertexArray);
			gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
			fixed (float* ptr = _vertices)
			{
				gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(_vertices.Length * 4), (IntPtr)ptr, GL.STATIC_DRAW);
			}
			gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
			fixed (ushort* ptr2 = _indices)
			{
				gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(_indices.Length * 2), (IntPtr)ptr2, GL.STATIC_DRAW);
			}
		}
		ushort addVert(ushort x, ushort y, ushort z)
		{
			ushort num23 = vertLookup[x, y, z];
			if (num23 != 0)
			{
				return (ushort)(num23 - 1);
			}
			if (vertArrayOffset + 3 >= vertArray.Length)
			{
				Array.Resize(ref vertArray, (int)((double)vertArray.Length * 1.2));
			}
			vertLookup[x, y, z] = vertCount++;
			vertArray[vertArrayOffset++] = x;
			vertArray[vertArrayOffset++] = y;
			vertArray[vertArrayOffset++] = z;
			return (ushort)(vertCount - 2);
		}
	}

	public void DrawBlockShapeOutline()
	{
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		gL.DrawElements(GL.ONE, _lineIndicesTotal, GL.UNSIGNED_SHORT, (IntPtr)(_quadIndicesTotal * 2));
	}

	public void DrawBlockShape()
	{
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		gL.DrawElements(GL.TRIANGLES, _quadIndicesTotal, GL.UNSIGNED_SHORT, (IntPtr)0);
	}
}
