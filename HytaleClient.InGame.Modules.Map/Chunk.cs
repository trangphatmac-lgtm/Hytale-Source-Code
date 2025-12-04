#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Core;
using HytaleClient.Data.Map;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Map;
using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules.Map;

internal class Chunk : Disposable
{
	public readonly object DisposeLock = new object();

	public readonly int X;

	public readonly int Y;

	public readonly int Z;

	public bool IsUnderground;

	public int SolidPlaneMinY;

	public readonly ChunkData Data;

	public RenderedChunk Rendered { get; private set; }

	public Chunk(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
		Data = new ChunkData();
	}

	protected override void DoDispose()
	{
		Rendered?.Dispose();
		if (Data.SelfLightAmounts != null || Data.BorderedLightAmounts != null)
		{
			throw new Exception("Chunk was not disposed properly before its column.");
		}
		if (Data.CurrentInteractionStates != null)
		{
			throw new Exception("Chunk interaction was not disposed properly.");
		}
	}

	public void Initialize(GraphicsDevice graphics)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(Chunk).FullName);
		}
		Rendered = new RenderedChunk(graphics);
	}
}
