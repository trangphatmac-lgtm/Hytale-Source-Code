using System.Runtime.CompilerServices;
using HytaleClient.Core;

namespace HytaleClient.Graphics;

public class FullscreenTriangleRenderer : Disposable
{
	private readonly GraphicsDevice _graphics;

	private readonly GLVertexArray _vertexArray;

	public FullscreenTriangleRenderer(GraphicsDevice graphics)
	{
		_graphics = graphics;
		GLFunctions gL = _graphics.GL;
		_vertexArray = gL.GenVertexArray();
	}

	protected override void DoDispose()
	{
		_graphics.GL.DeleteVertexArray(_vertexArray);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Draw()
	{
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		gL.DrawArrays(GL.TRIANGLES, 0, 3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BindVertexArray()
	{
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DrawRaw()
	{
		GLFunctions gL = _graphics.GL;
		gL.DrawArrays(GL.TRIANGLES, 0, 3);
	}
}
