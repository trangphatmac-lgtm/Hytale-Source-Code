using System;
using HytaleClient.Core;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Brush;

public class BrushAxisLockPlaneRenderer : Disposable
{
	private const int GridWidth = 75;

	private const int GridHeight = 75;

	private const int GridSquareSize = 5;

	private const int TotalGridWidth = 375;

	private const int TotalGridHeight = 375;

	private readonly GraphicsDevice _graphics;

	private float[] _vertices;

	private ushort[] _indices;

	private readonly GLVertexArray _vertexArray;

	private readonly GLBuffer _verticesBuffer;

	private readonly GLBuffer _indicesBuffer;

	private Vector3 _centerPos;

	private Matrix _matrix = Matrix.Identity;

	private Matrix _rotationMatrix = Matrix.Identity;

	public BrushAxisLockPlaneRenderer(GraphicsDevice graphics)
	{
		_graphics = graphics;
		GLFunctions gL = _graphics.GL;
		_vertexArray = gL.GenVertexArray();
		gL.BindVertexArray(_vertexArray);
		_verticesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		_indicesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		gL.EnableVertexAttribArray(basicProgram.AttribPosition.Index);
		gL.VertexAttribPointer(basicProgram.AttribPosition.Index, 3, GL.FLOAT, normalized: false, 32, IntPtr.Zero);
		gL.EnableVertexAttribArray(basicProgram.AttribTexCoords.Index);
		gL.VertexAttribPointer(basicProgram.AttribTexCoords.Index, 2, GL.FLOAT, normalized: false, 32, (IntPtr)12);
	}

	protected override void DoDispose()
	{
		GLFunctions gL = _graphics.GL;
		gL.DeleteVertexArray(_vertexArray);
		gL.DeleteBuffer(_verticesBuffer);
		gL.DeleteBuffer(_indicesBuffer);
	}

	public unsafe void UpdatePlane(Vector3 pos, Matrix rotationMatrix)
	{
		_centerPos = pos;
		_rotationMatrix = rotationMatrix;
		int num = 752;
		int num2 = 752;
		int num3 = num + num2;
		_vertices = new float[num3 * 8];
		_indices = new ushort[num3 * 2];
		int vertexInc = 0;
		ushort indexInc = 0;
		for (int i = 0; i <= 375; i += 5)
		{
			BuildLine(ref vertexInc, ref indexInc, new Vector3((float)i - 187.5f, -187.5f, 0f), new Vector3((float)i - 187.5f, 187.5f, 0f));
		}
		for (int j = 0; j <= 375; j += 5)
		{
			BuildLine(ref vertexInc, ref indexInc, new Vector3(-187.5f, (float)j - 187.5f, 0f), new Vector3(187.5f, (float)j - 187.5f, 0f));
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

	private void BuildLine(ref int vertexInc, ref ushort indexInc, Vector3 a, Vector3 b)
	{
		int num = vertexInc / 8;
		AddLineVertex(ref vertexInc, a);
		AddLineVertex(ref vertexInc, b);
		_indices[indexInc++] = (ushort)num;
		_indices[indexInc++] = (ushort)(num + 1);
	}

	private void AddLineVertex(ref int vertexInc, Vector3 pos)
	{
		_vertices[vertexInc++] = pos.X;
		_vertices[vertexInc++] = pos.Y;
		_vertices[vertexInc++] = pos.Z;
		_vertices[vertexInc++] = 0f;
		_vertices[vertexInc++] = 0f;
		_vertices[vertexInc++] = 0f;
		_vertices[vertexInc++] = 0f;
		_vertices[vertexInc++] = 0f;
	}

	public void Draw(ref Matrix viewProjectionMatrix, Vector3 positionOffset, Vector3 color, float opacity)
	{
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		basicProgram.AssertInUse();
		_graphics.GL.AssertTextureBound(GL.TEXTURE0, _graphics.WhitePixelTexture.GLTexture);
		Vector3 position = _centerPos + positionOffset;
		Matrix.CreateTranslation(ref position, out var result);
		Matrix.Multiply(ref _rotationMatrix, ref result, out _matrix);
		Matrix.Multiply(ref _matrix, ref viewProjectionMatrix, out _matrix);
		basicProgram.MVPMatrix.SetValue(ref _matrix);
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		basicProgram.Color.SetValue(color);
		basicProgram.Opacity.SetValue(opacity);
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
		gL.DrawElements(GL.ONE, _indices.Length, GL.UNSIGNED_SHORT, (IntPtr)0);
	}
}
