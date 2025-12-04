using System;
using HytaleClient.Core;
using HytaleClient.Graphics.Gizmos.Models;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Gizmos;

internal class PrimitiveModelRenderer : Disposable
{
	private readonly GraphicsDevice _graphics;

	private readonly BasicProgram _program;

	private GLVertexArray _vertexArray;

	private GLBuffer _verticesBuffer;

	private GLBuffer _indicesBuffer;

	private Matrix _matrix;

	private int _indiceCount = 0;

	public PrimitiveModelRenderer(GraphicsDevice graphics, BasicProgram program)
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

	public unsafe void UpdateModelData(PrimitiveModelData modelData)
	{
		GLFunctions gL = _graphics.GL;
		_indiceCount = modelData.Indices.Length;
		gL.BindVertexArray(_vertexArray);
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		fixed (float* ptr = modelData.Vertices)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(modelData.Vertices.Length * 4), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		fixed (ushort* ptr2 = modelData.Indices)
		{
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(modelData.Indices.Length * 2), (IntPtr)ptr2, GL.STATIC_DRAW);
		}
	}

	protected override void DoDispose()
	{
		GLFunctions gL = _graphics.GL;
		gL.DeleteBuffer(_verticesBuffer);
		gL.DeleteBuffer(_indicesBuffer);
		gL.DeleteVertexArray(_vertexArray);
	}

	public void Draw(Matrix viewProjectionMatrix, Matrix transformMatrix, Vector3 color, float opacity, GL drawMode = GL.ONE)
	{
		GLFunctions gL = _graphics.GL;
		_program.AssertInUse();
		gL.AssertTextureBound(GL.TEXTURE0, _graphics.WhitePixelTexture.GLTexture);
		_matrix = Matrix.Identity;
		Matrix.Multiply(ref _matrix, ref transformMatrix, out _matrix);
		Matrix.Multiply(ref _matrix, ref viewProjectionMatrix, out _matrix);
		_program.MVPMatrix.SetValue(ref _matrix);
		_program.Color.SetValue(color);
		_program.Opacity.SetValue(opacity);
		gL.BindVertexArray(_vertexArray);
		gL.DrawElements(drawMode, _indiceCount, GL.UNSIGNED_SHORT, (IntPtr)0);
	}
}
