#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.FX;
using HytaleClient.Data.Map;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Particles;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using SDL2;

namespace HytaleClient.InGame.Modules.Particles;

internal class ParticleSystemStoreModule : Module
{
	public class DebugParticleSystemProxy
	{
		public string SystemId;

		public Vector3 Position;

		public bool UseDebug;

		public ParticleSystemProxy ParticleSystemProxy;

		public bool NeedDebugRefreshing;
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public bool FrustumCheck = true;

	public bool DistanceCheck = true;

	public bool ProxyCheck = true;

	private Dictionary<string, ParticleSystemSettings> _systemSettingsById = new Dictionary<string, ParticleSystemSettings>();

	private Dictionary<string, ParticleSpawnerSettings> _spawnerSettingsById = new Dictionary<string, ParticleSpawnerSettings>();

	private Dictionary<string, ParticleSettings> _particlesByFile;

	private bool _resettingParticles;

	private static readonly HitDetection.RaycastOptions RaycastOptions = new HitDetection.RaycastOptions
	{
		IgnoreEmptyCollisionMaterial = true
	};

	public readonly Dictionary<int, DebugParticleSystemProxy> DebugParticleSytemProxiesById = new Dictionary<int, DebugParticleSystemProxy>();

	private int _nextDebugId = 0;

	public int SystemCount => _gameInstance.Engine.FXSystem.Particles.ParticleSystemCount;

	public int MaxSpawnedSystems => _gameInstance.Engine.FXSystem.Particles.MaxParticleSystemSpawned;

	public void SetMaxSpawnedSystems(int max)
	{
		_gameInstance.Engine.FXSystem.Particles.SetMaxParticleSystemSpawned(max);
	}

	public ParticleSystemStoreModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_gameInstance.Engine.FXSystem.Particles.InitializeFunctions(UpdateParticleSpawnerLighting, UpdateParticleCollision, InitParticle);
	}

	protected override void DoDispose()
	{
		_gameInstance.Engine.FXSystem.Particles.DisposeFunctions();
		Clear();
	}

	private void UpdateParticleSpawnerLighting(ParticleSpawner particleSpawnerInstance)
	{
		if (particleSpawnerInstance.LightInfluence > 0f && !_gameInstance.MapModule.Disposed)
		{
			Vector4 one = Vector4.One;
			one = Vector4.Lerp(value2: (!particleSpawnerInstance.IsOvergroundOnly) ? _gameInstance.MapModule.GetLightColorAtBlockPosition((int)System.Math.Floor(particleSpawnerInstance.Position.X), (int)System.Math.Floor(particleSpawnerInstance.Position.Y), (int)System.Math.Floor(particleSpawnerInstance.Position.Z)) : new Vector4(_gameInstance.WeatherModule.SunlightColor * _gameInstance.WeatherModule.SunLight, 1f), value1: Vector4.One, amount: particleSpawnerInstance.LightInfluence);
			particleSpawnerInstance.UpdateLight(one);
		}
	}

	private void UpdateParticleCollision(ParticleSpawner particleSpawner, ref ParticleBuffers.ParticleSimulationData particleData0, ref ParticleBuffers.ParticleRenderData particleData1, ref Vector2 particleScale, ref ParticleBuffers.ParticleLifeData particleLife, Vector3 previousPosition, Quaternion inverseRotation)
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Invalid comparison between Unknown and I4
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Invalid comparison between Unknown and I4
		Vector3 vector = particleSpawner.Position + Vector3.Transform(particleData1.Position, particleSpawner.Rotation);
		int worldX = (int)System.Math.Floor(vector.X);
		int worldY = (int)System.Math.Floor(vector.Y);
		int worldZ = (int)System.Math.Floor(vector.Z);
		int block = _gameInstance.MapModule.GetBlock(worldX, worldY, worldZ, 0);
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
		bool flag = (float)(int)clientBlockType.VerticalFill * (1f / (float)(int)clientBlockType.MaxFillLevel) > vector.Y % 1f;
		if ((!flag || ((particleSpawner.ParticleCollisionBlockType != ParticleFXSystem.ParticleCollisionBlockType.All || (int)_gameInstance.MapModule.ClientBlockTypes[block].CollisionMaterial == 0) && (particleSpawner.ParticleCollisionBlockType != ParticleFXSystem.ParticleCollisionBlockType.Solid || (int)_gameInstance.MapModule.ClientBlockTypes[block].CollisionMaterial != 1))) && ((flag && (!flag || ((int)_gameInstance.MapModule.ClientBlockTypes[block].CollisionMaterial != 0 && (int)_gameInstance.MapModule.ClientBlockTypes[block].CollisionMaterial != 1))) || particleSpawner.ParticleCollisionBlockType != ParticleFXSystem.ParticleCollisionBlockType.Air))
		{
			return;
		}
		BitUtils.SwitchOnBit(2, ref particleData1.BoolData);
		if (particleSpawner.ParticleCollisionAction == ParticleFXSystem.ParticleCollisionAction.Expire || particleLife.LifeSpanTimer == particleLife.LifeSpan)
		{
			particleLife.LifeSpanTimer = 0.0001f;
			particleScale = Vector2.Zero;
			return;
		}
		RaycastOptions.Distance = Vector3.Distance(particleData1.Position, previousPosition);
		Vector3 vector2 = Vector3.Transform(particleData1.Position, particleSpawner.Rotation) - Vector3.Transform(previousPosition, particleSpawner.Rotation);
		if (RaycastOptions.Distance != 0f && _gameInstance.HitDetection.RaycastBlock(particleSpawner.Position + Vector3.Transform(previousPosition, particleSpawner.Rotation), vector2, RaycastOptions, out var raycastHit))
		{
			particleData1.Position = Vector3.Transform(raycastHit.HitPosition - particleSpawner.Position, inverseRotation) - Vector3.Normalize(vector2) * 0.05f;
		}
	}

	private bool InitParticle(ParticleSpawner particleSpawner, ref Vector3 particlePosition)
	{
		if (particleSpawner.IsOvergroundOnly)
		{
			Vector3 vector = particleSpawner.Position + particlePosition;
			int num = (int)System.Math.Floor(vector.X);
			int num2 = (int)System.Math.Floor(vector.Z);
			int num3 = num >> 5;
			int num4 = num2 >> 5;
			ChunkColumn chunkColumn = _gameInstance.MapModule.GetChunkColumn(num3, num4);
			if (chunkColumn != null)
			{
				int num5 = num - num3 * 32;
				int num6 = num2 - num4 * 32;
				int num7 = (num6 << 5) + num5;
				if (vector.Y < (float)(chunkColumn.Heights[num7] + 1))
				{
					return false;
				}
			}
		}
		return true;
	}

	public void PrepareParticles(Dictionary<string, ParticleSpawner> networkParticleSpawners, out Dictionary<string, ParticleSettings> upcomingParticles, out Dictionary<string, PacketHandler.TextureInfo> upcomingTextureInfo, out List<string> upcomingUVMotionTexturePaths, CancellationToken cancellationToken)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		Debug.Assert(!ThreadHelper.IsMainThread());
		upcomingParticles = new Dictionary<string, ParticleSettings>();
		upcomingTextureInfo = new Dictionary<string, PacketHandler.TextureInfo>();
		upcomingUVMotionTexturePaths = new List<string>(32);
		int width = 0;
		int height = 0;
		foreach (ParticleSpawner value3 in networkParticleSpawners.Values)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			if (value3.Particle_ == null)
			{
				continue;
			}
			ParticleSettings clientParticle = new ParticleSettings();
			ParticleCollision particleCollision_ = value3.ParticleCollision_;
			ParticleRotationInfluence collisionRotationInfluence = (ParticleRotationInfluence)((particleCollision_ != null) ? ((int)particleCollision_.ParticleRotationInfluence_) : 0);
			ParticleProtocolInitializer.Initialize(value3.Particle_, value3.ParticleRotationInfluence_, collisionRotationInfluence, ref clientParticle);
			if (clientParticle.TexturePath == null || !_gameInstance.HashesByServerAssetPath.TryGetValue(clientParticle.TexturePath, out var value))
			{
				_gameInstance.App.DevTools.Error("Missing particle texture: " + clientParticle.TexturePath + " for particle " + value3.Id);
				continue;
			}
			if (!upcomingTextureInfo.TryGetValue(value, out var value2))
			{
				value2 = new PacketHandler.TextureInfo
				{
					Checksum = value
				};
				string assetLocalPathUsingHash = AssetManager.GetAssetLocalPathUsingHash(value);
				if (Image.TryGetPngDimensions(assetLocalPathUsingHash, out value2.Width, out value2.Height))
				{
					upcomingTextureInfo[value] = value2;
					if (value2.Width % 32 != 0 || value2.Height % 32 != 0 || value2.Width < 32 || value2.Height < 32)
					{
						_gameInstance.App.DevTools.Warn($"Texture width/height must be a multiple of 32 and at least 32x32: {clientParticle.TexturePath} ({value2.Width}x{value2.Height})");
					}
				}
				else
				{
					_gameInstance.App.DevTools.Error("Failed to get PNG dimensions for: " + clientParticle.TexturePath + ", " + assetLocalPathUsingHash + " (" + value + ")");
				}
			}
			if (value3.UvMotion_ != null && value3.UvMotion_.Texture != null)
			{
				if (!Image.TryGetPngDimensions(Path.Combine(Paths.BuiltInAssets, "Common", value3.UvMotion_.Texture), out width, out height))
				{
					_gameInstance.App.DevTools.Error("Missing particle UV motion texture: " + Path.Combine(Paths.BuiltInAssets, "Common", value3.UvMotion_.Texture) + " for particle " + value3.Id);
				}
				else if (width != 64 || height != 64)
				{
					_gameInstance.App.DevTools.Error($"UV motion exture width/height must be 64x64: {value3.UvMotion_.Texture} ({width}x{height})");
				}
				else if (!upcomingUVMotionTexturePaths.Contains(value3.UvMotion_.Texture))
				{
					upcomingUVMotionTexturePaths.Add(value3.UvMotion_.Texture);
				}
			}
			upcomingParticles[value3.Id] = clientParticle;
		}
	}

	public void SetupParticleSystems(Dictionary<string, ParticleSystem> networkParticleSystems)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, ParticleSystem> networkParticleSystem in networkParticleSystems)
		{
			try
			{
				ParticleSystemSettings clientParticleSystem = new ParticleSystemSettings();
				ParticleProtocolInitializer.Initialize(networkParticleSystem.Value, ref clientParticleSystem);
				int num = 0;
				while (num < clientParticleSystem.SystemSpawnerCount)
				{
					ParticleSystemSettings.SystemSpawnerSettings systemSpawnerSettings = clientParticleSystem.SystemSpawnerSettingsList[num];
					if (LoadParticleSpawnerSettings(systemSpawnerSettings.ParticleSpawnerId, out var spawnerSettings))
					{
						systemSpawnerSettings.ParticleSpawnerSettings = spawnerSettings;
						num++;
					}
					else
					{
						clientParticleSystem.DeleteSpawnerSettings((byte)num);
					}
				}
				if (clientParticleSystem.SystemSpawnerCount == 0)
				{
					_gameInstance.App.DevTools.Error("No valid spawner settings listed in: " + networkParticleSystem.Key);
					list.Add(networkParticleSystem.Key);
				}
				_systemSettingsById[networkParticleSystem.Key] = clientParticleSystem;
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to setup particle system! {0}:", new object[1] { networkParticleSystem.Key });
				_gameInstance.App.DevTools.Error("Error failed to setup particle system: " + networkParticleSystem.Key);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			_systemSettingsById.Remove(list[i]);
		}
	}

	public void RemoveParticleSystems(Dictionary<string, ParticleSystem> networkParticleSystems)
	{
		foreach (string key in networkParticleSystems.Keys)
		{
			_systemSettingsById.Remove(key);
		}
	}

	public void SetupParticleSpawners(Dictionary<string, ParticleSystem> networkParticleSystems, Dictionary<string, ParticleSpawner> networkParticleSpawners, Dictionary<string, ParticleSettings> upcomingParticles, List<string> upcomingUVMotionTexturePaths)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_particlesByFile = upcomingParticles;
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, ParticleSpawner> networkParticleSpawner in networkParticleSpawners)
		{
			try
			{
				ParticleSpawnerSettings clientParticleSpawner = new ParticleSpawnerSettings();
				ParticleProtocolInitializer.Initialize(networkParticleSpawner.Value, ref clientParticleSpawner);
				if (!_particlesByFile.TryGetValue(networkParticleSpawner.Value.Id, out var value))
				{
					_gameInstance.App.DevTools.Error("Could not load particle settings for spawner " + networkParticleSpawner.Key);
					list.Add(networkParticleSpawner.Key);
				}
				else
				{
					if (value.TexturePath == null || !_gameInstance.HashesByServerAssetPath.ContainsKey(value.TexturePath))
					{
						_gameInstance.App.DevTools.Error("Failed to find particle texture: " + value.TexturePath + " for spawner " + networkParticleSpawner.Key);
						list.Add(networkParticleSpawner.Key);
					}
					clientParticleSpawner.ParticleSettings = value;
					if (clientParticleSpawner.UVMotion.TexturePath != null && upcomingUVMotionTexturePaths.Contains(clientParticleSpawner.UVMotion.TexturePath))
					{
						clientParticleSpawner.UVMotion.TextureId = upcomingUVMotionTexturePaths.IndexOf(clientParticleSpawner.UVMotion.TexturePath);
					}
					else
					{
						clientParticleSpawner.UVMotion.TextureId = -1;
					}
				}
				_spawnerSettingsById[networkParticleSpawner.Key] = clientParticleSpawner;
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to setup particle spawner! {0}:", new object[1] { networkParticleSpawner.Key });
				_gameInstance.App.DevTools.Error("Error failed to setup particle spawner: " + networkParticleSpawner.Key);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			_spawnerSettingsById.Remove(list[i]);
		}
		SetupParticleSystems(networkParticleSystems);
	}

	public void RemoveParticleSpawners(Dictionary<string, ParticleSystem> networkParticleSystems, Dictionary<string, ParticleSpawner> networkParticleSpawners, Dictionary<string, ParticleSettings> upcomingParticles)
	{
		_particlesByFile = upcomingParticles;
		foreach (string key in networkParticleSpawners.Keys)
		{
			_spawnerSettingsById.Remove(key);
		}
		SetupParticleSystems(networkParticleSystems);
	}

	public void UpdateTextures()
	{
		foreach (ParticleSettings value3 in _particlesByFile.Values)
		{
			if (!_gameInstance.HashesByServerAssetPath.TryGetValue(value3.TexturePath, out var value) || !_gameInstance.FXModule.ImageLocations.TryGetValue(value, out var value2))
			{
				_gameInstance.App.DevTools.Error("Missing particle texture: " + value3.TexturePath + " for particle " + value3.Id);
			}
			else
			{
				value3.ImageLocation = value2;
			}
		}
	}

	public void ResetParticleSystems(bool skipEntities = false)
	{
		ResetDebugParticleSystems();
		if (!skipEntities)
		{
			_gameInstance.EntityStoreModule.RebuildRenderers(itemOnly: false);
		}
		_gameInstance.EntityStoreModule.ResetMovementParticleSystems();
		_gameInstance.WeatherModule.ResetParticleSystems();
		_gameInstance.MapModule.ResetParticleSystems();
	}

	public bool CheckSettingsExist(string systemId)
	{
		bool flag = _systemSettingsById.ContainsKey(systemId);
		if (!flag)
		{
			_gameInstance.App.DevTools.Error("Could not find particle system settings: " + systemId);
		}
		return flag;
	}

	private bool LoadParticleSpawnerSettings(string spawnerId, out ParticleSpawnerSettings spawnerSettings)
	{
		if (!_spawnerSettingsById.TryGetValue(spawnerId, out spawnerSettings))
		{
			_gameInstance.App.DevTools.Error("Could not find particle spawner settings " + spawnerId);
			return false;
		}
		return true;
	}

	public void ClearDebug()
	{
		_gameInstance.Engine.FXSystem.Particles.ClearParticleSystemDebugs();
	}

	public void Clear()
	{
		_gameInstance.Engine.FXSystem.Particles.ClearParticleSystems();
	}

	public bool TrySpawnBlockSystem(Vector3 position, ClientBlockType blockType, ClientBlockParticleEvent particleEvent, out ParticleSystemProxy particleSystemProxy, bool faceCameraYaw = false, bool isTracked = false)
	{
		particleSystemProxy = null;
		if (blockType.BlockParticleSetId == null)
		{
			return false;
		}
		if (_gameInstance.ServerSettings.BlockParticleSets.TryGetValue(blockType.BlockParticleSetId, out var value) && value.ParticleSystemIds.TryGetValue(particleEvent, out var value2) && TrySpawnSystem(value2, out particleSystemProxy, isLocalPlayer: false, isTracked))
		{
			if (!blockType.ParticleColor.IsTransparent)
			{
				particleSystemProxy.DefaultColor = blockType.ParticleColor;
			}
			else if (blockType.BiomeTintMultipliersBySide[0] != 0f)
			{
				Vector3 blockEnvironmentTint = _gameInstance.MapModule.GetBlockEnvironmentTint((int)System.Math.Floor(position.X), (int)System.Math.Floor(position.Y), (int)System.Math.Floor(position.Z), blockType);
				particleSystemProxy.DefaultColor = UInt32Color.FromRGBA((byte)blockEnvironmentTint.X, (byte)blockEnvironmentTint.Y, (byte)blockEnvironmentTint.Z, byte.MaxValue);
			}
			else if (blockType.FluidFXIndex != 0)
			{
				Vector3 blockFluidTint = _gameInstance.MapModule.GetBlockFluidTint((int)System.Math.Floor(position.X), (int)System.Math.Floor(position.Y), (int)System.Math.Floor(position.Z), blockType);
				particleSystemProxy.DefaultColor = UInt32Color.FromRGBA((byte)blockFluidTint.X, (byte)blockFluidTint.Y, (byte)blockFluidTint.Z, byte.MaxValue);
			}
			else
			{
				particleSystemProxy.DefaultColor = value.Color;
			}
			particleSystemProxy.Scale = value.Scale;
			particleSystemProxy.Position = value.PositionOffset;
			if (faceCameraYaw)
			{
				particleSystemProxy.Rotation = Quaternion.CreateFromYawPitchRoll(_gameInstance.CameraModule.Controller.Rotation.Yaw, 0f, 0f);
			}
			else
			{
				particleSystemProxy.Rotation = value.RotationOffset;
			}
			return true;
		}
		return false;
	}

	public bool TrySpawnSystem(string systemId, out ParticleSystemProxy particleSystemProxy, bool isLocalPlayer = false, bool isTracked = false)
	{
		particleSystemProxy = null;
		if (!_systemSettingsById.TryGetValue(systemId, out var value))
		{
			_gameInstance.App.DevTools.Error("Could not find particle system settings: " + systemId);
			return false;
		}
		Vector2 textureAltasInverseSize = new Vector2(1f / (float)_gameInstance.FXModule.TextureAtlas.Width, 1f / (float)_gameInstance.FXModule.TextureAtlas.Height);
		if (!_gameInstance.Engine.FXSystem.Particles.TrySpawnParticleSystemProxy(value, textureAltasInverseSize, out particleSystemProxy, isLocalPlayer, isTracked))
		{
			Logger.Warn("Particle system proxy limit reached");
			return false;
		}
		return true;
	}

	public void PreUpdate(Vector3 cameraPosition)
	{
		SceneRenderer sceneRenderer = _gameInstance.SceneRenderer;
		FXSystem fXSystem = _gameInstance.Engine.FXSystem;
		fXSystem.Particles.CleanDeadProxies();
		ParticleSystemProxy[] particleSystemProxies = fXSystem.Particles.ParticleSystemProxies;
		int particleSystemProxyCount = fXSystem.Particles.ParticleSystemProxyCount;
		sceneRenderer.PrepareForIncomingOccludeeParticle(particleSystemProxyCount);
		BoundingBox boundingBox = default(BoundingBox);
		for (int i = 0; i < particleSystemProxyCount; i++)
		{
			Vector3 vector = particleSystemProxies[i].Position - cameraPosition;
			Vector3 vector2 = new Vector3(particleSystemProxies[i].Settings.BoundingRadius);
			boundingBox.Min = vector - vector2;
			boundingBox.Max = vector + vector2;
			sceneRenderer.RegisterOccludeeParticle(ref boundingBox);
		}
	}

	public void Update(Vector3 cameraPosition)
	{
		FXSystem fXSystem = _gameInstance.Engine.FXSystem;
		fXSystem.Particles.UpdateAnimatedBlockParticles();
		fXSystem.Particles.UpdateProxies(cameraPosition, ProxyCheck);
		UpdateDebugInfo();
		if (_gameInstance.Input.IsKeyHeld((SDL_Scancode)62))
		{
			if (!_resettingParticles)
			{
				if (_gameInstance.Input.IsKeyHeld((SDL_Scancode)225))
				{
					_gameInstance.ParticleSystemStoreModule.ResetParticleSystems();
				}
				else
				{
					_gameInstance.ParticleSystemStoreModule.ResetParticleSystems(skipEntities: true);
				}
			}
			_resettingParticles = true;
		}
		else
		{
			_resettingParticles = false;
		}
	}

	public void UpdateVisibilityPrediction(int[] occludeesVisibility, int particleResultsOffset, int particleResultsCount, bool updateParticles)
	{
		FXSystem fXSystem = _gameInstance.Engine.FXSystem;
		ParticleSystemProxy[] particleSystemProxies = fXSystem.Particles.ParticleSystemProxies;
		bool flag = !updateParticles;
		for (int i = 0; i < particleResultsCount; i++)
		{
			bool visibilityPrediction = flag || occludeesVisibility[particleResultsOffset + i] == 1;
			particleSystemProxies[i].VisibilityPrediction = visibilityPrediction;
		}
	}

	public void GatherRenderableSpawners(Vector3 cameraPosition, BoundingFrustum viewFrustum)
	{
		FXSystem fXSystem = _gameInstance.Engine.FXSystem;
		foreach (ParticleSystem value in fXSystem.Particles.ParticleSystems.Values)
		{
			BoundingSphere sphere = new BoundingSphere(value.Position, value.BoundingRadius);
			bool flag = !FrustumCheck || value.IsFirstPerson || viewFrustum.Intersects(sphere);
			float num = (value.IsFirstPerson ? 0.01f : Vector3.DistanceSquared(cameraPosition, value.Position));
			bool flag2 = !DistanceCheck || num < value.CullDistanceSquared;
			bool flag3 = value.IsFirstPerson || value.Proxy.VisibilityPrediction;
			bool isVisible = value.IsExpiring || ((value.IsImportant || (flag2 && flag && flag3)) && value.Proxy.Visible);
			fXSystem.Particles.RegisterTask(value, isVisible, num);
		}
	}

	public void DespawnDebugSystem(int id)
	{
		if (DebugParticleSytemProxiesById.TryGetValue(id, out var value))
		{
			value.ParticleSystemProxy.Expire();
			DebugParticleSytemProxiesById.Remove(id);
		}
	}

	public string GetSystemsList()
	{
		string text = "";
		foreach (string key in _systemSettingsById.Keys)
		{
			text = text + key + "\n";
		}
		return text;
	}

	public void GetSettingsStats(out int particleSystemSettingsCount, out int particleSettingsCount, out int keyframeArrayCount, out int keyframeArrayMaxSize, out int keyframeCount)
	{
		particleSystemSettingsCount = _systemSettingsById.Count;
		particleSettingsCount = _particlesByFile.Count;
		keyframeArrayCount = 0;
		keyframeArrayMaxSize = 0;
		keyframeCount = 0;
		foreach (ParticleSettings value in _particlesByFile.Values)
		{
			keyframeArrayCount += ((value.ColorKeyFrameCount != 0) ? 1 : 0);
			keyframeArrayCount += ((value.OpacityKeyFrameCount != 0) ? 1 : 0);
			keyframeArrayCount += ((value.RotationKeyFrameCount != 0) ? 1 : 0);
			keyframeArrayCount += ((value.ScaleKeyFrameCount != 0) ? 1 : 0);
			keyframeArrayCount += ((value.TextureKeyFrameCount != 0) ? 1 : 0);
			keyframeArrayMaxSize = System.Math.Max(keyframeArrayMaxSize, value.ColorKeyFrameCount);
			keyframeArrayMaxSize = System.Math.Max(keyframeArrayMaxSize, value.OpacityKeyFrameCount);
			keyframeArrayMaxSize = System.Math.Max(keyframeArrayMaxSize, value.RotationKeyFrameCount);
			keyframeArrayMaxSize = System.Math.Max(keyframeArrayMaxSize, value.ScaleKeyFrameCount);
			keyframeArrayMaxSize = System.Math.Max(keyframeArrayMaxSize, value.TextureKeyFrameCount);
			keyframeCount += value.ColorKeyFrameCount;
			keyframeCount += value.OpacityKeyFrameCount;
			keyframeCount += value.RotationKeyFrameCount;
			keyframeCount += value.ScaleKeyFrameCount;
			keyframeCount += value.TextureKeyFrameCount;
		}
	}

	public void AddDebug(ParticleSystem particleSystem)
	{
		_gameInstance.Engine.FXSystem.Particles.AddParticleSystemDebug(particleSystem);
	}

	public void TrySpawnDebugSystem(string systemId, Vector3 startPosition, bool useDebug, int quantity)
	{
		if (quantity > 1)
		{
			startPosition.X -= 2f * (float)System.Math.Floor((float)(quantity / 10) * 0.5f);
			startPosition.Z -= 2f * (float)System.Math.Floor(5.0);
		}
		int nextDebugId = _nextDebugId;
		int nextDebugId2 = _nextDebugId;
		for (int i = 0; i < quantity; i++)
		{
			Vector3 position = startPosition;
			if (quantity > 1)
			{
				position.X += (float)System.Math.Floor((float)i / 10f) * 2f;
				position.Z += 2 * (i % 10);
			}
			if (TrySpawnSystem(systemId, out var particleSystemProxy, isLocalPlayer: false, isTracked: true))
			{
				DebugParticleSystemProxy debugParticleSystemProxy = new DebugParticleSystemProxy();
				debugParticleSystemProxy.SystemId = systemId;
				debugParticleSystemProxy.UseDebug = useDebug;
				debugParticleSystemProxy.NeedDebugRefreshing = debugParticleSystemProxy.UseDebug;
				debugParticleSystemProxy.Position = position;
				debugParticleSystemProxy.ParticleSystemProxy = particleSystemProxy;
				particleSystemProxy.Position = debugParticleSystemProxy.Position;
				particleSystemProxy.Rotation = Quaternion.Identity;
				DebugParticleSytemProxiesById[_nextDebugId] = debugParticleSystemProxy;
				nextDebugId2 = _nextDebugId;
				_nextDebugId++;
			}
			else
			{
				_gameInstance.Chat.Log("Particle System (" + systemId + ") could not be created!");
			}
		}
		if (quantity > 1)
		{
			_gameInstance.Chat.Log($"Particle systems created! (Ids: {nextDebugId} - {nextDebugId2})");
		}
		else
		{
			_gameInstance.Chat.Log($"Particle system created! (Id: {nextDebugId})");
		}
	}

	public void UpdateDebugInfo()
	{
		foreach (DebugParticleSystemProxy value in DebugParticleSytemProxiesById.Values)
		{
			if (value.ParticleSystemProxy != null)
			{
				if (value.UseDebug && value.ParticleSystemProxy.ParticleSystem != null && value.NeedDebugRefreshing)
				{
					AddDebug(value.ParticleSystemProxy.ParticleSystem);
				}
				value.NeedDebugRefreshing = value.ParticleSystemProxy.ParticleSystem == null;
			}
		}
	}

	public void ResetDebugParticleSystems()
	{
		foreach (DebugParticleSystemProxy value in DebugParticleSytemProxiesById.Values)
		{
			if (value.ParticleSystemProxy != null)
			{
				value.ParticleSystemProxy.Expire(instant: true);
				value.ParticleSystemProxy = null;
			}
			if (TrySpawnSystem(value.SystemId, out var particleSystemProxy, isLocalPlayer: false, isTracked: true))
			{
				particleSystemProxy.Position = value.Position;
				particleSystemProxy.Rotation = Quaternion.Identity;
				value.ParticleSystemProxy = particleSystemProxy;
			}
		}
	}
}
