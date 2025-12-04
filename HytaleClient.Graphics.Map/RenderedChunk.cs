#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HytaleClient.Audio;
using HytaleClient.Core;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Graphics.Particles;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Graphics.Map;

internal class RenderedChunk : Disposable
{
	public enum ChunkRebuildState
	{
		Waiting,
		ReadyForRebuild,
		Rebuilding,
		UpdateReady
	}

	public class ChunkUpdateTask
	{
		public ChunkGeometryData OpaqueData;

		public ChunkGeometryData AlphaBlendedData;

		public ChunkGeometryData AlphaTestedData;

		public int AlphaTestedAnimatedLowLODIndicesCount;

		public int AlphaTestedLowLODIndicesCount;

		public int AlphaTestedHighLODIndicesCount;

		public int AlphaTestedAnimatedHighLODIndicesCount;

		public AnimatedBlock[] AnimatedBlocks;

		public MapParticle[] MapParticles;

		public MapSoundObject[] SoundObjects;

		public int SolidPlaneMinY;

		public bool IsUnderground;
	}

	public struct MapSoundObject
	{
		public int BlockIndex;

		public Vector3 Position;

		public uint SoundEventIndex;

		public AudioDevice.SoundEventReference SoundEventReference;
	}

	public struct MapParticle
	{
		public int BlockIndex;

		public string ParticleSystemId;

		public Vector3 Position;

		public Quaternion Rotation;

		public Vector3 PositionOffset;

		public Quaternion RotationOffset;

		public UInt32Color Color;

		public float BlockScale;

		public float Scale;

		public int TargetNodeIndex;

		public ParticleSystemProxy ParticleSystemProxy;
	}

	public struct AnimatedBlock
	{
		public bool IsBeingHit;

		public int Index;

		public Vector3 Position;

		public BoundingBox BoundingBox;

		public Matrix Matrix;

		public AnimatedBlockRenderer Renderer;

		public BlockyAnimation Animation;

		public float AnimationTimeOffset;

		public int[] MapParticleIndices;
	}

	public volatile bool GeometryNeedsUpdate = true;

	public volatile ChunkRebuildState RebuildState;

	public ChunkUpdateTask UpdateTask;

	public int OpaqueIndicesCount;

	public readonly GLVertexArray OpaqueVertexArray;

	public readonly GLBuffer OpaqueVerticesBuffer;

	public readonly GLBuffer OpaqueIndicesBuffer;

	public int AlphaBlendedIndicesCount;

	public readonly GLVertexArray AlphaBlendedVertexArray;

	public readonly GLBuffer AlphaBlendedVerticesBuffer;

	public readonly GLBuffer AlphaBlendedIndicesBuffer;

	public int AlphaTestedAnimatedLowLODIndicesCount;

	public int AlphaTestedLowLODIndicesCount;

	public int AlphaTestedHighLODIndicesCount;

	public int AlphaTestedAnimatedHighLODIndicesCount;

	public readonly GLVertexArray AlphaTestedVertexArray;

	public readonly GLBuffer AlphaTestedVerticesBuffer;

	public readonly GLBuffer AlphaTestedIndicesBuffer;

	public AnimatedBlock[] AnimatedBlocks;

	public MapParticle[] MapParticles;

	public MapSoundObject[] SoundObjects;

	public int BufferUpdateCount;

	private readonly GraphicsDevice _graphics;

	public int AlphaTestedIndicesCount => AlphaTestedAnimatedLowLODIndicesCount + AlphaTestedLowLODIndicesCount + AlphaTestedHighLODIndicesCount + AlphaTestedAnimatedHighLODIndicesCount;

	public RenderedChunk(GraphicsDevice graphics)
	{
		_graphics = graphics;
		GLFunctions gL = graphics.GL;
		OpaqueVertexArray = gL.GenVertexArray();
		gL.BindVertexArray(OpaqueVertexArray);
		OpaqueVerticesBuffer = gL.GenBuffer();
		gL.BindBuffer(OpaqueVertexArray, GL.ARRAY_BUFFER, OpaqueVerticesBuffer);
		OpaqueIndicesBuffer = gL.GenBuffer();
		gL.BindBuffer(OpaqueVertexArray, GL.ELEMENT_ARRAY_BUFFER, OpaqueIndicesBuffer);
		SetupVertexAttributes();
		AlphaBlendedVertexArray = gL.GenVertexArray();
		gL.BindVertexArray(AlphaBlendedVertexArray);
		AlphaBlendedVerticesBuffer = gL.GenBuffer();
		gL.BindBuffer(AlphaBlendedVertexArray, GL.ARRAY_BUFFER, AlphaBlendedVerticesBuffer);
		AlphaBlendedIndicesBuffer = gL.GenBuffer();
		gL.BindBuffer(AlphaBlendedVertexArray, GL.ELEMENT_ARRAY_BUFFER, AlphaBlendedIndicesBuffer);
		SetupVertexAttributes();
		AlphaTestedVertexArray = gL.GenVertexArray();
		gL.BindVertexArray(AlphaTestedVertexArray);
		AlphaTestedVerticesBuffer = gL.GenBuffer();
		gL.BindBuffer(AlphaTestedVertexArray, GL.ARRAY_BUFFER, AlphaTestedVerticesBuffer);
		AlphaTestedIndicesBuffer = gL.GenBuffer();
		gL.BindBuffer(AlphaTestedVertexArray, GL.ELEMENT_ARRAY_BUFFER, AlphaTestedIndicesBuffer);
		SetupVertexAttributes();
	}

	private void SetupVertexAttributes()
	{
		GLFunctions gL = _graphics.GL;
		MapChunkNearProgram mapChunkNearOpaqueProgram = _graphics.GPUProgramStore.MapChunkNearOpaqueProgram;
		IntPtr zero = IntPtr.Zero;
		gL.EnableVertexAttribArray(mapChunkNearOpaqueProgram.AttribPositionAndDoubleSidedAndBlockId.Index);
		gL.VertexAttribIPointer(mapChunkNearOpaqueProgram.AttribPositionAndDoubleSidedAndBlockId.Index, 4, GL.SHORT, ChunkVertex.Size, zero);
		zero += 8;
		gL.EnableVertexAttribArray(mapChunkNearOpaqueProgram.AttribTexCoords.Index);
		gL.VertexAttribPointer(mapChunkNearOpaqueProgram.AttribTexCoords.Index, 4, GL.UNSIGNED_SHORT, normalized: true, ChunkVertex.Size, zero);
		zero += 8;
		gL.EnableVertexAttribArray(mapChunkNearOpaqueProgram.AttribDataPacked.Index);
		gL.VertexAttribIPointer(mapChunkNearOpaqueProgram.AttribDataPacked.Index, 4, GL.UNSIGNED_INT, ChunkVertex.Size, zero);
		zero += 16;
	}

	public void Discard()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (UpdateTask != null)
		{
			if (UpdateTask.AnimatedBlocks != null)
			{
				for (int i = 0; i < UpdateTask.AnimatedBlocks.Length; i++)
				{
					UpdateTask.AnimatedBlocks[i].Renderer.Dispose();
					UpdateTask.AnimatedBlocks[i].MapParticleIndices = null;
				}
			}
			if (UpdateTask.MapParticles != null)
			{
				for (int j = 0; j < UpdateTask.MapParticles.Length; j++)
				{
					if (UpdateTask.MapParticles[j].ParticleSystemProxy != null)
					{
						UpdateTask.MapParticles[j].ParticleSystemProxy.Expire(instant: true);
						UpdateTask.MapParticles[j].ParticleSystemProxy = null;
					}
				}
			}
		}
		UpdateTask = null;
		RebuildState = ChunkRebuildState.UpdateReady;
		BufferUpdateCount = 0;
		GeometryNeedsUpdate = true;
	}

	protected override void DoDispose()
	{
		GLFunctions gL = _graphics.GL;
		gL.DeleteBuffer(OpaqueVerticesBuffer);
		gL.DeleteBuffer(OpaqueIndicesBuffer);
		gL.DeleteVertexArray(OpaqueVertexArray);
		gL.DeleteBuffer(AlphaBlendedVerticesBuffer);
		gL.DeleteBuffer(AlphaBlendedIndicesBuffer);
		gL.DeleteVertexArray(AlphaBlendedVertexArray);
		gL.DeleteBuffer(AlphaTestedVerticesBuffer);
		gL.DeleteBuffer(AlphaTestedIndicesBuffer);
		gL.DeleteVertexArray(AlphaTestedVertexArray);
		if (AnimatedBlocks != null)
		{
			for (int i = 0; i < AnimatedBlocks.Length; i++)
			{
				AnimatedBlocks[i].Renderer.Dispose();
			}
			AnimatedBlocks = null;
		}
		if (MapParticles != null)
		{
			for (int j = 0; j < MapParticles.Length; j++)
			{
				if (MapParticles[j].ParticleSystemProxy != null)
				{
					MapParticles[j].ParticleSystemProxy.Expire(instant: true);
					MapParticles[j].ParticleSystemProxy = null;
				}
			}
		}
		if (SoundObjects != null)
		{
			throw new Exception("SoundObjects not disposed properly before its column.");
		}
		if (UpdateTask != null)
		{
			throw new Exception("Chunk was not disposed properly before its column.");
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetAlphaTestedData(bool useAnimatedBlocks, float levelOfDetailFactor, out int dataCount, out IntPtr dataOffset)
	{
		if (useAnimatedBlocks)
		{
			float num = (float)AlphaTestedHighLODIndicesCount * levelOfDetailFactor;
			int num2 = (int)((float)AlphaTestedLowLODIndicesCount + num);
			dataCount = num2;
			dataOffset = (IntPtr)(AlphaTestedAnimatedLowLODIndicesCount * 4);
		}
		else
		{
			float num3 = (float)(AlphaTestedHighLODIndicesCount + AlphaTestedAnimatedHighLODIndicesCount) * levelOfDetailFactor;
			int num4 = (int)((float)(AlphaTestedAnimatedLowLODIndicesCount + AlphaTestedLowLODIndicesCount) + num3);
			dataCount = num4;
			dataOffset = IntPtr.Zero;
		}
	}
}
