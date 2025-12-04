#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Graphics;

internal class SceneView
{
	public Vector3 Position;

	public Vector3 Direction;

	public BoundingFrustum Frustum;

	public KDop KDopFrustum;

	public bool UseKDopForCulling = false;

	public int IncomingEntityDrawTaskCount;

	private const int EntitiesDefaultSize = 1000;

	private const int EntitiesGrowth = 500;

	private int _entitiesCount;

	private ushort[] _entitiesIds = new ushort[1000];

	private Vector3[] _entitiesPositions = new Vector3[1000];

	private float[] _entitiesDistancesToCamera = new float[1000];

	private ushort[] _sortedEntitiesIds = new ushort[1000];

	public bool[] EntitiesFrustumCullingResults = new bool[1000];

	public int IncomingChunkDrawTaskCount;

	private const int ChunksDefaultSize = 1000;

	private const int ChunksGrowth = 500;

	private int _chunksCount;

	private ushort[] _chunksIds = new ushort[1000];

	private Vector3[] _chunksPositions = new Vector3[1000];

	private float[] _chunksDistancesToCamera = new float[1000];

	private ushort[] _sortedChunksIds = new ushort[1000];

	public bool[] ChunksFrustumCullingResults = new bool[1000];

	public int EntitiesCount => _entitiesCount;

	public int ChunksCount => _chunksCount;

	public void ResetCounters()
	{
		_entitiesCount = 0;
		_chunksCount = 0;
	}

	public void PrepareForIncomingEntities(int max)
	{
		ArrayUtils.GrowArrayIfNecessary(ref _entitiesIds, max, 500);
		ArrayUtils.GrowArrayIfNecessary(ref _entitiesPositions, max, 500);
		ArrayUtils.GrowArrayIfNecessary(ref _entitiesDistancesToCamera, max, 500);
		ArrayUtils.GrowArrayIfNecessary(ref _sortedEntitiesIds, max, 500);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterEntity(int entityLocalId, Vector3 entityPosition)
	{
		int entitiesCount = _entitiesCount;
		_entitiesIds[entitiesCount] = (ushort)entityLocalId;
		_entitiesPositions[entitiesCount] = entityPosition;
		_entitiesDistancesToCamera[entitiesCount] = Vector3.DistanceSquared(entityPosition, Position);
		_entitiesCount++;
	}

	public void SortEntitiesByDistance()
	{
		Debug.Assert(_entitiesCount < _sortedEntitiesIds.Length, "Array is too small. Did you forget a call to PrepareForIncomingEntities()?");
		Array.Copy(_entitiesIds, _sortedEntitiesIds, _entitiesCount);
		Array.Sort(_entitiesDistancesToCamera, _sortedEntitiesIds, 0, _entitiesCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetSortedEntityId(int id)
	{
		return _sortedEntitiesIds[id];
	}

	public void PrepareForIncomingChunks(int max)
	{
		ArrayUtils.GrowArrayIfNecessary(ref _chunksIds, max, 500);
		ArrayUtils.GrowArrayIfNecessary(ref _chunksPositions, max, 500);
		ArrayUtils.GrowArrayIfNecessary(ref _chunksDistancesToCamera, max, 500);
		ArrayUtils.GrowArrayIfNecessary(ref _sortedChunksIds, max, 500);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterChunk(int chunkLocalId, Vector3 chunkPosition)
	{
		int chunksCount = _chunksCount;
		_chunksIds[chunksCount] = (ushort)chunkLocalId;
		_chunksPositions[chunksCount] = chunkPosition;
		_chunksDistancesToCamera[chunksCount] = Vector3.DistanceSquared(chunkPosition, Position);
		_chunksCount++;
	}

	public void SortChunksByDistance()
	{
		Debug.Assert(_chunksCount < _sortedChunksIds.Length, "Array is too small. Did you forget a call to PrepareForIncomingChunks()?");
		Array.Copy(_chunksIds, _sortedChunksIds, _chunksCount);
		Array.Sort(_chunksDistancesToCamera, _sortedChunksIds, 0, _chunksCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetSortedChunkId(int id)
	{
		return _sortedChunksIds[id];
	}
}
