using System;
using HytaleClient.Core;
using HytaleClient.Graphics.Programs;

namespace HytaleClient.Graphics;

public class QuadRenderer : Disposable
{
	private readonly GraphicsDevice _graphics;

	private readonly GLVertexArray _vertexArray;

	private readonly GLBuffer _vertexBuffer;

	private static readonly float[] Vertices = new float[48]
	{
		1f, 1f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 1f,
		0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
		1f, 0f, 0f, 0f, 1f, 1f, 0f, 1f, 0f, 0f,
		0f, 0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f,
		1f, 0f, 0f, 1f, 1f, 0f, 0f, 0f
	};

	public QuadRenderer(GraphicsDevice graphics, Attrib attribPosition, Attrib attribTexCoords)
	{
		_graphics = graphics;
		GLFunctions gL = _graphics.GL;
		_vertexArray = gL.GenVertexArray();
		gL.BindVertexArray(_vertexArray);
		_vertexBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _vertexBuffer);
		gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(Vertices.Length * 4), IntPtr.Zero, GL.DYNAMIC_DRAW);
		gL.EnableVertexAttribArray(attribPosition.Index);
		gL.VertexAttribPointer(attribPosition.Index, 3, GL.FLOAT, normalized: false, 32, IntPtr.Zero);
		gL.EnableVertexAttribArray(attribTexCoords.Index);
		gL.VertexAttribPointer(attribTexCoords.Index, 2, GL.FLOAT, normalized: false, 32, (IntPtr)12);
		UpdateUVs(1f, 1f);
	}

	protected override void DoDispose()
	{
		_graphics.GL.DeleteBuffer(_vertexBuffer);
		_graphics.GL.DeleteVertexArray(_vertexArray);
	}

	public unsafe void UpdateUVs(float right, float bottom, float left = 0f, float top = 0f)
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(QuadRenderer).FullName);
		}
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _vertexBuffer);
		Vertices[3] = (Vertices[27] = (Vertices[43] = right));
		Vertices[20] = (Vertices[36] = (Vertices[44] = bottom));
		Vertices[11] = (Vertices[19] = (Vertices[35] = left));
		Vertices[4] = (Vertices[12] = (Vertices[28] = top));
		fixed (float* ptr = Vertices)
		{
			gL.BufferSubData(GL.ARRAY_BUFFER, IntPtr.Zero, (IntPtr)(Vertices.Length * 4), (IntPtr)ptr);
		}
	}

	public void Draw()
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(QuadRenderer).FullName);
		}
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		gL.DrawArrays(GL.TRIANGLES, 0, 6);
	}

	public void BindVertexArray()
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(QuadRenderer).FullName);
		}
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
	}

	public void DrawRaw()
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(QuadRenderer).FullName);
		}
		GLFunctions gL = _graphics.GL;
		gL.DrawArrays(GL.TRIANGLES, 0, 6);
	}
}
