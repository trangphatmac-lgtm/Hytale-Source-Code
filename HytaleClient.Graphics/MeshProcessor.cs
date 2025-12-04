using System;
using HytaleClient.Math;

namespace HytaleClient.Graphics;

internal static class MeshProcessor
{
	private static GLFunctions _gl;

	public static void InitializeGL(GLFunctions gl)
	{
		_gl = gl;
	}

	public static void ReleaseGL()
	{
		_gl = null;
	}

	public unsafe static void CreateQuad(ref Mesh result, float size = 1f, int vertPositionAttrib = -1, int vertTexCoordsAttrib = -1, int vertNormalAttrib = -1)
	{
		result.Count = 6;
		float num = size * 0.5f;
		float[] obj = new float[48]
		{
			0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f, 0f, 0f,
			0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f,
			1f, 0f, 0f, 1f, 0f, 0f, 0f, 1f, 0f, 0f,
			0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 1f,
			0f, 0f, 0f, 1f, 1f, 0f, 0f, 1f
		};
		obj[0] = num;
		obj[1] = num;
		obj[2] = 0f - num;
		obj[8] = 0f - num;
		obj[9] = num;
		obj[10] = 0f - num;
		obj[16] = 0f - num;
		obj[17] = 0f - num;
		obj[18] = 0f - num;
		obj[24] = num;
		obj[25] = num;
		obj[26] = 0f - num;
		obj[32] = 0f - num;
		obj[33] = 0f - num;
		obj[34] = 0f - num;
		obj[40] = num;
		obj[41] = 0f - num;
		obj[42] = 0f - num;
		float[] array = obj;
		if (result.VertexArray == GLVertexArray.None)
		{
			result.VertexArray = _gl.GenVertexArray();
		}
		_gl.BindVertexArray(result.VertexArray);
		if (result.VerticesBuffer == GLBuffer.None)
		{
			result.VerticesBuffer = _gl.GenBuffer();
		}
		_gl.BindBuffer(GL.ARRAY_BUFFER, result.VerticesBuffer);
		fixed (float* ptr = array)
		{
			_gl.BufferData(GL.ARRAY_BUFFER, (IntPtr)(array.Length * 4), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		if (vertPositionAttrib != -1)
		{
			_gl.EnableVertexAttribArray((uint)vertPositionAttrib);
			_gl.VertexAttribPointer((uint)vertPositionAttrib, 3, GL.FLOAT, normalized: false, 32, IntPtr.Zero);
		}
		if (vertTexCoordsAttrib != -1)
		{
			_gl.EnableVertexAttribArray((uint)vertTexCoordsAttrib);
			_gl.VertexAttribPointer((uint)vertTexCoordsAttrib, 2, GL.FLOAT, normalized: false, 32, (IntPtr)12);
		}
		if (vertNormalAttrib != -1)
		{
			_gl.EnableVertexAttribArray((uint)vertNormalAttrib);
			_gl.VertexAttribPointer((uint)vertNormalAttrib, 3, GL.FLOAT, normalized: false, 32, (IntPtr)20);
		}
	}

	public unsafe static void CreateSimpleBox(ref Mesh result, float size = 1f)
	{
		float num = size * 0.5f;
		float[] array = new float[24]
		{
			0f - num,
			0f - num,
			num,
			num,
			0f - num,
			num,
			num,
			num,
			num,
			0f - num,
			num,
			num,
			0f - num,
			0f - num,
			0f - num,
			num,
			0f - num,
			0f - num,
			num,
			num,
			0f - num,
			0f - num,
			num,
			0f - num
		};
		ushort[] array2 = new ushort[36]
		{
			0, 1, 2, 2, 3, 0, 1, 5, 6, 6,
			2, 1, 7, 6, 5, 5, 4, 7, 4, 0,
			3, 3, 7, 4, 4, 5, 1, 1, 0, 4,
			3, 2, 6, 6, 7, 3
		};
		result.Count = (ushort)array2.Length;
		if (result.VertexArray == GLVertexArray.None)
		{
			result.VertexArray = _gl.GenVertexArray();
		}
		_gl.BindVertexArray(result.VertexArray);
		if (result.VerticesBuffer == GLBuffer.None)
		{
			result.VerticesBuffer = _gl.GenBuffer();
		}
		_gl.BindBuffer(GL.ARRAY_BUFFER, result.VerticesBuffer);
		fixed (float* ptr = array)
		{
			_gl.BufferData(GL.ARRAY_BUFFER, (IntPtr)(array.Length * 4), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		if (result.IndicesBuffer == GLBuffer.None)
		{
			result.IndicesBuffer = _gl.GenBuffer();
		}
		_gl.BindBuffer(GL.ELEMENT_ARRAY_BUFFER, result.IndicesBuffer);
		fixed (ushort* ptr2 = array2)
		{
			_gl.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(array2.Length * 2), (IntPtr)ptr2, GL.STATIC_DRAW);
		}
		_gl.EnableVertexAttribArray(0u);
		_gl.VertexAttribPointer(0u, 3, GL.FLOAT, normalized: false, 12, IntPtr.Zero);
	}

	public unsafe static void CreateBox(ref Mesh result, float size = 1f, int vertPositionAttrib = -1, int vertTexCoordsAttrib = -1, int vertNormalAttrib = -1)
	{
		result.Count = 36;
		float num = size * 0.5f;
		float[] array = new float[108]
		{
			0f - num,
			0f - num,
			0f - num,
			num,
			0f - num,
			0f - num,
			0f - num,
			0f - num,
			num,
			0f - num,
			0f - num,
			num,
			num,
			0f - num,
			0f - num,
			num,
			0f - num,
			num,
			num,
			0f - num,
			0f - num,
			num,
			num,
			0f - num,
			num,
			0f - num,
			num,
			num,
			0f - num,
			num,
			num,
			num,
			0f - num,
			num,
			num,
			num,
			num,
			num,
			0f - num,
			0f - num,
			num,
			num,
			num,
			num,
			num,
			0f - num,
			num,
			0f - num,
			0f - num,
			num,
			num,
			num,
			num,
			0f - num,
			0f - num,
			num,
			num,
			0f - num,
			num,
			0f - num,
			0f - num,
			0f - num,
			num,
			0f - num,
			num,
			0f - num,
			0f - num,
			0f - num,
			0f - num,
			0f - num,
			0f - num,
			num,
			0f - num,
			0f - num,
			num,
			num,
			0f - num,
			num,
			0f - num,
			num,
			num,
			num,
			0f - num,
			num,
			num,
			num,
			num,
			0f - num,
			num,
			num,
			0f - num,
			0f - num,
			0f - num,
			0f - num,
			num,
			0f - num,
			num,
			0f - num,
			0f - num,
			num,
			0f - num,
			0f - num,
			0f - num,
			num,
			0f - num,
			num,
			num,
			0f - num
		};
		float[] array2 = new float[108]
		{
			0f, -1f, 0f, 0f, -1f, 0f, 0f, -1f, 0f, 0f,
			-1f, 0f, 0f, -1f, 0f, 0f, -1f, 0f, 1f, 0f,
			0f, 1f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 0f,
			1f, 0f, 0f, 1f, 0f, 0f, 0f, 1f, 0f, 0f,
			1f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 0f, 1f,
			0f, 0f, 1f, 0f, -1f, 0f, 0f, -1f, 0f, 0f,
			-1f, 0f, 0f, -1f, 0f, 0f, -1f, 0f, 0f, -1f,
			0f, 0f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 0f,
			1f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 0f, 1f,
			0f, 0f, -1f, 0f, 0f, -1f, 0f, 0f, -1f, 0f,
			0f, -1f, 0f, 0f, -1f, 0f, 0f, -1f
		};
		float[] array3 = new float[72]
		{
			0f, 0f, 1f, 0f, 0f, 1f, 0f, 1f, 1f, 0f,
			1f, 1f, 0f, 0f, 0f, 1f, 1f, 0f, 1f, 0f,
			0f, 1f, 1f, 1f, 0f, 0f, 1f, 1f, 1f, 0f,
			0f, 1f, 1f, 1f, 0f, 0f, 1f, 1f, 0f, 1f,
			1f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 0f, 0f,
			1f, 0f, 0f, 1f, 1f, 0f, 1f, 1f, 0f, 1f,
			0f, 0f, 0f, 1f, 1f, 0f, 1f, 0f, 0f, 1f,
			1f, 1f
		};
		if (result.VertexArray == GLVertexArray.None)
		{
			result.VertexArray = _gl.GenVertexArray();
		}
		_gl.BindVertexArray(result.VertexArray);
		if (result.VerticesBuffer == GLBuffer.None)
		{
			result.VerticesBuffer = _gl.GenBuffer();
		}
		_gl.BindBuffer(GL.ARRAY_BUFFER, result.VerticesBuffer);
		int num2 = ((vertPositionAttrib != -1) ? (array.Length * 4 * 3) : 0);
		int num3 = ((vertTexCoordsAttrib != -1) ? (array3.Length * 4 * 2) : 0);
		int num4 = ((vertNormalAttrib != -1) ? (array2.Length * 4 * 3) : 0);
		int num5 = num2 + num3 + num4;
		_gl.BufferData(GL.ARRAY_BUFFER, (IntPtr)num5, IntPtr.Zero, GL.STATIC_DRAW);
		int num6 = 0;
		if (num2 != 0)
		{
			fixed (float* ptr = array)
			{
				_gl.BufferSubData(GL.ARRAY_BUFFER, (IntPtr)num6, (IntPtr)num2, (IntPtr)ptr);
			}
			num6 += num2;
			_gl.EnableVertexAttribArray((uint)vertPositionAttrib);
			_gl.VertexAttribPointer((uint)vertPositionAttrib, 3, GL.FLOAT, normalized: false, 12, IntPtr.Zero);
		}
		if (num3 != 0)
		{
			fixed (float* ptr2 = array3)
			{
				_gl.BufferSubData(GL.ARRAY_BUFFER, (IntPtr)num6, (IntPtr)num3, (IntPtr)ptr2);
			}
			num6 += num3;
			_gl.EnableVertexAttribArray((uint)vertTexCoordsAttrib);
			_gl.VertexAttribPointer((uint)vertTexCoordsAttrib, 2, GL.FLOAT, normalized: false, 8, (IntPtr)num2);
		}
		if (num4 != 0)
		{
			fixed (float* ptr3 = array2)
			{
				_gl.BufferSubData(GL.ARRAY_BUFFER, (IntPtr)num6, (IntPtr)num4, (IntPtr)ptr3);
			}
			_gl.EnableVertexAttribArray((uint)vertNormalAttrib);
			_gl.VertexAttribPointer((uint)vertNormalAttrib, 3, GL.FLOAT, normalized: false, 12, (IntPtr)(num2 + num3));
		}
	}

	public unsafe static void CreateCylinder(ref Mesh result, ushort meridianCount = 8, float radius = 1f, int vertPositionAttrib = -1, int vertTexCoordsAttrib = -1, int vertNormalAttrib = -1)
	{
		ushort num = 2;
		ushort num2 = meridianCount;
		uint num3 = (ushort)(num * num2 + 2);
		Vector3[] array = new Vector3[num3];
		Vector2[] array2 = new Vector2[num3];
		Vector3[] array3 = new Vector3[num3];
		double num4 = 180.0 / (double)(num + 1) * 3.141592 / 180.0;
		double num5 = 360.0 / (double)(num2 - 1) * 3.141592 / 180.0;
		int num6 = 0;
		for (int i = 0; i < num; i++)
		{
			float num7 = radius * (float)(i * 2 - 1);
			float num8 = radius;
			for (int j = 0; j < num2; j++)
			{
				array[num6] = new Vector3((float)((double)num8 * System.Math.Cos(num5 * (double)j)), num7, (float)((double)num8 * System.Math.Sin(num5 * (double)j)));
				array2[num6] = new Vector2((float)j / (float)(int)num2, (float)(num - i) / (float)(int)num);
				array3[num6] = Vector3.Normalize(array[num6]);
				num6++;
			}
		}
		array[num6] = new Vector3(0f, 0f - radius, 0f);
		array2[num6] = new Vector2(1f, 1f);
		array3[num6] = Vector3.Normalize(array[num6]);
		num6++;
		array[num6] = new Vector3(0f, radius, 0f);
		array2[num6] = new Vector2(0f, 0f);
		array3[num6] = Vector3.Normalize(array[num6]);
		num6++;
		result.Count = (ushort)(num2 * (num - 1) * 2 * 3);
		result.Count += (ushort)(num2 * 2 * 3);
		ushort[] array4 = new ushort[result.Count];
		num6 = 0;
		for (int k = 0; k < num - 1; k++)
		{
			for (int l = 0; l < num2; l++)
			{
				array4[num6++] = (ushort)(num2 * k + l);
				array4[num6++] = (ushort)(num2 * (k + 1) + (l + 1) % num2);
				array4[num6++] = (ushort)(num2 * k + (l + 1) % num2);
				array4[num6++] = (ushort)(num2 * (k + 1) + (l + 1) % num2);
				array4[num6++] = (ushort)(num2 * k + l);
				array4[num6++] = (ushort)(num2 * (k + 1) + l);
			}
		}
		for (int m = 0; m < num2; m++)
		{
			array4[num6++] = (ushort)(num2 * num);
			array4[num6++] = (ushort)m;
			array4[num6++] = (ushort)((m + 1) % num2);
		}
		for (int n = 0; n < num2; n++)
		{
			array4[num6++] = (ushort)(num2 * num + 1);
			array4[num6++] = (ushort)(num2 * (num - 1) + (n + 1) % num2);
			array4[num6++] = (ushort)(num2 * (num - 1) + n);
		}
		if (result.VertexArray == GLVertexArray.None)
		{
			result.VertexArray = _gl.GenVertexArray();
		}
		_gl.BindVertexArray(result.VertexArray);
		if (result.VerticesBuffer == GLBuffer.None)
		{
			result.VerticesBuffer = _gl.GenBuffer();
		}
		_gl.BindBuffer(result.VertexArray, GL.ARRAY_BUFFER, result.VerticesBuffer);
		int num9 = ((vertPositionAttrib != -1) ? (array.Length * 4 * 3) : 0);
		int num10 = ((vertTexCoordsAttrib != -1) ? (array2.Length * 4 * 2) : 0);
		int num11 = ((vertNormalAttrib != -1) ? (array3.Length * 4 * 3) : 0);
		int num12 = num9 + num10 + num11;
		_gl.BufferData(GL.ARRAY_BUFFER, (IntPtr)num12, IntPtr.Zero, GL.STATIC_DRAW);
		int num13 = 0;
		if (num9 != 0)
		{
			fixed (Vector3* ptr = array)
			{
				_gl.BufferSubData(GL.ARRAY_BUFFER, (IntPtr)num13, (IntPtr)num9, (IntPtr)ptr);
			}
			num13 += num9;
			_gl.EnableVertexAttribArray((uint)vertPositionAttrib);
			_gl.VertexAttribPointer((uint)vertPositionAttrib, 3, GL.FLOAT, normalized: false, 0, IntPtr.Zero);
		}
		if (num10 != 0)
		{
			fixed (Vector2* ptr2 = array2)
			{
				_gl.BufferSubData(GL.ARRAY_BUFFER, (IntPtr)num13, (IntPtr)num10, (IntPtr)ptr2);
			}
			num13 += num10;
			_gl.EnableVertexAttribArray((uint)vertTexCoordsAttrib);
			_gl.VertexAttribPointer((uint)vertTexCoordsAttrib, 2, GL.FLOAT, normalized: false, 0, (IntPtr)num9);
		}
		if (num11 != 0)
		{
			fixed (Vector3* ptr3 = array3)
			{
				_gl.BufferSubData(GL.ARRAY_BUFFER, (IntPtr)num13, (IntPtr)num11, (IntPtr)ptr3);
			}
			_gl.EnableVertexAttribArray((uint)vertNormalAttrib);
			_gl.VertexAttribPointer((uint)vertNormalAttrib, 3, GL.FLOAT, normalized: false, 0, (IntPtr)(num9 + num10));
		}
		if (result.IndicesBuffer == GLBuffer.None)
		{
			result.IndicesBuffer = _gl.GenBuffer();
		}
		_gl.BindBuffer(result.VertexArray, GL.ELEMENT_ARRAY_BUFFER, result.IndicesBuffer);
		fixed (ushort* ptr4 = array4)
		{
			_gl.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(array4.Length * 2), (IntPtr)ptr4, GL.STATIC_DRAW);
		}
	}

	public unsafe static void CreateCone(ref Mesh result, ushort meridianCount = 8, float radius = 1f, int vertPositionAttrib = -1, int vertTexCoordsAttrib = -1, int vertNormalAttrib = -1)
	{
		ushort num = meridianCount;
		uint num2 = (ushort)(num + 2);
		Vector3[] array = new Vector3[num2];
		Vector2[] array2 = new Vector2[num2];
		Vector3[] array3 = new Vector3[num2];
		double num3 = 1.570796;
		double num4 = 360.0 / (double)(num - 1) * 3.141592 / 180.0;
		int num5 = 0;
		float num6 = 0f - radius;
		for (int i = 0; i < num; i++)
		{
			array[num5] = new Vector3((float)((double)radius * System.Math.Cos(num4 * (double)i)), num6, (float)((double)radius * System.Math.Sin(num4 * (double)i)));
			array2[num5] = new Vector2((float)i / (float)(int)num, 1f);
			array3[num5] = Vector3.Normalize(array[num5]);
			num5++;
		}
		array[num5] = new Vector3(0f, 0f - radius, 0f);
		array2[num5] = new Vector2(1f, 1f);
		array3[num5] = Vector3.Normalize(array[num5]);
		num5++;
		array[num5] = new Vector3(0f, radius, 0f);
		array2[num5] = new Vector2(0f, 0f);
		array3[num5] = Vector3.Normalize(array[num5]);
		num5++;
		result.Count = 0;
		result.Count += (ushort)(num * 2 * 3);
		ushort[] array4 = new ushort[result.Count];
		num5 = 0;
		for (int j = 0; j < num; j++)
		{
			array4[num5++] = (ushort)(num + 1);
			array4[num5++] = (ushort)j;
			array4[num5++] = (ushort)((j + 1) % num);
		}
		for (int k = 0; k < num; k++)
		{
			array4[num5++] = num;
			array4[num5++] = (ushort)k;
			array4[num5++] = (ushort)((k + 1) % num);
		}
		if (result.VertexArray == GLVertexArray.None)
		{
			result.VertexArray = _gl.GenVertexArray();
		}
		_gl.BindVertexArray(result.VertexArray);
		if (result.VerticesBuffer == GLBuffer.None)
		{
			result.VerticesBuffer = _gl.GenBuffer();
		}
		_gl.BindBuffer(result.VertexArray, GL.ARRAY_BUFFER, result.VerticesBuffer);
		int num7 = ((vertPositionAttrib != -1) ? (array.Length * 4 * 3) : 0);
		int num8 = ((vertTexCoordsAttrib != -1) ? (array2.Length * 4 * 2) : 0);
		int num9 = ((vertNormalAttrib != -1) ? (array3.Length * 4 * 3) : 0);
		int num10 = num7 + num8 + num9;
		_gl.BufferData(GL.ARRAY_BUFFER, (IntPtr)num10, IntPtr.Zero, GL.STATIC_DRAW);
		int num11 = 0;
		if (num7 != 0)
		{
			fixed (Vector3* ptr = array)
			{
				_gl.BufferSubData(GL.ARRAY_BUFFER, (IntPtr)num11, (IntPtr)num7, (IntPtr)ptr);
			}
			num11 += num7;
			_gl.EnableVertexAttribArray((uint)vertPositionAttrib);
			_gl.VertexAttribPointer((uint)vertPositionAttrib, 3, GL.FLOAT, normalized: false, 0, IntPtr.Zero);
		}
		if (num8 != 0)
		{
			fixed (Vector2* ptr2 = array2)
			{
				_gl.BufferSubData(GL.ARRAY_BUFFER, (IntPtr)num11, (IntPtr)num8, (IntPtr)ptr2);
			}
			num11 += num8;
			_gl.EnableVertexAttribArray((uint)vertTexCoordsAttrib);
			_gl.VertexAttribPointer((uint)vertTexCoordsAttrib, 2, GL.FLOAT, normalized: false, 0, (IntPtr)num7);
		}
		if (num9 != 0)
		{
			fixed (Vector3* ptr3 = array3)
			{
				_gl.BufferSubData(GL.ARRAY_BUFFER, (IntPtr)num11, (IntPtr)num9, (IntPtr)ptr3);
			}
			_gl.EnableVertexAttribArray((uint)vertNormalAttrib);
			_gl.VertexAttribPointer((uint)vertNormalAttrib, 3, GL.FLOAT, normalized: false, 0, (IntPtr)(num7 + num8));
		}
		if (result.IndicesBuffer == GLBuffer.None)
		{
			result.IndicesBuffer = _gl.GenBuffer();
		}
		_gl.BindBuffer(result.VertexArray, GL.ELEMENT_ARRAY_BUFFER, result.IndicesBuffer);
		fixed (ushort* ptr4 = array4)
		{
			_gl.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(array4.Length * 2), (IntPtr)ptr4, GL.STATIC_DRAW);
		}
	}

	public unsafe static void CreateSphere(ref Mesh result, ushort parallelCount = 5, ushort meridianCount = 8, float radius = 1f, int vertPositionAttrib = -1, int vertTexCoordsAttrib = -1, int vertNormalAttrib = -1)
	{
		ushort num = parallelCount;
		ushort num2 = meridianCount;
		uint num3 = (ushort)(num * num2 + 2);
		Vector3[] array = new Vector3[num3];
		Vector2[] array2 = new Vector2[num3];
		Vector3[] array3 = new Vector3[num3];
		double num4 = 180.0 / (double)(num + 1) * 3.141592 / 180.0;
		double num5 = 360.0 / (double)(num2 - 1) * 3.141592 / 180.0;
		int num6 = 0;
		for (int i = 0; i < num; i++)
		{
			double num7 = -1.570796 + num4 * (double)(i + 1);
			double num8 = (double)radius * System.Math.Sin(num7);
			double num9 = (double)radius * System.Math.Cos(num7);
			for (int j = 0; j < num2; j++)
			{
				array[num6] = new Vector3((float)(num9 * System.Math.Cos(num5 * (double)j)), (float)num8, (float)(num9 * System.Math.Sin(num5 * (double)j)));
				array2[num6] = new Vector2((float)j / (float)(int)num2, (float)(num - i) / (float)(int)num);
				array3[num6] = Vector3.Normalize(array[num6]);
				num6++;
			}
		}
		array[num6] = new Vector3(0f, 0f - radius, 0f);
		array2[num6] = new Vector2(0.5f, 1f);
		array3[num6] = Vector3.Normalize(array[num6]);
		num6++;
		array[num6] = new Vector3(0f, radius, 0f);
		array2[num6] = new Vector2(0.5f, 0f);
		array3[num6] = Vector3.Normalize(array[num6]);
		num6++;
		result.Count = (ushort)(num2 * (num - 1) * 2 * 3);
		result.Count += (ushort)(num2 * 2 * 3);
		ushort[] array4 = new ushort[result.Count];
		num6 = 0;
		for (int k = 0; k < num - 1; k++)
		{
			for (int l = 0; l < num2; l++)
			{
				array4[num6++] = (ushort)(num2 * k + l);
				array4[num6++] = (ushort)(num2 * (k + 1) + (l + 1) % num2);
				array4[num6++] = (ushort)(num2 * k + (l + 1) % num2);
				array4[num6++] = (ushort)(num2 * (k + 1) + (l + 1) % num2);
				array4[num6++] = (ushort)(num2 * k + l);
				array4[num6++] = (ushort)(num2 * (k + 1) + l);
			}
		}
		for (int m = 0; m < num2; m++)
		{
			array4[num6++] = (ushort)(num2 * num);
			array4[num6++] = (ushort)m;
			array4[num6++] = (ushort)((m + 1) % num2);
		}
		for (int n = 0; n < num2; n++)
		{
			array4[num6++] = (ushort)(num2 * num + 1);
			array4[num6++] = (ushort)(num2 * (num - 1) + (n + 1) % num2);
			array4[num6++] = (ushort)(num2 * (num - 1) + n);
		}
		if (result.VertexArray == GLVertexArray.None)
		{
			result.VertexArray = _gl.GenVertexArray();
		}
		_gl.BindVertexArray(result.VertexArray);
		if (result.VerticesBuffer == GLBuffer.None)
		{
			result.VerticesBuffer = _gl.GenBuffer();
		}
		_gl.BindBuffer(result.VertexArray, GL.ARRAY_BUFFER, result.VerticesBuffer);
		int num10 = ((vertPositionAttrib != -1) ? (array.Length * 4 * 3) : 0);
		int num11 = ((vertTexCoordsAttrib != -1) ? (array2.Length * 4 * 2) : 0);
		int num12 = ((vertNormalAttrib != -1) ? (array3.Length * 4 * 3) : 0);
		int num13 = num10 + num11 + num12;
		_gl.BufferData(GL.ARRAY_BUFFER, (IntPtr)num13, IntPtr.Zero, GL.STATIC_DRAW);
		int num14 = 0;
		if (num10 != 0)
		{
			fixed (Vector3* ptr = array)
			{
				_gl.BufferSubData(GL.ARRAY_BUFFER, (IntPtr)num14, (IntPtr)num10, (IntPtr)ptr);
			}
			num14 += num10;
			_gl.EnableVertexAttribArray((uint)vertPositionAttrib);
			_gl.VertexAttribPointer((uint)vertPositionAttrib, 3, GL.FLOAT, normalized: false, 0, IntPtr.Zero);
		}
		if (num11 != 0)
		{
			fixed (Vector2* ptr2 = array2)
			{
				_gl.BufferSubData(GL.ARRAY_BUFFER, (IntPtr)num14, (IntPtr)num11, (IntPtr)ptr2);
			}
			num14 += num11;
			_gl.EnableVertexAttribArray((uint)vertTexCoordsAttrib);
			_gl.VertexAttribPointer((uint)vertTexCoordsAttrib, 2, GL.FLOAT, normalized: false, 0, (IntPtr)num10);
		}
		if (num12 != 0)
		{
			fixed (Vector3* ptr3 = array3)
			{
				_gl.BufferSubData(GL.ARRAY_BUFFER, (IntPtr)num14, (IntPtr)num12, (IntPtr)ptr3);
			}
			_gl.EnableVertexAttribArray((uint)vertNormalAttrib);
			_gl.VertexAttribPointer((uint)vertNormalAttrib, 3, GL.FLOAT, normalized: false, 0, (IntPtr)(num10 + num11));
		}
		if (result.IndicesBuffer == GLBuffer.None)
		{
			result.IndicesBuffer = _gl.GenBuffer();
		}
		_gl.BindBuffer(result.VertexArray, GL.ELEMENT_ARRAY_BUFFER, result.IndicesBuffer);
		fixed (ushort* ptr4 = array4)
		{
			_gl.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(array4.Length * 2), (IntPtr)ptr4, GL.STATIC_DRAW);
		}
	}

	public unsafe static void CreateFrustum(ref Mesh result, ref BoundingFrustum frustum)
	{
		CreateSimpleBox(ref result, 2f);
		_gl.BindBuffer(GL.ARRAY_BUFFER, result.VerticesBuffer);
		Vector3[] corners = frustum.GetCorners();
		fixed (Vector3* ptr = corners)
		{
			_gl.BufferData(GL.ARRAY_BUFFER, (IntPtr)(corners.Length * 3 * 4), (IntPtr)ptr, GL.STATIC_DRAW);
		}
	}
}
