using System;
using HytaleClient.Core;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Gizmos;

internal class BoxRenderer : Disposable
{
	private const int QuadIndiceCount = 36;

	private const int OutlineIndiceCount = 24;

	private readonly GraphicsDevice _graphics;

	private readonly BasicProgram _program;

	private GLVertexArray _vertexArray;

	private GLBuffer _verticesBuffer;

	private GLBuffer _indicesBuffer;

	private Matrix _matrix;

	private Matrix _tempMatrix;

	private static readonly float[] Vertices = new float[64]
	{
		0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 1f, 0f,
		1f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 0f,
		0f, 0f, 0f, 0f, 0f, 1f, 1f, 0f, 0f, 0f,
		0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
		0f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f,
		0f, 0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f,
		0f, 0f, 0f, 0f
	};

	private static readonly ushort[] Indices = new ushort[60]
	{
		0, 1, 2, 0, 2, 3, 4, 5, 6, 4,
		6, 7, 5, 3, 2, 5, 2, 6, 4, 7,
		1, 4, 1, 0, 7, 6, 2, 7, 2, 1,
		4, 0, 3, 4, 3, 5, 0, 1, 1, 2,
		2, 3, 3, 0, 4, 5, 5, 6, 6, 7,
		7, 4, 0, 4, 1, 7, 2, 6, 3, 5
	};

	public unsafe BoxRenderer(GraphicsDevice graphics, BasicProgram program)
	{
		_graphics = graphics;
		_program = program;
		GLFunctions gL = graphics.GL;
		_vertexArray = gL.GenVertexArray();
		gL.BindVertexArray(_vertexArray);
		_verticesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		fixed (float* ptr = Vertices)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(Vertices.Length * 4), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		_indicesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		fixed (ushort* ptr2 = Indices)
		{
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(Indices.Length * 2), (IntPtr)ptr2, GL.STATIC_DRAW);
		}
		gL.EnableVertexAttribArray(program.AttribPosition.Index);
		gL.VertexAttribPointer(program.AttribPosition.Index, 3, GL.FLOAT, normalized: false, 32, IntPtr.Zero);
		gL.EnableVertexAttribArray(program.AttribTexCoords.Index);
		gL.VertexAttribPointer(program.AttribTexCoords.Index, 2, GL.FLOAT, normalized: false, 32, (IntPtr)12);
	}

	protected override void DoDispose()
	{
		_graphics.GL.DeleteBuffer(_verticesBuffer);
		_graphics.GL.DeleteBuffer(_indicesBuffer);
		_graphics.GL.DeleteVertexArray(_vertexArray);
	}

	public void Draw(ref Matrix MVPMatrix, Vector3 outlineColor, float outlineOpacity, Vector3 quadColor, float quadOpacity)
	{
		_program.AssertInUse();
		_graphics.GL.AssertTextureBound(GL.TEXTURE0, _graphics.WhitePixelTexture.GLTexture);
		_program.MVPMatrix.SetValue(ref MVPMatrix);
		_program.Color.SetValue(quadColor);
		_program.Opacity.SetValue(quadOpacity);
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		gL.DrawElements(GL.TRIANGLES, 36, GL.UNSIGNED_SHORT, (IntPtr)0);
		if (quadOpacity != outlineOpacity)
		{
			_program.Opacity.SetValue(outlineOpacity);
		}
		if (quadColor != outlineColor)
		{
			_program.Color.SetValue(outlineColor);
		}
		gL.DrawElements(GL.ONE, 24, GL.UNSIGNED_SHORT, (IntPtr)72);
	}

	public void Draw(Vector3 position, BoundingBox box, Matrix viewProjectionMatrix, Vector3 outlineColor, float outlineOpacity, Vector3 quadColor, float quadOpacity)
	{
		_program.AssertInUse();
		_graphics.GL.AssertTextureBound(GL.TEXTURE0, _graphics.WhitePixelTexture.GLTexture);
		Vector3 position2 = box.Min + position;
		Vector3 scales = box.GetSize() / Vector3.One;
		Matrix.CreateScale(ref scales, out _matrix);
		Matrix.CreateTranslation(ref position2, out _tempMatrix);
		Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
		Matrix.Multiply(ref _matrix, ref viewProjectionMatrix, out _matrix);
		_program.MVPMatrix.SetValue(ref _matrix);
		_program.Color.SetValue(quadColor);
		_program.Opacity.SetValue(quadOpacity);
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		gL.DrawElements(GL.TRIANGLES, 36, GL.UNSIGNED_SHORT, (IntPtr)0);
		if (quadOpacity != outlineOpacity)
		{
			_program.Opacity.SetValue(outlineOpacity);
		}
		if (quadColor != outlineColor)
		{
			_program.Color.SetValue(outlineColor);
		}
		gL.DrawElements(GL.ONE, 24, GL.UNSIGNED_SHORT, (IntPtr)72);
	}
}
