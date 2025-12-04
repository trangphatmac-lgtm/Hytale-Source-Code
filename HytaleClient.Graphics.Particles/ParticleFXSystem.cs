#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HytaleClient.Core;
using HytaleClient.Graphics.Map;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Graphics.Particles;

internal class ParticleFXSystem : Disposable
{
	public enum ParticleRotationInfluence
	{
		None,
		Billboard,
		BillboardY,
		BillboardVelocity,
		Velocity
	}

	public enum ParticleCollisionBlockType
	{
		None,
		Air,
		Solid,
		All
	}

	public enum ParticleCollisionAction
	{
		Expire,
		LastFrame,
		Linger
	}

	private struct UpdateTask
	{
		public ParticleSystem ParticleSystem;

		public bool IsVisible;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct AnimatedBlockParticleUpdateTask
	{
		public AnimatedBlockRenderer AnimatedBlockRenderer;

		public RenderedChunk.MapParticle MapParticle;
	}

	private const float MaxDeltaTime = 0.033f;

	public bool IsLowResRenderingEnabled = true;

	public int PreviousFrameBlendDrawCount;

	public int PreviousFrameErosionDrawCount;

	public int PreviousFrameDistortionDrawCount;

	private GraphicsDevice _graphics;

	private float _engineTimeStep;

	private bool _isPaused;

	private bool _useParallelExecution = true;

	private const int ProxyDefaultSize = 50;

	private const int ProxyGrowth = 100;

	private int _maxParticleSystemSpawned = 300;

	private UpdateSpawnerLightingFunc _updateParticleSpawnerLightingFunc;

	private UpdateParticleCollisionFunc _updateParticleCollisionFunc;

	private InitParticleFunc _initParticleFunc;

	private ConcurrentQueue<int> _expiredParticleSystemIds;

	private ushort _particleSystemProxyCount = 0;

	private ParticleSystemProxy[] _particleSystemProxies;

	private float[] _particleSystemProxyDistanceToCamera;

	private ushort[] _sortedParticleSystemProxyIds;

	private Dictionary<int, ParticleSystemDebug> _particleSystemDebugs;

	private Dictionary<int, ParticleSystem> _particleSystems;

	private Mesh _debugSphereMesh;

	private int _nextParticleSystemId = 0;

	private float _accumulatedDeltaTime;

	private UpdateTask[] _updateTasks;

	private int _updateTaskCount = 0;

	private ParticleSystem[] _drawTasks;

	private int _drawTaskCount = 0;

	private float[] _distanceSquaredToCamera;

	private ushort[] _sortedDrawTaskIds;

	private ushort _sortedDrawTaskCount;

	private FXMemoryPool<ParticleBuffers> _particleMemoryPool;

	private const int _maxParticleDrawCount = 20000;

	private const int SpawnerDrawDefaultSize = 200;

	private const int SpawnerDrawGrowth = 200;

	private const int SpawnerFPVDrawDefaultSize = 10;

	private const int SpawnerFPVDrawGrowth = 5;

	private const int SpawnerDistortionDrawDefaultSize = 50;

	private const int SpawnerDistortionDrawGrowth = 50;

	private const int SpawnerErosionDrawDefaultSize = 100;

	private const int SpawnerErosionDrawGrowth = 100;

	private ParticleSpawner[] _spawnerErosionDraw = new ParticleSpawner[100];

	private ParticleSpawner[] _spawnerLowResDraw = new ParticleSpawner[200];

	private ParticleSpawner[] _spawnerBlendDraw = new ParticleSpawner[200];

	private ParticleSpawner[] _spawnerFPVDraw = new ParticleSpawner[10];

	private ParticleSpawner[] _spawnerDistortionDraw = new ParticleSpawner[50];

	private int _spawnerBlendDrawCount = 0;

	private int _spawnerLowResDrawCount = 0;

	private int _spawnerFPVDrawCount = 0;

	private int _spawnerDistortionDrawCount = 0;

	private int _spawnerErosionDrawCount = 0;

	private int _incomingMapParticlesTaskCount;

	private const int AnimatedBlockParticleUpdateTasksDefaultSize = 100;

	private const int AnimatedBlockParticleUpdateTasksGrowth = 25;

	private AnimatedBlockParticleUpdateTask[] _animatedBlockParticleUpdateTasks = new AnimatedBlockParticleUpdateTask[100];

	private int _animatedBlockParticleUpdateTaskCount;

	public bool IsPaused => _isPaused;

	public bool HasDistortionTasks => _spawnerDistortionDrawCount > 0;

	public bool HasErosionTasks => _spawnerErosionDrawCount > 0;

	public int HighResDrawCount => _spawnerFPVDrawCount + _spawnerBlendDrawCount;

	public int LowResDrawCount => _spawnerLowResDrawCount;

	public int ParticleSpawnerDrawCount => HighResDrawCount + LowResDrawCount;

	public ParticleSystemProxy[] ParticleSystemProxies => _particleSystemProxies;

	public int ParticleSystemProxyCount => _particleSystemProxyCount;

	public Dictionary<int, ParticleSystem> ParticleSystems => _particleSystems;

	public int ParticleSystemCount => _particleSystems.Count;

	public int MaxParticleCount => _particleMemoryPool.ItemMaxCount;

	public int MaxParticleDrawCount => 20000;

	public int MaxParticleSystemSpawned => _maxParticleSystemSpawned;

	public ParticleBuffers ParticleBuffer => _particleMemoryPool.Storage;

	public int ParticleBufferStorageMaxCount => _particleMemoryPool.ItemMaxCount;

	public bool UseParallelExecution(bool enable)
	{
		return _useParallelExecution = enable;
	}

	public void SetPaused(bool enable)
	{
		_isPaused = enable;
	}

	public bool DebugInfoNeedsDrawing()
	{
		return _particleSystemDebugs.Count != 0;
	}

	public void SetMaxParticleSystemSpawned(int max)
	{
		_maxParticleSystemSpawned = max;
		ArrayUtils.GrowArrayIfNecessary(ref _updateTasks, _maxParticleSystemSpawned, 0);
		ArrayUtils.GrowArrayIfNecessary(ref _drawTasks, _maxParticleSystemSpawned, 0);
		ArrayUtils.GrowArrayIfNecessary(ref _distanceSquaredToCamera, _maxParticleSystemSpawned, 0);
		ArrayUtils.GrowArrayIfNecessary(ref _sortedDrawTaskIds, _maxParticleSystemSpawned, 0);
	}

	public ParticleFXSystem(GraphicsDevice graphics, float engineTimeStep)
	{
		_graphics = graphics;
		_engineTimeStep = engineTimeStep;
		Initialize();
	}

	public void InitializeFunctions(UpdateSpawnerLightingFunc updateSpawnerLighting, UpdateParticleCollisionFunc updateCollision, InitParticleFunc initParticle)
	{
		_updateParticleSpawnerLightingFunc = updateSpawnerLighting;
		_updateParticleCollisionFunc = updateCollision;
		_initParticleFunc = initParticle;
	}

	public void DisposeFunctions()
	{
		_updateParticleSpawnerLightingFunc = null;
		_updateParticleCollisionFunc = null;
		_initParticleFunc = null;
	}

	public void Initialize()
	{
		MeshProcessor.CreateSphere(ref _debugSphereMesh, 5, 8, 1f, 0);
		InitMemory();
		InitParticleSystems();
	}

	protected override void DoDispose()
	{
		DisposeParticleSystems();
		DisposeMemory();
		_debugSphereMesh.Dispose();
	}

	private void InitParticleSystems()
	{
		_particleSystemProxies = new ParticleSystemProxy[50];
		_particleSystemProxyDistanceToCamera = new float[50];
		_sortedParticleSystemProxyIds = new ushort[50];
		_updateTasks = new UpdateTask[_maxParticleSystemSpawned];
		_drawTasks = new ParticleSystem[_maxParticleSystemSpawned];
		_distanceSquaredToCamera = new float[_maxParticleSystemSpawned];
		_sortedDrawTaskIds = new ushort[_maxParticleSystemSpawned];
		_expiredParticleSystemIds = new ConcurrentQueue<int>();
		_particleSystemDebugs = new Dictionary<int, ParticleSystemDebug>();
		_particleSystems = new Dictionary<int, ParticleSystem>();
	}

	private void DisposeParticleSystems()
	{
		foreach (ParticleSystem value in _particleSystems.Values)
		{
			value.Dispose();
		}
		foreach (ParticleSystemDebug value2 in _particleSystemDebugs.Values)
		{
			value2.Dispose();
		}
	}

	private void InitMemory()
	{
		_particleMemoryPool = new FXMemoryPool<ParticleBuffers>();
		_particleMemoryPool.Initialize(256000);
	}

	private void DisposeMemory()
	{
		_particleMemoryPool.Release();
		_particleMemoryPool = null;
	}

	public void BeginFrame()
	{
		_updateTaskCount = 0;
		_drawTaskCount = 0;
		_sortedDrawTaskCount = 0;
		ResetMapFXTaskCounters();
		ResetDrawCounters();
	}

	public bool TrySpawnDebugSystem(ParticleSystemSettings settings, Vector2 textureAltasInverseSize, out ParticleSystem particleSystem)
	{
		particleSystem = new ParticleSystem(this, _graphics.IsGPULowEnd, _updateParticleSpawnerLightingFunc, _updateParticleCollisionFunc, _initParticleFunc, textureAltasInverseSize, _nextParticleSystemId++, settings);
		return particleSystem.Initialize();
	}

	public bool TrySpawnParticleSystemProxy(ParticleSystemSettings settings, Vector2 textureAltasInverseSize, out ParticleSystemProxy particleSystemProxy, bool isLocalPlayer = false, bool isTracked = false)
	{
		particleSystemProxy = null;
		if (_particleSystemProxyCount == ushort.MaxValue)
		{
			return false;
		}
		ArrayUtils.GrowArrayIfNecessary(ref _particleSystemProxies, _particleSystemProxyCount + 1, 100);
		ArrayUtils.GrowArrayIfNecessary(ref _particleSystemProxyDistanceToCamera, _particleSystemProxyCount + 1, 100);
		ArrayUtils.GrowArrayIfNecessary(ref _sortedParticleSystemProxyIds, _particleSystemProxyCount + 1, 100);
		particleSystemProxy = new ParticleSystemProxy();
		particleSystemProxy.Settings = settings;
		particleSystemProxy.TextureAltasInverseSize = textureAltasInverseSize;
		particleSystemProxy.IsLocalPlayer = isLocalPlayer;
		particleSystemProxy.IsTracked = isTracked;
		_particleSystemProxies[_particleSystemProxyCount] = particleSystemProxy;
		_particleSystemProxyCount++;
		return true;
	}

	public void ClearParticleSystems()
	{
		foreach (ParticleSystem value in _particleSystems.Values)
		{
			value.Dispose();
		}
		_particleSystems.Clear();
		ClearParticleSystemDebugs();
		_nextParticleSystemId = 0;
	}

	public void ClearParticleSystemDebugs()
	{
		foreach (ParticleSystemDebug value in _particleSystemDebugs.Values)
		{
			value.Dispose();
		}
		_particleSystemDebugs.Clear();
	}

	private void ResetDrawCounters()
	{
		PreviousFrameErosionDrawCount = _spawnerErosionDrawCount;
		PreviousFrameDistortionDrawCount = _spawnerDistortionDrawCount;
		PreviousFrameBlendDrawCount = ParticleSpawnerDrawCount;
		_spawnerErosionDrawCount = 0;
		_spawnerDistortionDrawCount = 0;
		_spawnerLowResDrawCount = 0;
		_spawnerBlendDrawCount = 0;
		_spawnerFPVDrawCount = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterTask(ParticleSystem particleSystem, bool isVisible, float distanceSquared)
	{
		RegisterUpdateTask(particleSystem, isVisible);
		if (isVisible)
		{
			RegisterDrawTask(particleSystem, distanceSquared);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void RegisterUpdateTask(ParticleSystem particleSystem, bool isVisible)
	{
		_updateTasks[_updateTaskCount].ParticleSystem = particleSystem;
		_updateTasks[_updateTaskCount].IsVisible = isVisible;
		_updateTaskCount++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void RegisterDrawTask(ParticleSystem particleSystem, float distanceSquared)
	{
		_drawTasks[_drawTaskCount] = particleSystem;
		_drawTaskCount++;
		_distanceSquaredToCamera[_sortedDrawTaskCount] = distanceSquared;
		_sortedDrawTaskIds[_sortedDrawTaskCount] = _sortedDrawTaskCount;
		_sortedDrawTaskCount++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ParticleSystem CreateParticleSystem(ParticleSystemProxy proxy)
	{
		ParticleSystem particleSystem = new ParticleSystem(this, _graphics.IsGPULowEnd, _updateParticleSpawnerLightingFunc, _updateParticleCollisionFunc, _initParticleFunc, proxy.TextureAltasInverseSize, _nextParticleSystemId++, proxy.Settings);
		if (particleSystem.Initialize())
		{
			particleSystem.Proxy = proxy;
			particleSystem.Scale = proxy.Scale;
			particleSystem.DefaultColor = proxy.DefaultColor;
			particleSystem.IsOvergroundOnly = proxy.IsOvergroundOnly;
			particleSystem.Position = proxy.Position;
			particleSystem.Rotation = proxy.Rotation;
			particleSystem.SetFirstPerson(proxy.IsFirstPerson);
			_particleSystems.Add(particleSystem.Id, particleSystem);
		}
		return particleSystem;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void DeleteSystem(int systemId)
	{
		_particleSystems[systemId].Dispose();
		_particleSystems.Remove(systemId);
		if (_particleSystemDebugs.TryGetValue(systemId, out var value))
		{
			value.Dispose();
			_particleSystemDebugs.Remove(systemId);
		}
	}

	public void CleanDeadProxies()
	{
		int num = 0;
		while (num < _particleSystemProxyCount)
		{
			ParticleSystemProxy particleSystemProxy = _particleSystemProxies[num];
			if (particleSystemProxy.IsExpired || (!particleSystemProxy.IsTracked && particleSystemProxy.ParticleSystem != null && particleSystemProxy.ParticleSystem.IsExpired))
			{
				if (particleSystemProxy.ParticleSystem != null)
				{
					particleSystemProxy.ParticleSystem.Expire(particleSystemProxy.HasInstantExpire);
					particleSystemProxy.ParticleSystem = null;
				}
				_particleSystemProxyCount--;
				_particleSystemProxies[num] = _particleSystemProxies[_particleSystemProxyCount];
			}
			else
			{
				num++;
			}
		}
	}

	public void UpdateProxies(Vector3 cameraPosition, bool useProxyCheck)
	{
		for (int i = 0; i < _particleSystemProxyCount; i++)
		{
			ParticleSystemProxy particleSystemProxy = _particleSystemProxies[i];
			_particleSystemProxyDistanceToCamera[i] = (particleSystemProxy.IsLocalPlayer ? 0f : Vector3.DistanceSquared(cameraPosition, particleSystemProxy.Position));
			_sortedParticleSystemProxyIds[i] = (ushort)i;
		}
		Array.Sort(_particleSystemProxyDistanceToCamera, _sortedParticleSystemProxyIds, 0, _particleSystemProxyCount);
		int num = 0;
		for (int j = 0; j < _particleSystemProxyCount; j++)
		{
			ushort num2 = _sortedParticleSystemProxyIds[j];
			float num3 = _particleSystemProxyDistanceToCamera[j];
			ParticleSystemProxy particleSystemProxy2 = _particleSystemProxies[num2];
			if (particleSystemProxy2.ParticleSystem == null)
			{
				if (_particleSystems.Count == MaxParticleSystemSpawned)
				{
					num++;
				}
				else if (!useProxyCheck || num3 < particleSystemProxy2.Settings.CullDistanceSquared + 125f)
				{
					particleSystemProxy2.ParticleSystem = CreateParticleSystem(particleSystemProxy2);
				}
				else if (!particleSystemProxy2.IsTracked)
				{
					particleSystemProxy2.Expire(instant: true);
				}
				continue;
			}
			particleSystemProxy2.ParticleSystem.Position = particleSystemProxy2.Position;
			particleSystemProxy2.ParticleSystem.Rotation = particleSystemProxy2.Rotation;
			bool flag = j >= MaxParticleSystemSpawned && num > 0;
			if ((useProxyCheck && num3 > particleSystemProxy2.Settings.CullDistanceSquared + 125f) || flag)
			{
				particleSystemProxy2.ParticleSystem.Expire(instant: true);
				particleSystemProxy2.ParticleSystem = null;
				num--;
				if (!particleSystemProxy2.IsTracked)
				{
					particleSystemProxy2.Expire(instant: true);
				}
			}
		}
	}

	public void UpdateSimulationOnSingleCore(float deltaTime)
	{
		if (_isPaused)
		{
			return;
		}
		_accumulatedDeltaTime += deltaTime;
		if (_accumulatedDeltaTime < _engineTimeStep)
		{
			for (int i = 0; i < _updateTaskCount; i++)
			{
				ParticleSystem particleSystem = _updateTasks[i].ParticleSystem;
				if (_updateTasks[i].IsVisible || particleSystem.IsImportant)
				{
					particleSystem.LightUpdate();
				}
			}
			return;
		}
		deltaTime = MathHelper.Min(0.033f, _accumulatedDeltaTime);
		for (int j = 0; j < _updateTaskCount; j++)
		{
			ParticleSystem particleSystem2 = _updateTasks[j].ParticleSystem;
			if (_updateTasks[j].IsVisible || particleSystem2.IsImportant)
			{
				particleSystem2.Update(deltaTime);
				if (particleSystem2.IsExpired)
				{
					_expiredParticleSystemIds.Enqueue(particleSystem2.Id);
				}
			}
			else
			{
				particleSystem2.UpdateLife(deltaTime);
				if (particleSystem2.IsExpired)
				{
					_expiredParticleSystemIds.Enqueue(particleSystem2.Id);
				}
			}
		}
		int count = _expiredParticleSystemIds.Count;
		for (int k = 0; k < count; k++)
		{
			int result;
			bool flag = _expiredParticleSystemIds.TryDequeue(out result);
			DeleteSystem(result);
		}
		_accumulatedDeltaTime = 0f;
	}

	public void UpdateSimulationOnMultiCore(float deltaTime)
	{
		if (_isPaused)
		{
			return;
		}
		_accumulatedDeltaTime += deltaTime;
		if (_accumulatedDeltaTime < _engineTimeStep)
		{
			Parallel.For(0, _updateTaskCount, delegate(int i)
			{
				ParticleSystem particleSystem2 = _updateTasks[i].ParticleSystem;
				if (_updateTasks[i].IsVisible || particleSystem2.IsImportant)
				{
					particleSystem2.LightUpdate();
				}
			});
			return;
		}
		deltaTime = MathHelper.Min(0.033f, _accumulatedDeltaTime);
		Parallel.For(0, _updateTaskCount, delegate(int i)
		{
			ParticleSystem particleSystem = _updateTasks[i].ParticleSystem;
			if (_updateTasks[i].IsVisible || particleSystem.IsImportant)
			{
				particleSystem.Update(deltaTime);
				if (particleSystem.IsExpired)
				{
					_expiredParticleSystemIds.Enqueue(particleSystem.Id);
				}
			}
			else
			{
				particleSystem.UpdateLife(deltaTime);
				if (particleSystem.IsExpired)
				{
					_expiredParticleSystemIds.Enqueue(particleSystem.Id);
				}
			}
		});
		int count = _expiredParticleSystemIds.Count;
		for (int j = 0; j < count; j++)
		{
			int result;
			bool flag = _expiredParticleSystemIds.TryDequeue(out result);
			DeleteSystem(result);
		}
		_accumulatedDeltaTime = 0f;
	}

	public void UpdateSimulation(float deltaTime)
	{
		if (_useParallelExecution)
		{
			UpdateSimulationOnMultiCore(deltaTime);
		}
		else
		{
			UpdateSimulationOnSingleCore(deltaTime);
		}
	}

	public void DispatchSpawnersDrawTasks(bool sort = true)
	{
		if (sort)
		{
			Array.Sort(_distanceSquaredToCamera, _sortedDrawTaskIds, 0, _sortedDrawTaskCount);
		}
		int num = 0;
		for (int i = 0; i < _sortedDrawTaskCount; i++)
		{
			ushort num2 = _sortedDrawTaskIds[i];
			ParticleSystem particleSystem = _drawTasks[num2];
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			bool flag = true;
			for (int j = 0; j < particleSystem.AliveSpawnerCount; j++)
			{
				ParticleSpawner particleSpawner = particleSystem.SystemSpawners[j].ParticleSpawner;
				if (particleSpawner.ActiveParticles == 0)
				{
					continue;
				}
				num += particleSpawner.ParticleDrawCount;
				if (num < 20000)
				{
					if (particleSpawner.IsDistortion)
					{
						ArrayUtils.GrowArrayIfNecessary(ref _spawnerDistortionDraw, _spawnerDistortionDrawCount + 1, 50);
						_spawnerDistortionDraw[_spawnerDistortionDrawCount] = particleSpawner;
						_spawnerDistortionDrawCount++;
						num4++;
					}
					else if (particleSpawner.RenderMode == FXSystem.RenderMode.Erosion)
					{
						ArrayUtils.GrowArrayIfNecessary(ref _spawnerErosionDraw, _spawnerErosionDrawCount + 1, 100);
						_spawnerErosionDraw[_spawnerErosionDrawCount] = particleSpawner;
						_spawnerErosionDrawCount++;
						num3++;
					}
					else if (IsLowResRenderingEnabled && particleSpawner.IsLowRes)
					{
						FXSystem.RenderMode renderMode = particleSpawner.RenderMode;
						FXSystem.RenderMode renderMode2 = renderMode;
						if ((uint)renderMode2 <= 1u)
						{
							ArrayUtils.GrowArrayIfNecessary(ref _spawnerLowResDraw, _spawnerLowResDrawCount + 1, 200);
							_spawnerLowResDraw[_spawnerLowResDrawCount] = particleSpawner;
							_spawnerLowResDrawCount++;
							num5++;
						}
					}
					else if (!particleSpawner.IsFirstPerson)
					{
						FXSystem.RenderMode renderMode3 = particleSpawner.RenderMode;
						FXSystem.RenderMode renderMode4 = renderMode3;
						if ((uint)renderMode4 <= 1u)
						{
							ArrayUtils.GrowArrayIfNecessary(ref _spawnerBlendDraw, _spawnerBlendDrawCount + 1, 200);
							_spawnerBlendDraw[_spawnerBlendDrawCount] = particleSpawner;
							_spawnerBlendDrawCount++;
							num6++;
						}
					}
					else
					{
						ArrayUtils.GrowArrayIfNecessary(ref _spawnerFPVDraw, _spawnerFPVDrawCount + 1, 5);
						_spawnerFPVDraw[_spawnerFPVDrawCount] = particleSpawner;
						_spawnerFPVDrawCount++;
						num7++;
					}
					continue;
				}
				flag = false;
				break;
			}
			if (!flag)
			{
				_spawnerErosionDrawCount -= num3;
				_spawnerDistortionDrawCount -= num4;
				_spawnerLowResDrawCount -= num5;
				_spawnerBlendDrawCount -= num6;
				_spawnerFPVDrawCount -= num7;
				_sortedDrawTaskCount = (ushort)i;
				break;
			}
		}
		Debug.Assert(_spawnerErosionDrawCount >= 0, $"_spawnerErosionDrawCount ({_spawnerErosionDrawCount})should not be negative.");
		Debug.Assert(_spawnerDistortionDrawCount >= 0, $"_spawnerDistortionDrawCount ({_spawnerDistortionDrawCount})should not be negative.");
		Debug.Assert(_spawnerLowResDrawCount >= 0, $"_spawnerLowResDrawCount({_spawnerLowResDrawCount})should not be negative.");
		Debug.Assert(_spawnerBlendDrawCount >= 0, $"_spawnerBlendDrawCount ({_spawnerBlendDrawCount})should not be negative.");
		Debug.Assert(_spawnerFPVDrawCount >= 0, $"_spawnerFPVDrawCount ({_spawnerFPVDrawCount})should not be negative.");
		Debug.Assert(_sortedDrawTaskCount <= _drawTaskCount);
	}

	public void PrepareErosionVertexDataStorage(FXRenderer fXRenderer)
	{
		if (_spawnerErosionDrawCount != 0)
		{
			int num = 0;
			for (int i = 0; i < _spawnerErosionDrawCount; i++)
			{
				ushort drawId = fXRenderer.ReserveDrawTask();
				_spawnerErosionDraw[i].ReserveVertexDataStorage(ref fXRenderer.FXVertexBuffer, drawId);
				num += _spawnerErosionDraw[i].ParticleDrawCount;
			}
			fXRenderer.ErosionDrawParams.Count = num;
			fXRenderer.ErosionDrawParams.StartOffset = (uint)_spawnerErosionDraw[0].ParticleVertexDataStartIndex;
		}
	}

	public void PrepareBlendVertexDataStorage(FXRenderer fXRenderer)
	{
		if (_spawnerBlendDrawCount != 0)
		{
			int num = 0;
			for (int num2 = _spawnerBlendDrawCount - 1; num2 >= 0; num2--)
			{
				ushort drawId = fXRenderer.ReserveDrawTask();
				_spawnerBlendDraw[num2].ReserveVertexDataStorage(ref fXRenderer.FXVertexBuffer, drawId);
				num += _spawnerBlendDraw[num2].ParticleDrawCount;
			}
			fXRenderer.BlendDrawParams.Count = num;
			fXRenderer.BlendDrawParams.StartOffset = (uint)_spawnerBlendDraw[_spawnerBlendDrawCount - 1].ParticleVertexDataStartIndex;
			fXRenderer.BlendDrawParams.IsStartOffsetSet = true;
		}
	}

	public void PrepareFPVVertexDataStorage(FXRenderer fXRenderer)
	{
		if (_spawnerFPVDrawCount != 0)
		{
			int num = 0;
			for (int num2 = _spawnerFPVDrawCount - 1; num2 >= 0; num2--)
			{
				ushort drawId = fXRenderer.ReserveDrawTask();
				_spawnerFPVDraw[num2].ReserveVertexDataStorage(ref fXRenderer.FXVertexBuffer, drawId);
				num += _spawnerFPVDraw[num2].ParticleDrawCount;
			}
			fXRenderer.BlendFPVDrawParams.Count = num;
			fXRenderer.BlendFPVDrawParams.StartOffset = (uint)_spawnerFPVDraw[_spawnerFPVDrawCount - 1].ParticleVertexDataStartIndex;
			fXRenderer.BlendFPVDrawParams.IsStartOffsetSet = true;
		}
	}

	public void PrepareLowResVertexDataStorage(FXRenderer fXRenderer)
	{
		if (_spawnerLowResDrawCount != 0)
		{
			int num = 0;
			for (int num2 = _spawnerLowResDrawCount - 1; num2 >= 0; num2--)
			{
				ushort drawId = fXRenderer.ReserveDrawTask();
				_spawnerLowResDraw[num2].ReserveVertexDataStorage(ref fXRenderer.FXVertexBuffer, drawId);
				num += _spawnerLowResDraw[num2].ParticleDrawCount;
			}
			fXRenderer.BlendLowResDrawParams.Count = num;
			fXRenderer.BlendLowResDrawParams.StartOffset = (uint)_spawnerLowResDraw[_spawnerLowResDrawCount - 1].ParticleVertexDataStartIndex;
		}
	}

	public void PrepareDistortionVertexDataStorage(FXRenderer fXRenderer)
	{
		if (_spawnerDistortionDrawCount != 0)
		{
			int num = 0;
			for (int i = 0; i < _spawnerDistortionDrawCount; i++)
			{
				ushort drawId = fXRenderer.ReserveDrawTask();
				_spawnerDistortionDraw[i].ReserveVertexDataStorage(ref fXRenderer.FXVertexBuffer, drawId);
				num += _spawnerDistortionDraw[i].ParticleDrawCount;
			}
			fXRenderer.DistortionDrawParams.Count = num;
			fXRenderer.DistortionDrawParams.StartOffset = (uint)_spawnerDistortionDraw[0].ParticleVertexDataStartIndex;
			fXRenderer.DistortionDrawParams.IsStartOffsetSet = true;
		}
	}

	private void PrepareForDrawOnSingleCore(FXRenderer fXRenderer, Vector3 cameraPosition, IntPtr dataPtr)
	{
		for (int i = 0; i < _sortedDrawTaskCount; i++)
		{
			ushort num = _sortedDrawTaskIds[i];
			_drawTasks[num].PrepareForDraw(cameraPosition, ref fXRenderer.FXVertexBuffer, dataPtr);
		}
	}

	private void PrepareForDrawOnMultiCore(FXRenderer fXRenderer, Vector3 cameraPosition, IntPtr gpuDrawDataPtr)
	{
		Parallel.For(0, _sortedDrawTaskCount, delegate(int i)
		{
			ushort num = _sortedDrawTaskIds[i];
			_drawTasks[num].PrepareForDraw(cameraPosition, ref fXRenderer.FXVertexBuffer, gpuDrawDataPtr);
		});
	}

	public void PrepareForDraw(FXRenderer fXRenderer, Vector3 cameraPosition, IntPtr dataPtr)
	{
		if (_useParallelExecution)
		{
			PrepareForDrawOnMultiCore(fXRenderer, cameraPosition, dataPtr);
		}
		else
		{
			PrepareForDrawOnSingleCore(fXRenderer, cameraPosition, dataPtr);
		}
	}

	public void AddParticleSystemDebug(ParticleSystem particleSystem)
	{
		_particleSystemDebugs[particleSystem.Id] = new ParticleSystemDebug(_graphics, particleSystem);
	}

	public void DrawDebugInfo(ref Matrix viewRotationProjectionMatrix)
	{
		foreach (ParticleSystemDebug value in _particleSystemDebugs.Values)
		{
			value.Draw(viewRotationProjectionMatrix);
		}
	}

	public void DrawDebugBoundingVolumes(ref Vector3 cameraPosition, ref Matrix viewRotationProjectionMatrix)
	{
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_debugSphereMesh.VertexArray);
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		gL.UseProgram(basicProgram);
		basicProgram.Opacity.SetValue(1f);
		Vector3 vector = new Vector3(1f, 1f, 1f);
		Vector3 vector2 = new Vector3(0.65f, 0.65f, 0.65f);
		Vector3 vector3 = new Vector3(0.5f, 1f, 1f);
		Vector3 vector4 = new Vector3(0f, 1f, 0f);
		BoundingSphere boundingSphere = default(BoundingSphere);
		for (int i = 0; i < _particleSystemProxyCount; i++)
		{
			ParticleSystemProxy particleSystemProxy = _particleSystemProxies[i];
			float boundingRadius = particleSystemProxy.Settings.BoundingRadius;
			Vector3 vector5 = (boundingSphere.Center = particleSystemProxy.Position);
			boundingSphere.Radius = boundingRadius;
			vector5 -= cameraPosition;
			Matrix.CreateScale(boundingRadius, out var result);
			Matrix.AddTranslation(ref result, vector5.X, vector5.Y, vector5.Z);
			Matrix.Multiply(ref result, ref viewRotationProjectionMatrix, out result);
			if (particleSystemProxy.ParticleSystem == null)
			{
				vector = vector2;
			}
			else
			{
				ParticleSystem particleSystem = particleSystemProxy.ParticleSystem;
				if (particleSystem.IsFirstPerson)
				{
					continue;
				}
				float num = Vector3.DistanceSquared(cameraPosition, particleSystem.Position);
				vector = ((num < particleSystem.CullDistanceSquared) ? vector4 : vector3);
			}
			basicProgram.Color.SetValue(vector);
			basicProgram.MVPMatrix.SetValue(ref result);
			gL.DrawElements(GL.TRIANGLES, _debugSphereMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int RequestParticleBufferStorage(int count)
	{
		return _particleMemoryPool.TakeSlots(count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ReleaseParticleBufferStorage(int particleStartIndex, int particleCount)
	{
		_particleMemoryPool.ReleaseSlots(particleStartIndex, particleCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ClearParticleBufferStorage()
	{
		_particleMemoryPool.Clear();
	}

	public void ResetDrawStates()
	{
	}

	private void ResetMapFXTaskCounters()
	{
		_incomingMapParticlesTaskCount = 0;
		_animatedBlockParticleUpdateTaskCount = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PrepareForIncomingAnimatedBlockParticlesTasks(int size)
	{
		_incomingMapParticlesTaskCount += size;
		ArrayUtils.GrowArrayIfNecessary(ref _animatedBlockParticleUpdateTasks, _animatedBlockParticleUpdateTaskCount + size, 25);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterFXAnimatedBlockParticlesTask(AnimatedBlockRenderer animatedBlockRenderer, RenderedChunk.MapParticle mapParticle)
	{
		_animatedBlockParticleUpdateTasks[_animatedBlockParticleUpdateTaskCount].AnimatedBlockRenderer = animatedBlockRenderer;
		_animatedBlockParticleUpdateTasks[_animatedBlockParticleUpdateTaskCount].MapParticle = mapParticle;
		_animatedBlockParticleUpdateTaskCount++;
	}

	public void UpdateAnimatedBlockParticles()
	{
		for (int i = 0; i < _animatedBlockParticleUpdateTaskCount; i++)
		{
			RenderedChunk.MapParticle mapParticle = _animatedBlockParticleUpdateTasks[i].MapParticle;
			ref AnimatedRenderer.NodeTransform reference = ref _animatedBlockParticleUpdateTasks[i].AnimatedBlockRenderer.NodeTransforms[mapParticle.TargetNodeIndex];
			mapParticle.ParticleSystemProxy.Position = mapParticle.Position + Vector3.Transform(reference.Position * (1f / 32f) * mapParticle.BlockScale, _animatedBlockParticleUpdateTasks[i].MapParticle.Rotation) + Vector3.Transform(mapParticle.PositionOffset * mapParticle.BlockScale, _animatedBlockParticleUpdateTasks[i].MapParticle.Rotation * reference.Orientation);
			mapParticle.ParticleSystemProxy.Rotation = _animatedBlockParticleUpdateTasks[i].MapParticle.Rotation * reference.Orientation * mapParticle.RotationOffset;
		}
	}
}
