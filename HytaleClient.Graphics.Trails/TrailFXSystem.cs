using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HytaleClient.Core;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Graphics.Trails;

internal class TrailFXSystem : Disposable
{
	private const float DefaultCullDistanceSquared = 1600f;

	private GraphicsDevice _graphics;

	private float _engineTimeStep;

	private const int ProxyDefaultSize = 50;

	private const int ProxyGrowth = 100;

	private int _maxTrailSpawned = 300;

	private UpdateTrailLightingFunc _updateTrailLightingFunc;

	private ushort _trailProxyCount = 0;

	private TrailProxy[] _trailProxies;

	private float[] _trailProxyDistanceToCamera;

	private ushort[] _sortedTrailProxyIds;

	private readonly List<int> _expiredTrailIds = new List<int>();

	private readonly Dictionary<int, Trail> _trails = new Dictionary<int, Trail>();

	private int _nextTrailId = 0;

	private float _accumulatedDeltaTime = 0f;

	private FXMemoryPool<SegmentBuffers> _segmentMemoryPool;

	private const int _maxParticleDrawCount = 10000;

	private int _trailBlendDrawCount = 0;

	private int _trailFPSDrawCount = 0;

	private int _trailDistortionDrawCount = 0;

	private int _trailErosionDrawCount = 0;

	private const int TrailDrawDefaultSize = 20;

	private const int TrailDrawGrowth = 10;

	private const int TrailFPSDrawDefaultSize = 2;

	private const int TrailFPSDrawGrowth = 4;

	private Trail[] _trailBlendDraw = new Trail[20];

	private Trail[] _trailFPSDraw = new Trail[2];

	private Trail[] _trailDistortionDraw = new Trail[20];

	private Trail[] _trailErosionDraw = new Trail[20];

	public int BlendDrawCount => _trailBlendDrawCount + _trailFPSDrawCount;

	public bool HasDistortionTasks => _trailDistortionDrawCount > 0;

	public bool HasErosionTasks => _trailErosionDrawCount > 0;

	public ushort TrailProxyCount => _trailProxyCount;

	public int TrailCount => _trails.Count;

	public SegmentBuffers SegmentBuffer => _segmentMemoryPool.Storage;

	public int MaxParticleDrawCount => 10000;

	public int SegmentBufferStorageMaxCount => _segmentMemoryPool.ItemMaxCount;

	public TrailFXSystem(GraphicsDevice graphics, float engineTimeStep)
	{
		_graphics = graphics;
		_engineTimeStep = engineTimeStep;
		Initialize();
	}

	public void InitializeFunction(UpdateTrailLightingFunc updateTrailLighting)
	{
		_updateTrailLightingFunc = updateTrailLighting;
	}

	public void DisposeFunction()
	{
		_updateTrailLightingFunc = null;
	}

	private void Initialize()
	{
		InitMemory();
		InitTrails();
	}

	private void InitMemory()
	{
		_segmentMemoryPool = new FXMemoryPool<SegmentBuffers>();
		_segmentMemoryPool.Initialize(256000);
	}

	private void InitTrails()
	{
		_trailProxies = new TrailProxy[50];
		_trailProxyDistanceToCamera = new float[50];
		_sortedTrailProxyIds = new ushort[50];
	}

	private void DisposeMemory()
	{
		_segmentMemoryPool.Release();
		_segmentMemoryPool = null;
	}

	protected override void DoDispose()
	{
		DisposeTrails();
		DisposeMemory();
	}

	private void DisposeTrails()
	{
		foreach (Trail value in _trails.Values)
		{
			value.Dispose();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int RequestSegmentBufferStorage(int count)
	{
		return _segmentMemoryPool.TakeSlots(count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ReleaseSegmentBufferStorage(int segmentStartIndex, int segmentCount)
	{
		_segmentMemoryPool.ReleaseSlots(segmentStartIndex, segmentCount);
	}

	public void UpdateProxies(Vector3 cameraPosition, bool useProxyCheck)
	{
		int num = 0;
		while (num < _trailProxyCount)
		{
			TrailProxy trailProxy = _trailProxies[num];
			if (trailProxy.IsExpired)
			{
				if (trailProxy.Trail != null)
				{
					trailProxy.Trail.IsExpired = true;
					trailProxy.Trail = null;
				}
				_trailProxyCount--;
				_trailProxies[num] = _trailProxies[_trailProxyCount];
			}
			else
			{
				_trailProxyDistanceToCamera[num] = (trailProxy.IsLocalPlayer ? 0f : Vector3.DistanceSquared(cameraPosition, trailProxy.Position));
				_sortedTrailProxyIds[num] = (ushort)num;
				num++;
			}
		}
		Array.Sort(_trailProxyDistanceToCamera, _sortedTrailProxyIds, 0, _trailProxyCount);
		int num2 = 0;
		for (int i = 0; i < _trailProxyCount; i++)
		{
			ushort num3 = _sortedTrailProxyIds[i];
			float num4 = _trailProxyDistanceToCamera[i];
			TrailProxy trailProxy2 = _trailProxies[num3];
			if (trailProxy2.Trail == null)
			{
				if (_trails.Count == _maxTrailSpawned)
				{
					num2++;
					continue;
				}
				if (!useProxyCheck || num4 < 1600f)
				{
					trailProxy2.Trail = CreateTrail(trailProxy2);
				}
			}
			else
			{
				trailProxy2.Trail.Position = trailProxy2.Position;
				trailProxy2.Trail.Rotation = trailProxy2.Rotation;
				bool flag = i >= _maxTrailSpawned && num2 > 0;
				if ((useProxyCheck && num4 > 1625f) || flag)
				{
					trailProxy2.Trail.IsExpired = true;
					trailProxy2.Trail = null;
					num2--;
				}
			}
			if (trailProxy2.Trail != null)
			{
				trailProxy2.Trail.Visible = trailProxy2.Visible;
			}
		}
	}

	public void UpdateSimulation(float deltaTime)
	{
		_accumulatedDeltaTime += deltaTime;
		if (_accumulatedDeltaTime < _engineTimeStep)
		{
			foreach (Trail value in _trails.Values)
			{
				value.LightUpdate();
			}
			return;
		}
		_trailBlendDrawCount = 0;
		_trailDistortionDrawCount = 0;
		_trailErosionDrawCount = 0;
		_trailFPSDrawCount = 0;
		int num = 0;
		foreach (Trail value2 in _trails.Values)
		{
			if (value2.NeedsUpdating())
			{
				value2.Update();
				_updateTrailLightingFunc(value2);
				if (!value2.Visible)
				{
					continue;
				}
				num += value2.ParticleCount;
				if (num > 10000)
				{
					break;
				}
				if (value2.IsDistortion)
				{
					if (_trailDistortionDrawCount == _trailDistortionDraw.Length)
					{
						Array.Resize(ref _trailDistortionDraw, _trailDistortionDraw.Length + 10);
					}
					_trailDistortionDraw[_trailDistortionDrawCount] = value2;
					_trailDistortionDrawCount++;
				}
				else if (value2.RenderMode == FXSystem.RenderMode.Erosion)
				{
					if (_trailErosionDrawCount == _trailErosionDraw.Length)
					{
						Array.Resize(ref _trailErosionDraw, _trailErosionDraw.Length + 10);
					}
					_trailErosionDraw[_trailErosionDrawCount] = value2;
					_trailErosionDrawCount++;
				}
				else if (!value2.IsFirstPerson)
				{
					FXSystem.RenderMode renderMode = value2.RenderMode;
					FXSystem.RenderMode renderMode2 = renderMode;
					if ((uint)renderMode2 <= 1u)
					{
						if (_trailBlendDrawCount == _trailBlendDraw.Length)
						{
							Array.Resize(ref _trailBlendDraw, _trailBlendDraw.Length + 10);
						}
						_trailBlendDraw[_trailBlendDrawCount] = value2;
						_trailBlendDrawCount++;
					}
				}
				else
				{
					if (_trailFPSDrawCount == _trailFPSDraw.Length)
					{
						Array.Resize(ref _trailFPSDraw, _trailFPSDraw.Length + 4);
					}
					_trailFPSDraw[_trailFPSDrawCount] = value2;
					_trailFPSDrawCount++;
				}
			}
			else if (value2.IsExpired)
			{
				_expiredTrailIds.Add(value2.Id);
			}
		}
		for (int i = 0; i < _expiredTrailIds.Count; i++)
		{
			_trails[_expiredTrailIds[i]].Dispose();
			_trails.Remove(_expiredTrailIds[i]);
		}
		_expiredTrailIds.Clear();
		_accumulatedDeltaTime = 0f;
	}

	public bool TrySpawnTrail(TrailSettings trailSettings, Vector2 textureAltasInverseSize, out TrailProxy trailProxy, bool isLocalPlayer = false)
	{
		trailProxy = null;
		if (_trailProxyCount == ushort.MaxValue)
		{
			return false;
		}
		ArrayUtils.GrowArrayIfNecessary(ref _trailProxies, _trailProxyCount + 1, 100);
		ArrayUtils.GrowArrayIfNecessary(ref _trailProxyDistanceToCamera, _trailProxyCount + 1, 100);
		ArrayUtils.GrowArrayIfNecessary(ref _sortedTrailProxyIds, _trailProxyCount + 1, 100);
		trailProxy = new TrailProxy();
		trailProxy.Settings = trailSettings;
		trailProxy.TextureAltasInverseSize = textureAltasInverseSize;
		trailProxy.IsLocalPlayer = isLocalPlayer;
		_trailProxies[_trailProxyCount] = trailProxy;
		_trailProxyCount++;
		return true;
	}

	public void PrepareBlendVertexDataStorage(FXRenderer fXRenderer)
	{
		if (_trailBlendDrawCount != 0)
		{
			int num = fXRenderer.BlendDrawParams.Count;
			for (int i = 0; i < _trailBlendDrawCount; i++)
			{
				ushort drawId = fXRenderer.ReserveDrawTask();
				_trailBlendDraw[i].ReserveVertexDataStorage(ref fXRenderer.FXVertexBuffer, drawId);
				num += _trailBlendDraw[i].ParticleCount;
			}
			fXRenderer.BlendDrawParams.Count = num;
			if (!fXRenderer.BlendDrawParams.IsStartOffsetSet)
			{
				fXRenderer.BlendDrawParams.StartOffset = (uint)_trailBlendDraw[0].ParticleVertexDataStartIndex;
				fXRenderer.BlendDrawParams.IsStartOffsetSet = true;
			}
		}
	}

	public void PrepareFPVVertexDataStorage(FXRenderer fXRenderer)
	{
		if (_trailFPSDrawCount != 0)
		{
			int num = fXRenderer.BlendFPVDrawParams.Count;
			for (int i = 0; i < _trailFPSDrawCount; i++)
			{
				ushort drawId = fXRenderer.ReserveDrawTask();
				_trailFPSDraw[i].ReserveVertexDataStorage(ref fXRenderer.FXVertexBuffer, drawId);
				num += _trailFPSDraw[i].ParticleCount;
			}
			fXRenderer.BlendFPVDrawParams.Count = num;
			if (!fXRenderer.BlendFPVDrawParams.IsStartOffsetSet)
			{
				fXRenderer.BlendFPVDrawParams.StartOffset = (uint)_trailFPSDraw[0].ParticleVertexDataStartIndex;
				fXRenderer.BlendFPVDrawParams.IsStartOffsetSet = true;
			}
		}
	}

	public void PrepareDistortionVertexDataStorage(FXRenderer fXRenderer)
	{
		if (_trailDistortionDrawCount != 0)
		{
			int num = fXRenderer.DistortionDrawParams.Count;
			for (int i = 0; i < _trailDistortionDrawCount; i++)
			{
				ushort drawId = fXRenderer.ReserveDrawTask();
				_trailDistortionDraw[i].ReserveVertexDataStorage(ref fXRenderer.FXVertexBuffer, drawId);
				num += _trailDistortionDraw[i].ParticleCount;
			}
			fXRenderer.DistortionDrawParams.Count = num;
			if (!fXRenderer.DistortionDrawParams.IsStartOffsetSet)
			{
				fXRenderer.DistortionDrawParams.StartOffset = (uint)_trailDistortionDraw[0].ParticleVertexDataStartIndex;
				fXRenderer.DistortionDrawParams.IsStartOffsetSet = true;
			}
		}
	}

	public void PrepareErosionVertexDataStorage(FXRenderer fXRenderer)
	{
		if (_trailErosionDrawCount != 0)
		{
			int num = fXRenderer.ErosionDrawParams.Count;
			for (int i = 0; i < _trailErosionDrawCount; i++)
			{
				ushort drawId = fXRenderer.ReserveDrawTask();
				_trailErosionDraw[i].ReserveVertexDataStorage(ref fXRenderer.FXVertexBuffer, drawId);
				num += _trailErosionDraw[i].ParticleCount;
			}
			fXRenderer.ErosionDrawParams.Count = num;
			if (!fXRenderer.ErosionDrawParams.IsStartOffsetSet)
			{
				fXRenderer.ErosionDrawParams.StartOffset = (uint)_trailErosionDraw[0].ParticleVertexDataStartIndex;
				fXRenderer.ErosionDrawParams.IsStartOffsetSet = true;
			}
		}
	}

	public void PrepareForDraw(FXRenderer fXRenderer, Vector3 cameraPosition, IntPtr gpuDrawDataPtr)
	{
		for (int i = 0; i < _trailBlendDrawCount; i++)
		{
			_trailBlendDraw[i].PrepareForDraw(cameraPosition, ref fXRenderer.FXVertexBuffer, gpuDrawDataPtr);
		}
		for (int j = 0; j < _trailFPSDrawCount; j++)
		{
			_trailFPSDraw[j].PrepareForDraw(cameraPosition, ref fXRenderer.FXVertexBuffer, gpuDrawDataPtr);
		}
		for (int k = 0; k < _trailDistortionDrawCount; k++)
		{
			_trailDistortionDraw[k].PrepareForDraw(cameraPosition, ref fXRenderer.FXVertexBuffer, gpuDrawDataPtr);
		}
		for (int l = 0; l < _trailErosionDrawCount; l++)
		{
			_trailErosionDraw[l].PrepareForDraw(cameraPosition, ref fXRenderer.FXVertexBuffer, gpuDrawDataPtr);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Trail CreateTrail(TrailProxy trailProxy)
	{
		Trail trail = new Trail(_graphics, this, trailProxy.Settings, trailProxy.TextureAltasInverseSize, _nextTrailId++);
		if (trail.Initialize())
		{
			trail.SetScale(trailProxy.Scale);
			trail.IsFirstPerson = trailProxy.IsFirstPerson;
			trail.Position = trailProxy.Position;
			trail.Rotation = trailProxy.Rotation;
			trail.SetSpawn();
			_trails.Add(trail.Id, trail);
		}
		return trail;
	}
}
