using System.Runtime.CompilerServices;
using System.Threading;
using HytaleClient.Math;

namespace HytaleClient.Graphics;

internal struct FXVertexBuffer
{
	private FXVertex[] _particleVertices;

	private int _particleCount;

	public FXVertex[] ParticleVertices => _particleVertices;

	public void Initialize(int maxParticleCount)
	{
		_particleVertices = new FXVertex[maxParticleCount * 4];
	}

	public void Dispose()
	{
		_particleVertices = null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ClearVertexDataStorage()
	{
		_particleCount = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetVertexDataStored()
	{
		return _particleCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ReserveVertexDataStorage(int count)
	{
		int num = Interlocked.Add(ref _particleCount, count);
		return num - count;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetVertexDataConfig(int particleIndex, uint config)
	{
		_particleVertices[particleIndex * 4].Config = config;
		_particleVertices[particleIndex * 4 + 1].Config = config;
		_particleVertices[particleIndex * 4 + 2].Config = config;
		_particleVertices[particleIndex * 4 + 3].Config = config;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetParticleVertexDataPositionAndScale(int particleIndex, Vector3 position, Vector2 scale)
	{
		_particleVertices[particleIndex * 4].Position = position;
		_particleVertices[particleIndex * 4 + 1].Position = position;
		_particleVertices[particleIndex * 4 + 2].Position = position;
		_particleVertices[particleIndex * 4 + 3].Position = position;
		_particleVertices[particleIndex * 4].Scale = scale;
		_particleVertices[particleIndex * 4 + 1].Scale = scale;
		_particleVertices[particleIndex * 4 + 2].Scale = scale;
		_particleVertices[particleIndex * 4 + 3].Scale = scale;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetParticleVertexDataTextureInfo(int particleIndex, uint textureInfo)
	{
		_particleVertices[particleIndex * 4].TextureInfo = textureInfo;
		_particleVertices[particleIndex * 4 + 1].TextureInfo = textureInfo;
		_particleVertices[particleIndex * 4 + 2].TextureInfo = textureInfo;
		_particleVertices[particleIndex * 4 + 3].TextureInfo = textureInfo;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetParticleVertexDataColor(int particleIndex, uint color)
	{
		_particleVertices[particleIndex * 4].Color = color;
		_particleVertices[particleIndex * 4 + 1].Color = color;
		_particleVertices[particleIndex * 4 + 2].Color = color;
		_particleVertices[particleIndex * 4 + 3].Color = color;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetParticleVertexDataVelocityAndRotation(int particleIndex, Vector3 velocity, Vector4 rotation)
	{
		_particleVertices[particleIndex * 4].Velocity = velocity;
		_particleVertices[particleIndex * 4 + 1].Velocity = velocity;
		_particleVertices[particleIndex * 4 + 2].Velocity = velocity;
		_particleVertices[particleIndex * 4 + 3].Velocity = velocity;
		_particleVertices[particleIndex * 4].Rotation = rotation;
		_particleVertices[particleIndex * 4 + 1].Rotation = rotation;
		_particleVertices[particleIndex * 4 + 2].Rotation = rotation;
		_particleVertices[particleIndex * 4 + 3].Rotation = rotation;
	}

	public void SetParticleVertexDataSeedAndLifeRatio(int particleIndex, uint seedAndLifeRatio)
	{
		_particleVertices[particleIndex * 4].SeedAndLifeRatio = seedAndLifeRatio;
		_particleVertices[particleIndex * 4 + 1].SeedAndLifeRatio = seedAndLifeRatio;
		_particleVertices[particleIndex * 4 + 2].SeedAndLifeRatio = seedAndLifeRatio;
		_particleVertices[particleIndex * 4 + 3].SeedAndLifeRatio = seedAndLifeRatio;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTrailFirstSegmentVertexLength(int segmentIndex, float length)
	{
		_particleVertices[segmentIndex * 4].Length = length;
		_particleVertices[segmentIndex * 4 + 1].Length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTrailLastSegmentVertexLength(int segmentIndex, float length)
	{
		_particleVertices[(segmentIndex - 2) * 4 + 2].Length = length;
		_particleVertices[(segmentIndex - 2) * 4 + 3].Length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTrailSegmentVertexLength(int segmentIndex, float length)
	{
		segmentIndex--;
		_particleVertices[2 + segmentIndex * 4].Length = length;
		_particleVertices[2 + segmentIndex * 4 + 1].Length = length;
		_particleVertices[2 + segmentIndex * 4 + 2].Length = length;
		_particleVertices[2 + segmentIndex * 4 + 3].Length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTrailFirstSegmentVertexPosition(int segmentIndex, Vector3 position, Quaternion rotation, float length)
	{
		Vector3 topLeftPosition = position + Vector3.Transform(new Vector3(0f, length, 0f), rotation);
		Vector3 bottomLeftPosition = position + Vector3.Transform(new Vector3(0f, 0f - length, 0f), rotation);
		ref FXVertex reference = ref _particleVertices[segmentIndex * 4];
		reference.TopLeftPosition = topLeftPosition;
		reference.BottomLeftPosition = bottomLeftPosition;
		ref FXVertex reference2 = ref _particleVertices[segmentIndex * 4 + 1];
		reference2.TopLeftPosition = topLeftPosition;
		reference2.BottomLeftPosition = bottomLeftPosition;
		ref FXVertex reference3 = ref _particleVertices[segmentIndex * 4 + 2];
		reference3.TopLeftPosition = topLeftPosition;
		reference3.BottomLeftPosition = bottomLeftPosition;
		ref FXVertex reference4 = ref _particleVertices[segmentIndex * 4 + 3];
		reference4.TopLeftPosition = topLeftPosition;
		reference4.BottomLeftPosition = bottomLeftPosition;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTrailLastSegmentVertexPosition(int segmentIndex, Vector3 position, Quaternion rotation, float length)
	{
		Vector3 topRightPosition = position + Vector3.Transform(new Vector3(0f, length, 0f), rotation);
		Vector3 bottomRightPosition = position + Vector3.Transform(new Vector3(0f, 0f - length, 0f), rotation);
		ref FXVertex reference = ref _particleVertices[(segmentIndex - 2) * 4];
		reference.TopRightPosition = topRightPosition;
		reference.BottomRightPosition = bottomRightPosition;
		ref FXVertex reference2 = ref _particleVertices[(segmentIndex - 2) * 4 + 1];
		reference2.TopRightPosition = topRightPosition;
		reference2.BottomRightPosition = bottomRightPosition;
		ref FXVertex reference3 = ref _particleVertices[(segmentIndex - 2) * 4 + 2];
		reference3.TopRightPosition = topRightPosition;
		reference3.BottomRightPosition = bottomRightPosition;
		ref FXVertex reference4 = ref _particleVertices[(segmentIndex - 2) * 4 + 3];
		reference4.TopRightPosition = topRightPosition;
		reference4.BottomRightPosition = bottomRightPosition;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTrailSegmentVertexPosition(int segmentIndex, Vector3 position, Quaternion rotation, float length)
	{
		Vector3 vector = position + Vector3.Transform(new Vector3(0f, length, 0f), rotation);
		Vector3 vector2 = position + Vector3.Transform(new Vector3(0f, 0f - length, 0f), rotation);
		ref FXVertex reference = ref _particleVertices[(segmentIndex - 1) * 4];
		reference.TopRightPosition = vector;
		reference.BottomRightPosition = vector2;
		ref FXVertex reference2 = ref _particleVertices[(segmentIndex - 1) * 4 + 1];
		reference2.TopRightPosition = vector;
		reference2.BottomRightPosition = vector2;
		ref FXVertex reference3 = ref _particleVertices[(segmentIndex - 1) * 4 + 2];
		reference3.TopRightPosition = vector;
		reference3.BottomRightPosition = vector2;
		ref FXVertex reference4 = ref _particleVertices[(segmentIndex - 1) * 4 + 3];
		reference4.TopRightPosition = vector;
		reference4.BottomRightPosition = vector2;
		ref FXVertex reference5 = ref _particleVertices[(segmentIndex - 1) * 4 + 4];
		reference5.TopLeftPosition = vector;
		reference5.BottomLeftPosition = vector2;
		ref FXVertex reference6 = ref _particleVertices[(segmentIndex - 1) * 4 + 5];
		reference6.TopLeftPosition = vector;
		reference6.BottomLeftPosition = vector2;
		ref FXVertex reference7 = ref _particleVertices[(segmentIndex - 1) * 4 + 6];
		reference7.TopLeftPosition = vector;
		reference7.BottomLeftPosition = vector2;
		ref FXVertex reference8 = ref _particleVertices[(segmentIndex - 1) * 4 + 7];
		reference8.TopLeftPosition = vector;
		reference8.BottomLeftPosition = vector2;
	}
}
