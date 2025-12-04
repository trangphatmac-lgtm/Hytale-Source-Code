using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HytaleClient.Core;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics;

internal class FXRenderer : Disposable
{
	public enum DrawType
	{
		Particle,
		Trail
	}

	public struct FXGPUData
	{
		public GLVertexArray VertexArray;

		public GLBuffer VerticesBuffer;

		public GLBuffer IndicesBuffer;

		public unsafe void CreateGPUData(GraphicsDevice graphics, int maxParticles)
		{
			if (VertexArray != GLVertexArray.None)
			{
				throw new Exception("Error : GPUData.CreateGPUData() must be called once only.");
			}
			GLFunctions gL = graphics.GL;
			int num = 6 * maxParticles;
			uint[] array = new uint[num];
			for (int i = 0; i < maxParticles; i++)
			{
				array[i * 6] = (uint)(i * 4);
				array[i * 6 + 1] = (uint)(i * 4 + 1);
				array[i * 6 + 2] = (uint)(i * 4 + 2);
				array[i * 6 + 3] = (uint)(i * 4);
				array[i * 6 + 4] = (uint)(i * 4 + 2);
				array[i * 6 + 5] = (uint)(i * 4 + 3);
			}
			VertexArray = gL.GenVertexArray();
			gL.BindVertexArray(VertexArray);
			VerticesBuffer = gL.GenBuffer();
			gL.BindBuffer(VertexArray, GL.ARRAY_BUFFER, VerticesBuffer);
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(4 * maxParticles * FXVertex.Size), IntPtr.Zero, GL.DYNAMIC_DRAW);
			IndicesBuffer = gL.GenBuffer();
			gL.BindBuffer(VertexArray, GL.ELEMENT_ARRAY_BUFFER, IndicesBuffer);
			fixed (uint* ptr = array)
			{
				gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(num * 4), (IntPtr)ptr, GL.STATIC_DRAW);
			}
			SetupVertexAttributes(graphics);
		}

		public void Dispose(GLFunctions gl)
		{
			if (VertexArray == GLVertexArray.None)
			{
				throw new Exception("Error : GPUData.Dispose() was already called (or GPUData was never created ?).");
			}
			gl.DeleteVertexArray(VertexArray);
			gl.DeleteBuffer(VerticesBuffer);
			gl.DeleteBuffer(IndicesBuffer);
		}

		private void SetupVertexAttributes(GraphicsDevice graphics)
		{
			GLFunctions gL = graphics.GL;
			ParticleProgram particleProgram = graphics.GPUProgramStore.ParticleProgram;
			IntPtr zero = IntPtr.Zero;
			gL.EnableVertexAttribArray(particleProgram.AttribData1.Index);
			gL.VertexAttribPointer(particleProgram.AttribData1.Index, 4, GL.FLOAT, normalized: false, FXVertex.Size, zero);
			zero += 16;
			gL.EnableVertexAttribArray(particleProgram.AttribData2.Index);
			gL.VertexAttribPointer(particleProgram.AttribData2.Index, 4, GL.FLOAT, normalized: false, FXVertex.Size, zero);
			zero += 16;
			gL.EnableVertexAttribArray(particleProgram.AttribData3.Index);
			gL.VertexAttribPointer(particleProgram.AttribData3.Index, 4, GL.FLOAT, normalized: false, FXVertex.Size, zero);
			zero += 16;
			gL.EnableVertexAttribArray(particleProgram.AttribData4.Index);
			gL.VertexAttribPointer(particleProgram.AttribData4.Index, 4, GL.FLOAT, normalized: false, FXVertex.Size, zero);
			zero += 16;
		}
	}

	public struct DrawParams
	{
		public bool IsStartOffsetSet;

		public uint StartOffset;

		public int Count;
	}

	public DrawParams ErosionDrawParams;

	public DrawParams BlendLowResDrawParams;

	public DrawParams BlendDrawParams;

	public DrawParams BlendFPVDrawParams;

	public DrawParams DistortionDrawParams;

	public FXVertexBuffer FXVertexBuffer;

	public static readonly int DrawDataSize = Marshal.SizeOf(typeof(Matrix)) + Marshal.SizeOf(typeof(Vector4)) * 8;

	private readonly GraphicsDevice _graphics;

	private FXGPUData[] _gpuData;

	private GPUBufferTexture _drawDataBufferTexture;

	private const uint BufferGrowth = 1024u;

	private uint _bufferSize = (uint)(DrawDataSize * 2048);

	private int _currentGPUDataId;

	private ushort _drawTaskCount;

	private int _maxFXDrawCount;

	public FXRenderer(GraphicsDevice graphics)
	{
		_graphics = graphics;
	}

	public void Initialize(int maxParticleCount, int particleMaxDrawCount)
	{
		_maxFXDrawCount = particleMaxDrawCount;
		InitMemory(maxParticleCount);
		CreateGPUData(particleMaxDrawCount);
	}

	protected override void DoDispose()
	{
		DestroyGPUData();
	}

	private void InitMemory(int itemMaxCount)
	{
		FXVertexBuffer = default(FXVertexBuffer);
		FXVertexBuffer.Initialize(itemMaxCount);
	}

	private void CreateGPUData(int particleMaxCount)
	{
		InitFXGPUData(particleMaxCount);
		InitDrawDataBufferTexture();
	}

	private void DestroyGPUData()
	{
		DisposeDrawDataBufferTexture();
		DisposeFXGPUData();
	}

	private void InitFXGPUData(int maxParticleDrawCount)
	{
		_gpuData = new FXGPUData[2];
		_gpuData[0].CreateGPUData(_graphics, maxParticleDrawCount);
		_gpuData[1].CreateGPUData(_graphics, maxParticleDrawCount);
	}

	private void DisposeFXGPUData()
	{
		_gpuData[0].Dispose(_graphics.GL);
		_gpuData[1].Dispose(_graphics.GL);
	}

	private void InitDrawDataBufferTexture()
	{
		_drawDataBufferTexture.CreateStorage(GL.RGBA32F, GL.STREAM_DRAW, useDoubleBuffering: true, _bufferSize, 1024u, GPUBuffer.GrowthPolicy.GrowthAutoNoLimit);
	}

	public bool TryBeginDrawDataTransfer(out IntPtr dataPtr)
	{
		dataPtr = IntPtr.Zero;
		uint num = (uint)(_drawTaskCount * DrawDataSize);
		if (num == 0)
		{
			return false;
		}
		_drawDataBufferTexture.GrowStorageIfNecessary(num);
		dataPtr = _drawDataBufferTexture.BeginTransfer(num);
		return true;
	}

	public void EndDrawDataTransfer()
	{
		_drawDataBufferTexture.EndTransfer();
	}

	private void DisposeDrawDataBufferTexture()
	{
		_drawDataBufferTexture.DestroyStorage();
	}

	public void SetupDrawDataTexture(uint unitId)
	{
		GLFunctions gL = _graphics.GL;
		gL.ActiveTexture((GL)(33984 + unitId));
		gL.BindTexture(GL.TEXTURE_BUFFER, _drawDataBufferTexture.CurrentTexture);
	}

	public void BeginFrame()
	{
		ResetDrawCounters();
		PingPongBuffers();
	}

	private void ResetDrawCounters()
	{
		_drawTaskCount = 0;
		ErosionDrawParams.Count = 0;
		BlendLowResDrawParams.Count = 0;
		BlendDrawParams.Count = 0;
		BlendDrawParams.IsStartOffsetSet = false;
		BlendFPVDrawParams.Count = 0;
		BlendFPVDrawParams.IsStartOffsetSet = false;
		DistortionDrawParams.Count = 0;
		DistortionDrawParams.IsStartOffsetSet = false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ushort ReserveDrawTask()
	{
		ushort drawTaskCount = _drawTaskCount;
		_drawTaskCount++;
		return drawTaskCount;
	}

	public void ClearVertexData()
	{
		FXVertexBuffer.ClearVertexDataStorage();
	}

	public unsafe void SendVertexDataToGPU()
	{
		GLFunctions gL = _graphics.GL;
		int num = System.Math.Min(_maxFXDrawCount, FXVertexBuffer.GetVertexDataStored());
		if (num > 0)
		{
			_currentGPUDataId++;
			_currentGPUDataId %= 2;
			gL.BindBuffer(GL.ARRAY_BUFFER, _gpuData[_currentGPUDataId].VerticesBuffer);
			fixed (FXVertex* ptr = FXVertexBuffer.ParticleVertices)
			{
				gL.BufferSubData(GL.ARRAY_BUFFER, IntPtr.Zero, (IntPtr)(num * 4 * FXVertex.Size), (IntPtr)ptr);
			}
		}
	}

	public void DrawTransparency()
	{
		if (BlendDrawParams.Count > 0 || BlendFPVDrawParams.Count > 0)
		{
			GLFunctions gL = _graphics.GL;
			gL.BindVertexArray(_gpuData[_currentGPUDataId].VertexArray);
			if (BlendDrawParams.Count > 0)
			{
				gL.DrawElements(GL.TRIANGLES, BlendDrawParams.Count * 6, GL.UNSIGNED_INT, (IntPtr)(BlendDrawParams.StartOffset * 6 * 4));
			}
			if (BlendFPVDrawParams.Count > 0)
			{
				gL.DrawElements(GL.TRIANGLES, BlendFPVDrawParams.Count * 6, GL.UNSIGNED_INT, (IntPtr)(BlendFPVDrawParams.StartOffset * 6 * 4));
			}
		}
	}

	public void DrawTransparencyLowRes()
	{
		if (BlendLowResDrawParams.Count > 0)
		{
			GLFunctions gL = _graphics.GL;
			gL.BindVertexArray(_gpuData[_currentGPUDataId].VertexArray);
			gL.DrawElements(GL.TRIANGLES, BlendLowResDrawParams.Count * 6, GL.UNSIGNED_INT, (IntPtr)(BlendLowResDrawParams.StartOffset * 6 * 4));
		}
	}

	public void DrawDistortion()
	{
		if (DistortionDrawParams.Count > 0)
		{
			GLFunctions gL = _graphics.GL;
			gL.BindVertexArray(_gpuData[_currentGPUDataId].VertexArray);
			gL.DrawElements(GL.TRIANGLES, DistortionDrawParams.Count * 6, GL.UNSIGNED_INT, (IntPtr)(DistortionDrawParams.StartOffset * 6 * 4));
		}
	}

	public void DrawErosion()
	{
		if (ErosionDrawParams.Count > 0)
		{
			GLFunctions gL = _graphics.GL;
			gL.BindVertexArray(_gpuData[_currentGPUDataId].VertexArray);
			gL.DrawElements(GL.TRIANGLES, ErosionDrawParams.Count * 6, GL.UNSIGNED_INT, (IntPtr)(ErosionDrawParams.StartOffset * 6 * 4));
		}
	}

	private void PingPongBuffers()
	{
		_drawDataBufferTexture.Swap();
	}
}
