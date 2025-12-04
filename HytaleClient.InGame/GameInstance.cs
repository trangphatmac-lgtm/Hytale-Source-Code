#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Hypixel.ProtoPlus;
using HytaleClient.Application;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.Items;
using HytaleClient.Data.Map;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame.Commands;
using HytaleClient.InGame.Modules;
using HytaleClient.InGame.Modules.AmbienceFX;
using HytaleClient.InGame.Modules.Audio;
using HytaleClient.InGame.Modules.BuilderTools;
using HytaleClient.InGame.Modules.Camera;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.InGame.Modules.CharacterController;
using HytaleClient.InGame.Modules.Collision;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.ImmersiveScreen;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.InGame.Modules.InterfaceRenderPreview;
using HytaleClient.InGame.Modules.Machinima;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.InGame.Modules.Particles;
using HytaleClient.InGame.Modules.Shortcuts;
using HytaleClient.InGame.Modules.Trails;
using HytaleClient.InGame.Modules.WorldMap;
using HytaleClient.Interface;
using HytaleClient.Math;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using SDL2;

namespace HytaleClient.InGame;

internal class GameInstance : Disposable
{
	private enum GameInstanceStage
	{
		WaitingForJoinWorldPacket,
		WaitingForNearbyChunks,
		WorldJoined
	}

	public enum WireframePass
	{
		Off,
		OnAll,
		OnEntities,
		OnMapOpaque,
		OnMapAlphaTested,
		OnMapAnim,
		OnMapAlphaBlend,
		OnSky
	}

	private enum RenderPassId
	{
		ShadowMap,
		FirstPerson,
		MapNear_Opaque,
		MapNear_AlphaTested,
		MapAnimated,
		MapFar_Opaque,
		MapFar_AlphaTested,
		Entities,
		Sky,
		MapAlphaBlended,
		VFX,
		Nameplates,
		PostFX,
		Max
	}

	public enum RenderingProfile
	{
		FullFrame,
		QueuedActions,
		OnNewFrame,
		ChunksPrepare,
		EntitiesPrepare,
		ModulesTick,
		ModulesPreUpdate,
		InteractionModule,
		LightsPrepare,
		ChunksGather,
		ChunksPrepareForDraw,
		AnimationUpdateEarly,
		OcclusionSetup,
		OcclusionBuildMap,
		OcclusionRenderOccluders,
		OcclusionReproject,
		OcclusionCreateHiZ,
		OcclusionPrepareOccludees,
		OcclusionTestOccludees,
		ModulesUpdate,
		MapModuleUpdate,
		AmbienceUpdate,
		WeatherUpdate,
		EntityStoreModuleUpdate,
		UpdateSceneData,
		ChunksShadowGather,
		AnimatedChunksGather,
		EntitiesGather,
		FXUpdate,
		FXGather,
		FXUpdateSimulation,
		AnimationUpdate,
		EntitiesPrepareForDraw,
		ShadowCascadesPrepare,
		LightsUpdate,
		LightsUpdateClear,
		LightsUpdateClustering,
		LightsUpdateRefine,
		LightsUpdateFillGridData,
		LightsUpdateSendDataToGPU,
		FXPrepareForDraw,
		FXSendDataToGPU,
		Render,
		OcclusionFetchResults,
		AnalyzeSunOcclusion,
		ReflectionBuildMips,
		ShadowMap,
		FirstPersonView,
		MapNear,
		MapAnimated,
		MapFar,
		Entities,
		LinearZ,
		LinearZDownsample,
		ZDownsample,
		EdgeDetection,
		DeferredShadow,
		SSAO,
		BlurSSAOAndShadow,
		VolumetricSunshafts,
		Lights,
		LightsStencil,
		LightsFullRes,
		LightsLowRes,
		LightsMix,
		ApplyDeferred,
		ParticlesOpaque,
		Weather,
		MapAlphaBlended,
		Transparency,
		OITPrepass,
		OITAccumulateQuarterRes,
		OITAccumulateHalfRes,
		OITAccumulateFullRes,
		OITComposite,
		Texts,
		Distortion,
		PostFX,
		DepthOfField,
		Bloom,
		CombineAndFXAA,
		TAA,
		Blur,
		ScreenFX,
		MAX
	}

	private enum TransparencyPassTextureUnit
	{
		MapAtlas,
		SceneDepthLowRes,
		Normals,
		Refraction,
		SceneColor,
		Caustics,
		CloudShadow,
		FXAtlasPointSampling,
		FXAtlasLinearSampling,
		ForceField,
		UVMotion,
		FXData,
		SceneDepth,
		FogNoise,
		ShadowMap,
		LightGrid,
		LightIndicesOrDataBuffer,
		OITTotalOpticalDepth,
		OITMoments
	}

	public delegate void Command(string[] args);

	private class DebugModule : Module
	{
		public DebugModule(GameInstance gameInstance)
			: base(gameInstance)
		{
		}

		[Obsolete]
		public override void OnNewFrame(float deltaTime)
		{
			Logger.Info("DebugModule: OnNewFrame({0})", deltaTime);
		}

		[Obsolete]
		public override void Tick()
		{
			Logger.Info("DebugModule: Tick()");
		}

		protected override void DoDispose()
		{
			Logger.Info("DebugModule: Dispose()");
		}
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public float TimeDilationModifier = 1f;

	public int ServerUpdatesPerSecond = 30;

	private GameInstanceStage _stage = GameInstanceStage.WaitingForJoinWorldPacket;

	private bool _isAwaitingJoiningWorldFade = false;

	public readonly App App;

	public readonly Engine Engine;

	public readonly Input Input;

	private const int ConsumableDownDuration = 200;

	private readonly Stopwatch _consumableDownTime = new Stopwatch();

	public readonly Chat Chat;

	public readonly Notifications Notifications;

	[Obsolete]
	public readonly HitDetection HitDetection;

	public bool RenderPlayers = true;

	public readonly ConnectionToServer Connection;

	private readonly PacketHandler _packetHandler;

	private readonly Stopwatch _stopwatchSinceJoiningServer = new Stopwatch();

	private float _tickAccumulator;

	public SceneRenderer SceneRenderer;

	private SceneView _cameraSceneView;

	private SceneView _sunSceneView;

	public WireframePass Wireframe;

	private Vector3 _foliageInteractionParams;

	private LightData[] GlobalLightData = new LightData[1024];

	private int GlobalLightDataCount;

	public bool CullUndergroundChunkShadowCasters = true;

	public bool CullUndergroundEntityShadowCasters = true;

	public bool CullSmallEntityShadowCasters = true;

	private bool _isCameraUnderwater;

	public bool UseAnimationLOD = true;

	public readonly float ResolutionScaleMin = 0.25f;

	public readonly float ResolutionScaleMax = 4f;

	public bool DrawOcclusionMap;

	public bool DebugDrawOccludeeChunks;

	public bool DebugDrawOccludeeEntities;

	public bool DebugDrawOccludeeLights;

	public bool DebugDrawOccludeeParticles;

	private int _requestedOpaqueChunkOccludersCount = 15;

	private bool _useChunkOccluderPlanes = true;

	private bool _useOpaqueChunkOccluders = true;

	private bool _useAlphaTestedChunkOccluders = true;

	private bool _useOcclusionCullingReprojection = true;

	private bool _useOcclusionCullingReprojectionHoleFilling = true;

	private Vector4[] _previousFrameInvalidScreenAreas = new Vector4[10];

	public bool RenderTimePaused;

	private bool _testBranch;

	public bool DebugEntitiesZTest;

	public bool DebugCollisionOnlyCollided;

	private bool _debugParticleOverdraw;

	private bool _debugParticleTexture;

	private bool _debugParticleBoundingVolume;

	private bool _debugParticleZTestEnabled = true;

	private bool _debugParticleUVMotion;

	public bool DebugDrawLight;

	private bool _debugLightClusters;

	private bool _chunkUseFoliageFading = true;

	private bool _debugChunkBoundaries;

	public bool DebugMap;

	private const int DebugDrawMapLevelMax = 8;

	private const int DebugDrawMapOpacityStepMax = 5;

	private int _debugDrawMapLevel;

	private int _debugDrawMapOpacityStep = 5;

	private int _debugTextureArrayActiveLayer;

	private int _debugTextureArrayLayerCount;

	private bool _debugMapVerticalDisplay;

	private string[] _activeDebugMapsNames;

	public bool UseLessSkyAmbientAtNoon = true;

	public float SkyAmbientIntensityAtNoon = 0f;

	public float SkyAmbientIntensity = 0.12f;

	private Texture _fogNoise;

	private bool _useMoodFog;

	private bool _useCustomMoodFog;

	private float _customDensity = 1f;

	private float _customHeightFalloff = 8f;

	private float _densityVariationScale;

	private float _fogSpeedFactor;

	public bool UseSkyboxTest;

	private int _waterQuality;

	private Texture _waterNormals;

	private Texture _waterCaustics;

	private float _underwaterCausticsIntensity = 1f;

	private float _underwaterCausticsScale = 0.095f;

	private float _underwaterCausticsDistortion = 0.05f;

	private float _cloudsUVMotionScale = 30f;

	private float _cloudsUVMotionStrength = 0.0005f;

	private float _cloudsShadowsIntensity = 0.25f;

	private float _cloudsShadowsScale = 0.005f;

	private float _cloudsShadowsBlurriness = 3.5f;

	private float _cloudsShadowsSpeed = 1f;

	private GLTexture _projectionTexture;

	private Texture _flowMap;

	private Texture _glowMask;

	public bool UseBloomUnderwater = true;

	public float UnderwaterBloomIntensity = 0.25f;

	public float UnderwaterBloomPower = 8f;

	public float DefaultBloomIntensity = 0.04f;

	public float DefaultBloomPower = 5f;

	private bool _useVolumetricSunshaft;

	public int ForceFieldTest;

	public bool ForceFieldOptionAnimation = true;

	public bool ForceFieldOptionOutline = true;

	public bool ForceFieldOptionDistortion = true;

	public bool ForceFieldOptionColor = true;

	public int ForceFieldCount = 1;

	private const int MaxFieldMatricesCount = 20;

	private Matrix[] _forceFieldModelMatrices = new Matrix[20];

	private Matrix[] _forceFieldNormalMatrices = new Matrix[20];

	private Texture _forceFieldNormalMap;

	private bool _useChunksOIT;

	private int _oitRes;

	private const uint RenderPassesCount = 13u;

	private readonly bool[] _renderPassStates = new bool[13];

	private Texture _cubemap;

	private Mesh _cube;

	private Vector2 _atlasSizeFactor0;

	private Vector2 _atlasSizeFactor1;

	private Vector2 _atlasSizeFactor2;

	public bool UseLocalPlayerOccluder = true;

	public bool UseOcclusionCullingForEntities = true;

	public bool UseOcclusionCullingForEntitiesAnimations = true;

	public bool UseOcclusionCullingForLights = true;

	public bool UseOcclusionCullingForParticles = true;

	public const string LocalCommandPrefix = ".";

	public const string ServerCommandPrefix = "/";

	public const string MacroCommandPrefix = "..";

	private readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>();

	private readonly List<Module> _modules = new List<Module>();

	private NetworkModule _networkModule;

	private MovementSoundModule _movementSoundModule;

	private AutoCameraModule _autoCameraModule;

	private DebugCommandsModule _debugCommandsModule;

	public RenderingOptions TrailerMode;

	public RenderingOptions CutscenesMode;

	public RenderingOptions IngameMode;

	public RenderingOptions LowEndGPUMode;

	public readonly ConcurrentDictionary<string, string> HashesByServerAssetPath = new ConcurrentDictionary<string, string>();

	public int LocalPlayerNetworkId { get; private set; }

	public PlayerEntity LocalPlayer { get; private set; }

	public GameMode GameMode { get; private set; } = (GameMode)0;


	public float ActiveFieldOfView { get; private set; }

	public bool IsPlaying => !base.Disposed && _stage == GameInstanceStage.WorldJoined && LocalPlayer != null;

	public ServerSettings ServerSettings { get; private set; }

	public bool IsReadyToDraw { get; private set; }

	public bool IsOnPacketHandlerThread => _packetHandler.IsOnThread;

	public PostEffectRenderer PostEffectRenderer { get; private set; }

	public uint FrameCounter { get; private set; }

	public float FrameTime { get; private set; }

	public float DeltaTime { get; private set; }

	public float ResolutionScale { get; private set; } = 1f;


	public bool TestBranch
	{
		get
		{
			return _testBranch;
		}
		set
		{
			_testBranch = value;
		}
	}

	public string[] RenderPassNames { get; private set; } = new string[13];


	public Point[] AtlasSizes { get; private set; }

	public TimeModule TimeModule { get; private set; }

	public AudioModule AudioModule { get; private set; }

	public MapModule MapModule { get; private set; }

	public ItemLibraryModule ItemLibraryModule { get; private set; }

	public CharacterControllerModule CharacterControllerModule { get; private set; }

	public CameraModule CameraModule { get; private set; }

	public CollisionModule CollisionModule { get; private set; }

	public EntityStoreModule EntityStoreModule { get; private set; }

	public InventoryModule InventoryModule { get; private set; }

	public InteractionModule InteractionModule { get; private set; }

	public BuilderToolsModule BuilderToolsModule { get; private set; }

	public MachinimaModule MachinimaModule { get; private set; }

	public FXModule FXModule { get; private set; }

	public TrailStoreModule TrailStoreModule { get; private set; }

	public ParticleSystemStoreModule ParticleSystemStoreModule { get; private set; }

	public ScreenEffectStoreModule ScreenEffectStoreModule { get; private set; }

	public WeatherModule WeatherModule { get; private set; }

	public AmbienceFXModule AmbienceFXModule { get; private set; }

	public DamageEffectModule DamageEffectModule { get; private set; }

	public ClientFeatureModule ClientFeatureModule { get; private set; }

	public ProfilingModule ProfilingModule { get; private set; }

	public ShortcutsModule ShortcutsModule { get; private set; }

	public ImmersiveScreenModule ImmersiveScreenModule { get; private set; }

	public InterfaceRenderPreviewModule InterfaceRenderPreviewModule { get; private set; }

	public EditorWebViewModule EditorWebViewModule { get; private set; }

	public WorldMapModule WorldMapModule { get; private set; }

	public DebugDisplayModule DebugDisplayModule { get; private set; }

	public GameInstance(App app, ConnectionToServer establishedConnection)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Expected O, but got Unknown
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Invalid comparison between Unknown and I4
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Expected O, but got Unknown
		//IL_03a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Expected O, but got Unknown
		App = app;
		Engine = app.Engine;
		Input = new Input(Engine, app.Settings.InputBindings);
		Chat = new Chat(this);
		Notifications = new Notifications(this);
		HitDetection = new HitDetection(this);
		InitModules();
		InitDraw();
		Connection = establishedConnection;
		_packetHandler = new PacketHandler(this);
		Connection.OnPacketReceived = _packetHandler.Receive;
		ConnectionMode val = (ConnectionMode)((Connection.Referral != null) ? 4 : 2);
		if (App.AuthManager.Settings.IsInsecure)
		{
			val = (ConnectionMode)3;
		}
		Connection.SendPacketImmediate((ProtoPacket)new Connect("f4c63561b2d2f5120b4c81ad1b8544e396088277d88f650aea892b6f0cb113f", 1643968234458L, val, App.Settings.Language ?? Language.SystemLanguage));
		if ((int)val == 3)
		{
			Connection.SendPacket((ProtoPacket)new SetUsername(App.Username));
			return;
		}
		if (App.AuthManager.CertPathBytes == null)
		{
			throw new Exception("Attempted to execute an online-mode handshake while not authenticated!");
		}
		Auth1 packet = new Auth1((sbyte[])(object)App.AuthManager.CertPathBytes);
		Connection.SendPacket((ProtoPacket)(object)packet);
	}

	protected override void DoDispose()
	{
		DisposeDraw();
		_packetHandler.Dispose();
		Connection.OnPacketReceived = null;
		AssetManager.UnloadServerRequiredAssets();
		foreach (Module module in _modules)
		{
			module.Dispose();
		}
		AudioModule.Dispose();
		Engine.Audio.ResourceManager.FilePathsByFileName.Clear();
		Engine.Audio.RefreshBanks();
	}

	public void InjectPacket(ProtoPacket packet)
	{
		_packetHandler.Receive(packet);
	}

	public int AddPendingCallback<T>(Disposable disposable, Action<FailureReply, T> callback) where T : ProtoPacket
	{
		return _packetHandler.AddPendingCallback(disposable, callback);
	}

	public void SetServerSettings(ServerSettings newSettings)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		ServerSettings = newSettings;
	}

	public void OnSetupComplete()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		Logger.Info("GameInstance.OnSetupComplete()");
		App.MainMenu.StopMusic();
		AudioModule.Initialize();
		foreach (Module module in _modules)
		{
			module.Initialize();
		}
		App.ResetElapsedTime();
		_stage = GameInstanceStage.WaitingForJoinWorldPacket;
		App.Interface.FadeOut();
	}

	public void PrepareJoiningWorld(JoinWorld packet)
	{
		Logger.Info("GameInstance.PrepareJoiningWorld()");
		if (_isAwaitingJoiningWorldFade)
		{
			Logger.Info("JoingingWorld OnFadeComplete canceled.");
			App.Interface.CancelOnFadeComplete();
			_isAwaitingJoiningWorldFade = false;
		}
		if (packet.FadeInOut)
		{
			_isAwaitingJoiningWorldFade = true;
			App.Interface.FadeOut(delegate
			{
				StartJoiningWorld(packet);
				_isAwaitingJoiningWorldFade = false;
			});
		}
		else
		{
			StartJoiningWorld(packet);
		}
	}

	private void StartJoiningWorld(JoinWorld packet)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		Debug.Assert(ThreadHelper.IsMainThread());
		Logger.Info("GameInstance.StartJoiningWorld()");
		if (packet.ClearWorld)
		{
			MapModule.ClearAllColumns();
			EntityStoreModule.DespawnAll();
			SetLocalPlayer(null);
		}
		Connection.SendPacket((ProtoPacket)new ClientReady(true, false));
		_stage = GameInstanceStage.WaitingForNearbyChunks;
		_stopwatchSinceJoiningServer.Restart();
		Input.ResetKeys();
		Input.ResetMouseButtons();
	}

	public void SetGameMode(GameMode gameMode, bool executeCommand = false)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Invalid comparison between Unknown and I4
		Debug.Assert(ThreadHelper.IsMainThread());
		GameMode = gameMode;
		App.Interface.InGameView.OnGameModeChanged();
		if ((int)GameMode == 0)
		{
			CharacterControllerModule.MovementController.MovementStates.IsFlying = false;
			BuilderToolsModule.PlaySelection.CancelAllActions();
		}
		if (executeCommand)
		{
			if ((int)gameMode == 1)
			{
				Chat.SendCommand("gm c");
			}
			else
			{
				Chat.SendCommand("gm a");
			}
		}
	}

	public void SetLocalPlayerId(int localPlayerId)
	{
		LocalPlayerNetworkId = localPlayerId;
	}

	public void SetLocalPlayer(PlayerEntity entity)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		LocalPlayer = entity;
	}

	private void OnWorldJoined()
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		Debug.Assert(_stage == GameInstanceStage.WaitingForNearbyChunks, $"Called OnWorldJoined but stage was {_stage}");
		Logger.Info("GameInstance.OnWorldJoined()");
		_stage = GameInstanceStage.WorldJoined;
		if (App.Stage == App.AppStage.GameLoading)
		{
			App.GameLoading.AssertStage(AppGameLoading.GameLoadingStage.Loading);
			App.GameLoading.SetStage(AppGameLoading.GameLoadingStage.Complete);
			App.InGame.Open();
		}
		Connection.SendPacket((ProtoPacket)new ClientReady(true, true));
		App.Interface.FadeIn(delegate
		{
			CharacterControllerModule.MovementController.MovementEnabled = true;
		}, longFade: true);
		Chat.HandleBeforePlayingMessages();
		if (CameraModule.Controller is ServerCameraController serverCameraController)
		{
			serverCameraController.OnWorldJoined();
		}
		if (App.SingleplayerServer != null)
		{
			AffinityHelper.SetupSingleplayerAffinity(App.SingleplayerServer.Process);
		}
		Connection.SendPacket((ProtoPacket)(object)App.Settings.DebugSettings.CreatePacket());
	}

	public void DisconnectWithReason(string reason, Exception exception)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		Logger.Info<App.AppStage, string>("Disconnecting with error during stage {0}: {1}", App.Stage, reason);
		Logger.Info<Exception>(exception);
		if (App.Settings.DiagnosticMode)
		{
			reason = exception.Message;
		}
		Connection.SendPacketImmediate((ProtoPacket)new Disconnect(reason, (DisconnectType)1));
		Connection.Close();
		App.Disconnection.SetReason(reason);
		App.Disconnection.Open(exception.Message, Connection.Hostname, Connection.Port);
	}

	private void ManageReticleEvents()
	{
		InputBindings inputBindings = App.Settings.InputBindings;
		if (Input.IsBindingHeld(inputBindings.StrafeLeft) && !Input.IsBindingHeld(inputBindings.StrafeRight))
		{
			App.Interface.InGameView.ReticleComponent.OnClientEvent((ItemReticleClientEvent)2);
			return;
		}
		if (Input.IsBindingHeld(inputBindings.StrafeRight) && !Input.IsBindingHeld(inputBindings.StrafeLeft))
		{
			App.Interface.InGameView.ReticleComponent.OnClientEvent((ItemReticleClientEvent)3);
			return;
		}
		App.Interface.InGameView.ReticleComponent.RemoveClientReticle((ItemReticleClientEvent)2);
		App.Interface.InGameView.ReticleComponent.RemoveClientReticle((ItemReticleClientEvent)3);
	}

	public void OnUserInput(SDL_Event evt)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Invalid comparison between Unknown and I4
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Expected I4, but got Unknown
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Expected I4, but got Unknown
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_064c: Unknown result type (might be due to invalid IL or missing references)
		//IL_064d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0657: Invalid comparison between Unknown and I4
		//IL_0659: Unknown result type (might be due to invalid IL or missing references)
		//IL_065a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f05: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f06: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f10: Invalid comparison between Unknown and I4
		//IL_0674: Unknown result type (might be due to invalid IL or missing references)
		//IL_0675: Unknown result type (might be due to invalid IL or missing references)
		//IL_067a: Unknown result type (might be due to invalid IL or missing references)
		//IL_067f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0684: Unknown result type (might be due to invalid IL or missing references)
		//IL_0686: Unknown result type (might be due to invalid IL or missing references)
		//IL_0697: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f39: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f3f: Invalid comparison between Unknown and I4
		//IL_06dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f4e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f4f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f54: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f59: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f5e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f60: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f71: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f9f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fb0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fde: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fef: Unknown result type (might be due to invalid IL or missing references)
		//IL_072f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0740: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eb3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ec4: Unknown result type (might be due to invalid IL or missing references)
		//IL_1099: Unknown result type (might be due to invalid IL or missing references)
		//IL_10aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0816: Unknown result type (might be due to invalid IL or missing references)
		//IL_081c: Invalid comparison between Unknown and I4
		//IL_0868: Unknown result type (might be due to invalid IL or missing references)
		//IL_0879: Unknown result type (might be due to invalid IL or missing references)
		//IL_089a: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a0: Invalid comparison between Unknown and I4
		//IL_08c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_08fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0900: Invalid comparison between Unknown and I4
		//IL_0928: Unknown result type (might be due to invalid IL or missing references)
		//IL_0939: Unknown result type (might be due to invalid IL or missing references)
		//IL_095a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0960: Invalid comparison between Unknown and I4
		//IL_0988: Unknown result type (might be due to invalid IL or missing references)
		//IL_0999: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c0: Invalid comparison between Unknown and I4
		//IL_09e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a1a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a20: Invalid comparison between Unknown and I4
		//IL_0a48: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a59: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b12: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b23: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a7d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a83: Invalid comparison between Unknown and I4
		//IL_0bb3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bb9: Invalid comparison between Unknown and I4
		//IL_0b44: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b4a: Invalid comparison between Unknown and I4
		//IL_0bc5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bd6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c71: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c82: Unknown result type (might be due to invalid IL or missing references)
		//IL_0caa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cbb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cfa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d3b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d4c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d75: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d7b: Invalid comparison between Unknown and I4
		//IL_0e51: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e62: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d95: Unknown result type (might be due to invalid IL or missing references)
		//IL_0da6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0de2: Unknown result type (might be due to invalid IL or missing references)
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(GameInstance).FullName);
		}
		InputBindings inputBindings = App.Settings.InputBindings;
		if (WorldMapModule.MapNeedsDrawing)
		{
			WorldMapModule.OnUserInput(evt);
		}
		if (!App.Engine.Window.IsMouseLocked)
		{
			InterfaceRenderPreviewModule.OnUserInput(evt);
		}
		if ((int)evt.type == 1024)
		{
			if (CameraModule.Controller.DisplayCursor)
			{
				CameraModule.Controller.OnMouseInput(evt);
			}
			else if (Engine.Window.IsMouseLocked && !Input.MouseInputDisabled)
			{
				CameraModule.OffsetLook(-evt.motion.yrel, -evt.motion.xrel);
			}
			return;
		}
		SDL_EventType type = evt.type;
		SDL_EventType val = type;
		switch (val - 768)
		{
		default:
			switch (val - 1025)
			{
			case 2:
				break;
			case 0:
				goto IL_02aa;
			case 1:
				goto IL_02e1;
			default:
				goto IL_031a;
			}
			if (InteractionModule.ShouldForwardMouseWheelEvents)
			{
				InteractionModule.OnMouseWheelEvent(evt);
				break;
			}
			if (CameraModule.Controller.DisplayCursor)
			{
				CameraModule.Controller.OnMouseInput(evt);
			}
			if (Engine.Window.IsMouseLocked && evt.wheel.y != 0)
			{
				InventoryModule.ScrollHotbarSlot(evt.wheel.y < 0);
			}
			break;
		case 0:
			Input.OnUserInput(evt);
			ManageReticleEvents();
			if (evt.key.repeat == 0 && !Engine.Window.IsMouseLocked && Input.EventMatchesBinding(evt, inputBindings.DropItem))
			{
				App.Interface.TriggerEvent("inventory.dropItemBindingDown");
			}
			break;
		case 1:
			Input.OnUserInput(evt);
			ManageReticleEvents();
			if (!Engine.Window.IsMouseLocked && Input.EventMatchesBinding(evt, inputBindings.DropItem))
			{
				App.Interface.TriggerEvent("inventory.dropItemBindingUp");
			}
			break;
		case 2:
			goto IL_031a;
		case 3:
			break;
			IL_02aa:
			if (CameraModule.Controller.DisplayCursor)
			{
				CameraModule.Controller.OnMouseInput(evt);
			}
			Input.OnUserInput(evt);
			break;
			IL_031a:
			throw new ArgumentOutOfRangeException("evt", ((object)(SDL_EventType)(ref evt.type)).ToString());
			IL_02e1:
			if (CameraModule.Controller.DisplayCursor)
			{
				CameraModule.Controller.OnMouseInput(evt);
			}
			Input.OnUserInput(evt);
			break;
		}
		if (DrawOcclusionMap || DebugDrawLight || DebugMap)
		{
			if (Input.ConsumeKey((SDL_Scancode)82) && _debugDrawMapOpacityStep < 5)
			{
				_debugDrawMapOpacityStep++;
				Chat.Log("Debug Map Transparency : " + _debugDrawMapOpacityStep + " / " + 5);
			}
			if (Input.ConsumeKey((SDL_Scancode)81) && _debugDrawMapOpacityStep > 0)
			{
				_debugDrawMapOpacityStep--;
				Chat.Log("Debug Map Transparency : " + _debugDrawMapOpacityStep + " / " + 5);
			}
			if (Input.ConsumeKey((SDL_Scancode)79) && _debugTextureArrayActiveLayer < _debugTextureArrayLayerCount - 1)
			{
				_debugTextureArrayActiveLayer++;
				int num = _debugTextureArrayActiveLayer + 1;
				Chat.Log("Debug Texture Array Layer : " + num + " / " + _debugTextureArrayLayerCount);
			}
			if (Input.ConsumeKey((SDL_Scancode)80) && _debugTextureArrayActiveLayer > 0)
			{
				_debugTextureArrayActiveLayer--;
				int num2 = _debugTextureArrayActiveLayer + 1;
				Chat.Log("Debug Texture Array Layer : " + num2 + " / " + _debugTextureArrayLayerCount);
			}
			if (Input.ConsumeKey((SDL_Scancode)87) && _debugDrawMapLevel < 8)
			{
				_debugDrawMapLevel++;
				Chat.Log("Draw Map Level : " + _debugDrawMapLevel + " / " + 8);
			}
			if (Input.ConsumeKey((SDL_Scancode)86) && _debugDrawMapLevel > 0)
			{
				_debugDrawMapLevel--;
				Chat.Log("Draw Map Level : " + _debugDrawMapLevel + " / " + 8);
			}
		}
		if (Input.ConsumeKey((SDL_Scancode)67))
		{
			Wireframe = ((Wireframe != WireframePass.OnAll) ? WireframePass.OnAll : WireframePass.Off);
			Chat.Log("Wireframe: " + Wireframe);
		}
		if (Input.ConsumeKey((SDL_Scancode)63))
		{
			ReloadShaderTextures();
			Chat.Log("Shader Textures have been reloaded.");
		}
		if ((int)evt.type == 768 && evt.key.repeat == 0)
		{
			SDL_Keycode sym = evt.key.keysym.sym;
			if ((SDL_Keycode?)sym == inputBindings.Chat.Keycode && !Chat.IsOpen)
			{
				Chat.TryOpen(inputBindings.Chat.Keycode);
			}
			else if ((SDL_Keycode?)sym == inputBindings.Command.Keycode && !Chat.IsOpen)
			{
				Chat.TryOpen(inputBindings.Command.Keycode, isCommand: true);
			}
			else if ((SDL_Keycode?)sym == inputBindings.SwitchHudVisibility.Keycode && !App.Interface.Desktop.IsShortcutKeyDown)
			{
				App.InGame.SwitchHudVisibility();
			}
			if (App.InGame.CurrentOverlay == AppInGame.InGameOverlay.None)
			{
				if (App.InGame.HasUnclosablePage || Chat.IsOpen || App.Interface.Desktop.FocusedElement != null)
				{
					return;
				}
				if ((SDL_Keycode?)sym == inputBindings.OpenToolsSettings.Keycode && (int)App.InGame.Instance.GameMode == 1)
				{
					if (App.InGame.IsToolsSettingsModalOpened)
					{
						App.InGame.CloseToolsSettingsModal();
					}
					else
					{
						App.InGame.OpenToolsSettingsPage();
					}
				}
				else if ((SDL_Keycode?)sym == inputBindings.ToolPaintBrush.Keycode && (int)App.InGame.Instance.GameMode == 1)
				{
					App.InGame.ToogleToolById("EditorTool_PlayPaint");
				}
				else if ((SDL_Keycode?)sym == inputBindings.ToolSculptBrush.Keycode && (int)App.InGame.Instance.GameMode == 1)
				{
					App.InGame.ToogleToolById("EditorTool_PlaySculpt");
				}
				else if ((SDL_Keycode?)sym == inputBindings.ToolSelectionTool.Keycode && (int)App.InGame.Instance.GameMode == 1)
				{
					App.InGame.ToogleToolById("EditorTool_PlaySelection");
				}
				else if ((SDL_Keycode?)sym == inputBindings.ToolLine.Keycode && (int)App.InGame.Instance.GameMode == 1)
				{
					App.InGame.ToogleToolById("EditorTool_Line");
				}
				else if ((SDL_Keycode?)sym == inputBindings.ToolPaste.Keycode && (int)App.InGame.Instance.GameMode == 1)
				{
					App.InGame.ToogleToolById("EditorTool_Paste");
				}
				else if ((SDL_Keycode?)sym == inputBindings.OpenInventory.Keycode)
				{
					if ((int)App.InGame.CurrentPage == 2)
					{
						App.Interface.InGameView.TryClosePageOrOverlayWithInputBinding();
						return;
					}
					App.InGame.SetCurrentPage((Page)2, wasOpenedWithInteractionBinding: false, playSound: true);
					if (App.InGame.Instance.InventoryModule.UsingToolsItem() && !App.InGame.IsToolsSettingsModalOpened)
					{
						App.InGame.Instance.BuilderToolsModule.ClearConfiguringTool();
					}
				}
				else if ((SDL_Keycode?)sym == inputBindings.OpenMap.Keycode)
				{
					if ((int)App.InGame.CurrentPage == 6)
					{
						App.Interface.InGameView.TryClosePageOrOverlayWithInputBinding();
					}
					else if (WorldMapModule.IsWorldMapEnabled)
					{
						App.InGame.SetCurrentPage((Page)6, wasOpenedWithInteractionBinding: false, playSound: true);
					}
					else
					{
						Chat.Log("ui.map.disabled");
					}
				}
				else
				{
					if ((int)App.InGame.CurrentPage != 0)
					{
						return;
					}
					if ((SDL_Keycode?)sym == inputBindings.OpenAssetEditor.Keycode)
					{
						if (Input.IsShiftHeld() && InteractionModule.HasFoundTargetBlock)
						{
							App.InGame.OpenAssetIdInAssetEditor("Item", ClientBlockType.GetOriginalBlockName(MapModule.ClientBlockTypes[InteractionModule.TargetBlockHit.BlockId].Name));
						}
						else
						{
							App.InGame.OpenAssetEditor();
						}
					}
					else if ((SDL_Keycode?)sym == inputBindings.OpenMachinimaEditor.Keycode)
					{
						MachinimaModule.ShowInterface();
					}
					else if ((SDL_Keycode?)sym == inputBindings.ShowPlayerList.Keycode)
					{
						App.InGame.SetPlayerListVisible(visible: true);
					}
					else if ((SDL_Keycode?)sym == inputBindings.ShowUtilitySlotSelector.Keycode && !BuilderToolsModule.HasActiveBrush)
					{
						App.InGame.SetActiveItemSelector(AppInGame.ItemSelector.Utility);
					}
					else if ((SDL_Keycode?)sym == inputBindings.ShowConsumableSlotSelector.Keycode)
					{
						_consumableDownTime.Restart();
					}
					else if ((int)GameMode == 1 && BuilderToolsModule.HasActiveBrush)
					{
						if ((SDL_Keycode?)sym == inputBindings.TertiaryItemAction.Keycode)
						{
							App.InGame.SetActiveItemSelector(AppInGame.ItemSelector.BuilderToolsMaterial);
						}
						else if ((SDL_Keycode?)sym == inputBindings.ToggleBuilderToolsLegend.Keycode)
						{
							App.Settings.BuilderToolsSettings.DisplayLegend = !App.Settings.BuilderToolsSettings.DisplayLegend;
							App.Settings.Save();
							App.Interface.InGameView.UpdateBuilderToolsLegendVisibility(doLayout: true);
						}
					}
					else if ((SDL_Keycode?)sym == inputBindings.TertiaryItemAction.Keycode)
					{
						App.Interface.InGameView.AbilitiesHudComponent?.OnTertiaryAction();
					}
				}
			}
			else if (App.InGame.CurrentOverlay == AppInGame.InGameOverlay.MachinimaEditor && (SDL_Keycode?)sym == inputBindings.OpenMachinimaEditor.Keycode)
			{
				EditorWebViewModule.WebView.TriggerEvent("requestCloseInGameOverlayWithInputBinding", EditorWebViewModule.WebViewType.MachinimaEditor);
			}
		}
		else
		{
			if ((int)evt.type != 769 || App.InGame.CurrentOverlay != 0 || (int)App.InGame.CurrentPage != 0)
			{
				return;
			}
			SDL_Keycode sym2 = evt.key.keysym.sym;
			if ((SDL_Keycode?)sym2 == inputBindings.ShowPlayerList.Keycode)
			{
				App.InGame.SetPlayerListVisible(visible: false);
			}
			else if ((SDL_Keycode?)sym2 == inputBindings.ShowUtilitySlotSelector.Keycode)
			{
				App.InGame.SetActiveItemSelector(AppInGame.ItemSelector.None);
			}
			else if ((SDL_Keycode?)sym2 == inputBindings.ShowConsumableSlotSelector.Keycode)
			{
				if (App.InGame.ActiveItemSelector == AppInGame.ItemSelector.Consumable)
				{
					App.InGame.SetActiveItemSelector(AppInGame.ItemSelector.None);
				}
				else if (_consumableDownTime.IsRunning && _consumableDownTime.ElapsedMilliseconds < 200)
				{
					InventoryModule inventoryModule = App.InGame.Instance.InventoryModule;
					inventoryModule.SetActiveConsumableSlot(inventoryModule.ConsumableActiveSlot, sendPacket: true, doInteraction: true);
				}
				_consumableDownTime.Reset();
			}
			else if ((SDL_Keycode?)sym2 == inputBindings.TertiaryItemAction.Keycode)
			{
				App.InGame.SetActiveItemSelector(AppInGame.ItemSelector.None);
			}
		}
	}

	private void TickAllModules()
	{
		TimeModule.Tick();
		CharacterControllerModule.Tick();
		_networkModule.Tick();
		MachinimaModule.Tick();
		ProfilingModule.Tick();
	}

	private void PreUpdateModules(float timeFraction)
	{
		CharacterControllerModule.PreUpdate(timeFraction);
		TimeModule.OnNewFrame(DeltaTime);
		CameraModule.Update(DeltaTime);
		InventoryModule.Update(DeltaTime);
		EntityStoreModule.PreUpdate(DeltaTime);
		Engine.Profiling.StartMeasure(8);
		EntityStoreModule.PrepareLights(CameraModule.Controller.Position);
		Engine.Profiling.StopMeasure(8);
		Engine.Profiling.StartMeasure(7);
		InteractionModule.Update(DeltaTime);
		Engine.Profiling.StopMeasure(7);
		ParticleSystemStoreModule.PreUpdate(CameraModule.Controller.Position);
	}

	private void UpdateModules()
	{
		Engine.Profiling.StartMeasure(20);
		MapModule.Update(DeltaTime);
		Engine.Profiling.StopMeasure(20);
		Engine.Profiling.StartMeasure(23);
		EntityStoreModule.Update(DeltaTime);
		Engine.Profiling.StopMeasure(23);
		AudioModule.Update(DeltaTime);
		MachinimaModule.OnNewFrame(DeltaTime);
		Engine.Profiling.StartMeasure(21);
		AmbienceFXModule.Update(DeltaTime);
		Engine.Profiling.StopMeasure(21);
		BuilderToolsModule.Update(DeltaTime);
		Engine.Profiling.StartMeasure(22);
		WeatherModule.Update(DeltaTime);
		Engine.Profiling.StopMeasure(22);
		ScreenEffectStoreModule.Update(DeltaTime);
		DamageEffectModule.Update(DeltaTime);
		ProfilingModule.OnNewFrame(DeltaTime);
		ImmersiveScreenModule.Update(DeltaTime);
		ShortcutsModule.Update();
		InterfaceRenderPreviewModule.Update(DeltaTime);
		WorldMapModule.Update(DeltaTime);
		CharacterControllerModule.Update(DeltaTime);
		_movementSoundModule.Update(DeltaTime);
	}

	private void BeginFrame()
	{
		Engine.Profiling.RegisterExternalMeasure(1, (float)Engine.TimeSpentInQueuedActions);
		Engine.AnimationSystem.BeginFrame();
		Engine.FXSystem.BeginFrame();
		Engine.Graphics.RTStore.BeginFrame();
		SceneRenderer.BeginFrame();
		MapModule.BeginFrame();
		EntityStoreModule.BeginFrame();
		_cameraSceneView.ResetCounters();
		_sunSceneView.ResetCounters();
	}

	public void OnNewFrame(float deltaTime, bool needsDrawing)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(GameInstance).FullName);
		}
		IsReadyToDraw = false;
		FrameCounter++;
		FrameTime = (float)_stopwatchSinceJoiningServer.ElapsedMilliseconds * 0.001f;
		DeltaTime = deltaTime;
		if (_consumableDownTime.ElapsedMilliseconds >= 200)
		{
			_consumableDownTime.Reset();
			if ((int)App.InGame.CurrentPage == 0 && App.InGame.CurrentOverlay == AppInGame.InGameOverlay.None && !Chat.IsOpen)
			{
				App.InGame.SetActiveItemSelector(AppInGame.ItemSelector.Consumable);
			}
		}
		BeginFrame();
		if (!IsPlaying)
		{
			if (_stage == GameInstanceStage.WaitingForNearbyChunks && LocalPlayer != null)
			{
				MapModule.PrepareChunks(FrameTime);
				if (App.Interface.FadeState == BaseInterface.InterfaceFadeState.FadedOut && (MapModule.AreNearbyChunksRendered || _stopwatchSinceJoiningServer.ElapsedMilliseconds > 3000))
				{
					OnWorldJoined();
				}
			}
			if (!IsPlaying)
			{
				return;
			}
		}
		Engine.Profiling.StartMeasure(3);
		MapModule.PrepareChunks(FrameTime);
		Engine.Profiling.StopMeasure(3);
		Engine.Profiling.StartMeasure(4);
		EntityStoreModule.PrepareEntities();
		Engine.Profiling.StopMeasure(4);
		Engine.Profiling.StartMeasure(5);
		_tickAccumulator += deltaTime;
		if (_tickAccumulator > 1f / 12f)
		{
			_tickAccumulator = 1f / 12f;
		}
		while (_tickAccumulator >= 1f / 60f)
		{
			TickAllModules();
			_tickAccumulator -= 1f / 60f;
		}
		float timeFraction = System.Math.Min(_tickAccumulator / (1f / 60f), 1f);
		Engine.Profiling.StopMeasure(5);
		Engine.Profiling.StartMeasure(6);
		PreUpdateModules(timeFraction);
		Engine.Profiling.StopMeasure(6);
		if (needsDrawing)
		{
			UpdateRenderData();
			Engine.Profiling.StartMeasure(9);
			MapModule.ProcessFrustumCulling(_cameraSceneView);
			MapModule.GatherRenderableChunks(_cameraSceneView);
			Engine.Profiling.StopMeasure(9);
			Engine.Profiling.StartMeasure(10);
			_cameraSceneView.SortChunksByDistance();
			MapModule.PrepareChunksForDraw(_cameraSceneView);
			Engine.Profiling.StopMeasure(10);
			Engine.Profiling.StartMeasure(11);
			LocalPlayer.RegisterAnimationTasks();
			Engine.AnimationSystem.ProcessAnimationTasks();
			Engine.Profiling.StopMeasure(11);
			Engine.Profiling.StartMeasure(12);
			UpdateOcclusionCulling();
			Engine.Profiling.StopMeasure(12);
		}
		Engine.Profiling.StartMeasure(19);
		UpdateModules();
		Engine.Profiling.StopMeasure(19);
		Connection.TriggerSend();
		if (needsDrawing)
		{
			Engine.Profiling.StartMeasure(24);
			UpdateSceneData();
			UpdateInterfaceData();
			Engine.Profiling.StopMeasure(24);
			IsReadyToDraw = true;
			Chat.NotifyPlayerOfSkippedDiagnosticMessages();
		}
	}

	public bool IsBuilderModeEnabled()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		return (int)GameMode == 1 && App.Settings.BuilderMode;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void BeginWireframeMode(WireframePass pass)
	{
		if (Wireframe == pass)
		{
			Engine.Graphics.GL.PolygonMode(GL.FRONT_AND_BACK, GL.LINE);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EndWireframeMode(WireframePass pass)
	{
		if (Wireframe == pass)
		{
			Engine.Graphics.GL.PolygonMode(GL.FRONT_AND_BACK, GL.FILL);
		}
	}

	private void InitLighting()
	{
		UseClusteredLightingCustomZDistribution(enable: true);
		UseClusteredLightingDirectAccess(enable: true);
		UseClusteredLightingRefinedVoxelization(enable: true);
		UseClusteredLightingMappedGPUBuffers(enable: true);
		UseClusteredLightingPBO(enable: true);
		SetLightBufferCompression(enable: true);
	}

	public void SetLightBufferCompression(bool enable)
	{
		SceneRenderer.SetLightBufferCompression(enable);
	}

	public void UseClusteredLighting(bool enable)
	{
		SceneRenderer.UseClusteredLighting = enable;
	}

	public void UseClusteredLightingRefinedVoxelization(bool enable)
	{
		SceneRenderer.ClusteredLighting.UseRefinedVoxelization(enable);
	}

	public void UseClusteredLightingMappedGPUBuffers(bool enable)
	{
		SceneRenderer.ClusteredLighting.UseMappedGPUBuffers(enable);
	}

	public void UseClusteredLightingPBO(bool enable)
	{
		SceneRenderer.ClusteredLighting.UsePBO(enable);
	}

	public void UseClusteredLightingDoubleBuffering(bool enable)
	{
		SceneRenderer.ClusteredLighting.UseDoubleBuffering(enable);
	}

	public void SetClusteredLightingGridResolution(uint width, uint height, uint depth)
	{
		SceneRenderer.ClusteredLighting.ChangeGridResolution(width, height, depth);
		bool debugTiles = Engine.Graphics.GPUProgramStore.PostEffectProgram.DebugTiles;
		ClusteredLighting clusteredLighting = SceneRenderer.ClusteredLighting;
		Vector2 resolution = ((!debugTiles) ? Vector2.Zero : new Vector2(clusteredLighting.GridWidth, clusteredLighting.GridHeight));
		PostEffectRenderer.UpdateDebugTileResolution(resolution);
	}

	public void UseClusteredLightingCustomZDistribution(bool enable)
	{
		SceneRenderer.ClusteredLighting.UseCustomZDistribution(enable);
		GPUProgramStore gPUProgramStore = Engine.Graphics.GPUProgramStore;
		if (gPUProgramStore.LightClusteredProgram.UseCustomZDistribution != enable)
		{
			gPUProgramStore.LightClusteredProgram.UseCustomZDistribution = enable;
			gPUProgramStore.MapChunkAlphaBlendedProgram.UseCustomZDistribution = enable;
			gPUProgramStore.MapBlockAnimatedProgram.UseCustomZDistribution = enable;
			gPUProgramStore.ParticleProgram.UseCustomZDistribution = enable;
			gPUProgramStore.ParticleErosionProgram.UseCustomZDistribution = enable;
			gPUProgramStore.LightClusteredProgram.Reset();
			gPUProgramStore.MapChunkAlphaBlendedProgram.Reset();
			gPUProgramStore.MapBlockAnimatedProgram.Reset();
			gPUProgramStore.ParticleProgram.Reset();
			gPUProgramStore.ParticleErosionProgram.Reset();
		}
	}

	public void UseClusteredLightingDirectAccess(bool enable)
	{
		SceneRenderer.ClusteredLighting.UseLightDirectAccess(enable);
		GPUProgramStore gPUProgramStore = Engine.Graphics.GPUProgramStore;
		if (gPUProgramStore.LightClusteredProgram.UseLightDirectAccess != enable)
		{
			gPUProgramStore.LightClusteredProgram.UseLightDirectAccess = enable;
			gPUProgramStore.MapChunkAlphaBlendedProgram.UseLightDirectAccess = enable;
			gPUProgramStore.MapBlockAnimatedProgram.UseLightDirectAccess = enable;
			gPUProgramStore.ParticleProgram.UseLightDirectAccess = enable;
			gPUProgramStore.ParticleErosionProgram.UseLightDirectAccess = enable;
			gPUProgramStore.LightClusteredProgram.Reset();
			gPUProgramStore.MapChunkAlphaBlendedProgram.Reset();
			gPUProgramStore.MapBlockAnimatedProgram.Reset();
			gPUProgramStore.ParticleProgram.Reset();
			gPUProgramStore.ParticleErosionProgram.Reset();
		}
	}

	public void SetUseShadowBackfaceLODDistance(bool enable, float distance = -1f)
	{
		ZOnlyChunkProgram mapChunkShadowMapProgram = Engine.Graphics.GPUProgramStore.MapChunkShadowMapProgram;
		mapChunkShadowMapProgram.UseDistantBackfaceCulling = enable;
		mapChunkShadowMapProgram.DistantBackfaceCullingDistance = ((distance > 0f) ? distance : mapChunkShadowMapProgram.DistantBackfaceCullingDistance);
		mapChunkShadowMapProgram.Reset();
	}

	public void UseAlphaBlendedChunksSunShadows(bool enable)
	{
		MapChunkAlphaBlendedProgram mapChunkAlphaBlendedProgram = Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram;
		if (mapChunkAlphaBlendedProgram.UseForwardSunShadows != enable)
		{
			mapChunkAlphaBlendedProgram.UseForwardSunShadows = enable;
			mapChunkAlphaBlendedProgram.Reset();
		}
	}

	public void UseParticleSunShadows(bool enable)
	{
		GPUProgramStore gPUProgramStore = Engine.Graphics.GPUProgramStore;
		if (gPUProgramStore.ParticleProgram.UseSunShadows != enable)
		{
			gPUProgramStore.ParticleProgram.UseSunShadows = enable;
			gPUProgramStore.ParticleProgram.Reset();
		}
		if (gPUProgramStore.ParticleErosionProgram.UseSunShadows != enable)
		{
			gPUProgramStore.ParticleErosionProgram.UseSunShadows = enable;
			gPUProgramStore.ParticleErosionProgram.Reset();
		}
	}

	private void InitShadowMapping()
	{
		SceneRenderer.SetSunShadowsEnabled(enable: true);
		SceneRenderer.SetSunShadowsWithChunks(enable: true);
		int sunShadowsCascadeCount = (Engine.Graphics.IsGPULowEnd ? 3 : 4);
		SceneRenderer.SetSunShadowsCascadeCount(sunShadowsCascadeCount);
		SceneRenderer.SetSunShadowMapResolution(1024u, 1024u);
		SceneRenderer.SetSunShadowsIntensity(0.7f);
		SceneRenderer.SetDeferredShadowsBlurEnabled(enable: true);
		SceneRenderer.SetDeferredShadowsNoiseEnabled(enable: true);
		SceneRenderer.SetDeferredShadowsFadingEnabled(enable: true);
		SceneRenderer.SetSunShadowMappingStableProjectionEnabled(enable: false);
		SceneRenderer.SetSunShadowsGlobalKDopEnabled(enable: true);
		SceneRenderer.SetSunShadowCastersSmartCascadeDispatchEnabled(enable: true);
		SceneRenderer.SetSunShadowCastersDrawInstancedEnabled(enable: false);
		SceneRenderer.SetSunShadowsSafeAngleEnabled(enable: true);
		SceneRenderer.SetSunShadowsDirectionSun(useCleanBackFaces: true);
		SceneRenderer.SetDeferredShadowsCameraBiasEnabled(enable: true);
		SceneRenderer.SetDeferredShadowsNormalBiasEnabled(enable: true);
		SceneRenderer.SetDeferredShadowsManualModeEnabled(enable: false);
		SceneRenderer.SetSunShadowMapCachingEnabled(enable: true);
		float deferredShadowResolutionScale = (Engine.Graphics.IsGPULowEnd ? 0.5f : 1f);
		SceneRenderer.SetDeferredShadowResolutionScale(deferredShadowResolutionScale);
	}

	public void SetUseLOD(bool enable)
	{
		MapModule.LODSetup.Enabled = enable;
		ParticleSystemStoreModule.DistanceCheck = enable;
		GPUProgramStore gPUProgramStore = Engine.Graphics.GPUProgramStore;
		gPUProgramStore.MapChunkNearAlphaTestedProgram.UseLOD = enable;
		gPUProgramStore.MapChunkFarAlphaTestedProgram.UseLOD = enable;
		gPUProgramStore.MapChunkNearAlphaTestedProgram.Reset();
		gPUProgramStore.MapChunkFarAlphaTestedProgram.Reset();
	}

	public void SetLODDistance(uint distance, uint range = 0u)
	{
		float num = MathHelper.Clamp(distance, 0f, 512f);
		GPUProgramStore gPUProgramStore = Engine.Graphics.GPUProgramStore;
		gPUProgramStore.MapChunkNearAlphaTestedProgram.LODDistance = num;
		gPUProgramStore.MapChunkFarAlphaTestedProgram.LODDistance = num;
		gPUProgramStore.MapChunkNearAlphaTestedProgram.Reset();
		gPUProgramStore.MapChunkFarAlphaTestedProgram.Reset();
		MapModule.LODSetup.StartDistance = num;
		MapModule.LODSetup.InvRange = ((range == 0) ? MapModule.LODSetup.InvRange : (1f / (float)range));
	}

	public void PrintLODState()
	{
		Chat.Log($"LOD state:\n- enabled ? {MapModule.LODSetup.Enabled}!\n- distance start {MapModule.LODSetup.StartDistance}\n- range {1.0 / (double)MapModule.LODSetup.InvRange}\n- distance treshold for entity rotation {EntityStoreModule.CurrentSetup.DistanceToCameraBeforeRotation}");
	}

	public bool SetResolutionScale(float scale)
	{
		if (scale < ResolutionScaleMin || scale > ResolutionScaleMax)
		{
			return false;
		}
		ResolutionScale = scale;
		Resize(Engine.Window.Viewport.Width, Engine.Window.Viewport.Height);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void BeginDebugEntitiesZTest()
	{
		if (DebugEntitiesZTest)
		{
			Engine.Graphics.GL.Disable(GL.DEPTH_TEST);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EndDebugEntitiesZTest()
	{
		if (DebugEntitiesZTest)
		{
			Engine.Graphics.GL.Enable(GL.DEPTH_TEST);
		}
	}

	public void ToggleDebugParticleOverdraw()
	{
		_debugParticleOverdraw = !_debugParticleOverdraw;
		Engine.Graphics.GPUProgramStore.ParticleProgram.UseDebugOverdraw = _debugParticleOverdraw;
		Engine.Graphics.GPUProgramStore.ParticleProgram.Reset();
		if (_debugParticleOverdraw)
		{
			string[] names = new string[1] { "overdraw" };
			SelectDebugMaps(names, verticalDisplay: false);
		}
		else
		{
			DebugMap = false;
		}
	}

	public void ToggleDebugParticleTexture()
	{
		_debugParticleTexture = !_debugParticleTexture;
		Engine.Graphics.GPUProgramStore.ParticleProgram.UseDebugTexture = _debugParticleTexture;
		Engine.Graphics.GPUProgramStore.ParticleProgram.Reset();
	}

	public void ToggleDebugParticleBoundingVolume()
	{
		_debugParticleBoundingVolume = !_debugParticleBoundingVolume;
	}

	public void ToggleDebugParticleZTest()
	{
		_debugParticleZTestEnabled = !_debugParticleZTestEnabled;
	}

	public void ToggleParticleSimulationPaused()
	{
		Engine.FXSystem.Particles.SetPaused(!Engine.FXSystem.Particles.IsPaused);
	}

	public void ToggleDebugParticleUVMotion()
	{
		_debugParticleUVMotion = !_debugParticleUVMotion;
		Engine.Graphics.GPUProgramStore.ParticleProgram.UseDebugUVMotion = _debugParticleUVMotion;
		Engine.Graphics.GPUProgramStore.ParticleErosionProgram.UseDebugUVMotion = _debugParticleUVMotion;
		Engine.Graphics.GPUProgramStore.ParticleProgram.Reset();
		Engine.Graphics.GPUProgramStore.ParticleErosionProgram.Reset();
	}

	public void SetParticleLowResRenderingEnabled(bool enable)
	{
		Engine.FXSystem.Particles.IsLowResRenderingEnabled = enable;
	}

	public void SetDebugLightClusters(bool enable)
	{
		if (_debugLightClusters != enable)
		{
			_debugLightClusters = enable;
			Engine.Graphics.GPUProgramStore.LightClusteredProgram.Debug = enable;
			Engine.Graphics.GPUProgramStore.PostEffectProgram.DebugTiles = enable;
			ClusteredLighting clusteredLighting = SceneRenderer.ClusteredLighting;
			Vector2 resolution = ((!enable) ? Vector2.Zero : new Vector2(clusteredLighting.GridWidth, clusteredLighting.GridHeight));
			PostEffectRenderer.UpdateDebugTileResolution(resolution);
			Engine.Graphics.GPUProgramStore.LightClusteredProgram.Reset();
			Engine.Graphics.GPUProgramStore.PostEffectProgram.Reset();
		}
	}

	public void SetChunkUseFoliageFading(bool enable)
	{
		if (_chunkUseFoliageFading != enable)
		{
			_chunkUseFoliageFading = enable;
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.UseFoliageFading = enable;
			Engine.Graphics.GPUProgramStore.MapChunkFarAlphaTestedProgram.UseFoliageFading = enable;
			Engine.Graphics.GPUProgramStore.MapChunkFarOpaqueProgram.UseFoliageFading = enable;
			Engine.Graphics.GPUProgramStore.MapChunkNearAlphaTestedProgram.UseFoliageFading = enable;
			Engine.Graphics.GPUProgramStore.MapChunkNearOpaqueProgram.UseFoliageFading = enable;
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapChunkFarAlphaTestedProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapChunkFarOpaqueProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapChunkNearAlphaTestedProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapChunkNearOpaqueProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapBlockAnimatedProgram.Reset();
		}
	}

	public void SetDebugChunkBoundaries(bool enable)
	{
		if (_debugChunkBoundaries != enable)
		{
			_debugChunkBoundaries = enable;
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.UseDebugBoundaries = enable;
			Engine.Graphics.GPUProgramStore.MapChunkFarAlphaTestedProgram.UseDebugBoundaries = enable;
			Engine.Graphics.GPUProgramStore.MapChunkFarOpaqueProgram.UseDebugBoundaries = enable;
			Engine.Graphics.GPUProgramStore.MapChunkNearAlphaTestedProgram.UseDebugBoundaries = enable;
			Engine.Graphics.GPUProgramStore.MapChunkNearOpaqueProgram.UseDebugBoundaries = enable;
			Engine.Graphics.GPUProgramStore.MapBlockAnimatedProgram.UseDebugBoundaries = enable;
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapChunkFarAlphaTestedProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapChunkFarOpaqueProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapChunkNearAlphaTestedProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapChunkNearOpaqueProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapBlockAnimatedProgram.Reset();
		}
	}

	public string GetDebugPixelInfoList()
	{
		return string.Format("{0}", string.Join(", ", Enum.GetNames(typeof(DeferredProgram.DebugPixelInfo))));
	}

	public bool SetDebugPixelInfo(bool enable, string name = null)
	{
		bool result = false;
		DeferredProgram.DebugPixelInfo debugPixelInfo = DeferredProgram.DebugPixelInfo.None;
		if (name != null)
		{
			debugPixelInfo = (DeferredProgram.DebugPixelInfo)Enum.Parse(typeof(DeferredProgram.DebugPixelInfo), name, ignoreCase: true);
		}
		DeferredProgram deferredProgram = Engine.Graphics.GPUProgramStore.DeferredProgram;
		if (debugPixelInfo != deferredProgram.DebugPixelInfoView)
		{
			deferredProgram.DebugPixelInfoView = debugPixelInfo;
			deferredProgram.Reset();
			result = true;
		}
		return result;
	}

	public int SelectDebugMaps(string[] names, bool verticalDisplay)
	{
		_debugMapVerticalDisplay = verticalDisplay;
		RenderTargetStore rTStore = Engine.Graphics.RTStore;
		int num = 0;
		for (int i = 0; i < names.Length; i++)
		{
			bool flag = rTStore.ContainsDebugMap(names[i]);
			num += (flag ? 1 : 0);
		}
		DebugMap = num > 0;
		_activeDebugMapsNames = new string[num];
		int num2 = 0;
		for (int j = 0; j < names.Length; j++)
		{
			if (rTStore.ContainsDebugMap(names[j]))
			{
				_activeDebugMapsNames[num2] = names[j];
				num2++;
			}
		}
		return num;
	}

	public void SetSkyAmbientIntensity(float value)
	{
		SkyAmbientIntensity = System.Math.Min(1f, System.Math.Max(value, 0f));
	}

	public void InitFog()
	{
		GLFunctions gL = Engine.Graphics.GL;
		RenderTargetStore rTStore = Engine.Graphics.RTStore;
		UseFog(WeatherModule.ActiveFogMode);
		UseMoodFog(enable: true);
		UseMoodFogSmoothColor(enable: false);
		SetMoodFogDensityVariationScale(1f);
		SetMoodFogSpeedFactor(1f);
		int width = (Engine.Graphics.IsGPULowEnd ? 256 : 512);
		rTStore.SunOcclusionHistory.Resize(width, 1);
		rTStore.SunOcclusionHistory.Bind(clear: false, setupViewport: true);
		float[] data = new float[1] { 0.5f };
		gL.ClearBufferfv(GL.COLOR, 0, data);
		rTStore.SunOcclusionHistory.Unbind();
		RenderTarget.BindHardwareFramebuffer();
	}

	public void UseFog(WeatherModule.FogMode fogMode)
	{
		bool flag = fogMode switch
		{
			WeatherModule.FogMode.Off => false, 
			WeatherModule.FogMode.Static => true, 
			WeatherModule.FogMode.Dynamic => true, 
			_ => false, 
		};
		if (Engine.Graphics.GPUProgramStore.ParticleProgram.UseFog != flag)
		{
			Engine.Graphics.GPUProgramStore.ParticleProgram.UseFog = flag;
			Engine.Graphics.GPUProgramStore.ParticleErosionProgram.UseFog = flag;
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.UseFog = flag;
			Engine.Graphics.GPUProgramStore.DeferredProgram.UseFog = flag;
			Engine.Graphics.GPUProgramStore.MapBlockAnimatedProgram.UseFog = flag;
			Engine.Graphics.GPUProgramStore.SkyProgram.UseMoodFog = flag;
			Engine.Graphics.GPUProgramStore.CloudsProgram.UseMoodFog = flag;
			Engine.Graphics.GPUProgramStore.ParticleProgram.Reset();
			Engine.Graphics.GPUProgramStore.ParticleErosionProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.Reset();
			Engine.Graphics.GPUProgramStore.DeferredProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapBlockAnimatedProgram.Reset();
			Engine.Graphics.GPUProgramStore.SkyProgram.Reset();
			Engine.Graphics.GPUProgramStore.CloudsProgram.Reset();
		}
	}

	public void UseMoodFog(bool enable)
	{
		if (_useMoodFog != enable)
		{
			_useMoodFog = enable;
			Engine.Graphics.GPUProgramStore.ParticleProgram.UseMoodFog = enable;
			Engine.Graphics.GPUProgramStore.ParticleErosionProgram.UseMoodFog = enable;
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.UseMoodFog = enable;
			Engine.Graphics.GPUProgramStore.DeferredProgram.UseMoodFog = enable;
			Engine.Graphics.GPUProgramStore.MapBlockAnimatedProgram.UseMoodFog = enable;
			Engine.Graphics.GPUProgramStore.SkyProgram.UseMoodFog = enable;
			Engine.Graphics.GPUProgramStore.CloudsProgram.UseMoodFog = enable;
			Engine.Graphics.GPUProgramStore.ParticleProgram.Reset();
			Engine.Graphics.GPUProgramStore.ParticleErosionProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.Reset();
			Engine.Graphics.GPUProgramStore.DeferredProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapBlockAnimatedProgram.Reset();
			Engine.Graphics.GPUProgramStore.SkyProgram.Reset();
			Engine.Graphics.GPUProgramStore.CloudsProgram.Reset();
		}
	}

	public void UseMoodFogOnSky(bool enable)
	{
		Engine.Graphics.GPUProgramStore.SkyProgram.UseMoodFog = enable;
		Engine.Graphics.GPUProgramStore.CloudsProgram.UseMoodFog = enable;
		Engine.Graphics.GPUProgramStore.SkyProgram.Reset();
		Engine.Graphics.GPUProgramStore.CloudsProgram.Reset();
	}

	public void UseCustomMoodFog(bool enable)
	{
		_useCustomMoodFog = enable;
	}

	public void SetMoodFogCustomDensity(float density)
	{
		if (_useCustomMoodFog)
		{
			_customDensity = density;
		}
	}

	public void SetMoodFogHeightCustomHeightFalloff(float falloff)
	{
		if (_useCustomMoodFog)
		{
			_customHeightFalloff = falloff;
		}
	}

	public void SetMoodFogDensityVariationScale(float variation)
	{
		_densityVariationScale = variation;
		SceneRenderer.Data.FogMoodParams.W = variation;
	}

	public void SetMoodFogSpeedFactor(float speed)
	{
		_fogSpeedFactor = speed;
		SceneRenderer.Data.FogMoodParams.Z = speed;
	}

	public void SetMoodFogDensityUnderwater(float density)
	{
		if (_useMoodFog)
		{
			SceneRenderer.Data.FogDensityUnderwater = density;
		}
	}

	public void SetMoodFogHeightFalloffUnderwater(float falloff)
	{
		SceneRenderer.Data.FogHeightFalloffUnderwater = falloff;
	}

	public void UseMoodFogDithering(bool enable)
	{
		if (Engine.Graphics.GPUProgramStore.DeferredProgram.UseDithering != enable)
		{
			Engine.Graphics.GPUProgramStore.DeferredProgram.UseDithering = enable;
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.UseDithering = enable;
			Engine.Graphics.GPUProgramStore.MapBlockAnimatedProgram.UseDithering = enable;
			Engine.Graphics.GPUProgramStore.SkyProgram.UseDitheringOnFog = enable;
			Engine.Graphics.GPUProgramStore.CloudsProgram.UseDithering = enable;
			Engine.Graphics.GPUProgramStore.DeferredProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapBlockAnimatedProgram.Reset();
			Engine.Graphics.GPUProgramStore.SkyProgram.Reset();
			Engine.Graphics.GPUProgramStore.CloudsProgram.Reset();
		}
	}

	public void UseMoodFogDitheringOnSkyAndClouds(bool enable)
	{
		Engine.Graphics.GPUProgramStore.SkyProgram.UseDitheringOnFog = enable;
		Engine.Graphics.GPUProgramStore.CloudsProgram.UseDithering = enable;
		Engine.Graphics.GPUProgramStore.SkyProgram.Reset();
		Engine.Graphics.GPUProgramStore.CloudsProgram.Reset();
	}

	public void UseMoodFogSmoothColor(bool enable)
	{
		if (Engine.Graphics.GPUProgramStore.DeferredProgram.UseSmoothNearMoodColor != enable)
		{
			Engine.Graphics.GPUProgramStore.DeferredProgram.UseSmoothNearMoodColor = enable;
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.UseSmoothNearMoodColor = enable;
			Engine.Graphics.GPUProgramStore.ParticleProgram.UseSmoothNearMoodColor = enable;
			Engine.Graphics.GPUProgramStore.ParticleErosionProgram.UseSmoothNearMoodColor = enable;
			Engine.Graphics.GPUProgramStore.MapBlockAnimatedProgram.UseSmoothNearMoodColor = enable;
			Engine.Graphics.GPUProgramStore.DeferredProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.Reset();
			Engine.Graphics.GPUProgramStore.ParticleProgram.Reset();
			Engine.Graphics.GPUProgramStore.ParticleErosionProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapBlockAnimatedProgram.Reset();
		}
	}

	public void PrintFogState()
	{
		string text = "Fog state :";
		switch (WeatherModule.ActiveFogMode)
		{
		case WeatherModule.FogMode.Off:
			text += " off";
			break;
		case WeatherModule.FogMode.Static:
			text += " static";
			break;
		case WeatherModule.FogMode.Dynamic:
			text += " dynamic";
			break;
		}
		if (_useMoodFog)
		{
			float num = SceneRenderer.Data.FogHeightFalloffUnderwater * 100f;
			float num2 = (float)System.Math.Round(System.Math.Log(SceneRenderer.Data.FogDensityUnderwater + 1f), 2);
			float num3 = (_useCustomMoodFog ? _customDensity : WeatherModule.FogDensity);
			float num4 = (_useCustomMoodFog ? _customHeightFalloff : WeatherModule.FogHeightFalloff);
			text += " mood_on";
			if (_useCustomMoodFog)
			{
				text += " custom";
			}
			text = text + "\n underwater density : " + num2;
			text = text + "\n underwater falloff : " + num;
			text = text + "\n global density : " + num3;
			text = text + "\n global falloff : " + num4;
			text = text + "\n speed factor : " + _fogSpeedFactor;
			text = text + "\n variation scale : " + _densityVariationScale;
		}
		else
		{
			text += " mood_off";
		}
		Chat.Log(text);
	}

	public void UseDitheringOnSky(bool enable)
	{
		Engine.Graphics.GPUProgramStore.SkyProgram.UseDitheringOnSky = enable;
		Engine.Graphics.GPUProgramStore.SkyProgram.Reset();
	}

	public void SetWaterQuality(int quality)
	{
		if (_waterQuality != quality)
		{
			if (_waterQuality == 0 || quality == 0)
			{
				Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.WriteRenderConfigBitsInAlpha = quality != 0;
				Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.Reset();
			}
			_waterQuality = quality;
		}
	}

	public void SetUseUnderwaterCaustics(bool enable)
	{
		GraphicsDevice graphics = Engine.Graphics;
		if (graphics.GPUProgramStore.DeferredProgram.UseUnderwaterCaustics != enable)
		{
			graphics.GPUProgramStore.DeferredProgram.UseUnderwaterCaustics = enable;
			graphics.GPUProgramStore.DeferredProgram.Reset();
		}
		if (graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.UseUnderwaterCaustics != enable)
		{
			graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.UseUnderwaterCaustics = enable;
			graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.Reset();
		}
	}

	public void SetUnderwaterCausticsIntensity(float value)
	{
		_underwaterCausticsIntensity = System.Math.Min(1f, System.Math.Max(value, 0f));
	}

	public void SetUnderwaterCausticsScale(float value)
	{
		_underwaterCausticsScale = value;
	}

	public void SetUnderwaterCausticsDistortion(float value)
	{
		_underwaterCausticsDistortion = value;
	}

	public void PrintUnderwaterCausticsParams()
	{
		Chat.Log($"Underwater caustics current params: enabled = {Engine.Graphics.GPUProgramStore.DeferredProgram.UseUnderwaterCaustics}, itensity = {_underwaterCausticsIntensity}, scale = {_underwaterCausticsScale}, distortion = {_underwaterCausticsDistortion}.");
	}

	public void SetCloudsUVMotionScale(float value)
	{
		_cloudsUVMotionScale = value;
	}

	public void SetCloudsUVMotionStrength(float value)
	{
		_cloudsUVMotionStrength = value * 0.01f;
	}

	public void PrintCloudsUVMotionParams()
	{
		Chat.Log($"Clouds UV Motion params: scale = {_cloudsUVMotionScale}, strength = {_cloudsUVMotionStrength * 100f}.");
	}

	public void SetUseCloudsShadows(bool enable)
	{
		GraphicsDevice graphics = Engine.Graphics;
		if (graphics.GPUProgramStore.DeferredProgram.UseCloudsShadows != enable)
		{
			graphics.GPUProgramStore.DeferredProgram.UseCloudsShadows = enable;
			graphics.GPUProgramStore.DeferredProgram.Reset();
		}
		if (graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.UseCloudsShadows != enable)
		{
			graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.UseCloudsShadows = enable;
			graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.Reset();
		}
	}

	public void SetCloudsShadowsIntensity(float value)
	{
		_cloudsShadowsIntensity = System.Math.Min(1f, System.Math.Max(value, 0f));
	}

	public void SetCloudsShadowsScale(float value)
	{
		_cloudsShadowsScale = value;
	}

	public void SetCloudsShadowsBlurriness(float value)
	{
		_cloudsShadowsBlurriness = value;
	}

	public void SetCloudsShadowsSpeed(float value)
	{
		_cloudsShadowsSpeed = value;
	}

	public void PrintCloudsShadowsParams()
	{
		Chat.Log($"Clouds shadows current params: enabled = {Engine.Graphics.GPUProgramStore.DeferredProgram.UseCloudsShadows}, itensity = {_cloudsShadowsIntensity}, scale = {_cloudsShadowsScale}, blur = {_cloudsShadowsBlurriness}, speed = {_cloudsShadowsSpeed}.");
	}

	public void InitPostEffects()
	{
		RenderTargetStore rTStore = Engine.Graphics.RTStore;
		PostEffectProgram postEffectProgram = Engine.Graphics.GPUProgramStore.PostEffectProgram;
		PostEffectRenderer = new PostEffectRenderer(Engine.Graphics, Engine.Profiling, postEffectProgram);
		PostEffectRenderer.InitDepthOfField(rTStore.SceneColor.GetTexture(RenderTarget.Target.Depth));
		PostEffectRenderer.InitBloom(WeatherModule.SkyRenderer.SunTexture, WeatherModule.SkyRenderer.MoonTexture, _glowMask.GLTexture, WeatherModule.SkyRenderer.DrawSun, WeatherModule.SkyRenderer.DrawMoon, useBloom: true, useSun: true, useMoon: false, useSunshaft: false, usePow: true, useFullbright: true);
	}

	public void UseVolumetricSunshaft(bool enable)
	{
		_useVolumetricSunshaft = enable;
		PostEffectProgram postEffectProgram = Engine.Graphics.GPUProgramStore.PostEffectProgram;
		postEffectProgram.UseVolumetricSunshaft = enable;
		postEffectProgram.Reset();
	}

	private void InitForceField()
	{
		Image image = new Image(File.ReadAllBytes(Path.Combine(Paths.GameData, "ShaderTextures/ShieldNormalMap.png")));
		_forceFieldNormalMap = new Texture(Texture.TextureTypes.Texture2D);
		_forceFieldNormalMap.CreateTexture2D(image.Width, image.Height, image.Pixels, 5, GL.LINEAR_MIPMAP_NEAREST, GL.LINEAR, GL.REPEAT, GL.REPEAT, GL.RGBA, GL.RGBA, GL.UNSIGNED_BYTE, requestMipMapChain: true);
		Engine.FXSystem.ForceFields.NormalMap = _forceFieldNormalMap.GLTexture;
	}

	private void DisposeForceField()
	{
		_forceFieldNormalMap.Dispose();
	}

	private void UpdateForceField()
	{
		ForceFieldProgram forceFieldProgram = Engine.Graphics.GPUProgramStore.ForceFieldProgram;
		Vector2 uvAnimationSpeed = new Vector2(0f, -0.05f);
		Vector4 color = new Vector4(0f, 0.4f, 0.8f, 0.15f);
		Vector4 intersectionHighlightColorOpacity = new Vector4(0f, 0.4f, 0.8f, 0.75f);
		float intersectionHighlightThickness = 1f;
		ForceFieldFXSystem.FXShape shape;
		float num;
		int outlineMode;
		if (ForceFieldTest == 1)
		{
			shape = ForceFieldFXSystem.FXShape.Quad;
			num = 5f;
			outlineMode = (ForceFieldOptionOutline ? forceFieldProgram.OutlineModeUV : forceFieldProgram.OutlineModeNone);
		}
		else if (ForceFieldTest == 2)
		{
			shape = ForceFieldFXSystem.FXShape.Sphere;
			num = 1.5f;
			outlineMode = (ForceFieldOptionOutline ? forceFieldProgram.OutlineModeNormal : forceFieldProgram.OutlineModeNone);
		}
		else
		{
			shape = ForceFieldFXSystem.FXShape.Sphere;
			num = 1.5f;
			outlineMode = (ForceFieldOptionOutline ? forceFieldProgram.OutlineModeNormal : forceFieldProgram.OutlineModeNone);
		}
		int num2 = System.Math.Min(ForceFieldCount, 20);
		Vector3 vector = new Vector3(0f, 0f, num);
		for (int i = 0; i < num2; i++)
		{
			if (ForceFieldTest == 1)
			{
				_forceFieldModelMatrices[i] = Matrix.CreateTranslation(new Vector3(0f, 0f, 15f) + i * vector + SceneRenderer.Data.PlayerRenderPosition);
			}
			else if (ForceFieldTest == 2)
			{
				_forceFieldModelMatrices[i] = Matrix.CreateTranslation(new Vector3(0f, num * 0.5f, 0f) + i * vector + SceneRenderer.Data.PlayerRenderPosition);
			}
			else
			{
				_forceFieldModelMatrices[i] = Matrix.CreateTranslation(new Vector3(0f, num * 0.5f, 0f) + i * vector + SceneRenderer.Data.PlayerRenderPosition);
			}
			Matrix matrix = Matrix.CreateScale(num);
			Matrix.Multiply(ref matrix, ref _forceFieldModelMatrices[i], out _forceFieldModelMatrices[i]);
			_forceFieldNormalMatrices[i] = Matrix.Transpose(Matrix.Invert(_forceFieldModelMatrices[i] * SceneRenderer.Data.ViewRotationMatrix));
		}
		if (ForceFieldTest <= 0)
		{
			return;
		}
		float num3 = (ForceFieldOptionAnimation ? FrameTime : 0f);
		Engine.FXSystem.ForceFields.SetupSceneData(ref SceneRenderer.Data.ViewRotationMatrix, ref SceneRenderer.Data.ViewRotationProjectionMatrix);
		int num4 = num2;
		Engine.FXSystem.ForceFields.PrepareForIncomingColorTasks(num4);
		Engine.FXSystem.ForceFields.PrepareForIncomingDistortionTasks(num4);
		for (int j = 0; j < num4; j++)
		{
			if (ForceFieldOptionColor)
			{
				Engine.FXSystem.ForceFields.RegisterColorTask(shape, ref _forceFieldModelMatrices[j], ref _forceFieldNormalMatrices[j], uvAnimationSpeed, outlineMode, color, intersectionHighlightColorOpacity, intersectionHighlightThickness);
			}
			if (ForceFieldOptionDistortion)
			{
				Engine.FXSystem.ForceFields.RegisterDistortionTask(shape, ref _forceFieldModelMatrices[j], uvAnimationSpeed);
			}
		}
	}

	private void InitOIT()
	{
		Debug.Assert(SceneRenderer.OIT != null);
		SceneRenderer.OIT.SetupRenderingProfiles(70, 71, 72, 73, 74);
		SetupOIT(OrderIndependentTransparency.Method.MOIT);
		SetupOITPrepassScale(8u);
		SceneRenderer.OIT.SetupTextureUnits(18, 17);
		SceneRenderer.OIT.RegisterDrawTransparentsFunc(DrawTransparentsFullRes, DrawTransparentsHalfRes, null);
		_oitRes = 0;
	}

	public void SetUseChunksOIT(bool enable)
	{
		if (_useChunksOIT != enable)
		{
			_useChunksOIT = enable;
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.UseOIT = enable;
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.WriteRenderConfigBitsInAlpha = !enable;
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.Reset();
		}
	}

	public void ChangeOITResolution()
	{
		_oitRes = (_oitRes + 1) % 3;
		switch (_oitRes)
		{
		case 0:
			SceneRenderer.OIT.RegisterDrawTransparentsFunc(DrawTransparentsFullRes, DrawTransparentsHalfRes, null);
			break;
		case 1:
			SceneRenderer.OIT.RegisterDrawTransparentsFunc(null, DrawTransparentsFullRes, DrawTransparentsHalfRes);
			break;
		case 2:
			SceneRenderer.OIT.RegisterDrawTransparentsFunc(null, null, DrawTransparents);
			break;
		}
	}

	public void UseOITEdgeFixup(bool fixupHalfRes, bool fixupQuarterRes)
	{
		SceneRenderer.OIT.UseEdgeFixupPass(fixupHalfRes, fixupQuarterRes, 7);
	}

	public void SetupOIT(OrderIndependentTransparency.Method method)
	{
		if (SceneRenderer.OIT.CurrentMethod != method)
		{
			SceneRenderer.OIT.SetMethod(method);
			Engine.Graphics.GPUProgramStore.ParticleProgram.UseOIT = method != OrderIndependentTransparency.Method.None;
			Engine.Graphics.GPUProgramStore.ForceFieldProgram.UseOIT = method != OrderIndependentTransparency.Method.None;
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.UseOIT = _useChunksOIT && method != OrderIndependentTransparency.Method.None;
			Engine.Graphics.GPUProgramStore.ParticleProgram.Reset();
			Engine.Graphics.GPUProgramStore.ForceFieldProgram.Reset();
			Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.Reset();
		}
	}

	public bool SetupOITPrepassScale(uint prepassInvScale)
	{
		bool result = true;
		OrderIndependentTransparency.ResolutionScale prepassResolutionScale = OrderIndependentTransparency.ResolutionScale.Full;
		switch (prepassInvScale)
		{
		case 1u:
			prepassResolutionScale = OrderIndependentTransparency.ResolutionScale.Full;
			break;
		case 2u:
			prepassResolutionScale = OrderIndependentTransparency.ResolutionScale.Half;
			break;
		case 4u:
			prepassResolutionScale = OrderIndependentTransparency.ResolutionScale.Quarter;
			break;
		case 8u:
			prepassResolutionScale = OrderIndependentTransparency.ResolutionScale.Eighth;
			break;
		default:
			result = false;
			break;
		}
		SceneRenderer.OIT.SetPrepassResolutionScale(prepassResolutionScale);
		return result;
	}

	public void SetRenderPassEnabled(uint passId, bool enable)
	{
		if (passId >= 13)
		{
			throw new Exception($"Invalid pass id {passId} - it should be < {13u}");
		}
		if (passId == 12)
		{
			PostEffectRenderer.UseFXAA(enable);
		}
		else
		{
			_renderPassStates[passId] = enable;
		}
	}

	private void InitRenderSetup()
	{
		for (int i = 0; (long)i < 13L; i++)
		{
			_renderPassStates[i] = true;
		}
		RenderPassNames[0] = "shadowmap";
		RenderPassNames[1] = "firstperson";
		RenderPassNames[2] = "map_near_opaque";
		RenderPassNames[3] = "map_near_alphatested";
		RenderPassNames[4] = "map_anim";
		RenderPassNames[5] = "map_far_opaque";
		RenderPassNames[6] = "map_far_alphatested";
		RenderPassNames[7] = "entities";
		RenderPassNames[8] = "sky";
		RenderPassNames[9] = "map_alphablended";
		RenderPassNames[10] = "vfx";
		RenderPassNames[11] = "names";
		RenderPassNames[12] = "postfx";
	}

	private void CreateCubeMapMesh()
	{
		MeshProcessor.CreateSimpleBox(ref _cube, 2f);
	}

	private void DrawCubeMapTest()
	{
		GLFunctions gL = Engine.Graphics.GL;
		gL.Disable(GL.CULL_FACE);
		gL.ActiveTexture(GL.TEXTURE0);
		gL.BindTexture(GL.TEXTURE_CUBE_MAP, _cubemap.GLTexture);
		gL.BindVertexArray(_cube.VertexArray);
		CubemapProgram cubemapProgram = Engine.Graphics.GPUProgramStore.CubemapProgram;
		gL.UseProgram(cubemapProgram);
		Matrix matrix = Matrix.Identity;
		Matrix.ApplyScale(ref matrix, 30f);
		Matrix.Multiply(ref matrix, ref SceneRenderer.Data.ViewRotationProjectionMatrix, out matrix);
		cubemapProgram.MVPMatrix.SetValue(ref matrix);
		gL.DrawElements(GL.TRIANGLES, _cube.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
		gL.Enable(GL.CULL_FACE);
	}

	private void DisposeCubemapMesh()
	{
		_cube.Dispose();
	}

	public static byte[][] ReadCubemapImagesFromDisk(string pathToCubemapFolder)
	{
		Image image = new Image(File.ReadAllBytes(Path.Combine(pathToCubemapFolder, "right.png")));
		Image image2 = new Image(File.ReadAllBytes(Path.Combine(pathToCubemapFolder, "left.png")));
		Image image3 = new Image(File.ReadAllBytes(Path.Combine(pathToCubemapFolder, "top.png")));
		Image image4 = new Image(File.ReadAllBytes(Path.Combine(pathToCubemapFolder, "bottom.png")));
		Image image5 = new Image(File.ReadAllBytes(Path.Combine(pathToCubemapFolder, "front.png")));
		Image image6 = new Image(File.ReadAllBytes(Path.Combine(pathToCubemapFolder, "back.png")));
		Debug.Assert(image.Width == image2.Width && image2.Width == image3.Width && image3.Width == image4.Width && image4.Width == image5.Width && image5.Width == image6.Width);
		Debug.Assert(image.Height == image2.Height && image2.Height == image3.Height && image3.Height == image4.Height && image4.Height == image5.Height && image5.Height == image6.Height);
		return new byte[6][] { image.Pixels, image2.Pixels, image3.Pixels, image4.Pixels, image5.Pixels, image6.Pixels };
	}

	public void UpdateAtlasSizes()
	{
		AtlasSizes = new Point[4]
		{
			new Point(MapModule.TextureAtlas.Width, MapModule.TextureAtlas.Height),
			new Point(EntityStoreModule.TextureAtlas.Width, EntityStoreModule.TextureAtlas.Height),
			new Point(App.CharacterPartStore.TextureAtlas.Width, App.CharacterPartStore.TextureAtlas.Height),
			new Point(FXModule.TextureAtlas.Width, FXModule.TextureAtlas.Height)
		};
	}

	private void InitShaderTextures()
	{
		Image image = new Image(File.ReadAllBytes(Path.Combine(Paths.GameData, "ShaderTextures/WaterNormals.png")));
		_waterNormals = new Texture(Texture.TextureTypes.Texture2D);
		_waterNormals.CreateTexture2D(image.Width, image.Height, image.Pixels, 5, GL.LINEAR_MIPMAP_NEAREST, GL.LINEAR, GL.REPEAT, GL.REPEAT, GL.RGBA, GL.RGBA, GL.UNSIGNED_BYTE, requestMipMapChain: true);
		Image image2 = new Image(File.ReadAllBytes(Path.Combine(Paths.GameData, "ShaderTextures/WaterCaustics.png")));
		_waterCaustics = new Texture(Texture.TextureTypes.Texture2D);
		_waterCaustics.CreateTexture2D(image2.Width, image2.Height, image2.Pixels, 5, GL.LINEAR, GL.LINEAR, GL.REPEAT, GL.REPEAT, GL.RGBA, GL.RGBA, GL.UNSIGNED_BYTE, requestMipMapChain: true);
		Image image3 = new Image(File.ReadAllBytes(Path.Combine(Paths.GameData, "ShaderTextures/FlowMap.png")));
		_flowMap = new Texture(Texture.TextureTypes.Texture2D);
		_flowMap.CreateTexture2D(image3.Width, image3.Height, image3.Pixels, 5, GL.LINEAR, GL.LINEAR, GL.REPEAT, GL.REPEAT, GL.RGBA, GL.RGBA, GL.UNSIGNED_BYTE, requestMipMapChain: true);
		Image image4 = new Image(File.ReadAllBytes(Path.Combine(Paths.GameData, "ShaderTextures/FogNoiseMap.png")));
		_fogNoise = new Texture(Texture.TextureTypes.Texture2D);
		_fogNoise.CreateTexture2D(image4.Width, image4.Height, image4.Pixels, 5, GL.LINEAR, GL.LINEAR, GL.REPEAT, GL.REPEAT, GL.RGBA, GL.RGBA, GL.UNSIGNED_BYTE, requestMipMapChain: true);
		Image image5 = new Image(File.ReadAllBytes(Path.Combine(Paths.GameData, "ShaderTextures/GlowMask.png")));
		_glowMask = new Texture(Texture.TextureTypes.Texture2D);
		_glowMask.CreateTexture2D(image5.Width, image5.Height, image5.Pixels, 5, GL.LINEAR, GL.LINEAR);
		byte[][] pixels = ReadCubemapImagesFromDisk(Path.Combine(Paths.GameData, "ShaderTextures/skybox"));
		_cubemap = new Texture(Texture.TextureTypes.TextureCubemap);
		_cubemap.CreateTextureCubemap(2048, 2048, pixels, 0, GL.LINEAR, GL.LINEAR, GL.CLAMP_TO_EDGE, GL.CLAMP_TO_EDGE, GL.CLAMP_TO_EDGE);
	}

	private void DisposeShaderTextures()
	{
		_cubemap.Dispose();
		_glowMask.Dispose();
		_fogNoise.Dispose();
		_flowMap.Dispose();
		_waterCaustics.Dispose();
		_waterNormals.Dispose();
	}

	private void InitDraw()
	{
		GLFunctions gL = Engine.Graphics.GL;
		int width = Engine.Window.Viewport.Width;
		int height = Engine.Window.Viewport.Height;
		Engine.AnimationSystem.SetTransferMethod(AnimationSystem.TransferMethod.ParallelInterleaved);
		SceneRenderer = new SceneRenderer(Engine.Graphics, Engine.Profiling, width, height);
		_cameraSceneView = new SceneView();
		_sunSceneView = new SceneView();
		SetFieldOfView(App.Settings.FieldOfView);
		InitTexureUnitsUsage();
		InitShaderTextures();
		InitPostEffects();
		InitGraphicsProfiling();
		InitDebugMapInfos();
		InitForceField();
		InitRenderSetup();
		InitFog();
		InitRenderingOptions();
		SetRenderingOptions(ref IngameMode);
		UseVolumetricSunshaft(enable: false);
		PostEffectRenderer.UseBloomSunShaft(enable: false);
		InitShadowMapping();
		InitLighting();
		InitOIT();
		CreateCubeMapMesh();
	}

	private void DisposeDraw()
	{
		ReleaseDebugMapInfos();
		DisposeForceField();
		DisposeShaderTextures();
		SceneRenderer.Dispose();
		PostEffectRenderer.Dispose();
		DisposeCubemapMesh();
	}

	private void InitTexureUnitsUsage()
	{
		GPUProgramStore gPUProgramStore = Engine.Graphics.GPUProgramStore;
		ForceFieldProgram.TextureUnitLayout textureUnitLayout = default(ForceFieldProgram.TextureUnitLayout);
		textureUnitLayout.Texture = 9;
		textureUnitLayout.SceneDepth = 12;
		textureUnitLayout.OITTotalOpticalDepth = 17;
		textureUnitLayout.OITMoments = 18;
		gPUProgramStore.ForceFieldProgram.SetupTextureUnits(ref textureUnitLayout, initUniforms: true);
		gPUProgramStore.BuilderToolProgram.SetupTextureUnits(ref textureUnitLayout, initUniforms: true);
		ParticleProgram.TextureUnitLayout textureUnitLayout2 = default(ParticleProgram.TextureUnitLayout);
		textureUnitLayout2.Atlas = 7;
		textureUnitLayout2.LinearFilteredAtlas = 8;
		textureUnitLayout2.UVMotion = 10;
		textureUnitLayout2.FXDataBuffer = 11;
		textureUnitLayout2.LightIndicesOrDataBuffer = 16;
		textureUnitLayout2.LightGrid = 15;
		textureUnitLayout2.ShadowMap = 14;
		textureUnitLayout2.FogNoise = 13;
		textureUnitLayout2.SceneDepth = 12;
		textureUnitLayout2.OITMoments = 18;
		textureUnitLayout2.OITTotalOpticalDepth = 17;
		gPUProgramStore.ParticleProgram.SetupTextureUnits(ref textureUnitLayout2, initUniforms: true);
		gPUProgramStore.ParticleErosionProgram.SetupTextureUnits(ref textureUnitLayout2, initUniforms: true);
		gPUProgramStore.ParticleDistortionProgram.SetupTextureUnits(ref textureUnitLayout2, initUniforms: true);
		MapChunkAlphaBlendedProgram.TextureUnitLayout textureUnitLayout3 = default(MapChunkAlphaBlendedProgram.TextureUnitLayout);
		textureUnitLayout3.Texture = 0;
		textureUnitLayout3.SceneDepth = 12;
		textureUnitLayout3.SceneDepthLowRes = 1;
		textureUnitLayout3.Normals = 2;
		textureUnitLayout3.Refraction = 3;
		textureUnitLayout3.SceneColor = 4;
		textureUnitLayout3.Caustics = 5;
		textureUnitLayout3.CloudShadow = 6;
		textureUnitLayout3.FogNoise = 13;
		textureUnitLayout3.ShadowMap = 14;
		textureUnitLayout3.LightIndicesOrDataBuffer = 16;
		textureUnitLayout3.LightGrid = 15;
		textureUnitLayout3.OITMoments = 18;
		textureUnitLayout3.OITTotalOpticalDepth = 17;
		gPUProgramStore.MapChunkAlphaBlendedProgram.SetupTextureUnits(ref textureUnitLayout3, initUniforms: true);
	}

	private void SetupChunkAlphaBlendedTextures(bool skipLightTextures, bool skipShadowMap, bool skipSceneDepth, bool skipFogNoise)
	{
		GLFunctions gL = Engine.Graphics.GL;
		RenderTargetStore rTStore = Engine.Graphics.RTStore;
		RenderTarget renderTarget = (Engine.Graphics.IsGPULowEnd ? rTStore.LinearZHalfRes : rTStore.LinearZ);
		if (WeatherModule.SkyRenderer.CloudsTextures.Length != 0)
		{
			gL.ActiveTexture(GL.TEXTURE6);
			gL.BindTexture(GL.TEXTURE_2D, WeatherModule.SkyRenderer.CloudsTextures[0]);
		}
		gL.ActiveTexture(GL.TEXTURE5);
		gL.BindTexture(GL.TEXTURE_2D, _waterCaustics.GLTexture);
		gL.ActiveTexture(GL.TEXTURE1);
		gL.BindTexture(GL.TEXTURE_2D, rTStore.LinearZHalfRes.GetTexture(RenderTarget.Target.Color0));
		gL.ActiveTexture(GL.TEXTURE2);
		gL.BindTexture(GL.TEXTURE_2D, _waterNormals.GLTexture);
		gL.ActiveTexture(GL.TEXTURE3);
		gL.BindSampler(3u, Engine.Graphics.SamplerLinearMipmapLinearA);
		gL.BindTexture(GL.TEXTURE_2D, rTStore.SceneColorHalfRes.GetTexture(RenderTarget.Target.Color0));
		gL.ActiveTexture(GL.TEXTURE4);
		gL.BindSampler(4u, Engine.Graphics.SamplerLinearMipmapLinearB);
		gL.BindTexture(GL.TEXTURE_2D, rTStore.PreviousSceneColor.GetTexture(RenderTarget.Target.Color0));
		gL.ActiveTexture(GL.TEXTURE0);
		gL.BindTexture(GL.TEXTURE_2D, MapModule.TextureAtlas.GLTexture);
		if (!skipLightTextures)
		{
			SceneRenderer.ClusteredLighting.SetupLightDataTextures(15u, 16u);
		}
		if (!skipSceneDepth)
		{
			gL.ActiveTexture(GL.TEXTURE12);
			gL.BindTexture(GL.TEXTURE_2D, renderTarget.GetTexture(RenderTarget.Target.Color0));
		}
		if (!skipFogNoise)
		{
			gL.ActiveTexture(GL.TEXTURE13);
			gL.BindTexture(GL.TEXTURE_2D, _fogNoise.GLTexture);
		}
		if (!skipShadowMap)
		{
			gL.ActiveTexture(GL.TEXTURE14);
			gL.BindTexture(GL.TEXTURE_2D, rTStore.ShadowMap.GetTexture(RenderTarget.Target.Depth));
		}
		gL.ActiveTexture(GL.TEXTURE0);
	}

	private void SetupVFXTextures(bool skipLightTextures, bool skipShadowMap, bool skipSceneDepth, bool skipFogNoise, bool skipForceFieldTextures)
	{
		GLFunctions gL = Engine.Graphics.GL;
		RenderTargetStore rTStore = Engine.Graphics.RTStore;
		RenderTarget renderTarget = (Engine.Graphics.IsGPULowEnd ? rTStore.LinearZHalfRes : rTStore.LinearZ);
		if (!skipLightTextures)
		{
			SceneRenderer.ClusteredLighting.SetupLightDataTextures(15u, 16u);
		}
		Engine.FXSystem.SetupDrawDataTexture(11u);
		gL.ActiveTexture(GL.TEXTURE8);
		gL.BindSampler(8u, Engine.FXSystem.SmoothSampler);
		gL.BindTexture(GL.TEXTURE_2D, FXModule.TextureAtlas.GLTexture);
		gL.ActiveTexture(GL.TEXTURE7);
		gL.BindTexture(GL.TEXTURE_2D, FXModule.TextureAtlas.GLTexture);
		gL.ActiveTexture(GL.TEXTURE10);
		gL.BindTexture(GL.TEXTURE_2D_ARRAY, FXModule.UVMotionTextureArray2D);
		if (!skipForceFieldTextures)
		{
			gL.ActiveTexture(GL.TEXTURE9);
			gL.BindTexture(GL.TEXTURE_2D, Engine.FXSystem.ForceFields.NormalMap);
		}
		if (!skipSceneDepth)
		{
			gL.ActiveTexture(GL.TEXTURE12);
			gL.BindTexture(GL.TEXTURE_2D, renderTarget.GetTexture(RenderTarget.Target.Color0));
		}
		if (!skipFogNoise)
		{
			gL.ActiveTexture(GL.TEXTURE13);
			gL.BindTexture(GL.TEXTURE_2D, _fogNoise.GLTexture);
		}
		if (!skipShadowMap)
		{
			gL.ActiveTexture(GL.TEXTURE14);
			gL.BindTexture(GL.TEXTURE_2D, rTStore.ShadowMap.GetTexture(RenderTarget.Target.Depth));
		}
		gL.ActiveTexture(GL.TEXTURE0);
	}

	private void InitDebugMapInfos()
	{
		RenderTargetStore rTStore = Engine.Graphics.RTStore;
		rTStore.RegisterDebugMap("atlas_map", MapModule.TextureAtlas);
		rTStore.RegisterDebugMap("atlas_entity", EntityStoreModule.TextureAtlas);
		rTStore.RegisterDebugMap("atlas_fx", FXModule.TextureAtlas);
		rTStore.RegisterDebugMap("water_normals", _waterNormals);
		rTStore.RegisterDebugMap("water_caustics", _waterCaustics);
		rTStore.RegisterDebugMap("flow", _flowMap);
		_debugTextureArrayLayerCount = FXModule.UVMotionTextureCount;
		rTStore.RegisterDebugMap2DArray("uvmotion", FXModule.UVMotionTextureArray2D, 64, 64, _debugTextureArrayLayerCount);
		rTStore.RegisterDebugMapCubemap("cubemap", _cubemap);
		_activeDebugMapsNames = new string[1] { "atlas_map" };
	}

	private void ReleaseDebugMapInfos()
	{
		RenderTargetStore rTStore = Engine.Graphics.RTStore;
		rTStore.UnregisterDebugMap("atlas_map");
		rTStore.UnregisterDebugMap("atlas_entity");
		rTStore.UnregisterDebugMap("atlas_fx");
		rTStore.UnregisterDebugMap("water_normals");
		rTStore.UnregisterDebugMap("water_caustics");
		rTStore.UnregisterDebugMap("flow");
		rTStore.UnregisterDebugMap("uvmotion");
		rTStore.UnregisterDebugMap("cubemap");
	}

	private void UpdateDebugDrawMap()
	{
		RenderTargetStore rTStore = Engine.Graphics.RTStore;
		if (_debugTextureArrayLayerCount != FXModule.UVMotionTextureCount)
		{
			rTStore.UnregisterDebugMap("uvmotion");
			rTStore.RegisterDebugMap2DArray("uvmotion", FXModule.UVMotionTextureArray2D, 64, 64, FXModule.UVMotionTextureCount);
			_debugTextureArrayLayerCount = FXModule.UVMotionTextureCount;
		}
	}

	public void ReloadShaderTextures()
	{
		ReleaseDebugMapInfos();
		DisposeShaderTextures();
		InitShaderTextures();
		InitDebugMapInfos();
		PostEffectRenderer.InitBloom(WeatherModule.SkyRenderer.SunTexture, WeatherModule.SkyRenderer.MoonTexture, _glowMask.GLTexture, WeatherModule.SkyRenderer.DrawSun, WeatherModule.SkyRenderer.DrawMoon, useBloom: true, useSun: true, useMoon: false, useSunshaft: false, usePow: true, useFullbright: true);
	}

	private void InitGraphicsProfiling()
	{
		Engine.Profiling.Initialize(84);
		Engine.Profiling.CreateMeasure("Full-Frame", 0);
		Engine.Profiling.CreateMeasure("QueuedActions", 1, cpuOnly: true, alwaysEnabled: false, isExternal: true);
		Engine.Profiling.CreateMeasure("OnNewFrame", 2, cpuOnly: true);
		Engine.Profiling.CreateMeasure("-> Prepare-Chunks", 3, cpuOnly: true);
		Engine.Profiling.CreateMeasure("-> Prepare-Entities", 4, cpuOnly: true);
		Engine.Profiling.CreateMeasure("-> Modules.Tick", 5, cpuOnly: true);
		Engine.Profiling.CreateMeasure("-> Modules.PreUpdate", 6, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> InteractionModule", 7, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> PrepareLights", 8, cpuOnly: true);
		Engine.Profiling.CreateMeasure("-> GatherChunks", 9, cpuOnly: true);
		Engine.Profiling.CreateMeasure("-> PrepareChunksForDraw", 10, cpuOnly: true);
		Engine.Profiling.CreateMeasure("-> UpdateAnimation-Early", 11, cpuOnly: true);
		Engine.Profiling.CreateMeasure("-> Occlusion-Setup", 12);
		Engine.Profiling.CreateMeasure(" --> BuildMap", 13);
		Engine.Profiling.CreateMeasure("   -> RenderOccluders", 14);
		Engine.Profiling.CreateMeasure("   -> Reproject", 15);
		Engine.Profiling.CreateMeasure("   -> CreateHiZ", 16);
		Engine.Profiling.CreateMeasure(" --> PrepareOccludees", 17);
		Engine.Profiling.CreateMeasure(" --> TestOccludees", 18);
		Engine.Profiling.CreateMeasure("-> Modules.Update", 19, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> MapModule.Update", 20, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> EntityStoreModule.Update", 23, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> AmbienceUpdate", 21, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> WeatherUpdate", 22, cpuOnly: true);
		Engine.Profiling.CreateMeasure("-> UpdateSceneData", 24, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> GatherChunksForShadowMap", 25, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> GatherAnimatedChunks", 26, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> GatherEntities", 27, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> UpdateFX", 28, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> GatherFX", 29, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> UpdateFXSimulation", 30, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> UpdateAnimation", 31, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> PrepareEntitiesForDraw", 32, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> PrepareShadowCascades", 33, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> UpdateLights", 34, cpuOnly: true);
		Engine.Profiling.CreateMeasure("   -> Clear", 35, cpuOnly: true);
		Engine.Profiling.CreateMeasure("   -> Clustering", 36, cpuOnly: true);
		Engine.Profiling.CreateMeasure("   -> Refine", 37, cpuOnly: true);
		Engine.Profiling.CreateMeasure("   -> FillGridData", 38, cpuOnly: true);
		Engine.Profiling.CreateMeasure("   -> SendDataToGPU", 39, cpuOnly: true);
		Engine.Profiling.CreateMeasure(" --> PrepareFXForDraw", 40, cpuOnly: true);
		Engine.Profiling.CreateMeasure("   -> SendDataToGPU", 41, cpuOnly: true);
		Engine.Profiling.CreateMeasure("Render", 42);
		Engine.Profiling.CreateMeasure("-> Occlusion-FetchResults", 43);
		Engine.Profiling.CreateMeasure("-> AnalyzeSunOcclusion", 44);
		Engine.Profiling.CreateMeasure("-> Reflection-BuildMips", 45);
		Engine.Profiling.CreateMeasure("-> ShadowMap-Build", 46);
		Engine.Profiling.CreateMeasure("-> FirstPerson", 47);
		Engine.Profiling.CreateMeasure("-> World-Near", 48);
		Engine.Profiling.CreateMeasure(" --> World-Animated", 49);
		Engine.Profiling.CreateMeasure("-> World-Far", 50);
		Engine.Profiling.CreateMeasure("-> Entities", 51);
		Engine.Profiling.CreateMeasure("-> LinearZ", 52);
		Engine.Profiling.CreateMeasure("-> LinearZDownsample", 53);
		Engine.Profiling.CreateMeasure("-> ZDownsample", 54);
		Engine.Profiling.CreateMeasure("-> Edge-Detection", 55);
		Engine.Profiling.CreateMeasure("-> DeferredShadow", 56);
		Engine.Profiling.CreateMeasure("-> SSAO", 57);
		Engine.Profiling.CreateMeasure("-> Blur(AO,DefShadow)", 58);
		Engine.Profiling.CreateMeasure("-> Volumetric sunshafts", 59);
		Engine.Profiling.CreateMeasure("-> Lights", 60, cpuOnly: false, alwaysEnabled: true);
		Engine.Profiling.CreateMeasure(" --> Stencil", 61);
		Engine.Profiling.CreateMeasure(" --> Full-Res", 62);
		Engine.Profiling.CreateMeasure(" --> Low-Res", 63);
		Engine.Profiling.CreateMeasure(" --> Mix", 64);
		Engine.Profiling.CreateMeasure("-> ApplyDeferred", 65);
		Engine.Profiling.CreateMeasure("-> Particles-Opaque", 66);
		Engine.Profiling.CreateMeasure("-> Weather", 67);
		Engine.Profiling.CreateMeasure("-> World-AlphaBlended", 68);
		Engine.Profiling.CreateMeasure("-> Transparency", 69);
		Engine.Profiling.CreateMeasure(" --> OIT-Prepass", 70);
		Engine.Profiling.CreateMeasure(" --> OIT-Accumulate-Quarter-Res", 71);
		Engine.Profiling.CreateMeasure(" --> OIT-Accumulate-Half-Res", 72);
		Engine.Profiling.CreateMeasure(" --> OIT-Accumulate-Full-Res", 73);
		Engine.Profiling.CreateMeasure(" --> OIT-Composite", 74);
		Engine.Profiling.CreateMeasure("-> Texts", 75);
		Engine.Profiling.CreateMeasure("-> Distortion", 76);
		Engine.Profiling.CreateMeasure("-> PostFX", 77);
		Engine.Profiling.CreateMeasure(" ---> DepthOfField", 78);
		Engine.Profiling.CreateMeasure(" ---> Bloom", 79);
		Engine.Profiling.CreateMeasure(" ---> Combine + FXAA", 80);
		Engine.Profiling.CreateMeasure(" ---> TAA", 81);
		Engine.Profiling.CreateMeasure(" ---> Blur", 82);
		Engine.Profiling.CreateMeasure("-> ScreenFX", 83);
		ProfilingModule.SetupDetailedMeasures();
		SceneRenderer.SetupRenderingProfiles(60, 62, 63, 61, 64, 52, 53, 54, 55);
		SceneRenderer.SetupClusteredLightingRenderingProfiles(35, 36, 37, 38, 39);
		Engine.OcclusionCulling.SetupRenderingProfiles(13, 14, 15, 16, 17, 18, 43);
		Engine.FXSystem.SetupRenderingProfile(41);
		PostEffectRenderer.SetupRenderingProfiles(78, 79, 80, 81, 82);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetFieldOfView(float fieldOfView)
	{
		ActiveFieldOfView = fieldOfView;
		SceneRenderer.ComputeNearChunkDistance(ActiveFieldOfView);
	}

	public void SetOcclusionCulling(bool enable)
	{
		Engine.OcclusionCulling.IsEnabled = enable;
		_requestedOpaqueChunkOccludersCount = 15;
	}

	public void SetOpaqueOccludersCount(int count)
	{
		_requestedOpaqueChunkOccludersCount = count;
	}

	public void UseOcclusionCullingReprojection(bool enable)
	{
		_useOcclusionCullingReprojection = enable;
	}

	public void UseOcclusionCullingReprojectionHoleFilling(bool enable)
	{
		_useOcclusionCullingReprojectionHoleFilling = enable;
	}

	public void UseChunkOccluderPlanes(bool enable)
	{
		_useChunkOccluderPlanes = enable;
	}

	public void UseOpaqueChunkOccluders(bool enable)
	{
		_useOpaqueChunkOccluders = enable;
	}

	public void UseAlphaTestedChunkOccluders(bool enable)
	{
		if (_useAlphaTestedChunkOccluders != enable)
		{
			_useAlphaTestedChunkOccluders = enable;
			Engine.Graphics.GPUProgramStore.ZOnlyMapChunkProgram.AlphaTest = _useAlphaTestedChunkOccluders;
			Engine.Graphics.GPUProgramStore.ZOnlyMapChunkProgram.Reset();
		}
	}

	public void DrawOccluders()
	{
		GLFunctions gL = Engine.Graphics.GL;
		if (UseLocalPlayerOccluder)
		{
			SceneRenderer.SetupEntityShadowMapDataTexture(7u);
			SceneRenderer.SetupModelVFXDataTexture(6u);
			gL.ActiveTexture(GL.TEXTURE4);
			gL.BindTexture(GL.TEXTURE_2D, _fogNoise.GLTexture);
			gL.ActiveTexture(GL.TEXTURE2);
			gL.BindTexture(GL.TEXTURE_2D, App.CharacterPartStore.TextureAtlas.GLTexture);
			gL.ActiveTexture(GL.TEXTURE1);
			gL.BindTexture(GL.TEXTURE_2D, EntityStoreModule.TextureAtlas.GLTexture);
			gL.ActiveTexture(GL.TEXTURE0);
			gL.BindTexture(GL.TEXTURE_2D, MapModule.TextureAtlas.GLTexture);
			LocalPlayer.DrawOccluders(_cameraSceneView);
		}
		SceneRenderer.DrawOccluders();
	}

	private void UpdateOcclusionCulling()
	{
		SceneRenderer.PrepareOcclusionCulling(_requestedOpaqueChunkOccludersCount, _useChunkOccluderPlanes, _useOpaqueChunkOccluders, _useAlphaTestedChunkOccluders, 0, MapModule.TextureAtlas.GLTexture);
		int occludeesCount;
		ref OcclusionCulling.OccludeeData[] occludeesData = ref SceneRenderer.GetOccludeesData(out occludeesCount);
		ref int[] visibleOccludees = ref SceneRenderer.VisibleOccludees;
		Action drawOccluders = DrawOccluders;
		RenderTarget previousZBuffer = (_useOcclusionCullingReprojection ? Engine.Graphics.RTStore.LinearZHalfRes : null);
		RenderTarget.Target previousZBufferTarget = RenderTarget.Target.Color0;
		MapModule.GetBlocksRemovedThisFrame(SceneRenderer.PreviousData.CameraPosition, SceneRenderer.Data.CameraPosition, SceneRenderer.Data.RelativeViewFrustum, 16f, out var blocksCount, out var blocksPositionFromCamera);
		int maxInvalidScreenAreasForReprojection = Engine.OcclusionCulling.MaxInvalidScreenAreasForReprojection;
		int num = System.Math.Min(maxInvalidScreenAreasForReprojection, blocksCount);
		Vector3 one = Vector3.One;
		for (int i = 0; i < num; i++)
		{
			MathHelper.ComputeScreenArea(blocksPositionFromCamera[i], one, ref SceneRenderer.PreviousData.ViewRotationProjectionMatrix, out _previousFrameInvalidScreenAreas[i]);
		}
		Vector4[] previousFrameInvalidScreenAreas = ((num == 0) ? null : _previousFrameInvalidScreenAreas);
		Engine.OcclusionCulling.Update(ref SceneRenderer.Data.ViewRotationProjectionMatrix, FrameTime, SceneRenderer.IsSpatialContinuityLost(), drawOccluders, ref SceneRenderer.Data.ReprojectFromPreviousViewToCurrentProjection, ref SceneRenderer.PreviousData.ProjectionMatrix, previousZBuffer, previousZBufferTarget, previousFrameInvalidScreenAreas, num, _useOcclusionCullingReprojectionHoleFilling, ref occludeesData, occludeesCount, ref visibleOccludees);
	}

	public void Resize(int width, int height)
	{
		Engine.Graphics.RTStore.Resize(width, height, ResolutionScale);
		ProfilingModule.Resize(width, height);
		int width2 = (int)((float)width * ResolutionScale);
		int height2 = (int)((float)height * ResolutionScale);
		SceneRenderer.Resize(width2, height2);
		PostEffectRenderer.Resize(width, height, ResolutionScale);
		InterfaceRenderPreviewModule.Resize(width, height);
		WorldMapModule.Resize(width, height);
		DamageEffectModule.Resize(width, height);
		EditorWebViewModule.OnWindowSizeChanged();
	}

	private void UpdateDynamicLights()
	{
		GlobalLightDataCount = 0;
		int entityLightCount = EntityStoreModule.EntityLightCount;
		bool useOcclusionCullingForLights = Engine.OcclusionCulling.IsActive && UseOcclusionCullingForLights;
		EntityStoreModule.GatherLights(ref SceneRenderer.Data.ViewFrustum, useOcclusionCullingForLights, 1024, ref GlobalLightData, out GlobalLightDataCount);
		SceneRenderer.PrepareLights(GlobalLightData, GlobalLightDataCount);
	}

	private void UpdateAtmosphericData()
	{
		_isCameraUnderwater = WeatherModule.IsUnderWater;
		float num = SceneRenderer.Data.Time * 0.035f;
		float num2 = (0f - WeatherModule.SkyRenderer.CloudOffsets[0]) * _cloudsShadowsSpeed;
		float cloudsShadowIntensity = MathHelper.Lerp(0f, _cloudsShadowsIntensity, WeatherModule.CloudsTransitionOpacity);
		_projectionTexture = (_isCameraUnderwater ? _waterCaustics.GLTexture : WeatherModule.SkyRenderer.CloudsTextures[0]);
		Vector4 sunLightColor = new Vector4(WeatherModule.SunlightColor.X, WeatherModule.SunlightColor.Y, WeatherModule.SunlightColor.Z, WeatherModule.SunLight);
		float num3 = 1f - System.Math.Abs(WeatherModule.NormalizedSunPosition.Y);
		num3 = (float)System.Math.Pow(num3, 2.5);
		Vector3 vector = (_isCameraUnderwater ? WeatherModule.FogColor : new Vector3(WeatherModule.SkyTopGradientColor.X, WeatherModule.SkyTopGradientColor.Y, WeatherModule.SkyTopGradientColor.Z));
		Vector3 vector2 = Vector3.Lerp(WeatherModule.FogColor, new Vector3(WeatherModule.SunsetColor.X, WeatherModule.SunsetColor.Y, WeatherModule.SunsetColor.Z), num3);
		Vector3 fogColor = WeatherModule.FogColor;
		float y = ((WeatherModule.ActiveFogMode == WeatherModule.FogMode.Off) ? 0f : WeatherModule.LerpFogEnd);
		float w = ((WeatherModule.ActiveFogMode == WeatherModule.FogMode.Off || !_isCameraUnderwater) ? 0f : WeatherModule.FogDepthFalloff);
		float z = ((WeatherModule.ActiveFogMode == WeatherModule.FogMode.Off || !_isCameraUnderwater) ? 0f : WeatherModule.FogDepthStart);
		Vector4 fogParams = new Vector4(WeatherModule.LerpFogStart, y, z, w);
		float num4 = 1f;
		float num5 = 1f;
		if (_useCustomMoodFog)
		{
			float num6 = _customHeightFalloff - 1.5f;
			float num7 = _customDensity * num6;
			float num8 = (float)System.Math.Exp(num7) - 1f;
			float num9 = (float)System.Math.Exp(SceneRenderer.Data.FogDensityUnderwater) - 1f;
			num5 = (_isCameraUnderwater ? num9 : num8);
			num4 = (_isCameraUnderwater ? (SceneRenderer.Data.FogHeightFalloffUnderwater * 0.01f) : (_customHeightFalloff * 0.01f));
		}
		else
		{
			float num10 = WeatherModule.FogHeightFalloff - 1.5f;
			float num11 = WeatherModule.FogDensity * num10;
			num4 = (_isCameraUnderwater ? (SceneRenderer.Data.FogHeightFalloffUnderwater * 0.01f) : (WeatherModule.FogHeightFalloff * 0.01f));
			float num12 = (float)System.Math.Exp(num11) - 1f;
			num5 = ((!_isCameraUnderwater && _useMoodFog) ? num12 : 0f);
		}
		float num13 = (_isCameraUnderwater ? num : num2);
		float num14 = ((num13 == 0f) ? (SceneRenderer.Data.Time * 0.01f) : num13);
		num14 *= _fogSpeedFactor;
		Vector4 fogMoodParams = new Vector4(num4, num5, num14, _densityVariationScale);
		float fogHeightDensityAtViewer = (float)System.Math.Exp((0f - num4) * SceneRenderer.Data.CameraPosition.Y);
		Vector3 ambientFrontColor = Vector3.Lerp(vector, vector2, num3);
		Vector3 ambientBackColor = Vector3.Lerp(vector, fogColor, num3);
		float amount = 1f;
		if (UseLessSkyAmbientAtNoon)
		{
			amount = ((WeatherModule.NormalizedSunPosition.Y < 0f) ? 1f : num3);
		}
		float ambientIntensity = MathHelper.Lerp(SkyAmbientIntensityAtNoon, SkyAmbientIntensity, amount);
		if (WeatherModule.IsUnderWater)
		{
			vector2 = fogColor;
		}
		SceneRenderer.UpdateAtmosphericData(_isCameraUnderwater, WeatherModule.SunColor, sunLightColor, WeatherModule.NormalizedSunPosition, vector, vector2, fogColor, fogParams, fogMoodParams, fogHeightDensityAtViewer, ambientFrontColor, ambientBackColor, ambientIntensity, num, _underwaterCausticsDistortion, _underwaterCausticsScale, _underwaterCausticsIntensity, num2, _cloudsShadowsBlurriness, _cloudsShadowsScale, cloudsShadowIntensity);
	}

	private void UpdateRenderData()
	{
		SceneRenderer.UpdateProjectionMatrix(ActiveFieldOfView, Engine.Window.AspectRatio, PostEffectRenderer.NeedsJittering);
		SceneRenderer.UpdateRenderData(CameraModule.Controller.Rotation, CameraModule.Controller.Position, LocalPlayer.RenderPosition, FrameCounter, RenderTimePaused ? 0f : FrameTime, DeltaTime);
		_cameraSceneView.Frustum = SceneRenderer.Data.ViewFrustum;
		_cameraSceneView.Position = SceneRenderer.Data.CameraPosition;
		_cameraSceneView.Direction = SceneRenderer.Data.CameraDirection;
		if (SceneRenderer.IsSunShadowMappingEnabled)
		{
			SceneRenderer.UpdateSunShadowRenderData();
			_sunSceneView.Position = SceneRenderer.Data.SunShadowRenderData.VirtualSunPosition;
			_sunSceneView.Direction = SceneRenderer.Data.SunShadowRenderData.VirtualSunDirection;
			_sunSceneView.Frustum = SceneRenderer.Data.SunShadowRenderData.VirtualSunViewFrustum;
			_sunSceneView.KDopFrustum = SceneRenderer.Data.SunShadowRenderData.VirtualSunKDopFrustum;
			_sunSceneView.UseKDopForCulling = SceneRenderer.UseSunShadowsGlobalKDop;
		}
	}

	private void UpdateSceneData()
	{
		bool isSunShadowMappingEnabled = SceneRenderer.IsSunShadowMappingEnabled;
		bool isWorldShadowEnabled = SceneRenderer.IsWorldShadowEnabled;
		UpdateAtmosphericData();
		if (isSunShadowMappingEnabled)
		{
			int height = ChunkHelper.Height;
			SceneRenderer.SetSunShadowsMaxWorldHeight(height);
			if (isWorldShadowEnabled)
			{
				Engine.Profiling.StartMeasure(25);
				MapModule.ProcessFrustumCulling(_sunSceneView);
				MapModule.GatherRenderableChunksForShadowMap(_sunSceneView, CullUndergroundChunkShadowCasters, 2000);
				_sunSceneView.SortChunksByDistance();
				MapModule.PrepareForSunShadowMapDraw(_sunSceneView, SceneRenderer.Data.CameraPosition);
				Engine.Profiling.StopMeasure(25);
			}
			else
			{
				Engine.Profiling.SkipMeasure(25);
			}
		}
		Engine.Profiling.StartMeasure(27);
		SceneView sunSceneView = (isSunShadowMappingEnabled ? _sunSceneView : null);
		EntityStoreModule.ProcessFrustumCulling(_cameraSceneView, sunSceneView);
		EntityStoreModule.GatherRenderableEntities(_cameraSceneView, sunSceneView, SceneRenderer.Data.SunShadowRenderData.VirtualSunDirection, UseAnimationLOD, CullUndergroundEntityShadowCasters, CullSmallEntityShadowCasters);
		_cameraSceneView.SortEntitiesByDistance();
		if (isSunShadowMappingEnabled)
		{
			_sunSceneView.SortEntitiesByDistance();
		}
		bool isFirstPerson = CameraModule.Controller.IsFirstPerson;
		Vector3 renderPosition = LocalPlayer.RenderPosition;
		EntityStoreModule.ExtractClosestEntityPositions(_cameraSceneView, isFirstPerson, renderPosition);
		Engine.Profiling.StopMeasure(27);
		Engine.Profiling.StartMeasure(26);
		MapModule.GatherRenderableAnimatedBlocks(_cameraSceneView, sunSceneView);
		Engine.Profiling.StopMeasure(26);
		_foliageInteractionParams = new Vector3(1.5f, 4f, 0.33f);
		Engine.Profiling.StartMeasure(34);
		UpdateDynamicLights();
		Engine.Profiling.StopMeasure(34);
		Engine.Profiling.StartMeasure(31);
		Engine.AnimationSystem.ProcessAnimationTasks();
		Engine.Profiling.StopMeasure(31);
		Engine.Profiling.StartMeasure(32);
		EntityStoreModule.PrepareForDraw(_cameraSceneView, ref SceneRenderer.Data.ViewMatrix, ref SceneRenderer.Data.ProjectionMatrix, ref SceneRenderer.Data.ViewProjectionMatrix);
		SceneRenderer.SendEntityDataToGPU();
		SceneRenderer.SendModelVFXDataToGPU();
		if (isSunShadowMappingEnabled)
		{
			EntityStoreModule.PrepareForShadowMapDraw(_sunSceneView);
		}
		_atlasSizeFactor0 = new Vector2((float)MapModule.TextureAtlas.Width / 2048f, (float)MapModule.TextureAtlas.Height / 2048f);
		_atlasSizeFactor1 = new Vector2((float)EntityStoreModule.TextureAtlas.Width / 2048f, (float)EntityStoreModule.TextureAtlas.Height / 2048f);
		_atlasSizeFactor2 = new Vector2((float)App.CharacterPartStore.TextureAtlas.Width / 2048f, (float)App.CharacterPartStore.TextureAtlas.Height / 2048f);
		Engine.Profiling.StopMeasure(32);
		if (isSunShadowMappingEnabled)
		{
			Engine.Profiling.StartMeasure(33);
			SceneRenderer.PrepareShadowCastersForDraw();
			SceneRenderer.SendEntityShadowMapDataToGPU();
			Engine.Profiling.StopMeasure(33);
		}
		else
		{
			Engine.Profiling.SkipMeasure(33);
		}
		if (LocalPlayer.FirstPersonViewNeedsDrawing())
		{
			if (!Engine.OcclusionCulling.IsEnabled || !UseLocalPlayerOccluder)
			{
				LocalPlayer.PrepareForDrawInFirstPersonView();
			}
			LocalPlayer.UpdateFirstPersonFX();
		}
		Engine.Profiling.StartMeasure(28);
		EntityStoreModule.ProcessFXUpdateTasks();
		LocalPlayer.PrepareFXForViewSwitch();
		ParticleSystemStoreModule.Update(SceneRenderer.Data.CameraPosition);
		TrailStoreModule.Update(SceneRenderer.Data.CameraPosition);
		Engine.Profiling.StopMeasure(28);
		Engine.Profiling.StartMeasure(29);
		ParticleSystemStoreModule.GatherRenderableSpawners(SceneRenderer.Data.CameraPosition, SceneRenderer.Data.ViewFrustum);
		Engine.Profiling.StopMeasure(29);
		Engine.Profiling.StartMeasure(30);
		Engine.FXSystem.Particles.UpdateSimulation(DeltaTime);
		Engine.FXSystem.Trails.UpdateSimulation(DeltaTime);
		Engine.Profiling.StopMeasure(30);
		if (EntityStoreModule.DebugInfoNeedsDrawing)
		{
			EntityStoreModule.PrepareDebugInfoForDraw(_cameraSceneView, ref SceneRenderer.Data.ViewProjectionMatrix);
		}
		Vector3 horizonPosition = WeatherModule.FluidHorizonPosition - SceneRenderer.Data.CameraPosition;
		WeatherModule.SkyRenderer.PrepareSkyForDraw(ref SceneRenderer.Data.ViewRotationProjectionMatrix);
		WeatherModule.SkyRenderer.PrepareCloudsForDraw(ref SceneRenderer.Data.ViewRotationProjectionMatrix, ref WeatherModule.SkyRotation);
		WeatherModule.SkyRenderer.PrepareHorizonForDraw(ref SceneRenderer.Data.ViewRotationProjectionMatrix, horizonPosition, WeatherModule.FluidHorizonScale);
		if (WeatherModule.SkyRenderer.SunNeedsDrawing(SceneRenderer.Data.SunPositionWS, SceneRenderer.Data.CameraDirection, WeatherModule.SunScale))
		{
			WeatherModule.SkyRenderer.PrepareSunForDraw(ref SceneRenderer.Data.ViewRotationMatrix, ref SceneRenderer.Data.ProjectionMatrix, SceneRenderer.Data.SunPositionWS, WeatherModule.SunScale);
		}
		if (WeatherModule.SkyRenderer.MoonNeedsDrawing(SceneRenderer.Data.SunPositionWS, SceneRenderer.Data.CameraDirection, WeatherModule.MoonScale))
		{
			WeatherModule.SkyRenderer.PrepareMoonForDraw(ref SceneRenderer.Data.ViewRotationMatrix, ref SceneRenderer.Data.ProjectionMatrix, SceneRenderer.Data.SunPositionWS, WeatherModule.MoonScale);
		}
		BuilderToolsModule.SelectionArea.Update();
		if (ImmersiveScreenModule.NeedsDrawing())
		{
			ImmersiveScreenModule.PrepareForDraw(ref SceneRenderer.Data.ViewProjectionMatrix);
		}
		Engine.Profiling.StartMeasure(40);
		Engine.FXSystem.PrepareForDraw(SceneRenderer.Data.CameraPosition);
		UpdateForceField();
		Engine.Profiling.StopMeasure(40);
		if (WorldMapModule.MapNeedsDrawing)
		{
			WorldMapModule.PrepareMapForDraw();
		}
		DamageEffectModule.PrepareForDraw();
		if (ProfilingModule.IsVisible)
		{
			ProfilingModule.PrepareForDraw();
		}
	}

	public void DrawScene()
	{
		RenderTargetStore rTStore = Engine.Graphics.RTStore;
		bool isSunShadowMappingEnabled = SceneRenderer.IsSunShadowMappingEnabled;
		bool isWorldShadowEnabled = SceneRenderer.IsWorldShadowEnabled;
		SceneRenderer.BeginDraw();
		GLFunctions gL = Engine.Graphics.GL;
		gL.Enable(GL.CULL_FACE);
		gL.Disable(GL.BLEND);
		if (_useMoodFog)
		{
			Engine.Profiling.StartMeasure(44);
			SceneRenderer.AnalyzeSunOcclusion();
			Engine.Profiling.StopMeasure(44);
		}
		else
		{
			Engine.Profiling.SkipMeasure(44);
		}
		SceneRenderer.SendSceneDataToGPU();
		Engine.Profiling.StartMeasure(45);
		SceneRenderer.BuildReflectionMips();
		Engine.Profiling.StopMeasure(45);
		gL.Enable(GL.CULL_FACE);
		gL.Enable(GL.DEPTH_TEST);
		gL.DepthFunc(GL.LEQUAL);
		gL.DepthMask(write: true);
		SceneRenderer.SetupEntityShadowMapDataTexture(7u);
		SceneRenderer.SetupModelVFXDataTexture(6u);
		SceneRenderer.SetupEntityDataTexture(5u);
		gL.ActiveTexture(GL.TEXTURE4);
		gL.BindTexture(GL.TEXTURE_2D, _fogNoise.GLTexture);
		gL.ActiveTexture(GL.TEXTURE3);
		gL.BindTexture(GL.TEXTURE_2D, App.CharacterPartStore.CharacterGradientAtlas.GLTexture);
		gL.ActiveTexture(GL.TEXTURE2);
		gL.BindTexture(GL.TEXTURE_2D, App.CharacterPartStore.TextureAtlas.GLTexture);
		gL.ActiveTexture(GL.TEXTURE1);
		gL.BindTexture(GL.TEXTURE_2D, EntityStoreModule.TextureAtlas.GLTexture);
		gL.ActiveTexture(GL.TEXTURE0);
		gL.BindTexture(GL.TEXTURE_2D, MapModule.TextureAtlas.GLTexture);
		if (_renderPassStates[0] && isSunShadowMappingEnabled)
		{
			Engine.Profiling.StartMeasure(46);
			SceneRenderer.BuildShadowMap();
			Engine.Profiling.StopMeasure(46);
		}
		else
		{
			Engine.Profiling.SkipMeasure(46);
		}
		gL.Disable(GL.CULL_FACE);
		rTStore.GBuffer.Bind(clear: true, setupViewport: true);
		float w = SceneRenderer.Data.SunLightColor.W;
		float num = 0f;
		float[] data = new float[4] { 0f, 0f, 0f, 1f };
		float[] data2 = new float[4] { w, w, 1f, num };
		gL.ClearBufferfv(GL.COLOR, 0, data);
		gL.ClearBufferfv(GL.COLOR, 1, data2);
		BeginWireframeMode(WireframePass.OnAll);
		Engine.Graphics.GPUProgramStore.MapChunkNearOpaqueProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
		Engine.Graphics.GPUProgramStore.MapChunkNearAlphaTestedProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
		Engine.Graphics.GPUProgramStore.MapChunkFarOpaqueProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
		Engine.Graphics.GPUProgramStore.MapChunkFarAlphaTestedProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
		Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
		Engine.Graphics.GPUProgramStore.MapBlockAnimatedProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
		Engine.Graphics.GPUProgramStore.BlockyModelProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
		Engine.Graphics.GPUProgramStore.FirstPersonBlockyModelProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
		Engine.Graphics.GPUProgramStore.FirstPersonClippingBlockyModelProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
		Engine.Graphics.GPUProgramStore.BlockyModelDistortionProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
		Engine.Graphics.GPUProgramStore.BlockyModelDitheringProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
		Engine.Graphics.GPUProgramStore.VolumetricSunshaftProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
		BlockyModelProgram blockyModelProgram = Engine.Graphics.GPUProgramStore.BlockyModelProgram;
		Engine.Profiling.StartMeasure(47);
		if (!LocalPlayer.IsFirstPersonClipping())
		{
			gL.Enable(GL.STENCIL_TEST);
		}
		if (_renderPassStates[1] && LocalPlayer.FirstPersonViewNeedsDrawing())
		{
			BeginWireframeMode(WireframePass.OnEntities);
			gL.StencilFunc(GL.ALWAYS, 0, 255u);
			gL.StencilOp(GL.KEEP, GL.KEEP, GL.REPLACE);
			gL.StencilMask(255u);
			gL.ColorMask(red: false, green: false, blue: false, alpha: false);
			gL.DepthMask(write: false);
			BlockyModelProgram blockyModelProgram2 = (LocalPlayer.IsFirstPersonClipping() ? Engine.Graphics.GPUProgramStore.FirstPersonClippingBlockyModelProgram : Engine.Graphics.GPUProgramStore.FirstPersonBlockyModelProgram);
			gL.UseProgram(blockyModelProgram2);
			blockyModelProgram2.ViewProjectionMatrix.SetValue(ref SceneRenderer.Data.FirstPersonProjectionMatrix);
			LocalPlayer.SendFirstPersonViewUniforms(_atlasSizeFactor0, _atlasSizeFactor1, _atlasSizeFactor2);
			LocalPlayer.DrawInFirstPersonView();
			gL.ColorMask(red: true, green: true, blue: true, alpha: true);
			gL.DepthMask(write: true);
			gL.StencilMask(0u);
			gL.StencilFunc(GL.EQUAL, 0, 255u);
			LocalPlayer.DrawInFirstPersonView();
			EndWireframeMode(WireframePass.OnEntities);
		}
		else
		{
			gL.DepthMask(write: true);
			gL.StencilMask(0u);
		}
		Engine.Profiling.StopMeasure(47);
		gL.StencilFunc(GL.NOTEQUAL, 0, 255u);
		MapChunkNearProgram mapChunkNearOpaqueProgram = Engine.Graphics.GPUProgramStore.MapChunkNearOpaqueProgram;
		gL.UseProgram(mapChunkNearOpaqueProgram);
		Engine.Profiling.StartMeasure(48);
		if (_renderPassStates[2])
		{
			BeginWireframeMode(WireframePass.OnMapOpaque);
			SceneRenderer.DrawMapChunksOpaque(nearChunks: true, useOcclusionCulling: false);
			EndWireframeMode(WireframePass.OnMapOpaque);
		}
		bool flag = SceneRenderer.MapBlocksAnimatedNeedDrawing();
		if (_renderPassStates[4] && flag)
		{
			Engine.Profiling.StartMeasure(49);
			MapBlockAnimatedProgram mapBlockAnimatedProgram = Engine.Graphics.GPUProgramStore.MapBlockAnimatedProgram;
			gL.UseProgram(mapBlockAnimatedProgram);
			BeginWireframeMode(WireframePass.OnMapAnim);
			SceneRenderer.DrawMapBlocksAnimated();
			EndWireframeMode(WireframePass.OnMapAnim);
			Engine.Profiling.StopMeasure(49);
		}
		else
		{
			Engine.Profiling.SkipMeasure(49);
		}
		MapChunkNearProgram mapChunkNearAlphaTestedProgram = Engine.Graphics.GPUProgramStore.MapChunkNearAlphaTestedProgram;
		gL.UseProgram(mapChunkNearAlphaTestedProgram);
		Vector3 playerRenderPosition = SceneRenderer.Data.PlayerRenderPosition;
		Vector3[] closestEntityPositions = EntityStoreModule.ClosestEntityPositions;
		mapChunkNearAlphaTestedProgram.FoliageInteractionPositions.SetValue(closestEntityPositions);
		mapChunkNearAlphaTestedProgram.FoliageInteractionParams.SetValue(_foliageInteractionParams);
		if (_renderPassStates[3])
		{
			BeginWireframeMode(WireframePass.OnMapAlphaTested);
			SceneRenderer.DrawMapChunksAlphaTested(nearChunks: true, useOcclusionCulling: false);
			EndWireframeMode(WireframePass.OnMapAlphaTested);
		}
		Engine.Profiling.StopMeasure(48);
		OcclusionCulling occlusionCulling = Engine.OcclusionCulling;
		bool isActive = occlusionCulling.IsActive;
		bool flag2 = isActive;
		bool flag3 = isActive && UseOcclusionCullingForEntities;
		bool flag4 = flag3 && UseOcclusionCullingForEntitiesAnimations;
		bool flag5 = isActive && UseOcclusionCullingForLights;
		bool flag6 = isActive && UseOcclusionCullingForParticles;
		occlusionCulling.FetchVisibleOccludeesFromGPU(ref SceneRenderer.VisibleOccludees);
		Engine.Profiling.StartMeasure(50);
		MapChunkFarProgram mapChunkFarOpaqueProgram = Engine.Graphics.GPUProgramStore.MapChunkFarOpaqueProgram;
		gL.UseProgram(mapChunkFarOpaqueProgram);
		if (_renderPassStates[5])
		{
			BeginWireframeMode(WireframePass.OnMapOpaque);
			SceneRenderer.DrawMapChunksOpaque(nearChunks: false, isActive);
			EndWireframeMode(WireframePass.OnMapOpaque);
		}
		MapChunkFarProgram mapChunkFarAlphaTestedProgram = Engine.Graphics.GPUProgramStore.MapChunkFarAlphaTestedProgram;
		gL.UseProgram(mapChunkFarAlphaTestedProgram);
		if (_renderPassStates[6])
		{
			BeginWireframeMode(WireframePass.OnMapAlphaTested);
			SceneRenderer.DrawMapChunksAlphaTested(nearChunks: false, isActive);
			EndWireframeMode(WireframePass.OnMapAlphaTested);
		}
		Engine.Profiling.StopMeasure(50);
		gL.Enable(GL.CULL_FACE);
		if (_renderPassStates[7])
		{
			Engine.Profiling.StartMeasure(51);
			gL.UseProgram(blockyModelProgram);
			blockyModelProgram.NearScreendoorThreshold.SetValue(0.55f);
			BeginDebugEntitiesZTest();
			BeginWireframeMode(WireframePass.OnEntities);
			blockyModelProgram.AtlasSizeFactor0.SetValue(_atlasSizeFactor0);
			blockyModelProgram.AtlasSizeFactor1.SetValue(_atlasSizeFactor1);
			blockyModelProgram.AtlasSizeFactor2.SetValue(_atlasSizeFactor2);
			SceneRenderer.DrawEntityCharactersAndItems(flag3);
			EndWireframeMode(WireframePass.OnEntities);
			EndDebugEntitiesZTest();
			Engine.Profiling.StopMeasure(51);
		}
		else
		{
			Engine.Profiling.SkipMeasure(51);
		}
		rTStore.GBuffer.Unbind();
		EndWireframeMode(WireframePass.OnAll);
		gL.DepthMask(write: false);
		SceneRenderer.RenderIntermediateBuffers();
		gL.StencilMask(0u);
		gL.StencilFunc(GL.ALWAYS, 0, 255u);
		bool flag7 = false;
		if (isSunShadowMappingEnabled)
		{
			flag7 = SceneRenderer.UseDeferredShadowBlur;
			Engine.Profiling.StartMeasure(56);
			SceneRenderer.DrawDeferredShadow();
			Engine.Profiling.StopMeasure(56);
		}
		else
		{
			Engine.Profiling.SkipMeasure(56);
		}
		if (SceneRenderer.UseSSAO)
		{
			flag7 = flag7 || SceneRenderer.UseSSAOBlur;
			Engine.Profiling.StartMeasure(57);
			SceneRenderer.DrawSSAO();
			Engine.Profiling.StopMeasure(57);
		}
		else
		{
			Engine.Profiling.SkipMeasure(57);
		}
		if (flag7)
		{
			Engine.Profiling.StartMeasure(58);
			SceneRenderer.BlurSSAOAndShadow();
			Engine.Profiling.StopMeasure(58);
		}
		else
		{
			Engine.Profiling.SkipMeasure(58);
		}
		if (_useVolumetricSunshaft)
		{
			Engine.Profiling.StartMeasure(59);
			DrawVolumetricSunshafts();
			Engine.Profiling.StopMeasure(59);
		}
		BasicProgram basicProgram = Engine.Graphics.GPUProgramStore.BasicProgram;
		Engine.Profiling.StartMeasure(60);
		SceneRenderer.DrawLightPass();
		Engine.Profiling.StopMeasure(60);
		Engine.Profiling.StartMeasure(65);
		OrderIndependentTransparency.Method currentMethod = SceneRenderer.OIT.CurrentMethod;
		if (currentMethod == OrderIndependentTransparency.Method.POIT)
		{
			rTStore.FinalSceneColor.Bind(clear: true, setupViewport: true);
		}
		else
		{
			rTStore.SceneColor.Bind(clear: false, setupViewport: true);
		}
		float[] data3 = new float[4];
		gL.ClearBufferfv(GL.COLOR, 0, data3);
		SceneRenderer.ApplyDeferred(_projectionTexture, _fogNoise.GLTexture);
		Engine.Profiling.StopMeasure(65);
		RenderTarget renderTarget = (Engine.Graphics.IsGPULowEnd ? rTStore.LinearZHalfRes : rTStore.LinearZ);
		BeginWireframeMode(WireframePass.OnAll);
		SceneRenderer.ClusteredLighting.SetupLightDataTextures(15u, 16u);
		bool flag8 = (Engine.FXSystem.Particles.HasErosionTasks || Engine.FXSystem.Trails.HasErosionTasks) && _renderPassStates[10];
		gL.Disable(GL.CULL_FACE);
		if (flag8)
		{
			Engine.Profiling.StartMeasure(66);
			gL.DepthMask(write: true);
			SetupVFXTextures(skipLightTextures: true, skipShadowMap: false, skipSceneDepth: false, skipFogNoise: false, skipForceFieldTextures: true);
			ParticleProgram particleErosionProgram = Engine.Graphics.GPUProgramStore.ParticleErosionProgram;
			gL.UseProgram(particleErosionProgram);
			particleErosionProgram.InvTextureAtlasSize.SetValue(1f / (float)FXModule.TextureAtlas.Width, 1f / (float)FXModule.TextureAtlas.Height);
			particleErosionProgram.CurrentInvViewportSize.SetValue(SceneRenderer.Data.InvViewportSize);
			Engine.FXSystem.DrawErosion();
			gL.BindSampler(1u, GLSampler.None);
			gL.DepthMask(write: false);
			Engine.Profiling.StopMeasure(66);
		}
		else
		{
			Engine.Profiling.SkipMeasure(66);
		}
		gL.Enable(GL.BLEND);
		gL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
		gL.ColorMask(red: true, green: true, blue: true, alpha: false);
		if (_renderPassStates[8])
		{
			Engine.Profiling.StartMeasure(67);
			bool flag9 = true;
			bool flag10 = WeatherModule.SkyRenderer.SunNeedsDrawing(SceneRenderer.Data.SunPositionWS, SceneRenderer.Data.CameraDirection, WeatherModule.SunScale);
			bool flag11 = WeatherModule.SkyRenderer.MoonNeedsDrawing(SceneRenderer.Data.SunPositionWS, SceneRenderer.Data.CameraDirection, WeatherModule.MoonScale);
			bool flag12 = WeatherModule.SkyRenderer.StarsNeedDrawing(SceneRenderer.Data.SunPositionWS);
			flag9 = flag9 && !_isCameraUnderwater;
			SkyProgram skyProgram = Engine.Graphics.GPUProgramStore.SkyProgram;
			gL.UseProgram(skyProgram);
			gL.ActiveTexture(GL.TEXTURE4);
			gL.BindTexture(GL.TEXTURE_2D, rTStore.SunOcclusionHistory.GetTexture(RenderTarget.Target.Color0));
			gL.ActiveTexture(GL.TEXTURE0);
			gL.BindTexture(GL.TEXTURE_2D, WeatherModule.SkyRenderer.StarsTexture);
			skyProgram.StarsOpacity.SetValue(WeatherModule.StarsOpacity);
			skyProgram.TopGradientColor.SetValue(WeatherModule.SkyTopGradientColor);
			skyProgram.SunsetColor.SetValue(WeatherModule.SunsetColor);
			skyProgram.FogFrontColor.SetValue(SceneRenderer.Data.FogFrontColor);
			skyProgram.FogBackColor.SetValue(SceneRenderer.Data.FogBackColor);
			if (_useMoodFog)
			{
				skyProgram.FogMoodParams.SetValue(SceneRenderer.Data.FogMoodParams.X, SceneRenderer.Data.FogMoodParams.Y, SceneRenderer.Data.FogHeightDensityAtViewer);
				skyProgram.CameraPosition.SetValue(SceneRenderer.Data.CameraPosition);
			}
			skyProgram.SunPosition.SetValue(SceneRenderer.Data.SunPositionWS);
			float value = MathHelper.Min(WeatherModule.SunScale, 3f);
			float value2 = MathHelper.Min(WeatherModule.MoonScale, 3f);
			skyProgram.SunScale.SetValue(value);
			skyProgram.SunGlowColor.SetValue(WeatherModule.SunGlowColor);
			skyProgram.MoonOpacity.SetValue(WeatherModule.MoonColor.W);
			skyProgram.MoonScale.SetValue(value2);
			skyProgram.MoonGlowColor.SetValue(WeatherModule.MoonGlowColor);
			skyProgram.DrawSkySunMoonStars.SetValue(flag9 ? 1 : 0, flag10 ? 1 : 0, flag11 ? 1 : 0, flag12 ? 1 : 0);
			BeginWireframeMode(WireframePass.OnSky);
			WeatherModule.SkyRenderer.DrawSky();
			EndWireframeMode(WireframePass.OnSky);
			if (!_isCameraUnderwater && (flag10 || flag11))
			{
				gL.UseProgram(basicProgram);
				if (flag10)
				{
					basicProgram.Opacity.SetValue(1f);
					basicProgram.Color.SetValue(WeatherModule.SunColor.X, WeatherModule.SunColor.Y, WeatherModule.SunColor.Z);
					gL.BindTexture(GL.TEXTURE_2D, WeatherModule.SkyRenderer.SunTexture);
					WeatherModule.SkyRenderer.DrawSun();
				}
				if (flag11)
				{
					basicProgram.Opacity.SetValue(WeatherModule.MoonColor.W);
					basicProgram.Color.SetValue(WeatherModule.MoonColor.X, WeatherModule.MoonColor.Y, WeatherModule.MoonColor.Z);
					gL.BindTexture(GL.TEXTURE_2D, WeatherModule.SkyRenderer.MoonTexture);
					WeatherModule.SkyRenderer.DrawMoon();
				}
			}
			if (!_isCameraUnderwater)
			{
				CloudsProgram cloudsProgram = Engine.Graphics.GPUProgramStore.CloudsProgram;
				gL.UseProgram(cloudsProgram);
				cloudsProgram.Colors.SetValue(WeatherModule.SkyRenderer.CloudColors);
				cloudsProgram.UVOffsets.SetValue(WeatherModule.SkyRenderer.CloudOffsets);
				cloudsProgram.UVMotionParams.SetValue(_cloudsUVMotionScale, _cloudsUVMotionStrength);
				cloudsProgram.FogFrontColor.SetValue(SceneRenderer.Data.FogFrontColor);
				cloudsProgram.FogBackColor.SetValue(SceneRenderer.Data.FogBackColor);
				cloudsProgram.SunPosition.SetValue(SceneRenderer.Data.SunPositionWS);
				if (_useMoodFog)
				{
					cloudsProgram.FogMoodParams.SetValue(SceneRenderer.Data.FogMoodParams.X, SceneRenderer.Data.FogMoodParams.Y, SceneRenderer.Data.FogHeightDensityAtViewer);
					cloudsProgram.CameraPosition.SetValue(SceneRenderer.Data.CameraPosition);
				}
				gL.ActiveTexture(GL.TEXTURE5);
				gL.BindTexture(GL.TEXTURE_2D, _flowMap.GLTexture);
				int cloudsTexturesCount = WeatherModule.SkyRenderer.CloudsTexturesCount;
				if (cloudsTexturesCount != 0)
				{
					for (int num2 = cloudsTexturesCount - 1; num2 >= 0; num2--)
					{
						gL.ActiveTexture((GL)(33984 + num2));
						gL.BindTexture(GL.TEXTURE_2D, WeatherModule.SkyRenderer.CloudsTextures[num2]);
					}
					cloudsProgram.CloudsTextureCount.SetValue(cloudsTexturesCount);
					WeatherModule.SkyRenderer.DrawClouds();
				}
			}
			Engine.Profiling.StopMeasure(67);
		}
		else
		{
			Engine.Profiling.SkipMeasure(67);
		}
		if (UseSkyboxTest)
		{
			DrawCubeMapTest();
		}
		RenderTarget renderTarget2 = ((currentMethod == OrderIndependentTransparency.Method.POIT) ? rTStore.FinalSceneColor : rTStore.SceneColor);
		SceneRenderer.BlitSceneColorToHalfRes(renderTarget2, GL.LINEAR, generateMipMap: true);
		if (_renderPassStates[9] && !_useChunksOIT)
		{
			Engine.Profiling.StartMeasure(68);
			gL.Disable(GL.STENCIL_TEST);
			gL.ColorMask(red: true, green: true, blue: true, alpha: true);
			MapChunkAlphaBlendedProgram mapChunkAlphaBlendedProgram = Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram;
			gL.UseProgram(mapChunkAlphaBlendedProgram);
			mapChunkAlphaBlendedProgram.InvTextureAtlasSize.SetValue(1f / (float)MapModule.TextureAtlas.Width, 1f / (float)MapModule.TextureAtlas.Height);
			mapChunkAlphaBlendedProgram.WaterTintColor.SetValue(WeatherModule.WaterTintColor);
			mapChunkAlphaBlendedProgram.WaterQuality.SetValue(_waterQuality);
			mapChunkAlphaBlendedProgram.CurrentInvViewportSize.SetValue(SceneRenderer.Data.InvViewportSize);
			SetupChunkAlphaBlendedTextures(skipLightTextures: true, mapChunkAlphaBlendedProgram.UseForwardSunShadows, skipSceneDepth: false, _useMoodFog);
			if (_waterQuality != 0)
			{
				gL.Disable(GL.BLEND);
				gL.DepthMask(write: true);
			}
			BeginWireframeMode(WireframePass.OnMapAlphaBlend);
			SceneRenderer.DrawMapChunksAlphaBlended(isActive);
			EndWireframeMode(WireframePass.OnMapAlphaBlend);
			if (_waterQuality != 0)
			{
				gL.Enable(GL.BLEND);
				gL.DepthMask(write: false);
			}
			gL.ColorMask(red: true, green: true, blue: true, alpha: false);
			gL.BindSampler(mapChunkAlphaBlendedProgram.TextureUnits.SceneColor, GLSampler.None);
			gL.BindSampler(mapChunkAlphaBlendedProgram.TextureUnits.Refraction, GLSampler.None);
			Engine.Profiling.StopMeasure(68);
		}
		else
		{
			Engine.Profiling.SkipMeasure(68);
		}
		gL.Disable(GL.STENCIL_TEST);
		gL.StencilMask(255u);
		gL.ActiveTexture(GL.TEXTURE0);
		gL.BindTexture(GL.TEXTURE_2D, Engine.Graphics.WhitePixelTexture.GLTexture);
		bool flag13 = WeatherModule.IsUnderWater && WeatherModule.ActiveFogMode != WeatherModule.FogMode.Off;
		if (InteractionModule.TargetBlockOutineNeedsDrawing() || EntityStoreModule.DebugInfoNeedsDrawing || flag13 || BuilderToolsModule.NeedsDrawing() || ImmersiveScreenModule.NeedsDrawing() || MachinimaModule.NeedsDrawing() || InteractionModule.ShowSelectorDebug || DebugDisplayModule.ShouldDraw)
		{
			gL.UseProgram(basicProgram);
			if (InteractionModule.TargetBlockOutineNeedsDrawing())
			{
				basicProgram.Color.SetValue(Engine.Graphics.BlackColor);
				basicProgram.Opacity.SetValue(0.12f);
				InteractionModule.DrawTargetBlockOutline(ref SceneRenderer.Data.CameraPosition, ref SceneRenderer.Data.ViewRotationProjectionMatrix);
				ClientItemBase primaryItem = LocalPlayer.PrimaryItem;
				if (App.Settings.DisplayBlockSubfaces && primaryItem != null)
				{
					int blockId = primaryItem.BlockId;
					ClientBlockType blockType = MapModule.ClientBlockTypes[blockId];
					InteractionModule.DrawTargetBlockSubface(ref SceneRenderer.Data.CameraPosition, ref SceneRenderer.Data.ViewRotationProjectionMatrix, blockType);
				}
			}
			if (InteractionModule.BlockBreakHealth.NeedsDrawing())
			{
				InteractionModule.BlockBreakHealth.Draw();
			}
			if (InteractionModule.ShowSelectorDebug)
			{
				InteractionModule.DrawDebugSelector(Engine.Graphics, gL, ref SceneRenderer.Data.CameraPosition, ref SceneRenderer.Data.ViewRotationProjectionMatrix);
			}
			if (DebugDisplayModule.ShouldDraw)
			{
				DebugDisplayModule.Draw(Engine.Graphics, gL, DeltaTime, ref SceneRenderer.Data.CameraPosition, ref SceneRenderer.Data.ViewRotationProjectionMatrix);
			}
			if (EntityStoreModule.DebugInfoNeedsDrawing)
			{
				SceneRenderer.DrawEntityDebugInfo();
			}
			if (flag13)
			{
				basicProgram.Color.SetValue(WeatherModule.FogColor.X, WeatherModule.FogColor.Y, WeatherModule.FogColor.Z);
				basicProgram.Opacity.SetValue(1f);
				WeatherModule.SkyRenderer.DrawHorizon();
			}
			if (BuilderToolsModule.NeedsDrawing())
			{
				SetupVFXTextures(skipLightTextures: true, skipShadowMap: true, skipSceneDepth: false, skipFogNoise: true, skipForceFieldTextures: false);
				BuilderToolsModule.Draw(ref SceneRenderer.Data.ViewRotationProjectionMatrix);
			}
			if (MachinimaModule.NeedsDrawing())
			{
				gL.DepthMask(write: true);
				MachinimaModule.Draw(ref SceneRenderer.Data.ViewProjectionMatrix);
				gL.DepthMask(write: false);
			}
			if (ImmersiveScreenModule.NeedsDrawing())
			{
				basicProgram.Color.SetValue(Engine.Graphics.WhiteColor);
				basicProgram.Opacity.SetValue(1f);
				ImmersiveScreenModule.Draw();
			}
		}
		renderTarget2.Unbind();
		bool flag14 = Engine.FXSystem.Particles.ParticleSpawnerDrawCount != 0;
		bool flag15 = Engine.FXSystem.Trails.BlendDrawCount != 0;
		bool hasColorTasks = Engine.FXSystem.ForceFields.HasColorTasks;
		flag14 = flag14 && _renderPassStates[10];
		if (flag14 || flag15 || hasColorTasks || currentMethod == OrderIndependentTransparency.Method.POIT || _useChunksOIT)
		{
			Engine.Profiling.StartMeasure(69);
			FXSystem fXSystem = Engine.FXSystem;
			SetupVFXTextures(skipLightTextures: true, skipShadowMap: false, skipSceneDepth: false, skipFogNoise: false, skipForceFieldTextures: false);
			if (_useChunksOIT)
			{
				SetupChunkAlphaBlendedTextures(skipLightTextures: true, skipShadowMap: true, skipSceneDepth: true, skipFogNoise: true);
			}
			Engine.Graphics.GPUProgramStore.ParticleProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
			Engine.Graphics.GPUProgramStore.ForceFieldProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
			Engine.Graphics.GPUProgramStore.BuilderToolProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
			if (currentMethod != 0)
			{
				bool flag16 = fXSystem.ForceFields.HasColorTasks || fXSystem.Particles.HighResDrawCount + fXSystem.Trails.BlendDrawCount > 0;
				bool flag17 = fXSystem.Particles.LowResDrawCount > 0;
				bool hasQuarterResItems = false;
				flag16 = flag16 || _useChunksOIT;
				switch (_oitRes)
				{
				case 1:
					hasQuarterResItems = flag17;
					flag17 = flag16;
					flag16 = false;
					break;
				case 2:
					hasQuarterResItems = flag16 || flag17;
					flag17 = false;
					flag16 = false;
					break;
				}
				SceneRenderer.OIT.Draw(flag16, flag17, hasQuarterResItems);
			}
			else
			{
				SceneRenderer.OIT.SkipInternalMeasures();
				gL.BlendFunc(GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
				if (flag14)
				{
					DrawParticles((int)currentMethod, 0, SceneRenderer.Data.InvViewportSize, lowRes: false, sendDataToGPU: true);
					Engine.FXSystem.DrawTransparencyLowRes();
				}
				if (hasColorTasks)
				{
					DrawForceFields((int)currentMethod, 0, SceneRenderer.Data.ViewportSize, sendDataToGPU: true);
				}
				gL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
			}
			gL.BindSampler(8u, GLSampler.None);
			gL.BindSampler(4u, GLSampler.None);
			gL.BindSampler(3u, GLSampler.None);
			Engine.Profiling.StopMeasure(69);
		}
		else
		{
			Engine.Profiling.SkipMeasure(69);
			SceneRenderer.OIT.SkipInternalMeasures();
		}
		gL.DepthMask(write: true);
		bool flag18 = SceneRenderer.HasVisibleNameplates || BuilderToolsModule.NeedsTextDrawing() || MachinimaModule.TextNeedsDrawing();
		if (_renderPassStates[11] && flag18)
		{
			Engine.Profiling.StartMeasure(75);
			TextProgram textProgram = Engine.Graphics.GPUProgramStore.TextProgram;
			gL.UseProgram(textProgram);
			gL.BindTexture(GL.TEXTURE_2D, App.Fonts.DefaultFontFamily.RegularFont.TextureAtlas.GLTexture);
			textProgram.FogColor.SetValue(WeatherModule.FogColor);
			textProgram.FogParams.SetValue(SceneRenderer.Data.FogParams);
			textProgram.FillThreshold.SetValue(0f);
			textProgram.OutlineThreshold.SetValue(0f);
			textProgram.OutlineBlurThreshold.SetValue(0f);
			textProgram.OutlineOffset.SetValue(Vector2.Zero);
			textProgram.Opacity.SetValue(1f);
			SceneRenderer.DrawEntityNameplates(flag3);
			if (BuilderToolsModule.NeedsTextDrawing())
			{
				BuilderToolsModule.DrawText(ref SceneRenderer.Data.ViewProjectionMatrix);
			}
			if (MachinimaModule.TextNeedsDrawing())
			{
				MachinimaModule.DrawText(ref SceneRenderer.Data.ViewProjectionMatrix);
			}
			Engine.Profiling.StopMeasure(75);
		}
		else
		{
			Engine.Profiling.SkipMeasure(75);
		}
		SceneRenderer.SetupModelVFXDataTexture(6u);
		SceneRenderer.SetupEntityDataTexture(5u);
		gL.ActiveTexture(GL.TEXTURE4);
		gL.BindTexture(GL.TEXTURE_2D, _flowMap.GLTexture);
		gL.ActiveTexture(GL.TEXTURE2);
		gL.BindTexture(GL.TEXTURE_2D, App.CharacterPartStore.TextureAtlas.GLTexture);
		gL.ActiveTexture(GL.TEXTURE1);
		gL.BindTexture(GL.TEXTURE_2D, EntityStoreModule.TextureAtlas.GLTexture);
		gL.ActiveTexture(GL.TEXTURE0);
		gL.BindTexture(GL.TEXTURE_2D, MapModule.TextureAtlas.GLTexture);
		BlockyModelProgram blockyModelDitheringProgram = Engine.Graphics.GPUProgramStore.BlockyModelDitheringProgram;
		gL.UseProgram(blockyModelDitheringProgram);
		SceneRenderer.DrawForwardEntity(_atlasSizeFactor0, _atlasSizeFactor1, _atlasSizeFactor2);
		EndWireframeMode(WireframePass.OnAll);
		gL.StencilMask(255u);
		bool flag19 = flag2 && DebugDrawOccludeeChunks;
		bool flag20 = flag3 && DebugDrawOccludeeEntities;
		bool flag21 = flag5 && DebugDrawOccludeeLights;
		bool flag22 = flag6 && DebugDrawOccludeeParticles;
		if (flag19 || flag20 || flag21 || flag22 || DebugDrawLight || _debugParticleBoundingVolume || Engine.FXSystem.Particles.DebugInfoNeedsDrawing())
		{
			gL.Enable(GL.DEPTH_TEST);
			gL.Disable(GL.BLEND);
			gL.DepthMask(write: false);
			gL.PolygonMode(GL.FRONT_AND_BACK, GL.LINE);
			if (flag19)
			{
				Engine.OcclusionCulling.DebugDrawOccludees(SceneRenderer.ChunkOccludeesOffset, SceneRenderer.ChunkOccludeesCount, ref SceneRenderer.Data.ViewRotationProjectionMatrix, drawCulledOnly: true);
			}
			if (flag20)
			{
				Engine.OcclusionCulling.DebugDrawOccludees(SceneRenderer.EntityOccludeesOffset, SceneRenderer.EntityOccludeesCount, ref SceneRenderer.Data.ViewRotationProjectionMatrix);
			}
			if (flag21)
			{
				Engine.OcclusionCulling.DebugDrawOccludees(SceneRenderer.LightOccludeesOffset, SceneRenderer.LightOccludeesCount, ref SceneRenderer.Data.ViewRotationProjectionMatrix);
			}
			if (flag22)
			{
				Engine.OcclusionCulling.DebugDrawOccludees(SceneRenderer.ParticleOccludeesOffset, SceneRenderer.ParticleOccludeesCount, ref SceneRenderer.Data.ViewRotationProjectionMatrix);
			}
			if (DebugDrawLight)
			{
				SceneRenderer.DebugDrawLights(GlobalLightData, GlobalLightDataCount);
			}
			if (_debugParticleBoundingVolume || Engine.FXSystem.Particles.DebugInfoNeedsDrawing())
			{
				if (!_debugParticleZTestEnabled)
				{
					gL.Disable(GL.DEPTH_TEST);
				}
				gL.UseProgram(basicProgram);
				gL.BindTexture(GL.TEXTURE_2D, Engine.Graphics.WhitePixelTexture.GLTexture);
				Engine.FXSystem.Particles.DrawDebugInfo(ref SceneRenderer.Data.ViewProjectionMatrix);
				Engine.FXSystem.Particles.DrawDebugBoundingVolumes(ref SceneRenderer.Data.CameraPosition, ref SceneRenderer.Data.ViewRotationProjectionMatrix);
			}
			gL.DepthMask(write: true);
			gL.Enable(GL.BLEND);
			gL.PolygonMode(GL.FRONT_AND_BACK, GL.FILL);
		}
		if (SceneRenderer.NeedsDebugDrawShadowRelated)
		{
			SceneRenderer.DebugDrawShadowRelated();
		}
		gL.StencilMask(255u);
		rTStore.SceneColor.Unbind();
		gL.ColorMask(red: true, green: true, blue: true, alpha: true);
		if (PostEffectRenderer.IsDistortionEnabled)
		{
			Engine.Profiling.StartMeasure(76);
			rTStore.Distortion.Bind(clear: false, setupViewport: true);
			gL.DepthMask(write: false);
			gL.BlendEquationSeparate(GL.FUNC_ADD, GL.FUNC_ADD);
			gL.BlendFunc(GL.ONE, GL.ONE);
			gL.ClearColor(0f, 0f, 0f, 0f);
			gL.Clear(GL.COLOR_BUFFER_BIT);
			if (Engine.FXSystem.ForceFields.HasDistortionTasks)
			{
				Engine.FXSystem.ForceFields.DrawDistortion();
			}
			if (SceneRenderer.HasEntityDistortionTask || LocalPlayer.NeedsDistortionDraw)
			{
				gL.Enable(GL.CULL_FACE);
				SceneRenderer.SetupModelVFXDataTexture(6u);
				SceneRenderer.SetupEntityDataTexture(5u);
				gL.ActiveTexture(GL.TEXTURE4);
				gL.BindTexture(GL.TEXTURE_2D, _flowMap.GLTexture);
				gL.ActiveTexture(GL.TEXTURE2);
				gL.BindTexture(GL.TEXTURE_2D, App.CharacterPartStore.TextureAtlas.GLTexture);
				gL.ActiveTexture(GL.TEXTURE1);
				gL.BindTexture(GL.TEXTURE_2D, EntityStoreModule.TextureAtlas.GLTexture);
				gL.ActiveTexture(GL.TEXTURE0);
				gL.BindTexture(GL.TEXTURE_2D, MapModule.TextureAtlas.GLTexture);
				if (SceneRenderer.HasEntityDistortionTask)
				{
					BlockyModelProgram blockyModelDistortionProgram = Engine.Graphics.GPUProgramStore.BlockyModelDistortionProgram;
					gL.UseProgram(blockyModelDistortionProgram);
					blockyModelDistortionProgram.CurrentInvViewportSize.SetValue(rTStore.Distortion.InvResolution);
					SceneRenderer.DrawEntityDistortion();
				}
				if (LocalPlayer.NeedsDistortionDraw)
				{
					BlockyModelProgram firstPersonDistortionBlockyModelProgram = Engine.Graphics.GPUProgramStore.FirstPersonDistortionBlockyModelProgram;
					gL.UseProgram(firstPersonDistortionBlockyModelProgram);
					firstPersonDistortionBlockyModelProgram.ViewMatrix.SetValue(ref SceneRenderer.Data.FirstPersonViewMatrix);
					firstPersonDistortionBlockyModelProgram.ViewProjectionMatrix.SetValue(ref SceneRenderer.Data.FirstPersonProjectionMatrix);
					LocalPlayer.DrawDistortionInFirstPersonView();
				}
				gL.Disable(GL.CULL_FACE);
			}
			if (Engine.FXSystem.Particles.HasDistortionTasks || Engine.FXSystem.Trails.HasDistortionTasks)
			{
				Engine.FXSystem.SetupDrawDataTexture(11u);
				gL.ActiveTexture(GL.TEXTURE7);
				gL.BindTexture(GL.TEXTURE_2D, FXModule.TextureAtlas.GLTexture);
				gL.ActiveTexture(GL.TEXTURE0);
				ParticleProgram particleDistortionProgram = Engine.Graphics.GPUProgramStore.ParticleDistortionProgram;
				gL.UseProgram(particleDistortionProgram);
				particleDistortionProgram.InvTextureAtlasSize.SetValue(1f / (float)FXModule.TextureAtlas.Width, 1f / (float)FXModule.TextureAtlas.Height);
				particleDistortionProgram.CurrentInvViewportSize.SetValue(rTStore.Distortion.InvResolution);
				Engine.FXSystem.DrawDistortion();
			}
			gL.ClearColor(0f, 0f, 0f, 1f);
			gL.DepthMask(write: true);
			gL.BlendEquationSeparate(GL.FUNC_ADD, GL.FUNC_ADD);
			gL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
			rTStore.Distortion.Unbind();
			Engine.Profiling.StopMeasure(76);
		}
		else
		{
			Engine.Profiling.SkipMeasure(76);
		}
		if (_debugParticleOverdraw)
		{
			gL.AssertEnabled(GL.BLEND);
			rTStore.DebugFXOverdraw.Bind(clear: true, setupViewport: true);
			if (Engine.FXSystem.Particles.ParticleSpawnerDrawCount != 0)
			{
				gL.DepthMask(write: false);
				gL.BlendFunc(GL.ONE, GL.ONE);
				ParticleProgram particleProgram = Engine.Graphics.GPUProgramStore.ParticleProgram;
				gL.UseProgram(particleProgram);
				particleProgram.DebugOverdraw.SetValue(1);
				DrawParticles(0, 0, rTStore.DebugFXOverdraw.InvResolution, lowRes: false, sendDataToGPU: false);
				Engine.FXSystem.DrawTransparencyLowRes();
				particleProgram.DebugOverdraw.SetValue(0);
				gL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
				gL.DepthMask(write: true);
			}
			rTStore.DebugFXOverdraw.Unbind();
		}
		bool updateEntities = flag4;
		bool updateLights = flag5;
		EntityStoreModule.UpdateVisibilityPrediction(SceneRenderer.VisibleOccludees, SceneRenderer.EntityOccludeesOffset, SceneRenderer.EntityOccludeesCount, SceneRenderer.LightOccludeesOffset, SceneRenderer.LightOccludeesCount, updateEntities, updateLights);
		bool updateParticles = flag6;
		ParticleSystemStoreModule.UpdateVisibilityPrediction(SceneRenderer.VisibleOccludees, SceneRenderer.ParticleOccludeesOffset, SceneRenderer.ParticleOccludeesCount, updateParticles);
	}

	private void DrawAlphaBlendedMapChunks(int methodId, int extra, Vector2 invViewportSize, bool lowRes, bool sendDataToGPU)
	{
		GLFunctions gL = Engine.Graphics.GL;
		RenderTargetStore rTStore = Engine.Graphics.RTStore;
		if (!_debugParticleZTestEnabled)
		{
			gL.Disable(GL.DEPTH_TEST);
		}
		MapChunkAlphaBlendedProgram mapChunkAlphaBlendedProgram = Engine.Graphics.GPUProgramStore.MapChunkAlphaBlendedProgram;
		gL.UseProgram(mapChunkAlphaBlendedProgram);
		mapChunkAlphaBlendedProgram.CurrentInvViewportSize.SetValue(invViewportSize);
		mapChunkAlphaBlendedProgram.OITParams.SetValue(methodId, extra);
		mapChunkAlphaBlendedProgram.InvTextureAtlasSize.SetValue(1f / (float)MapModule.TextureAtlas.Width, 1f / (float)MapModule.TextureAtlas.Height);
		mapChunkAlphaBlendedProgram.WaterTintColor.SetValue(WeatherModule.WaterTintColor);
		mapChunkAlphaBlendedProgram.WaterQuality.SetValue(_waterQuality);
		SceneRenderer.DrawMapChunksAlphaBlended(useOcclusionCulling: true);
		if (!_debugParticleZTestEnabled)
		{
			gL.Enable(GL.DEPTH_TEST);
		}
	}

	private void DrawParticles(int methodId, int extra, Vector2 invViewportSize, bool lowRes, bool sendDataToGPU)
	{
		GLFunctions gL = Engine.Graphics.GL;
		if (!_debugParticleZTestEnabled)
		{
			gL.Disable(GL.DEPTH_TEST);
		}
		ParticleProgram particleProgram = Engine.Graphics.GPUProgramStore.ParticleProgram;
		gL.UseProgram(particleProgram);
		particleProgram.InvTextureAtlasSize.SetValue(1f / (float)FXModule.TextureAtlas.Width, 1f / (float)FXModule.TextureAtlas.Height);
		particleProgram.CurrentInvViewportSize.SetValue(invViewportSize);
		particleProgram.OITParams.SetValue(methodId, extra);
		if (lowRes)
		{
			Engine.FXSystem.DrawTransparencyLowRes();
		}
		else
		{
			Engine.FXSystem.DrawTransparency();
		}
		if (!_debugParticleZTestEnabled)
		{
			gL.Enable(GL.DEPTH_TEST);
		}
	}

	private void DrawForceFields(int methodId, int extra, Vector2 invViewportSize, bool sendDataToGPU)
	{
		GLFunctions gL = Engine.Graphics.GL;
		ForceFieldProgram forceFieldProgram = Engine.Graphics.GPUProgramStore.ForceFieldProgram;
		gL.UseProgram(forceFieldProgram);
		forceFieldProgram.DrawAndBlendMode.SetValue(forceFieldProgram.DrawModeColor, forceFieldProgram.BlendModePremultLinear);
		forceFieldProgram.CurrentInvViewportSize.SetValue(invViewportSize);
		forceFieldProgram.OITParams.SetValue(methodId, extra);
		Engine.FXSystem.ForceFields.DrawColor(sendDataToGPU);
	}

	private void DrawTransparents(int methodId, int extra, Vector2 invViewportSize, bool sendDataToGPU)
	{
		DrawTransparentsFullRes(methodId, extra, invViewportSize, sendDataToGPU);
		DrawTransparentsHalfRes(methodId, extra, invViewportSize, sendDataToGPU);
	}

	private void DrawTransparentsFullRes(int methodId, int extra, Vector2 invViewportSize, bool sendDataToGPU)
	{
		if (_useChunksOIT)
		{
			DrawAlphaBlendedMapChunks(methodId, extra, invViewportSize, lowRes: false, sendDataToGPU);
		}
		if (Engine.FXSystem.Particles.HighResDrawCount > 0 || Engine.FXSystem.Trails.BlendDrawCount > 0)
		{
			DrawParticles(methodId, extra, invViewportSize, lowRes: false, sendDataToGPU);
		}
		if (Engine.FXSystem.ForceFields.HasColorTasks)
		{
			DrawForceFields(methodId, extra, invViewportSize, sendDataToGPU);
		}
	}

	private void DrawTransparentsHalfRes(int methodId, int extra, Vector2 invViewportSize, bool sendDataToGPU)
	{
		if (Engine.FXSystem.Particles.LowResDrawCount > 0)
		{
			DrawParticles(methodId, extra, invViewportSize, lowRes: true, sendDataToGPU);
		}
	}

	public void DrawVolumetricSunshafts()
	{
		GLFunctions gL = Engine.Graphics.GL;
		RenderTargetStore rTStore = Engine.Graphics.RTStore;
		rTStore.VolumetricSunshaft.Bind(clear: true, setupViewport: true);
		VolumetricSunshaftProgram volumetricSunshaftProgram = Engine.Graphics.GPUProgramStore.VolumetricSunshaftProgram;
		volumetricSunshaftProgram.SceneDataBlock.SetBuffer(SceneRenderer.SceneDataBuffer);
		gL.UseProgram(volumetricSunshaftProgram);
		gL.ActiveTexture(GL.TEXTURE2);
		gL.BindTexture(GL.TEXTURE_2D, rTStore.GBuffer.GetTexture(RenderTarget.Target.Color0));
		gL.ActiveTexture(GL.TEXTURE1);
		gL.BindTexture(GL.TEXTURE_2D, rTStore.ShadowMap.GetTexture(RenderTarget.Target.Depth));
		gL.ActiveTexture(GL.TEXTURE0);
		gL.BindTexture(GL.TEXTURE_2D, rTStore.LinearZHalfRes.GetTexture(RenderTarget.Target.Color0));
		volumetricSunshaftProgram.FarCorners.SetValue(SceneRenderer.Data.FrustumFarCornersWS);
		float num = SceneRenderer.Data.SunPositionWS.Y + 0.2f;
		if (num > 0f)
		{
			Vector4 value = new Vector4(WeatherModule.SunGlowColor.X, WeatherModule.SunGlowColor.Y, WeatherModule.SunGlowColor.Z, 1f);
			volumetricSunshaftProgram.SunColor.SetValue(value);
		}
		else
		{
			Vector4 value2 = new Vector4(WeatherModule.MoonGlowColor.X, WeatherModule.MoonGlowColor.Y, WeatherModule.MoonGlowColor.Z, 0.25f);
			volumetricSunshaftProgram.SunColor.SetValue(value2);
		}
		volumetricSunshaftProgram.SunDirection.SetValue(SceneRenderer.Data.SunShadowRenderData.VirtualSunDirection);
		Engine.Graphics.ScreenTriangleRenderer.Draw();
		rTStore.VolumetricSunshaft.Unbind();
		BlurProgram blurProgram = Engine.Graphics.GPUProgramStore.BlurProgram;
		gL.UseProgram(blurProgram);
		rTStore.BlurXResBy2.Bind(clear: false, setupViewport: true);
		gL.BindTexture(GL.TEXTURE_2D, rTStore.VolumetricSunshaft.GetTexture(RenderTarget.Target.Color0));
		blurProgram.PixelSize.SetValue(1f / (float)rTStore.BlurXResBy2.Width, 1f / (float)rTStore.BlurXResBy2.Height);
		blurProgram.BlurScale.SetValue(1f);
		blurProgram.HorizontalPass.SetValue(1f);
		Engine.Graphics.ScreenTriangleRenderer.DrawRaw();
		rTStore.BlurXResBy2.Unbind();
		rTStore.VolumetricSunshaft.Bind(clear: false, setupViewport: false);
		gL.BindTexture(GL.TEXTURE_2D, rTStore.BlurXResBy2.GetTexture(RenderTarget.Target.Color0));
		blurProgram.HorizontalPass.SetValue(0f);
		Engine.Graphics.ScreenTriangleRenderer.DrawRaw();
		rTStore.VolumetricSunshaft.Unbind();
	}

	public void DrawPostEffect()
	{
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		if (!IsPlaying)
		{
			return;
		}
		RenderTargetStore rTStore = Engine.Graphics.RTStore;
		Engine.Profiling.StartMeasure(77);
		float time = (float)_stopwatchSinceJoiningServer.ElapsedMilliseconds / 1000f;
		bool isUnderWater = WeatherModule.IsUnderWater;
		float distortionAmplitude = 0f;
		float distortionFrequency = 0f;
		float colorSaturation = 1f;
		Vector3 colorFilter = new Vector3(WeatherModule.ColorFilter.X, WeatherModule.ColorFilter.Y, WeatherModule.ColorFilter.Z);
		if (isUnderWater)
		{
			FluidFX fluidFX = WeatherModule.FluidFX;
			Vector3 vector = (((int)fluidFX.FogMode == 0) ? Vector3.One : WeatherModule.FluidBlockLightColor);
			distortionAmplitude = fluidFX.DistortionAmplitude;
			distortionFrequency = fluidFX.DistortionFrequency;
			colorSaturation = fluidFX.ColorSaturation;
			colorFilter = new Vector3((float)(int)(byte)fluidFX.ColorFilter.Red / 255f * vector.X, (float)(int)(byte)fluidFX.ColorFilter.Green / 255f * vector.Y, (float)(int)(byte)fluidFX.ColorFilter.Blue / 255f * vector.Z);
		}
		PostEffectRenderer.UpdateDistortion(time, distortionAmplitude, distortionFrequency);
		PostEffectRenderer.UpdateColorFilters(colorFilter, colorSaturation);
		if (PostEffectRenderer.IsBloomEnabled)
		{
			if (isUnderWater && UseBloomUnderwater)
			{
				PostEffectRenderer.SetBloomOnPowIntensity(UnderwaterBloomIntensity);
				PostEffectRenderer.SetBloomOnPowPower(UnderwaterBloomPower);
			}
			else
			{
				PostEffectRenderer.SetBloomOnPowIntensity(DefaultBloomIntensity);
				PostEffectRenderer.SetBloomOnPowPower(DefaultBloomPower);
			}
			bool allowBloom = !isUnderWater || UseBloomUnderwater;
			bool isSunVisible = WeatherModule.SkyRenderer.SunNeedsDrawing(SceneRenderer.Data.SunPositionWS, SceneRenderer.Data.CameraDirection, WeatherModule.SunScale);
			bool isMoonVisible = WeatherModule.SkyRenderer.MoonNeedsDrawing(SceneRenderer.Data.SunPositionWS, SceneRenderer.Data.CameraDirection, WeatherModule.MoonScale);
			PostEffectRenderer.UpdateBloom(WeatherModule.SkyRenderer.SunMVPMatrix, isSunVisible, allowBloom, SceneRenderer.Data.SunColor, isMoonVisible, WeatherModule.MoonColor, SceneRenderer.Data.Time);
		}
		if (PostEffectRenderer.IsTemporalAAEnabled)
		{
			PostEffectRenderer.UpdateTemporalAA(SceneRenderer.Data.HasCameraMoved);
		}
		if (PostEffectRenderer.IsDepthOfFieldEnabled)
		{
			PostEffectRenderer.UpdateDepthOfField(SceneRenderer.Data.ProjectionMatrix);
		}
		PostEffectRenderer.Draw(rTStore.SceneColor.GetTexture(RenderTarget.Target.Color0), rTStore.Distortion.GetTexture(RenderTarget.Target.Color0), Engine.Window.Viewport.Width, Engine.Window.Viewport.Height, ResolutionScale);
		Engine.Profiling.StopMeasure(77);
	}

	public void DrawAfterPostEffect()
	{
		if (IsPlaying)
		{
			GLFunctions gL = Engine.Graphics.GL;
			BasicProgram basicProgram = Engine.Graphics.GPUProgramStore.BasicProgram;
			gL.UseProgram(basicProgram);
			Engine.Profiling.StartMeasure(83);
			if (DamageEffectModule.NeedsDrawing())
			{
				basicProgram.Color.SetValue(Engine.Graphics.WhiteColor);
				DamageEffectModule.Draw();
			}
			if (ScreenEffectStoreModule.NeedsDrawing())
			{
				ScreenEffectStoreModule.Draw();
			}
			Engine.Profiling.StopMeasure(83);
			if (WorldMapModule.MapNeedsDrawing)
			{
				WorldMapModule.DrawMap();
			}
			TextProgram textProgram = Engine.Graphics.GPUProgramStore.TextProgram;
			if (ProfilingModule.IsVisible)
			{
				gL.UseProgram(textProgram);
				textProgram.FillThreshold.SetValue(0f);
				textProgram.OutlineThreshold.SetValue(0f);
				textProgram.OutlineBlurThreshold.SetValue(0f);
				textProgram.OutlineOffset.SetValue(Vector2.Zero);
				textProgram.FogParams.SetValue(Vector4.Zero);
			}
			RenderTargetStore rTStore = Engine.Graphics.RTStore;
			if (DrawOcclusionMap)
			{
				float opacity = (float)_debugDrawMapOpacityStep / 5f;
				Engine.OcclusionCulling.DebugDrawOcclusionMap(opacity, _debugDrawMapLevel);
			}
			else if (DebugMap)
			{
				UpdateDebugDrawMap();
				float opacity2 = (float)_debugDrawMapOpacityStep / 5f;
				rTStore.DebugDrawMaps(_activeDebugMapsNames, _debugMapVerticalDisplay, opacity2, _debugDrawMapLevel, _debugTextureArrayActiveLayer);
			}
			if (ProfilingModule.IsVisible)
			{
				gL.UseProgram(basicProgram);
				gL.BindTexture(GL.TEXTURE_2D, Engine.Graphics.WhitePixelTexture.GLTexture);
				basicProgram.Opacity.SetValue(0.8f);
				ProfilingModule.DrawGraphsData();
				gL.UseProgram(Engine.Graphics.GPUProgramStore.Batcher2DProgram);
				ProfilingModule.Draw();
			}
		}
	}

	private void UpdateInterfaceData()
	{
		App.Interface.PrepareForDraw();
		InterfaceRenderPreviewModule.PrepareForDraw();
	}

	public void DrawAfterInterface()
	{
		if (EditorWebViewModule.NeedsDrawing())
		{
			EditorWebViewModule.Draw();
		}
		if (InterfaceRenderPreviewModule.NeedsDrawing())
		{
			InterfaceRenderPreviewModule.Draw();
		}
	}

	public void RegisterCommand(string command, Command method)
	{
		_commands.Add(command, method);
	}

	public void UnregisterCommand(string command)
	{
		_commands.Remove(command);
	}

	public bool IsRegisteredCommand(string command)
	{
		return _commands.ContainsKey(command);
	}

	public void ExecuteCommand(string str)
	{
		string[] array = str.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (array[0].StartsWith(".."))
		{
			array[0] = array[0].Substring("..".Length);
			ShortcutsModule.ExecuteMacro(array);
			return;
		}
		array[0] = array[0].Substring(".".Length);
		if (array[0].Length == 0)
		{
			Chat.Log("Please enter a command after the . symbol");
			return;
		}
		if (!_commands.TryGetValue(array[0], out var value))
		{
			Chat.Log("Unknown local command! '" + str + "'");
			return;
		}
		string[] array2 = new string[array.Length - 1];
		Array.Copy(array, 1, array2, 0, array.Length - 1);
		try
		{
			value(array2);
		}
		catch (InvalidCommandUsage invalidCommandUsage)
		{
			Chat.Log("Invalid usage!");
			object[] customAttributes = invalidCommandUsage.TargetSite.GetCustomAttributes(typeof(UsageAttribute), inherit: false);
			UsageAttribute usageAttribute = ((customAttributes.Length != 0) ? ((UsageAttribute)customAttributes[0]) : ((UsageAttribute)Attribute.GetCustomAttribute(value.Method, typeof(UsageAttribute))));
			if (usageAttribute != null)
			{
				Chat.Log(usageAttribute.ToString());
			}
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Exception running command '{0}':", new object[1] { str });
			Chat.Log("Exception running command! '" + str + "': " + ex.Message);
		}
	}

	public string GetCommandDescription(string commandName)
	{
		string result = "N/A";
		if (_commands.TryGetValue(commandName, out var value))
		{
			DescriptionAttribute descriptionAttribute = (DescriptionAttribute)Attribute.GetCustomAttribute(value.Method, typeof(DescriptionAttribute));
			if (descriptionAttribute != null)
			{
				result = descriptionAttribute.Description;
			}
		}
		return result;
	}

	[Usage("help", new string[] { "[command]" })]
	[Description("Provides help for commands.")]
	public void HelpCommand(string[] args)
	{
		Command value;
		if (args.Length == 0)
		{
			List<string> list = new List<string>();
			foreach (string key in _commands.Keys)
			{
				list.Add("- " + key + ": " + GetCommandDescription(key));
			}
			list.Sort();
			string message = "Available commands:\n" + string.Join("\n", list);
			Chat.Log(message);
		}
		else if (!_commands.TryGetValue(args[0], out value))
		{
			Chat.Log("Unknown local command! '" + args[0] + "'");
		}
		else
		{
			UsageAttribute usageAttribute = (UsageAttribute)Attribute.GetCustomAttribute(value.Method, typeof(UsageAttribute));
			Chat.Log(usageAttribute?.ToString() ?? ("No usage info found for command '" + args[0] + "'"));
		}
	}

	private void InitModules()
	{
		TimeModule = AddModule(new TimeModule(this));
		AudioModule = new AudioModule(this);
		MapModule = AddModule(new MapModule(this));
		ItemLibraryModule = AddModule(new ItemLibraryModule(this));
		EditorWebViewModule = AddModule(new EditorWebViewModule(this));
		CharacterControllerModule = AddModule(new CharacterControllerModule(this));
		CameraModule = AddModule(new CameraModule(this));
		EntityStoreModule = AddModule(new EntityStoreModule(this));
		CollisionModule = AddModule(new CollisionModule(this));
		_networkModule = AddModule(new NetworkModule(this));
		_movementSoundModule = AddModule(new MovementSoundModule(this));
		InventoryModule = AddModule(new InventoryModule(this));
		InteractionModule = AddModule(new InteractionModule(this));
		BuilderToolsModule = AddModule(new BuilderToolsModule(this));
		MachinimaModule = AddModule(new MachinimaModule(this));
		FXModule = AddModule(new FXModule(this));
		TrailStoreModule = AddModule(new TrailStoreModule(this));
		WeatherModule = AddModule(new WeatherModule(this));
		ScreenEffectStoreModule = AddModule(new ScreenEffectStoreModule(this));
		ParticleSystemStoreModule = AddModule(new ParticleSystemStoreModule(this));
		AmbienceFXModule = AddModule(new AmbienceFXModule(this));
		DamageEffectModule = AddModule(new DamageEffectModule(this));
		ClientFeatureModule = AddModule(new ClientFeatureModule(this));
		_autoCameraModule = AddModule(new AutoCameraModule(this));
		_debugCommandsModule = AddModule(new DebugCommandsModule(this));
		ProfilingModule = AddModule(new ProfilingModule(this));
		ShortcutsModule = AddModule(new ShortcutsModule(this));
		ImmersiveScreenModule = AddModule(new ImmersiveScreenModule(this));
		InterfaceRenderPreviewModule = AddModule(new InterfaceRenderPreviewModule(this));
		WorldMapModule = AddModule(new WorldMapModule(this));
		DebugDisplayModule = AddModule(new DebugDisplayModule(this));
	}

	public T AddModule<T>(T module) where T : Module
	{
		_modules.Add(module);
		return module;
	}

	private void InitRenderingOptions()
	{
		bool isGPULowEnd = Engine.Graphics.IsGPULowEnd;
		TrailerMode.UseDof = false;
		TrailerMode.DofQuality = 3;
		TrailerMode.UseBloom = true;
		TrailerMode.BloomQuality = 1;
		TrailerMode.UseSunshaft = true;
		TrailerMode.WaterQuality = 3;
		TrailerMode.SsaoQuality = 2;
		TrailerMode.RenderScale = 200;
		TrailerMode.UseFxaaSharpened = true;
		TrailerMode.UseFxaa = true;
		TrailerMode.UseTaa = false;
		TrailerMode.UseFoliageFading = false;
		TrailerMode.UseLod = false;
		TrailerMode.LodDistanceStart = 160u;
		CutscenesMode.UseDof = false;
		CutscenesMode.DofQuality = (isGPULowEnd ? 1 : 3);
		CutscenesMode.UseBloom = true;
		CutscenesMode.BloomQuality = 1;
		CutscenesMode.UseSunshaft = true;
		CutscenesMode.WaterQuality = 3;
		CutscenesMode.SsaoQuality = ((!isGPULowEnd) ? 1 : 0);
		CutscenesMode.RenderScale = 100;
		CutscenesMode.UseFxaaSharpened = true;
		CutscenesMode.UseFxaa = true;
		CutscenesMode.UseTaa = false;
		CutscenesMode.UseFoliageFading = true;
		CutscenesMode.UseLod = true;
		CutscenesMode.LodDistanceStart = 190u;
		IngameMode.UseDof = false;
		IngameMode.DofQuality = (isGPULowEnd ? 1 : 3);
		IngameMode.UseBloom = true;
		IngameMode.BloomQuality = 0;
		IngameMode.UseSunshaft = !isGPULowEnd;
		IngameMode.WaterQuality = (isGPULowEnd ? 2 : 3);
		IngameMode.SsaoQuality = ((!isGPULowEnd) ? 1 : 0);
		IngameMode.RenderScale = (isGPULowEnd ? 70 : 100);
		IngameMode.UseFxaaSharpened = !isGPULowEnd;
		IngameMode.UseFxaa = true;
		IngameMode.UseTaa = isGPULowEnd;
		IngameMode.UseFoliageFading = true;
		IngameMode.UseLod = true;
		IngameMode.LodDistanceStart = 160u;
		LowEndGPUMode.UseDof = false;
		LowEndGPUMode.DofQuality = 1;
		LowEndGPUMode.UseBloom = true;
		LowEndGPUMode.BloomQuality = 0;
		LowEndGPUMode.UseSunshaft = false;
		LowEndGPUMode.WaterQuality = 2;
		LowEndGPUMode.SsaoQuality = 0;
		LowEndGPUMode.RenderScale = 70;
		LowEndGPUMode.UseFxaaSharpened = false;
		LowEndGPUMode.UseFxaa = true;
		LowEndGPUMode.UseTaa = true;
		LowEndGPUMode.UseFoliageFading = true;
		LowEndGPUMode.UseLod = true;
		LowEndGPUMode.LodDistanceStart = 160u;
	}

	public void SetRenderingOptions(ref RenderingOptions renderingOptions)
	{
		PostEffectRenderer.UseDepthOfField(renderingOptions.UseDof);
		PostEffectRenderer.SetDepthOfFieldVersion(renderingOptions.DofQuality);
		PostEffectRenderer.UseBloom(renderingOptions.UseBloom);
		PostEffectRenderer.SetBloomVersion(renderingOptions.BloomQuality);
		PostEffectRenderer.UseBloomSunShaft(renderingOptions.UseSunshaft);
		SetWaterQuality(renderingOptions.WaterQuality);
		SceneRenderer.SetUseSSAO(useSSAO: true, useTemporalFiltering: true, renderingOptions.SsaoQuality);
		float resolutionScale = (float)(App.Settings.AutomaticRenderScale ? renderingOptions.RenderScale : App.Settings.RenderScale) * 0.01f;
		SetResolutionScale(resolutionScale);
		PostEffectRenderer.UseFXAASharpened(renderingOptions.UseFxaaSharpened);
		PostEffectRenderer.UseFXAA(renderingOptions.UseFxaa);
		PostEffectRenderer.UseTemporalAA(renderingOptions.UseTaa);
		SetChunkUseFoliageFading(renderingOptions.UseFoliageFading);
		SetUseLOD(renderingOptions.UseLod);
		SetLODDistance(renderingOptions.LodDistanceStart);
	}

	public void RegisterHashForServerAsset(string serverAssetPath, string hash)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		HashesByServerAssetPath[serverAssetPath] = hash;
	}

	public void RemoveHashForServerAsset(string serverAssetPath)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		HashesByServerAssetPath.TryRemove(serverAssetPath, out var _);
	}
}
