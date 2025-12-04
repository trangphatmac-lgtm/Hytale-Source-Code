using System;
using HytaleClient.Core;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Gizmos;

internal class BlockOutlineRenderer : Disposable
{
	private readonly GraphicsDevice _graphics;

	private GLVertexArray _vertexArray;

	private GLBuffer _verticesBuffer;

	private GLBuffer _indicesBuffer;

	private Matrix _matrix;

	private Matrix _tempMatrix;

	private const float LineWidth = 0.01f;

	private static readonly float[] Vertices = new float[256]
	{
		0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 1f,
		1f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 0f,
		0f, 0f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0f,
		0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
		0f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f,
		0f, 0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f,
		0f, 0f, 0f, 0f, 0.01f, 0f, 1f, 0f, 0f, 0f,
		0f, 0f, 0.01f, 1f, 1f, 0f, 0f, 0f, 0f, 0f,
		0f, 0.99f, 1f, 0f, 0f, 0f, 0f, 0f, 1f, 0.99f,
		1f, 0f, 0f, 0f, 0f, 0f, 0.99f, 1f, 1f, 0f,
		0f, 0f, 0f, 0f, 0.99f, 0f, 1f, 0f, 0f, 0f,
		0f, 0f, 1f, 0.01f, 1f, 0f, 0f, 0f, 0f, 0f,
		0f, 0.01f, 1f, 0f, 0f, 0f, 0f, 0f, 0.01f, 0f,
		0f, 0f, 0f, 0f, 0f, 0f, 0.01f, 1f, 0f, 0f,
		0f, 0f, 0f, 0f, 0f, 0.99f, 0f, 0f, 0f, 0f,
		0f, 0f, 1f, 0.99f, 0f, 0f, 0f, 0f, 0f, 0f,
		0.99f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0.99f, 0f,
		0f, 0f, 0f, 0f, 0f, 0f, 1f, 0.01f, 0f, 0f,
		0f, 0f, 0f, 0f, 0f, 0.01f, 0f, 0f, 0f, 0f,
		0f, 0f, 0f, 0f, 0.01f, 0f, 0f, 0f, 0f, 0f,
		0f, 1f, 0.01f, 0f, 0f, 0f, 0f, 0f, 0f, 1f,
		0.99f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.99f, 0f,
		0f, 0f, 0f, 0f, 1f, 0f, 0.01f, 0f, 0f, 0f,
		0f, 0f, 1f, 1f, 0.01f, 0f, 0f, 0f, 0f, 0f,
		1f, 1f, 0.99f, 0f, 0f, 0f, 0f, 0f, 1f, 0f,
		0.99f, 0f, 0f, 0f, 0f, 0f
	};

	private static readonly ushort[] Indices = new ushort[144]
	{
		0, 1, 8, 8, 1, 9, 1, 2, 10, 10,
		2, 11, 2, 3, 12, 12, 3, 13, 3, 0,
		14, 14, 0, 15, 4, 5, 16, 16, 5, 17,
		5, 6, 18, 18, 6, 19, 6, 7, 20, 20,
		7, 21, 7, 4, 22, 22, 4, 23, 5, 4,
		24, 24, 5, 25, 1, 5, 18, 18, 1, 10,
		0, 1, 26, 26, 0, 27, 4, 0, 15, 15,
		4, 23, 6, 7, 28, 28, 6, 29, 2, 6,
		19, 19, 2, 11, 3, 2, 30, 30, 3, 31,
		7, 3, 14, 14, 7, 22, 1, 5, 9, 9,
		5, 17, 2, 1, 30, 30, 1, 26, 6, 2,
		20, 20, 2, 12, 6, 5, 29, 29, 5, 25,
		0, 4, 8, 8, 4, 16, 3, 0, 31, 31,
		0, 27, 7, 3, 21, 21, 3, 13, 7, 4,
		28, 28, 4, 24
	};

	public unsafe BlockOutlineRenderer(GraphicsDevice graphics)
	{
		_graphics = graphics;
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
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		gL.EnableVertexAttribArray(basicProgram.AttribPosition.Index);
		gL.VertexAttribPointer(basicProgram.AttribPosition.Index, 3, GL.FLOAT, normalized: false, 32, IntPtr.Zero);
		gL.EnableVertexAttribArray(basicProgram.AttribTexCoords.Index);
		gL.VertexAttribPointer(basicProgram.AttribTexCoords.Index, 2, GL.FLOAT, normalized: false, 32, (IntPtr)12);
	}

	protected override void DoDispose()
	{
		GLFunctions gL = _graphics.GL;
		gL.DeleteBuffer(_verticesBuffer);
		gL.DeleteBuffer(_indicesBuffer);
		gL.DeleteVertexArray(_vertexArray);
	}

	public void Draw(Vector3 position, BlockHitbox hitbox, Matrix viewProjectionMatrix, bool debugOutline)
	{
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		basicProgram.AssertInUse();
		basicProgram.Opacity.AssertValue(0.12f);
		_graphics.GL.AssertTextureBound(GL.TEXTURE0, _graphics.WhitePixelTexture.GLTexture);
		basicProgram.Color.SetValue(debugOutline ? _graphics.BlueColor : _graphics.BlackColor);
		Vector3 scales = hitbox.BoundingBox.Max - hitbox.BoundingBox.Min;
		position += hitbox.BoundingBox.Min;
		Matrix.CreateScale(ref scales, out _matrix);
		Matrix.CreateTranslation(ref position, out _tempMatrix);
		Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
		Matrix.Multiply(ref _matrix, ref viewProjectionMatrix, out _matrix);
		basicProgram.MVPMatrix.SetValue(ref _matrix);
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		gL.DrawElements(GL.TRIANGLES, Indices.Length, GL.UNSIGNED_SHORT, (IntPtr)0);
	}
}
