#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using HytaleClient.Core;
using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules.Map;

internal class ChunkColumn : Disposable
{
	public readonly object DisposeLock = new object();

	public readonly int X;

	public readonly int Z;

	public uint[] Tints;

	public ushort[] Heights;

	public ushort[][] Environments;

	private readonly ConcurrentDictionary<int, Chunk> _chunks = new ConcurrentDictionary<int, Chunk>();

	public ChunkColumn(int x, int z)
	{
		X = x;
		Z = z;
	}

	protected override void DoDispose()
	{
		foreach (Chunk value in _chunks.Values)
		{
			if (!value.Disposed)
			{
				throw new Exception("Chunk was not disposed properly before its column.");
			}
		}
		_chunks.Clear();
	}

	public Chunk CreateChunk(int y)
	{
		Chunk chunk = new Chunk(X, y, Z);
		_chunks.TryAdd(y, chunk);
		return chunk;
	}

	public Chunk GetChunk(int y)
	{
		_chunks.TryGetValue(y, out var value);
		return value;
	}

	public void DiscardRenderedChunks()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		foreach (Chunk value in _chunks.Values)
		{
			lock (value.DisposeLock)
			{
				if (value.Rendered != null)
				{
					value.Rendered.Discard();
				}
				value.Data.SelfLightNeedsUpdate = true;
			}
		}
	}
}
