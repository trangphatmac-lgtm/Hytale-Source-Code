#define TRACE
#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Hypixel.ProtoPlus;
using HytaleClient.Application;
using HytaleClient.Audio;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.Audio;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.Characters;
using HytaleClient.Data.ClientInteraction;
using HytaleClient.Data.Entities;
using HytaleClient.Data.Entities.Initializers;
using HytaleClient.Data.EntityStats;
using HytaleClient.Data.EntityUI;
using HytaleClient.Data.FX;
using HytaleClient.Data.Items;
using HytaleClient.Data.Map;
using HytaleClient.Data.Palette;
using HytaleClient.Data.Weather;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Particles;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules;
using HytaleClient.InGame.Modules.AmbienceFX;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.InGame.Modules.Machinima.Actors;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.InGame.Modules.WorldMap;
using HytaleClient.Interface;
using HytaleClient.Interface.InGame;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using HytaleClient.Net.Protocol;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Pkix;
using Wwise;

namespace HytaleClient.Networking;

internal class PacketHandler : Disposable
{
	private enum AssetUpdatePrepareSteps
	{
		PrepareBlockTypes,
		CopyBlockTypes,
		PrepareItemIconAtlas,
		PrepareEntityAtlas,
		PrepareItems,
		PrepareParticles,
		PrepareTrails,
		PrepareItemPlayerAnimations,
		PrepareInteractions,
		PrepareWorldMapAtlas,
		PrepareFXAtlas,
		PrepareSoundEffects,
		PrepareSoundBanks
	}

	private enum AssetUpdateSetupSteps
	{
		SetupBlockTypes,
		SetupBlockAtlas,
		SetupItemIcons,
		SetupItemPlayerAnimations,
		SetupItems,
		SetupInteractions,
		SetupParticleSpawners,
		SetupTrails,
		SetupFXAtlas,
		SetupEntityAtlas,
		SetupModelsAndAnimation,
		UpdateAtlasSizes,
		RebuildEntityRenderers,
		UpdateInterfaceRenderPreview,
		BuildWorldMapAtlas,
		ReloadSkyRenderer,
		UpdateWeatherTexture,
		ResetParticleSystems
	}

	[Flags]
	private enum ConnectionStage : byte
	{
		WaitingForSetup = 2,
		SettingUp = 4,
		Playing = 8
	}

	[Flags]
	private enum AssetType : uint
	{
		AmbienceFX = 2u,
		BlockHitboxes = 4u,
		BlockTypes = 8u,
		Environment = 0x10u,
		FluidFX = 0x20u,
		Items = 0x40u,
		ItemCategories = 0x80u,
		ParticleSpawners = 0x100u,
		ParticleSystems = 0x200u,
		ResourceTypes = 0x400u,
		Weather = 0x800u,
		Translations = 0x1000u,
		FieldcraftCategories = 0x2000u,
		Trails = 0x4000u,
		EntityEffects = 0x8000u,
		BlockParticleSets = 0x10000u,
		ItemAnimations = 0x20000u,
		Interactions = 0x40000u,
		RootInteractions = 0x80000u,
		UnarmedInteractions = 0x100000u,
		BlockSoundSets = 0x200000u,
		EntityStatTypes = 0x400000u,
		ItemQuality = 0x800000u,
		ItemReticles = 0x1000000u,
		HitboxCollisionConfig = 0x2000000u,
		RepulsionConfig = 0x4000000u,
		ModelVFX = 0x8000000u,
		EntityUIComponents = 0x10000000u
	}

	public class PendingCallback
	{
		public Action<FailureReply, ProtoPacket> Callback;

		public Disposable Disposable;
	}

	public class InventoryWindow
	{
		public int Id;

		public WindowType WindowType;

		public string WindowDataStringified;

		public JObject WindowData;

		public ClientItemStack[] Inventory;
	}

	public class EventTitle
	{
		public float Duration;

		public float FadeInDuration;

		public float FadeOutDuration;

		public string PrimaryTitle;

		public string SecondaryTitle;

		public bool IsMajor;

		public string Icon;
	}

	public class ClientKnownRecipe
	{
		public string ItemId;

		public ClientItemCraftingRecipe Recipe;

		public ClientKnownRecipe(string itemId, CraftingRecipe recipe)
		{
			ItemId = itemId;
			Recipe = new ClientItemCraftingRecipe(recipe);
		}
	}

	public class PlayerListPlayer
	{
		public Guid Uuid;

		public string DisplayName;

		public int Ping;

		public PlayerListPlayer(PlayerListPlayer player)
		{
			Uuid = player.Uuid_;
			DisplayName = player.DisplayName;
			Ping = player.Ping;
		}
	}

	public class TextureInfo
	{
		public string Checksum;

		public int Width;

		public int Height;
	}

	public AmbienceFX[] _networkAmbienceFXs;

	private FileStream _blobFileStream;

	private Asset _blobAsset;

	private readonly List<string> _assetNamesToUpdate = new List<string>();

	private readonly long[] _assetUpdatePrepareTimes = new long[typeof(AssetUpdatePrepareSteps).GetEnumValues().Length];

	private readonly Stopwatch _assetUpdatePrepareTimer = new Stopwatch();

	private readonly long[] _assetUpdateSetupTimes = new long[typeof(AssetUpdateSetupSteps).GetEnumValues().Length];

	private readonly Stopwatch _assetUpdateSetupTimer = new Stopwatch();

	private static readonly Regex HashRegex = new Regex("^[A-Fa-f0-9]{64}$");

	private int _highestReceivedBlockId;

	private Dictionary<int, BlockType> _networkBlockTypes = new Dictionary<int, BlockType>();

	private ClientBlockType[] _upcomingBlockTypes;

	private Dictionary<string, MapModule.AtlasLocation> _upcomingBlocksImageLocations;

	private Point _upcomingBlocksAtlasSize;

	private const int MaxChunkBufferSize = 327683;

	private readonly byte[] _compressedChunkBuffer = new byte[327683];

	private int _compressedChunkBufferPosition = 0;

	private readonly byte[] _decompressedChunkBuffer = new byte[327683];

	private readonly BitFieldArr _bitFieldArr = new BitFieldArr(10, 1024);

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly GameInstance _gameInstance;

	private readonly BlockingCollection<ProtoPacket> _packets = new BlockingCollection<ProtoPacket>();

	private readonly HashSet<string> _unhandledPacketTypes = new HashSet<string>();

	private readonly Thread _thread;

	private readonly CancellationTokenSource _threadCancellationTokenSource = new CancellationTokenSource();

	private readonly CancellationToken _threadCancellationToken;

	private readonly ServerSettings _upcomingServerSettings = new ServerSettings();

	private readonly Stopwatch _connectionStopwatch = Stopwatch.StartNew();

	private readonly Stopwatch _stageStopwatch = Stopwatch.StartNew();

	private ConnectionStage _stage = ConnectionStage.WaitingForSetup;

	private string _stageValidationPacketId;

	private AssetType _receivedAssetTypes;

	private long _lastAssetReceivedMs;

	private ConcurrentDictionary<int, PendingCallback> _pendingCallbacks = new ConcurrentDictionary<int, PendingCallback>();

	private int _lastCallbackToken;

	private DateTime _lastCallbackWarning;

	private EntityEffect[] _entityEffects;

	private Dictionary<string, ItemPlayerAnimations> _networkItemPlayerAnimations;

	private Dictionary<string, ClientItemPlayerAnimations> _upcomingItemPlayerAnimations;

	private Dictionary<string, ItemBase> _networkItems;

	private Dictionary<string, ClientItemBase> _upcomingItems = new Dictionary<string, ClientItemBase>();

	private ModelVFX[] _modelVFXs;

	private Dictionary<string, ParticleSystem> _networkParticleSystems;

	private Dictionary<string, ParticleSpawner> _networkParticleSpawners;

	private Dictionary<string, TextureInfo> _upcomingParticleTextureInfo;

	private List<string> _upcomingUVMotionTexturePaths;

	private readonly object _setupLock = new object();

	private Dictionary<string, Point> _upcomingEntitiesImageLocations;

	private Dictionary<string, WorldMapModule.Texture> _upcomingWorldMapImageLocations;

	private Interaction[] _networkInteractions;

	private ClientInteraction[] _upcomingInteractions;

	private RootInteraction[] _networkRootInteractions;

	private ClientRootInteraction[] _upcomingRootInteractions;

	private Dictionary<string, TextureInfo> _upcomingTrailTextureInfo;

	private Dictionary<string, Trail> _networkTrails;

	public bool IsOnThread => ThreadHelper.IsOnThread(_thread);

	private void ProcessUpdateAmbienceFXPacket(UpdateAmbienceFX packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Invalid comparison between Unknown and I4
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Expected O, but got Unknown
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		UpdateAmbienceFX val = new UpdateAmbienceFX(packet);
		UpdateType type = val.Type;
		UpdateType val2 = type;
		if ((int)val2 != 0)
		{
			if (val2 - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] AmbienceFX: Starting {val.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if ((int)val.Type == 1)
				{
					if (val.MaxId > _networkAmbienceFXs.Length)
					{
						Array.Resize(ref _networkAmbienceFXs, val.MaxId);
					}
					foreach (KeyValuePair<int, AmbienceFX> item in val.AmbienceFX_)
					{
						_networkAmbienceFXs[item.Key] = item.Value;
					}
				}
				AmbienceFX[] clonedAmbienceFXs = (AmbienceFX[])(object)new AmbienceFX[_networkAmbienceFXs.Length];
				for (int i = 0; i < _networkAmbienceFXs.Length; i++)
				{
					clonedAmbienceFXs[i] = new AmbienceFX(_networkAmbienceFXs[i]);
				}
				_gameInstance.AmbienceFXModule.PrepareAmbienceFXs(clonedAmbienceFXs, out var ambienceFXSettings);
				UpdateType updateType = val.Type;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_006a: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.AmbienceFXModule.SetupAmbienceFXs(clonedAmbienceFXs, ambienceFXSettings);
					_gameInstance.AmbienceFXModule.OnAmbienceFXChanged();
					_gameInstance.AudioModule.OnSoundEffectCollectionChanged();
					_gameInstance.App.DevTools.Info($"[AssetUpdate] AmbienceFX: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)val).GetType().Name} at {_stage} with type {val.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.AmbienceFX);
		if (_networkAmbienceFXs == null)
		{
			_networkAmbienceFXs = (AmbienceFX[])(object)new AmbienceFX[val.MaxId];
		}
		foreach (KeyValuePair<int, AmbienceFX> item2 in val.AmbienceFX_)
		{
			_networkAmbienceFXs[item2.Key] = item2.Value;
		}
		FinishedReceivedAssetType(AssetType.AmbienceFX);
	}

	private static bool IsAssetPathItemIconRelated(string assetPath)
	{
		return assetPath.StartsWith("Icons/ItemsGenerated/") || assetPath.StartsWith("Icons/Items/");
	}

	private static bool IsAssetPathBlockRelated(string assetPath)
	{
		return assetPath.StartsWith("Blocks/") || assetPath.StartsWith("BlockTextures/") || assetPath.StartsWith("Resources/");
	}

	private static bool IsAssetPathParticlesRelated(string assetPath)
	{
		return assetPath.StartsWith("Particles/");
	}

	private static bool IsAssetPathTrailsRelated(string assetPath)
	{
		return assetPath.StartsWith("Trails/");
	}

	private static bool IsAssetPathSkyRelated(string assetPath)
	{
		return assetPath.StartsWith("Sky/");
	}

	private static bool IsAssetPathWorldMapUIRelated(string assetPath)
	{
		return assetPath.StartsWith("UI/WorldMap/");
	}

	private static bool IsAssetPathScreenEffectRelated(string assetPath)
	{
		return assetPath.StartsWith("ScreenEffects/");
	}

	private static bool IsAssetPathSoundEffectRelated(string assetPath)
	{
		return assetPath.StartsWith("SoundEffects/");
	}

	private static bool IsAssetPathSoundBankRelated(string assetPath)
	{
		return assetPath.StartsWith("SoundBanks/");
	}

	private static bool IsAssetPathItemAnimationRelated(string assetPath)
	{
		return assetPath.StartsWith("Characters/Animations/Items/") || assetPath.StartsWith("NPC/");
	}

	private static bool IsAssetPathCharacterAnimationRelated(string assetPath)
	{
		return assetPath.StartsWith("Characters/Animations/");
	}

	private static bool IsAssetPathItemRelated(string assetPath)
	{
		return assetPath.StartsWith("UI/Reticles/") || assetPath.StartsWith("Items/") || EntityStoreModule.IsAssetPathCharacterRelated(assetPath) || IsAssetPathBlockRelated(assetPath);
	}

	private void ProcessAssetInitializePacket(AssetInitialize packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		if (_blobAsset != null)
		{
			throw new Exception("A blob download has already started! Name: " + _blobAsset.Name + ", Hash: " + _blobAsset.Hash);
		}
		if (!HashRegex.IsMatch(packet.Asset_.Hash))
		{
			throw new Exception("Invalid asset hash " + packet.Asset_.Hash + " for " + packet.Asset_.Name);
		}
		_blobFileStream = File.Create(Paths.TempAssetDownload);
		_blobAsset = packet.Asset_;
	}

	private void ProcessAssetPartPacket(AssetPart packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		_blobFileStream.Write((byte[])(object)packet.Part, 0, packet.Part.Length);
	}

	private void ProcessAssetFinalizePacket(AssetFinalize packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		_blobFileStream.Flush(flushToDisk: true);
		_blobFileStream.Close();
		_blobFileStream = null;
		string assetName = _blobAsset.Name;
		string assetHash = _blobAsset.Hash;
		_blobAsset = null;
		string text = Path.Combine(Paths.CachedAssets, assetHash.Substring(0, 2));
		string text2 = Path.Combine(text, assetHash.Substring(2));
		Directory.CreateDirectory(Paths.CachedAssets);
		Directory.CreateDirectory(text);
		if (File.Exists(text2))
		{
			File.Delete(text2);
		}
		File.Move(Paths.TempAssetDownload, text2);
		switch (_stage)
		{
		case ConnectionStage.SettingUp:
			AssetManager.AddServerAssetToCache(assetName, assetHash);
			break;
		case ConnectionStage.Playing:
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				AssetManager.AddServerAssetToCache(assetName, assetHash);
				_gameInstance.RegisterHashForServerAsset(assetName, assetHash);
				_assetNamesToUpdate.Add(assetName);
				_gameInstance.App.DevTools.Info("[AssetUpdate] Assets: Starting AddOrUpdate \"" + assetName + "\"");
			});
			break;
		}
	}

	private void ProcessRemoveAssetsPacket(RemoveAssets packet)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		Asset[] assets = (Asset[])(object)new Asset[packet.Asset_.Length];
		for (int i = 0; i < packet.Asset_.Length; i++)
		{
			if (packet.Asset_[i] != null)
			{
				assets[i] = new Asset(packet.Asset_[i]);
			}
		}
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			List<string> list = new List<string>();
			Asset[] array = assets;
			foreach (Asset val in array)
			{
				if (!HashRegex.IsMatch(val.Hash))
				{
					throw new Exception("Invalid asset hash " + val.Hash + " for " + val.Name);
				}
				list.Add(val.Name);
				_assetNamesToUpdate.Add(val.Name);
				_gameInstance.RemoveHashForServerAsset(val.Name);
				AssetManager.RemoveServerAssetFromCache(val.Name, val.Hash);
			}
			_gameInstance.App.DevTools.Info("[AssetUpdate] Assets: Starting Remove [" + string.Join(",", list) + "]");
		});
	}

	private void ProcessRequestCommonAssetsRebuildPacket(RequestCommonAssetsRebuild packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			string[] array = _assetNamesToUpdate.ToArray();
			_assetNamesToUpdate.Clear();
			_gameInstance.App.Interface.TriggerEvent("assets.updated", array);
			QueueCommonAssetUpdate(array);
		});
	}

	private void QueueCommonAssetUpdate(string[] assetNamesToUpdate)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		ThreadPool.QueueUserWorkItem(delegate
		{
			//IL_0a0d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a13: Expected O, but got Unknown
			_gameInstance.App.DevTools.Info("[AssetUpdate] Assets: Inside ThreadPool.QueueUserWorkItem " + TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds));
			byte[][] upcomingBlocksAtlasPixelsPerLevel = null;
			byte[][] upcomingEntitiesAtlasPixelsPerLevel = null;
			byte[][] upcomingWorldMapAtlasPixelsPerLevel = null;
			byte[][] upcomingFXAtlasPixelsPerLevel = null;
			byte[] upcomingIconAtlasPixels = null;
			int upcomingIconAtlasWidth = 0;
			int upcomingIconAtlasHeight = 0;
			ClientBlockType[] upcomingBlockTypes = null;
			Dictionary<string, ClientIcon> upcomingItemIcons = null;
			Dictionary<string, Point> upcomingEntitiesImageLocations = null;
			Dictionary<string, ClientItemPlayerAnimations> upcomingItemAnimations = null;
			Dictionary<string, ClientItemBase> upcomingItems = null;
			ClientInteraction[] upcomingInteractions = null;
			Dictionary<string, ParticleSystem> upcomingParticleSystems = null;
			Dictionary<string, ParticleSpawner> upcomingParticleSpawners = null;
			Dictionary<string, ParticleSettings> upcomingParticles = null;
			Dictionary<string, Trail> upcomingTrails = null;
			Dictionary<string, Rectangle> upcomingFXImageLocations = null;
			Dictionary<string, WorldMapModule.Texture> upcomingWorldMapImageLocations = null;
			AmbienceFXSettings[] ambienceFXSettings = null;
			Dictionary<string, WwiseResource> upcomingWwiseIds = null;
			AmbienceFX[] clonedAmbienceFXs = null;
			bool blockRelatedUpdate = false;
			bool flag = false;
			bool characterRelatedUpdate = false;
			bool flag2 = false;
			bool particlesRelatedUpdate = false;
			bool flag3 = false;
			bool trailsRelatedUpdate = false;
			bool skyRelatedUpdate = false;
			bool screenEffectsRelatedUpdate = false;
			bool flag4 = false;
			bool flag5 = false;
			bool soundEffectRelatedUpdate = false;
			bool soundBankRelatedUpdate = false;
			string[] array = assetNamesToUpdate;
			foreach (string assetPath in array)
			{
				if (IsAssetPathBlockRelated(assetPath))
				{
					blockRelatedUpdate = true;
				}
				if (IsAssetPathItemIconRelated(assetPath))
				{
					flag = true;
				}
				if (EntityStoreModule.IsAssetPathCharacterRelated(assetPath))
				{
					characterRelatedUpdate = true;
				}
				if (IsAssetPathItemRelated(assetPath))
				{
					flag2 = true;
				}
				if (IsAssetPathParticlesRelated(assetPath))
				{
					particlesRelatedUpdate = true;
				}
				if (IsAssetPathWorldMapUIRelated(assetPath))
				{
					flag3 = true;
				}
				if (IsAssetPathTrailsRelated(assetPath))
				{
					trailsRelatedUpdate = true;
				}
				if (IsAssetPathSkyRelated(assetPath))
				{
					skyRelatedUpdate = true;
				}
				if (IsAssetPathScreenEffectRelated(assetPath))
				{
					screenEffectsRelatedUpdate = true;
				}
				if (IsAssetPathItemAnimationRelated(assetPath))
				{
					flag4 = true;
				}
				if (IsAssetPathCharacterAnimationRelated(assetPath))
				{
					flag5 = true;
				}
				if (IsAssetPathSoundEffectRelated(assetPath))
				{
					soundEffectRelatedUpdate = true;
				}
				if (IsAssetPathSoundBankRelated(assetPath))
				{
					soundBankRelatedUpdate = true;
				}
			}
			long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
			lock (_setupLock)
			{
				Logger.Info("[AssetUpdate] Assets: Took {0} to obtain lock", TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds - elapsedMilliseconds));
				Array.Clear(_assetUpdatePrepareTimes, 0, _assetUpdatePrepareTimes.Length);
				try
				{
					if (blockRelatedUpdate)
					{
						_assetUpdatePrepareTimer.Restart();
						_gameInstance.MapModule.PrepareBlockTypes(_networkBlockTypes, _highestReceivedBlockId, atlasNeedsUpdate: true, ref _upcomingBlockTypes, ref _upcomingBlocksImageLocations, ref _upcomingBlocksAtlasSize, out upcomingBlocksAtlasPixelsPerLevel, _threadCancellationToken);
						CancellationToken threadCancellationToken = _threadCancellationToken;
						if (threadCancellationToken.IsCancellationRequested)
						{
							return;
						}
						_assetUpdatePrepareTimes[0] = _assetUpdatePrepareTimer.ElapsedMilliseconds;
						_assetUpdatePrepareTimer.Restart();
						upcomingBlockTypes = new ClientBlockType[_upcomingBlockTypes.Length];
						Array.Copy(_upcomingBlockTypes, upcomingBlockTypes, _upcomingBlockTypes.Length);
						_assetUpdatePrepareTimes[1] = _assetUpdatePrepareTimer.ElapsedMilliseconds;
					}
					if (flag)
					{
						_assetUpdatePrepareTimer.Restart();
						_gameInstance.ItemLibraryModule.PrepareItemIconAtlas(_networkItems, out upcomingItemIcons, out upcomingIconAtlasPixels, out upcomingIconAtlasWidth, out upcomingIconAtlasHeight, _threadCancellationToken);
						CancellationToken threadCancellationToken = _threadCancellationToken;
						if (threadCancellationToken.IsCancellationRequested)
						{
							return;
						}
						_assetUpdatePrepareTimes[2] = _assetUpdatePrepareTimer.ElapsedMilliseconds;
					}
					if (characterRelatedUpdate || flag)
					{
						_assetUpdatePrepareTimer.Restart();
						_gameInstance.EntityStoreModule.PrepareAtlas(out _upcomingEntitiesImageLocations, out upcomingEntitiesAtlasPixelsPerLevel, _threadCancellationToken);
						upcomingEntitiesImageLocations = _upcomingEntitiesImageLocations;
						CancellationToken threadCancellationToken = _threadCancellationToken;
						if (threadCancellationToken.IsCancellationRequested)
						{
							return;
						}
						_assetUpdatePrepareTimes[3] = _assetUpdatePrepareTimer.ElapsedMilliseconds;
					}
					if (flag2)
					{
						_assetUpdatePrepareTimer.Restart();
						_upcomingItems.Clear();
						_gameInstance.ItemLibraryModule.PrepareItems(_networkItems, _upcomingEntitiesImageLocations, ref _upcomingItems, _threadCancellationToken);
						upcomingItems = new Dictionary<string, ClientItemBase>(_upcomingItems);
						CancellationToken threadCancellationToken = _threadCancellationToken;
						if (threadCancellationToken.IsCancellationRequested)
						{
							return;
						}
						_assetUpdatePrepareTimes[4] = _assetUpdatePrepareTimer.ElapsedMilliseconds;
					}
					if (particlesRelatedUpdate)
					{
						_assetUpdatePrepareTimer.Restart();
						upcomingParticleSystems = new Dictionary<string, ParticleSystem>(_networkParticleSystems);
						upcomingParticleSpawners = new Dictionary<string, ParticleSpawner>(_networkParticleSpawners);
						_gameInstance.ParticleSystemStoreModule.PrepareParticles(upcomingParticleSpawners, out upcomingParticles, out _upcomingParticleTextureInfo, out _upcomingUVMotionTexturePaths, _threadCancellationToken);
						CancellationToken threadCancellationToken = _threadCancellationToken;
						if (threadCancellationToken.IsCancellationRequested)
						{
							return;
						}
						_assetUpdatePrepareTimes[5] = _assetUpdatePrepareTimer.ElapsedMilliseconds;
					}
					if (trailsRelatedUpdate)
					{
						_assetUpdatePrepareTimer.Restart();
						upcomingTrails = new Dictionary<string, Trail>(_networkTrails);
						_gameInstance.TrailStoreModule.PrepareTrails(_networkTrails, out _upcomingTrailTextureInfo, _threadCancellationToken);
						CancellationToken threadCancellationToken = _threadCancellationToken;
						if (threadCancellationToken.IsCancellationRequested)
						{
							return;
						}
						_assetUpdatePrepareTimes[6] = _assetUpdatePrepareTimer.ElapsedMilliseconds;
					}
					if (flag4)
					{
						_assetUpdatePrepareTimer.Restart();
						_gameInstance.ItemLibraryModule.PrepareItemPlayerAnimations(_networkItemPlayerAnimations, out _upcomingItemPlayerAnimations);
						upcomingItemAnimations = _upcomingItemPlayerAnimations;
						CancellationToken threadCancellationToken = _threadCancellationToken;
						if (threadCancellationToken.IsCancellationRequested)
						{
							return;
						}
						_assetUpdatePrepareTimes[7] = _assetUpdatePrepareTimer.ElapsedMilliseconds;
					}
					if (flag5)
					{
						_assetUpdatePrepareTimer.Restart();
						_gameInstance.InteractionModule.PrepareInteractions(_networkInteractions, out _upcomingInteractions);
						upcomingInteractions = _upcomingInteractions;
						CancellationToken threadCancellationToken = _threadCancellationToken;
						if (threadCancellationToken.IsCancellationRequested)
						{
							return;
						}
						_assetUpdatePrepareTimes[8] = _assetUpdatePrepareTimer.ElapsedMilliseconds;
					}
					if (flag3)
					{
						_assetUpdatePrepareTimer.Restart();
						_gameInstance.WorldMapModule.PrepareTextureAtlas(out _upcomingWorldMapImageLocations, out upcomingWorldMapAtlasPixelsPerLevel, _threadCancellationToken);
						CancellationToken threadCancellationToken = _threadCancellationToken;
						if (threadCancellationToken.IsCancellationRequested)
						{
							return;
						}
						upcomingWorldMapImageLocations = _upcomingWorldMapImageLocations;
						_assetUpdatePrepareTimes[9] = _assetUpdatePrepareTimer.ElapsedMilliseconds;
					}
					if (particlesRelatedUpdate || trailsRelatedUpdate)
					{
						_assetUpdatePrepareTimer.Restart();
						_gameInstance.FXModule.PrepareAtlas(_upcomingParticleTextureInfo, _upcomingTrailTextureInfo, out upcomingFXImageLocations, out upcomingFXAtlasPixelsPerLevel, _threadCancellationToken);
						CancellationToken threadCancellationToken = _threadCancellationToken;
						if (threadCancellationToken.IsCancellationRequested)
						{
							return;
						}
						_assetUpdatePrepareTimes[10] = _assetUpdatePrepareTimer.ElapsedMilliseconds;
					}
					if (soundEffectRelatedUpdate)
					{
						_assetUpdatePrepareTimer.Restart();
						clonedAmbienceFXs = (AmbienceFX[])(object)new AmbienceFX[_networkAmbienceFXs.Length];
						for (int j = 0; j < _networkAmbienceFXs.Length; j++)
						{
							clonedAmbienceFXs[j] = new AmbienceFX(_networkAmbienceFXs[j]);
						}
						_gameInstance.AmbienceFXModule.PrepareAmbienceFXs(clonedAmbienceFXs, out ambienceFXSettings);
						_assetUpdatePrepareTimes[11] = _assetUpdatePrepareTimer.ElapsedMilliseconds;
					}
					if (soundBankRelatedUpdate)
					{
						_assetUpdatePrepareTimer.Restart();
						_gameInstance.AudioModule.PrepareSoundBanks(out upcomingWwiseIds);
						_assetUpdatePrepareTimes[12] = _assetUpdatePrepareTimer.ElapsedMilliseconds;
					}
				}
				catch (FileNotFoundException ex)
				{
					_gameInstance.App.DevTools.Error("[AssetUpdate] Failed to update assets! File disappeared: " + ex.FileName);
					Logger.Error((Exception)ex, "Failed to update assets! File disappeared:");
					return;
				}
				catch (IOException ex2)
				{
					_gameInstance.App.DevTools.Error("[AssetUpdate] Failed to update assets! " + ex2.Message);
					Logger.Error((Exception)ex2, "Failed to update assets:");
					return;
				}
				catch (KeyNotFoundException ex3)
				{
					_gameInstance.App.DevTools.Error("[AssetUpdate] Failed to update assets! " + ex3.Message);
					Logger.Error((Exception)ex3, "Failed to update assets:");
					return;
				}
				Logger.Info("[AssetUpdate] Assets: Finished prepare in {0}.", TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds));
				for (int k = 0; k < _assetUpdatePrepareTimes.Length; k++)
				{
					long num = _assetUpdatePrepareTimes[k];
					if (num > 0)
					{
						AssetUpdatePrepareSteps assetUpdatePrepareSteps = (AssetUpdatePrepareSteps)k;
						Logger.Info<AssetUpdatePrepareSteps, string>("[AssetUpdate] {0}: {1}", assetUpdatePrepareSteps, TimeHelper.FormatMillis(num));
					}
				}
			}
			long beforeMainThread = stopwatch.ElapsedMilliseconds;
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				Logger.Info("[AssetUpdate] Assets: Took {0} to enter main thread!", TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds - beforeMainThread));
				Array.Clear(_assetUpdateSetupTimes, 0, _assetUpdateSetupTimes.Length);
				if (upcomingBlockTypes != null)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.MapModule.SetupBlockTypes(upcomingBlockTypes);
					_assetUpdateSetupTimes[0] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (upcomingBlocksAtlasPixelsPerLevel != null)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.MapModule.TextureAtlas.UpdateTexture2DMipMaps(upcomingBlocksAtlasPixelsPerLevel);
					_assetUpdateSetupTimes[1] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (upcomingItemIcons != null)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.ItemLibraryModule.SetupItemIcons(upcomingItemIcons, upcomingIconAtlasPixels, upcomingIconAtlasWidth, upcomingIconAtlasHeight);
					_assetUpdateSetupTimes[2] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (upcomingItemAnimations != null)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.ItemLibraryModule.SetupItemPlayerAnimations(upcomingItemAnimations);
					_assetUpdateSetupTimes[3] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (upcomingItems != null)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.ItemLibraryModule.SetupItems(upcomingItems);
					_assetUpdateSetupTimes[4] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (upcomingInteractions != null)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.InteractionModule.SetupInteractions(upcomingInteractions);
					_assetUpdateSetupTimes[5] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (upcomingParticles != null)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.ParticleSystemStoreModule.SetupParticleSpawners(upcomingParticleSystems, upcomingParticleSpawners, upcomingParticles, _upcomingUVMotionTexturePaths);
					_assetUpdateSetupTimes[6] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (trailsRelatedUpdate)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.TrailStoreModule.SetupTrailSettings(upcomingTrails);
					_assetUpdateSetupTimes[7] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (upcomingParticles != null || trailsRelatedUpdate)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.FXModule.CreateAtlasTextures(upcomingFXImageLocations, upcomingFXAtlasPixelsPerLevel);
					_assetUpdateSetupTimes[8] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (upcomingEntitiesImageLocations != null)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.EntityStoreModule.CreateAtlasTexture(upcomingEntitiesImageLocations, upcomingEntitiesAtlasPixelsPerLevel);
					_assetUpdateSetupTimes[9] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (characterRelatedUpdate)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.EntityStoreModule.SetupModelsAndAnimations();
					_assetUpdateSetupTimes[10] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				_assetUpdateSetupTimer.Restart();
				_gameInstance.UpdateAtlasSizes();
				_assetUpdateSetupTimes[11] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				if (characterRelatedUpdate || blockRelatedUpdate || trailsRelatedUpdate || particlesRelatedUpdate)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.EntityStoreModule.RebuildRenderers(itemOnly: false);
					_assetUpdateSetupTimes[12] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (trailsRelatedUpdate || particlesRelatedUpdate)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.ParticleSystemStoreModule.ResetParticleSystems(skipEntities: true);
					_assetUpdateSetupTimes[17] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				_assetUpdateSetupTimer.Restart();
				_assetUpdateSetupTimes[13] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				if (upcomingWorldMapImageLocations != null)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.WorldMapModule.BuildTextureAtlas(upcomingWorldMapImageLocations, upcomingWorldMapAtlasPixelsPerLevel);
					_assetUpdateSetupTimes[14] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (skyRelatedUpdate)
				{
					_assetUpdateSetupTimer.Restart();
					if (_gameInstance.HashesByServerAssetPath.TryGetValue("Sky/Sun.png", out var value))
					{
						_gameInstance.WeatherModule.SkyRenderer.LoadSunTexture(value);
					}
					_assetUpdateSetupTimes[15] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (skyRelatedUpdate || screenEffectsRelatedUpdate)
				{
					_assetUpdateSetupTimer.Restart();
					_gameInstance.WeatherModule.RequestTextureUpdateFromWeather(_gameInstance.WeatherModule.CurrentWeather, forceUpdate: true);
					_assetUpdateSetupTimes[16] = _assetUpdateSetupTimer.ElapsedMilliseconds;
				}
				if (soundEffectRelatedUpdate)
				{
					_gameInstance.AmbienceFXModule.SetupAmbienceFXs(clonedAmbienceFXs, ambienceFXSettings);
					_gameInstance.AmbienceFXModule.OnAmbienceFXChanged();
					_gameInstance.AudioModule.OnSoundEffectCollectionChanged();
				}
				if (soundBankRelatedUpdate)
				{
					_gameInstance.AudioModule.SetupSoundBanks(upcomingWwiseIds);
					_gameInstance.Engine.Audio.RefreshBanks();
				}
				_gameInstance.App.DevTools.Info("[AssetUpdate] Assets: Finished in " + TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds) + ".");
				for (int l = 0; l < _assetUpdateSetupTimes.Length; l++)
				{
					long num2 = _assetUpdateSetupTimes[l];
					if (num2 > 0)
					{
						AssetUpdateSetupSteps assetUpdateSetupSteps = (AssetUpdateSetupSteps)l;
						Logger.Info<AssetUpdateSetupSteps, string>("[AssetUpdate] {0}: {1}", assetUpdateSetupSteps, TimeHelper.FormatMillis(num2));
					}
				}
			});
		});
	}

	public void ProcessPlaySoundEvent2DPacket(PlaySoundEvent2D packet)
	{
		ValidateStage(ConnectionStage.Playing);
		uint soundEventIndex = ResourceManager.GetNetworkWwiseId(packet.SoundEventIndex);
		if (soundEventIndex != 0)
		{
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				int playbackId = _gameInstance.AudioModule.PlayLocalSoundEvent(soundEventIndex);
				_gameInstance.AudioModule.ActionOnEvent(playbackId, (AkActionOnEventType)3);
			});
		}
	}

	public void ProcessPlaySoundEvent3DPacket(PlaySoundEvent3D packet)
	{
		ValidateStage(ConnectionStage.Playing);
		uint soundEventIndex = ResourceManager.GetNetworkWwiseId(packet.SoundEventIndex);
		if (soundEventIndex != 0)
		{
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				Vector3 position = new Vector3((float)packet.Position_.X, (float)packet.Position_.Y, (float)packet.Position_.Z);
				_gameInstance.AudioModule.PlaySoundEvent(soundEventIndex, position, Vector3.Zero);
			});
		}
	}

	public void ProcessPlaySoundEventEntityPacket(PlaySoundEventEntity packet)
	{
		ValidateStage(ConnectionStage.Playing);
		int networkId = packet.NetworkId;
		uint soundEventIndex = ResourceManager.GetNetworkWwiseId(packet.SoundEventIndex);
		if (soundEventIndex != 0)
		{
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				_gameInstance.EntityStoreModule.QueueSoundEvent(soundEventIndex, networkId);
			});
		}
	}

	private void ProcessAuth2Packet(Auth2 packet)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		ValidateStage(ConnectionStage.WaitingForSetup);
		_gameInstance.App.AuthManager.HandleAuth2(packet.NonceA, packet.Cert, out var encryptedNonceA, out var encryptedNonceB);
		_gameInstance.Connection.SendPacket((ProtoPacket)new Auth3(encryptedNonceA, encryptedNonceB));
	}

	private void ProcessAuth4Packet(Auth4 packet)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		ValidateStage(ConnectionStage.WaitingForSetup);
		_gameInstance.App.AuthManager.HandleAuth4(packet.Secret, packet.NonceB);
		_gameInstance.Connection.SendPacket((ProtoPacket)new Auth5());
	}

	private void ProcessAuth6Packet(Auth6 packet)
	{
		ValidateStage(ConnectionStage.WaitingForSetup);
		_gameInstance.App.AuthManager.HandleAuth6();
		if (_gameInstance.Connection.Referral != null)
		{
			_gameInstance.Connection.SendPacket((ProtoPacket)(object)_gameInstance.Connection.Referral);
		}
	}

	private void ProcessUpdateBlockTypesPacket(UpdateBlockTypes packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		UpdateBlockTypes val = new UpdateBlockTypes(packet);
		UpdateType type = val.Type;
		UpdateType val2 = type;
		if ((int)val2 != 0)
		{
			if ((int)val2 == 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] BlockTypes: Starting {val.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				bool atlasNeedsUpdate = val.UpdateBlockTextures || val.UpdateModelTextures;
				byte[][] upcomingBlocksAtlasPixelsPerLevel;
				ClientBlockType[] upcomingBlockTypes;
				lock (_setupLock)
				{
					foreach (KeyValuePair<int, BlockType> blockType in val.BlockTypes)
					{
						_networkBlockTypes[blockType.Key] = blockType.Value;
						if (blockType.Key > _highestReceivedBlockId)
						{
							_highestReceivedBlockId = blockType.Key;
						}
					}
					_gameInstance.MapModule.PrepareBlockTypes(val.BlockTypes, _highestReceivedBlockId, atlasNeedsUpdate, ref _upcomingBlockTypes, ref _upcomingBlocksImageLocations, ref _upcomingBlocksAtlasSize, out upcomingBlocksAtlasPixelsPerLevel, _threadCancellationToken);
					CancellationToken threadCancellationToken = _threadCancellationToken;
					if (threadCancellationToken.IsCancellationRequested)
					{
						return;
					}
					upcomingBlockTypes = new ClientBlockType[_upcomingBlockTypes.Length];
					Array.Copy(_upcomingBlockTypes, upcomingBlockTypes, _upcomingBlockTypes.Length);
				}
				UpdateType updateType = val.Type;
				bool updateModels = val.UpdateModels;
				bool updateMapGeometry = val.UpdateMapGeometry;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
					bool flag = atlasNeedsUpdate || updateModels || updateMapGeometry;
					Logger.Info("[AssetUpdate] BlockTypes: Rebuild all chunks: {0}", flag);
					_gameInstance.MapModule.SetupBlockTypes(upcomingBlockTypes, flag);
					if (atlasNeedsUpdate)
					{
						_gameInstance.MapModule.TextureAtlas.UpdateTexture2DMipMaps(upcomingBlocksAtlasPixelsPerLevel);
						_gameInstance.UpdateAtlasSizes();
					}
					if (updateModels)
					{
						_gameInstance.EntityStoreModule.RebuildRenderers(itemOnly: true);
					}
					_gameInstance.App.DevTools.Info($"[AssetUpdate] BlockTypes: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)val).GetType().Name} at {_stage} with type {val.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.BlockTypes);
		foreach (KeyValuePair<int, BlockType> blockType2 in val.BlockTypes)
		{
			_networkBlockTypes[blockType2.Key] = blockType2.Value;
			if (blockType2.Key > _highestReceivedBlockId)
			{
				_highestReceivedBlockId = blockType2.Key;
			}
		}
		_upcomingBlockTypes = new ClientBlockType[_highestReceivedBlockId + 1];
		_upcomingBlocksImageLocations = new Dictionary<string, MapModule.AtlasLocation>();
		_upcomingBlocksAtlasSize = new Point(_gameInstance.MapModule.TextureAtlas.Width, 0);
		FinishedReceivedAssetType(AssetType.BlockTypes);
	}

	private void ProcessUpdateBlockHitboxesPacket(UpdateBlockHitboxes packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Invalid comparison between Unknown and I4
		//IL_036c: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		UpdateBlockHitboxes val = new UpdateBlockHitboxes(packet);
		UpdateType type = val.Type;
		UpdateType val2 = type;
		if ((int)val2 != 0)
		{
			if (val2 - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] BlockHitboxes: Starting {val.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if (val.MaxId > _upcomingServerSettings.BlockHitboxes.Length)
				{
					BlockHitbox[] array = new BlockHitbox[val.MaxId];
					Array.Copy(_upcomingServerSettings.BlockHitboxes, array, _upcomingServerSettings.BlockHitboxes.Length);
					_upcomingServerSettings.BlockHitboxes = array;
				}
				if ((int)val.Type == 1)
				{
					foreach (KeyValuePair<int, Hitbox[]> blockHitbox in val.BlockHitboxes)
					{
						Hitbox[] value = blockHitbox.Value;
						BoundingBox[] array2 = new BoundingBox[value.Length];
						for (int i = 0; i < value.Length; i++)
						{
							Hitbox val3 = value[i];
							array2[i] = new BoundingBox(new Vector3(val3.MinX, val3.MinY, val3.MinZ), new Vector3(val3.MaxX, val3.MaxY, val3.MaxZ));
						}
						_upcomingServerSettings.BlockHitboxes[blockHitbox.Key] = new BlockHitbox(array2);
					}
				}
				else
				{
					foreach (KeyValuePair<int, Hitbox[]> blockHitbox2 in val.BlockHitboxes)
					{
						_upcomingServerSettings.BlockHitboxes[blockHitbox2.Key] = null;
					}
				}
				ServerSettings newServerSettings = _upcomingServerSettings.Clone();
				UpdateType updateType = val.Type;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_0033: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.SetServerSettings(newServerSettings);
					_gameInstance.App.DevTools.Info($"[AssetUpdate] BlockHitboxes: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)val).GetType().Name} at {_stage} with type {val.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.BlockHitboxes);
		if (_upcomingServerSettings.BlockHitboxes == null)
		{
			_upcomingServerSettings.BlockHitboxes = new BlockHitbox[packet.MaxId];
		}
		foreach (KeyValuePair<int, Hitbox[]> blockHitbox3 in val.BlockHitboxes)
		{
			Hitbox[] value2 = blockHitbox3.Value;
			BoundingBox[] array3 = new BoundingBox[value2.Length];
			for (int j = 0; j < value2.Length; j++)
			{
				Hitbox val4 = value2[j];
				array3[j] = new BoundingBox(new Vector3(val4.MinX, val4.MinY, val4.MinZ), new Vector3(val4.MaxX, val4.MaxY, val4.MaxZ));
			}
			_upcomingServerSettings.BlockHitboxes[blockHitbox3.Key] = new BlockHitbox(array3);
		}
		FinishedReceivedAssetType(AssetType.BlockHitboxes);
	}

	private void ProcessUpdateBlockSoundSets(UpdateBlockSoundSets packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Invalid comparison between Unknown and I4
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Expected O, but got Unknown
		UpdateBlockSoundSets val = new UpdateBlockSoundSets(packet);
		UpdateType type = val.Type;
		UpdateType val2 = type;
		if ((int)val2 != 0)
		{
			if (val2 - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] BlockSounndSets: Starting {val.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if ((int)val.Type == 1)
				{
					if (val.MaxId > _upcomingServerSettings.BlockSoundSets.Length)
					{
						Array.Resize(ref _upcomingServerSettings.BlockSoundSets, val.MaxId);
					}
					foreach (KeyValuePair<int, BlockSoundSet> blockSoundSet in val.BlockSoundSets)
					{
						_upcomingServerSettings.BlockSoundSets[blockSoundSet.Key] = new BlockSoundSet(blockSoundSet.Value);
					}
				}
				ServerSettings newServerSettings = _upcomingServerSettings.Clone();
				UpdateType updateType = val.Type;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_0033: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.SetServerSettings(newServerSettings);
					_gameInstance.App.DevTools.Info($"[AssetUpdate] BlockSoundSets: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)val).GetType().Name} at {_stage} with type {val.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.BlockSoundSets);
		_upcomingServerSettings.BlockSoundSets = (BlockSoundSet[])(object)new BlockSoundSet[val.MaxId];
		foreach (KeyValuePair<int, BlockSoundSet> blockSoundSet2 in val.BlockSoundSets)
		{
			_upcomingServerSettings.BlockSoundSets[blockSoundSet2.Key] = new BlockSoundSet(blockSoundSet2.Value);
		}
		FinishedReceivedAssetType(AssetType.BlockSoundSets);
	}

	private void ProcessUpdateBlockParticleSets(UpdateBlockParticleSets packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Invalid comparison between Unknown and I4
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		UpdateBlockParticleSets val = new UpdateBlockParticleSets(packet);
		UpdateType type = val.Type;
		UpdateType val2 = type;
		if ((int)val2 != 0)
		{
			if (val2 - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] BlockParticleSets: Starting {val.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if ((int)val.Type == 1)
				{
					foreach (KeyValuePair<string, BlockParticleSet> blockParticleSet in val.BlockParticleSets)
					{
						ClientBlockParticleSet clientBlockParticleSet = new ClientBlockParticleSet();
						ParticleProtocolInitializer.Initialize(blockParticleSet.Value, ref clientBlockParticleSet);
						_upcomingServerSettings.BlockParticleSets[blockParticleSet.Key] = clientBlockParticleSet;
					}
				}
				else
				{
					foreach (KeyValuePair<string, BlockParticleSet> blockParticleSet2 in val.BlockParticleSets)
					{
						_upcomingServerSettings.BlockParticleSets.Remove(blockParticleSet2.Key);
					}
				}
				ServerSettings newServerSettings = _upcomingServerSettings.Clone();
				UpdateType updateType = val.Type;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_0049: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.SetServerSettings(newServerSettings);
					_gameInstance.EntityStoreModule.ResetMovementParticleSystems();
					_gameInstance.App.DevTools.Info($"[AssetUpdate] BlockParticleSets: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)val).GetType().Name} at {_stage} with type {val.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.BlockParticleSets);
		if (_upcomingServerSettings.BlockParticleSets == null)
		{
			_upcomingServerSettings.BlockParticleSets = new Dictionary<string, ClientBlockParticleSet>();
		}
		foreach (KeyValuePair<string, BlockParticleSet> blockParticleSet3 in val.BlockParticleSets)
		{
			ClientBlockParticleSet clientBlockParticleSet2 = new ClientBlockParticleSet();
			ParticleProtocolInitializer.Initialize(blockParticleSet3.Value, ref clientBlockParticleSet2);
			_upcomingServerSettings.BlockParticleSets[blockParticleSet3.Key] = clientBlockParticleSet2;
		}
		FinishedReceivedAssetType(AssetType.BlockParticleSets);
	}

	private void ProcessUpdateBlockGroups(UpdateBlockGroups packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		UpdateBlockGroups val = new UpdateBlockGroups(packet);
		UpdateType type = val.Type;
		UpdateType val2 = type;
		if ((int)val2 != 0)
		{
			if (val2 - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				ReceivedAssetType(AssetType.BlockTypes | AssetType.Items);
				return;
			}
			throw new ArgumentOutOfRangeException();
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.BlockTypes | AssetType.Items);
		if (_upcomingServerSettings.BlockGroups == null)
		{
			_upcomingServerSettings.BlockGroups = new Dictionary<string, BlockGroup>();
		}
		foreach (KeyValuePair<string, BlockGroup> group in val.Groups)
		{
			_upcomingServerSettings.BlockGroups.Add(group.Key, group.Value);
		}
	}

	private void ProcessSupportValidationResponse(SupportValidationResponse response)
	{
		ValidateStage(ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.InteractionModule.BlockPreview.HandleSupportValidationResponse(response);
		});
	}

	private void ProcessChunkPartPacket(ChunkPart packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		Buffer.BlockCopy(packet.Part, 0, _compressedChunkBuffer, _compressedChunkBufferPosition, packet.Part.Length);
		_compressedChunkBufferPosition += packet.Part.Length;
	}

	private void ProcessSetChunkPacket(SetChunk packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		int compressedChunkBufferPosition = _compressedChunkBufferPosition;
		_compressedChunkBufferPosition = 0;
		int x = packet.X;
		int y = packet.Y;
		int z = packet.Z;
		if (compressedChunkBufferPosition == 0)
		{
			_gameInstance.MapModule.SetChunkBlocks(x, y, z, null, _upcomingBlockTypes.Length - 1, (byte[])(object)packet.LocalLight, (byte[])(object)packet.GlobalLight);
			return;
		}
		Buffer.BlockCopy(_compressedChunkBuffer, 0, _decompressedChunkBuffer, 0, _decompressedChunkBuffer.Length);
		_gameInstance.MapModule.SetChunkBlocks(x, y, z, _decompressedChunkBuffer, _upcomingBlockTypes.Length - 1, (byte[])(object)packet.LocalLight, (byte[])(object)packet.GlobalLight);
	}

	private void ProcessSetChunkHeightmapPacket(SetChunkHeightmap packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		int x = packet.X;
		int z = packet.Z;
		ushort[] array3;
		using (MemoryStream input = new MemoryStream((byte[])(object)packet.Heightmap))
		{
			using BinaryReader binaryReader = new BinaryReader(input);
			ushort num = binaryReader.ReadUInt16();
			ushort[] array = new ushort[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = binaryReader.ReadUInt16();
			}
			int num2 = binaryReader.ReadInt32();
			byte[] array2 = new byte[num2];
			binaryReader.Read(array2, 0, num2);
			_bitFieldArr.Set(array2);
			array3 = new ushort[1024];
			for (int j = 0; j < 1024; j++)
			{
				array3[j] = array[(ushort)_bitFieldArr.Get(j)];
			}
		}
		_gameInstance.MapModule.SetChunkColumnHeights(x, z, array3);
	}

	private void ProcessSetChunkTintmapPacket(SetChunkTintmap packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		int x = packet.X;
		int z = packet.Z;
		uint[] array3;
		using (MemoryStream input = new MemoryStream((byte[])(object)packet.Tintmap))
		{
			using BinaryReader binaryReader = new BinaryReader(input);
			ushort num = binaryReader.ReadUInt16();
			uint[] array = new uint[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = binaryReader.ReadUInt32();
			}
			int num2 = binaryReader.ReadInt32();
			byte[] array2 = new byte[num2];
			binaryReader.Read(array2, 0, num2);
			_bitFieldArr.Set(array2);
			array3 = new uint[1024];
			for (int j = 0; j < 1024; j++)
			{
				array3[j] = array[_bitFieldArr.Get(j)];
			}
		}
		_gameInstance.MapModule.SetChunkColumnTints(x, z, array3);
	}

	private void ProcessSetChunkEnvironmentsPacket(SetChunkEnvironments packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		int x = packet.X;
		int z = packet.Z;
		ushort[][] array;
		using (MemoryStream input = new MemoryStream((byte[])(object)packet.Environments))
		{
			using BinaryReader binaryReader = new BinaryReader(input);
			array = new ushort[1024][];
			for (int i = 0; i < 1024; i++)
			{
				short num = binaryReader.ReadInt16();
				ushort[] array2 = new ushort[num * 2];
				for (int j = 0; j < num; j++)
				{
					array2[j * 2] = binaryReader.ReadUInt16();
					array2[j * 2 + 1] = binaryReader.ReadUInt16();
				}
				array[i] = array2;
			}
		}
		_gameInstance.MapModule.SetChunkColumnEnvironments(x, z, array);
	}

	private void ProcessSetBlockPacket(ServerSetBlock packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		if (_gameInstance.InteractionModule.BlockBreakHealth.IsEnabled)
		{
			_gameInstance.InteractionModule.BlockBreakHealth.UpdateHealth(-1, packet.X, packet.Y, packet.Z, 1f, 1f);
		}
		_gameInstance.MapModule.SetBlock(packet.X, packet.Y, packet.Z, packet.BlockId, packet.InteractionStateSound);
	}

	private void ProcessUpdateBlockDamagePacket(UpdateBlockDamage packet)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Invalid comparison between Unknown and I4
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		int block = _gameInstance.MapModule.GetBlock(packet.BlockPosition_.X, packet.BlockPosition_.Y, packet.BlockPosition_.Z, 1);
		ClientBlockType blockType = _gameInstance.MapModule.ClientBlockTypes[block];
		if ((int)blockType.DrawType > 0 && _gameInstance.InteractionModule.BlockBreakHealth.IsEnabled)
		{
			_gameInstance.InteractionModule.BlockBreakHealth.UpdateHealth(block, packet.BlockPosition_.X, packet.BlockPosition_.Y, packet.BlockPosition_.Z, 1f, packet.Damage);
		}
		if (packet.Delta >= 0f || blockType.BlockParticleSetId == null)
		{
			return;
		}
		Vector3 blockPosition = new Vector3(packet.BlockPosition_.X, packet.BlockPosition_.Y, packet.BlockPosition_.Z);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			if (_gameInstance.ParticleSystemStoreModule.TrySpawnBlockSystem(blockPosition, blockType, ClientBlockParticleEvent.Hit, out var particleSystemProxy, faceCameraYaw: true))
			{
				particleSystemProxy.Position = blockPosition + new Vector3(0.5f) + particleSystemProxy.Position;
			}
		});
	}

	private void ProcessUnloadChunk(UnloadChunk packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		_gameInstance.MapModule.UnloadChunkColumn(packet.ChunkX, packet.ChunkZ);
	}

	private void ProcessViewRadiusPacket(ViewRadius packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		int viewRadius = packet.ViewRadius_;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.MapModule.MaxServerViewRadius = viewRadius;
		});
	}

	private void ProcessSetUpdateRatePacket(SetUpdateRate packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		if (packet.UpdatesPerSecond <= 0 || packet.UpdatesPerSecond > 2048)
		{
			throw new ArgumentException($"UpdatesPerSecond is out of bounds (<=0 or >2048): ${packet.UpdatesPerSecond}");
		}
		int updatesPerSecond = packet.UpdatesPerSecond;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.ServerUpdatesPerSecond = updatesPerSecond;
		});
	}

	private void ProcessUpdateFeaturesPacket(UpdateFeatures packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			foreach (KeyValuePair<ClientFeature, bool> feature in packet.Features)
			{
				_gameInstance.ClientFeatureModule.SetFeatureEnabled(feature.Key, feature.Value);
			}
		});
	}

	private void ProcessSetTimeDilationPacket(SetTimeDilation packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		if ((double)packet.TimeDilation <= 0.01 || packet.TimeDilation > 4f)
		{
			throw new ArgumentException($"TimeDilation is out of bounds (<=0.01 or >4): ${packet.TimeDilation}");
		}
		float timeDilation = packet.TimeDilation;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.TimeDilationModifier = timeDilation;
		});
	}

	private void ProcessSetClientIdPacket(SetClientId packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		int clientId = packet.ClientId;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.SetLocalPlayerId(clientId);
		});
	}

	private void ProcessPingPacket(Ping packet)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		ValidateStage(ConnectionStage.WaitingForSetup | ConnectionStage.SettingUp | ConnectionStage.Playing);
		DateTime utcNow = DateTime.UtcNow;
		InstantData time = new InstantData(packet.Time);
		int packetId = packet.Id;
		int lastPingValueDirect = packet.LastPingValueDirect;
		_gameInstance.TimeModule.UpdatePing(time, utcNow, (PongType)1, lastPingValueDirect);
		_gameInstance.Connection.SendPacketImmediate((ProtoPacket)new Pong(packetId, TimeHelper.DateTimeToInstantData(utcNow), (PongType)1, (short)_packets.Count));
		int lastPingValueTick = packet.LastPingValueTick;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Expected O, but got Unknown
			DateTime utcNow2 = DateTime.UtcNow;
			_gameInstance.TimeModule.UpdatePing(time, utcNow2, (PongType)2, lastPingValueTick);
			_gameInstance.Connection.SendPacket((ProtoPacket)new Pong(packetId, TimeHelper.DateTimeToInstantData(utcNow2), (PongType)2, (short)_packets.Count));
		});
	}

	private void ProcessReferral(ClientReferral packet)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		ValidateStage(ConnectionStage.WaitingForSetup | ConnectionStage.SettingUp | ConnectionStage.Playing);
		string hostname = string.Copy(packet.HostTo.Host);
		short port = packet.HostTo.Port;
		Logger.Info<string, short, bool>("Wanted referral to {0} with port {1} and HDC {2}", hostname, port, packet.HardDisconnect);
		PkixCertPath val = new PkixCertPath((Stream)new MemoryStream((byte[])(object)packet.Referral.CertPath), "PEM");
		for (int i = 0; i < val.Certificates.Count; i++)
		{
			Logger.Info<int, object>("Cert {0}: {1}", i, val.Certificates[i]);
		}
		if (packet.HardDisconnect)
		{
			App app = _gameInstance.App;
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				app.GameLoading.Open(hostname, port, AppMainMenu.MainMenuPage.Minigames);
			});
		}
	}

	public PacketHandler(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
		_threadCancellationToken = _threadCancellationTokenSource.Token;
		_thread = new Thread(ProcessPacketsThreadStart)
		{
			Name = "BackgroundPacketHandler",
			IsBackground = true
		};
		_thread.Start();
	}

	protected override void DoDispose()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_threadCancellationTokenSource.Cancel();
		_thread.Join();
		if (_blobFileStream != null)
		{
			_blobFileStream.Close();
			_blobFileStream = null;
			try
			{
				File.Delete(Paths.TempAssetDownload);
			}
			catch
			{
				throw;
			}
		}
		_threadCancellationTokenSource.Dispose();
	}

	public void Receive(byte[] buffer, int payloadLength)
	{
		ProtoBinaryReader val = ProtoBinaryReader.Create(buffer, payloadLength);
		try
		{
			Receive(PacketReader.ReadPacket(val));
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void Receive(ProtoPacket packet)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected O, but got Unknown
		if (packet.GetId() == 1)
		{
			App app = _gameInstance.App;
			string reason = ((Disconnect)packet).Reason;
			_gameInstance.Engine.RunOnMainThread(_gameInstance.Engine, delegate
			{
				app.Disconnection.SetReason(reason);
			}, allowCallFromMainThread: true);
			return;
		}
		if (packet.GetId() == 2)
		{
			Ping val = (Ping)packet;
			DateTime utcNow = DateTime.UtcNow;
			_gameInstance.TimeModule.UpdatePing(val.Time, utcNow, (PongType)0, val.LastPingValueRaw);
			_gameInstance.Connection.SendPacketImmediate((ProtoPacket)new Pong(val.Id, TimeHelper.DateTimeToInstantData(utcNow), (PongType)0, (short)_packets.Count));
		}
		_packets.Add(packet, _threadCancellationToken);
	}

	private void ProcessPacketsThreadStart()
	{
		Debug.Assert(ThreadHelper.IsOnThread(_thread));
		Stopwatch stopwatch = Stopwatch.StartNew();
		ProtoPacket val = null;
		while (true)
		{
			CancellationToken threadCancellationToken = _threadCancellationToken;
			if (!threadCancellationToken.IsCancellationRequested)
			{
				try
				{
					val = _packets.Take(_threadCancellationToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				stopwatch.Restart();
				ProcessPacket(val);
				ref ConnectionToServer.PacketStat reference = ref _gameInstance.Connection.PacketStats[val.GetId()];
				if (reference.Name == null)
				{
					reference.Name = ((object)val).GetType().Name;
				}
				reference.AddReceivedTime(stopwatch.ElapsedTicks);
				continue;
			}
			break;
		}
	}

	private void ReceivedAssetType(AssetType assetType)
	{
		long elapsedMilliseconds = _connectionStopwatch.ElapsedMilliseconds;
		Logger.Info<AssetType, long>("Received AssetType {0} at {1}ms", assetType, elapsedMilliseconds);
		_receivedAssetTypes |= assetType;
		_lastAssetReceivedMs = elapsedMilliseconds;
	}

	private void FinishedReceivedAssetType(AssetType assetType)
	{
		Logger.Info<AssetType, long>("Finished handling AssetType {0} took {1}ms", assetType, _connectionStopwatch.ElapsedMilliseconds - _lastAssetReceivedMs);
	}

	private void ValidateReceivedAssets()
	{
		string text = "";
		foreach (object value in Enum.GetValues(typeof(AssetType)))
		{
			if (!_receivedAssetTypes.HasFlag((AssetType)value))
			{
				text = ((text.Length > 0) ? (text + $", {value}") : (text + $"{value}"));
			}
		}
		if (text.Length > 0)
		{
			throw new Exception("We have not received the asset types of " + text + ".");
		}
	}

	private void SetStage(ConnectionStage stage)
	{
		Logger.Info<ConnectionStage, ConnectionStage, long>("Stage {0} -> {1} took {2}ms", _stage, stage, _stageStopwatch.ElapsedMilliseconds);
		_stage = stage;
		_stageStopwatch.Restart();
	}

	private void ValidateStage(ConnectionStage stageFlags)
	{
		if (!stageFlags.HasFlag(_stage))
		{
			throw new Exception($"Received {_stageValidationPacketId} at {_stage} connection stage but expected it only during {stageFlags}.");
		}
		_stageValidationPacketId = string.Empty;
	}

	private bool ValidateEntityId(int id)
	{
		return id >= 0;
	}

	private bool ValidateFloat(float number, float min = float.NegativeInfinity, float max = float.PositiveInfinity)
	{
		if (float.IsNaN(number))
		{
			return false;
		}
		if (number < min || number > max)
		{
			return false;
		}
		return true;
	}

	private void ProcessPacket(ProtoPacket packet)
	{
		//IL_0459: Unknown result type (might be due to invalid IL or missing references)
		//IL_0463: Expected O, but got Unknown
		//IL_08d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e3: Expected O, but got Unknown
		//IL_09b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09bb: Expected O, but got Unknown
		//IL_051f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0529: Expected O, but got Unknown
		//IL_04fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0505: Expected O, but got Unknown
		//IL_050d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0517: Expected O, but got Unknown
		//IL_0c4b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c55: Expected O, but got Unknown
		//IL_0c5d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c67: Expected O, but got Unknown
		//IL_0c6f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c79: Expected O, but got Unknown
		//IL_0ca5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0caf: Expected O, but got Unknown
		//IL_0cb7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cc1: Expected O, but got Unknown
		//IL_0c93: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c9d: Expected O, but got Unknown
		//IL_0a41: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a4b: Expected O, but got Unknown
		//IL_09e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f1: Expected O, but got Unknown
		//IL_099f: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a9: Expected O, but got Unknown
		//IL_0699: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a3: Expected O, but got Unknown
		//IL_0801: Unknown result type (might be due to invalid IL or missing references)
		//IL_080b: Expected O, but got Unknown
		//IL_090f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0919: Expected O, but got Unknown
		//IL_046b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0475: Expected O, but got Unknown
		//IL_074d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0757: Expected O, but got Unknown
		//IL_0aad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab7: Expected O, but got Unknown
		//IL_0a65: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a6f: Expected O, but got Unknown
		//IL_0a77: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a81: Expected O, but got Unknown
		//IL_0ae3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aed: Expected O, but got Unknown
		//IL_0a2f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a39: Expected O, but got Unknown
		//IL_0a0b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a15: Expected O, but got Unknown
		//IL_0c81: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c8b: Expected O, but got Unknown
		//IL_0729: Unknown result type (might be due to invalid IL or missing references)
		//IL_0733: Expected O, but got Unknown
		//IL_0abf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ac9: Expected O, but got Unknown
		//IL_08c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d1: Expected O, but got Unknown
		//IL_073b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0745: Expected O, but got Unknown
		//IL_08a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ad: Expected O, but got Unknown
		//IL_0a1d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a27: Expected O, but got Unknown
		//IL_0891: Unknown result type (might be due to invalid IL or missing references)
		//IL_089b: Expected O, but got Unknown
		//IL_0a89: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a93: Expected O, but got Unknown
		//IL_075f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0769: Expected O, but got Unknown
		//IL_09f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a03: Expected O, but got Unknown
		//IL_0c15: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c1f: Expected O, but got Unknown
		//IL_0c27: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c31: Expected O, but got Unknown
		//IL_0c39: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c43: Expected O, but got Unknown
		//IL_0531: Unknown result type (might be due to invalid IL or missing references)
		//IL_053b: Expected O, but got Unknown
		//IL_08eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f5: Expected O, but got Unknown
		//IL_0543: Unknown result type (might be due to invalid IL or missing references)
		//IL_054d: Expected O, but got Unknown
		//IL_0cdb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce5: Expected O, but got Unknown
		//IL_0b07: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b11: Expected O, but got Unknown
		//IL_0af5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aff: Expected O, but got Unknown
		//IL_048f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0499: Expected O, but got Unknown
		//IL_087f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0889: Expected O, but got Unknown
		//IL_06f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fd: Expected O, but got Unknown
		//IL_04e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f3: Expected O, but got Unknown
		//IL_0933: Unknown result type (might be due to invalid IL or missing references)
		//IL_093d: Expected O, but got Unknown
		//IL_06ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b5: Expected O, but got Unknown
		//IL_06e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06eb: Expected O, but got Unknown
		//IL_06bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c7: Expected O, but got Unknown
		//IL_06cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d9: Expected O, but got Unknown
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_0451: Expected O, but got Unknown
		//IL_0771: Unknown result type (might be due to invalid IL or missing references)
		//IL_077b: Expected O, but got Unknown
		//IL_0945: Unknown result type (might be due to invalid IL or missing references)
		//IL_094f: Expected O, but got Unknown
		//IL_0ced: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cf7: Expected O, but got Unknown
		//IL_0957: Unknown result type (might be due to invalid IL or missing references)
		//IL_0961: Expected O, but got Unknown
		//IL_0a53: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a5d: Expected O, but got Unknown
		//IL_09c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_09cd: Expected O, but got Unknown
		//IL_0423: Unknown result type (might be due to invalid IL or missing references)
		//IL_042d: Expected O, but got Unknown
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		//IL_041b: Expected O, but got Unknown
		//IL_08b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_08bf: Expected O, but got Unknown
		//IL_0849: Unknown result type (might be due to invalid IL or missing references)
		//IL_0853: Expected O, but got Unknown
		//IL_085b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0865: Expected O, but got Unknown
		//IL_0837: Unknown result type (might be due to invalid IL or missing references)
		//IL_0841: Expected O, but got Unknown
		//IL_0ad1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0adb: Expected O, but got Unknown
		//IL_0675: Unknown result type (might be due to invalid IL or missing references)
		//IL_067f: Expected O, but got Unknown
		//IL_09d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09df: Expected O, but got Unknown
		//IL_0d0b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d15: Expected O, but got Unknown
		//IL_0b4f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b59: Expected O, but got Unknown
		//IL_0b3d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b47: Expected O, but got Unknown
		//IL_0b2b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b35: Expected O, but got Unknown
		//IL_0b61: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b6b: Expected O, but got Unknown
		//IL_0b19: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b23: Expected O, but got Unknown
		//IL_0717: Unknown result type (might be due to invalid IL or missing references)
		//IL_0721: Expected O, but got Unknown
		//IL_0d1a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d24: Expected O, but got Unknown
		//IL_0555: Unknown result type (might be due to invalid IL or missing references)
		//IL_055f: Expected O, but got Unknown
		//IL_0705: Unknown result type (might be due to invalid IL or missing references)
		//IL_070f: Expected O, but got Unknown
		//IL_0687: Unknown result type (might be due to invalid IL or missing references)
		//IL_0691: Expected O, but got Unknown
		//IL_062d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0637: Expected O, but got Unknown
		//IL_0663: Unknown result type (might be due to invalid IL or missing references)
		//IL_066d: Expected O, but got Unknown
		//IL_0651: Unknown result type (might be due to invalid IL or missing references)
		//IL_065b: Expected O, but got Unknown
		//IL_063f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0649: Expected O, but got Unknown
		//IL_097b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0985: Expected O, but got Unknown
		//IL_07ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f9: Expected O, but got Unknown
		//IL_07dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e7: Expected O, but got Unknown
		//IL_0783: Unknown result type (might be due to invalid IL or missing references)
		//IL_078d: Expected O, but got Unknown
		//IL_05e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ef: Expected O, but got Unknown
		//IL_061b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0625: Expected O, but got Unknown
		//IL_058b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0595: Expected O, but got Unknown
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_043f: Expected O, but got Unknown
		//IL_0bcd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bd7: Expected O, but got Unknown
		//IL_0567: Unknown result type (might be due to invalid IL or missing references)
		//IL_0571: Expected O, but got Unknown
		//IL_05f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0601: Expected O, but got Unknown
		//IL_0cc9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cd3: Expected O, but got Unknown
		//IL_05af: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b9: Expected O, but got Unknown
		//IL_0bbb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bc5: Expected O, but got Unknown
		//IL_0b97: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ba1: Expected O, but got Unknown
		//IL_0bf1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bfb: Expected O, but got Unknown
		//IL_0c03: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c0d: Expected O, but got Unknown
		//IL_0ba9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bb3: Expected O, but got Unknown
		//IL_0b85: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b8f: Expected O, but got Unknown
		//IL_0cfc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d06: Expected O, but got Unknown
		//IL_0795: Unknown result type (might be due to invalid IL or missing references)
		//IL_079f: Expected O, but got Unknown
		//IL_0969: Unknown result type (might be due to invalid IL or missing references)
		//IL_0973: Expected O, but got Unknown
		//IL_0d29: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d33: Expected O, but got Unknown
		//IL_0825: Unknown result type (might be due to invalid IL or missing references)
		//IL_082f: Expected O, but got Unknown
		//IL_0813: Unknown result type (might be due to invalid IL or missing references)
		//IL_081d: Expected O, but got Unknown
		//IL_0921: Unknown result type (might be due to invalid IL or missing references)
		//IL_092b: Expected O, but got Unknown
		//IL_08fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0907: Expected O, but got Unknown
		//IL_0609: Unknown result type (might be due to invalid IL or missing references)
		//IL_0613: Expected O, but got Unknown
		//IL_0bdf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0be9: Expected O, but got Unknown
		//IL_05c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cb: Expected O, but got Unknown
		//IL_07a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b1: Expected O, but got Unknown
		//IL_07b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c3: Expected O, but got Unknown
		//IL_086d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0877: Expected O, but got Unknown
		//IL_059d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a7: Expected O, but got Unknown
		//IL_05d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05dd: Expected O, but got Unknown
		//IL_098d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0997: Expected O, but got Unknown
		//IL_0b73: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b7d: Expected O, but got Unknown
		//IL_07cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d5: Expected O, but got Unknown
		//IL_0579: Unknown result type (might be due to invalid IL or missing references)
		//IL_0583: Expected O, but got Unknown
		//IL_0a9b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa5: Expected O, but got Unknown
		//IL_04d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e1: Expected O, but got Unknown
		//IL_04c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cf: Expected O, but got Unknown
		//IL_03ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0409: Expected O, but got Unknown
		//IL_04b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bd: Expected O, but got Unknown
		//IL_04a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ab: Expected O, but got Unknown
		//IL_047d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0487: Expected O, but got Unknown
		Debug.Assert(ThreadHelper.IsOnThread(_thread));
		_stageValidationPacketId = ((object)packet).GetType().Name;
		switch (packet.GetId())
		{
		case 239:
			ProcessViewRadiusPacket((ViewRadius)packet);
			break;
		case 154:
			ProcessSetUpdateRatePacket((SetUpdateRate)packet);
			break;
		case 153:
			ProcessSetTimeDilationPacket((SetTimeDilation)packet);
			break;
		case 200:
			ProcessUpdateFeaturesPacket((UpdateFeatures)packet);
			break;
		case 145:
			ProcessSetClientIdPacket((SetClientId)packet);
			break;
		case 2:
			ProcessPingPacket((Ping)packet);
			break;
		case 90:
			ProcessReferral((ClientReferral)packet);
			break;
		case 242:
			ProcessWorldSettingsPacket((WorldSettings)packet);
			break;
		case 136:
			ProcessServerInfoPacket((ServerInfo)packet);
			break;
		case 241:
			ProcessWorldLoadProgressPacket((WorldLoadProgress)packet);
			break;
		case 240:
			ProcessWorldLoadFinishedPacket((WorldLoadFinished)packet);
			break;
		case 237:
			ProcessUpdateWorldMapSettingsPacket((UpdateWorldMapSettings)packet);
			break;
		case 236:
			ProcessUpdateWorldMapPacket((UpdateWorldMap)packet);
			break;
		case 139:
			ProcessServerTagsPacket((ServerTags)packet);
			break;
		case 55:
			ProcessAssetInitializePacket((AssetInitialize)packet);
			break;
		case 56:
			ProcessAssetPartPacket((AssetPart)packet);
			break;
		case 54:
			ProcessAssetFinalizePacket((AssetFinalize)packet);
			break;
		case 125:
			ProcessRemoveAssetsPacket((RemoveAssets)packet);
			break;
		case 128:
			ProcessRequestCommonAssetsRebuildPacket((RequestCommonAssetsRebuild)packet);
			break;
		case 186:
			ProcessUpdateAmbienceFXPacket((UpdateAmbienceFX)packet);
			break;
		case 202:
			ProcessUpdateFluidFXPacket((UpdateFluidFX)packet);
			break;
		case 234:
			ProcessUpdateWeathersPacket((UpdateWeathers)packet);
			break;
		case 199:
			ProcessUpdateEnvironmentsPacket((UpdateEnvironments)packet);
			break;
		case 229:
			ProcessUpdateTranslationsPacket((UpdateTranslations)packet);
			break;
		case 205:
			ProcessUpdateInteractions((UpdateInteractions)packet);
			break;
		case 224:
			ProcessUpdateRootInteractions((UpdateRootInteractions)packet);
			break;
		case 230:
			ProcessUpdateUnarmedInteractions((UpdateUnarmedInteractions)packet);
			break;
		case 197:
			ProcessUpdateEntityStatTypes((UpdateEntityStatTypes)packet);
			break;
		case 203:
			ProcessUpdateHitboxCollisionConfig((UpdateHitboxCollisionConfig)packet);
			break;
		case 222:
			ProcessUpdateRepulsionConfig((UpdateRepulsionConfig)packet);
			break;
		case 198:
			ProcessUpdateEntityUIComponents((UpdateEntityUIComponents)packet);
			break;
		case 189:
			ProcessUpdateBlockHitboxesPacket((UpdateBlockHitboxes)packet);
			break;
		case 192:
			ProcessUpdateBlockTypesPacket((UpdateBlockTypes)packet);
			break;
		case 191:
			ProcessUpdateBlockSoundSets((UpdateBlockSoundSets)packet);
			break;
		case 190:
			ProcessUpdateBlockParticleSets((UpdateBlockParticleSets)packet);
			break;
		case 166:
			ProcessSupportValidationResponse((SupportValidationResponse)packet);
			break;
		case 188:
			ProcessUpdateBlockGroups((UpdateBlockGroups)packet);
			break;
		case 85:
			ProcessChunkPartPacket((ChunkPart)packet);
			break;
		case 141:
			ProcessSetChunkPacket((SetChunk)packet);
			break;
		case 143:
			ProcessSetChunkHeightmapPacket((SetChunkHeightmap)packet);
			break;
		case 144:
			ProcessSetChunkTintmapPacket((SetChunkTintmap)packet);
			break;
		case 142:
			ProcessSetChunkEnvironmentsPacket((SetChunkEnvironments)packet);
			break;
		case 138:
			ProcessSetBlockPacket((ServerSetBlock)packet);
			break;
		case 187:
			ProcessUpdateBlockDamagePacket((UpdateBlockDamage)packet);
			break;
		case 184:
			ProcessUnloadChunk((UnloadChunk)packet);
			break;
		case 102:
			ProcessEntityUpdatesPacket((EntityUpdates)packet);
			break;
		case 106:
			ProcessJoinWorldPacket((JoinWorld)packet);
			break;
		case 91:
			ProcessClientTeleportPacket((ClientTeleport)packet);
			break;
		case 117:
			ProcessPlayAnimation((PlayAnimation)packet);
			break;
		case 147:
			ProcessSetEntitySeed((SetEntitySeed)packet);
			break;
		case 196:
			ProcessUpdateEntityEffectsPacket((UpdateEntityEffects)packet);
			break;
		case 214:
			ProcessUpdateModelVFXsPacket((UpdateModelvfxs)packet);
			break;
		case 226:
			ProcessUpdateTimePacket((UpdateTime)packet);
			break;
		case 227:
			ProcessUpdateTimeSettingsPacket((UpdateTimeSettings)packet);
			break;
		case 233:
			ProcessUpdateWeatherPacket((UpdateWeather)packet);
			break;
		case 195:
			ProcessUpdateEditorWeatherOverride((UpdateEditorWeatherOverride)packet);
			break;
		case 194:
			ProcessUpdateEditorTimeOverride((UpdateEditorTimeOverride)packet);
			break;
		case 86:
			ProcessClearEditorTimeOverride((ClearEditorTimeOverride)packet);
			break;
		case 219:
			ProcessUpdateParticleSystemsPacket((UpdateParticleSystems)packet);
			break;
		case 218:
			ProcessUpdateParticleSpawnersPacket((UpdateParticleSpawners)packet);
			break;
		case 162:
			ProcessSpawnParticleSystemPacket((SpawnParticleSystem)packet);
			break;
		case 160:
			ProcessSpawnBlockParticleSystem((SpawnBlockParticleSystem)packet);
			break;
		case 161:
			ProcessSpawnModelParticles((SpawnModelParticles)packet);
			break;
		case 228:
			ProcessUpdateTrailsPacket((UpdateTrails)packet);
			break;
		case 137:
			ProcessServerMessagePacket((ServerMessage)packet);
			break;
		case 113:
			ProcessNotificationPacket((Notification)packet);
			break;
		case 108:
			ProcessKillFeedMessage((KillFeedMessage)packet);
			break;
		case 156:
			ProcessShowEventTitlePacket((ShowEventTitle)packet);
			break;
		case 104:
			ProcessHideEventTitlePacket((HideEventTitle)packet);
			break;
		case 4:
			ProcessAddToPlayerListPacket((AddToPlayerList)packet);
			break;
		case 126:
			ProcessRemoveFromPlayerListPacket((RemoveFromPlayerList)packet);
			break;
		case 221:
			ProcessUpdatePlayerListPacket((UpdatePlayerList)packet);
			break;
		case 87:
			ProcessClearPlayerListPacket((ClearPlayerList)packet);
			break;
		case 220:
			ProcessInventoryPacket((UpdatePlayerInventory)packet);
			break;
		case 140:
			ProcessSetActiveSlot((SetActiveSlot)packet);
			break;
		case 148:
			ProcessSetGameModePacket((SetGameMode)packet);
			break;
		case 150:
			ProcessSetMovementStates((SetMovementStates)packet);
			break;
		case 215:
			ProcessUpdateMovementSettings((UpdateMovementSettings)packet);
			break;
		case 193:
			ProcessCameraShakeProfiles((UpdateCameraShake)packet);
			break;
		case 231:
			ProcessViewBobbingProfiles((UpdateViewBobbing)packet);
			break;
		case 82:
			ProcessChangeVelocity((ChangeVelocity)packet);
			break;
		case 5:
			ProcessApplyKnockback((ApplyKnockback)packet);
			break;
		case 152:
			ProcessSetServerCamera((SetServerCamera)packet);
			break;
		case 168:
			ProcessSyncInteractionChain((SyncInteractionChain)packet);
			break;
		case 81:
			ProcessCancelInteractionChain((CancelInteractionChain)packet);
			break;
		case 118:
			ProcessPlayInteractionFor((PlayInteractionFor)packet);
			break;
		case 98:
			ProcessDisplayDebug((DisplayDebug)packet);
			break;
		case 110:
			ProcessMountNpc((MountNPC)packet);
			break;
		case 97:
			ProcessDismountNpc((DismountNPC)packet);
			break;
		case 80:
			ProcessCameraEffect((CameraShakeEffect)packet);
			break;
		case 151:
			ProcessSetPagePacket((SetPage)packet);
			break;
		case 93:
			ProcessCustomHudPacket((CustomHud)packet);
			break;
		case 94:
			ProcessCustomPagePacket((CustomPage)packet);
			break;
		case 115:
			ProcessOpenWindow((OpenWindow)packet);
			break;
		case 235:
			ProcessUpdateWindow((UpdateWindow)packet);
			break;
		case 92:
			ProcessCloseWindow((CloseWindow)packet);
			break;
		case 103:
			ProcessFailureReply((FailureReply)packet);
			break;
		case 164:
			ProcessSuccessReply((SuccessReply)packet);
			break;
		case 96:
			ProcessDamageInfo((DamageInfo)packet);
			break;
		case 132:
			ProcessReticleEvent((ReticleEvent)packet);
			break;
		case 131:
			ProcessResetUserInterfaceState((ResetUserInterfaceState)packet);
			break;
		case 183:
			ProcessTriggerEditorUpdateScriptReply((TriggerEditorUpdateScriptReply)packet);
			break;
		case 179:
			ProcessTriggerEditorRequestScriptsReply((TriggerEditorRequestScriptsReply)packet);
			break;
		case 177:
			ProcessTriggerEditorRequestScriptReply((TriggerEditorRequestScriptReply)packet);
			break;
		case 175:
			ProcessTriggerEditorRequestBlockReply((TriggerEditorRequestBlockReply)packet);
			break;
		case 181:
			ProcessTriggerEditorUpdateBlockReply((TriggerEditorUpdateBlockReply)packet);
			break;
		case 232:
			ProcessUpdateVisibleHudComponents((UpdateVisibleHudComponents)packet);
			break;
		case 211:
			ProcessUpdateKnownRecipes((UpdateKnownRecipes)packet);
			break;
		case 207:
			ProcessUpdateItemPlayerAnimationsPacket((UpdateItemPlayerAnimations)packet);
			break;
		case 210:
			ProcessUpdateItemsPacket((UpdateItems)packet);
			break;
		case 206:
			ProcessUpdateItemCategoriesPacket((UpdateItemCategories)packet);
			break;
		case 201:
			ProcessUpdateFieldcraftCategoriesPacket((UpdateFieldcraftCategories)packet);
			break;
		case 223:
			ProcessUpdateResourceTypes((UpdateResourceTypes)packet);
			break;
		case 208:
			ProcessUpdateItemQualitiesPacket((UpdateItemQualities)packet);
			break;
		case 209:
			ProcessUpdateItemReticles((UpdateItemReticles)packet);
			break;
		case 119:
			ProcessPlaySoundEvent2DPacket((PlaySoundEvent2D)packet);
			break;
		case 120:
			ProcessPlaySoundEvent3DPacket((PlaySoundEvent3D)packet);
			break;
		case 121:
			ProcessPlaySoundEventEntityPacket((PlaySoundEventEntity)packet);
			break;
		case 58:
			ProcessAuth2Packet((Auth2)packet);
			break;
		case 60:
			ProcessAuth4Packet((Auth4)packet);
			break;
		case 62:
			ProcessAuth6Packet((Auth6)packet);
			break;
		case 100:
			ProcessEditorBlocksChangePacket((EditorBlocksChange)packet);
			break;
		case 78:
			ProcessBuilderToolShowAnchorPacket((BuilderToolShowAnchor)packet);
			break;
		case 67:
			ProcessBuilderToolHideAnchorPacket((BuilderToolHideAnchors)packet);
			break;
		case 73:
			ProcessBuilderToolSelectionToolReplyWithClipboard((BuilderToolSelectionToolReplyWithClipboard)packet);
			break;
		case 204:
			ProcessUpdateImmersiveViewPacket((UpdateImmersiveView)packet);
			break;
		case 130:
			ProcessRequestServerAccess((RequestServerAccess)packet);
			break;
		case 149:
			ProcessSetMachinimaActorModelPacket((SetMachinimaActorModel)packet);
			break;
		case 213:
			ProcessUpdateMachinimaScene((UpdateMachinimaScene)packet);
			break;
		case 172:
			ProcessTrackOrUpdateObjective((TrackOrUpdateObjective)packet);
			break;
		case 185:
			ProcessUntrackObjective((UntrackObjective)packet);
			break;
		case 216:
			ProcessUpdateObjectiveTask((UpdateObjectiveTask)packet);
			break;
		default:
			if (_unhandledPacketTypes.Add(((object)packet).GetType().Name))
			{
				Logger.Warn("Received unhandled packet type: {0}", ((object)packet).GetType().Name);
			}
			_stageValidationPacketId = string.Empty;
			break;
		}
		if (_stageValidationPacketId != string.Empty)
		{
			throw new Exception("Connection stage hasn't been validated for " + _stageValidationPacketId);
		}
	}

	public int AddPendingCallback<T>(Disposable disposable, Action<FailureReply, T> callback) where T : ProtoPacket
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected O, but got Unknown
		if (_pendingCallbacks.Count > 1000)
		{
			if ((DateTime.Now - _lastCallbackWarning).TotalSeconds > 5.0)
			{
				_lastCallbackWarning = DateTime.Now;
				Logger.Warn("There are currently more than 1000 pending packet callbacks. Removing oldest callback...");
			}
			int num = _pendingCallbacks.Keys.First();
			_pendingCallbacks.TryRemove(num, out var value);
			value.Callback(new FailureReply(num, BsonHelper.ToBson(JToken.FromObject((object)FormattedMessage.FromMessageId("ui.general.callback.cancelled")))), null);
		}
		int num2 = Interlocked.Add(ref _lastCallbackToken, 1);
		_pendingCallbacks[num2] = new PendingCallback
		{
			Callback = delegate(FailureReply err, ProtoPacket res)
			{
				callback(err, (T)(object)res);
			},
			Disposable = disposable
		};
		return num2;
	}

	private void CallPendingCallback(int token, ProtoPacket responsePacket, FailureReply failurePacket)
	{
		if (_pendingCallbacks.TryRemove(token, out var value) && !value.Disposable.Disposed)
		{
			value.Callback?.Invoke(failurePacket, responsePacket);
		}
	}

	private void ProcessEntityUpdatesPacket(EntityUpdates packet)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		EntityUpdates entityUpdates = new EntityUpdates(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_0143: Expected I4, but got Unknown
			//IL_022b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0230: Unknown result type (might be due to invalid IL or missing references)
			//IL_0232: Unknown result type (might be due to invalid IL or missing references)
			//IL_0234: Unknown result type (might be due to invalid IL or missing references)
			//IL_0236: Unknown result type (might be due to invalid IL or missing references)
			//IL_0291: Expected I4, but got Unknown
			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
			if (entityUpdates.Removed != null)
			{
				int[] removed = entityUpdates.Removed;
				foreach (int networkId in removed)
				{
					_gameInstance.EntityStoreModule.Despawn(networkId);
				}
			}
			if (entityUpdates.Updates != null)
			{
				EntityUpdate[] updates = entityUpdates.Updates;
				foreach (EntityUpdate val in updates)
				{
					Entity entity;
					bool flag = _gameInstance.EntityStoreModule.Spawn(val.NetworkId, out entity);
					bool flag2 = false;
					bool flag3 = false;
					Model val2 = null;
					Equipment val3 = null;
					Guid? guid = null;
					if (val.Removed != null)
					{
						ComponentUpdateType[] removed2 = val.Removed;
						foreach (ComponentUpdateType val4 in removed2)
						{
							ComponentUpdateType val5 = val4;
							ComponentUpdateType val6 = val5;
							switch ((int)val6)
							{
							case 3:
							case 4:
							case 5:
							case 6:
							case 7:
							case 8:
							case 9:
							case 10:
							case 19:
								throw new NotImplementedException($"Removing {val4} component isn't supported!");
							case 0:
								entity.SetName("", nameTagVisible: false);
								break;
							case 1:
								entity.ClearUIComponents();
								break;
							case 2:
								entity.ClearCombatTexts();
								break;
							case 11:
								entity.ClearEffects();
								break;
							case 12:
								entity.Interactions.Clear();
								break;
							case 14:
								entity.SetUsable(usable: false);
								break;
							case 15:
								entity.SetIsTangible(isTangible: true);
								break;
							case 16:
								entity.SetInvulnerable(isInvulnerable: false);
								break;
							case 13:
								entity.DynamicLight = ColorRgb.Zero;
								flag2 = true;
								break;
							case 17:
								entity.HitboxCollisionConfigIndex = -1;
								break;
							case 18:
								entity.RepulsionConfigIndex = -1;
								break;
							default:
								throw new ArgumentOutOfRangeException();
							}
						}
					}
					if (val.Updates != null)
					{
						ComponentUpdate[] updates2 = val.Updates;
						foreach (ComponentUpdate val7 in updates2)
						{
							ComponentUpdateType type = val7.Type;
							ComponentUpdateType val8 = type;
							switch ((int)val8)
							{
							case 19:
								guid = val7.PredictionId;
								break;
							case 0:
								entity.SetName(val7.Nameplate_.Text, nameTagVisible: true);
								break;
							case 1:
								entity.SetUIComponents(val7.EntityUIComponents);
								break;
							case 2:
								entity.AddCombatText(val7.CombatTextUpdate_);
								break;
							case 14:
								entity.SetUsable(usable: true);
								break;
							case 15:
								entity.SetIsTangible(isTangible: false);
								break;
							case 16:
								entity.SetInvulnerable(isInvulnerable: true);
								break;
							case 3:
								val2 = val7.Model_;
								break;
							case 4:
								entity.PlayerSkin = val7.Skin;
								flag3 = true;
								break;
							case 5:
								entity.SetItem(val7.Item_);
								break;
							case 6:
								entity.SetBlock(val7.BlockId, val7.BlockEntityScale);
								break;
							case 7:
								val3 = val7.Equipment_;
								break;
							case 8:
								entity.UpdateEntityStats(val7.EntityStatUpdates);
								break;
							case 9:
							{
								Vector3 position = entity.Position;
								Vector3 bodyOrientation = entity.BodyOrientation;
								Vector3 lookOrientation = entity.LookOrientation;
								ModelTransformHelper.Decompose(val7.Transform, ref position, ref bodyOrientation, ref lookOrientation);
								if (flag)
								{
									entity.SetSpawnTransform(position, bodyOrientation, lookOrientation);
									if (val.NetworkId == _gameInstance.LocalPlayerNetworkId)
									{
										entity.SoundObjectReference = AudioDevice.PlayerSoundObjectReference;
									}
									else
									{
										_gameInstance.AudioModule.TryRegisterSoundObject(position, bodyOrientation, ref entity.SoundObjectReference);
									}
								}
								else
								{
									if (_gameInstance.EntityStoreModule.MountEntityLocalId == val.NetworkId)
									{
										return;
									}
									entity.SetTransform(position, bodyOrientation, lookOrientation);
								}
								break;
							}
							case 10:
								if (val.NetworkId != _gameInstance.EntityStoreModule.MountEntityLocalId)
								{
									ClientMovementStatesProtocolHelper.Parse(val7.MovementStates_, ref entity.ServerMovementStates);
								}
								break;
							case 11:
								_gameInstance.EntityStoreModule.UpdateEffects(val.NetworkId, val7.EntityEffectUpdates);
								break;
							case 12:
								entity.Interactions = val7.Interactions;
								break;
							case 13:
								ClientItemBaseProtocolInitializer.ParseLightColor(val7.DynamicLight, ref entity.DynamicLight);
								flag2 = true;
								break;
							case 17:
								entity.HitboxCollisionConfigIndex = val7.HitboxCollisionConfigIndex;
								break;
							case 18:
								entity.RepulsionConfigIndex = val7.RepulsionConfigIndex;
								break;
							case 20:
							{
								int[] soundEventIds = val7.SoundEventIds;
								foreach (int value in soundEventIds)
								{
									uint networkWwiseId = ResourceManager.GetNetworkWwiseId(value);
									_gameInstance.EntityStoreModule.QueueSoundEvent(networkWwiseId, entity.NetworkId);
								}
								break;
							}
							default:
								throw new ArgumentOutOfRangeException();
							}
						}
					}
					if (guid.HasValue)
					{
						entity.Predictable = true;
						_gameInstance.EntityStoreModule.MapPrediction(guid.Value, entity);
					}
					if (val2 != null || ((val3?.ArmorIds != null || flag3) && entity.ModelPacket != null))
					{
						if (!flag3 && val2 != null)
						{
							entity.PlayerSkin = null;
						}
						entity.SetCharacterModel(val2, val3?.ArmorIds);
						flag2 = true;
					}
					if (val3 != null && val3.RightHandItemId != null && val3?.LeftHandItemId != null)
					{
						string newItemId = ((val3.RightHandItemId == "Empty") ? null : val3.RightHandItemId);
						string newSecondaryItemId = ((val3.LeftHandItemId == "Empty") ? null : val3.LeftHandItemId);
						entity.ChangeCharacterItem(newItemId, newSecondaryItemId);
						flag2 = true;
						if (entity == _gameInstance.LocalPlayer)
						{
							Logger.Warn($"A {(object)(ComponentUpdateType)7} packet had been received on the local player");
						}
					}
					if (flag2)
					{
						entity.UpdateLight();
					}
				}
			}
		});
	}

	private void ProcessJoinWorldPacket(JoinWorld packet)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		Logger.Info("Received JoinWorldPacket");
		JoinWorld joinWorld = new JoinWorld(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.CharacterControllerModule.MovementController.MovementEnabled = false;
			Logger.Info("ProcessJoinWorldPacket: FadeInOut is {0}", joinWorld.FadeInOut);
			_gameInstance.PrepareJoiningWorld(joinWorld);
		});
	}

	private void ProcessClientTeleportPacket(ClientTeleport packet)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		ClientTeleport clientTeleport = new ClientTeleport(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Expected O, but got Unknown
			//IL_0143: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Expected O, but got Unknown
			Vector3 position = _gameInstance.LocalPlayer.Position;
			Vector3 bodyOrientation = _gameInstance.LocalPlayer.BodyOrientation;
			Vector3 lookOrientation = _gameInstance.LocalPlayer.LookOrientation;
			ModelTransformHelper.Decompose(clientTeleport.ModelTransform_, ref position, ref bodyOrientation, ref lookOrientation);
			_gameInstance.LocalPlayer.SetTransform(position, bodyOrientation, lookOrientation);
			_gameInstance.LocalPlayer.SkipTransformLerp();
			_gameInstance.CharacterControllerModule.MovementController.InvalidateState();
			ClientMovement val = new ClientMovement();
			val.MovementStates_ = ClientMovementStatesProtocolHelper.ToPacket(ref _gameInstance.CharacterControllerModule.MovementController.MovementStates);
			val.AbsolutePosition = _gameInstance.LocalPlayer.Position.ToPositionPacket();
			val.BodyOrientation = _gameInstance.LocalPlayer.BodyOrientation.ToDirectionPacket();
			val.LookOrientation = _gameInstance.LocalPlayer.LookOrientation.ToDirectionPacket();
			val.TeleportAck_ = new TeleportAck(clientTeleport.TeleportId);
			_gameInstance.Connection.SendPacket((ProtoPacket)(object)val);
		});
	}

	private void ProcessPlayAnimation(PlayAnimation packet)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		PlayAnimation playAnimation = new PlayAnimation(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Invalid comparison between Unknown and I4
			//IL_011d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0123: Invalid comparison between Unknown and I4
			//IL_0177: Unknown result type (might be due to invalid IL or missing references)
			Entity entity = _gameInstance.EntityStoreModule.GetEntity(playAnimation.EntityId);
			if (entity != null)
			{
				if (entity.ModelRenderer == null)
				{
					Logger.Warn("Received server play animation packet for entity {0} with no model", playAnimation.EntityId);
				}
				else
				{
					entity.ClearPassiveAnimationData();
					if (playAnimation.ItemAnimationsId != null && (int)playAnimation.Slot == 2)
					{
						EntityAnimation value = null;
						if (playAnimation.AnimationId != null && _gameInstance.ItemLibraryModule.GetItemPlayerAnimation(playAnimation.ItemAnimationsId, out var ret))
						{
							ret?.Animations?.TryGetValue(playAnimation.AnimationId, out value);
						}
						if (value == null)
						{
							value = EntityAnimation.Empty;
						}
						entity.SetActionAnimation(value);
					}
					else
					{
						if ((int)playAnimation.Slot == 1 && entity.PredictedStatusCount > 0)
						{
							entity.PredictedStatusCount--;
							if (playAnimation.AnimationId == "Hurt")
							{
								return;
							}
						}
						entity.SetServerAnimation(playAnimation.AnimationId, playAnimation.Slot, 0f);
					}
				}
			}
		});
	}

	private void ProcessSetEntitySeed(SetEntitySeed packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		int entitySeed = packet.EntitySeed;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			Entity.ServerEntitySeed = entitySeed;
		});
	}

	private void ProcessUpdateEntityEffectsPacket(UpdateEntityEffects packet)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Invalid comparison between Unknown and I4
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		UpdateEntityEffects updateEntityEffects = new UpdateEntityEffects(packet);
		UpdateType type = updateEntityEffects.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] EntityEffects: Starting {updateEntityEffects.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if (updateEntityEffects.MaxId > _entityEffects.Length)
				{
					Array.Resize(ref _entityEffects, updateEntityEffects.MaxId);
				}
				if ((int)updateEntityEffects.Type == 1)
				{
					foreach (KeyValuePair<int, EntityEffect> entityEffect in updateEntityEffects.EntityEffects)
					{
						_entityEffects[entityEffect.Key] = entityEffect.Value;
					}
				}
				else
				{
					foreach (KeyValuePair<int, EntityEffect> entityEffect2 in updateEntityEffects.EntityEffects)
					{
						_entityEffects[entityEffect2.Key] = null;
					}
				}
				_gameInstance.EntityStoreModule.PrepareEntityEffects(_entityEffects, out var upcomingEntityEffects);
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_004c: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.EntityStoreModule.SetupEntityEffects(upcomingEntityEffects);
					_gameInstance.App.DevTools.Info($"[AssetUpdate] EntityEffects: Finished {updateEntityEffects.Type} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)updateEntityEffects).GetType().Name} at {_stage} with type {updateEntityEffects.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.EntityEffects);
		if (_entityEffects == null)
		{
			_entityEffects = (EntityEffect[])(object)new EntityEffect[updateEntityEffects.MaxId];
		}
		foreach (KeyValuePair<int, EntityEffect> entityEffect3 in updateEntityEffects.EntityEffects)
		{
			_entityEffects[entityEffect3.Key] = entityEffect3.Value;
		}
		FinishedReceivedAssetType(AssetType.EntityEffects);
	}

	private void ProcessUpdateTranslationsPacket(UpdateTranslations packet)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Expected I4, but got Unknown
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		UpdateType updateType = packet.Type;
		Dictionary<string, string> translations = new Dictionary<string, string>(packet.Translations.Count);
		foreach (KeyValuePair<string, string> translation in packet.Translations)
		{
			translations[string.Copy(translation.Key)] = string.Copy(translation.Value);
		}
		UpdateType val = updateType;
		UpdateType val2 = val;
		switch ((int)val2)
		{
		case 0:
			ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
			if (_stage == ConnectionStage.SettingUp)
			{
				ReceivedAssetType(AssetType.Translations);
			}
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				_gameInstance.App.Interface.SetServerMessages(translations);
			});
			if (_stage == ConnectionStage.SettingUp)
			{
				FinishedReceivedAssetType(AssetType.Translations);
			}
			break;
		case 1:
		{
			ValidateStage(ConnectionStage.Playing);
			_gameInstance.App.DevTools.Info($"[AssetUpdate] Translations: Starting {updateType}");
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_0051: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.App.Interface.AddServerMessages(translations);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] Translations: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch2.ElapsedMilliseconds)}");
			});
			break;
		}
		case 2:
		{
			ValidateStage(ConnectionStage.Playing);
			_gameInstance.App.DevTools.Info($"[AssetUpdate] Translations: Starting {updateType}");
			Stopwatch stopwatch = Stopwatch.StartNew();
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_0056: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.App.Interface.RemoveServerMessages(translations.Keys);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] Translations: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
			});
			break;
		}
		default:
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {updateType}.");
		}
	}

	private void ProcessUpdateImmersiveViewPacket(UpdateImmersiveView packet)
	{
		ValidateStage(ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			if (packet != null)
			{
				_gameInstance.ImmersiveScreenModule.HandleUpdatePacket(packet);
			}
		});
	}

	private void ProcessUpdateWorldMapSettingsPacket(UpdateWorldMapSettings packet)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		UpdateWorldMapSettings updateWorldMapSettings = new UpdateWorldMapSettings(packet);
		_gameInstance.WorldMapModule.RunOnWorldMapThread(delegate
		{
			_gameInstance.WorldMapModule.UpdateMapSettings(updateWorldMapSettings.BiomeDataMap, !updateWorldMapSettings.Disabled, updateWorldMapSettings.AllowTeleportToCoordinates, updateWorldMapSettings.AllowTeleportToMarkers);
		});
	}

	private void ProcessUpdateWorldMapPacket(UpdateWorldMap packet)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		UpdateWorldMap updateWorldMap = new UpdateWorldMap(packet);
		_gameInstance.WorldMapModule.RunOnWorldMapThread(delegate
		{
			if (updateWorldMap.Chunks != null)
			{
				Chunk[] chunks = updateWorldMap.Chunks;
				foreach (Chunk val2 in chunks)
				{
					_gameInstance.WorldMapModule.SetMapChunk(val2.ChunkX, val2.ChunkZ, val2.Image_);
				}
			}
		});
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			if (updateWorldMap.AddedMarkers != null)
			{
				for (int i = 0; i < updateWorldMap.AddedMarkers.Length; i++)
				{
					Marker val = updateWorldMap.AddedMarkers[i];
					_gameInstance.WorldMapModule.AddMapMarker(new WorldMapModule.MapMarker
					{
						Id = val.Id,
						Name = val.Name,
						MarkerImage = val.MarkerImage,
						X = (float)val.Transform_.Position_.X,
						Y = (float)val.Transform_.Position_.Y,
						Z = (float)val.Transform_.Position_.Z,
						Yaw = val.Transform_.Orientation.Yaw,
						Pitch = val.Transform_.Orientation.Pitch,
						Roll = val.Transform_.Orientation.Roll
					});
				}
			}
			if (updateWorldMap.RemovedMarkers != null)
			{
				_gameInstance.WorldMapModule.RemoveMapMarker(updateWorldMap.RemovedMarkers);
			}
		});
	}

	private void ProcessShowEventTitlePacket(ShowEventTitle packet)
	{
		ValidateStage(ConnectionStage.Playing);
		EventTitle eventTitle = new EventTitle
		{
			PrimaryTitle = packet.PrimaryTitle,
			SecondaryTitle = packet.SecondaryTitle,
			IsMajor = packet.IsMajor,
			Icon = packet.Icon,
			Duration = packet.Duration,
			FadeInDuration = packet.FadeInDuration,
			FadeOutDuration = packet.FadeOutDuration
		};
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.TriggerEvent("eventTitle.show", eventTitle);
		});
	}

	private void ProcessHideEventTitlePacket(HideEventTitle packet)
	{
		ValidateStage(ConnectionStage.Playing);
		float fadeOutDuration = packet.FadeOutDuration;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.TriggerEvent("eventTitle.hide", fadeOutDuration);
		});
	}

	private void ProcessSetPagePacket(SetPage packet)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		ValidateStage(ConnectionStage.Playing);
		if ((int)packet.Page_ == 7)
		{
			throw new Exception("CustomPage must be opened with CustomPage packet!");
		}
		Page page = packet.Page_;
		bool canCloseThroughInteraction = packet.CanCloseThroughInteraction;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Invalid comparison between Unknown and I4
			_gameInstance.App.InGame.SetCurrentPage(page, (int)page > 0 && canCloseThroughInteraction);
		});
	}

	private void ProcessUpdateKnownRecipes(UpdateKnownRecipes packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.TriggerEvent("crafting.knownRecipesUpdated", packet.Known.Select((KeyValuePair<string, CraftingRecipe> kr) => new ClientKnownRecipe(kr.Key, kr.Value)).ToArray());
		});
	}

	private void ProcessCustomHudPacket(CustomHud packet)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		CustomHud customHud = new CustomHud(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.InGame.UpdateCustomHud(customHud);
		});
	}

	private void ProcessCustomPagePacket(CustomPage packet)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		CustomPage customPage = new CustomPage(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.InGame.OpenOrUpdateCustomPage(customPage);
		});
	}

	private void ProcessOpenWindow(OpenWindow packet)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		ValidateStage(ConnectionStage.Playing);
		InventoryWindow inventoryWindow = new InventoryWindow
		{
			Id = packet.Id,
			WindowType = packet.WindowType_,
			WindowDataStringified = string.Copy(packet.WindowData),
			WindowData = ((packet.WindowData != null) ? JObject.Parse(packet.WindowData) : null),
			Inventory = ConvertToClientItemStacks(packet.Inventory)
		};
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.TriggerEvent("inventory.windows.open", inventoryWindow);
		});
	}

	private void ProcessUpdateWindow(UpdateWindow packet)
	{
		ValidateStage(ConnectionStage.Playing);
		InventoryWindow inventoryWindow = new InventoryWindow
		{
			Id = packet.Id,
			WindowDataStringified = string.Copy(packet.WindowData),
			WindowData = ((packet.WindowData != null) ? JObject.Parse(packet.WindowData) : null),
			Inventory = ConvertToClientItemStacks(packet.Inventory)
		};
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.TriggerEvent("inventory.windows.update", inventoryWindow);
		});
	}

	private void ProcessCloseWindow(CloseWindow packet)
	{
		ValidateStage(ConnectionStage.Playing);
		int packetId = packet.Id;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.TriggerEvent("inventory.windows.close", packetId);
		});
	}

	private void ProcessFailureReply(FailureReply packet)
	{
		ValidateStage(ConnectionStage.Playing);
		CallPendingCallback(packet.Token, null, packet);
	}

	private void ProcessSuccessReply(SuccessReply packet)
	{
		ValidateStage(ConnectionStage.Playing);
		CallPendingCallback(packet.Token, (ProtoPacket)(object)packet, null);
	}

	private void ProcessUpdateVisibleHudComponents(UpdateVisibleHudComponents packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		if (packet.VisibleComponents == null)
		{
			throw new Exception("VisibleComponents cannot be empty!");
		}
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.TriggerEvent("hud.visibleComponentsUpdated", packet.VisibleComponents);
		});
	}

	private void ProcessDamageInfo(DamageInfo packet)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		DamageInfo damageInfo = new DamageInfo(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			if (packet.DamageAmount > 0f)
			{
				_gameInstance.DamageEffectModule.AddDamageEffect(damageInfo.DamageSourcePosition, damageInfo.DamageAmount, damageInfo.DamageCause_);
			}
			_gameInstance.InteractionModule.DamageInfos.Add(packet);
		});
	}

	private void ProcessReticleEvent(ReticleEvent packet)
	{
		ValidateStage(ConnectionStage.Playing);
		int eventIndex = packet.EventIndex;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.InGameView.OnReticleServerEvent(eventIndex);
		});
	}

	private void ProcessResetUserInterfaceState(ResetUserInterfaceState packet)
	{
		ValidateStage(ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.InGame.Reset(isStayingConnected: true);
		});
	}

	private ClientItemStack[] ConvertToClientItemStacks(InventorySection section)
	{
		if (section == null)
		{
			return null;
		}
		ClientItemStack[] array = new ClientItemStack[section.Capacity];
		foreach (KeyValuePair<int, Item> item in section.Items)
		{
			array[item.Key] = new ClientItemStack(item.Value);
		}
		return array;
	}

	private void ProcessUpdateItemPlayerAnimationsPacket(UpdateItemPlayerAnimations packet)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Invalid comparison between Unknown and I4
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Invalid comparison between Unknown and I4
		UpdateItemPlayerAnimations updateItemPlayerAnimations = new UpdateItemPlayerAnimations(packet);
		UpdateType type = updateItemPlayerAnimations.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 > 1)
			{
				return;
			}
			ValidateStage(ConnectionStage.Playing);
			_gameInstance.App.DevTools.Info($"[AssetUpdate] ItemAnimations: Starting {updateItemPlayerAnimations.Type}");
			Stopwatch stopwatch = Stopwatch.StartNew();
			Dictionary<string, ClientItemPlayerAnimations> upcomingItemPlayerAnimations;
			lock (_setupLock)
			{
				if ((int)updateItemPlayerAnimations.Type == 1)
				{
					foreach (KeyValuePair<string, ItemPlayerAnimations> item in updateItemPlayerAnimations.ItemPlayerAnimations_)
					{
						_networkItemPlayerAnimations[item.Key] = item.Value;
					}
				}
				else
				{
					foreach (KeyValuePair<string, ItemPlayerAnimations> item2 in updateItemPlayerAnimations.ItemPlayerAnimations_)
					{
						_networkItemPlayerAnimations.Remove(item2.Key);
					}
				}
				_gameInstance.ItemLibraryModule.PrepareItemPlayerAnimations(_networkItemPlayerAnimations, out _upcomingItemPlayerAnimations);
				CancellationToken threadCancellationToken = _threadCancellationToken;
				if (threadCancellationToken.IsCancellationRequested)
				{
					return;
				}
				upcomingItemPlayerAnimations = _upcomingItemPlayerAnimations;
			}
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_0083: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.ItemLibraryModule.SetupItemPlayerAnimations(upcomingItemPlayerAnimations);
				_gameInstance.ItemLibraryModule.LinkItemPlayerAnimations();
				_gameInstance.EntityStoreModule.RebuildRenderers(itemOnly: true);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] ItemAnimations: Finished {updateItemPlayerAnimations.Type} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
			});
			return;
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.ItemAnimations);
		if (_networkItemPlayerAnimations == null)
		{
			_networkItemPlayerAnimations = new Dictionary<string, ItemPlayerAnimations>();
		}
		foreach (KeyValuePair<string, ItemPlayerAnimations> item3 in updateItemPlayerAnimations.ItemPlayerAnimations_)
		{
			_networkItemPlayerAnimations[item3.Key] = item3.Value;
		}
		FinishedReceivedAssetType(AssetType.ItemAnimations);
	}

	private void ProcessUpdateItemsPacket(UpdateItems packet)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Invalid comparison between Unknown and I4
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Invalid comparison between Unknown and I4
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] Items: Starting {packet.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				Dictionary<string, ClientIcon> upcomingIcons = null;
				byte[] iconAtlasPixels = null;
				int iconAtlasWidth = 0;
				int iconAtlasHeight = 0;
				Dictionary<string, ClientItemBase> upcomingItems;
				lock (_setupLock)
				{
					if ((int)packet.Type == 1)
					{
						foreach (KeyValuePair<string, ItemBase> item in packet.Items)
						{
							_networkItems[item.Key] = item.Value;
						}
					}
					else
					{
						foreach (KeyValuePair<string, ItemBase> item2 in packet.Items)
						{
							_networkItems.Remove(item2.Key);
						}
					}
					_gameInstance.ItemLibraryModule.PrepareItems(packet.Items, _upcomingEntitiesImageLocations, ref _upcomingItems, _threadCancellationToken);
					if (packet.UpdateIcons)
					{
						_gameInstance.ItemLibraryModule.PrepareItemIconAtlas(_networkItems, out upcomingIcons, out iconAtlasPixels, out iconAtlasWidth, out iconAtlasHeight, _threadCancellationToken);
					}
					CancellationToken threadCancellationToken = _threadCancellationToken;
					if (threadCancellationToken.IsCancellationRequested)
					{
						return;
					}
					upcomingItems = new Dictionary<string, ClientItemBase>(_upcomingItems);
				}
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_002d: Unknown result type (might be due to invalid IL or missing references)
					//IL_0033: Invalid comparison between Unknown and I4
					//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.ItemLibraryModule.SetupItems(upcomingItems);
					if ((int)packet.Type == 1)
					{
						_gameInstance.App.Interface.TriggerEvent("items.added", packet.Items.Keys.ToDictionary((string k) => k, (string k) => upcomingItems[k]));
					}
					else
					{
						_gameInstance.App.Interface.TriggerEvent("items.removed", packet.Items.Keys.ToArray());
					}
					if (packet.UpdateModels || packet.UpdateIcons)
					{
						_gameInstance.EntityStoreModule.RebuildRenderers(itemOnly: false);
						_gameInstance.InterfaceRenderPreviewModule.HandleAssetsChanged();
					}
					if (upcomingIcons != null)
					{
						_gameInstance.ItemLibraryModule.SetupItemIcons(upcomingIcons, iconAtlasPixels, iconAtlasWidth, iconAtlasHeight);
					}
					_gameInstance.App.DevTools.Info($"[AssetUpdate] Items: Finished {packet.Type} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.Items);
		if (_networkItems == null)
		{
			_networkItems = new Dictionary<string, ItemBase>();
		}
		foreach (KeyValuePair<string, ItemBase> item3 in packet.Items)
		{
			_networkItems[item3.Key] = item3.Value;
		}
		FinishedReceivedAssetType(AssetType.Items);
	}

	private void ProcessUpdateItemCategoriesPacket(UpdateItemCategories packet)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected I4, but got Unknown
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		switch ((int)val)
		{
		case 0:
			ValidateStage(ConnectionStage.SettingUp);
			ReceivedAssetType(AssetType.ItemCategories);
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				_gameInstance.App.InGame.OnItemCategoriesInitialized(packet.ItemCategories);
			});
			FinishedReceivedAssetType(AssetType.ItemCategories);
			break;
		case 1:
		{
			ValidateStage(ConnectionStage.Playing);
			_gameInstance.App.DevTools.Info($"[AssetUpdate] ItemCategories: Starting {packet.Type}");
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_005b: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.App.InGame.OnItemCategoriesAdded(packet.ItemCategories);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] ItemCategories: Finished {packet.Type} in {TimeHelper.FormatMillis(stopwatch2.ElapsedMilliseconds)}");
			});
			break;
		}
		case 2:
		{
			ValidateStage(ConnectionStage.Playing);
			_gameInstance.App.DevTools.Info($"[AssetUpdate] ItemCategories: Starting {packet.Type}");
			Stopwatch stopwatch = Stopwatch.StartNew();
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_005b: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.App.InGame.OnItemCategoriesRemoved(packet.ItemCategories);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] ItemCategories: Finished {packet.Type} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
			});
			break;
		}
		default:
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
		}
	}

	private void ProcessUpdateFieldcraftCategoriesPacket(UpdateFieldcraftCategories packet)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if ((int)val != 1)
			{
				throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
			}
			ValidateStage(ConnectionStage.Playing);
			_gameInstance.App.DevTools.Info($"[AssetUpdate] FieldcraftCategories: Starting {packet.Type}");
			Stopwatch stopwatch = Stopwatch.StartNew();
			ClientCraftingCategory[] categories2 = (from category in packet.ItemCategories
				select new ClientCraftingCategory
				{
					Id = category.Id,
					Icon = category.Icon,
					Order = category.Order
				} into category
				orderby category.Order
				select category).ToArray();
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_005b: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.App.Interface.TriggerEvent("fieldcraftCategories.added", categories2);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] FieldcraftCategories: Finished {packet.Type} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
			});
		}
		else
		{
			ValidateStage(ConnectionStage.SettingUp);
			ReceivedAssetType(AssetType.FieldcraftCategories);
			ClientCraftingCategory[] categories = (from category in packet.ItemCategories
				select new ClientCraftingCategory
				{
					Id = category.Id,
					Icon = category.Icon,
					Order = category.Order
				} into category
				orderby category.Order
				select category).ToArray();
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				_gameInstance.App.Interface.TriggerEvent("fieldcraftCategories.initialized", categories);
			});
			FinishedReceivedAssetType(AssetType.FieldcraftCategories);
		}
	}

	private void ProcessUpdateResourceTypes(UpdateResourceTypes packet)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected I4, but got Unknown
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		switch ((int)val)
		{
		case 0:
		{
			ValidateStage(ConnectionStage.SettingUp);
			ReceivedAssetType(AssetType.ResourceTypes);
			Dictionary<string, ClientResourceType> resourceTypes = new Dictionary<string, ClientResourceType>();
			foreach (KeyValuePair<string, ResourceType> resourceType in packet.ResourceTypes)
			{
				resourceTypes[resourceType.Key] = new ClientResourceType(resourceType.Value);
			}
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				_gameInstance.App.Interface.TriggerEvent("resourceTypes.initialized", resourceTypes);
				_gameInstance.ItemLibraryModule.SetupResourceTypes(resourceTypes);
			});
			FinishedReceivedAssetType(AssetType.ResourceTypes);
			break;
		}
		case 1:
		{
			ValidateStage(ConnectionStage.Playing);
			_gameInstance.App.DevTools.Info($"[AssetUpdate] ResourceTypes: Starting {packet.Type}");
			Stopwatch stopwatch = Stopwatch.StartNew();
			Dictionary<string, ClientResourceType> addedResourceTypes = new Dictionary<string, ClientResourceType>();
			foreach (KeyValuePair<string, ResourceType> resourceType2 in packet.ResourceTypes)
			{
				addedResourceTypes[resourceType2.Key] = new ClientResourceType(resourceType2.Value);
			}
			Dictionary<string, ClientResourceType> resourceTypes3 = new Dictionary<string, ClientResourceType>(_gameInstance.ItemLibraryModule.ResourceTypes);
			foreach (KeyValuePair<string, ClientResourceType> item in addedResourceTypes)
			{
				resourceTypes3[item.Key] = item.Value;
			}
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_007c: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.App.Interface.TriggerEvent("resourceTypes.added", addedResourceTypes);
				_gameInstance.ItemLibraryModule.SetupResourceTypes(resourceTypes3);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] ResourceTypes: Finished {packet.Type} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
			});
			break;
		}
		case 2:
		{
			ValidateStage(ConnectionStage.Playing);
			string[] removedKeys = packet.ResourceTypes.Keys.ToArray();
			Dictionary<string, ClientResourceType> resourceTypes2 = new Dictionary<string, ClientResourceType>(_gameInstance.ItemLibraryModule.ResourceTypes);
			string[] array = removedKeys;
			foreach (string key in array)
			{
				resourceTypes2.Remove(key);
			}
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				_gameInstance.App.Interface.TriggerEvent("resourceTypes.removed", removedKeys);
				_gameInstance.ItemLibraryModule.SetupResourceTypes(resourceTypes2);
			});
			break;
		}
		default:
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
		}
	}

	private void ProcessUpdateItemQualitiesPacket(UpdateItemQualities packet)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Invalid comparison between Unknown and I4
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 > 1)
			{
				return;
			}
			ValidateStage(ConnectionStage.Playing);
			_gameInstance.App.DevTools.Info($"[AssetUpdate] ItemQualities: Starting {packet.Type}");
			Stopwatch stopwatch = Stopwatch.StartNew();
			if (packet.MaxId > _upcomingServerSettings.ItemQualities.Length)
			{
				Array.Resize(ref _upcomingServerSettings.ItemQualities, packet.MaxId);
			}
			if ((int)packet.Type == 1)
			{
				foreach (KeyValuePair<int, ItemQuality> itemQuality in packet.ItemQualities)
				{
					_upcomingServerSettings.ItemQualities[itemQuality.Key] = new ClientItemQuality(itemQuality.Value);
				}
			}
			ServerSettings newServerSettings = _upcomingServerSettings.Clone();
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_0047: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.SetServerSettings(newServerSettings);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] ItemQualities: Finished {packet.Type} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
			});
			return;
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.ItemQuality);
		if (_upcomingServerSettings.ItemQualities == null)
		{
			_upcomingServerSettings.ItemQualities = new ClientItemQuality[packet.MaxId];
		}
		foreach (KeyValuePair<int, ItemQuality> itemQuality2 in packet.ItemQualities)
		{
			_upcomingServerSettings.ItemQualities[itemQuality2.Key] = new ClientItemQuality(itemQuality2.Value);
		}
		FinishedReceivedAssetType(AssetType.ItemQuality);
	}

	private void ProcessUpdateItemReticles(UpdateItemReticles packet)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Invalid comparison between Unknown and I4
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] ItemQualities: Starting {packet.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if (packet.MaxId > _upcomingServerSettings.ItemReticleConfigs.Length)
				{
					Array.Resize(ref _upcomingServerSettings.ItemReticleConfigs, packet.MaxId);
				}
				if ((int)packet.Type == 1)
				{
					foreach (KeyValuePair<int, ItemReticleConfig> itemReticleConfig in packet.ItemReticleConfigs)
					{
						_upcomingServerSettings.ItemReticleConfigs[itemReticleConfig.Key] = new ClientItemReticleConfig(itemReticleConfig.Value);
					}
				}
				ServerSettings newServerSettings = _upcomingServerSettings.Clone();
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_006c: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.SetServerSettings(newServerSettings);
					_gameInstance.App.Interface.InGameView.OnReticlesUpdated();
					_gameInstance.App.DevTools.Info($"[AssetUpdate] ItemReticleConfigs: Finished {packet.Type} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.ItemReticles);
		if (_upcomingServerSettings.ItemReticleConfigs == null)
		{
			_upcomingServerSettings.ItemReticleConfigs = new ClientItemReticleConfig[packet.MaxId];
		}
		foreach (KeyValuePair<int, ItemReticleConfig> itemReticleConfig2 in packet.ItemReticleConfigs)
		{
			_upcomingServerSettings.ItemReticleConfigs[itemReticleConfig2.Key] = new ClientItemReticleConfig(itemReticleConfig2.Value);
		}
		FinishedReceivedAssetType(AssetType.ItemReticles);
	}

	private void ProcessSetMachinimaActorModelPacket(SetMachinimaActorModel packet)
	{
		ValidateStage(ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			((EntityActor)_gameInstance.MachinimaModule.GetScene(packet.SceneName).GetActor(packet.ActorName)).SetBaseModel(packet.Model_);
		});
	}

	private void ProcessUpdateMachinimaScene(UpdateMachinimaScene packet)
	{
		ValidateStage(ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.MachinimaModule.HandleSceneUpdatePacket(packet);
		});
	}

	private void ProcessServerMessagePacket(ServerMessage packet)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		ServerMessage serverMessage = new ServerMessage(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.Chat.AddBsonMessage(serverMessage.Message);
		});
	}

	private void ProcessNotificationPacket(Notification packet)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		Notification notification = new Notification(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.Notifications.AddNotification(notification);
		});
	}

	private void ProcessKillFeedMessage(KillFeedMessage packet)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		KillFeedMessage killFeedMessage = new KillFeedMessage(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.InGameView.KillFeedComponent.OnReceiveNewEntry(killFeedMessage.Decedent, killFeedMessage.Killer, killFeedMessage.Icon);
		});
	}

	private void ProcessUpdateModelVFXsPacket(UpdateModelvfxs packet)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Invalid comparison between Unknown and I4
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		UpdateModelvfxs updateModelVFXs = new UpdateModelvfxs(packet);
		UpdateType type = updateModelVFXs.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] ModelVFXs: Starting {updateModelVFXs.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if (updateModelVFXs.MaxId > _modelVFXs.Length)
				{
					Array.Resize(ref _modelVFXs, updateModelVFXs.MaxId);
				}
				if ((int)updateModelVFXs.Type == 1)
				{
					foreach (KeyValuePair<int, ModelVFX> modelVFX in updateModelVFXs.ModelVFXs)
					{
						_modelVFXs[modelVFX.Key] = modelVFX.Value;
					}
				}
				else
				{
					foreach (KeyValuePair<int, ModelVFX> modelVFX2 in updateModelVFXs.ModelVFXs)
					{
						_modelVFXs[modelVFX2.Key] = null;
					}
				}
				_gameInstance.EntityStoreModule.PrepareModelVFXs(_modelVFXs, out var upcomingModelVFXs);
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_004c: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.EntityStoreModule.SetupModelVFXs(upcomingModelVFXs);
					_gameInstance.App.DevTools.Info($"[AssetUpdate] ModelVFXs: Finished {updateModelVFXs.Type} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)updateModelVFXs).GetType().Name} at {_stage} with type {updateModelVFXs.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.ModelVFX);
		if (_modelVFXs == null)
		{
			_modelVFXs = (ModelVFX[])(object)new ModelVFX[updateModelVFXs.MaxId];
		}
		foreach (KeyValuePair<int, ModelVFX> modelVFX3 in updateModelVFXs.ModelVFXs)
		{
			_modelVFXs[modelVFX3.Key] = modelVFX3.Value;
		}
		FinishedReceivedAssetType(AssetType.ModelVFX);
	}

	private void ProcessTrackOrUpdateObjective(TrackOrUpdateObjective packet)
	{
		ValidateStage(ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.TriggerEvent("objectives.updateObjective", packet.Objective_);
		});
	}

	private void ProcessUntrackObjective(UntrackObjective packet)
	{
		ValidateStage(ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.TriggerEvent("objectives.removeObjective", packet.ObjectiveUuid);
		});
	}

	private void ProcessUpdateObjectiveTask(UpdateObjectiveTask packet)
	{
		ValidateStage(ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.TriggerEvent("objectives.updateTask", packet.ObjectiveUuid, packet.TaskIndex, packet.Task);
		});
	}

	private void ProcessUpdateParticleSystemsPacket(UpdateParticleSystems packet)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected I4, but got Unknown
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		UpdateParticleSystems updateParticleSystems = new UpdateParticleSystems(packet);
		UpdateType type = updateParticleSystems.Type;
		UpdateType val = type;
		switch ((int)val)
		{
		case 0:
			ValidateStage(ConnectionStage.SettingUp);
			ReceivedAssetType(AssetType.ParticleSystems);
			if (_networkParticleSystems == null)
			{
				_networkParticleSystems = new Dictionary<string, ParticleSystem>();
			}
			foreach (KeyValuePair<string, ParticleSystem> particleSystem in updateParticleSystems.ParticleSystems)
			{
				_networkParticleSystems[particleSystem.Key] = particleSystem.Value;
			}
			FinishedReceivedAssetType(AssetType.ParticleSystems);
			break;
		case 1:
		{
			ValidateStage(ConnectionStage.Playing);
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			foreach (KeyValuePair<string, ParticleSystem> particleSystem2 in updateParticleSystems.ParticleSystems)
			{
				_networkParticleSystems[particleSystem2.Key] = particleSystem2.Value;
			}
			Dictionary<string, ParticleSystem> upcomingParticleSystems = new Dictionary<string, ParticleSystem>(updateParticleSystems.ParticleSystems);
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_0063: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.ParticleSystemStoreModule.SetupParticleSystems(upcomingParticleSystems);
				_gameInstance.ParticleSystemStoreModule.ResetParticleSystems();
				_gameInstance.Chat.Log($"[AssetUpdate] ParticleSystems: Finished {updateParticleSystems.Type} in {TimeHelper.FormatMillis(stopwatch2.ElapsedMilliseconds)}");
			});
			break;
		}
		case 2:
		{
			ValidateStage(ConnectionStage.Playing);
			_gameInstance.App.DevTools.Info($"[AssetUpdate] ParticleSystems: Starting {updateParticleSystems.Type}");
			Stopwatch stopwatch = Stopwatch.StartNew();
			foreach (KeyValuePair<string, ParticleSystem> particleSystem3 in updateParticleSystems.ParticleSystems)
			{
				_networkParticleSystems.Remove(particleSystem3.Key);
			}
			Dictionary<string, ParticleSystem> removedParticleSystems = new Dictionary<string, ParticleSystem>(updateParticleSystems.ParticleSystems);
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_0068: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.ParticleSystemStoreModule.RemoveParticleSystems(removedParticleSystems);
				_gameInstance.ParticleSystemStoreModule.ResetParticleSystems();
				_gameInstance.App.DevTools.Info($"[AssetUpdate] ParticleSystems: Finished {updateParticleSystems.Type} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
			});
			break;
		}
		default:
			throw new Exception($"Received invalid packet UpdateType for {((object)updateParticleSystems).GetType().Name} at {_stage} with type {updateParticleSystems.Type}.");
		}
	}

	private void ProcessUpdateParticleSpawnersPacket(UpdateParticleSpawners packet)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected I4, but got Unknown
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b5: Unknown result type (might be due to invalid IL or missing references)
		UpdateParticleSpawners updateParticleSpawners = new UpdateParticleSpawners(packet);
		UpdateType type = updateParticleSpawners.Type;
		UpdateType val = type;
		switch ((int)val)
		{
		case 0:
			ValidateStage(ConnectionStage.SettingUp);
			ReceivedAssetType(AssetType.ParticleSpawners);
			if (_networkParticleSpawners == null)
			{
				_networkParticleSpawners = new Dictionary<string, ParticleSpawner>();
			}
			foreach (KeyValuePair<string, ParticleSpawner> particleSpawner in updateParticleSpawners.ParticleSpawners)
			{
				_networkParticleSpawners[particleSpawner.Key] = particleSpawner.Value;
			}
			FinishedReceivedAssetType(AssetType.ParticleSpawners);
			break;
		case 1:
		{
			ValidateStage(ConnectionStage.Playing);
			_gameInstance.App.DevTools.Info($"[AssetUpdate] ParticleSpawners: Starting {updateParticleSpawners.Type}");
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			Dictionary<string, ParticleSystem> upcomingParticleSystems2;
			Dictionary<string, ParticleSpawner> upcomingParticleSpawners;
			Dictionary<string, ParticleSettings> upcomingParticles;
			Dictionary<string, Rectangle> upcomingFXImageLocations;
			byte[][] upcomingFXAtlasPixelsPerLevel;
			byte[][] upcomingUVMotionTextureArrayPixelsPerLevel;
			lock (_setupLock)
			{
				foreach (KeyValuePair<string, ParticleSpawner> particleSpawner2 in updateParticleSpawners.ParticleSpawners)
				{
					_networkParticleSpawners[particleSpawner2.Key] = particleSpawner2.Value;
				}
				upcomingParticleSystems2 = new Dictionary<string, ParticleSystem>(_networkParticleSystems);
				upcomingParticleSpawners = new Dictionary<string, ParticleSpawner>(_networkParticleSpawners);
				_gameInstance.ParticleSystemStoreModule.PrepareParticles(upcomingParticleSpawners, out upcomingParticles, out _upcomingParticleTextureInfo, out _upcomingUVMotionTexturePaths, _threadCancellationToken);
				CancellationToken threadCancellationToken = _threadCancellationToken;
				if (threadCancellationToken.IsCancellationRequested)
				{
					break;
				}
				_gameInstance.FXModule.PrepareAtlas(_upcomingParticleTextureInfo, _upcomingTrailTextureInfo, out upcomingFXImageLocations, out upcomingFXAtlasPixelsPerLevel, _threadCancellationToken);
				threadCancellationToken = _threadCancellationToken;
				if (threadCancellationToken.IsCancellationRequested)
				{
					break;
				}
				_gameInstance.FXModule.PrepareUVMotionTextureArray(_upcomingUVMotionTexturePaths, out upcomingUVMotionTextureArrayPixelsPerLevel);
			}
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.ParticleSystemStoreModule.SetupParticleSpawners(upcomingParticleSystems2, upcomingParticleSpawners, upcomingParticles, _upcomingUVMotionTexturePaths);
				_gameInstance.FXModule.CreateAtlasTextures(upcomingFXImageLocations, upcomingFXAtlasPixelsPerLevel);
				_gameInstance.FXModule.CreateUVMotionTextureArray(upcomingUVMotionTextureArrayPixelsPerLevel);
				_gameInstance.ParticleSystemStoreModule.ResetParticleSystems();
				_gameInstance.App.DevTools.Info($"[AssetUpdate] ParticleSpawners: Finished {updateParticleSpawners.Type} in {TimeHelper.FormatMillis(stopwatch2.ElapsedMilliseconds)}");
			});
			break;
		}
		case 2:
		{
			ValidateStage(ConnectionStage.Playing);
			_gameInstance.App.DevTools.Info($"[AssetUpdate] ParticleSpawners: Starting {updateParticleSpawners.Type}");
			Stopwatch stopwatch = Stopwatch.StartNew();
			Dictionary<string, ParticleSystem> upcomingParticleSystems;
			Dictionary<string, ParticleSpawner> upcomingParticleSpawners2;
			Dictionary<string, ParticleSettings> upcomingParticles2;
			Dictionary<string, Rectangle> upcomingFXImageLocations2;
			byte[][] upcomingFXAtlasPixelsPerLevel2;
			byte[][] upcomingUVMotionTextureArrayPixelsPerLevel2;
			lock (_setupLock)
			{
				foreach (KeyValuePair<string, ParticleSpawner> particleSpawner3 in updateParticleSpawners.ParticleSpawners)
				{
					_networkParticleSpawners.Remove(particleSpawner3.Key);
				}
				upcomingParticleSystems = new Dictionary<string, ParticleSystem>(_networkParticleSystems);
				Dictionary<string, ParticleSpawner> networkParticleSpawners = new Dictionary<string, ParticleSpawner>(_networkParticleSpawners);
				upcomingParticleSpawners2 = new Dictionary<string, ParticleSpawner>(updateParticleSpawners.ParticleSpawners);
				_gameInstance.ParticleSystemStoreModule.PrepareParticles(networkParticleSpawners, out upcomingParticles2, out _upcomingParticleTextureInfo, out _upcomingUVMotionTexturePaths, _threadCancellationToken);
				CancellationToken threadCancellationToken = _threadCancellationToken;
				if (threadCancellationToken.IsCancellationRequested)
				{
					break;
				}
				_gameInstance.FXModule.PrepareAtlas(_upcomingParticleTextureInfo, _upcomingTrailTextureInfo, out upcomingFXImageLocations2, out upcomingFXAtlasPixelsPerLevel2, _threadCancellationToken);
				threadCancellationToken = _threadCancellationToken;
				if (threadCancellationToken.IsCancellationRequested)
				{
					break;
				}
				_gameInstance.FXModule.PrepareUVMotionTextureArray(_upcomingUVMotionTexturePaths, out upcomingUVMotionTextureArrayPixelsPerLevel2);
			}
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.ParticleSystemStoreModule.RemoveParticleSpawners(upcomingParticleSystems, upcomingParticleSpawners2, upcomingParticles2);
				_gameInstance.FXModule.CreateAtlasTextures(upcomingFXImageLocations2, upcomingFXAtlasPixelsPerLevel2);
				_gameInstance.FXModule.CreateUVMotionTextureArray(upcomingUVMotionTextureArrayPixelsPerLevel2);
				_gameInstance.ParticleSystemStoreModule.ResetParticleSystems();
				_gameInstance.App.DevTools.Info($"[AssetUpdate] ParticleSpawners: Finished {updateParticleSpawners.Type} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
			});
			break;
		}
		default:
			throw new Exception($"Received invalid packet UpdateType for {((object)updateParticleSpawners).GetType().Name} at {_stage} with type {updateParticleSpawners.Type}.");
		}
	}

	private void ProcessSpawnParticleSystemPacket(SpawnParticleSystem packet)
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Expected O, but got Unknown
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		int systemCount = _gameInstance.ParticleSystemStoreModule.SystemCount;
		if (systemCount > _gameInstance.ParticleSystemStoreModule.MaxSpawnedSystems)
		{
			_gameInstance.App.DevTools.Error("Particle system limit reached: " + _gameInstance.ParticleSystemStoreModule.MaxSpawnedSystems + ": " + (object)packet);
			return;
		}
		SpawnParticleSystem spawnParticleSystem = new SpawnParticleSystem(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			if (_gameInstance.ParticleSystemStoreModule.TrySpawnSystem(spawnParticleSystem.ParticleSystemId, out var particleSystemProxy))
			{
				particleSystemProxy.Position = new Vector3((float)spawnParticleSystem.Position_.X, (float)spawnParticleSystem.Position_.Y, (float)spawnParticleSystem.Position_.Z);
				particleSystemProxy.Rotation = ((spawnParticleSystem.Rotation != null) ? Quaternion.CreateFromYawPitchRoll(spawnParticleSystem.Rotation.Yaw, spawnParticleSystem.Rotation.Pitch, spawnParticleSystem.Rotation.Roll) : Quaternion.Identity);
				if (spawnParticleSystem.Color_ != null)
				{
					particleSystemProxy.DefaultColor = UInt32Color.FromRGBA((byte)spawnParticleSystem.Color_.Red, (byte)spawnParticleSystem.Color_.Green, (byte)spawnParticleSystem.Color_.Blue, byte.MaxValue);
				}
				particleSystemProxy.Scale = spawnParticleSystem.Scale;
			}
		});
	}

	private void ProcessSpawnBlockParticleSystem(SpawnBlockParticleSystem packet)
	{
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Expected I4, but got Unknown
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		int systemCount = _gameInstance.ParticleSystemStoreModule.SystemCount;
		if (systemCount > _gameInstance.ParticleSystemStoreModule.MaxSpawnedSystems)
		{
			_gameInstance.App.DevTools.Error("Particle system limit reached: " + _gameInstance.ParticleSystemStoreModule.MaxSpawnedSystems + ": " + (object)packet);
			return;
		}
		Vector3 position = new Vector3((float)packet.Position_.X, (float)packet.Position_.Y, (float)packet.Position_.Z);
		int blockId = packet.BlockId;
		ClientBlockParticleEvent particleType = (ClientBlockParticleEvent)packet.ParticleType;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			ClientBlockType blockType = _gameInstance.MapModule.ClientBlockTypes[blockId];
			if (_gameInstance.ParticleSystemStoreModule.TrySpawnBlockSystem(position, blockType, particleType, out var particleSystemProxy, faceCameraYaw: true))
			{
				particleSystemProxy.Position = position + particleSystemProxy.Position;
			}
		});
	}

	private void ProcessSpawnModelParticles(SpawnModelParticles packet)
	{
		ValidateStage(ConnectionStage.Playing);
		Entity entity = _gameInstance.EntityStoreModule.GetEntity(packet.EntityId);
		ModelParticle[] modelParticles = packet.ModelParticles;
		if (entity != null && modelParticles != null && modelParticles.Length != 0)
		{
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				ParticleProtocolInitializer.Initialize(modelParticles, out var clientModelParticles, _gameInstance.EntityStoreModule.NodeNameManager);
				entity.AddModelParticles(clientModelParticles);
			});
		}
	}

	private void ProcessInventoryPacket(UpdatePlayerInventory packet)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		UpdatePlayerInventory updatePlayerInventory = new UpdatePlayerInventory(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.InventoryModule.SetInventory(updatePlayerInventory);
		});
	}

	private void ProcessDisplayDebug(DisplayDebug packet)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		Matrix matrix = new Matrix(packet.Matrix);
		Vector3 color = new Vector3(packet.Color.X, packet.Color.Y, packet.Color.Z);
		float time = packet.Time;
		bool fade = packet.Fade;
		DebugShape shape = packet.Shape;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			_gameInstance.DebugDisplayModule.Add(shape, matrix, time, color, fade);
		});
	}

	private void ProcessSetActiveSlot(SetActiveSlot packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		int inventorySectionId = packet.InventorySectionId;
		int activeSlot = packet.ActiveSlot;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			switch ((InventorySectionType)inventorySectionId)
			{
			case InventorySectionType.Hotbar:
				_gameInstance.InventoryModule.SetActiveHotbarSlot(activeSlot, triggerInteraction: false);
				break;
			case InventorySectionType.Utility:
				_gameInstance.InventoryModule.SetActiveUtilitySlot(activeSlot, sendPacket: false);
				break;
			case InventorySectionType.Consumable:
				_gameInstance.InventoryModule.SetActiveConsumableSlot(activeSlot, sendPacket: false);
				break;
			case InventorySectionType.Tools:
				_gameInstance.InventoryModule.SetActiveToolsSlot(activeSlot, sendPacket: false, useTool: false);
				break;
			default:
				throw new Exception("Inventory section with id " + inventorySectionId + " cannot select an active slot");
			}
		});
	}

	private void ProcessSetGameModePacket(SetGameMode packet)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		GameMode gameMode = packet.GameMode_;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			_gameInstance.SetGameMode(gameMode);
		});
	}

	private void ProcessSetMovementStates(SetMovementStates packet)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		SavedMovementStates movementStates = new SavedMovementStates(packet.MovementStates);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.CharacterControllerModule.MovementController.SetSavedMovementStates(movementStates);
		});
	}

	private void ProcessUpdateMovementSettings(UpdateMovementSettings packet)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		MovementSettings movementSettings = new MovementSettings(packet.MovementSettings_);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.CharacterControllerModule.MovementController.UpdateMovementSettings(movementSettings);
		});
	}

	private void ProcessCameraShakeProfiles(UpdateCameraShake packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.CameraModule.CameraShakeController.UpdateCameraShakeAssets(packet);
		});
	}

	private void ProcessCameraEffect(CameraShakeEffect packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			_gameInstance.CameraModule.CameraShakeController.PlayCameraShake(packet.CameraShakeId, packet.Intensity, packet.Mode);
		});
	}

	private void ProcessViewBobbingProfiles(UpdateViewBobbing packet)
	{
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.CameraModule.CameraShakeController.UpdateViewBobbingAssets(packet);
		});
	}

	private void ProcessChangeVelocity(ChangeVelocity packet)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		if (!ValidateFloat(packet.X) || !ValidateFloat(packet.Y) || !ValidateFloat(packet.Z))
		{
			_gameInstance.App.DevTools.Error($"Invalid packet data: {packet}");
			return;
		}
		ChangeVelocity changeVelocity = new ChangeVelocity(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			_gameInstance.CharacterControllerModule.MovementController.RequestVelocityChange(changeVelocity.X, changeVelocity.Y, changeVelocity.Z, changeVelocity.ChangeType, changeVelocity.Config);
		});
	}

	private void ProcessApplyKnockback(ApplyKnockback packet)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		if (!ValidateFloat(packet.X) || !ValidateFloat(packet.Y) || !ValidateFloat(packet.Z))
		{
			_gameInstance.App.DevTools.Error($"Invalid packet data: {packet}");
			return;
		}
		ApplyKnockback applyKnockback = new ApplyKnockback(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.CharacterControllerModule.MovementController.ApplyKnockback(applyKnockback);
		});
	}

	private void ProcessSetServerCamera(SetServerCamera packet)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		SetServerCamera setServerCamera = new SetServerCamera(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Invalid comparison between Unknown and I4
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Expected I4, but got Unknown
			if ((int)setServerCamera.ClientCameraView_ == 2)
			{
				if (setServerCamera.CameraSettings == null)
				{
					_gameInstance.CameraModule.ResetCameraController();
				}
				else if (_gameInstance.CameraModule.Controller is ServerCameraController serverCameraController)
				{
					serverCameraController.CameraSettings = setServerCamera.CameraSettings;
				}
				else
				{
					_gameInstance.CameraModule.SetCustomCameraController(new ServerCameraController(_gameInstance, setServerCamera.CameraSettings));
				}
			}
			else
			{
				_gameInstance.CameraModule.SetCameraControllerIndex((int)setServerCamera.ClientCameraView_);
				if (setServerCamera.IsLocked)
				{
					_gameInstance.CameraModule.LockCamera();
				}
			}
		});
	}

	private void ProcessSyncInteractionChain(SyncInteractionChain packet)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		SyncInteractionChain syncInteractionCHain = new SyncInteractionChain(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.InteractionModule.Handle(syncInteractionCHain);
		});
	}

	private void ProcessCancelInteractionChain(CancelInteractionChain packet)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		CancelInteractionChain cancelInteractionChain = new CancelInteractionChain(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.InteractionModule.Handle(cancelInteractionChain);
		});
	}

	private void ProcessPlayInteractionFor(PlayInteractionFor packet)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		PlayInteractionFor playInteractionFor = new PlayInteractionFor(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a7: Invalid comparison between Unknown and I4
			//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_02af: Unknown result type (might be due to invalid IL or missing references)
			ClientInteraction clientInteraction = _gameInstance.InteractionModule.Interactions[playInteractionFor.InteractionId];
			Entity entity = _gameInstance.EntityStoreModule.GetEntity(playInteractionFor.EntityId);
			if (entity != null)
			{
				bool isFirstInteraction = playInteractionFor.OperationIndex == 0;
				InteractionMetaStore value2;
				if (playInteractionFor.Cancel)
				{
					if (!entity.InteractionMetaStores.TryGetValue((playInteractionFor.ChainId, playInteractionFor.ForkedId), out var value) || !value.TryGetValue(playInteractionFor.OperationIndex, out value2))
					{
						return;
					}
					value.Remove(playInteractionFor.OperationIndex);
					if (value.Count == 0)
					{
						entity.InteractionMetaStores.Remove((playInteractionFor.ChainId, playInteractionFor.ForkedId));
					}
				}
				else
				{
					if (!entity.InteractionMetaStores.TryGetValue((playInteractionFor.ChainId, playInteractionFor.ForkedId), out var value3))
					{
						value3 = new Dictionary<int, InteractionMetaStore>();
						entity.InteractionMetaStores.Add((playInteractionFor.ChainId, playInteractionFor.ForkedId), value3);
					}
					value2 = new InteractionMetaStore();
					value3.Add(playInteractionFor.OperationIndex, value2);
				}
				if ((int)playInteractionFor.InteractionType_ == 6 && entity.NetworkId != _gameInstance.LocalPlayer.NetworkId)
				{
					entity.ConsumableItem = _gameInstance.ItemLibraryModule.GetItem(playInteractionFor.InteractedItemId);
				}
				InteractionContext interactionContext = null;
				if (entity.NetworkId == _gameInstance.LocalPlayer.NetworkId)
				{
					_gameInstance.InteractionModule.Chains.TryGetValue(playInteractionFor.ChainId, out var value4);
					ForkedChainId forkedId = playInteractionFor.ForkedId;
					while (forkedId != null && value4 != null)
					{
						if (!value4.GetForkedChain(forkedId, out value4))
						{
							return;
						}
						forkedId = forkedId.ForkedId;
					}
					interactionContext = value4?.Context;
				}
				if (interactionContext == null)
				{
					interactionContext = InteractionContext.ForInteraction(entity, playInteractionFor.InteractionType_);
				}
				clientInteraction.HandlePlayFor(_gameInstance, entity, playInteractionFor.InteractionType_, interactionContext, value2, playInteractionFor.Cancel, isFirstInteraction);
			}
		});
	}

	private void ProcessMountNpc(MountNPC packet)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		MountNPC mountNpc = new MountNPC(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.CharacterControllerModule.MountNpc(mountNpc);
		});
	}

	private void ProcessDismountNpc(DismountNPC packet)
	{
		ValidateStage(ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.CharacterControllerModule.DismountNpc();
		});
	}

	private void ProcessAddToPlayerListPacket(AddToPlayerList packet)
	{
		ValidateStage(ConnectionStage.WaitingForSetup | ConnectionStage.SettingUp | ConnectionStage.Playing);
		PlayerListPlayer[] players = new PlayerListPlayer[packet.Players.Length];
		for (int i = 0; i < packet.Players.Length; i++)
		{
			players[i] = new PlayerListPlayer(packet.Players[i]);
		}
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.InGameView.OnPlayerListAdd(players);
		});
	}

	private void ProcessRemoveFromPlayerListPacket(RemoveFromPlayerList packet)
	{
		ValidateStage(ConnectionStage.WaitingForSetup | ConnectionStage.SettingUp | ConnectionStage.Playing);
		Guid[] players = (Guid[])packet.Players.Clone();
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.InGameView.OnPlayerListRemove(players);
		});
	}

	private void ProcessUpdatePlayerListPacket(UpdatePlayerList packet)
	{
		ValidateStage(ConnectionStage.WaitingForSetup | ConnectionStage.SettingUp | ConnectionStage.Playing);
		Dictionary<Guid, int> players = new Dictionary<Guid, int>(packet.Players);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.InGameView.OnPlayerListUpdate(players);
		});
	}

	private void ProcessClearPlayerListPacket(ClearPlayerList packet)
	{
		ValidateStage(ConnectionStage.WaitingForSetup | ConnectionStage.SettingUp | ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.Interface.InGameView.OnPlayerListClear();
		});
	}

	private void ProcessWorldSettingsPacket(WorldSettings packet)
	{
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
		ValidateStage(ConnectionStage.WaitingForSetup);
		SetStage(ConnectionStage.SettingUp);
		if (packet.WorldHeight % 32 != 0)
		{
			throw new Exception($"Invalid world height. Must be a multiple of {32}: {packet.WorldHeight}");
		}
		ChunkHelper.SetHeight(packet.WorldHeight);
		Asset[] requiredAssets = null;
		if (packet.RequiredAssets != null)
		{
			requiredAssets = (Asset[])(object)new Asset[packet.RequiredAssets.Length];
			for (int i = 0; i < packet.RequiredAssets.Length; i++)
			{
				if (packet.RequiredAssets[i] != null)
				{
					requiredAssets[i] = new Asset(packet.RequiredAssets[i]);
				}
			}
		}
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_012f: Expected O, but got Unknown
			//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Expected O, but got Unknown
			if (requiredAssets != null)
			{
				long elapsedMilliseconds = _connectionStopwatch.ElapsedMilliseconds;
				Logger.Info("Handle RequiredAssets at {0}ms", elapsedMilliseconds);
				List<Asset> list = new List<Asset>(requiredAssets.Length);
				foreach (Asset val in requiredAssets)
				{
					string hash = val.Hash;
					string name = val.Name;
					_gameInstance.RegisterHashForServerAsset(name, hash);
					AssetManager.MarkAssetAsServerRequired(name, hash, out var foundInCache);
					if (!foundInCache)
					{
						list.Add(val);
					}
				}
				Logger.Info("Finished handling RequiredAssets, took {0}ms", _connectionStopwatch.ElapsedMilliseconds - elapsedMilliseconds);
				_gameInstance.Connection.SendPacket((ProtoPacket)new RequestAssets(list.ToArray()));
			}
			else
			{
				Logger.Info("Handle RequiredAssets at {0}ms, No assets to process", _connectionStopwatch.ElapsedMilliseconds);
				_gameInstance.Connection.SendPacket((ProtoPacket)new RequestAssets());
			}
		});
	}

	private void ProcessServerTagsPacket(ServerTags packet)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		ValidateStage(ConnectionStage.WaitingForSetup | ConnectionStage.SettingUp | ConnectionStage.Playing);
		ServerSettings newServerSettings = _upcomingServerSettings.Clone();
		newServerSettings.ServerTags = new ServerTags(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.SetServerSettings(newServerSettings);
		});
	}

	private void ProcessServerInfoPacket(ServerInfo packet)
	{
		ValidateStage(ConnectionStage.WaitingForSetup | ConnectionStage.SettingUp | ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.App.InGame.ServerName = packet.ServerName;
			_gameInstance.App.Interface.InGameView.OnServerInfoUpdate(packet.ServerName, packet.Motd, packet.MaxPlayers);
		});
	}

	private void ProcessWorldLoadProgressPacket(WorldLoadProgress packet)
	{
		ValidateStage(ConnectionStage.SettingUp);
		string status = string.Copy(packet.Status);
		int percentComplete = packet.PercentComplete;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			App app = _gameInstance.App;
			if (app.Stage == App.AppStage.GameLoading)
			{
				app.Interface.GameLoadingView.SetStatus(status, percentComplete);
			}
		});
	}

	private void ProcessWorldLoadFinishedPacket(WorldLoadFinished packet)
	{
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Expected O, but got Unknown
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Expected O, but got Unknown
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Expected O, but got Unknown
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Expected O, but got Unknown
		ValidateStage(ConnectionStage.SettingUp);
		ValidateReceivedAssets();
		SetStage(ConnectionStage.Playing);
		DateTime startTime = DateTime.UtcNow;
		EventWaitHandle waitHandle = new EventWaitHandle(initialState: false, EventResetMode.ManualReset);
		int tasksCount = 9;
		ThreadPool.QueueUserWorkItem(delegate
		{
			Stopwatch stopwatch9 = Stopwatch.StartNew();
			bool atlasNeedsUpdate = true;
			_gameInstance.MapModule.PrepareBlockTypes(_networkBlockTypes, _highestReceivedBlockId, atlasNeedsUpdate, ref _upcomingBlockTypes, ref _upcomingBlocksImageLocations, ref _upcomingBlocksAtlasSize, out var upcomingBlocksAtlasPixelsPerLevel, _threadCancellationToken);
			CancellationToken threadCancellationToken9 = _threadCancellationToken;
			if (!threadCancellationToken9.IsCancellationRequested)
			{
				ClientBlockType[] upcomingBlockTypes = new ClientBlockType[_upcomingBlockTypes.Length];
				Array.Copy(_upcomingBlockTypes, upcomingBlockTypes, _upcomingBlockTypes.Length);
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					_gameInstance.MapModule.SetupBlockTypes(upcomingBlockTypes);
					_gameInstance.MapModule.TextureAtlas.UpdateTexture2DMipMaps(upcomingBlocksAtlasPixelsPerLevel);
				});
				if (Interlocked.Decrement(ref tasksCount) == 0)
				{
					waitHandle.Set();
				}
				Logger.Info("PrepareBlockTypes: {0} ms", stopwatch9.ElapsedMilliseconds);
			}
		});
		ThreadPool.QueueUserWorkItem(delegate
		{
			Stopwatch stopwatch8 = Stopwatch.StartNew();
			_gameInstance.EntityStoreModule.PrepareAtlas(out _upcomingEntitiesImageLocations, out var upcomingEntitiesAtlasPixelsPerLevel, _threadCancellationToken);
			CancellationToken threadCancellationToken8 = _threadCancellationToken;
			if (!threadCancellationToken8.IsCancellationRequested)
			{
				Dictionary<string, Point> upcomingEntitiesImageLocations = _upcomingEntitiesImageLocations;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					_gameInstance.EntityStoreModule.CreateAtlasTexture(upcomingEntitiesImageLocations, upcomingEntitiesAtlasPixelsPerLevel);
					_gameInstance.EntityStoreModule.SetupModelsAndAnimations();
				});
				if (Interlocked.Decrement(ref tasksCount) == 0)
				{
					waitHandle.Set();
				}
				Logger.Info("PrepareEntitiesAtlas: {0}ms", stopwatch8.ElapsedMilliseconds);
			}
		});
		ThreadPool.QueueUserWorkItem(delegate
		{
			Stopwatch stopwatch7 = Stopwatch.StartNew();
			_gameInstance.ItemLibraryModule.PrepareItems(_networkItems, null, ref _upcomingItems, _threadCancellationToken);
			CancellationToken threadCancellationToken7 = _threadCancellationToken;
			if (!threadCancellationToken7.IsCancellationRequested)
			{
				_gameInstance.ItemLibraryModule.PrepareItemIconAtlas(_networkItems, out var upcomingItemIcons, out var iconAtlasPixels, out var iconAtlasWidth, out var iconAtlasHeight, _threadCancellationToken);
				threadCancellationToken7 = _threadCancellationToken;
				if (!threadCancellationToken7.IsCancellationRequested)
				{
					_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
					{
						_gameInstance.ItemLibraryModule.SetupItemIcons(upcomingItemIcons, iconAtlasPixels, iconAtlasWidth, iconAtlasHeight);
					});
					if (Interlocked.Decrement(ref tasksCount) == 0)
					{
						waitHandle.Set();
					}
					Logger.Info("PrepareItems: {0}ms", stopwatch7.ElapsedMilliseconds);
				}
			}
		});
		ThreadPool.QueueUserWorkItem(delegate
		{
			Stopwatch stopwatch6 = Stopwatch.StartNew();
			_gameInstance.ParticleSystemStoreModule.PrepareParticles(_networkParticleSpawners, out var upcomingParticles, out _upcomingParticleTextureInfo, out _upcomingUVMotionTexturePaths, _threadCancellationToken);
			CancellationToken threadCancellationToken6 = _threadCancellationToken;
			if (!threadCancellationToken6.IsCancellationRequested)
			{
				_gameInstance.TrailStoreModule.PrepareTrails(_networkTrails, out _upcomingTrailTextureInfo, _threadCancellationToken);
				threadCancellationToken6 = _threadCancellationToken;
				if (!threadCancellationToken6.IsCancellationRequested)
				{
					_gameInstance.FXModule.PrepareAtlas(_upcomingParticleTextureInfo, _upcomingTrailTextureInfo, out var upcomingFXImageLocations, out var upcomingAtlasPixelsPerLevel, _threadCancellationToken);
					threadCancellationToken6 = _threadCancellationToken;
					if (!threadCancellationToken6.IsCancellationRequested)
					{
						_gameInstance.FXModule.PrepareUVMotionTextureArray(_upcomingUVMotionTexturePaths, out var upcomingUVMotionTextureArrayPixelsPerLevel);
						_gameInstance.EntityStoreModule.PrepareEntityEffects(_entityEffects, out var upcomingEntityEffects);
						_gameInstance.EntityStoreModule.PrepareModelVFXs(_modelVFXs, out var upcomingModelVFXs);
						_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
						{
							_gameInstance.ParticleSystemStoreModule.SetupParticleSpawners(_networkParticleSystems, _networkParticleSpawners, upcomingParticles, _upcomingUVMotionTexturePaths);
							_gameInstance.TrailStoreModule.SetupTrailSettings(_networkTrails);
							_gameInstance.FXModule.CreateUVMotionTextureArray(upcomingUVMotionTextureArrayPixelsPerLevel);
							_gameInstance.FXModule.CreateAtlasTextures(upcomingFXImageLocations, upcomingAtlasPixelsPerLevel);
							_gameInstance.EntityStoreModule.SetupEntityEffects(upcomingEntityEffects);
							_gameInstance.EntityStoreModule.SetupModelVFXs(upcomingModelVFXs);
						});
						if (Interlocked.Decrement(ref tasksCount) == 0)
						{
							waitHandle.Set();
						}
						Logger.Info("PrepareParticles: {0}ms", stopwatch6.ElapsedMilliseconds);
					}
				}
			}
		});
		ThreadPool.QueueUserWorkItem(delegate
		{
			Stopwatch stopwatch5 = Stopwatch.StartNew();
			_gameInstance.ItemLibraryModule.PrepareItemPlayerAnimations(_networkItemPlayerAnimations, out _upcomingItemPlayerAnimations);
			CancellationToken threadCancellationToken5 = _threadCancellationToken;
			if (!threadCancellationToken5.IsCancellationRequested)
			{
				Dictionary<string, ClientItemPlayerAnimations> upcomingItemAnimations = _upcomingItemPlayerAnimations;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					_gameInstance.ItemLibraryModule.SetupItemPlayerAnimations(upcomingItemAnimations);
				});
				if (Interlocked.Decrement(ref tasksCount) == 0)
				{
					waitHandle.Set();
				}
				Logger.Info("PrepareItemAnimations: {0}ms", stopwatch5.ElapsedMilliseconds);
			}
		});
		ThreadPool.QueueUserWorkItem(delegate
		{
			Stopwatch stopwatch4 = Stopwatch.StartNew();
			_gameInstance.InteractionModule.PrepareInteractions(_networkInteractions, out _upcomingInteractions);
			CancellationToken threadCancellationToken4 = _threadCancellationToken;
			if (!threadCancellationToken4.IsCancellationRequested)
			{
				ClientInteraction[] upcomingInteractions = _upcomingInteractions;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					_gameInstance.InteractionModule.SetupInteractions(upcomingInteractions);
				});
				if (Interlocked.Decrement(ref tasksCount) == 0)
				{
					waitHandle.Set();
				}
				Trace.WriteLine($"PrepareInterations: {stopwatch4.ElapsedMilliseconds}ms", "Loading world");
			}
		});
		ThreadPool.QueueUserWorkItem(delegate
		{
			Stopwatch stopwatch3 = Stopwatch.StartNew();
			_gameInstance.InteractionModule.PrepareRootInteractions(_networkRootInteractions, out _upcomingRootInteractions);
			CancellationToken threadCancellationToken3 = _threadCancellationToken;
			if (!threadCancellationToken3.IsCancellationRequested)
			{
				ClientRootInteraction[] upcomingRootInteractions = _upcomingRootInteractions;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					_gameInstance.InteractionModule.SetupRootInteractions(upcomingRootInteractions);
				});
				if (Interlocked.Decrement(ref tasksCount) == 0)
				{
					waitHandle.Set();
				}
				Trace.WriteLine($"PrepareRootInterations: {stopwatch3.ElapsedMilliseconds}ms", "Loading world");
			}
		});
		ThreadPool.QueueUserWorkItem(delegate
		{
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			_gameInstance.WorldMapModule.PrepareTextureAtlas(out _upcomingWorldMapImageLocations, out var upcomingWorldMapAtlasPixelsPerLevel, _threadCancellationToken);
			CancellationToken threadCancellationToken2 = _threadCancellationToken;
			if (!threadCancellationToken2.IsCancellationRequested)
			{
				Dictionary<string, WorldMapModule.Texture> upcomingWorldMapImageLocations = _upcomingWorldMapImageLocations;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					_gameInstance.WorldMapModule.BuildTextureAtlas(upcomingWorldMapImageLocations, upcomingWorldMapAtlasPixelsPerLevel);
				});
				if (Interlocked.Decrement(ref tasksCount) == 0)
				{
					waitHandle.Set();
				}
				Logger.Info("PrepareWorldMap: {0}ms", stopwatch2.ElapsedMilliseconds);
			}
		});
		ThreadPool.QueueUserWorkItem(delegate
		{
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Expected O, but got Unknown
			Stopwatch stopwatch = Stopwatch.StartNew();
			AmbienceFX[] clonedAmbienceFXs = (AmbienceFX[])(object)new AmbienceFX[_networkAmbienceFXs.Length];
			for (int i = 0; i < _networkAmbienceFXs.Length; i++)
			{
				clonedAmbienceFXs[i] = new AmbienceFX(_networkAmbienceFXs[i]);
			}
			_gameInstance.AmbienceFXModule.PrepareAmbienceFXs(clonedAmbienceFXs, out var ambienceFXSettings);
			Dictionary<string, WwiseResource> upcomingWwiseIds = null;
			_gameInstance.AudioModule.PrepareSoundBanks(out upcomingWwiseIds);
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				_gameInstance.AudioModule.SetupSoundBanks(upcomingWwiseIds);
				_gameInstance.AmbienceFXModule.SetupAmbienceFXs(clonedAmbienceFXs, ambienceFXSettings);
				_gameInstance.AudioModule.OnSoundEffectCollectionChanged();
				_gameInstance.InteractionModule.BlockPreview.RegisterSoundObjectReference();
			});
			if (Interlocked.Decrement(ref tasksCount) == 0)
			{
				waitHandle.Set();
			}
			Logger.Info("PrepareAmbienceFX: {0}ms", stopwatch.ElapsedMilliseconds);
		});
		CancellationToken threadCancellationToken;
		do
		{
			threadCancellationToken = _threadCancellationToken;
		}
		while (!threadCancellationToken.IsCancellationRequested && !waitHandle.WaitOne(100));
		threadCancellationToken = _threadCancellationToken;
		if (threadCancellationToken.IsCancellationRequested)
		{
			return;
		}
		_gameInstance.Connection.SendPacket((ProtoPacket)new ViewRadius(_gameInstance.App.Settings.ViewDistance));
		ClientPlayerSkin playerSkin = _gameInstance.App.PlayerSkin;
		PlayerSkin val = new PlayerSkin((BodyType)(playerSkin.BodyType != CharacterBodyType.Masculine), playerSkin.SkinTone, playerSkin.Face, playerSkin.Eyes?.ToString(), playerSkin.FacialHair?.ToString(), playerSkin.Haircut?.ToString(), playerSkin.Eyebrows?.ToString(), playerSkin.Pants?.ToString(), playerSkin.Overpants?.ToString(), playerSkin.Undertop?.ToString(), playerSkin.Overtop?.ToString(), playerSkin.Shoes?.ToString(), playerSkin.HeadAccessory?.ToString(), playerSkin.FaceAccessory?.ToString(), playerSkin.EarAccessory?.ToString(), playerSkin.SkinFeature?.ToString(), playerSkin.Gloves?.ToString());
		_gameInstance.Connection.SendPacket((ProtoPacket)new PlayerOptions(new PlayerOptions(val)));
		ServerSettings upcomingServerSettings = _upcomingServerSettings.Clone();
		_gameInstance.ItemLibraryModule.PrepareItemUVs(ref _upcomingItems, _upcomingEntitiesImageLocations, _threadCancellationToken);
		Dictionary<string, ClientItemBase> upcomingItems = new Dictionary<string, ClientItemBase>(_upcomingItems);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			HytaleClient.Interface.Interface @interface = _gameInstance.App.Interface;
			@interface.InGameView.ClearMarkupError();
			try
			{
				@interface.InGameCustomUIProvider.LoadDocuments();
			}
			catch (TextParser.TextParserException ex)
			{
				@interface.InGameCustomUIProvider.ClearDocuments();
				if (!@interface.App.Settings.DiagnosticMode)
				{
					_gameInstance.DisconnectWithReason("Failed to load CustomUI documents", ex);
					return;
				}
				@interface.InGameView.DisplayMarkupError(ex.RawMessage, ex.Span);
			}
			catch (Exception exception)
			{
				@interface.InGameCustomUIProvider.ClearDocuments();
				_gameInstance.DisconnectWithReason("Failed to load CustomUI documents", exception);
				return;
			}
			try
			{
				@interface.InGameCustomUIProvider.LoadTextures(@interface.Desktop.Scale > 1f);
			}
			catch (Exception exception2)
			{
				_gameInstance.DisconnectWithReason("Failed to load CustomUI textures", exception2);
				return;
			}
			_gameInstance.SetServerSettings(upcomingServerSettings);
			_gameInstance.ItemLibraryModule.SetupItems(upcomingItems);
			@interface.TriggerEvent("items.initialized", upcomingItems);
			@interface.InGameView.ReloadAssetTextures();
			_gameInstance.UpdateAtlasSizes();
			_gameInstance.OnSetupComplete();
			DateTime utcNow = DateTime.UtcNow;
			Logger.Info("Global: {0}ms", (utcNow - startTime).TotalMilliseconds);
		});
	}

	private void ProcessUpdateWeathersPacket(UpdateWeathers packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Invalid comparison between Unknown and I4
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] Weather: Starting {packet.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if (packet.MaxId > _upcomingServerSettings.Weathers.Length)
				{
					Array.Resize(ref _upcomingServerSettings.Weathers, packet.MaxId);
				}
				if ((int)packet.Type == 1)
				{
					foreach (KeyValuePair<int, Weather> weather in packet.Weathers)
					{
						_upcomingServerSettings.Weathers[weather.Key] = new ClientWeather(weather.Value);
						_upcomingServerSettings.WeatherIndicesByIds[weather.Value.Id] = weather.Key;
					}
				}
				ServerSettings newServerSettings = _upcomingServerSettings.Clone();
				UpdateType updateType = packet.Type;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_0049: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.SetServerSettings(newServerSettings);
					_gameInstance.WeatherModule.OnWeatherCollectionChanged();
					_gameInstance.App.DevTools.Info($"[AssetUpdate] Weather: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.Weather);
		if (_upcomingServerSettings.Weathers == null)
		{
			_upcomingServerSettings.Weathers = new ClientWeather[packet.MaxId];
			_upcomingServerSettings.WeatherIndicesByIds = new Dictionary<string, int>();
		}
		foreach (KeyValuePair<int, Weather> weather2 in packet.Weathers)
		{
			_upcomingServerSettings.Weathers[weather2.Key] = new ClientWeather(weather2.Value);
			_upcomingServerSettings.WeatherIndicesByIds[weather2.Value.Id] = weather2.Key;
		}
		FinishedReceivedAssetType(AssetType.Weather);
	}

	private void ProcessUpdateEntityStatTypes(UpdateEntityStatTypes packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Invalid comparison between Unknown and I4
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] EntityStats: Starting {packet.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if (packet.MaxId > _upcomingServerSettings.EntityStatTypes.Length)
				{
					Array.Resize(ref _upcomingServerSettings.EntityStatTypes, packet.MaxId);
					Entity[] allEntities = _gameInstance.EntityStoreModule.GetAllEntities();
					foreach (Entity entity in allEntities)
					{
						if (entity != null)
						{
							Array.Resize(ref entity._entityStats, packet.MaxId);
							Array.Resize(ref entity._serverEntityStats, packet.MaxId);
						}
					}
				}
				if ((int)packet.Type == 1)
				{
					foreach (KeyValuePair<int, EntityStatType> type2 in packet.Types)
					{
						_upcomingServerSettings.EntityStatTypes[type2.Key] = new ClientEntityStatType(type2.Value);
					}
				}
				ServerSettings newServerSettings = _upcomingServerSettings.Clone();
				UpdateType updateType = packet.Type;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_0044: Unknown result type (might be due to invalid IL or missing references)
					DefaultEntityStats.Update(newServerSettings.EntityStatTypes);
					_gameInstance.SetServerSettings(newServerSettings);
					_gameInstance.App.DevTools.Info($"[AssetUpdate] EntityStats: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.EntityStatTypes);
		if (_upcomingServerSettings.EntityStatTypes == null)
		{
			_upcomingServerSettings.EntityStatTypes = new ClientEntityStatType[packet.MaxId];
		}
		foreach (KeyValuePair<int, EntityStatType> type3 in packet.Types)
		{
			_upcomingServerSettings.EntityStatTypes[type3.Key] = new ClientEntityStatType(type3.Value);
		}
		DefaultEntityStats.Update(_upcomingServerSettings.EntityStatTypes);
		FinishedReceivedAssetType(AssetType.EntityStatTypes);
	}

	private void ProcessUpdateEnvironmentsPacket(UpdateEnvironments packet)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Invalid comparison between Unknown and I4
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] Environment: Starting {packet.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if ((int)packet.Type == 1)
				{
					if (packet.MaxId > _upcomingServerSettings.Environments.Length)
					{
						ClientWorldEnvironment[] array = new ClientWorldEnvironment[packet.MaxId];
						Array.Copy(_upcomingServerSettings.Environments, array, _upcomingServerSettings.Environments.Length);
						_upcomingServerSettings.Environments = array;
					}
					foreach (KeyValuePair<int, WorldEnvironment> environment in packet.Environments)
					{
						_upcomingServerSettings.Environments[environment.Key] = new ClientWorldEnvironment(environment.Value);
					}
				}
				else
				{
					foreach (KeyValuePair<int, WorldEnvironment> environment2 in packet.Environments)
					{
						_upcomingServerSettings.Environments[environment2.Key] = null;
					}
				}
				ServerSettings newServerSettings = _upcomingServerSettings.Clone();
				bool rebuildMapGeometry = packet.RebuildMapGeometry;
				UpdateType updateType = packet.Type;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_006b: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.SetServerSettings(newServerSettings);
					_gameInstance.WeatherModule.OnEnvironmentCollectionChanged();
					if (rebuildMapGeometry)
					{
						_gameInstance.MapModule.DoWithMapGeometryBuilderPaused(discardAllRenderedChunks: true, null);
					}
					_gameInstance.App.DevTools.Info($"[AssetUpdate] Environment: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.Environment);
		_upcomingServerSettings.Environments = new ClientWorldEnvironment[packet.MaxId];
		foreach (KeyValuePair<int, WorldEnvironment> environment3 in packet.Environments)
		{
			_upcomingServerSettings.Environments[environment3.Key] = new ClientWorldEnvironment(environment3.Value);
		}
		FinishedReceivedAssetType(AssetType.Environment);
	}

	private void ProcessUpdateFluidFXPacket(UpdateFluidFX packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] FluidFX: Starting {packet.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if (packet.MaxId > _upcomingServerSettings.FluidFXs.Length)
				{
					FluidFX[] array = (FluidFX[])(object)new FluidFX[packet.MaxId];
					Array.Copy(_upcomingServerSettings.FluidFXs, array, _upcomingServerSettings.FluidFXs.Length);
					_upcomingServerSettings.FluidFXs = array;
				}
				foreach (KeyValuePair<int, FluidFX> item in packet.FluidFX_)
				{
					_upcomingServerSettings.FluidFXs[item.Key] = item.Value;
				}
				ServerSettings newServerSettings = _upcomingServerSettings.Clone();
				UpdateType updateType = packet.Type;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_0049: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.SetServerSettings(newServerSettings);
					_gameInstance.WeatherModule.OnFluidFXChanged();
					_gameInstance.App.DevTools.Info($"[AssetUpdate] FluidFX: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.FluidFX);
		if (_upcomingServerSettings.FluidFXs == null)
		{
			_upcomingServerSettings.FluidFXs = (FluidFX[])(object)new FluidFX[packet.MaxId];
		}
		foreach (KeyValuePair<int, FluidFX> item2 in packet.FluidFX_)
		{
			_upcomingServerSettings.FluidFXs[item2.Key] = item2.Value;
		}
		FinishedReceivedAssetType(AssetType.FluidFX);
	}

	private void ProcessUpdateRootInteractions(UpdateRootInteractions packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Invalid comparison between Unknown and I4
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Expected O, but got Unknown
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.Chat.Log($"[AssetUpdate] RootInteractions: Starting {packet.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if (packet.MaxId > _networkRootInteractions.Length)
				{
					RootInteraction[] array = (RootInteraction[])(object)new RootInteraction[packet.MaxId];
					Array.Copy(_networkRootInteractions, array, _networkRootInteractions.Length);
					_networkRootInteractions = array;
				}
				ClientRootInteraction[] upcomingRootInteractions;
				lock (_setupLock)
				{
					if ((int)packet.Type == 1)
					{
						foreach (KeyValuePair<int, RootInteraction> interaction in packet.Interactions)
						{
							_networkRootInteractions[interaction.Key] = new RootInteraction(interaction.Value);
						}
					}
					else
					{
						foreach (KeyValuePair<int, RootInteraction> interaction2 in packet.Interactions)
						{
							_networkRootInteractions[interaction2.Key] = null;
						}
					}
					_gameInstance.InteractionModule.PrepareRootInteractions(_networkRootInteractions, out _upcomingRootInteractions);
					CancellationToken threadCancellationToken = _threadCancellationToken;
					if (threadCancellationToken.IsCancellationRequested)
					{
						return;
					}
					upcomingRootInteractions = _upcomingRootInteractions;
				}
				UpdateType updateType = packet.Type;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_0033: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.InteractionModule.SetupRootInteractions(upcomingRootInteractions);
					_gameInstance.Chat.Log($"[AssetUpdate] RootInteractions: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.RootInteractions);
		if (_networkRootInteractions == null)
		{
			_networkRootInteractions = (RootInteraction[])(object)new RootInteraction[packet.MaxId];
		}
		foreach (KeyValuePair<int, RootInteraction> interaction3 in packet.Interactions)
		{
			_networkRootInteractions[interaction3.Key] = new RootInteraction(interaction3.Value);
		}
		FinishedReceivedAssetType(AssetType.RootInteractions);
	}

	private void ProcessUpdateInteractions(UpdateInteractions packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Invalid comparison between Unknown and I4
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Expected O, but got Unknown
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] Interactions: Starting {packet.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if (packet.MaxId > _networkInteractions.Length)
				{
					Interaction[] array = (Interaction[])(object)new Interaction[packet.MaxId];
					Array.Copy(_networkInteractions, array, _networkInteractions.Length);
					_networkInteractions = array;
				}
				ClientInteraction[] upcomingInteractions;
				lock (_setupLock)
				{
					if ((int)packet.Type == 1)
					{
						foreach (KeyValuePair<int, Interaction> interaction in packet.Interactions)
						{
							_networkInteractions[interaction.Key] = new Interaction(interaction.Value);
						}
					}
					else
					{
						foreach (KeyValuePair<int, Interaction> interaction2 in packet.Interactions)
						{
							_networkInteractions[interaction2.Key] = null;
						}
					}
					_gameInstance.InteractionModule.PrepareInteractions(_networkInteractions, out _upcomingInteractions);
					CancellationToken threadCancellationToken = _threadCancellationToken;
					if (threadCancellationToken.IsCancellationRequested)
					{
						return;
					}
					upcomingInteractions = _upcomingInteractions;
				}
				UpdateType updateType = packet.Type;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_0038: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.InteractionModule.SetupInteractions(upcomingInteractions);
					_gameInstance.App.DevTools.Info($"[AssetUpdate] Interactions: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.Interactions);
		if (_networkInteractions == null)
		{
			_networkInteractions = (Interaction[])(object)new Interaction[packet.MaxId];
		}
		foreach (KeyValuePair<int, Interaction> interaction3 in packet.Interactions)
		{
			_networkInteractions[interaction3.Key] = new Interaction(interaction3.Value);
		}
		FinishedReceivedAssetType(AssetType.Interactions);
	}

	private void ProcessUpdateUnarmedInteractions(UpdateUnarmedInteractions packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if ((int)val != 1)
			{
				throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
			}
			ValidateStage(ConnectionStage.Playing);
			_gameInstance.App.DevTools.Info($"[AssetUpdate] UnarmedInteractions: Starting {packet.Type}");
			Stopwatch stopwatch = Stopwatch.StartNew();
			_upcomingServerSettings.UnarmedInteractions = packet.Interactions;
			ServerSettings newServerSettings = _upcomingServerSettings.Clone();
			UpdateType updateType = packet.Type;
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_0033: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.SetServerSettings(newServerSettings);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] UnarmedInteractions: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
			});
		}
		else
		{
			ValidateStage(ConnectionStage.SettingUp);
			ReceivedAssetType(AssetType.UnarmedInteractions);
			_upcomingServerSettings.UnarmedInteractions = packet.Interactions;
			FinishedReceivedAssetType(AssetType.UnarmedInteractions);
		}
	}

	private void ProcessUpdateRepulsionConfig(UpdateRepulsionConfig packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] RepulsionConfig: Starting {packet.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if (packet.MaxId > _upcomingServerSettings.RepulsionConfigs.Length)
				{
					ClientRepulsionConfig[] array = new ClientRepulsionConfig[packet.MaxId];
					Array.Copy(_upcomingServerSettings.RepulsionConfigs, array, _upcomingServerSettings.RepulsionConfigs.Length);
					_upcomingServerSettings.RepulsionConfigs = array;
				}
				foreach (KeyValuePair<int, RepulsionConfig> repulsionConfig in packet.RepulsionConfigs)
				{
					ClientRepulsionConfig clientRepulsionConfig = new ClientRepulsionConfig();
					ClientRepulsionConfigInitializer.Initialize(repulsionConfig.Value, ref clientRepulsionConfig);
					_upcomingServerSettings.RepulsionConfigs[repulsionConfig.Key] = clientRepulsionConfig;
				}
				ServerSettings newServerSettings = _upcomingServerSettings.Clone();
				UpdateType updateType = packet.Type;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_0033: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.SetServerSettings(newServerSettings);
					_gameInstance.App.DevTools.Info($"[AssetUpdate] RepulsionConfig: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.RepulsionConfig);
		if (_upcomingServerSettings.RepulsionConfigs == null)
		{
			_upcomingServerSettings.RepulsionConfigs = new ClientRepulsionConfig[packet.MaxId];
		}
		foreach (KeyValuePair<int, RepulsionConfig> repulsionConfig2 in packet.RepulsionConfigs)
		{
			ClientRepulsionConfig clientRepulsionConfig2 = new ClientRepulsionConfig();
			ClientRepulsionConfigInitializer.Initialize(repulsionConfig2.Value, ref clientRepulsionConfig2);
			_upcomingServerSettings.RepulsionConfigs[repulsionConfig2.Key] = clientRepulsionConfig2;
		}
		FinishedReceivedAssetType(AssetType.RepulsionConfig);
	}

	private void ProcessUpdateHitboxCollisionConfig(UpdateHitboxCollisionConfig packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] HitboxCollisionConfig: Starting {packet.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if (packet.MaxId > _upcomingServerSettings.HitboxCollisionConfigs.Length)
				{
					ClientHitboxCollisionConfig[] array = new ClientHitboxCollisionConfig[packet.MaxId];
					Array.Copy(_upcomingServerSettings.HitboxCollisionConfigs, array, _upcomingServerSettings.HitboxCollisionConfigs.Length);
					_upcomingServerSettings.HitboxCollisionConfigs = array;
				}
				foreach (KeyValuePair<int, HitboxCollisionConfig> hitboxCollisionConfig in packet.HitboxCollisionConfigs)
				{
					ClientHitboxCollisionConfig clientHitboxCollisionConfig = new ClientHitboxCollisionConfig();
					ClientHitboxCollisionConfigInitializer.Initialize(hitboxCollisionConfig.Value, ref clientHitboxCollisionConfig);
					_upcomingServerSettings.HitboxCollisionConfigs[hitboxCollisionConfig.Key] = clientHitboxCollisionConfig;
				}
				ServerSettings newServerSettings = _upcomingServerSettings.Clone();
				UpdateType updateType = packet.Type;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_0033: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.SetServerSettings(newServerSettings);
					_gameInstance.App.DevTools.Info($"[AssetUpdate] HitboxCollisionConfig: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.HitboxCollisionConfig);
		if (_upcomingServerSettings.HitboxCollisionConfigs == null)
		{
			_upcomingServerSettings.HitboxCollisionConfigs = new ClientHitboxCollisionConfig[packet.MaxId];
		}
		foreach (KeyValuePair<int, HitboxCollisionConfig> hitboxCollisionConfig2 in packet.HitboxCollisionConfigs)
		{
			ClientHitboxCollisionConfig clientHitboxCollisionConfig2 = new ClientHitboxCollisionConfig();
			ClientHitboxCollisionConfigInitializer.Initialize(hitboxCollisionConfig2.Value, ref clientHitboxCollisionConfig2);
			_upcomingServerSettings.HitboxCollisionConfigs[hitboxCollisionConfig2.Key] = clientHitboxCollisionConfig2;
		}
		FinishedReceivedAssetType(AssetType.HitboxCollisionConfig);
	}

	private void ProcessUpdateEntityUIComponents(UpdateEntityUIComponents packet)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Invalid comparison between Unknown and I4
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Invalid comparison between Unknown and I4
		InGameView inGameView = _gameInstance.App.Interface.InGameView;
		UpdateType type = packet.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 <= 1)
			{
				ValidateStage(ConnectionStage.Playing);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] EntityUIComponentConfig: Starting {packet.Type}");
				Stopwatch stopwatch = Stopwatch.StartNew();
				if (packet.MaxId > _upcomingServerSettings.EntityUIComponents.Length)
				{
					ClientEntityUIComponent[] array = new ClientEntityUIComponent[packet.MaxId];
					Array.Copy(_upcomingServerSettings.EntityUIComponents, array, _upcomingServerSettings.EntityUIComponents.Length);
					_upcomingServerSettings.EntityUIComponents = array;
				}
				foreach (KeyValuePair<int, EntityUIComponent> component in packet.Components)
				{
					EntityUIType type2 = component.Value.Type;
					EntityUIType val2 = type2;
					if ((int)val2 != 0)
					{
						if ((int)val2 == 1)
						{
							_upcomingServerSettings.EntityUIComponents[component.Key] = new ClientCombatTextUIComponent(component.Key, component.Value, inGameView.EntityUIContainer.CombatTextUIComponentRenderer);
						}
					}
					else
					{
						_upcomingServerSettings.EntityUIComponents[component.Key] = new ClientEntityStatUIComponent(component.Key, component.Value, inGameView.EntityUIContainer.EntityStatUIComponentRenderer);
					}
				}
				ServerSettings newServerSettings = _upcomingServerSettings.Clone();
				UpdateType updateType = packet.Type;
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					//IL_0033: Unknown result type (might be due to invalid IL or missing references)
					_gameInstance.SetServerSettings(newServerSettings);
					_gameInstance.App.DevTools.Info($"[AssetUpdate] EntityUIComponentConfig: Finished {updateType} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
				});
				return;
			}
			throw new Exception($"Received invalid packet UpdateType for {((object)packet).GetType().Name} at {_stage} with type {packet.Type}.");
		}
		ValidateStage(ConnectionStage.SettingUp);
		ReceivedAssetType(AssetType.EntityUIComponents);
		if (_upcomingServerSettings.EntityUIComponents == null)
		{
			_upcomingServerSettings.EntityUIComponents = new ClientEntityUIComponent[packet.MaxId];
		}
		foreach (KeyValuePair<int, EntityUIComponent> component2 in packet.Components)
		{
			EntityUIType type3 = component2.Value.Type;
			EntityUIType val3 = type3;
			if ((int)val3 != 0)
			{
				if ((int)val3 == 1)
				{
					_upcomingServerSettings.EntityUIComponents[component2.Key] = new ClientCombatTextUIComponent(component2.Key, component2.Value, inGameView.EntityUIContainer.CombatTextUIComponentRenderer);
				}
			}
			else
			{
				_upcomingServerSettings.EntityUIComponents[component2.Key] = new ClientEntityStatUIComponent(component2.Key, component2.Value, inGameView.EntityUIContainer.EntityStatUIComponentRenderer);
			}
		}
		FinishedReceivedAssetType(AssetType.EntityUIComponents);
	}

	private void ValidateSingleplayer()
	{
		if (_gameInstance.App.SingleplayerServer == null)
		{
			throw new Exception("Received " + _stageValidationPacketId + " at but not connected to a singleplayer server!");
		}
		_stageValidationPacketId = string.Empty;
	}

	private void ProcessRequestServerAccess(RequestServerAccess packet)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		ValidateSingleplayer();
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.Chat.Log("Port Mapping is not implemented!");
		});
		_gameInstance.Connection.SendPacket((ProtoPacket)new UpdateServerAccess((Access)0, (HostAddress[])(object)new HostAddress[0]));
	}

	private void ProcessEditorBlocksChangePacket(EditorBlocksChange packet)
	{
		ValidateStage(ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			if (packet.Selection != null)
			{
				_gameInstance.BuilderToolsModule.SelectionArea.UpdateSelection(new Vector3(packet.Selection.MinX, packet.Selection.MinY, packet.Selection.MinZ), new Vector3(packet.Selection.MaxX, packet.Selection.MaxY, packet.Selection.MaxZ));
			}
			if (packet.BlocksChange != null)
			{
				_gameInstance.BuilderToolsModule.Paste.UpdateBlockSet(packet.BlocksChange);
			}
		});
	}

	private void ProcessBuilderToolShowAnchorPacket(BuilderToolShowAnchor packet)
	{
		ValidateStage(ConnectionStage.Playing);
		Vector3 position = new Vector3(packet.X, packet.Y, packet.Z);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.BuilderToolsModule.Anchor.ShowAnchor(position);
		});
	}

	private void ProcessBuilderToolSelectionToolReplyWithClipboard(BuilderToolSelectionToolReplyWithClipboard packet)
	{
		ValidateStage(ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.BuilderToolsModule.PlaySelection.TryEnterTranslationModeWithClipboard(packet.BlocksChange);
		});
	}

	private void ProcessBuilderToolHideAnchorPacket(BuilderToolHideAnchors packet)
	{
		ValidateStage(ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.BuilderToolsModule.Anchor.HideAnchors();
		});
	}

	private void ProcessUpdateTrailsPacket(UpdateTrails packet)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Invalid comparison between Unknown and I4
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		UpdateTrails updateTrails = new UpdateTrails(packet);
		UpdateType type = updateTrails.Type;
		UpdateType val = type;
		if ((int)val != 0)
		{
			if (val - 1 > 1)
			{
				throw new Exception($"Received invalid packet UpdateType for {((object)updateTrails).GetType().Name} at {_stage} with type {updateTrails.Type}.");
			}
			ValidateStage(ConnectionStage.Playing);
			_gameInstance.App.DevTools.Info($"[AssetUpdate] Trails: Starting {updateTrails.Type}");
			Stopwatch stopwatch = Stopwatch.StartNew();
			if ((int)updateTrails.Type == 1)
			{
				foreach (KeyValuePair<string, Trail> trail in updateTrails.Trails)
				{
					_networkTrails[trail.Key] = trail.Value;
				}
			}
			else
			{
				foreach (KeyValuePair<string, Trail> trail2 in updateTrails.Trails)
				{
					_networkTrails.Remove(trail2.Key);
				}
			}
			Dictionary<string, Rectangle> upcomingFXImageLocations;
			byte[][] upcomingFXAtlasPixelsPerLevel;
			lock (_setupLock)
			{
				_gameInstance.TrailStoreModule.PrepareTrails(_networkTrails, out _upcomingTrailTextureInfo, _threadCancellationToken);
				CancellationToken threadCancellationToken = _threadCancellationToken;
				if (threadCancellationToken.IsCancellationRequested)
				{
					return;
				}
				_gameInstance.FXModule.PrepareAtlas(_upcomingParticleTextureInfo, _upcomingTrailTextureInfo, out upcomingFXImageLocations, out upcomingFXAtlasPixelsPerLevel, _threadCancellationToken);
				threadCancellationToken = _threadCancellationToken;
				if (threadCancellationToken.IsCancellationRequested)
				{
					return;
				}
			}
			Dictionary<string, Trail> upcomingTrails = new Dictionary<string, Trail>(_networkTrails);
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
				_gameInstance.TrailStoreModule.SetupTrailSettings(upcomingTrails);
				_gameInstance.FXModule.CreateAtlasTextures(upcomingFXImageLocations, upcomingFXAtlasPixelsPerLevel);
				_gameInstance.EntityStoreModule.RebuildRenderers(itemOnly: false);
				_gameInstance.ParticleSystemStoreModule.ResetParticleSystems(skipEntities: true);
				_gameInstance.App.DevTools.Info($"[AssetUpdate] Trails: Finished {updateTrails.Type} in {TimeHelper.FormatMillis(stopwatch.ElapsedMilliseconds)}");
			});
		}
		else
		{
			ValidateStage(ConnectionStage.SettingUp);
			ReceivedAssetType(AssetType.Trails);
			_networkTrails = updateTrails.Trails;
			FinishedReceivedAssetType(AssetType.Trails);
		}
	}

	private void ProcessTriggerEditorUpdateScriptReply(TriggerEditorUpdateScriptReply packet)
	{
	}

	private void ProcessTriggerEditorRequestScriptsReply(TriggerEditorRequestScriptsReply packet)
	{
	}

	private void ProcessTriggerEditorRequestScriptReply(TriggerEditorRequestScriptReply packet)
	{
	}

	private void ProcessTriggerEditorRequestBlockReply(TriggerEditorRequestBlockReply packet)
	{
	}

	private void ProcessTriggerEditorUpdateBlockReply(TriggerEditorUpdateBlockReply packet)
	{
	}

	private void ProcessUpdateTimePacket(UpdateTime packet)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		InstantData gameTime = new InstantData(packet.GameTime);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.TimeModule.ProcessGameTimeFromServer(gameTime);
		});
	}

	private void ProcessUpdateTimeSettingsPacket(UpdateTimeSettings packet)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		ValidateStage(ConnectionStage.SettingUp | ConnectionStage.Playing);
		UpdateTimeSettings time = new UpdateTimeSettings(packet);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			TimeModule.SecondsPerGameDay = time.SecondsPerGameDay;
			WeatherModule.TotalMoonPhases = (byte)time.TotalMoonPhases;
			WeatherModule.DaylightPortion = (byte)time.DaylightPortion;
			_gameInstance.TimeModule.IsServerTimePaused = time.TimePaused;
			_gameInstance.WeatherModule.OnDaylightPortionChanged();
		});
	}

	private void ProcessUpdateWeatherPacket(UpdateWeather packet)
	{
		ValidateStage(ConnectionStage.Playing);
		int weatherIndex = packet.WeatherIndex;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.WeatherModule.SetServerWeather(weatherIndex);
		});
	}

	private void ProcessUpdateEditorTimeOverride(UpdateEditorTimeOverride packet)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		ValidateStage(ConnectionStage.Playing);
		InstantData gameTime = new InstantData(packet.GameTime);
		bool isPaused = packet.Paused;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.TimeModule.ProcessEditorTimeOverride(gameTime, isPaused);
		});
	}

	private void ProcessClearEditorTimeOverride(ClearEditorTimeOverride packet)
	{
		ValidateStage(ConnectionStage.Playing);
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.TimeModule.ProcessClearEditorTimeOverride();
		});
	}

	private void ProcessUpdateEditorWeatherOverride(UpdateEditorWeatherOverride packet)
	{
		ValidateStage(ConnectionStage.Playing);
		int weatherIndex = packet.WeatherIndex;
		_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
		{
			_gameInstance.WeatherModule.SetEditorWeatherOverride(weatherIndex);
		});
	}
}
