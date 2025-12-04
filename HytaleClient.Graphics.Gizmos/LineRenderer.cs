using System;
using HytaleClient.Core;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Gizmos;

internal class LineRenderer : Disposable
{
	private readonly GraphicsDevice _graphics;

	private readonly BasicProgram _program;

	private GLVertexArray _vertexArray;

	private GLBuffer _verticesBuffer;

	private GLBuffer _indicesBuffer;

	private int _indiceCount = 0;

	public LineRenderer(GraphicsDevice graphics, BasicProgram program)
	{
		_graphics = graphics;
		_program = program;
		GLFunctions gL = graphics.GL;
		_vertexArray = gL.GenVertexArray();
		gL.BindVertexArray(_vertexArray);
		_verticesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		_indicesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		gL.EnableVertexAttribArray(program.AttribPosition.Index);
		gL.VertexAttribPointer(program.AttribPosition.Index, 3, GL.FLOAT, normalized: false, 32, IntPtr.Zero);
		gL.EnableVertexAttribArray(program.AttribTexCoords.Index);
		gL.VertexAttribPointer(program.AttribTexCoords.Index, 2, GL.FLOAT, normalized: false, 32, (IntPtr)12);
	}

	public unsafe void UpdateLineData(Vector3[] linePoints)
	{
		GLFunctions gL = _graphics.GL;
		_indiceCount = (linePoints.Length - 1) * 2;
		float[] array = new float[linePoints.Length * 8];
		ushort[] array2 = new ushort[_indiceCount];
		for (int i = 0; i < linePoints.Length; i++)
		{
			array[i * 8] = linePoints[i].X;
			array[i * 8 + 1] = linePoints[i].Y;
			array[i * 8 + 2] = linePoints[i].Z;
			array[i * 8 + 3] = 0f;
			array[i * 8 + 4] = 0f;
			array[i * 8 + 5] = 0f;
			array[i * 8 + 6] = 0f;
			array[i * 8 + 7] = 0f;
		}
		for (int j = 0; j < _indiceCount / 2; j++)
		{
			array2[j * 2] = (ushort)j;
			array2[j * 2 + 1] = (ushort)(j + 1);
		}
		gL.BindVertexArray(_vertexArray);
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		fixed (float* ptr = array)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(array.Length * 4), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		fixed (ushort* ptr2 = array2)
		{
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(array2.Length * 2), (IntPtr)ptr2, GL.STATIC_DRAW);
		}
	}

	protected override void DoDispose()
	{
		_graphics.GL.DeleteBuffer(_verticesBuffer);
		_graphics.GL.DeleteBuffer(_indicesBuffer);
		_graphics.GL.DeleteVertexArray(_vertexArray);
	}

	public void Draw(ref Matrix MVPMatrix, Vector3 color, float opacity)
	{
		_program.AssertInUse();
		_graphics.GL.AssertTextureBound(GL.TEXTURE0, _graphics.WhitePixelTexture.GLTexture);
		_program.MVPMatrix.SetValue(ref MVPMatrix);
		_program.Color.SetValue(color);
		_program.Opacity.SetValue(opacity);
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		gL.DrawElements(GL.ONE, _indiceCount, GL.UNSIGNED_SHORT, (IntPtr)0);
	}
}
