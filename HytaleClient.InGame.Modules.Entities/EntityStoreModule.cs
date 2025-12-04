#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using HytaleClient.Audio;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.Entities;
using HytaleClient.Graphics;
using HytaleClient.Graphics.BlockyModels;
using HytaleClient.Graphics.Fonts;
using HytaleClient.InGame.Modules.CharacterController;
using HytaleClient.Math;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.InGame.Modules.Entities;

internal class EntityStoreModule : Module
{
	public struct Setup
	{
		public bool LogicalLoDUpdate;

		public bool DrawLocalPlayerName;

		public bool DisplayDebugCommandsOnEntityEffect;

		public float DistanceToCameraBeforeRotation;

		public bool DebugUI;

		public static Setup CreateDefault()
		{
			Setup result = default(Setup);
			result.LogicalLoDUpdate = true;
			result.DrawLocalPlayerName = false;
			result.DisplayDebugCommandsOnEntityEffect = false;
			result.DistanceToCameraBeforeRotation = 64f;
			result.DebugUI = false;
			return result;
		}
	}

	public struct QueuedSoundEvent
	{
		public float Time;

		public uint EventIndex;

		public int NetworkId;

		public QueuedSoundEvent(uint eventIndex, int networkId)
		{
			Time = 1f;
			EventIndex = eventIndex;
			NetworkId = networkId;
		}
	}

	private struct FXUpdateTask
	{
		public Entity Entity;
	}

	public struct EntityLight
	{
		public readonly int EntityNetworkId;

		public LightData LightData;

		public bool VisibilityPrediction;

		public EntityLight(int entityNetworkId, Vector3 color)
		{
			EntityNetworkId = entityNetworkId;
			LightData.Color = color;
			LightData.Sphere.Center = Vector3.Zero;
			LightData.Sphere.Radius = 0f;
			VisibilityPrediction = true;
		}
	}

	private readonly ConcurrentDictionary<string, BlockyModel> _models = new ConcurrentDictionary<string, BlockyModel>();

	private readonly ConcurrentDictionary<string, BlockyAnimation> _animations = new ConcurrentDictionary<string, BlockyAnimation>();

	private readonly Queue<Tuple<string, string>> _modelsAndAnimationsToLoad = new Queue<Tuple<string, string>>();

	private Thread _thread;

	private CancellationTokenSource _threadCancellationTokenSource;

	private CancellationToken _threadCancellationToken;

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly List<QueuedSoundEvent> _queuedSoundEvents = new List<QueuedSoundEvent>();

	private readonly Dictionary<int, Entity> _entitiesByNetworkId = new Dictionary<int, Entity>();

	private readonly Dictionary<Guid, Entity> _entitiesByPredictionId = new Dictionary<Guid, Entity>();

	private const int EntitiesDefaultSize = 1000;

	private const int EntitiesGrowth = 500;

	private int _entitiesCount;

	private Entity[] _entities = new Entity[1000];

	private bool _needDeferredDespawn = false;

	private List<int> _deferredDespawnIds = new List<int>(10);

	private int _playerEntityLocalId;

	public int MountEntityLocalId = -1;

	private const int BoundingVolumesDefaultSize = 1000;

	private const int BoundingVolumesGrowth = 500;

	private BoundingSphere[] _boundingVolumes = new BoundingSphere[1000];

	private float[] _distancesToCamera = new float[1000];

	private Vector3[] _orientations = new Vector3[1000];

	private AudioDevice.SoundObjectReference[] _soundObjectReferences = new AudioDevice.SoundObjectReference[1000];

	public const int MaxClosestEntities = 20;

	private const int FXUpdateTasksDefaultSize = 200;

	private const int FXUpdateTasksGrowth = 100;

	private int _incomingFXUpdateTaskCount;

	private int _fxUpdateTaskCount;

	private FXUpdateTask[] _fxUpdateTasks = new FXUpdateTask[200];

	public readonly Dictionary<string, int> ModelVFXByIds = new Dictionary<string, int>();

	public ModelVFX[] ModelVFXs;

	public readonly Dictionary<string, int> EntityEffectIndicesByIds = new Dictionary<string, int>();

	public EntityEffect[] EntityEffects;

	private const int EntityLightsDefaultSize = 200;

	private const int EntityLightsGrowth = 100;

	public int EntityLightCount = 0;

	public EntityLight[] EntityLights = new EntityLight[200];

	public ushort[] SortedEntityLightIds = new ushort[200];

	public float[] SortedEntityLightCameraSquaredDistances = new float[200];

	public Setup CurrentSetup = Setup.CreateDefault();

	public readonly Texture TextureAtlas;

	public bool DebugInfoNeedsDrawing = false;

	public bool DebugInfoBounds = false;

	private int _nextLocalEntityId = -1;

	public NodeNameManager NodeNameManager;

	public int PlayerEntityLocalId => _playerEntityLocalId;

	public Vector3[] ClosestEntityPositions { get; private set; } = new Vector3[20];


	public Dictionary<string, Point> ImageLocations { get; private set; }

	public void SetupModelsAndAnimations()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		SuspendBackgroundLoadingThread();
		_models.Clear();
		_animations.Clear();
		_modelsAndAnimationsToLoad.Clear();
		foreach (KeyValuePair<string, string> item in _gameInstance.HashesByServerAssetPath)
		{
			if (IsAssetPathCharacterRelated(item.Key) && (item.Key.EndsWith(".blockymodel") || item.Key.EndsWith(".blockyanim")))
			{
				_modelsAndAnimationsToLoad.Enqueue(new Tuple<string, string>(item.Key, item.Value));
			}
		}
		ResumeBackgroundLoadingThread();
	}

	private void SuspendBackgroundLoadingThread()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (_thread != null)
		{
			_threadCancellationTokenSource.Cancel();
			_thread.Join();
			_thread = null;
			_threadCancellationTokenSource = null;
		}
	}

	public void ResumeBackgroundLoadingThread()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_threadCancellationTokenSource = new CancellationTokenSource();
		_threadCancellationToken = _threadCancellationTokenSource.Token;
		_thread = new Thread(BackgroundLoadingThreadStart)
		{
			Name = "EntityStoreModuleBackgroundLoading",
			IsBackground = true
		};
		_thread.Start();
	}

	private void BackgroundLoadingThreadStart()
	{
		while (!_threadCancellationToken.IsCancellationRequested && _modelsAndAnimationsToLoad.Count > 0)
		{
			Tuple<string, string> tuple = _modelsAndAnimationsToLoad.Dequeue();
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			if (item.EndsWith(".blockymodel"))
			{
				if (!_models.ContainsKey(item2))
				{
					BlockyModel blockyModel = new BlockyModel(BlockyModel.MaxNodeCount);
					try
					{
						BlockyModelInitializer.Parse(AssetManager.GetAssetUsingHash(item2), NodeNameManager, ref blockyModel);
					}
					catch (Exception ex)
					{
						_gameInstance.App.DevTools.Error("Failed to parse blocky model: " + item + ". See log for details.");
						Logger.Error<Exception>(ex);
						blockyModel = new BlockyModel(0);
					}
					_models[item2] = blockyModel;
				}
			}
			else if (!_animations.ContainsKey(item2))
			{
				BlockyAnimation blockyAnimation = new BlockyAnimation();
				try
				{
					BlockyAnimationInitializer.Parse(AssetManager.GetAssetUsingHash(item2), NodeNameManager, ref blockyAnimation);
				}
				catch (Exception ex2)
				{
					_gameInstance.App.DevTools.Error("Failed to parse blocky animation: " + item + ". See log for details.");
					Logger.Error<Exception>(ex2);
					blockyAnimation = new BlockyAnimation();
				}
				_animations[item2] = blockyAnimation;
			}
		}
	}

	public bool GetModel(string hash, out BlockyModel model)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		string assetLocalPathUsingHash = AssetManager.GetAssetLocalPathUsingHash(hash);
		try
		{
			model = _models.GetOrAdd(hash, delegate(string x)
			{
				BlockyModel blockyModel = new BlockyModel(BlockyModel.MaxNodeCount);
				BlockyModelInitializer.Parse(AssetManager.GetAssetUsingHash(x, allowFromMainThread: true), NodeNameManager, ref blockyModel);
				return blockyModel;
			});
		}
		catch (Exception ex)
		{
			_gameInstance.App.DevTools.Error("Failed to parse blocky model: " + assetLocalPathUsingHash + ". See log for details.");
			Logger.Error<Exception>(ex);
			model = null;
			return false;
		}
		return true;
	}

	public bool GetAnimation(string hash, out BlockyAnimation animation)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		string assetLocalPathUsingHash = AssetManager.GetAssetLocalPathUsingHash(hash);
		try
		{
			animation = _animations.GetOrAdd(hash, delegate(string x)
			{
				BlockyAnimation blockyAnimation = new BlockyAnimation();
				BlockyAnimationInitializer.Parse(AssetManager.GetAssetUsingHash(x, allowFromMainThread: true), NodeNameManager, ref blockyAnimation);
				return blockyAnimation;
			});
		}
		catch (Exception ex)
		{
			_gameInstance.App.DevTools.Error("Failed to parse blocky animation: " + assetLocalPathUsingHash + ". See log for details.");
			Logger.Error<Exception>(ex);
			animation = null;
			return false;
		}
		return true;
	}

	public Entity[] GetAllEntities()
	{
		return _entities;
	}

	public int GetEntitiesCount()
	{
		return _entitiesCount;
	}

	public BoundingSphere[] GetBoundingVolumes()
	{
		return _boundingVolumes;
	}

	public Vector3[] GetOrientations()
	{
		return _orientations;
	}

	public AudioDevice.SoundObjectReference[] GetSoundObjectReferences()
	{
		return _soundObjectReferences;
	}

	public void ExtractClosestEntityPositions(SceneView cameraSceneView, bool insertFirstPersonCamera, Vector3 firstPersonCameraPosition)
	{
		int num = 0;
		if (insertFirstPersonCamera)
		{
			ClosestEntityPositions[0] = firstPersonCameraPosition - cameraSceneView.Position;
			num = 1;
		}
		int num2 = System.Math.Min(20, cameraSceneView.EntitiesCount);
		for (int i = num; i < num2; i++)
		{
			int sortedEntityId = cameraSceneView.GetSortedEntityId(i - num);
			ClosestEntityPositions[i] = _boundingVolumes[sortedEntityId].Center - cameraSceneView.Position;
		}
		for (int j = num2; j < 20; j++)
		{
			ClosestEntityPositions[j] = new Vector3(1000f);
		}
	}

	public EntityStoreModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		TextureAtlas = new Texture(Texture.TextureTypes.Texture2D);
		TextureAtlas.CreateTexture2D(4096, 32, null, 5, GL.NEAREST_MIPMAP_NEAREST);
		NodeNameManager = NodeNameManager.Copy(gameInstance.App.CharacterPartStore.CharacterNodeNameManager);
	}

	protected override void DoDispose()
	{
		SuspendBackgroundLoadingThread();
		foreach (Entity value in _entitiesByNetworkId.Values)
		{
			value.Dispose();
		}
		TextureAtlas.Dispose();
	}

	public void BeginFrame()
	{
		ResetFXUpdateTaskCounters();
		ProcessDeferredDespawn();
		_needDeferredDespawn = false;
	}

	public void RegisterEntity(Entity entity)
	{
		_entitiesByNetworkId.Add(entity.NetworkId, entity);
		if (entity.PredictionId.HasValue)
		{
			_entitiesByPredictionId.Add(entity.PredictionId.Value, entity);
		}
	}

	public void UnregisterEntity(int networkId)
	{
		Entity entity = _entitiesByNetworkId[networkId];
		_entitiesByNetworkId.Remove(networkId);
		if (entity.PredictionId.HasValue)
		{
			_entitiesByPredictionId.Remove(entity.PredictionId.Value);
		}
		if (networkId < 0 && networkId >= _nextLocalEntityId)
		{
			_nextLocalEntityId = networkId + 1;
		}
	}

	public void MapPrediction(Guid predictionId, Entity entity)
	{
		if (_entitiesByPredictionId.TryGetValue(predictionId, out var value))
		{
			value.ServerEntity = entity;
			entity.BeenPredicted = true;
		}
	}

	public Entity GetEntity(int networkId)
	{
		_entitiesByNetworkId.TryGetValue(networkId, out var value);
		return value;
	}

	public int NextLocalEntityId()
	{
		int num = --_nextLocalEntityId;
		while (_entitiesByNetworkId.ContainsKey(num))
		{
			num = --_nextLocalEntityId;
		}
		return num;
	}

	public bool Spawn(int networkId, out Entity entity)
	{
		if (networkId == -1)
		{
			networkId = NextLocalEntityId();
		}
		entity = GetEntity(networkId);
		if (entity != null)
		{
			return false;
		}
		if (networkId == _gameInstance.LocalPlayerNetworkId)
		{
			entity = new PlayerEntity(_gameInstance, networkId);
			_gameInstance.SetLocalPlayer(entity as PlayerEntity);
		}
		else
		{
			entity = new Entity(_gameInstance, networkId);
		}
		RegisterEntity(entity);
		return true;
	}

	public List<Entity> GetEntitiesInBox(BoundingBox box)
	{
		List<Entity> list = new List<Entity>(32);
		for (int i = 0; i < _entitiesCount; i++)
		{
			Entity entity = _entities[i];
			if (box.Contains(entity.Position) == ContainmentType.Contains)
			{
				list.Add(entity);
			}
		}
		return list;
	}

	public List<Entity> GetEntitiesInSphere(Vector3 position, float radius)
	{
		List<Entity> list = new List<Entity>(32);
		float num = radius * radius;
		for (int i = 0; i < _entitiesCount; i++)
		{
			Entity entity = _entities[i];
			if (Intersects(position, radius, entity.Position, entity.Hitbox))
			{
				list.Add(entity);
			}
		}
		return list;
	}

	public bool Intersects(Vector3 center, float radius, Vector3 pos, BoundingBox box)
	{
		box = new BoundingBox(box.Min, box.Max);
		box.Translate(pos);
		float num = System.Math.Max(box.Min.X, System.Math.Min(center.X, box.Max.X));
		float num2 = System.Math.Max(box.Min.Y, System.Math.Min(center.Y, box.Max.Y));
		float num3 = System.Math.Max(box.Min.Z, System.Math.Min(center.Z, box.Max.Z));
		double num4 = System.Math.Sqrt((num - center.X) * (num - center.X) + (num2 - center.Y) * (num2 - center.Y) + (num3 - center.Z) * (num3 - center.Z));
		return num4 < (double)radius;
	}

	public void Despawn(int networkId)
	{
		if (_needDeferredDespawn)
		{
			_deferredDespawnIds.Add(networkId);
		}
		else
		{
			RemoveEntity(networkId);
		}
	}

	public void DespawnAll()
	{
		int[] array = _entitiesByNetworkId.Keys.ToArray();
		foreach (int networkId in array)
		{
			Despawn(networkId);
		}
	}

	private void RemoveEntity(int networkId)
	{
		Entity entity = GetEntity(networkId);
		if (entity != null)
		{
			UnregisterEntity(networkId);
			entity.Dispose();
			UnregisterEntityLight(networkId);
		}
	}

	private void ProcessDeferredDespawn()
	{
		foreach (int deferredDespawnId in _deferredDespawnIds)
		{
			RemoveEntity(deferredDespawnId);
		}
		_deferredDespawnIds.Clear();
	}

	public void UpdateEffects(int entityNetworkId, EntityEffectUpdate[] networkEffectIndexes)
	{
		GetEntity(entityNetworkId)?.UpdateEffectsFromServerPacket(networkEffectIndexes);
	}

	public void SetEntityLight(int entityNetworkId, Vector3? lightColor)
	{
		if (lightColor.HasValue)
		{
			RegisterEntityLight(entityNetworkId, lightColor.Value);
		}
		else
		{
			UnregisterEntityLight(entityNetworkId);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void RegisterEntityLight(int entityNetworkId, Vector3 lightColor)
	{
		if (TryGetEntityLightIndex(entityNetworkId, out var entityLightIndex))
		{
			EntityLights[entityLightIndex].LightData.Color = lightColor;
			return;
		}
		if (EntityLightCount == EntityLights.Length)
		{
			int newSize = EntityLightCount + 100;
			Array.Resize(ref EntityLights, newSize);
			Array.Resize(ref SortedEntityLightIds, newSize);
			Array.Resize(ref SortedEntityLightCameraSquaredDistances, newSize);
		}
		EntityLights[EntityLightCount] = new EntityLight(entityNetworkId, lightColor);
		EntityLightCount++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void UnregisterEntityLight(int entityNetworkId)
	{
		if (TryGetEntityLightIndex(entityNetworkId, out var entityLightIndex))
		{
			EntityLights[entityLightIndex] = EntityLights[EntityLightCount - 1];
			EntityLightCount--;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool TryGetEntityLightIndex(int entityNetworkId, out int entityLightIndex)
	{
		entityLightIndex = -1;
		for (int i = 0; i < EntityLightCount; i++)
		{
			if (EntityLights[i].EntityNetworkId == entityNetworkId)
			{
				entityLightIndex = i;
				return true;
			}
		}
		return false;
	}

	public void PrepareAtlas(out Dictionary<string, Point> upcomingImageLocations, out byte[][] upcomingAtlasPixelsPerLevel, CancellationToken cancellationToken)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		upcomingImageLocations = new Dictionary<string, Point>();
		Dictionary<string, PacketHandler.TextureInfo> dictionary = new Dictionary<string, PacketHandler.TextureInfo>();
		foreach (KeyValuePair<string, string> item in _gameInstance.HashesByServerAssetPath)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				upcomingAtlasPixelsPerLevel = null;
				return;
			}
			string key = item.Key;
			if (!key.EndsWith(".png") || key.EndsWith("-Icon.png") || !IsAssetPathCharacterRelated(key) || _gameInstance.App.CharacterPartStore.ImageLocations.ContainsKey(key))
			{
				continue;
			}
			string value = item.Value;
			if (dictionary.TryGetValue(value, out var value2))
			{
				continue;
			}
			value2 = new PacketHandler.TextureInfo
			{
				Checksum = value
			};
			string assetLocalPathUsingHash = AssetManager.GetAssetLocalPathUsingHash(value);
			if (Image.TryGetPngDimensions(assetLocalPathUsingHash, out value2.Width, out value2.Height))
			{
				dictionary[value] = value2;
				if (value2.Width % 32 != 0 || value2.Height % 32 != 0 || value2.Width < 32 || value2.Height < 32)
				{
					_gameInstance.App.DevTools.Warn($"Texture width/height must be a multiple of 32 and at least 32x32: {key} ({value2.Width}x{value2.Height})");
				}
				continue;
			}
			_gameInstance.App.DevTools.Error("Failed to get PNG dimensions for: " + key + ", " + assetLocalPathUsingHash + " (" + value + ")");
		}
		List<PacketHandler.TextureInfo> list = new List<PacketHandler.TextureInfo>(dictionary.Values);
		list.Sort((PacketHandler.TextureInfo a, PacketHandler.TextureInfo b) => b.Height.CompareTo(a.Height));
		Point zero = Point.Zero;
		int num = 0;
		int num2 = 512;
		foreach (PacketHandler.TextureInfo item2 in list)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				upcomingAtlasPixelsPerLevel = null;
				return;
			}
			if (zero.X + item2.Width > TextureAtlas.Width)
			{
				zero.X = 0;
				zero.Y = num;
			}
			while (zero.Y + item2.Height > num2)
			{
				num2 <<= 1;
			}
			upcomingImageLocations.Add(item2.Checksum, zero);
			num = System.Math.Max(num, zero.Y + item2.Height);
			zero.X += item2.Width;
		}
		byte[] array = new byte[TextureAtlas.Width * num2 * 4];
		zero = Point.Zero;
		foreach (PacketHandler.TextureInfo item3 in list)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				upcomingAtlasPixelsPerLevel = null;
				return;
			}
			try
			{
				Image image = new Image(AssetManager.GetAssetUsingHash(item3.Checksum));
				for (int i = 0; i < image.Height; i++)
				{
					Point point = upcomingImageLocations[item3.Checksum];
					int dstOffset = ((point.Y + i) * TextureAtlas.Width + point.X) * 4;
					Buffer.BlockCopy(image.Pixels, i * image.Width * 4, array, dstOffset, image.Width * 4);
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to load model texture: " + AssetManager.GetAssetLocalPathUsingHash(item3.Checksum));
			}
		}
		upcomingAtlasPixelsPerLevel = Texture.BuildMipmapPixels(array, TextureAtlas.Width, TextureAtlas.MipmapLevelCount);
	}

	public void CreateAtlasTexture(Dictionary<string, Point> imageLocations, byte[][] atlasPixelsPerLevel)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		ImageLocations = imageLocations;
		TextureAtlas.UpdateTexture2DMipMaps(atlasPixelsPerLevel);
	}

	public static bool IsAssetPathCharacterRelated(string assetPath)
	{
		return assetPath.StartsWith("Characters/") || assetPath.StartsWith("NPC/") || assetPath.StartsWith("NPCs/") || assetPath.StartsWith("Items/") || assetPath.StartsWith("Consumable/") || assetPath.StartsWith("Resources/") || assetPath.StartsWith("VFX/") || assetPath.StartsWith("Trailer/");
	}

	public void RebuildRenderers(bool itemOnly)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		for (int i = 0; i < _entitiesCount; i++)
		{
			_entities[i].RebuildRenderers(itemOnly);
		}
	}

	public void ResetMovementParticleSystems()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		for (int i = 0; i < _entitiesCount; i++)
		{
			_entities[i].ResetMovementParticleSystems();
		}
	}

	public void PrepareEntityEffects(EntityEffect[] entityEffects, out EntityEffect[] preparedEntityEffects)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		preparedEntityEffects = (EntityEffect[])(object)new EntityEffect[entityEffects.Length];
		for (int i = 0; i < entityEffects.Length; i++)
		{
			preparedEntityEffects[i] = new EntityEffect(entityEffects[i]);
		}
	}

	public void SetupEntityEffects(EntityEffect[] entityEffects)
	{
		EntityEffects = entityEffects;
		EntityEffectIndicesByIds.Clear();
		for (int i = 0; i < entityEffects.Length; i++)
		{
			EntityEffectIndicesByIds[entityEffects[i].Id] = i;
		}
	}

	public void PrepareModelVFXs(ModelVFX[] modelVFXs, out ModelVFX[] preparedModelVFXs)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		preparedModelVFXs = (ModelVFX[])(object)new ModelVFX[modelVFXs.Length];
		for (int i = 0; i < modelVFXs.Length; i++)
		{
			preparedModelVFXs[i] = new ModelVFX(modelVFXs[i]);
		}
	}

	public void SetupModelVFXs(ModelVFX[] modelVFXs)
	{
		ModelVFXs = modelVFXs;
		ModelVFXByIds.Clear();
		for (int i = 0; i < modelVFXs.Length; i++)
		{
			ModelVFXByIds[modelVFXs[i].Id] = i;
		}
	}

	public void PrepareEntities()
	{
		_entitiesCount = _entitiesByNetworkId.Count;
		ArrayUtils.GrowArrayIfNecessary(ref _entities, _entitiesCount, 500);
		int num = 0;
		foreach (Entity value in _entitiesByNetworkId.Values)
		{
			_entities[num] = value;
			num++;
		}
		_playerEntityLocalId = -1;
		for (num = 0; num < _entitiesCount; num++)
		{
			Entity entity = _entities[num];
			if (entity.NetworkId == _gameInstance.LocalPlayerNetworkId)
			{
				Entity entity2 = _entities[num];
				_entities[num] = _entities[0];
				_entities[0] = entity2;
				_playerEntityLocalId = 0;
				break;
			}
		}
		ArrayUtils.GrowArrayIfNecessary(ref _boundingVolumes, _entitiesCount, 500);
		ArrayUtils.GrowArrayIfNecessary(ref _distancesToCamera, _entitiesCount, 500);
		ArrayUtils.GrowArrayIfNecessary(ref _orientations, _entitiesCount, 500);
		ArrayUtils.GrowArrayIfNecessary(ref _soundObjectReferences, _entitiesCount, 500);
		_needDeferredDespawn = true;
	}

	public void PrepareLights(Vector3 cameraPosition)
	{
		SceneRenderer sceneRenderer = _gameInstance.SceneRenderer;
		int entityLightCount = EntityLightCount;
		if (entityLightCount > 0)
		{
			for (ushort num = 0; num < entityLightCount; num++)
			{
				ref EntityLight reference = ref EntityLights[num];
				Vector3 position = GetEntity(reference.EntityNetworkId).Position;
				reference.LightData.Sphere.Center = position;
				float radius = LightData.ComputeRadiusFromColor(reference.LightData.Color);
				reference.LightData.Sphere.Radius = radius;
				SortedEntityLightIds[num] = num;
				SortedEntityLightCameraSquaredDistances[num] = Vector3.DistanceSquared(position, cameraPosition);
			}
			Array.Sort(SortedEntityLightCameraSquaredDistances, SortedEntityLightIds, 0, entityLightCount);
			sceneRenderer.PrepareForIncomingOccludeeLight(entityLightCount);
			BoundingBox boundingBox = default(BoundingBox);
			for (int i = 0; i < entityLightCount; i++)
			{
				ushort num2 = SortedEntityLightIds[i];
				ref BoundingSphere sphere = ref EntityLights[num2].LightData.Sphere;
				Vector3 vector = sphere.Center - cameraPosition;
				float value = sphere.Radius * 0.8f;
				boundingBox.Min = vector - new Vector3(value);
				boundingBox.Max = vector + new Vector3(value);
				sceneRenderer.RegisterOccludeeLight(ref boundingBox);
			}
		}
	}

	public void GatherLights(ref BoundingFrustum viewFrustum, bool useOcclusionCullingForLights, int maxLightCount, ref LightData[] visibleLightData, out int visibleLightCount)
	{
		int num = 0;
		int num2 = System.Math.Min(EntityLightCount, maxLightCount);
		for (int i = 0; i < num2; i++)
		{
			ushort num3 = SortedEntityLightIds[i];
			ref EntityLight reference = ref EntityLights[num3];
			bool flag = viewFrustum.Contains(reference.LightData.Sphere) != ContainmentType.Disjoint;
			bool flag2 = !useOcclusionCullingForLights || reference.VisibilityPrediction;
			if (flag && flag2)
			{
				Entity entity = GetEntity(reference.EntityNetworkId);
				if (entity != null && entity.ShouldRender)
				{
					visibleLightData[num] = reference.LightData;
					num++;
				}
			}
		}
		visibleLightCount = num;
	}

	public void PreUpdate(float deltaTime)
	{
		Vector3 position = _gameInstance.CameraModule.Controller.Position;
		SceneRenderer sceneRenderer = _gameInstance.SceneRenderer;
		sceneRenderer.PrepareForIncomingOccludeeEntity(_entitiesCount);
		int serverUpdatesPerSecond = _gameInstance.ServerUpdatesPerSecond;
		for (int num = _queuedSoundEvents.Count - 1; num >= 0; num--)
		{
			QueuedSoundEvent value = _queuedSoundEvents[num];
			Entity entity = GetEntity(value.NetworkId);
			if (entity != null || value.Time-- <= 0f)
			{
				if (entity != null && entity.ShouldRender && !entity.ActiveSounds.Contains(value.EventIndex))
				{
					entity.ActiveSounds.Add(value.EventIndex);
					_gameInstance.AudioModule.PlaySoundEvent(value.EventIndex, entity.SoundObjectReference, ref entity.SoundEventReference);
				}
				_queuedSoundEvents.RemoveAt(num);
			}
			else
			{
				_queuedSoundEvents[num] = value;
			}
		}
		BoundingBox boundingBox = default(BoundingBox);
		for (int i = 0; i < _entitiesCount; i++)
		{
			Entity entity2 = _entities[i];
			if (i != _playerEntityLocalId && i != MountEntityLocalId)
			{
				entity2.UpdatePosition(deltaTime, serverUpdatesPerSecond);
			}
			_distancesToCamera[i] = Vector3.Distance(entity2.RenderPosition, position);
			float val = System.Math.Max(System.Math.Abs(entity2.Hitbox.Min.X), entity2.Hitbox.Max.X);
			float val2 = System.Math.Max(System.Math.Abs(entity2.Hitbox.Min.Z), entity2.Hitbox.Max.Z);
			float num2 = System.Math.Max(val, val2);
			float num3 = (entity2.Hitbox.Max.Y - entity2.Hitbox.Min.Y) * 0.5f;
			float val3 = num3 * 2.5f;
			val3 = System.Math.Max(num2, val3);
			_boundingVolumes[i].Radius = val3;
			_boundingVolumes[i].Center = entity2.Position + new Vector3(0f, num3, 0f);
			Vector3 vector = entity2.Position - position;
			boundingBox.Min.X = vector.X - num2;
			boundingBox.Min.Y = vector.Y - entity2.Hitbox.Min.Y;
			boundingBox.Min.Z = vector.Z - num2;
			boundingBox.Max.X = vector.X + num2;
			boundingBox.Max.Y = vector.Y + entity2.Hitbox.Max.Y;
			boundingBox.Max.Z = vector.Z + num2;
			sceneRenderer.RegisterOccludeeEntity(ref boundingBox);
		}
		if (_playerEntityLocalId != -1)
		{
			UpdateLocalPlayerEntity(deltaTime);
		}
	}

	public void Update(float deltaTime)
	{
		if (_entitiesCount > 0)
		{
			UpdateLocalEntities(deltaTime, (uint)(_playerEntityLocalId + 1), (uint)(_entitiesCount - 1));
		}
	}

	private void UpdateLocalEntities(float deltaTime, uint startLocalId, uint endLocalId)
	{
		uint frameCounter = _gameInstance.SceneRenderer.Data.FrameCounter;
		bool logicalLoDUpdate = CurrentSetup.LogicalLoDUpdate;
		Vector3 position = _gameInstance.CameraModule.Controller.Position;
		float num = CurrentSetup.DistanceToCameraBeforeRotation * CurrentSetup.DistanceToCameraBeforeRotation;
		for (uint num2 = startLocalId; num2 <= endLocalId; num2++)
		{
			Entity entity = _entities[num2];
			ref BoundingSphere reference = ref _boundingVolumes[num2];
			float distanceToCamera = _distancesToCamera[num2];
			int lod = ComputeLevelOfDetail(distanceToCamera, reference.Radius);
			bool flag = logicalLoDUpdate && !ShouldUpdateBasedOnLodValue(lod, entity.LastLogicUpdateFrameId, frameCounter);
			if (logicalLoDUpdate && !flag)
			{
				entity.LastLogicUpdateFrameId = frameCounter;
			}
			entity.UpdateWithoutPosition(deltaTime, distanceToCamera, flag);
			_soundObjectReferences[num2] = entity.SoundObjectReference;
			_orientations[num2] = entity.BodyOrientation;
			if (entity.Type == Entity.EntityType.Character)
			{
				float num3 = Vector3.DistanceSquared(entity.Position, position);
				if (num3 < num)
				{
					Vector3 bodyOrientation = entity.BodyOrientation;
					bodyOrientation.Yaw -= (float)System.Math.PI;
					Quaternion.CreateFromYawPitchRoll(bodyOrientation.Yaw, 0f - bodyOrientation.Pitch, 0f - bodyOrientation.Roll, out entity.RenderOrientation);
					Vector3 lookOrientation = entity.LookOrientation;
					lookOrientation.Yaw -= (float)System.Math.PI;
					Quaternion.CreateFromYawPitchRoll(lookOrientation.Yaw, 0f - lookOrientation.Pitch, 0f - lookOrientation.Roll, out var result);
					entity.ModelRenderer.SetCameraOrientation(Quaternion.Inverse(entity.RenderOrientation) * result);
				}
				entity.RenderPosition = entity.Position;
			}
			else if (entity.Type == Entity.EntityType.Item)
			{
				float num4 = 0f;
				float num5 = 0f;
				if (!entity.IsUsable() && !entity.IsLocalEntity && entity.ItemBase.DroppedItemAnimation == null)
				{
					num4 = ((float)System.Math.Cos(entity.ItemAnimationTime * 2.5f) + 1f) * 0.1f;
					num5 = MathHelper.WrapAngle(entity.ItemAnimationTime * 2f);
				}
				Vector3 bodyOrientation2 = entity.BodyOrientation;
				bodyOrientation2.Yaw -= -(float)System.Math.PI + num5;
				Quaternion.CreateFromYawPitchRoll(bodyOrientation2.Yaw, 0f - bodyOrientation2.Pitch, 0f - bodyOrientation2.Roll, out entity.RenderOrientation);
				entity.RenderPosition = entity.Position;
				entity.RenderPosition.Y += num4;
			}
			else if (entity.Type == Entity.EntityType.Block)
			{
				Vector3 lookOrientation2 = entity.LookOrientation;
				lookOrientation2.Yaw -= (float)System.Math.PI;
				Quaternion.CreateFromYawPitchRoll(lookOrientation2.Yaw, 0f - lookOrientation2.Pitch, 0f - lookOrientation2.Roll, out entity.RenderOrientation);
				entity.RenderPosition = entity.Position;
			}
		}
	}

	private void UpdateLocalPlayerEntity(float deltaTime)
	{
		uint frameCounter = _gameInstance.SceneRenderer.Data.FrameCounter;
		bool logicalLoDUpdate = CurrentSetup.LogicalLoDUpdate;
		Vector3 position = _gameInstance.CameraModule.Controller.Position;
		uint playerEntityLocalId = (uint)_playerEntityLocalId;
		Entity entity = _entities[playerEntityLocalId];
		ref BoundingSphere reference = ref _boundingVolumes[playerEntityLocalId];
		float distanceToCamera = _distancesToCamera[playerEntityLocalId];
		int lod = ComputeLevelOfDetail(distanceToCamera, reference.Radius);
		bool flag = logicalLoDUpdate && !ShouldUpdateBasedOnLodValue(lod, entity.LastLogicUpdateFrameId, frameCounter);
		if (logicalLoDUpdate && !flag)
		{
			entity.LastLogicUpdateFrameId = frameCounter;
		}
		entity.UpdateWithoutPosition(deltaTime, distanceToCamera, flag);
		_soundObjectReferences[playerEntityLocalId] = entity.SoundObjectReference;
		_orientations[playerEntityLocalId] = entity.BodyOrientation;
		if (!_gameInstance.CameraModule.Controller.IsFirstPerson)
		{
			float num = Vector3.DistanceSquared(entity.Position, position);
			if (num < 4096f)
			{
				Vector3 bodyOrientation = entity.BodyOrientation;
				bodyOrientation.Yaw -= (float)System.Math.PI;
				Quaternion.CreateFromYawPitchRoll(bodyOrientation.Yaw, 0f - bodyOrientation.Pitch, 0f - bodyOrientation.Roll, out entity.RenderOrientation);
				Vector3 lookOrientation = entity.LookOrientation;
				lookOrientation.Yaw -= (float)System.Math.PI;
				Quaternion.CreateFromYawPitchRoll(lookOrientation.Yaw, 0f - lookOrientation.Pitch, 0f - lookOrientation.Roll, out var result);
				entity.ModelRenderer.SetCameraOrientation(Quaternion.Inverse(entity.RenderOrientation) * result);
			}
		}
	}

	public void ProcessFrustumCulling(SceneView cameraSceneView, SceneView sunSceneView)
	{
		if (sunSceneView != null)
		{
			ArrayUtils.GrowArrayIfNecessary(ref cameraSceneView.EntitiesFrustumCullingResults, _entitiesCount, 500);
			ArrayUtils.GrowArrayIfNecessary(ref sunSceneView.EntitiesFrustumCullingResults, _entitiesCount, 500);
			if (sunSceneView.UseKDopForCulling)
			{
				for (int i = 0; i < _entitiesCount; i++)
				{
					cameraSceneView.Frustum.Intersects(ref _boundingVolumes[i], out cameraSceneView.EntitiesFrustumCullingResults[i]);
					BoundingSphere volume = _boundingVolumes[i];
					volume.Center -= cameraSceneView.Position;
					sunSceneView.EntitiesFrustumCullingResults[i] = sunSceneView.KDopFrustum.Intersects(volume);
				}
			}
			else
			{
				for (int j = 0; j < _entitiesCount; j++)
				{
					cameraSceneView.Frustum.Intersects(ref _boundingVolumes[j], out cameraSceneView.EntitiesFrustumCullingResults[j]);
					BoundingSphere sphere = _boundingVolumes[j];
					sphere.Center -= cameraSceneView.Position;
					sunSceneView.Frustum.Intersects(ref sphere, out sunSceneView.EntitiesFrustumCullingResults[j]);
				}
			}
		}
		else
		{
			ArrayUtils.GrowArrayIfNecessary(ref cameraSceneView.EntitiesFrustumCullingResults, _entitiesCount, 500);
			for (int k = 0; k < _entitiesCount; k++)
			{
				cameraSceneView.Frustum.Intersects(ref _boundingVolumes[k], out cameraSceneView.EntitiesFrustumCullingResults[k]);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ComputeLevelOfDetail(float distanceToCamera, float boundingRadius)
	{
		float num = MathHelper.Clamp(2.6f / boundingRadius, 0.1f, 2f);
		float num2 = (distanceToCamera - boundingRadius) * num;
		if (num2 < 48f)
		{
			return 0;
		}
		if (num2 < 96f)
		{
			return 1;
		}
		if (num2 < 160f)
		{
			return 2;
		}
		return 3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool ShouldUpdateBasedOnLodValue(int lod, uint lastUpdateFrameId, uint currentFrameId)
	{
		int num = 1 << lod;
		return (lod != 3 && (currentFrameId - lastUpdateFrameId) % num == 0) || currentFrameId < lastUpdateFrameId;
	}

	public void GatherRenderableEntities(SceneView cameraSceneView, SceneView sunSceneView, Vector3 sunLightDirection, bool useAnimLOD, bool cullUndergroundShadowCasters, bool cullSmallShadowCasters)
	{
		AnimationSystem animationSystem = _gameInstance.Engine.AnimationSystem;
		int num = 0;
		int num2 = 0;
		cameraSceneView.PrepareForIncomingEntities(_entitiesCount);
		sunSceneView?.PrepareForIncomingEntities(_entitiesCount);
		int num3 = 0;
		if (_playerEntityLocalId != -1)
		{
			int playerEntityLocalId = _playerEntityLocalId;
			bool flag = cameraSceneView.EntitiesFrustumCullingResults[playerEntityLocalId];
			bool flag2 = sunSceneView != null && sunSceneView.EntitiesFrustumCullingResults[playerEntityLocalId];
			if ((flag || flag2) && !_gameInstance.LocalPlayer.FirstPersonViewNeedsDrawing())
			{
				Entity entity = _entities[playerEntityLocalId];
				if (entity.ModelRenderer != null && entity.ShouldRender)
				{
					bool flag3 = entity.HasFX();
					for (int i = 0; i < entity.EntityItems.Count; i++)
					{
						flag3 = flag3 || entity.EntityItems[i].HasFX();
					}
					if (flag && flag3)
					{
						ArrayUtils.GrowArrayIfNecessary(ref _fxUpdateTasks, _fxUpdateTaskCount + 1, 100);
						_fxUpdateTasks[_fxUpdateTaskCount].Entity = entity;
						_fxUpdateTaskCount++;
					}
					if (!_gameInstance.CameraModule.Controller.IsFirstPerson)
					{
						if (flag)
						{
							cameraSceneView.RegisterEntity(playerEntityLocalId, entity.Position);
							num += 1 + entity.EntityEffects.Length + entity.EntityItems.Count;
						}
						if (flag2)
						{
							sunSceneView.RegisterEntity(playerEntityLocalId, entity.Position);
							num2 += 1 + entity.EntityItems.Count;
						}
					}
				}
			}
			num3 = 1;
		}
		uint frameCounter = _gameInstance.FrameCounter;
		for (int j = num3; j < _entitiesCount; j++)
		{
			bool flag4 = cameraSceneView.EntitiesFrustumCullingResults[j];
			bool flag5 = sunSceneView != null && sunSceneView.EntitiesFrustumCullingResults[j];
			bool flag6 = flag4 || flag5;
			Entity entity2 = _entities[j];
			if (!_gameInstance.RenderPlayers && entity2.PlayerSkin != null)
			{
				continue;
			}
			ref BoundingSphere reference = ref _boundingVolumes[j];
			float distanceToCamera = _distancesToCamera[j];
			int num4 = ComputeLevelOfDetail(distanceToCamera, reference.Radius);
			bool flag7 = useAnimLOD && !ShouldUpdateBasedOnLodValue(num4, entity2.LastAnimationUpdateFrameId, frameCounter);
			if (useAnimLOD && !flag7)
			{
				entity2.LastAnimationUpdateFrameId = frameCounter;
			}
			bool flag8 = entity2.HasFX();
			animationSystem.PrepareForIncomingTasks(1 + entity2.EntityItems.Count + entity2.EntityEffects.Length);
			for (int k = 0; k < entity2.EntityItems.Count; k++)
			{
				animationSystem.RegisterAnimationTask(entity2.EntityItems[k].ModelRenderer, flag7);
				flag8 = flag8 || entity2.EntityItems[k].HasFX();
			}
			if (flag8)
			{
				ArrayUtils.GrowArrayIfNecessary(ref _fxUpdateTasks, _fxUpdateTaskCount + 1, 100);
				_fxUpdateTasks[_fxUpdateTaskCount].Entity = entity2;
				_fxUpdateTaskCount++;
			}
			if (!flag6 || entity2.ModelRenderer == null || !entity2.ShouldRender)
			{
				continue;
			}
			Vector3 position = entity2.Position;
			int chunkX = (int)System.Math.Floor(position.X) >> 5;
			int chunkY = (int)System.Math.Floor(position.Y) >> 5;
			int chunkZ = (int)System.Math.Floor(position.Z) >> 5;
			if (!_gameInstance.MapModule.IsChunkReadyForDraw(chunkX, chunkY, chunkZ))
			{
				continue;
			}
			Vector3 intersection;
			bool flag9 = CascadedShadowMapping.ComputeIntersection(new Plane(Vector3.Up, 0f), reference.Center - cameraSceneView.Position, sunLightDirection, out intersection);
			float num5 = intersection.Length() - reference.Radius;
			bool flag10 = flag4 || (flag9 && num5 > 32f);
			flag7 = flag7 || (flag10 && !entity2.VisibilityPrediction);
			animationSystem.RegisterAnimationTask(entity2.ModelRenderer, flag7);
			bool flag11 = cullUndergroundShadowCasters && entity2.BlockLightColor.W * 15f <= 2f;
			bool flag12 = cullSmallShadowCasters && num4 >= 2;
			flag5 = flag5 && !flag11 && !flag12;
			for (int l = 0; l < entity2.EntityEffects.Length; l++)
			{
				ref Entity.UniqueEntityEffect reference2 = ref entity2.EntityEffects[l];
				if (reference2.ModelRenderer != null)
				{
					animationSystem.RegisterAnimationTask(reference2.ModelRenderer, flag7);
				}
			}
			if (flag4)
			{
				cameraSceneView.RegisterEntity(j, entity2.Position);
				num += 1 + entity2.EntityEffects.Length + entity2.EntityItems.Count;
			}
			if (flag5)
			{
				sunSceneView.RegisterEntity(j, entity2.Position);
				num2 += 1 + entity2.EntityItems.Count;
			}
		}
		cameraSceneView.IncomingEntityDrawTaskCount = num;
		if (sunSceneView != null)
		{
			sunSceneView.IncomingEntityDrawTaskCount = num2;
		}
	}

	public void PrepareForDraw(SceneView cameraSceneView, ref Matrix viewMatrix, ref Matrix projectionMatrix, ref Matrix viewProjectionMatrix)
	{
		if (!_gameInstance.Engine.AnimationSystem.HasProcessed)
		{
			throw new InvalidOperationException();
		}
		AnimationSystem animationSystem = _gameInstance.Engine.AnimationSystem;
		float scale = 0.175f / (float)_gameInstance.App.Fonts.DefaultFontFamily.RegularFont.BaseSize;
		int spread = _gameInstance.App.Fonts.DefaultFontFamily.RegularFont.Spread;
		float num = 1f / (float)spread;
		_gameInstance.SceneRenderer.PrepareForIncomingEntityDrawTasks(cameraSceneView.IncomingEntityDrawTaskCount);
		bool isHudVisible = _gameInstance.App.InGame.IsHudVisible;
		for (int i = 0; i < cameraSceneView.EntitiesCount; i++)
		{
			int sortedEntityId = cameraSceneView.GetSortedEntityId(i);
			ref Entity reference = ref _entities[sortedEntityId];
			float num2 = _distancesToCamera[sortedEntityId];
			if (reference.ModelRenderer == null || !reference.ShouldRender)
			{
				throw new Exception("Entity with no ModelRenderer added to Render list!");
			}
			float scale2 = 1f / 64f * reference.Scale;
			Vector3 translation = reference.RenderPosition - cameraSceneView.Position;
			Matrix.Compose(scale2, reference.RenderOrientation, translation, out var result);
			float modelHeight = reference.Hitbox.Max.Y * 64f;
			reference.ModelVFX.IdInTBO = _gameInstance.SceneRenderer.RegisterModelVFXTask(reference.ModelVFX.AnimationProgress, reference.ModelVFX.HighlightColor, reference.ModelVFX.HighlightThickness, reference.ModelVFX.NoiseScale, reference.ModelVFX.NoiseScrollSpeed, reference.ModelVFX.PackedModelVFXParams, reference.ModelVFX.PostColor);
			ModelRenderer modelRenderer = reference.ModelRenderer;
			Matrix result2;
			switch (reference.Type)
			{
			case Entity.EntityType.Item:
			case Entity.EntityType.Block:
				if (modelRenderer.NodeCount > 0)
				{
					_gameInstance.SceneRenderer.RegisterEntityDrawTasks(sortedEntityId, ref result, modelRenderer.VertexArray, modelRenderer.IndicesCount, animationSystem.NodeBuffer, modelRenderer.NodeBufferOffset, modelRenderer.NodeCount, reference.BlockLightColor, reference.BottomTint, reference.TopTint, modelHeight, reference.UseDithering, reference.ModelVFX.AnimationProgress, reference.ModelVFX.PackedModelVFXParams, reference.ModelVFX.IdInTBO);
				}
				break;
			case Entity.EntityType.Character:
			{
				if (modelRenderer.NodeCount > 0)
				{
					_gameInstance.SceneRenderer.RegisterEntityDrawTasks(sortedEntityId, ref result, modelRenderer.VertexArray, modelRenderer.IndicesCount, animationSystem.NodeBuffer, modelRenderer.NodeBufferOffset, modelRenderer.NodeCount, reference.BlockLightColor, reference.BottomTint, reference.TopTint, modelHeight, reference.UseDithering, reference.ModelVFX.AnimationProgress, reference.ModelVFX.PackedModelVFXParams, reference.ModelVFX.IdInTBO);
				}
				for (int j = 0; j < reference.EntityEffects.Length; j++)
				{
					ref Entity.UniqueEntityEffect reference2 = ref reference.EntityEffects[j];
					if (reference2.ModelRenderer != null && reference2.ModelRenderer.NodeCount != 0)
					{
						_gameInstance.SceneRenderer.RegisterEntityDrawTasks(sortedEntityId, ref result, reference2.ModelRenderer.VertexArray, reference2.ModelRenderer.IndicesCount, animationSystem.NodeBuffer, reference2.ModelRenderer.NodeBufferOffset, reference2.ModelRenderer.NodeCount, reference.BlockLightColor, Vector3.Zero, Vector3.Zero, modelHeight, reference.UseDithering, reference.ModelVFX.AnimationProgress, reference.ModelVFX.PackedModelVFXParams, reference.ModelVFX.IdInTBO);
					}
				}
				for (int k = 0; k < reference.EntityItems.Count; k++)
				{
					Entity.EntityItem entityItem = reference.EntityItems[k];
					if (entityItem.ModelRenderer != null && entityItem.ModelRenderer.NodeCount != 0)
					{
						ClientModelVFX clientModelVFX = ((entityItem.ModelVFX.Id != null) ? entityItem.ModelVFX : reference.ModelVFX);
						ref AnimatedRenderer.NodeTransform reference3 = ref modelRenderer.NodeTransforms[entityItem.TargetNodeIndex];
						Matrix.Compose(reference3.Orientation, reference3.Position, out result2);
						Matrix.Multiply(ref entityItem.RootOffsetMatrix, ref result2, out result2);
						Matrix.Multiply(ref result2, ref result, out result2);
						Matrix.ApplyScale(ref result2, entityItem.Scale);
						_gameInstance.SceneRenderer.RegisterEntityDrawTasks(sortedEntityId, ref result2, entityItem.ModelRenderer.VertexArray, entityItem.ModelRenderer.IndicesCount, animationSystem.NodeBuffer, entityItem.ModelRenderer.NodeBufferOffset, entityItem.ModelRenderer.NodeCount, reference.BlockLightColor, Vector3.Zero, Vector3.Zero, modelHeight, reference.UseDithering, clientModelVFX.AnimationProgress, clientModelVFX.PackedModelVFXParams, clientModelVFX.IdInTBO);
						if (entityItem.ModelVFX.Id != null)
						{
							entityItem.ModelVFX.IdInTBO = _gameInstance.SceneRenderer.RegisterModelVFXTask(clientModelVFX.AnimationProgress, clientModelVFX.HighlightColor, clientModelVFX.HighlightThickness, clientModelVFX.NoiseScale, clientModelVFX.NoiseScrollSpeed, clientModelVFX.PackedModelVFXParams, clientModelVFX.PostColor);
						}
					}
				}
				if (isHudVisible && reference.NetworkId != _gameInstance.LocalPlayerNetworkId)
				{
					int[] uIComponents = reference.UIComponents;
					if (uIComponents != null && uIComponents.Length != 0)
					{
						SceneRenderer.SceneData data = _gameInstance.SceneRenderer.Data;
						float x = data.ViewportSize.X;
						float y = data.ViewportSize.Y;
						Vector3 position = reference.Position;
						position.Y += reference.Hitbox.Max.Y;
						Vector2 vector = Vector3.WorldToScreenPos(ref data.ViewProjectionMatrix, x, y, position);
						Matrix.CreateTranslation(vector.X - x / 2f, 0f - (vector.Y - y / 2f), 0f, out var result3);
						_gameInstance.App.Interface.InGameView.RegisterEntityUIDrawTasks(ref result3, reference, num2);
					}
				}
				break;
			}
			default:
				throw new NotImplementedException("Unknown entity type");
			}
			if (isHudVisible && reference.NameplateTextRenderer != null && num2 < 64f && (CurrentSetup.DrawLocalPlayerName || reference.NetworkId != _gameInstance.LocalPlayerNetworkId))
			{
				float fillBlurThreshold = MathHelper.Clamp(2f * num2 * 0.1f, 1f, spread) * num;
				Matrix.CreateTranslation(0f - reference.NameplateTextRenderer.GetHorizontalOffset(TextRenderer.TextAlignment.Center), 0f - reference.NameplateTextRenderer.GetVerticalOffset(TextRenderer.TextVerticalAlignment.Middle), 0f, out result);
				Matrix.CreateScale(scale, out result2);
				Matrix.Multiply(ref result, ref result2, out result);
				Vector3 rotation = _gameInstance.CameraModule.Controller.Rotation;
				Matrix.CreateFromYawPitchRoll(rotation.Y, rotation.X, 0f, out result2);
				Matrix.Multiply(ref result, ref result2, out result);
				translation = reference.RenderPosition;
				translation.Y += reference.Hitbox.Max.Y + 0.5f;
				Matrix.AddTranslation(ref result, translation.X, translation.Y, translation.Z);
				Matrix.Multiply(ref result, ref viewProjectionMatrix, out result);
				_gameInstance.SceneRenderer.RegisterEntityNameplateDrawTask(sortedEntityId, ref result, translation - cameraSceneView.Position, fillBlurThreshold, reference.NameplateTextRenderer.VertexArray, reference.NameplateTextRenderer.IndicesCount);
			}
		}
	}

	public void PrepareForShadowMapDraw(SceneView sunSceneView)
	{
		if (!_gameInstance.Engine.AnimationSystem.HasProcessed)
		{
			throw new InvalidOperationException();
		}
		AnimationSystem animationSystem = _gameInstance.Engine.AnimationSystem;
		Vector3 cameraPosition = _gameInstance.SceneRenderer.Data.CameraPosition;
		_gameInstance.SceneRenderer.PrepareForIncomingEntitySunShadowCasterDrawTasks(sunSceneView.IncomingEntityDrawTaskCount);
		for (int i = 0; i < sunSceneView.EntitiesCount; i++)
		{
			int sortedEntityId = sunSceneView.GetSortedEntityId(i);
			ref Entity reference = ref _entities[sortedEntityId];
			if (reference.ModelRenderer == null)
			{
				throw new Exception("Entity with no ModelRenderer added to Render list!");
			}
			if (reference.ModelRenderer != null && reference.ModelRenderer.NodeCount == 0)
			{
				continue;
			}
			BoundingSphere boundingSphere = _boundingVolumes[sortedEntityId];
			boundingSphere.Center -= cameraPosition;
			float num = 0.98f;
			float scale = 1f / 64f * reference.Scale * num;
			Vector3 translation = reference.RenderPosition - cameraPosition;
			Matrix.Compose(scale, reference.RenderOrientation, translation, out var result);
			float modelHeight = reference.Hitbox.Max.Y * 64f;
			ModelRenderer modelRenderer = reference.ModelRenderer;
			switch (reference.Type)
			{
			case Entity.EntityType.Item:
			case Entity.EntityType.Block:
				_gameInstance.SceneRenderer.RegisterEntitySunShadowCasterDrawTask(ref boundingSphere, ref result, modelRenderer.VertexArray, modelRenderer.IndicesCount, animationSystem.NodeBuffer, modelRenderer.NodeBufferOffset, modelRenderer.NodeCount, modelHeight, reference.ModelVFX.AnimationProgress, reference.ModelVFX.IdInTBO);
				break;
			case Entity.EntityType.Character:
			{
				_gameInstance.SceneRenderer.RegisterEntitySunShadowCasterDrawTask(ref boundingSphere, ref result, modelRenderer.VertexArray, modelRenderer.IndicesCount, animationSystem.NodeBuffer, modelRenderer.NodeBufferOffset, modelRenderer.NodeCount, modelHeight, reference.ModelVFX.AnimationProgress, reference.ModelVFX.IdInTBO);
				for (int j = 0; j < reference.EntityItems.Count; j++)
				{
					Entity.EntityItem entityItem = reference.EntityItems[j];
					ref AnimatedRenderer.NodeTransform reference2 = ref modelRenderer.NodeTransforms[entityItem.TargetNodeIndex];
					Matrix.Compose(reference2.Orientation, reference2.Position, out var result2);
					Matrix.Multiply(ref entityItem.RootOffsetMatrix, ref result2, out result2);
					Matrix.Multiply(ref result2, ref result, out result2);
					Matrix.ApplyScale(ref result2, entityItem.Scale);
					_gameInstance.SceneRenderer.RegisterEntitySunShadowCasterDrawTask(ref boundingSphere, ref result2, entityItem.ModelRenderer.VertexArray, entityItem.ModelRenderer.IndicesCount, animationSystem.NodeBuffer, entityItem.ModelRenderer.NodeBufferOffset, entityItem.ModelRenderer.NodeCount, modelHeight, reference.ModelVFX.AnimationProgress, reference.ModelVFX.IdInTBO);
				}
				break;
			}
			default:
				throw new NotImplementedException("Unknown entity type");
			}
		}
	}

	public void PrepareDebugInfoForDraw(SceneView sceneView, ref Matrix viewProjectionMatrix)
	{
		CharacterControllerModule characterControllerModule = _gameInstance.CharacterControllerModule;
		Matrix result = default(Matrix);
		Matrix result2 = default(Matrix);
		Matrix result3 = default(Matrix);
		for (int i = 0; i < sceneView.EntitiesCount; i++)
		{
			int sortedEntityId = sceneView.GetSortedEntityId(i);
			ref Entity reference = ref _entities[sortedEntityId];
			ref BoundingSphere reference2 = ref _boundingVolumes[sortedEntityId];
			float distanceToCamera = _distancesToCamera[sortedEntityId];
			bool flag = reference.NetworkId == _gameInstance.LocalPlayerNetworkId;
			if (flag && _gameInstance.CameraModule.Controller.IsFirstPerson)
			{
				continue;
			}
			bool hit = _gameInstance.InteractionModule.TargetEntityHit == reference;
			Matrix result4;
			Matrix result6;
			if (reference.LastPush != Vector2.Zero)
			{
				Vector3 position = reference.Position;
				position.Y += reference.Hitbox.GetSize().Y / 2f;
				float scale = reference.LastPush.Length() * 2f;
				Matrix.CreateScale(scale, out result4);
				Vector3 source = Vector3.Zero;
				Vector3 destination = new Vector3(reference.LastPush.X, 0f, reference.LastPush.Y);
				Quaternion.CreateFromVectors(ref source, ref destination, out var _);
				Matrix.CreateFromYawPitchRoll((float)System.Math.Atan2(destination.X, destination.Z) - (float)System.Math.PI / 2f, 0f, 0f, out result6);
				Matrix.Multiply(ref result4, ref result6, out result4);
				Matrix.CreateTranslation(ref position, out result6);
				Matrix.Multiply(ref result4, ref result6, out result4);
				Matrix.Multiply(ref result4, ref viewProjectionMatrix, out result);
			}
			Vector3 position2 = reference.Position;
			position2.Y += reference.EyeOffset - 0.001f;
			if (!flag)
			{
				position2.Y += (reference.ServerMovementStates.IsCrouching ? reference.CrouchOffset : 0f);
			}
			else
			{
				position2.Y += characterControllerModule.MovementController.AutoJumpHeightShift + characterControllerModule.MovementController.CrouchHeightShift;
			}
			float scale2 = MathHelper.Max(reference.Hitbox.GetSize().X, reference.Hitbox.GetSize().Z) / 2f + 0.75f;
			Matrix.CreateScale(scale2, out result4);
			Matrix.CreateFromYawPitchRoll(reference.LookOrientation.Yaw + (float)System.Math.PI / 2f, 0f, reference.LookOrientation.Pitch, out result6);
			Matrix.Multiply(ref result4, ref result6, out result4);
			Matrix.CreateTranslation(ref position2, out result6);
			Matrix.Multiply(ref result4, ref result6, out result4);
			Matrix.Multiply(ref result4, ref viewProjectionMatrix, out var result7);
			BoundingBox hitbox = reference.Hitbox;
			hitbox.Max.Y = 0.002f;
			Vector3 position3 = hitbox.Min + position2;
			Vector3 scales = hitbox.GetSize() / Vector3.One;
			Matrix.CreateScale(ref scales, out result4);
			Matrix.CreateTranslation(ref position3, out result6);
			Matrix.Multiply(ref result4, ref result6, out result4);
			Matrix.Multiply(ref result4, ref viewProjectionMatrix, out var result8);
			position3 = reference.Hitbox.Min + reference.Position;
			position3.Y += 0.001f;
			scales = reference.Hitbox.GetSize() / Vector3.One;
			Matrix.CreateScale(ref scales, out result4);
			Matrix.CreateTranslation(ref position3, out result6);
			Matrix.Multiply(ref result4, ref result6, out result4);
			Matrix.Multiply(ref result4, ref viewProjectionMatrix, out var result9);
			Matrix result10;
			if (DebugInfoBounds)
			{
				scales = new Vector3(reference2.Radius);
				position3 = reference2.Center;
				Matrix.CreateScale(ref scales, out result4);
				Matrix.CreateTranslation(ref position3, out result6);
				Matrix.Multiply(ref result4, ref result6, out result4);
				Matrix.Multiply(ref result4, ref viewProjectionMatrix, out result10);
			}
			else
			{
				result10 = default(Matrix);
			}
			HashSet<int> collidedEntities = _gameInstance.CharacterControllerModule.MovementController.CollidedEntities;
			bool flag2 = collidedEntities.Contains(reference.NetworkId);
			bool flag3 = (!_gameInstance.DebugCollisionOnlyCollided || flag2) && reference.HitboxCollisionConfigIndex != -1;
			if (flag3)
			{
				Vector3 entityHitboxExpand = _gameInstance.CharacterControllerModule.MovementController.EntityHitboxExpand;
				BoundingBox hitbox2 = reference.Hitbox;
				hitbox2.Grow(entityHitboxExpand * 2f);
				position3 = hitbox2.Min + reference.Position;
				scales = hitbox2.GetSize() / Vector3.One;
				Matrix.CreateScale(ref scales, out result4);
				Matrix.CreateTranslation(ref position3, out result6);
				Matrix.Multiply(ref result4, ref result6, out result4);
				Matrix.Multiply(ref result4, ref viewProjectionMatrix, out result3);
				if (reference.RepulsionConfigIndex != -1)
				{
					position3 = reference.Position + new Vector3(0f, reference.Hitbox.GetSize().Y / 2f, 0f);
					ClientRepulsionConfig clientRepulsionConfig = _gameInstance.ServerSettings.RepulsionConfigs[reference.RepulsionConfigIndex];
					scales = new Vector3(clientRepulsionConfig.Radius, 0f, clientRepulsionConfig.Radius);
					scales.Y = reference.Hitbox.GetSize().Y / 2f;
					Matrix.CreateScale(ref scales, out result4);
					Matrix.CreateTranslation(ref position3, out result6);
					Matrix.Multiply(ref result4, ref result6, out result4);
					Matrix.Multiply(ref result4, ref viewProjectionMatrix, out result2);
				}
			}
			int levelOfDetail = ComputeLevelOfDetail(distanceToCamera, reference2.Radius);
			SceneRenderer.DebugInfoDetailTask[] array = null;
			if (reference.DetailBoundingBoxes.Count > 0)
			{
				int num = 0;
				foreach (Entity.DetailBoundingBox[] value2 in reference.DetailBoundingBoxes.Values)
				{
					num += value2.Length;
				}
				array = new SceneRenderer.DebugInfoDetailTask[num];
				Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, reference.BodyOrientation.Y);
				int num2 = 0;
				foreach (KeyValuePair<string, Entity.DetailBoundingBox[]> detailBoundingBox2 in reference.DetailBoundingBoxes)
				{
					float h = System.Math.Abs((float)detailBoundingBox2.Key.GetHashCode() / 2.1474836E+09f);
					ColorHsla colorHsla = new ColorHsla(h, 0.83f, 0.77f);
					Vector3 color = default(Vector3);
					colorHsla.ToRgb(out color.X, out color.Y, out color.Z);
					Entity.DetailBoundingBox[] value = detailBoundingBox2.Value;
					for (int j = 0; j < value.Length; j++)
					{
						Entity.DetailBoundingBox detailBoundingBox = value[j];
						position3 = detailBoundingBox.Offset;
						Vector3.Transform(ref position3, ref rotation, out position3);
						position3 += reference.Position + detailBoundingBox.Box.Min;
						position3.Y += 0.001f;
						BoundingBox box = detailBoundingBox.Box;
						scales = box.GetSize() / Vector3.One;
						Matrix.CreateScale(ref scales, out result4);
						Matrix.CreateTranslation(ref position3, out result6);
						Matrix.Multiply(ref result4, ref result6, out result4);
						Matrix.Multiply(ref result4, ref viewProjectionMatrix, out var result11);
						array[num2++] = new SceneRenderer.DebugInfoDetailTask
						{
							Color = color,
							Matrix = result11
						};
					}
				}
			}
			_gameInstance.SceneRenderer.RegisterEntityDebugDrawTask(hit, flag3, flag2, levelOfDetail, ref result7, ref result8, ref result9, ref result10, ref result3, ref result2, ref result, array);
		}
	}

	public void UpdateVisibilityPrediction(int[] occludeesVisibility, int entityResultsOffset, int entityResultsCount, int lightResultsOffset, int lightResultsCount, bool updateEntities, bool updateLights)
	{
		bool flag = !updateEntities;
		bool flag2 = !updateLights;
		for (int i = 0; i < entityResultsCount; i++)
		{
			bool visibilityPrediction = flag || occludeesVisibility[entityResultsOffset + i] == 1;
			_entities[i].VisibilityPrediction = visibilityPrediction;
		}
		for (int j = 0; j < lightResultsCount; j++)
		{
			bool visibilityPrediction2 = flag2 || occludeesVisibility[lightResultsOffset + j] == 1;
			ushort num = SortedEntityLightIds[j];
			EntityLights[num].VisibilityPrediction = visibilityPrediction2;
		}
	}

	public void ProcessFXUpdateTasks()
	{
		if (!_gameInstance.Engine.AnimationSystem.HasProcessed)
		{
			throw new InvalidOperationException();
		}
		for (int i = 0; i < _fxUpdateTaskCount; i++)
		{
			_fxUpdateTasks[i].Entity.UpdateFX();
		}
	}

	private void ResetFXUpdateTaskCounters()
	{
		_incomingFXUpdateTaskCount = 0;
		_fxUpdateTaskCount = 0;
	}

	public void QueueSoundEvent(uint soundEventIndex, int networkId)
	{
		_queuedSoundEvents.Add(new QueuedSoundEvent(soundEventIndex, networkId));
	}
}
