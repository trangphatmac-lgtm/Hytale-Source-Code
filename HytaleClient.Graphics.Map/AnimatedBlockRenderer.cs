using System;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Map;

internal class AnimatedBlockRenderer : AnimatedRenderer
{
	private ChunkGeometryData _vertexData;

	public readonly int IndicesCount;

	private GLBuffer _verticesBuffer;

	private GLBuffer _indicesBuffer;

	public GLVertexArray VertexArray { get; private set; }

	public AnimatedBlockRenderer(BlockyModel model, Point[] atlasSizes, ChunkGeometryData vertexData, GraphicsDevice graphics = null, bool selfManageNodeBuffer = false)
		: base(model, atlasSizes, selfManageNodeBuffer)
	{
		_vertexData = vertexData;
		IndicesCount = _vertexData.IndicesCount;
		if (graphics != null)
		{
			CreateGPUData(graphics);
		}
	}

	public unsafe override void CreateGPUData(GraphicsDevice graphics)
	{
		base.CreateGPUData(graphics);
		GLFunctions gL = graphics.GL;
		VertexArray = gL.GenVertexArray();
		gL.BindVertexArray(VertexArray);
		_verticesBuffer = gL.GenBuffer();
		gL.BindBuffer(VertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		fixed (ChunkVertex* ptr = _vertexData.Vertices)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(_vertexData.VerticesCount * ChunkVertex.Size), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		_indicesBuffer = gL.GenBuffer();
		gL.BindBuffer(VertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		fixed (uint* ptr2 = _vertexData.Indices)
		{
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(_vertexData.IndicesCount * 4), (IntPtr)ptr2, GL.STATIC_DRAW);
		}
		_vertexData = null;
		MapBlockAnimatedProgram mapBlockAnimatedProgram = graphics.GPUProgramStore.MapBlockAnimatedProgram;
		IntPtr zero = IntPtr.Zero;
		gL.EnableVertexAttribArray(mapBlockAnimatedProgram.AttribPositionAndDoubleSidedAndBlockId.Index);
		gL.VertexAttribIPointer(mapBlockAnimatedProgram.AttribPositionAndDoubleSidedAndBlockId.Index, 4, GL.SHORT, ChunkVertex.Size, zero);
		zero += 8;
		gL.EnableVertexAttribArray(mapBlockAnimatedProgram.AttribTexCoords.Index);
		gL.VertexAttribPointer(mapBlockAnimatedProgram.AttribTexCoords.Index, 4, GL.UNSIGNED_SHORT, normalized: true, ChunkVertex.Size, zero);
		zero += 8;
		gL.EnableVertexAttribArray(mapBlockAnimatedProgram.AttribDataPacked.Index);
		gL.VertexAttribIPointer(mapBlockAnimatedProgram.AttribDataPacked.Index, 4, GL.UNSIGNED_INT, ChunkVertex.Size, zero);
		zero += 16;
	}

	protected override void DoDispose()
	{
		base.DoDispose();
		if (_graphics != null)
		{
			GLFunctions gL = _graphics.GL;
			gL.DeleteBuffer(_verticesBuffer);
			gL.DeleteBuffer(_indicesBuffer);
			gL.DeleteVertexArray(VertexArray);
		}
	}
}
