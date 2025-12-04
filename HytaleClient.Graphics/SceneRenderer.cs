#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HytaleClient.Core;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Graphics;

internal class SceneRenderer : Disposable
{
	public struct SceneData
	{
		public float Time;

		public float DeltaTime;

		public uint FrameCounter;

		public float WorldFieldOfView;

		public float AspectRatio;

		public Vector2 ViewportSize;

		public Vector2 InvViewportSize;

		public Matrix ViewRotationMatrix;

		public Matrix ViewRotationProjectionMatrix;

		public Matrix InvViewRotationProjectionMatrix;

		public Matrix InvViewRotationMatrix;

		public Matrix InvViewMatrix;

		public Matrix InvViewProjectionMatrix;

		public Matrix ViewMatrix;

		public Matrix ViewProjectionMatrix;

		public Matrix ProjectionMatrix;

		public Matrix FirstPersonViewMatrix;

		public Matrix FirstPersonProjectionMatrix;

		public Matrix ReprojectFromCurrentViewToPreviousProjectionMatrix;

		public Matrix ReprojectFromPreviousViewToCurrentProjection;

		public Vector2 ProjectionJittering;

		public bool HasCameraMoved;

		public bool IsCameraUnderwater;

		public Vector3 CameraPosition;

		public Vector3 CameraDirection;

		public Vector3 PlayerRenderPosition;

		public BoundingFrustum ViewFrustum;

		public BoundingFrustum RelativeViewFrustum;

		public Vector3[] FrustumFarCornersWS;

		public Vector3[] FrustumFarCornersVS;

		public CascadedShadowMapping.RenderData SunShadowRenderData;

		public Vector3 SunColor;

		public Vector4 SunLightColor;

		public Vector3 SunPositionWS;

		public Vector3 SunPositionVS;

		public Vector3 AmbientFrontColor;

		public Vector3 AmbientBackColor;

		public float AmbientIntensity;

		public Vector3 FogTopColor;

		public Vector3 FogFrontColor;

		public Vector3 FogBackColor;

		public Vector4 FogParams;

		public float FogHeightFalloffUnderwater;

		public float FogDensityUnderwater;

		public Vector4 FogMoodParams;

		public float FogHeightDensityAtViewer;

		public float WaterCausticsAnimTime;

		public float WaterCausticsDistortion;

		public float WaterCausticsScale;

		public float WaterCausticsIntensity;

		public float CloudsShadowAnimTime;

		public float CloudsShadowBlurriness;

		public float CloudsShadowScale;

		public float CloudsShadowIntensity;

		public void Init(int width = 1, int height = 1)
		{
			SetViewportSize(width, height);
			FrameCounter = 0u;
			WorldFieldOfView = (float)System.Math.PI / 2f;
			AspectRatio = 1.3333334f;
			ViewRotationMatrix = Matrix.Identity;
			ViewRotationProjectionMatrix = Matrix.Identity;
			InvViewRotationProjectionMatrix = Matrix.Identity;
			InvViewRotationMatrix = Matrix.Identity;
			InvViewMatrix = Matrix.Identity;
			InvViewProjectionMatrix = Matrix.Identity;
			ViewMatrix = Matrix.Identity;
			ViewProjectionMatrix = Matrix.Identity;
			ProjectionMatrix = Matrix.Identity;
			FirstPersonViewMatrix = Matrix.Identity;
			FirstPersonProjectionMatrix = Matrix.Identity;
			ReprojectFromCurrentViewToPreviousProjectionMatrix = Matrix.Identity;
			ReprojectFromPreviousViewToCurrentProjection = Matrix.Identity;
			ProjectionJittering = Vector2.Zero;
			HasCameraMoved = true;
			IsCameraUnderwater = false;
			CameraPosition = Vector3.Zero;
			CameraDirection = Vector3.Zero;
			ViewFrustum = new BoundingFrustum(Matrix.Identity);
			RelativeViewFrustum = new BoundingFrustum(Matrix.Identity);
			FrustumFarCornersWS = new Vector3[4];
			FrustumFarCornersVS = new Vector3[4];
			SunShadowRenderData.VirtualSunViewFrustum = new BoundingFrustum(Matrix.Identity);
			SunShadowRenderData.VirtualSunKDopFrustum = new KDop(13);
			SunShadowRenderData.VirtualSunPositions = new Vector3[4];
			SunShadowRenderData.VirtualSunViewRotationMatrix = new Matrix[4];
			SunShadowRenderData.VirtualSunProjectionMatrix = new Matrix[4];
			SunShadowRenderData.VirtualSunViewRotationProjectionMatrix = new Matrix[4];
			SunShadowRenderData.CascadeDistanceAndTexelScales = new Vector2[4];
			SunShadowRenderData.CascadeCachedTranslations = new Vector3[4];
			SunColor = Vector3.One;
			SunLightColor = Vector4.One;
			SunPositionWS = Vector3.One;
			SunPositionVS = Vector3.One;
			AmbientFrontColor = Vector3.One;
			AmbientBackColor = Vector3.One;
			AmbientIntensity = 0f;
			FogTopColor = Vector3.One;
			FogFrontColor = Vector3.One;
			FogBackColor = Vector3.One;
			FogParams = Vector4.One;
			FogHeightFalloffUnderwater = 4f;
			FogDensityUnderwater = 0.3f;
			WaterCausticsAnimTime = 0f;
			WaterCausticsDistortion = 0f;
			WaterCausticsScale = 0.05f;
			WaterCausticsIntensity = 1f;
			CloudsShadowAnimTime = 0f;
			CloudsShadowBlurriness = 3.5f;
			CloudsShadowScale = 0.005f;
			CloudsShadowIntensity = 0.5f;
		}

		public void SetViewportSize(int width, int height)
		{
			ViewportSize = new Vector2(width, height);
			InvViewportSize = new Vector2((float)(1.0 / (double)width), (float)(1.0 / (double)height));
		}
	}

	public struct ModelVFXDrawTask
	{
		public Vector3 ModelVFXHighlightColor;

		public float ModelVFXHighlightThickness;

		public Vector2 ModelVFXNoiseScale;

		public Vector2 ModelVFXNoiseScrollSpeed;

		public int ModelVFXPackedParams;

		public Vector4 ModelVFXPostColor;
	}

	private struct EntityDrawTask
	{
		public Vector4 BlockLightColor;

		public Vector3 BottomTint;

		public Vector3 TopTint;

		public float InvModelHeight;

		public Matrix ModelMatrix;

		public GLVertexArray VertexArray;

		public int DataCount;

		public GLBuffer AnimationData;

		public uint AnimationDataOffset;

		public ushort AnimationDataSize;

		public float ModelVFXAnimationProgress;

		public int ModelVFXId;

		public float UseDithering;

		public ushort EntityLocalId;
	}

	private struct EntityDistortionDrawTask
	{
		public float InvModelHeight;

		public Matrix ModelMatrix;

		public GLVertexArray VertexArray;

		public int DataCount;

		public GLBuffer AnimationData;

		public uint AnimationDataOffset;

		public ushort AnimationDataSize;

		public float ModelVFXAnimationProgress;

		public int ModelVFXId;

		public ushort EntityLocalId;
	}

	private struct NameplateDrawTask
	{
		public Matrix MVPMatrix;

		public Vector3 Position;

		public float FillBlurThreshold;

		public GLVertexArray VertexArray;

		public ushort DataCount;

		public ushort EntityLocalId;
	}

	private struct DebugInfoDrawTask
	{
		public Matrix LineSightMVPMatrix;

		public Matrix LineRepulsionMVPMatrix;

		public Matrix BoxHeadMVPMatrix;

		public Matrix BoxMVPMatrix;

		public Matrix BoxCollisionMatrix;

		public Matrix CylinderCollisionMatrix;

		public Matrix SphereMVPMatrix;

		public Vector3 SphereColor;

		public DebugInfoDetailTask[] DetailTasks;

		public bool Hit;

		public bool RenderCollision;

		public bool Collided;
	}

	public struct DebugInfoDetailTask
	{
		public Vector3 Color;

		public Matrix Matrix;
	}

	public enum LightingResolution
	{
		FULL,
		MIXED,
		LOW
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct ChunkDrawTask
	{
		public GLVertexArray VertexArray;

		public int DataCount;

		public IntPtr DataOffset;

		public Matrix ModelMatrix;
	}

	private struct AnimatedBlockDrawTask
	{
		public GLVertexArray VertexArray;

		public int DataCount;

		public GLBuffer AnimationData;

		public uint AnimationDataOffset;

		public ushort AnimationDataSize;

		public Matrix ModelMatrix;
	}

	private struct ChunkOcclusionCullingSetup
	{
		public byte RequestedOpaqueChunkOccludersCount;

		public bool UseChunkOccluderPlanes;

		public bool UseOpaqueChunkOccluders;

		public bool UseAlphaTestedChunkOccluders;

		public byte MapAtlasTextureUnit;

		public GLTexture MapAtlasTexture;
	}

	private struct SunShadowCastingSettings
	{
		public enum ShadowDirectionType
		{
			TopDown,
			StaticCustom,
			DynamicSun
		}

		public ShadowDirectionType DirectionType;

		public Vector3 Direction;

		public float ShadowIntensity;

		public bool UseSafeAngle;

		public bool UseChunkShadowCasters;

		public bool UseEntitiesModelVFX;

		public bool UseDrawInstanced;

		public bool UseSmartCascadeDispatch;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct EntityShadowMapDrawTask
	{
		public Matrix ModelMatrix;

		public GLVertexArray VertexArray;

		public int DataCount;

		public GLBuffer AnimationData;

		public uint AnimationDataOffset;

		public ushort AnimationDataSize;

		public UShortVector2 CascadeFirstLast;

		public float InvModelHeight;

		public float ModelVFXAnimationProgress;

		public int ModelVFXId;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct ChunkShadowMapDrawTask
	{
		public Matrix ModelMatrix;

		public GLVertexArray VertexArray;

		public int DataCount;

		public IntPtr DataOffset;

		public UShortVector2 CascadeFirstLast;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct AnimatedBlockShadowMapDrawTask
	{
		public Matrix ModelMatrix;

		public GLVertexArray VertexArray;

		public int DataCount;

		public IntPtr DataOffset;

		public GLBuffer AnimationData;

		public uint AnimationDataOffset;

		public ushort AnimationDataSize;

		public UShortVector2 CascadeFirstLast;
	}

	private struct ShadowCascadeStats
	{
		public ushort DrawCallCount;

		public ushort KiloTriangleCount;
	}

	private RenderTargetStore _renderTargetStore;

	public const float NearClippingPlane = 0.1f;

	public const float FarClippingPlane = 1024f;

	public SceneData Data;

	public SceneData PreviousData;

	public bool UseSSAOBlur = true;

	public float SSAOParamOcclusionMax;

	public float SSAOParamOcclusionStrength;

	public float SSAOParamRadius;

	private float _ssaoParamRadiusProjectedScale;

	private Vector4 _ssaoPackedParameters;

	public static Vector2 DefaultSSAOResolutionScale = new Vector2(0.5f, 0.5f);

	private Vector2 _ssaoResolutionScale = DefaultSSAOResolutionScale;

	private RenderTarget _ssaoTapsSource;

	private int _ssaoSamplesCount = 4;

	private Vector2[] _ssaoSamplesData = new Vector2[16];

	private float _ssaoTemporalSampleOffset;

	private bool _useSkyAmbient = true;

	private readonly Profiling Profiling;

	private int _renderingProfileLinearZ;

	private int _renderingProfileLinearZDownsample;

	private int _renderingProfileZDownsample;

	private int _renderingProfileEdgeDetection;

	private const int FirstPersonMinFov = 40;

	private const int FirstPersonMaxFov = 70;

	private GLSampler _pointSampler;

	private GLSampler _linearSampler;

	private readonly GraphicsDevice _graphics;

	private readonly GPUProgramStore _gpuProgramStore;

	private readonly GLFunctions _gl;

	private bool _hasDownsampledZ;

	public OrderIndependentTransparency OIT;

	public const byte EdgesStencilBit = 7;

	private GPUBuffer _sceneDataBuffer;

	private GLVertexArray _emptyVAO;

	private const uint SceneDataBufferSize = 1248u;

	private QuadRenderer _quadRenderer;

	private BoxRenderer _boxRenderer;

	private LineRenderer _lineRenderer;

	private GPUBufferTexture _modelVFXDataBufferTexture;

	public static uint ModelVFXDataSize = (uint)(Marshal.SizeOf(typeof(Vector4)) * 4);

	private const int ModelVFXBufferDefaultSize = 500;

	private const int ModelVFXBufferGrowth = 200;

	private ModelVFXDrawTask[] _modelVFXDrawTasks = new ModelVFXDrawTask[500];

	private int _modelVFXDrawTaskCount;

	private uint _modelVFXBufferSize = ModelVFXDataSize * 256;

	private GPUBufferTexture _entityDataBufferTexture;

	public static readonly int GPUEntityDataSize = Marshal.SizeOf(typeof(Matrix)) + Marshal.SizeOf(typeof(Vector4)) * 4;

	private const uint EntityBufferGrowth = 1024u;

	private uint _entityBufferSize = (uint)(GPUEntityDataSize * 2048);

	private const int EntityDrawTasksDefaultSize = 500;

	private const int EntityDrawTasksGrowth = 200;

	private EntityDrawTask[] _entityDrawTasks = new EntityDrawTask[500];

	private int _incomingEntityDrawTaskCount;

	private int _entityDrawTaskCount;

	private EntityDrawTask[] _entityForwardDrawTasks = new EntityDrawTask[500];

	private int _entityForwardDrawTaskCount;

	public static readonly int GPUEntityDistortionDataSize = Marshal.SizeOf(typeof(Matrix)) + Marshal.SizeOf(typeof(Vector4));

	private const uint EntityDistortionBufferGrowth = 1024u;

	private uint _entityDistortionBufferSize = (uint)(GPUEntityDataSize * 2048);

	private const int EntityDistortionDrawTasksDefaultSize = 500;

	private const int EntityDistortionDrawTasksGrowth = 200;

	private EntityDistortionDrawTask[] _entityDistortionDrawTasks = new EntityDistortionDrawTask[500];

	private int _entityDistortionDrawTaskCount;

	private const int NameplateDrawTasksDefaultSize = 100;

	private const int NameplateDrawTasksGrowth = 50;

	private NameplateDrawTask[] _nameplateDrawTasks = new NameplateDrawTask[100];

	private int _nameplateDrawTaskCount;

	private const int DebugInfoDrawTasksDefaultSize = 100;

	private const int DebugInfoDrawTasksGrowth = 50;

	private int _debugInfoDrawTaskCount;

	private DebugInfoDrawTask[] _debugInfoDrawTasks = new DebugInfoDrawTask[100];

	public bool UseDynamicLightResolutionSelection = true;

	public bool UseLinearZForLighting = true;

	public bool UseLightBlendMax = true;

	public bool UseClusteredLighting = true;

	public ClusteredLighting ClusteredLighting;

	public ClassicDeferredLighting ClassicDeferredLighting;

	private RenderTarget _lightBuffer;

	private LightingResolution _lightResolution = LightingResolution.FULL;

	private bool _requestLinearZ = true;

	private bool _requestDownsampledLinearZ = true;

	private bool _useLBufferCompression;

	private Mesh _sphereLightMesh;

	private Mesh _cylinderMesh;

	private int _renderingProfileLights;

	private int _renderingProfileLightsFullRes;

	private int _renderingProfileLightsLowRes;

	private int _renderingProfileLightsStencil;

	private int _renderingProfileLightsMix;

	private const int OpaqueDrawTasksDefaultSize = 400;

	private const int OpaqueDrawTasksGrowth = 100;

	private ChunkDrawTask[] _opaqueDrawTasks = new ChunkDrawTask[400];

	private int _opaqueDrawTaskCount;

	private const int AlphaBlendedDrawTasksDefaultSize = 200;

	private const int AlphaBlendedDrawTasksGrowth = 50;

	private ChunkDrawTask[] _alphaBlendedDrawTasks = new ChunkDrawTask[200];

	private int _alphaBlendedDrawTaskCount;

	private const int AlphaTestedDrawTasksDefaultSize = 400;

	private const int AlphaTestedDrawTasksGrowth = 100;

	private ChunkDrawTask[] _alphaTestedDrawTasks = new ChunkDrawTask[400];

	private int _alphaTestedDrawTaskCount;

	private readonly byte ChunkDrawTagOpaque = 1;

	private readonly byte ChunkDrawTagAlphaTested = 2;

	private readonly byte ChunkDrawTagAlphaBlended = 4;

	private readonly byte ChunkDrawTagAnimated = 8;

	private int _farProgramOpaqueChunkStartIndex;

	private int _nearProgramAlphaBlendedChunkStartIndex = -1;

	private int _farProgramAlphaTestedChunkStartIndex;

	private const int AnimatedBlockDrawTasksDefaultSize = 100;

	private const int AnimatedBlockDrawTasksGrowth = 25;

	private AnimatedBlockDrawTask[] _animatedBlockDrawTasks = new AnimatedBlockDrawTask[100];

	private int _animatedBlockDrawTaskCount;

	private readonly Vector3 _chunkSize = new Vector3(32f);

	private readonly Vector3 _chunkHalfSize = new Vector3(16f);

	private float _nearChunkDistance = 64f;

	public int[] VisibleOccludees;

	private const byte MaxAlphaTestedOccluders = 10;

	private const byte MaxOpaqueOccluders = 100;

	private byte[] _opaqueOccludersIDs = new byte[100];

	private byte _opaqueOccludersCount;

	private const int MaxOccludees = 2000;

	private const int OccludeesGrowth = 500;

	private OcclusionCulling.OccludeeData[] _occludeesData = new OcclusionCulling.OccludeeData[2000];

	private int _opaqueOccludeesCount;

	private Vector3[] _opaqueOccludeesData = new Vector3[400];

	private int _alphaTestedOccludeesCount;

	private Vector3[] _alphaTestedOccludeesData = new Vector3[400];

	private int _alphaBlendedOccludeesCount;

	private Vector3[] _alphaBlendedOccludeesData = new Vector3[200];

	private int _entitiesOccludeesCount;

	private BoundingBox[] _entitiesOccludeesData = new BoundingBox[1000];

	private int _lightOccludeesCount;

	private BoundingBox[] _lightOccludeesData = new BoundingBox[1000];

	private int _particleOccludeesCount;

	private BoundingBox[] _particleOccludeesData = new BoundingBox[1000];

	private const int MaxOccluderPlanes = 2000;

	private const int OccluderPlanesGrowth = 1000;

	private Vector4[] _occluderPlanes = new Vector4[2000];

	private int _occluderPlanesCount;

	private GLVertexArray _occluderPlanesVertexArray;

	private GLBuffer _occluderPlanesVerticesBuffer;

	private GLBuffer _occluderPlanesIndicesBuffer;

	private ChunkOcclusionCullingSetup _occlusionCullingSetup;

	private CascadedShadowMapping _cascadedShadowMapping;

	private bool UseSunShadows;

	private SunShadowCastingSettings _sunShadowCasting;

	private GPUBufferTexture _entityShadowMapDataBufferTexture;

	public static readonly int GPUEntityShadowMapDataSize = Marshal.SizeOf(typeof(Matrix)) + Marshal.SizeOf(typeof(Vector4)) * 2;

	private const uint EntityShadowMapBufferGrowth = 1024u;

	private uint _entityShadowMapBufferSize = (uint)(GPUEntityShadowMapDataSize * 2048);

	private const int EntityShadowMapDrawTasksDefaultSize = 500;

	private const int EntityShadowMapDrawTasksGrowth = 200;

	private EntityShadowMapDrawTask[] _entityShadowMapDrawTasks = new EntityShadowMapDrawTask[500];

	private BoundingSphere[] _entitiesBoundingVolumes = new BoundingSphere[500];

	private int _incomingEntityShadowMapDrawTaskCount;

	private int _entityShadowMapDrawTaskCount;

	private const int ChunkShadowMapDrawTasksDefaultSize = 500;

	private const int ChunkShadowMapDrawTasksGrowth = 200;

	private ChunkShadowMapDrawTask[] _chunkShadowMapDrawTasks = new ChunkShadowMapDrawTask[500];

	private int _incomingChunkShadowMapDrawTaskCount;

	private int _chunkShadowMapDrawTaskCount;

	private const int AnimatedBlockShadowMapDrawTasksDefaultSize = 500;

	private const int AnimatedBlockShadowMapDrawTasksGrowth = 200;

	private AnimatedBlockShadowMapDrawTask[] _animatedBlockShadowMapDrawTasks = new AnimatedBlockShadowMapDrawTask[500];

	private BoundingBox[] _animatedBlockBoundingVolumes = new BoundingBox[500];

	private int _incomingAnimatedBlockShadowMapDrawTaskCount;

	private int _animatedBlockShadowMapDrawTaskCount;

	private const int CascadeDrawTasksDefaultSize = 1000;

	private const int CascadeDrawTasksGrowth = 500;

	private ushort[][] _cascadeDrawTaskId = new ushort[4][];

	private ushort[] _cascadeEntityDrawTaskCount = new ushort[4];

	private ushort[] _cascadeChunkDrawTaskCount = new ushort[4];

	private ushort[] _cascadeAnimatedBlockDrawTaskCount = new ushort[4];

	private ShadowCascadeStats[] _cascadeStats = new ShadowCascadeStats[4];

	public bool UseSSAO { get; private set; } = true;


	public int SSAOQuality { get; private set; } = 0;


	public bool HasDownsampledZ => _hasDownsampledZ;

	public GLBuffer SceneDataBuffer => _sceneDataBuffer.Current;

	public bool HasVisibleNameplates => _nameplateDrawTaskCount != 0;

	public bool HasEntityDistortionTask => _entityDistortionDrawTaskCount > 0;

	public int ChunkOccludeesOffset => 0;

	public int OpaqueChunkOccludeesOffset => 0;

	public int AlphaTestedChunkOccludeesOffset => _opaqueOccludeesCount;

	public int AlphaBlendedOccludeesOffset => _opaqueOccludeesCount + _alphaTestedOccludeesCount;

	public int EntityOccludeesOffset => _opaqueOccludeesCount + _alphaTestedOccludeesCount + _alphaBlendedOccludeesCount;

	public int LightOccludeesOffset => _opaqueOccludeesCount + _alphaTestedOccludeesCount + _alphaBlendedOccludeesCount + _entitiesOccludeesCount;

	public int ParticleOccludeesOffset => _opaqueOccludeesCount + _alphaTestedOccludeesCount + _alphaBlendedOccludeesCount + _entitiesOccludeesCount + _lightOccludeesCount;

	public int ChunkOccludeesCount => _opaqueOccludeesCount + _alphaTestedOccludeesCount + _alphaBlendedOccludeesCount;

	public int OpaqueChunkOccludeesCount => _opaqueOccludeesCount;

	public int AlphaTestedChunkOccludeesCount => _alphaTestedOccludeesCount;

	public int AlphaBlendedOccludeesCount => _alphaBlendedOccludeesCount;

	public int EntityOccludeesCount => _entitiesOccludeesCount;

	public int LightOccludeesCount => _lightOccludeesCount;

	public int ParticleOccludeesCount => _particleOccludeesCount;

	public bool IsSunShadowMappingEnabled => UseSunShadows;

	public bool IsWorldShadowEnabled => _sunShadowCasting.UseChunkShadowCasters;

	public bool UseSunShadowsSmartCascadeDispatch => _sunShadowCasting.UseSmartCascadeDispatch;

	public bool UseDeferredShadowBlur => _cascadedShadowMapping.UseDeferredShadowBlur;

	public bool UseSunShadowsGlobalKDop => _cascadedShadowMapping.UseSunShadowsGlobalKDop;

	public bool NeedsDebugDrawShadowRelated => _cascadedShadowMapping.NeedsDebugDrawShadowRelated;

	public void ResetSSAOParameters()
	{
		SSAOParamOcclusionMax = 0.3f;
		SSAOParamOcclusionStrength = 3f;
		SSAOParamRadius = 0.75f;
		_ssaoParamRadiusProjectedScale = 500f;
	}

	private void PrepareSSAOParameters()
	{
		_ssaoPackedParameters.X = SSAOParamOcclusionMax;
		_ssaoPackedParameters.Y = SSAOParamOcclusionStrength;
		_ssaoPackedParameters.Z = SSAOParamRadius;
		_ssaoPackedParameters.W = SSAOParamRadius * (_ssaoParamRadiusProjectedScale * Data.ViewportSize.X / 1920f);
		_ssaoTemporalSampleOffset = (float)(Data.FrameCounter % _ssaoSamplesCount) * ((float)System.Math.PI * 2f) / (float)_ssaoSamplesCount;
	}

	private void ComputeSSAOSamplesData()
	{
		for (int i = 0; i < _ssaoSamplesCount; i++)
		{
			_ssaoSamplesData[i].X = (float)(System.Math.Sqrt((double)i + 0.5) / System.Math.Sqrt(_ssaoSamplesCount));
			_ssaoSamplesData[i].Y = (float)i * 2.4f;
		}
	}

	public void SetUseSSAO(bool useSSAO, bool useTemporalFiltering = true, int quality = -1)
	{
		bool flag = false;
		if (quality >= 0 && quality != SSAOQuality)
		{
			SSAOQuality = quality;
			switch (quality)
			{
			case 0:
				_ssaoResolutionScale = new Vector2(0.5f, 0.5f);
				_ssaoSamplesCount = 4;
				_gpuProgramStore.BlurSSAOAndShadowProgram.UseEdgeAwareness = false;
				_ssaoTapsSource = _renderTargetStore.LinearZHalfRes;
				break;
			case 1:
				_ssaoResolutionScale = new Vector2(0.7f, 0.7f);
				_ssaoSamplesCount = 6;
				_gpuProgramStore.BlurSSAOAndShadowProgram.UseEdgeAwareness = true;
				_ssaoTapsSource = _renderTargetStore.LinearZ;
				break;
			case 2:
				_ssaoResolutionScale = new Vector2(1f, 1f);
				_ssaoSamplesCount = 12;
				_gpuProgramStore.BlurSSAOAndShadowProgram.UseEdgeAwareness = true;
				_ssaoTapsSource = _renderTargetStore.LinearZ;
				break;
			}
			_gpuProgramStore.SSAOProgram.SamplesCount = _ssaoSamplesCount;
			ComputeSSAOSamplesData();
			_renderTargetStore.ResizeSSAOBuffers(_renderTargetStore.GBuffer.Width, _renderTargetStore.GBuffer.Height, _ssaoResolutionScale);
			flag = true;
		}
		if (useTemporalFiltering != _gpuProgramStore.SSAOProgram.UseTemporalFiltering)
		{
			_gpuProgramStore.SSAOProgram.UseTemporalFiltering = useTemporalFiltering;
			flag = true;
		}
		if (UseSSAO != useSSAO)
		{
			UseSSAO = useSSAO;
			_gpuProgramStore.DeferredProgram.UseSSAO = useSSAO;
			flag = true;
		}
		if (flag)
		{
			_gpuProgramStore.DeferredProgram.Reset();
			_gpuProgramStore.SSAOProgram.Reset();
			_gpuProgramStore.BlurSSAOAndShadowProgram.Reset();
		}
	}

	public void SetUseSkyAmbient(bool enable)
	{
		if (_useSkyAmbient != enable)
		{
			_useSkyAmbient = enable;
			_gpuProgramStore.DeferredProgram.UseSkyAmbient = enable;
			_gpuProgramStore.DeferredProgram.Reset();
			_gpuProgramStore.MapChunkAlphaBlendedProgram.UseSkyAmbient = enable;
			_gpuProgramStore.MapChunkAlphaBlendedProgram.Reset();
		}
	}

	public SceneRenderer(GraphicsDevice graphics, Profiling profiling, int width, int height)
	{
		_graphics = graphics;
		_gpuProgramStore = graphics.GPUProgramStore;
		_gl = _graphics.GL;
		Profiling = profiling;
		_renderTargetStore = graphics.RTStore;
		_ssaoTapsSource = _renderTargetStore.LinearZHalfRes;
		ComputeSSAOSamplesData();
		ResetSSAOParameters();
		Data.Init(width, height);
		PreviousData.Init(width, height);
		CreateGPUData();
		InitLighting();
		InitOcclusionCulling();
		InitEntityRendering();
		InitSunShadows();
		InitOIT();
	}

	protected override void DoDispose()
	{
		DestroyGPUData();
		DisposeLighting();
		DisposeOcclusionCulling();
		DisposeEntityRendering();
		DisposeSunShadows();
		DisposeOIT();
	}

	private void CreateGPUData()
	{
		_pointSampler = _gl.GenSampler();
		_gl.SamplerParameteri(_pointSampler, GL.TEXTURE_WRAP_S, GL.CLAMP_TO_EDGE);
		_gl.SamplerParameteri(_pointSampler, GL.TEXTURE_WRAP_T, GL.CLAMP_TO_EDGE);
		_gl.SamplerParameteri(_pointSampler, GL.TEXTURE_MAG_FILTER, GL.NEAREST);
		_gl.SamplerParameteri(_pointSampler, GL.TEXTURE_MIN_FILTER, GL.NEAREST);
		_linearSampler = _gl.GenSampler();
		_gl.SamplerParameteri(_linearSampler, GL.TEXTURE_WRAP_S, GL.CLAMP_TO_EDGE);
		_gl.SamplerParameteri(_linearSampler, GL.TEXTURE_WRAP_T, GL.CLAMP_TO_EDGE);
		_gl.SamplerParameteri(_linearSampler, GL.TEXTURE_MAG_FILTER, GL.LINEAR);
		_gl.SamplerParameteri(_linearSampler, GL.TEXTURE_MIN_FILTER, GL.LINEAR);
		_sceneDataBuffer.CreateStorage(GL.UNIFORM_BUFFER, GL.STREAM_DRAW, useDoubleBuffering: true, 1248u, 0u, GPUBuffer.GrowthPolicy.Never);
		_emptyVAO = _gl.GenVertexArray();
	}

	private void DestroyGPUData()
	{
		_gl.DeleteVertexArray(_emptyVAO);
		_sceneDataBuffer.DestroyStorage();
		_gl.DeleteSampler(_linearSampler);
		_gl.DeleteSampler(_pointSampler);
	}

	private void InitOIT()
	{
		OIT = new OrderIndependentTransparency(_graphics, _renderTargetStore, Profiling);
	}

	private void DisposeOIT()
	{
		OIT = null;
	}

	public void SetupRenderingProfiles(int profileLights, int profileLightsFullRes, int profileLightsLowRes, int profileLightsStencil, int profileLightsMix, int profileLinearZ, int profileLinearZDownsample, int profileZDownsample, int profileEdgeDetection)
	{
		SetupLightRenderingProfiles(profileLights, profileLightsFullRes, profileLightsLowRes, profileLightsStencil, profileLightsMix);
		_renderingProfileLinearZ = profileLinearZ;
		_renderingProfileLinearZDownsample = profileLinearZDownsample;
		_renderingProfileZDownsample = profileZDownsample;
		_renderingProfileEdgeDetection = profileEdgeDetection;
	}

	public void BeginFrame()
	{
		ResetOcclusionCullingCounters();
		ResetMapCounters();
		ResetEntityCounters();
		ResetSunShadowsCounters();
		PingPongBuffers();
		PingPongEntityDataBuffers();
		PingPongEntityShadowMapDataBuffers();
	}

	public void BeginDraw()
	{
		PrepareSSAOParameters();
		UpdateLightingHeuristics();
	}

	private void PingPongBuffers()
	{
		_sceneDataBuffer.Swap();
	}

	public void Resize(int width, int height)
	{
		int num = (int)((float)width * 0.25f);
		int num2 = (int)((float)height * 0.25f);
		int num3 = (int)((float)width * 0.125f);
		int num4 = (int)((float)height * 0.125f);
		int num5 = (int)((float)width * 0.0625f);
		int num6 = (int)((float)height * 0.0625f);
		Data.SetViewportSize(width, height);
	}

	public void UpdateProjectionMatrix(float fov, double ratio, bool useJittering = false)
	{
		float num = MathHelper.ToRadians(fov);
		_graphics.CreatePerspectiveMatrix(num, (float)ratio, 0.1f, 1024f, out Data.ProjectionMatrix);
		float fieldOfView = MathHelper.ToRadians(MathHelper.Clamp(fov, 40f, 70f));
		_graphics.CreatePerspectiveMatrix(fieldOfView, (float)ratio, 0.1f, 1024f, out Data.FirstPersonProjectionMatrix);
		if (useJittering)
		{
			Data.ProjectionJittering = ((Data.FrameCounter % 2 == 0) ? new Vector2(0.5f, 0f) : new Vector2(0f, 0.5f));
			Data.ProjectionJittering.X /= _graphics.RTStore.FinalSceneColor.Width;
			Data.ProjectionJittering.Y /= _graphics.RTStore.FinalSceneColor.Height;
			Data.ProjectionMatrix.M31 = Data.ProjectionJittering.X;
			Data.ProjectionMatrix.M32 = Data.ProjectionJittering.Y;
			Data.FirstPersonProjectionMatrix.M31 = Data.ProjectionJittering.X;
			Data.FirstPersonProjectionMatrix.M32 = Data.ProjectionJittering.Y;
		}
		Data.WorldFieldOfView = num;
		Data.AspectRatio = (float)ratio;
	}

	public void UpdateRenderData(Vector3 cameraLook, Vector3 cameraPosition, Vector3 localPlayerPosition, uint frameCounter, float time, float deltaTime)
	{
		PreviousData = Data;
		Data.FrameCounter = frameCounter;
		Data.Time = time;
		Data.DeltaTime = deltaTime;
		cameraLook.X = ((cameraLook.X > 1.5657964f) ? 1.5657964f : cameraLook.X);
		cameraLook.X = ((cameraLook.X < -1.5657964f) ? (-1.5657964f) : cameraLook.X);
		Matrix.CreateRotationX(0f - cameraLook.X, out Data.ViewMatrix);
		Matrix.CreateRotationY(0f - cameraLook.Y, out var result);
		Matrix.Multiply(ref result, ref Data.ViewMatrix, out Data.ViewRotationMatrix);
		Matrix.CreateRotationZ(0f - cameraLook.Z, out result);
		Matrix.Multiply(ref Data.ViewRotationMatrix, ref result, out Data.ViewRotationMatrix);
		Matrix.Multiply(ref Data.ViewRotationMatrix, ref Data.ProjectionMatrix, out Data.ViewRotationProjectionMatrix);
		Data.InvViewRotationProjectionMatrix = Matrix.Invert(Data.ViewRotationProjectionMatrix);
		Data.InvViewRotationMatrix = Matrix.Invert(Data.ViewRotationMatrix);
		Data.CameraPosition = cameraPosition;
		Data.PlayerRenderPosition = localPlayerPosition - Data.CameraPosition;
		Vector3 position = -Data.CameraPosition;
		Matrix.CreateTranslation(ref position, out result);
		Matrix.Multiply(ref result, ref Data.ViewRotationMatrix, out Data.ViewMatrix);
		Matrix.Multiply(ref Data.ViewMatrix, ref Data.ProjectionMatrix, out Data.ViewProjectionMatrix);
		Data.ViewFrustum.Matrix = Data.ViewProjectionMatrix;
		Data.RelativeViewFrustum.Matrix = Data.ViewRotationProjectionMatrix;
		Data.InvViewProjectionMatrix = Matrix.Invert(Data.ViewProjectionMatrix);
		Data.InvViewMatrix = Matrix.Invert(Data.ViewMatrix);
		Data.ReprojectFromCurrentViewToPreviousProjectionMatrix = Matrix.Multiply(Data.InvViewMatrix, PreviousData.ViewProjectionMatrix);
		Data.ReprojectFromPreviousViewToCurrentProjection = Matrix.Multiply(PreviousData.InvViewMatrix, Data.ViewProjectionMatrix);
		Matrix.CreateRotationX(cameraLook.X, out var result2);
		Matrix.CreateRotationY(cameraLook.Y, out result);
		Matrix.Multiply(ref result2, ref result, out result2);
		Data.CameraDirection = Vector3.Transform(Vector3.Forward, result2);
		Data.HasCameraMoved = Data.ViewMatrix != PreviousData.ViewMatrix;
		BoundingFrustum boundingFrustum = new BoundingFrustum(Data.ViewRotationProjectionMatrix);
		boundingFrustum.GetFarCorners(Data.FrustumFarCornersWS);
		Vector3.Transform(Data.FrustumFarCornersWS, 0, ref Data.ViewRotationMatrix, Data.FrustumFarCornersVS, 0, 4);
		Data.FrustumFarCornersWS[0] = Data.FrustumFarCornersWS[3] + (Data.FrustumFarCornersWS[0] - Data.FrustumFarCornersWS[3]) * 2f;
		Data.FrustumFarCornersWS[2] = Data.FrustumFarCornersWS[3] + (Data.FrustumFarCornersWS[2] - Data.FrustumFarCornersWS[3]) * 2f;
		Data.FrustumFarCornersVS[0] = Data.FrustumFarCornersVS[3] + (Data.FrustumFarCornersVS[0] - Data.FrustumFarCornersVS[3]) * 2f;
		Data.FrustumFarCornersVS[2] = Data.FrustumFarCornersVS[3] + (Data.FrustumFarCornersVS[2] - Data.FrustumFarCornersVS[3]) * 2f;
	}

	public void UpdateAtmosphericData(bool isCameraUnderwater, Vector3 sunColor, Vector4 sunLightColor, Vector3 sunPosition, Vector3 fogTopColor, Vector3 fogFrontColor, Vector3 fogBackColor, Vector4 fogParams, Vector4 fogMoodParams, float fogHeightDensityAtViewer, Vector3 ambientFrontColor, Vector3 ambientBackColor, float ambientIntensity, float waterCausticsAnimTime, float waterCausticsDistortion, float waterCausticsScale, float waterCausticsIntensity, float cloudsShadowAnimTime, float cloudsShadowsBlurriness, float cloudsShadowsScale, float cloudsShadowIntensity)
	{
		Data.IsCameraUnderwater = isCameraUnderwater;
		Data.SunColor = sunColor;
		Data.SunLightColor = sunLightColor;
		Data.SunPositionWS = sunPosition;
		Data.SunPositionVS = Vector3.TransformNormal(sunPosition, Data.ViewRotationMatrix);
		Data.FogTopColor = fogTopColor;
		Data.FogFrontColor = fogFrontColor;
		Data.FogBackColor = fogBackColor;
		Data.FogParams = fogParams;
		Data.FogMoodParams = fogMoodParams;
		Data.FogHeightDensityAtViewer = fogHeightDensityAtViewer;
		Data.AmbientFrontColor = ambientFrontColor;
		Data.AmbientBackColor = ambientBackColor;
		Data.AmbientIntensity = ambientIntensity;
		Data.WaterCausticsAnimTime = waterCausticsAnimTime;
		Data.WaterCausticsDistortion = waterCausticsDistortion;
		Data.WaterCausticsScale = waterCausticsScale;
		Data.WaterCausticsIntensity = waterCausticsIntensity;
		Data.CloudsShadowAnimTime = cloudsShadowAnimTime;
		Data.CloudsShadowBlurriness = cloudsShadowsBlurriness;
		Data.CloudsShadowScale = cloudsShadowsScale;
		Data.CloudsShadowIntensity = cloudsShadowIntensity;
	}

	public void AnalyzeSunOcclusion()
	{
		SunOcclusionDownsampleProgram sunOcclusionDownsampleProgram = _gpuProgramStore.SunOcclusionDownsampleProgram;
		_gl.UseProgram(sunOcclusionDownsampleProgram);
		_renderTargetStore.SunOcclusionBufferLowRes.Bind(clear: false, setupViewport: true);
		_gl.ActiveTexture(GL.TEXTURE1);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.LinearZHalfRes.GetTexture(RenderTarget.Target.Color0));
		_gl.ActiveTexture(GL.TEXTURE0);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.LightBufferFullRes.GetTexture(RenderTarget.Target.Color0));
		sunOcclusionDownsampleProgram.CameraPosition.SetValue(Data.CameraPosition);
		sunOcclusionDownsampleProgram.CameraDirection.SetValue(Data.CameraDirection);
		_graphics.ScreenTriangleRenderer.Draw();
		_renderTargetStore.SunOcclusionBufferLowRes.Unbind();
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.SunOcclusionBufferLowRes.GetTexture(RenderTarget.Target.Color0));
		_gl.GenerateMipmap(GL.TEXTURE_2D);
		_renderTargetStore.SunOcclusionHistory.Bind(clear: false, setupViewport: false);
		int x = (int)(Data.FrameCounter % (uint)_renderTargetStore.SunOcclusionHistory.Width);
		_gl.Viewport(x, 0, 1, 1);
		_gl.ActiveTexture(GL.TEXTURE0);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.SunOcclusionBufferLowRes.GetTexture(RenderTarget.Target.Color0));
		ScreenBlitProgram screenBlitProgram = _gpuProgramStore.ScreenBlitProgram;
		_gl.UseProgram(screenBlitProgram);
		int value = _renderTargetStore.SunOcclusionBufferLowRes.GetTextureMipLevelCount(RenderTarget.Target.Color0) - 1;
		screenBlitProgram.MipLevel.SetValue(value);
		_graphics.ScreenTriangleRenderer.Draw();
		_renderTargetStore.SunOcclusionHistory.Unbind();
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.SunOcclusionHistory.GetTexture(RenderTarget.Target.Color0));
		_gl.GenerateMipmap(GL.TEXTURE_2D);
	}

	public void BuildReflectionMips()
	{
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.PreviousSceneColor.GetTexture(RenderTarget.Target.Color0));
		_gl.GenerateMipmap(GL.TEXTURE_2D);
	}

	public unsafe void SendSceneDataToGPU()
	{
		IntPtr pointer = _sceneDataBuffer.BeginTransfer(1232u);
		int num = 0;
		Matrix* ptr = (Matrix*)pointer.ToPointer();
		ptr[num++] = Data.FirstPersonViewMatrix;
		ptr[num++] = Data.FirstPersonProjectionMatrix;
		ptr[num++] = Data.ViewRotationMatrix;
		ptr[num++] = Data.ProjectionMatrix;
		ptr[num++] = Data.ViewRotationProjectionMatrix;
		ptr[num++] = Data.InvViewRotationMatrix;
		ptr[num++] = Data.InvViewRotationProjectionMatrix;
		ptr[num++] = Data.ReprojectFromCurrentViewToPreviousProjectionMatrix;
		pointer = IntPtr.Add(pointer, sizeof(Matrix) * num);
		num = 0;
		Vector4* ptr2 = (Vector4*)pointer.ToPointer();
		Vector4 vector = new Vector4(-2f / Data.ProjectionMatrix.M11, -2f / Data.ProjectionMatrix.M22, (1f - Data.ProjectionMatrix.M13) / Data.ProjectionMatrix.M11, (1f + Data.ProjectionMatrix.M23) / Data.ProjectionMatrix.M22);
		ptr2[num++] = vector;
		ptr2[num++] = new Vector4(Data.CameraPosition, Data.IsCameraUnderwater ? 1f : 0f);
		ptr2[num++] = new Vector4(Data.CameraDirection);
		ptr2[num++] = new Vector4(Data.ViewportSize, Data.InvViewportSize);
		ptr2[num++] = new Vector4(0.1f, 1024f, 0f, 0f);
		ptr2[num++] = new Vector4(Data.Time, (float)System.Math.Sin(Data.Time), (float)System.Math.Cos(Data.Time), Data.DeltaTime);
		ptr2[num++] = new Vector4(Data.SunPositionWS);
		ptr2[num++] = new Vector4(Data.SunPositionVS);
		ptr2[num++] = Data.SunLightColor;
		ptr2[num++] = new Vector4(Data.SunColor);
		ptr2[num++] = new Vector4(Data.AmbientFrontColor, Data.AmbientIntensity);
		ptr2[num++] = new Vector4(Data.AmbientBackColor, Data.WaterCausticsAnimTime);
		ptr2[num++] = new Vector4(Data.FogTopColor, Data.WaterCausticsDistortion);
		ptr2[num++] = new Vector4(Data.FogFrontColor, Data.WaterCausticsScale);
		ptr2[num++] = new Vector4(Data.FogBackColor, Data.WaterCausticsIntensity);
		ptr2[num++] = Data.FogParams;
		ptr2[num++] = Data.FogMoodParams;
		ptr2[num++] = new Vector4(Data.FogHeightDensityAtViewer, 0f, 0f, Data.CloudsShadowAnimTime);
		ptr2[num++] = new Vector4(ClusteredLighting.GridWidth, ClusteredLighting.GridHeight, ClusteredLighting.GridDepth, 0f);
		ptr2[num++] = new Vector4(ClusteredLighting.GridNearZ, ClusteredLighting.GridFarZ, ClusteredLighting.GridRangeCoef, 0f);
		ptr2[num++] = new Vector4(Data.SunShadowRenderData.DynamicShadowIntensity, Data.CloudsShadowBlurriness, Data.CloudsShadowScale, Data.CloudsShadowIntensity);
		for (int i = 0; i < 4; i++)
		{
			ptr2[num++] = new Vector4(Data.SunShadowRenderData.CascadeDistanceAndTexelScales[i], 0f, 0f);
		}
		for (int j = 0; j < 4; j++)
		{
			ptr2[num++] = new Vector4(Data.SunShadowRenderData.CascadeCachedTranslations[j], 0f);
		}
		pointer = IntPtr.Add(pointer, sizeof(Vector4) * num);
		num = 0;
		ptr = (Matrix*)pointer.ToPointer();
		for (int k = 0; k < 4; k++)
		{
			ptr[num++] = Data.SunShadowRenderData.VirtualSunViewRotationProjectionMatrix[k];
		}
		_sceneDataBuffer.EndTransfer();
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.SunOcclusionHistory.GetTexture(RenderTarget.Target.Color0));
		_gl.UseProgram(_gpuProgramStore.SceneBrightnessPackProgram);
		_gl.Enable(GL.RASTERIZER_DISCARD);
		_gl.BindBufferRange(GL.TRANSFORM_FEEDBACK_BUFFER, 0u, _sceneDataBuffer.Current.InternalId, (IntPtr)1232L, (IntPtr)16L);
		_gl.BeginTransformFeedback(GL.NO_ERROR);
		_gl.BindVertexArray(_emptyVAO);
		_gl.DrawArrays(GL.NO_ERROR, 0, 1);
		_gl.EndTransformFeedback();
		_gl.Disable(GL.RASTERIZER_DISCARD);
	}

	public bool IsSpatialContinuityLost()
	{
		float x = QuantifyCameraMotion().X;
		return x > 225f;
	}

	private Vector2 QuantifyCameraMotion()
	{
		float x = (Data.CameraPosition - PreviousData.CameraPosition).LengthSquared();
		float y = (float)System.Math.Acos(Vector3.Dot(Data.CameraDirection, PreviousData.CameraDirection));
		return new Vector2(x, y);
	}

	public void RenderIntermediateBuffers()
	{
		bool needsZBufferEighthRes = OIT.NeedsZBufferEighthRes;
		bool flag = OIT.NeedsZBufferQuarterRes || needsZBufferEighthRes;
		bool flag2 = OIT.NeedsZBufferHalfRes || flag || _lightResolution != LightingResolution.FULL;
		bool flag3 = _requestLinearZ || _requestDownsampledLinearZ || UseLinearZForLighting;
		bool flag4 = _requestDownsampledLinearZ || (flag3 && flag2);
		_hasDownsampledZ = flag2;
		if (flag3 || flag2 || flag4 || flag || needsZBufferEighthRes)
		{
			_gl.Disable(GL.STENCIL_TEST);
			_gl.Disable(GL.DEPTH_TEST);
			if (flag3)
			{
				Profiling.StartMeasure(_renderingProfileLinearZ);
				RenderTarget hardwareZ = _renderTargetStore.HardwareZ;
				RenderTarget linearZ = _renderTargetStore.LinearZ;
				GenerateLinearZ(hardwareZ, linearZ);
				Profiling.StopMeasure(_renderingProfileLinearZ);
			}
			else
			{
				Profiling.SkipMeasure(_renderingProfileLinearZ);
			}
			if (flag4)
			{
				Profiling.StartMeasure(_renderingProfileLinearZDownsample);
				bool setupViewport = true;
				RenderTarget linearZ2 = _renderTargetStore.LinearZ;
				RenderTarget linearZHalfRes = _renderTargetStore.LinearZHalfRes;
				ZDownsampleProgram.DownsamplingMode mode = ZDownsampleProgram.DownsamplingMode.Z_MIN_MAX;
				DownsampleLinearZ(setupViewport, linearZ2, linearZHalfRes, mode);
				Profiling.StopMeasure(_renderingProfileLinearZDownsample);
			}
			else
			{
				Profiling.SkipMeasure(_renderingProfileLinearZDownsample);
			}
			if (flag2 || flag || needsZBufferEighthRes)
			{
				Profiling.StartMeasure(_renderingProfileZDownsample);
				_gl.Enable(GL.DEPTH_TEST);
				_gl.DepthMask(write: true);
				_gl.DepthFunc(GL.ALWAYS);
				ZDownsampleProgram.DownsamplingMode mode2 = ZDownsampleProgram.DownsamplingMode.Z_MIN;
				ZDownsampleProgram.DownsamplingMode mode3 = ZDownsampleProgram.DownsamplingMode.Z_MIN;
				ZDownsampleProgram.DownsamplingMode mode4 = ZDownsampleProgram.DownsamplingMode.Z_MIN;
				if (flag2)
				{
					DownsampleZBuffer(setupViewport: true, _renderTargetStore.HardwareZ, _renderTargetStore.HardwareZHalfRes, mode2);
				}
				if (flag)
				{
					DownsampleZBuffer(setupViewport: true, _renderTargetStore.HardwareZHalfRes, _renderTargetStore.HardwareZQuarterRes, mode3);
				}
				if (needsZBufferEighthRes)
				{
					DownsampleZBuffer(setupViewport: true, _renderTargetStore.HardwareZQuarterRes, _renderTargetStore.HardwareZEighthRes, mode4);
				}
				_gl.DepthFunc(GL.LEQUAL);
				_gl.DepthMask(write: false);
				Profiling.StopMeasure(_renderingProfileZDownsample);
			}
			else
			{
				Profiling.SkipMeasure(_renderingProfileZDownsample);
			}
			if (!flag2)
			{
				_gl.Enable(GL.DEPTH_TEST);
			}
			_gl.Enable(GL.STENCIL_TEST);
		}
		else
		{
			Profiling.SkipMeasure(_renderingProfileLinearZ);
			Profiling.SkipMeasure(_renderingProfileLinearZDownsample);
			Profiling.SkipMeasure(_renderingProfileZDownsample);
		}
		int num = (UseClusteredLighting ? ClusteredLighting.LightCount : ClassicDeferredLighting.LightCount);
		if ((num > 0 && _lightResolution == LightingResolution.MIXED) || OIT.HasHalfResPass || OIT.HasQuarterResPass)
		{
			Profiling.StartMeasure(_renderingProfileEdgeDetection);
			_gl.Disable(GL.DEPTH_TEST);
			_renderTargetStore.Edges.Bind(clear: true, setupViewport: true);
			int inputDownscaleFactor = (OIT.HasQuarterResPass ? 4 : 2);
			TagEdges(7, inputDownscaleFactor);
			_renderTargetStore.Edges.Unbind();
			_gl.Enable(GL.DEPTH_TEST);
			Profiling.StopMeasure(_renderingProfileEdgeDetection);
		}
		else
		{
			Profiling.SkipMeasure(_renderingProfileEdgeDetection);
		}
	}

	public void TagEdges(byte writeStencilBitId = 7, int inputDownscaleFactor = 1)
	{
		Debug.Assert(inputDownscaleFactor == 1 || inputDownscaleFactor == 2 || inputDownscaleFactor == 4);
		Debug.Assert(writeStencilBitId < 8, $"Invalid stencil bit id requested for Edges: {writeStencilBitId}. Valide entries are[0-7].");
		_gl.AssertEnabled(GL.STENCIL_TEST);
		_gl.AssertDisabled(GL.DEPTH_TEST);
		_gl.AssertDepthMask(write: false);
		bool useLinearZ = _gpuProgramStore.EdgeDetectionProgram.UseLinearZ;
		GLTexture texture;
		Vector2 value;
		switch (inputDownscaleFactor)
		{
		case 4:
			texture = _renderTargetStore.HardwareZQuarterRes.GetTexture(RenderTarget.Target.Depth);
			value = _renderTargetStore.HardwareZQuarterRes.InvResolution;
			break;
		case 2:
			texture = (useLinearZ ? _renderTargetStore.LinearZHalfRes.GetTexture(RenderTarget.Target.Color0) : _renderTargetStore.HardwareZHalfRes.GetTexture(RenderTarget.Target.Depth));
			value = (useLinearZ ? _renderTargetStore.LinearZHalfRes.InvResolution : _renderTargetStore.HardwareZHalfRes.InvResolution);
			break;
		default:
			texture = (useLinearZ ? _renderTargetStore.LinearZ.GetTexture(RenderTarget.Target.Color0) : _renderTargetStore.HardwareZ.GetTexture(RenderTarget.Target.Depth));
			value = (useLinearZ ? _renderTargetStore.LinearZ.InvResolution : _renderTargetStore.HardwareZ.InvResolution);
			break;
		}
		uint mask = (uint)(1 << (int)writeStencilBitId);
		_gl.StencilFunc(GL.ALWAYS, 1 << (int)writeStencilBitId, mask);
		_gl.StencilMask(mask);
		_gl.StencilOp(GL.KEEP, GL.KEEP, GL.REPLACE);
		_gl.BindTexture(GL.TEXTURE_2D, texture);
		EdgeDetectionProgram edgeDetectionProgram = _gpuProgramStore.EdgeDetectionProgram;
		_gl.UseProgram(edgeDetectionProgram);
		edgeDetectionProgram.InvDepthTextureSize.SetValue(value);
		if (useLinearZ)
		{
			edgeDetectionProgram.FarClip.SetValue(1024f);
		}
		else
		{
			edgeDetectionProgram.ProjectionMatrix.SetValue(ref Data.ProjectionMatrix);
		}
		_graphics.ScreenTriangleRenderer.Draw();
		_gl.ColorMask(red: true, green: true, blue: true, alpha: true);
		_gl.StencilMask(255u);
	}

	public void DrawSSAO()
	{
		_renderTargetStore.SSAORaw.Bind(clear: true, setupViewport: true);
		_gl.ActiveTexture(GL.TEXTURE4);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.DeferredShadow.GetTexture(RenderTarget.Target.Color0));
		_gl.ActiveTexture(GL.TEXTURE3);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.BlurSSAOAndShadow.GetTexture(RenderTarget.Target.Color0));
		_gl.ActiveTexture(GL.TEXTURE2);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.GBuffer.GetTexture(RenderTarget.Target.Color0));
		_gl.ActiveTexture(GL.TEXTURE1);
		_gl.BindTexture(GL.TEXTURE_2D, _ssaoTapsSource.GetTexture(RenderTarget.Target.Color0));
		_gl.ActiveTexture(GL.TEXTURE0);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.LinearZ.GetTexture(RenderTarget.Target.Color0));
		SSAOProgram sSAOProgram = _gpuProgramStore.SSAOProgram;
		_gl.UseProgram(sSAOProgram);
		sSAOProgram.PackedParameters.SetValue(_ssaoPackedParameters);
		sSAOProgram.ViewportSize.SetValue(Data.ViewportSize.X, Data.ViewportSize.Y);
		sSAOProgram.ViewMatrix.SetValue(ref Data.ViewRotationMatrix);
		sSAOProgram.ProjectionMatrix.SetValue(ref Data.ProjectionMatrix);
		sSAOProgram.ReprojectMatrix.SetValue(ref Data.ReprojectFromCurrentViewToPreviousProjectionMatrix);
		sSAOProgram.FarCorners.SetValue(Data.FrustumFarCornersVS);
		sSAOProgram.SamplesData.SetValue(_ssaoSamplesData);
		sSAOProgram.TemporalSampleOffset.SetValue(_ssaoTemporalSampleOffset);
		_graphics.ScreenTriangleRenderer.Draw();
		_renderTargetStore.SSAORaw.Unbind();
	}

	public void BlurSSAOAndShadow()
	{
		BlurProgram blurSSAOAndShadowProgram = _gpuProgramStore.BlurSSAOAndShadowProgram;
		_renderTargetStore.BlurSSAOAndShadowTmp.Bind(clear: true, setupViewport: false);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.SSAORaw.GetTexture(RenderTarget.Target.Color0));
		_gl.UseProgram(blurSSAOAndShadowProgram);
		blurSSAOAndShadowProgram.PixelSize.SetValue(1f / (float)_renderTargetStore.BlurSSAOAndShadow.Width, 1f / (float)_renderTargetStore.BlurSSAOAndShadow.Height);
		blurSSAOAndShadowProgram.BlurScale.SetValue(1f);
		blurSSAOAndShadowProgram.HorizontalPass.SetValue(1f);
		_graphics.ScreenTriangleRenderer.DrawRaw();
		_renderTargetStore.BlurSSAOAndShadowTmp.Unbind();
		_renderTargetStore.BlurSSAOAndShadow.Bind(clear: false, setupViewport: false);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.BlurSSAOAndShadowTmp.GetTexture(RenderTarget.Target.Color0));
		blurSSAOAndShadowProgram.HorizontalPass.SetValue(0f);
		_graphics.ScreenTriangleRenderer.DrawRaw();
		_renderTargetStore.BlurSSAOAndShadow.Unbind();
	}

	public void ApplyDeferred(GLTexture topProjectionTexture, GLTexture fogNoiseTexture)
	{
		GLFunctions gL = _graphics.GL;
		_gl.StencilFunc(GL.ALWAYS, 0, 255u);
		_gl.ActiveTexture(GL.TEXTURE6);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.DeferredShadow.GetTexture(RenderTarget.Target.Color0));
		DeferredProgram deferredProgram = _gpuProgramStore.DeferredProgram;
		if (deferredProgram.UseMoodFog)
		{
			_gl.ActiveTexture(GL.TEXTURE7);
			_gl.BindTexture(GL.TEXTURE_2D, fogNoiseTexture);
		}
		_gl.ActiveTexture(GL.TEXTURE4);
		_gl.BindTexture(GL.TEXTURE_2D, topProjectionTexture);
		RenderTarget renderTarget = (UseSSAOBlur ? _renderTargetStore.BlurSSAOAndShadow : _renderTargetStore.SSAORaw);
		_gl.ActiveTexture(GL.TEXTURE3);
		if (deferredProgram.UseSmartUpsampling)
		{
			_gl.BindSampler(3u, _pointSampler);
		}
		_gl.BindTexture(GL.TEXTURE_2D, renderTarget.GetTexture(RenderTarget.Target.Color0));
		GLTexture texture = (_graphics.UseLinearZ ? _renderTargetStore.LinearZ.GetTexture(RenderTarget.Target.Color0) : _renderTargetStore.GBuffer.GetTexture(RenderTarget.Target.Depth));
		_gl.ActiveTexture(GL.TEXTURE2);
		_gl.BindTexture(GL.TEXTURE_2D, texture);
		_gl.ActiveTexture(GL.TEXTURE1);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.LightBufferFullRes.GetTexture(RenderTarget.Target.Color0));
		_gl.ActiveTexture(GL.TEXTURE0);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.GBuffer.GetTexture(RenderTarget.Target.Color0));
		deferredProgram.SceneDataBlock.SetBuffer(SceneDataBuffer);
		_gl.UseProgram(deferredProgram);
		if (_graphics.UseLinearZ)
		{
			deferredProgram.FarCorners.SetValue(Data.FrustumFarCornersWS);
		}
		if (deferredProgram.DebugShadowCascades)
		{
			deferredProgram.DebugShadowMatrix.SetValue(Data.SunShadowRenderData.VirtualSunViewRotationProjectionMatrix);
		}
		_graphics.ScreenTriangleRenderer.Draw();
		if (deferredProgram.UseSmartUpsampling)
		{
			_gl.BindSampler(3u, GLSampler.None);
		}
	}

	public void BlitSceneColorToHalfRes(RenderTarget sceneColor, GL filteringMode = GL.LINEAR, bool generateMipMap = false, bool bindSource = false, bool rebindSourceAfter = true)
	{
		sceneColor.CopyColorTo(_renderTargetStore.SceneColorHalfRes, GL.COLOR_ATTACHMENT0, GL.COLOR_ATTACHMENT0, filteringMode, bindSource, rebindSourceAfter);
		if (generateMipMap)
		{
			_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.SceneColorHalfRes.GetTexture(RenderTarget.Target.Color0));
			_gl.GenerateMipmap(GL.TEXTURE_2D);
		}
	}

	public void GenerateLinearZ(RenderTarget source, RenderTarget destination)
	{
		_gl.AssertDisabled(GL.STENCIL_TEST);
		_gl.AssertDisabled(GL.DEPTH_TEST);
		_gl.AssertDepthMask(write: false);
		destination.Bind(clear: false, setupViewport: false);
		LinearZProgram linearZProgram = _gpuProgramStore.LinearZProgram;
		_gl.UseProgram(linearZProgram);
		linearZProgram.ProjectionMatrix.SetValue(ref Data.ProjectionMatrix);
		linearZProgram.InvFarClip.SetValue(0.0009765625f);
		_gl.BindTexture(GL.TEXTURE_2D, source.GetTexture(RenderTarget.Target.Depth));
		_graphics.ScreenTriangleRenderer.Draw();
		destination.Unbind();
	}

	public void DownsampleZBuffer(bool setupViewport, RenderTarget source, RenderTarget destination, ZDownsampleProgram.DownsamplingMode mode)
	{
		_gl.AssertDisabled(GL.STENCIL_TEST);
		_gl.AssertEnabled(GL.DEPTH_TEST);
		_gl.AssertDepthFunc(GL.ALWAYS);
		_gl.AssertDepthMask(write: true);
		destination.Bind(clear: false, setupViewport);
		_gl.BindTexture(GL.TEXTURE_2D, source.GetTexture(RenderTarget.Target.Depth));
		ZDownsampleProgram zDownsampleProgram = _gpuProgramStore.ZDownsampleProgram;
		_gl.UseProgram(zDownsampleProgram);
		zDownsampleProgram.Mode.SetValue((int)mode);
		zDownsampleProgram.PixelSize.SetValue(source.InvWidth, source.InvHeight);
		_graphics.ScreenTriangleRenderer.Draw();
		destination.Unbind();
	}

	public void DownsampleLinearZ(bool setupViewport, RenderTarget source, RenderTarget destination, ZDownsampleProgram.DownsamplingMode mode)
	{
		_gl.AssertDisabled(GL.STENCIL_TEST);
		_gl.AssertDisabled(GL.DEPTH_TEST);
		destination.Bind(clear: false, setupViewport);
		_gl.BindTexture(GL.TEXTURE_2D, source.GetTexture(RenderTarget.Target.Color0));
		ZDownsampleProgram linearZDownsampleProgram = _gpuProgramStore.LinearZDownsampleProgram;
		_gl.UseProgram(linearZDownsampleProgram);
		linearZDownsampleProgram.Mode.SetValue((int)mode);
		linearZDownsampleProgram.PixelSize.SetValue(source.InvWidth, source.InvHeight);
		_graphics.ScreenTriangleRenderer.Draw();
		destination.Unbind();
	}

	public void SetupModelVFXDataTexture(uint unitId)
	{
		_gl.ActiveTexture((GL)(33984 + unitId));
		_gl.BindTexture(GL.TEXTURE_BUFFER, _modelVFXDataBufferTexture.CurrentTexture);
	}

	public void InitModelVFXGPUData()
	{
		_modelVFXDataBufferTexture.CreateStorage(GL.RGBA32F, GL.STREAM_DRAW, useDoubleBuffering: true, _modelVFXBufferSize, 200u, GPUBuffer.GrowthPolicy.GrowthAutoNoLimit);
	}

	public void DisposeModelVFXGPUData()
	{
		_modelVFXDataBufferTexture.DestroyStorage();
	}

	public unsafe void SendModelVFXDataToGPU()
	{
		uint num = (uint)_modelVFXDrawTaskCount * ModelVFXDataSize;
		if (num != 0)
		{
			_modelVFXDataBufferTexture.GrowStorageIfNecessary(num);
			IntPtr pointer = _modelVFXDataBufferTexture.BeginTransfer(num);
			for (int i = 0; i < _modelVFXDrawTaskCount; i++)
			{
				Vector4* ptr = (Vector4*)IntPtr.Add(pointer, i * (int)ModelVFXDataSize).ToPointer();
				*ptr = new Vector4(_modelVFXDrawTasks[i].ModelVFXHighlightColor.X, _modelVFXDrawTasks[i].ModelVFXHighlightColor.Y, _modelVFXDrawTasks[i].ModelVFXHighlightColor.Z, _modelVFXDrawTasks[i].ModelVFXHighlightThickness);
				ptr[1] = new Vector4(_modelVFXDrawTasks[i].ModelVFXNoiseScale.X, _modelVFXDrawTasks[i].ModelVFXNoiseScale.Y, _modelVFXDrawTasks[i].ModelVFXNoiseScrollSpeed.X, _modelVFXDrawTasks[i].ModelVFXNoiseScrollSpeed.Y);
				ptr[2] = new Vector4(_modelVFXDrawTasks[i].ModelVFXPostColor.X, _modelVFXDrawTasks[i].ModelVFXPostColor.Y, _modelVFXDrawTasks[i].ModelVFXPostColor.Z, _modelVFXDrawTasks[i].ModelVFXPostColor.W);
				ptr[3] = new Vector4(_modelVFXDrawTasks[i].ModelVFXPackedParams, 0f, 0f, 0f);
			}
			_modelVFXDataBufferTexture.EndTransfer();
		}
	}

	public void SetupEntityDataTexture(uint unitId)
	{
		_gl.ActiveTexture((GL)(33984 + unitId));
		_gl.BindTexture(GL.TEXTURE_BUFFER, _entityDataBufferTexture.CurrentTexture);
	}

	private void InitEntitiesGPUData()
	{
		_entityDataBufferTexture.CreateStorage(GL.RGBA32F, GL.STREAM_DRAW, useDoubleBuffering: true, _entityBufferSize, 1024u, GPUBuffer.GrowthPolicy.GrowthAutoNoLimit);
	}

	private void DisposeEntitiesGPUData()
	{
		_entityDataBufferTexture.DestroyStorage();
	}

	private void PingPongEntityDataBuffers()
	{
		_entityDataBufferTexture.Swap();
	}

	public unsafe void SendEntityDataToGPU()
	{
		uint num = (uint)((_entityDrawTaskCount + _entityForwardDrawTaskCount) * GPUEntityDataSize);
		uint num2 = (uint)(_entityDistortionDrawTaskCount * GPUEntityDistortionDataSize);
		uint num3 = num + num2;
		if (num3 != 0)
		{
			_entityDataBufferTexture.GrowStorageIfNecessary(num3);
			IntPtr pointer = _entityDataBufferTexture.BeginTransfer(num3);
			for (int i = 0; i < _entityDrawTaskCount; i++)
			{
				IntPtr pointer2 = IntPtr.Add(pointer, i * GPUEntityDataSize);
				Matrix* ptr = (Matrix*)pointer2.ToPointer();
				*ptr = _entityDrawTasks[i].ModelMatrix;
				Vector4* ptr2 = (Vector4*)IntPtr.Add(pointer2, sizeof(Matrix)).ToPointer();
				*ptr2 = _entityDrawTasks[i].BlockLightColor;
				ptr2[1] = new Vector4(_entityDrawTasks[i].BottomTint.X, _entityDrawTasks[i].BottomTint.Y, _entityDrawTasks[i].BottomTint.Z, _entityDrawTasks[i].ModelVFXAnimationProgress);
				ptr2[2] = new Vector4(_entityDrawTasks[i].TopTint.X, _entityDrawTasks[i].TopTint.Y, _entityDrawTasks[i].TopTint.Z, _entityDrawTasks[i].ModelVFXId);
				ptr2[3] = new Vector4(_entityDrawTasks[i].InvModelHeight, _entityDrawTasks[i].UseDithering, 0f, 0f);
			}
			for (int j = 0; j < _entityForwardDrawTaskCount; j++)
			{
				IntPtr pointer3 = IntPtr.Add(pointer, (j + _entityDrawTaskCount) * GPUEntityDataSize);
				Matrix* ptr3 = (Matrix*)pointer3.ToPointer();
				*ptr3 = _entityForwardDrawTasks[j].ModelMatrix;
				Vector4* ptr4 = (Vector4*)IntPtr.Add(pointer3, sizeof(Matrix)).ToPointer();
				*ptr4 = _entityForwardDrawTasks[j].BlockLightColor;
				ptr4[1] = new Vector4(_entityForwardDrawTasks[j].BottomTint.X, _entityForwardDrawTasks[j].BottomTint.Y, _entityForwardDrawTasks[j].BottomTint.Z, _entityForwardDrawTasks[j].ModelVFXAnimationProgress);
				ptr4[2] = new Vector4(_entityForwardDrawTasks[j].TopTint.X, _entityForwardDrawTasks[j].TopTint.Y, _entityForwardDrawTasks[j].TopTint.Z, _entityForwardDrawTasks[j].ModelVFXId);
				ptr4[3] = new Vector4(_entityForwardDrawTasks[j].InvModelHeight, _entityForwardDrawTasks[j].UseDithering, 0f, 0f);
			}
			pointer = IntPtr.Add(pointer, (int)num);
			for (int k = 0; k < _entityDistortionDrawTaskCount; k++)
			{
				IntPtr pointer4 = IntPtr.Add(pointer, k * GPUEntityDistortionDataSize);
				Matrix* ptr5 = (Matrix*)pointer4.ToPointer();
				*ptr5 = _entityDistortionDrawTasks[k].ModelMatrix;
				Vector4* ptr6 = (Vector4*)IntPtr.Add(pointer4, sizeof(Matrix)).ToPointer();
				*ptr6 = new Vector4(_entityDistortionDrawTasks[k].ModelVFXAnimationProgress, _entityDistortionDrawTasks[k].ModelVFXId, _entityDistortionDrawTasks[k].InvModelHeight, 0f);
			}
			_entityDataBufferTexture.EndTransfer();
		}
	}

	protected void InitEntityRendering()
	{
		BasicFogProgram basicFogProgram = _gpuProgramStore.BasicFogProgram;
		BasicProgram basicProgram = _gpuProgramStore.BasicProgram;
		_quadRenderer = new QuadRenderer(_graphics, basicFogProgram.AttribPosition, basicFogProgram.AttribTexCoords);
		_boxRenderer = new BoxRenderer(_graphics, basicProgram);
		_lineRenderer = new LineRenderer(_graphics, basicProgram);
		_lineRenderer.UpdateLineData(new Vector3[2]
		{
			new Vector3(0f, 0f, 0f),
			new Vector3(1f, 0f, 0f)
		});
		InitEntitiesGPUData();
		InitModelVFXGPUData();
	}

	protected void DisposeEntityRendering()
	{
		_lineRenderer.Dispose();
		_boxRenderer.Dispose();
		_quadRenderer.Dispose();
		DisposeEntitiesGPUData();
		DisposeModelVFXGPUData();
	}

	private void ResetEntityCounters()
	{
		_entityDrawTaskCount = 0;
		_entityForwardDrawTaskCount = 0;
		_entityDistortionDrawTaskCount = 0;
		_modelVFXDrawTaskCount = 0;
		_incomingEntityDrawTaskCount = 0;
		_nameplateDrawTaskCount = 0;
		_debugInfoDrawTaskCount = 0;
	}

	public void PrepareForIncomingEntityDrawTasks(int size)
	{
		_incomingEntityDrawTaskCount += size;
		ArrayUtils.GrowArrayIfNecessary(ref _entityDrawTasks, _incomingEntityDrawTaskCount, 200);
		ArrayUtils.GrowArrayIfNecessary(ref _entityForwardDrawTasks, _incomingEntityDrawTaskCount, 200);
		ArrayUtils.GrowArrayIfNecessary(ref _entityDistortionDrawTasks, _incomingEntityDrawTaskCount, 200);
		ArrayUtils.GrowArrayIfNecessary(ref _modelVFXDrawTasks, _incomingEntityDrawTaskCount, 200);
	}

	public void RegisterEntityDrawTasks(int entityLocalId, ref Matrix modelMatrix, GLVertexArray vertexArray, int dataCount, GLBuffer animationData, uint animationDataOffset, ushort animationDataCount, Vector4 blockLightColor, Vector3 bottomTint, Vector3 topTint, float modelHeight, bool useDithering, float modelVFXAnimationProgress, int packedModelVFXParams, int modelVFXId)
	{
		if (useDithering)
		{
			int entityForwardDrawTaskCount = _entityForwardDrawTaskCount;
			_entityForwardDrawTasks[entityForwardDrawTaskCount].BlockLightColor = blockLightColor;
			_entityForwardDrawTasks[entityForwardDrawTaskCount].BottomTint = bottomTint;
			_entityForwardDrawTasks[entityForwardDrawTaskCount].TopTint = topTint;
			_entityForwardDrawTasks[entityForwardDrawTaskCount].InvModelHeight = 1f / modelHeight;
			_entityForwardDrawTasks[entityForwardDrawTaskCount].ModelMatrix = modelMatrix;
			_entityForwardDrawTasks[entityForwardDrawTaskCount].VertexArray = vertexArray;
			_entityForwardDrawTasks[entityForwardDrawTaskCount].DataCount = dataCount;
			_entityForwardDrawTasks[entityForwardDrawTaskCount].AnimationData = animationData;
			_entityForwardDrawTasks[entityForwardDrawTaskCount].AnimationDataOffset = animationDataOffset;
			_entityForwardDrawTasks[entityForwardDrawTaskCount].AnimationDataSize = (ushort)(animationDataCount * 64);
			_entityForwardDrawTasks[entityForwardDrawTaskCount].ModelVFXAnimationProgress = modelVFXAnimationProgress;
			_entityForwardDrawTasks[entityForwardDrawTaskCount].ModelVFXId = modelVFXId;
			_entityForwardDrawTasks[entityForwardDrawTaskCount].UseDithering = (useDithering ? 1f : 0f);
			_entityForwardDrawTasks[entityForwardDrawTaskCount].EntityLocalId = (ushort)entityLocalId;
			_entityForwardDrawTaskCount++;
		}
		else
		{
			int entityDrawTaskCount = _entityDrawTaskCount;
			_entityDrawTasks[entityDrawTaskCount].BlockLightColor = blockLightColor;
			_entityDrawTasks[entityDrawTaskCount].BottomTint = bottomTint;
			_entityDrawTasks[entityDrawTaskCount].TopTint = topTint;
			_entityDrawTasks[entityDrawTaskCount].InvModelHeight = 1f / modelHeight;
			_entityDrawTasks[entityDrawTaskCount].ModelMatrix = modelMatrix;
			_entityDrawTasks[entityDrawTaskCount].VertexArray = vertexArray;
			_entityDrawTasks[entityDrawTaskCount].DataCount = dataCount;
			_entityDrawTasks[entityDrawTaskCount].AnimationData = animationData;
			_entityDrawTasks[entityDrawTaskCount].AnimationDataOffset = animationDataOffset;
			_entityDrawTasks[entityDrawTaskCount].AnimationDataSize = (ushort)(animationDataCount * 64);
			_entityDrawTasks[entityDrawTaskCount].ModelVFXAnimationProgress = modelVFXAnimationProgress;
			_entityDrawTasks[entityDrawTaskCount].ModelVFXId = modelVFXId;
			_entityDrawTasks[entityDrawTaskCount].UseDithering = (useDithering ? 1f : 0f);
			_entityDrawTasks[entityDrawTaskCount].EntityLocalId = (ushort)entityLocalId;
			_entityDrawTaskCount++;
		}
		int num = (packedModelVFXParams >> 3) & 3;
		int entityDistortionDrawTaskCount = _entityDistortionDrawTaskCount;
		if (modelVFXAnimationProgress != 0f && num == 2)
		{
			_entityDistortionDrawTasks[entityDistortionDrawTaskCount].InvModelHeight = 1f / modelHeight;
			_entityDistortionDrawTasks[entityDistortionDrawTaskCount].ModelMatrix = modelMatrix;
			_entityDistortionDrawTasks[entityDistortionDrawTaskCount].VertexArray = vertexArray;
			_entityDistortionDrawTasks[entityDistortionDrawTaskCount].DataCount = dataCount;
			_entityDistortionDrawTasks[entityDistortionDrawTaskCount].AnimationData = animationData;
			_entityDistortionDrawTasks[entityDistortionDrawTaskCount].AnimationDataOffset = animationDataOffset;
			_entityDistortionDrawTasks[entityDistortionDrawTaskCount].AnimationDataSize = (ushort)(animationDataCount * 64);
			_entityDistortionDrawTasks[entityDistortionDrawTaskCount].ModelVFXAnimationProgress = modelVFXAnimationProgress;
			_entityDistortionDrawTasks[entityDistortionDrawTaskCount].ModelVFXId = modelVFXId;
			_entityDistortionDrawTasks[entityDistortionDrawTaskCount].EntityLocalId = (ushort)entityLocalId;
			_entityDistortionDrawTaskCount++;
		}
	}

	public int RegisterModelVFXTask(float modelVFXAnimationProgress, Vector3 modelVFXHighlightColor, float modelVFXHighlightThickness, Vector2 modelVFXNoiseScale, Vector2 modelVFXNoiseScrollSpeed, int packedModelVFXParams, Vector4 modelVFXPostColor, int entityTaskId = 0, int distortionTaskId = 0, int shadowTaskId = 0)
	{
		int num = -1;
		if (modelVFXAnimationProgress != 0f)
		{
			num = _modelVFXDrawTaskCount;
			_modelVFXDrawTasks[num].ModelVFXHighlightColor = modelVFXHighlightColor;
			_modelVFXDrawTasks[num].ModelVFXHighlightThickness = modelVFXHighlightThickness;
			_modelVFXDrawTasks[num].ModelVFXNoiseScale = modelVFXNoiseScale;
			_modelVFXDrawTasks[num].ModelVFXNoiseScrollSpeed = modelVFXNoiseScrollSpeed;
			_modelVFXDrawTasks[num].ModelVFXPackedParams = packedModelVFXParams;
			_modelVFXDrawTasks[num].ModelVFXPostColor = modelVFXPostColor;
			_modelVFXDrawTaskCount++;
		}
		return num;
	}

	public void RegisterEntityNameplateDrawTask(int entityLocalId, ref Matrix mvpMatrix, Vector3 position, float fillBlurThreshold, GLVertexArray vertexArray, ushort dataCount)
	{
		ArrayUtils.GrowArrayIfNecessary(ref _nameplateDrawTasks, _nameplateDrawTaskCount, 50);
		int nameplateDrawTaskCount = _nameplateDrawTaskCount;
		_nameplateDrawTasks[nameplateDrawTaskCount].FillBlurThreshold = fillBlurThreshold;
		_nameplateDrawTasks[nameplateDrawTaskCount].MVPMatrix = mvpMatrix;
		_nameplateDrawTasks[nameplateDrawTaskCount].Position = position;
		_nameplateDrawTasks[nameplateDrawTaskCount].VertexArray = vertexArray;
		_nameplateDrawTasks[nameplateDrawTaskCount].DataCount = dataCount;
		_nameplateDrawTasks[nameplateDrawTaskCount].EntityLocalId = (ushort)entityLocalId;
		_nameplateDrawTaskCount++;
	}

	public void RegisterEntityDebugDrawTask(bool hit, bool renderCollision, bool collided, int levelOfDetail, ref Matrix lineSightMVPMatrix, ref Matrix headMVPMatrix, ref Matrix boxMVPMatrix, ref Matrix sphereMVPMatrix, ref Matrix boxCollisionMatrix, ref Matrix cylinderCollisionMatrix, ref Matrix lineRepulsionMVPMatrix, DebugInfoDetailTask[] detailTasks)
	{
		ArrayUtils.GrowArrayIfNecessary(ref _debugInfoDrawTasks, _debugInfoDrawTaskCount, 50);
		int debugInfoDrawTaskCount = _debugInfoDrawTaskCount;
		_debugInfoDrawTasks[debugInfoDrawTaskCount].LineSightMVPMatrix = lineSightMVPMatrix;
		_debugInfoDrawTasks[debugInfoDrawTaskCount].BoxHeadMVPMatrix = headMVPMatrix;
		_debugInfoDrawTasks[debugInfoDrawTaskCount].BoxMVPMatrix = boxMVPMatrix;
		_debugInfoDrawTasks[debugInfoDrawTaskCount].SphereMVPMatrix = sphereMVPMatrix;
		_debugInfoDrawTasks[debugInfoDrawTaskCount].BoxCollisionMatrix = boxCollisionMatrix;
		_debugInfoDrawTasks[debugInfoDrawTaskCount].CylinderCollisionMatrix = cylinderCollisionMatrix;
		_debugInfoDrawTasks[debugInfoDrawTaskCount].LineRepulsionMVPMatrix = lineRepulsionMVPMatrix;
		switch (levelOfDetail)
		{
		case 0:
			_debugInfoDrawTasks[debugInfoDrawTaskCount].SphereColor = new Vector3(1f, 0f, 0f);
			break;
		case 1:
			_debugInfoDrawTasks[debugInfoDrawTaskCount].SphereColor = new Vector3(0f, 1f, 0f);
			break;
		case 2:
			_debugInfoDrawTasks[debugInfoDrawTaskCount].SphereColor = new Vector3(0f, 0f, 1f);
			break;
		case 3:
			_debugInfoDrawTasks[debugInfoDrawTaskCount].SphereColor = new Vector3(1f, 1f, 1f);
			break;
		}
		_debugInfoDrawTasks[debugInfoDrawTaskCount].Hit = hit;
		_debugInfoDrawTasks[debugInfoDrawTaskCount].RenderCollision = renderCollision;
		_debugInfoDrawTasks[debugInfoDrawTaskCount].Collided = collided;
		_debugInfoDrawTasks[debugInfoDrawTaskCount].DetailTasks = detailTasks;
		_debugInfoDrawTaskCount++;
	}

	public void DrawForwardEntity(Vector2 atlasSizeFactor0, Vector2 atlasSizeFactor1, Vector2 atlasSizeFactor2)
	{
		BlockyModelProgram blockyModelDitheringProgram = _gpuProgramStore.BlockyModelDitheringProgram;
		blockyModelDitheringProgram.AssertInUse();
		GLFunctions gL = _graphics.GL;
		blockyModelDitheringProgram.AtlasSizeFactor0.SetValue(atlasSizeFactor0);
		blockyModelDitheringProgram.AtlasSizeFactor1.SetValue(atlasSizeFactor1);
		blockyModelDitheringProgram.AtlasSizeFactor2.SetValue(atlasSizeFactor2);
		for (int i = 0; i < _entityForwardDrawTaskCount; i++)
		{
			blockyModelDitheringProgram.DrawId.SetValue(0, i + _entityDrawTaskCount);
			blockyModelDitheringProgram.NodeBlock.SetBufferRange(_entityForwardDrawTasks[i].AnimationData, _entityForwardDrawTasks[i].AnimationDataOffset, _entityForwardDrawTasks[i].AnimationDataSize);
			gL.BindVertexArray(_entityForwardDrawTasks[i].VertexArray);
			gL.DrawElements(GL.TRIANGLES, _entityForwardDrawTasks[i].DataCount, GL.UNSIGNED_SHORT, (IntPtr)0);
		}
	}

	public void DrawEntityCharactersAndItems(bool useOcclusionCulling = false)
	{
		BlockyModelProgram blockyModelProgram = _gpuProgramStore.BlockyModelProgram;
		blockyModelProgram.AssertInUse();
		GLFunctions gL = _graphics.GL;
		int entityOccludeesOffset = EntityOccludeesOffset;
		if (useOcclusionCulling)
		{
			for (int i = 0; i < _entityDrawTaskCount; i++)
			{
				if (VisibleOccludees[entityOccludeesOffset + _entityDrawTasks[i].EntityLocalId] == 1)
				{
					blockyModelProgram.DrawId.SetValue(0, i);
					blockyModelProgram.NodeBlock.SetBufferRange(_entityDrawTasks[i].AnimationData, _entityDrawTasks[i].AnimationDataOffset, _entityDrawTasks[i].AnimationDataSize);
					gL.BindVertexArray(_entityDrawTasks[i].VertexArray);
					gL.DrawElements(GL.TRIANGLES, _entityDrawTasks[i].DataCount, GL.UNSIGNED_SHORT, (IntPtr)0);
				}
			}
		}
		else
		{
			for (int j = 0; j < _entityDrawTaskCount; j++)
			{
				blockyModelProgram.DrawId.SetValue(0, j);
				blockyModelProgram.NodeBlock.SetBufferRange(_entityDrawTasks[j].AnimationData, _entityDrawTasks[j].AnimationDataOffset, _entityDrawTasks[j].AnimationDataSize);
				gL.BindVertexArray(_entityDrawTasks[j].VertexArray);
				gL.DrawElements(GL.TRIANGLES, _entityDrawTasks[j].DataCount, GL.UNSIGNED_SHORT, (IntPtr)0);
			}
		}
	}

	public void DrawEntityDistortion(bool useOcclusionCulling = false)
	{
		BlockyModelProgram blockyModelDistortionProgram = _gpuProgramStore.BlockyModelDistortionProgram;
		blockyModelDistortionProgram.AssertInUse();
		GLFunctions gL = _graphics.GL;
		int entityOccludeesOffset = EntityOccludeesOffset;
		if (useOcclusionCulling)
		{
			for (int i = 0; i < _entityDistortionDrawTaskCount; i++)
			{
				if (VisibleOccludees[entityOccludeesOffset + _entityDistortionDrawTasks[i].EntityLocalId] == 1)
				{
					blockyModelDistortionProgram.DrawId.SetValue(_entityDrawTaskCount + _entityForwardDrawTaskCount, i);
					blockyModelDistortionProgram.NodeBlock.SetBufferRange(_entityDistortionDrawTasks[i].AnimationData, _entityDistortionDrawTasks[i].AnimationDataOffset, _entityDistortionDrawTasks[i].AnimationDataSize);
					gL.BindVertexArray(_entityDistortionDrawTasks[i].VertexArray);
					gL.DrawElements(GL.TRIANGLES, _entityDistortionDrawTasks[i].DataCount, GL.UNSIGNED_SHORT, (IntPtr)0);
				}
			}
		}
		else
		{
			for (int j = 0; j < _entityDistortionDrawTaskCount; j++)
			{
				blockyModelDistortionProgram.DrawId.SetValue(_entityDrawTaskCount + _entityForwardDrawTaskCount, j);
				blockyModelDistortionProgram.NodeBlock.SetBufferRange(_entityDistortionDrawTasks[j].AnimationData, _entityDistortionDrawTasks[j].AnimationDataOffset, _entityDistortionDrawTasks[j].AnimationDataSize);
				gL.BindVertexArray(_entityDistortionDrawTasks[j].VertexArray);
				gL.DrawElements(GL.TRIANGLES, _entityDistortionDrawTasks[j].DataCount, GL.UNSIGNED_SHORT, (IntPtr)0);
			}
		}
	}

	public void DrawEntityNameplates(bool useOcclusionCulling = false)
	{
		TextProgram textProgram = _gpuProgramStore.TextProgram;
		textProgram.AssertInUse();
		textProgram.FillThreshold.AssertValue(0f);
		textProgram.OutlineThreshold.AssertValue(0f);
		textProgram.OutlineBlurThreshold.AssertValue(0f);
		textProgram.OutlineOffset.AssertValue(Vector2.Zero);
		textProgram.Opacity.AssertValue(1f);
		GLFunctions gL = _graphics.GL;
		int entityOccludeesOffset = EntityOccludeesOffset;
		if (useOcclusionCulling)
		{
			for (int i = 0; i < _nameplateDrawTaskCount; i++)
			{
				if (VisibleOccludees[entityOccludeesOffset + _nameplateDrawTasks[i].EntityLocalId] == 1)
				{
					textProgram.Position.SetValue(_nameplateDrawTasks[i].Position);
					textProgram.FillBlurThreshold.SetValue(_nameplateDrawTasks[i].FillBlurThreshold);
					textProgram.MVPMatrix.SetValue(ref _nameplateDrawTasks[i].MVPMatrix);
					gL.BindVertexArray(_nameplateDrawTasks[i].VertexArray);
					gL.DrawElements(GL.TRIANGLES, _nameplateDrawTasks[i].DataCount, GL.UNSIGNED_SHORT, (IntPtr)0);
				}
			}
		}
		else
		{
			for (int j = 0; j < _nameplateDrawTaskCount; j++)
			{
				textProgram.Position.SetValue(_nameplateDrawTasks[j].Position);
				textProgram.FillBlurThreshold.SetValue(_nameplateDrawTasks[j].FillBlurThreshold);
				textProgram.MVPMatrix.SetValue(ref _nameplateDrawTasks[j].MVPMatrix);
				gL.BindVertexArray(_nameplateDrawTasks[j].VertexArray);
				gL.DrawElements(GL.TRIANGLES, _nameplateDrawTasks[j].DataCount, GL.UNSIGNED_SHORT, (IntPtr)0);
			}
		}
	}

	public void DrawEntityDebugInfo()
	{
		for (int i = 0; i < _debugInfoDrawTaskCount; i++)
		{
			_lineRenderer.Draw(ref _debugInfoDrawTasks[i].LineSightMVPMatrix, _graphics.RedColor, 1f);
			_lineRenderer.Draw(ref _debugInfoDrawTasks[i].LineRepulsionMVPMatrix, _graphics.BlueColor, 1f);
			_boxRenderer.Draw(ref _debugInfoDrawTasks[i].BoxHeadMVPMatrix, _graphics.RedColor, 1f, _graphics.RedColor, 0.2f);
			Vector3 vector = (_debugInfoDrawTasks[i].Hit ? _graphics.BlueColor : _graphics.WhiteColor);
			_boxRenderer.Draw(ref _debugInfoDrawTasks[i].BoxMVPMatrix, vector, 1f, vector, 0.2f);
			if (_debugInfoDrawTasks[i].DetailTasks != null)
			{
				for (int j = 0; j < _debugInfoDrawTasks[i].DetailTasks.Length; j++)
				{
					ref DebugInfoDetailTask reference = ref _debugInfoDrawTasks[i].DetailTasks[j];
					_boxRenderer.Draw(ref reference.Matrix, reference.Color, 1f, reference.Color, 0.2f);
				}
			}
			_gpuProgramStore.BasicProgram.MVPMatrix.SetValue(ref _debugInfoDrawTasks[i].SphereMVPMatrix);
			_gpuProgramStore.BasicProgram.Opacity.SetValue(0.075f);
			_gpuProgramStore.BasicProgram.Color.SetValue(_debugInfoDrawTasks[i].SphereColor);
			_gl.BindVertexArray(_sphereLightMesh.VertexArray);
			_gl.DrawElements(GL.TRIANGLES, _sphereLightMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
			if (_debugInfoDrawTasks[i].RenderCollision)
			{
				Vector3 vector2 = (_debugInfoDrawTasks[i].Collided ? _graphics.GreenColor : _graphics.CyanColor);
				_boxRenderer.Draw(ref _debugInfoDrawTasks[i].BoxCollisionMatrix, vector2, 2f, vector2, 0.2f);
				_gpuProgramStore.BasicProgram.MVPMatrix.SetValue(ref _debugInfoDrawTasks[i].CylinderCollisionMatrix);
				_gpuProgramStore.BasicProgram.Opacity.SetValue(0.075f);
				_gpuProgramStore.BasicProgram.Color.SetValue(_graphics.BlueColor);
				_gl.BindVertexArray(_cylinderMesh.VertexArray);
				_gl.DrawElements(GL.TRIANGLES, _cylinderMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
			}
		}
	}

	private void InitLighting()
	{
		SetLinearZForLight(UseLinearZForLighting, force: true);
		MeshProcessor.CreateSphere(ref _sphereLightMesh, 5, 8, 1f, 0);
		MeshProcessor.CreateCylinder(ref _cylinderMesh, 8, 1f, 0);
		ClassicDeferredLighting = new ClassicDeferredLighting(_graphics, _renderTargetStore);
		ClassicDeferredLighting.Init();
		ClusteredLighting = new ClusteredLighting(_graphics, _renderTargetStore, Profiling);
		ClusteredLighting.Init();
	}

	private void DisposeLighting()
	{
		ClusteredLighting.Dispose();
		ClusteredLighting = null;
		ClassicDeferredLighting.Dispose();
		ClassicDeferredLighting = null;
		_sphereLightMesh.Dispose();
	}

	public void SetupLightRenderingProfiles(int profileLights, int profileLightsFullRes, int profileLightsLowRes, int profileLightsStencil, int profileLightsMix)
	{
		_renderingProfileLights = profileLights;
		_renderingProfileLightsFullRes = profileLightsFullRes;
		_renderingProfileLightsLowRes = profileLightsLowRes;
		_renderingProfileLightsStencil = profileLightsStencil;
		_renderingProfileLightsMix = profileLightsMix;
	}

	public void SetupClusteredLightingRenderingProfiles(int profileLightClusterClear, int profileLightClustering, int profileLightClusteringRefine, int profileLightFillGridData, int profileLightSendDataToGPU)
	{
		ClusteredLighting.SetupRenderingProfiles(profileLightClusterClear, profileLightClustering, profileLightClusteringRefine, profileLightFillGridData, profileLightSendDataToGPU);
	}

	public void SetLinearZForLight(bool enable, bool force = false)
	{
		if (enable != UseLinearZForLighting || force)
		{
			UseLinearZForLighting = enable;
			_graphics.UseLinearZForLight = enable;
			_graphics.UseLinearZ = enable;
			_gpuProgramStore.ResetPrograms(forceReset: true);
		}
	}

	public void SetLightBufferCompression(bool enable)
	{
		_useLBufferCompression = enable;
		RenderTargetStore rTStore = _graphics.RTStore;
		RenderTargetStore.DebugMapParam.ChromaSubsamplingMode chromaSubsamplingMode = (enable ? RenderTargetStore.DebugMapParam.ChromaSubsamplingMode.Light : RenderTargetStore.DebugMapParam.ChromaSubsamplingMode.None);
		rTStore.SetDebugMapChromaSubsamplingMode("lbuffer", chromaSubsamplingMode);
		rTStore.SetDebugMapChromaSubsamplingMode("gbuffer1_light", chromaSubsamplingMode);
		_gpuProgramStore.BlockyModelProgram.UseLightBufferCompression = enable;
		_gpuProgramStore.FirstPersonBlockyModelProgram.UseLightBufferCompression = enable;
		_gpuProgramStore.MapChunkAlphaBlendedProgram.UseLightBufferCompression = enable;
		_gpuProgramStore.MapChunkFarAlphaTestedProgram.UseLightBufferCompression = enable;
		_gpuProgramStore.MapChunkFarOpaqueProgram.UseLightBufferCompression = enable;
		_gpuProgramStore.MapChunkNearAlphaTestedProgram.UseLightBufferCompression = enable;
		_gpuProgramStore.MapChunkNearOpaqueProgram.UseLightBufferCompression = enable;
		_gpuProgramStore.MapBlockAnimatedProgram.UseLightBufferCompression = enable;
		_gpuProgramStore.DeferredProgram.UseLightBufferCompression = enable;
		_gpuProgramStore.LightProgram.UseLightBufferCompression = enable;
		_gpuProgramStore.LightMixProgram.UseLightBufferCompression = enable;
		_gpuProgramStore.BlockyModelProgram.Reset();
		_gpuProgramStore.FirstPersonBlockyModelProgram.Reset();
		_gpuProgramStore.MapChunkAlphaBlendedProgram.Reset();
		_gpuProgramStore.MapChunkFarAlphaTestedProgram.Reset();
		_gpuProgramStore.MapChunkFarOpaqueProgram.Reset();
		_gpuProgramStore.MapChunkNearAlphaTestedProgram.Reset();
		_gpuProgramStore.MapChunkNearOpaqueProgram.Reset();
		_gpuProgramStore.MapBlockAnimatedProgram.Reset();
		_gpuProgramStore.DeferredProgram.Reset();
		_gpuProgramStore.LightProgram.Reset();
		_gpuProgramStore.LightMixProgram.Reset();
	}

	public void SetLightingResolution(LightingResolution res)
	{
		_lightResolution = res;
	}

	private void UpdateLightingHeuristics()
	{
		if (UseDynamicLightResolutionSelection)
		{
			int renderingProfileLights = _renderingProfileLights;
			float num = Profiling.GetGPUMeasure(renderingProfileLights).AccumulatedElapsedTime / (float)Profiling.GetMeasureInfo(renderingProfileLights).AccumulatedFrameCount;
			if (num > 10f)
			{
				_lightResolution = LightingResolution.LOW;
			}
			else if (num > 5f)
			{
				if (_lightResolution == LightingResolution.LOW)
				{
					if (num < 7.5f)
					{
						_lightResolution = LightingResolution.MIXED;
					}
				}
				else
				{
					_lightResolution = LightingResolution.MIXED;
				}
			}
			else if (_lightResolution == LightingResolution.LOW || _lightResolution == LightingResolution.MIXED)
			{
				if (num < 3.5f)
				{
					_lightResolution = LightingResolution.FULL;
				}
			}
			else
			{
				_lightResolution = LightingResolution.FULL;
			}
		}
		if (_lightResolution != 0)
		{
			_lightBuffer = _renderTargetStore.LightBufferHalfRes;
		}
		else
		{
			_lightBuffer = _renderTargetStore.LightBufferFullRes;
		}
	}

	public void PrepareLights(LightData[] lightData, int lightCount)
	{
		ClusteredLighting.Prepare(lightData, lightCount, Data.WorldFieldOfView, Data.CameraPosition, ref Data.ViewRotationMatrix, ref Data.ProjectionMatrix);
		ClusteredLighting.SendDataToGPU();
		if (!UseClusteredLighting)
		{
			ClassicDeferredLighting.PrepareLightsForDraw(lightData, lightCount, Data.CameraPosition, ref Data.ViewRotationMatrix, ref Data.InvViewRotationMatrix, completeFullSetup: true);
			ClusteredLighting.SkipMeasures();
		}
	}

	public void DrawLightPass()
	{
		GLFunctions gL = _graphics.GL;
		BasicProgram basicProgram = _gpuProgramStore.BasicProgram;
		int num = (UseClusteredLighting ? ClusteredLighting.LightCount : ClassicDeferredLighting.LightCount);
		bool flag = num > 0;
		if (_graphics.UseDeferredLight && flag)
		{
			_gl.StencilMask(255u);
			_lightBuffer.Bind(_lightResolution != LightingResolution.FULL, setupViewport: true);
			if (!UseClusteredLighting && ClassicDeferredLighting.UseStencilForOuterLights)
			{
				Profiling.StartMeasure(_renderingProfileLightsStencil);
				ClassicDeferredLighting.TagStencil(32u, ref Data.ViewRotationProjectionMatrix);
				Profiling.StopMeasure(_renderingProfileLightsStencil);
			}
			else
			{
				Profiling.SkipMeasure(_renderingProfileLightsStencil);
			}
			int num2 = ((_lightResolution != 0) ? _renderingProfileLightsLowRes : _renderingProfileLightsFullRes);
			_gl.Enable(GL.BLEND);
			_gl.BlendFunc(GL.SRC_ALPHA, GL.ONE);
			if (UseLightBlendMax)
			{
				_gl.BlendEquationSeparate(GL.MAX, GL.MAX);
			}
			else
			{
				_gl.BlendEquationSeparate(GL.FUNC_ADD, GL.FUNC_ADD);
			}
			bool blue = _lightResolution != 0 || !_useLBufferCompression;
			_gl.ColorMask(red: true, green: true, blue, alpha: false);
			Profiling.StartMeasure(num2);
			bool fullResolution = _lightResolution == LightingResolution.FULL;
			if (UseClusteredLighting)
			{
				ClusteredLighting.DrawDeferredLights(Data.FrustumFarCornersVS, ref Data.ProjectionMatrix, fullResolution, secondPass: false);
			}
			else
			{
				ClassicDeferredLighting.DrawDeferredLights(fullResolution, ClassicDeferredLighting.UseStencilForOuterLights, ref Data.ViewRotationMatrix, ref Data.ProjectionMatrix, 1024f);
			}
			Profiling.StopMeasure(num2);
			if (num2 == _renderingProfileLightsFullRes)
			{
				Profiling.SkipMeasure(_renderingProfileLightsLowRes);
				Profiling.SkipMeasure(_renderingProfileLightsMix);
			}
			_lightBuffer.Unbind();
			if (_lightResolution != 0)
			{
				_renderTargetStore.LightBufferFullRes.Bind(clear: false, setupViewport: true);
				Profiling.StartMeasure(_renderingProfileLightsMix);
				_gl.ColorMask(red: true, green: true, !_useLBufferCompression, alpha: false);
				_gl.StencilFunc(GL.NOTEQUAL, 128, 128u);
				_gl.StencilMask(0u);
				_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.LightBufferHalfRes.GetTexture(RenderTarget.Target.Color0));
				LightMixProgram lightMixProgram = _gpuProgramStore.LightMixProgram;
				_gl.UseProgram(lightMixProgram);
				_graphics.ScreenTriangleRenderer.Draw();
				Profiling.StopMeasure(_renderingProfileLightsMix);
				if (_lightResolution == LightingResolution.MIXED)
				{
					Profiling.StartMeasure(_renderingProfileLightsFullRes);
					_gl.StencilFunc(GL.EQUAL, 128, 128u);
					if (UseClusteredLighting)
					{
						ClusteredLighting.DrawDeferredLights(Data.FrustumFarCornersVS, ref Data.ProjectionMatrix, fullResolution: true, secondPass: true);
					}
					else
					{
						ClassicDeferredLighting.DrawDeferredLights(fullResolution: true, useStencilForOuterLights: false, ref Data.ViewRotationMatrix, ref Data.ProjectionMatrix, 1024f);
					}
					Profiling.StopMeasure(_renderingProfileLightsFullRes);
				}
				else
				{
					Profiling.SkipMeasure(_renderingProfileLightsFullRes);
				}
				_renderTargetStore.LightBufferFullRes.Unbind();
			}
			if (UseLightBlendMax)
			{
				_gl.BlendEquationSeparate(GL.FUNC_ADD, GL.FUNC_ADD);
			}
			_gl.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
			_gl.Disable(GL.BLEND);
			_gl.Disable(GL.STENCIL_TEST);
			_gl.Enable(GL.DEPTH_TEST);
		}
		else
		{
			Profiling.SkipMeasure(_renderingProfileLightsStencil);
			Profiling.SkipMeasure(_renderingProfileLightsFullRes);
			Profiling.SkipMeasure(_renderingProfileLightsLowRes);
			Profiling.SkipMeasure(_renderingProfileLightsMix);
		}
		_gl.ColorMask(red: true, green: true, blue: true, alpha: true);
	}

	public void DebugDrawLights(LightData[] lightData, int lightCount)
	{
		_gl.AssertActiveTexture(GL.TEXTURE0);
		_gl.BindTexture(GL.TEXTURE_2D, _graphics.WhitePixelTexture.GLTexture);
		_gl.BindVertexArray(_sphereLightMesh.VertexArray);
		BasicProgram basicProgram = _gpuProgramStore.BasicProgram;
		_gl.UseProgram(basicProgram);
		basicProgram.Opacity.SetValue(1f);
		BoundingSphere boundingSphere = default(BoundingSphere);
		for (int i = 0; i < lightCount; i++)
		{
			float radius = lightData[i].Sphere.Radius;
			Vector3 vector = (boundingSphere.Center = lightData[i].Sphere.Center);
			boundingSphere.Radius = radius;
			vector -= Data.CameraPosition;
			Matrix.CreateScale(radius, out var result);
			Matrix.AddTranslation(ref result, vector.X, vector.Y, vector.Z);
			Matrix.Multiply(ref result, ref Data.ViewRotationProjectionMatrix, out result);
			Vector3 value = ((boundingSphere.Contains(Data.CameraPosition) != 0) ? new Vector3(0f, 1f, 0f) : new Vector3(1f, 0f, 0f));
			basicProgram.Color.SetValue(value);
			basicProgram.MVPMatrix.SetValue(ref result);
			_gl.DrawElements(GL.TRIANGLES, _sphereLightMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
		}
	}

	public void ComputeNearChunkDistance(float fieldOfView)
	{
		float num = MathHelper.Clamp(70f / fieldOfView, 1f, 2f);
		_nearChunkDistance = 64f * num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsNear(float distance)
	{
		return distance < _nearChunkDistance;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MapBlocksAnimatedNeedDrawing()
	{
		return _animatedBlockDrawTaskCount > 0;
	}

	private void ResetMapCounters()
	{
		_animatedBlockDrawTaskCount = 0;
		_opaqueDrawTaskCount = 0;
		_alphaTestedDrawTaskCount = 0;
		_alphaBlendedDrawTaskCount = 0;
		_farProgramOpaqueChunkStartIndex = 0;
		_nearProgramAlphaBlendedChunkStartIndex = 0;
		_farProgramAlphaTestedChunkStartIndex = 0;
	}

	public void PrepareForIncomingMapChunkDrawTasks(int opaqueCount, int alphaTestedCount, int alphaBlendedCount)
	{
		ArrayUtils.GrowArrayIfNecessary(ref _opaqueDrawTasks, opaqueCount, 100);
		ArrayUtils.GrowArrayIfNecessary(ref _alphaTestedDrawTasks, alphaTestedCount, 100);
		ArrayUtils.GrowArrayIfNecessary(ref _alphaBlendedDrawTasks, alphaBlendedCount, 50);
		PrepareForIncomingChunkOccludees(opaqueCount, alphaTestedCount, alphaBlendedCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterMapChunkOpaqueDrawTask(ref Matrix modelMatrix, GLVertexArray vertexArray, int indicesCount, bool isNear)
	{
		_opaqueDrawTasks[_opaqueDrawTaskCount].VertexArray = vertexArray;
		_opaqueDrawTasks[_opaqueDrawTaskCount].DataOffset = IntPtr.Zero;
		_opaqueDrawTasks[_opaqueDrawTaskCount].DataCount = indicesCount;
		_opaqueDrawTasks[_opaqueDrawTaskCount].ModelMatrix = modelMatrix;
		_opaqueDrawTaskCount++;
		RegisterOccludeeChunkOpaque(modelMatrix.Translation);
		if (isNear)
		{
			_farProgramOpaqueChunkStartIndex = _opaqueDrawTaskCount;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterMapChunkAlphaTestedDrawTask(ref Matrix modelMatrix, GLVertexArray vertexArray, IntPtr offset, int indicesCount, bool isNear)
	{
		_alphaTestedDrawTasks[_alphaTestedDrawTaskCount].VertexArray = vertexArray;
		_alphaTestedDrawTasks[_alphaTestedDrawTaskCount].DataOffset = offset;
		_alphaTestedDrawTasks[_alphaTestedDrawTaskCount].DataCount = indicesCount;
		_alphaTestedDrawTasks[_alphaTestedDrawTaskCount].ModelMatrix = modelMatrix;
		_alphaTestedDrawTaskCount++;
		RegisterOccludeeChunkAlphaTested(modelMatrix.Translation);
		if (isNear)
		{
			_farProgramAlphaTestedChunkStartIndex = _alphaTestedDrawTaskCount;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterMapChunkAlphaBlendedDrawTask(ref Matrix modelMatrix, GLVertexArray vertexArray, int indicesCount, bool isNear)
	{
		_alphaBlendedDrawTasks[_alphaBlendedDrawTaskCount].VertexArray = vertexArray;
		_alphaBlendedDrawTasks[_alphaBlendedDrawTaskCount].DataOffset = IntPtr.Zero;
		_alphaBlendedDrawTasks[_alphaBlendedDrawTaskCount].DataCount = indicesCount;
		_alphaBlendedDrawTasks[_alphaBlendedDrawTaskCount].ModelMatrix = modelMatrix;
		_alphaBlendedDrawTaskCount++;
		RegisterOccludeeChunkAlphaBlended(modelMatrix.Translation);
		if (isNear)
		{
			_nearProgramAlphaBlendedChunkStartIndex = _alphaBlendedDrawTaskCount;
		}
	}

	public void PrepareForIncomingMapBlockAnimatedDrawTasks(int count)
	{
		ArrayUtils.GrowArrayIfNecessary(ref _animatedBlockDrawTasks, _animatedBlockDrawTaskCount + count, 25);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterMapBlockAnimatedDrawTask(ref Matrix modelMatrix, GLVertexArray vertexArray, int indicesCount, GLBuffer animationData, uint animationDataOffset, uint animationDataCount)
	{
		_animatedBlockDrawTasks[_animatedBlockDrawTaskCount].VertexArray = vertexArray;
		_animatedBlockDrawTasks[_animatedBlockDrawTaskCount].DataCount = indicesCount;
		_animatedBlockDrawTasks[_animatedBlockDrawTaskCount].AnimationData = animationData;
		_animatedBlockDrawTasks[_animatedBlockDrawTaskCount].AnimationDataOffset = animationDataOffset;
		_animatedBlockDrawTasks[_animatedBlockDrawTaskCount].AnimationDataSize = (ushort)(animationDataCount * 64);
		_animatedBlockDrawTasks[_animatedBlockDrawTaskCount].ModelMatrix = modelMatrix;
		_animatedBlockDrawTaskCount++;
	}

	public void DrawMapChunksOpaque(bool nearChunks, bool useOcclusionCulling)
	{
		GLFunctions gL = _graphics.GL;
		if (useOcclusionCulling)
		{
			if (nearChunks)
			{
				MapChunkBaseProgram mapChunkNearOpaqueProgram = _gpuProgramStore.MapChunkNearOpaqueProgram;
				mapChunkNearOpaqueProgram.AssertInUse();
				for (int i = 0; i < _farProgramOpaqueChunkStartIndex; i++)
				{
					if (VisibleOccludees[i] == 1)
					{
						mapChunkNearOpaqueProgram.ModelMatrix.SetValue(ref _opaqueDrawTasks[i].ModelMatrix);
						gL.BindVertexArray(_opaqueDrawTasks[i].VertexArray);
						gL.DrawElements(GL.TRIANGLES, _opaqueDrawTasks[i].DataCount, GL.UNSIGNED_INT, _opaqueDrawTasks[i].DataOffset);
					}
				}
				return;
			}
			MapChunkBaseProgram mapChunkFarOpaqueProgram = _gpuProgramStore.MapChunkFarOpaqueProgram;
			mapChunkFarOpaqueProgram.AssertInUse();
			for (int j = _farProgramOpaqueChunkStartIndex; j < _opaqueDrawTaskCount; j++)
			{
				if (VisibleOccludees[j] == 1)
				{
					mapChunkFarOpaqueProgram.ModelMatrix.SetValue(ref _opaqueDrawTasks[j].ModelMatrix);
					gL.BindVertexArray(_opaqueDrawTasks[j].VertexArray);
					gL.DrawElements(GL.TRIANGLES, _opaqueDrawTasks[j].DataCount, GL.UNSIGNED_INT, _opaqueDrawTasks[j].DataOffset);
				}
			}
		}
		else if (nearChunks)
		{
			MapChunkBaseProgram mapChunkNearOpaqueProgram2 = _gpuProgramStore.MapChunkNearOpaqueProgram;
			mapChunkNearOpaqueProgram2.AssertInUse();
			for (int k = 0; k < _farProgramOpaqueChunkStartIndex; k++)
			{
				mapChunkNearOpaqueProgram2.ModelMatrix.SetValue(ref _opaqueDrawTasks[k].ModelMatrix);
				gL.BindVertexArray(_opaqueDrawTasks[k].VertexArray);
				gL.DrawElements(GL.TRIANGLES, _opaqueDrawTasks[k].DataCount, GL.UNSIGNED_INT, _opaqueDrawTasks[k].DataOffset);
			}
		}
		else
		{
			MapChunkBaseProgram mapChunkFarOpaqueProgram2 = _gpuProgramStore.MapChunkFarOpaqueProgram;
			mapChunkFarOpaqueProgram2.AssertInUse();
			for (int l = _farProgramOpaqueChunkStartIndex; l < _opaqueDrawTaskCount; l++)
			{
				mapChunkFarOpaqueProgram2.ModelMatrix.SetValue(ref _opaqueDrawTasks[l].ModelMatrix);
				gL.BindVertexArray(_opaqueDrawTasks[l].VertexArray);
				gL.DrawElements(GL.TRIANGLES, _opaqueDrawTasks[l].DataCount, GL.UNSIGNED_INT, _opaqueDrawTasks[l].DataOffset);
			}
		}
	}

	public void DrawMapChunksAlphaTested(bool nearChunks, bool useOcclusionCulling)
	{
		GLFunctions gL = _graphics.GL;
		int alphaTestedChunkOccludeesOffset = AlphaTestedChunkOccludeesOffset;
		if (useOcclusionCulling)
		{
			if (nearChunks)
			{
				MapChunkBaseProgram mapChunkNearAlphaTestedProgram = _gpuProgramStore.MapChunkNearAlphaTestedProgram;
				mapChunkNearAlphaTestedProgram.AssertInUse();
				for (int i = 0; i < _farProgramAlphaTestedChunkStartIndex; i++)
				{
					if (VisibleOccludees[i + alphaTestedChunkOccludeesOffset] == 1)
					{
						mapChunkNearAlphaTestedProgram.ModelMatrix.SetValue(ref _alphaTestedDrawTasks[i].ModelMatrix);
						gL.BindVertexArray(_alphaTestedDrawTasks[i].VertexArray);
						gL.DrawElements(GL.TRIANGLES, _alphaTestedDrawTasks[i].DataCount, GL.UNSIGNED_INT, _alphaTestedDrawTasks[i].DataOffset);
					}
				}
				return;
			}
			MapChunkBaseProgram mapChunkFarAlphaTestedProgram = _gpuProgramStore.MapChunkFarAlphaTestedProgram;
			mapChunkFarAlphaTestedProgram.AssertInUse();
			for (int j = _farProgramAlphaTestedChunkStartIndex; j < _alphaTestedDrawTaskCount; j++)
			{
				if (VisibleOccludees[j + alphaTestedChunkOccludeesOffset] == 1)
				{
					mapChunkFarAlphaTestedProgram.ModelMatrix.SetValue(ref _alphaTestedDrawTasks[j].ModelMatrix);
					gL.BindVertexArray(_alphaTestedDrawTasks[j].VertexArray);
					gL.DrawElements(GL.TRIANGLES, _alphaTestedDrawTasks[j].DataCount, GL.UNSIGNED_INT, _alphaTestedDrawTasks[j].DataOffset);
				}
			}
		}
		else if (nearChunks)
		{
			MapChunkBaseProgram mapChunkNearAlphaTestedProgram2 = _gpuProgramStore.MapChunkNearAlphaTestedProgram;
			mapChunkNearAlphaTestedProgram2.AssertInUse();
			for (int k = 0; k < _farProgramAlphaTestedChunkStartIndex; k++)
			{
				mapChunkNearAlphaTestedProgram2.ModelMatrix.SetValue(ref _alphaTestedDrawTasks[k].ModelMatrix);
				gL.BindVertexArray(_alphaTestedDrawTasks[k].VertexArray);
				gL.DrawElements(GL.TRIANGLES, _alphaTestedDrawTasks[k].DataCount, GL.UNSIGNED_INT, _alphaTestedDrawTasks[k].DataOffset);
			}
		}
		else
		{
			MapChunkBaseProgram mapChunkFarAlphaTestedProgram2 = _gpuProgramStore.MapChunkFarAlphaTestedProgram;
			mapChunkFarAlphaTestedProgram2.AssertInUse();
			for (int l = _farProgramAlphaTestedChunkStartIndex; l < _alphaTestedDrawTaskCount; l++)
			{
				mapChunkFarAlphaTestedProgram2.ModelMatrix.SetValue(ref _alphaTestedDrawTasks[l].ModelMatrix);
				gL.BindVertexArray(_alphaTestedDrawTasks[l].VertexArray);
				gL.DrawElements(GL.TRIANGLES, _alphaTestedDrawTasks[l].DataCount, GL.UNSIGNED_INT, _alphaTestedDrawTasks[l].DataOffset);
			}
		}
	}

	public void DrawMapChunksAlphaBlended(bool useOcclusionCulling)
	{
		MapChunkBaseProgram mapChunkAlphaBlendedProgram = _gpuProgramStore.MapChunkAlphaBlendedProgram;
		mapChunkAlphaBlendedProgram.AssertInUse();
		GLFunctions gL = _graphics.GL;
		int alphaBlendedOccludeesOffset = AlphaBlendedOccludeesOffset;
		int num = _nearProgramAlphaBlendedChunkStartIndex - 1;
		Debug.Assert(num < _alphaBlendedDrawTaskCount);
		if (useOcclusionCulling)
		{
			for (int num2 = _alphaBlendedDrawTaskCount - 1; num2 > num; num2--)
			{
				if (VisibleOccludees[num2 + alphaBlendedOccludeesOffset] == 1)
				{
					mapChunkAlphaBlendedProgram.ModelMatrix.SetValue(ref _alphaBlendedDrawTasks[num2].ModelMatrix);
					gL.BindVertexArray(_alphaBlendedDrawTasks[num2].VertexArray);
					gL.DrawElements(GL.TRIANGLES, _alphaBlendedDrawTasks[num2].DataCount, GL.UNSIGNED_INT, _alphaBlendedDrawTasks[num2].DataOffset);
				}
			}
			for (int num3 = num; num3 >= 0; num3--)
			{
				if (VisibleOccludees[num3 + alphaBlendedOccludeesOffset] == 1)
				{
					mapChunkAlphaBlendedProgram.ModelMatrix.SetValue(ref _alphaBlendedDrawTasks[num3].ModelMatrix);
					gL.BindVertexArray(_alphaBlendedDrawTasks[num3].VertexArray);
					gL.DrawElements(GL.TRIANGLES, _alphaBlendedDrawTasks[num3].DataCount, GL.UNSIGNED_INT, _alphaBlendedDrawTasks[num3].DataOffset);
				}
			}
		}
		else
		{
			for (int num4 = _alphaBlendedDrawTaskCount - 1; num4 > num; num4--)
			{
				mapChunkAlphaBlendedProgram.ModelMatrix.SetValue(ref _alphaBlendedDrawTasks[num4].ModelMatrix);
				gL.BindVertexArray(_alphaBlendedDrawTasks[num4].VertexArray);
				gL.DrawElements(GL.TRIANGLES, _alphaBlendedDrawTasks[num4].DataCount, GL.UNSIGNED_INT, _alphaBlendedDrawTasks[num4].DataOffset);
			}
			for (int num5 = num; num5 >= 0; num5--)
			{
				mapChunkAlphaBlendedProgram.ModelMatrix.SetValue(ref _alphaBlendedDrawTasks[num5].ModelMatrix);
				gL.BindVertexArray(_alphaBlendedDrawTasks[num5].VertexArray);
				gL.DrawElements(GL.TRIANGLES, _alphaBlendedDrawTasks[num5].DataCount, GL.UNSIGNED_INT, _alphaBlendedDrawTasks[num5].DataOffset);
			}
		}
	}

	public void DrawMapBlocksAnimated()
	{
		MapBlockAnimatedProgram mapBlockAnimatedProgram = _gpuProgramStore.MapBlockAnimatedProgram;
		mapBlockAnimatedProgram.AssertInUse();
		GLFunctions gL = _graphics.GL;
		for (int i = 0; i < _animatedBlockDrawTaskCount; i++)
		{
			mapBlockAnimatedProgram.ModelMatrix.SetValue(ref _animatedBlockDrawTasks[i].ModelMatrix);
			mapBlockAnimatedProgram.NodeBlock.SetBufferRange(_animatedBlockDrawTasks[i].AnimationData, _animatedBlockDrawTasks[i].AnimationDataOffset, _animatedBlockDrawTasks[i].AnimationDataSize);
			gL.BindVertexArray(_animatedBlockDrawTasks[i].VertexArray);
			gL.DrawElements(GL.TRIANGLES, _animatedBlockDrawTasks[i].DataCount, GL.UNSIGNED_INT, (IntPtr)0);
		}
	}

	private void InitOcclusionCulling()
	{
		CreateOcclusionCullingGPUData();
	}

	private void DisposeOcclusionCulling()
	{
		DestroyOcclusionCullingGPUData();
	}

	private unsafe void CreateOcclusionCullingGPUData()
	{
		GLFunctions gL = _graphics.GL;
		int num = 4000;
		int num2 = 4 * num;
		int num3 = 16;
		int num4 = 6 * num;
		ushort[] array = new ushort[num4];
		for (int i = 0; i < num; i++)
		{
			array[i * 6] = (ushort)(i * 4);
			array[i * 6 + 1] = (ushort)(i * 4 + 1);
			array[i * 6 + 2] = (ushort)(i * 4 + 2);
			array[i * 6 + 3] = (ushort)(i * 4);
			array[i * 6 + 4] = (ushort)(i * 4 + 2);
			array[i * 6 + 5] = (ushort)(i * 4 + 3);
		}
		_occluderPlanesVertexArray = gL.GenVertexArray();
		gL.BindVertexArray(_occluderPlanesVertexArray);
		_occluderPlanesVerticesBuffer = gL.GenBuffer();
		gL.BindBuffer(_occluderPlanesVertexArray, GL.ARRAY_BUFFER, _occluderPlanesVerticesBuffer);
		gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(num2 * num3), IntPtr.Zero, GL.DYNAMIC_DRAW);
		_occluderPlanesIndicesBuffer = gL.GenBuffer();
		gL.BindBuffer(_occluderPlanesVertexArray, GL.ELEMENT_ARRAY_BUFFER, _occluderPlanesIndicesBuffer);
		fixed (ushort* ptr = array)
		{
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(num4 * 2), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		gL.EnableVertexAttribArray(0u);
		gL.VertexAttribPointer(0u, 4, GL.FLOAT, normalized: false, num3, IntPtr.Zero);
	}

	private void DestroyOcclusionCullingGPUData()
	{
		GLFunctions gL = _graphics.GL;
		gL.DeleteBuffer(_occluderPlanesVerticesBuffer);
		gL.DeleteBuffer(_occluderPlanesIndicesBuffer);
		gL.DeleteVertexArray(_occluderPlanesVertexArray);
	}

	private void ResetOcclusionCullingCounters()
	{
		_opaqueOccludersCount = 0;
		_occluderPlanesCount = 0;
		_opaqueOccludeesCount = 0;
		_alphaTestedOccludeesCount = 0;
		_alphaBlendedOccludeesCount = 0;
		_entitiesOccludeesCount = 0;
		_lightOccludeesCount = 0;
		_particleOccludeesCount = 0;
	}

	public void GatherOcclusionCullingStats(out int occludedCount, out int occludedTrianglesCount)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < _opaqueDrawTaskCount; i++)
		{
			if (VisibleOccludees[i] == 0)
			{
				num++;
				num2 += _opaqueDrawTasks[i].DataCount;
			}
		}
		int opaqueDrawTaskCount = _opaqueDrawTaskCount;
		for (int j = 0; j < _alphaTestedDrawTaskCount; j++)
		{
			if (VisibleOccludees[j + opaqueDrawTaskCount] == 0)
			{
				num++;
				num2 += _alphaTestedDrawTasks[j].DataCount;
			}
		}
		opaqueDrawTaskCount += _alphaTestedDrawTaskCount;
		for (int k = 0; k < _alphaBlendedDrawTaskCount; k++)
		{
			if (VisibleOccludees[k + opaqueDrawTaskCount] == 0)
			{
				num++;
				num2 += _alphaBlendedDrawTasks[k].DataCount;
			}
		}
		occludedCount = num;
		occludedTrianglesCount = num2 / 3;
	}

	public void PrepareForIncomingChunkOccludees(int opaqueCount, int alphaTestedCount, int alphaBlendedCount)
	{
		ArrayUtils.GrowArrayIfNecessary(ref _opaqueOccludeesData, opaqueCount, 100);
		ArrayUtils.GrowArrayIfNecessary(ref _alphaTestedOccludeesData, alphaTestedCount, 100);
		ArrayUtils.GrowArrayIfNecessary(ref _alphaBlendedOccludeesData, alphaBlendedCount, 50);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterOccludeeChunkOpaque(Vector3 position)
	{
		_opaqueOccludeesData[_opaqueOccludeesCount] = position;
		_opaqueOccludeesCount++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterOccludeeChunkAlphaTested(Vector3 position)
	{
		_alphaTestedOccludeesData[_alphaTestedOccludeesCount] = position;
		_alphaTestedOccludeesCount++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterOccludeeChunkAlphaBlended(Vector3 position)
	{
		_alphaBlendedOccludeesData[_alphaBlendedOccludeesCount] = position;
		_alphaBlendedOccludeesCount++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PrepareForIncomingChunkOccluderPlane(int count)
	{
		ArrayUtils.GrowArrayIfNecessary(ref _occluderPlanes, _occluderPlanesCount + 4 * count, 1000);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterChunkOccluderPlane(Vector3 position, float minSolidPlaneY)
	{
		_occluderPlanes[_occluderPlanesCount] = new Vector4(position.X, position.Y, position.Z, minSolidPlaneY);
		_occluderPlanes[_occluderPlanesCount + 1] = new Vector4(position.X, position.Y, position.Z, minSolidPlaneY);
		_occluderPlanes[_occluderPlanesCount + 2] = new Vector4(position.X, position.Y, position.Z, minSolidPlaneY);
		_occluderPlanes[_occluderPlanesCount + 3] = new Vector4(position.X, position.Y, position.Z, minSolidPlaneY);
		_occluderPlanesCount += 4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PrepareForIncomingOccludeeEntity(int count)
	{
		ArrayUtils.GrowArrayIfNecessary(ref _entitiesOccludeesData, count, 250);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterOccludeeEntity(ref BoundingBox boundingBox)
	{
		_entitiesOccludeesData[_entitiesOccludeesCount] = boundingBox;
		_entitiesOccludeesCount++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PrepareForIncomingOccludeeLight(int count)
	{
		ArrayUtils.GrowArrayIfNecessary(ref _lightOccludeesData, count, 250);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterOccludeeLight(ref BoundingBox boundingBox)
	{
		_lightOccludeesData[_lightOccludeesCount] = boundingBox;
		_lightOccludeesCount++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PrepareForIncomingOccludeeParticle(int count)
	{
		ArrayUtils.GrowArrayIfNecessary(ref _particleOccludeesData, count, 250);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterOccludeeParticle(ref BoundingBox boundingBox)
	{
		_particleOccludeesData[_particleOccludeesCount] = boundingBox;
		_particleOccludeesCount++;
	}

	public ref OcclusionCulling.OccludeeData[] GetOccludeesData(out int occludeesCount)
	{
		occludeesCount = _opaqueOccludeesCount + _alphaTestedOccludeesCount + _alphaBlendedOccludeesCount + _entitiesOccludeesCount + _lightOccludeesCount + _particleOccludeesCount;
		int growth = System.Math.Max(500, 2 * (occludeesCount - 2000));
		ArrayUtils.GrowArrayIfNecessary(ref _occludeesData, occludeesCount, growth);
		Vector3 vector = new Vector3(32f);
		int num = 0;
		for (int i = 0; i < _opaqueOccludeesCount; i++)
		{
			_occludeesData[i].BoxMin = _opaqueOccludeesData[i];
			_occludeesData[i].BoxMax = _opaqueOccludeesData[i] + vector;
		}
		num += _opaqueOccludeesCount;
		for (int j = 0; j < _alphaTestedOccludeesCount; j++)
		{
			_occludeesData[j + num].BoxMin = _alphaTestedOccludeesData[j];
			_occludeesData[j + num].BoxMax = _alphaTestedOccludeesData[j] + vector;
		}
		num += _alphaTestedOccludeesCount;
		for (int k = 0; k < _alphaBlendedOccludeesCount; k++)
		{
			_occludeesData[k + num].BoxMin = _alphaBlendedOccludeesData[k];
			_occludeesData[k + num].BoxMax = _alphaBlendedOccludeesData[k] + vector;
		}
		num += _alphaBlendedOccludeesCount;
		for (int l = 0; l < _entitiesOccludeesCount; l++)
		{
			_occludeesData[l + num].BoxMin = _entitiesOccludeesData[l].Min;
			_occludeesData[l + num].BoxMax = _entitiesOccludeesData[l].Max;
		}
		num += _entitiesOccludeesCount;
		for (int m = 0; m < _lightOccludeesCount; m++)
		{
			_occludeesData[m + num].BoxMin = _lightOccludeesData[m].Min;
			_occludeesData[m + num].BoxMax = _lightOccludeesData[m].Max;
		}
		num += _lightOccludeesCount;
		for (int n = 0; n < _particleOccludeesCount; n++)
		{
			_occludeesData[n + num].BoxMin = _particleOccludeesData[n].Min;
			_occludeesData[n + num].BoxMax = _particleOccludeesData[n].Max;
		}
		return ref _occludeesData;
	}

	public void PrepareOcclusionCulling(int requestedOpaqueOccludersCount, bool useChunkOccluderPlanes, bool useOpaqueChunkOccluders, bool useAlphaTestedChunkOccluders, int mapAtlasTextureUnit, GLTexture mapAtlasTexture)
	{
		_occlusionCullingSetup.RequestedOpaqueChunkOccludersCount = (byte)requestedOpaqueOccludersCount;
		_occlusionCullingSetup.UseChunkOccluderPlanes = useChunkOccluderPlanes;
		_occlusionCullingSetup.UseOpaqueChunkOccluders = useOpaqueChunkOccluders;
		_occlusionCullingSetup.UseAlphaTestedChunkOccluders = useAlphaTestedChunkOccluders;
		_occlusionCullingSetup.MapAtlasTextureUnit = (byte)mapAtlasTextureUnit;
		_occlusionCullingSetup.MapAtlasTexture = mapAtlasTexture;
		int num = System.Math.Min(100, _opaqueDrawTaskCount);
		int num2 = 0;
		if (num > 0)
		{
			for (int i = 0; i < _opaqueDrawTaskCount; i++)
			{
				_opaqueOccludersIDs[num2] = (byte)i;
				num2++;
				if (num2 == num)
				{
					break;
				}
			}
		}
		_opaqueOccludersCount = (byte)num2;
	}

	private unsafe void DrawChunkOccluderPlanes()
	{
		GLFunctions gL = _graphics.GL;
		int num = 16;
		gL.BindBuffer(GL.ARRAY_BUFFER, _occluderPlanesVerticesBuffer);
		fixed (Vector4* ptr = _occluderPlanes)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(_occluderPlanesCount * num), (IntPtr)ptr, GL.DYNAMIC_DRAW);
		}
		ZOnlyChunkPlanesProgram zOnlyMapChunkPlanesProgram = _gpuProgramStore.ZOnlyMapChunkPlanesProgram;
		gL.UseProgram(zOnlyMapChunkPlanesProgram);
		zOnlyMapChunkPlanesProgram.ViewProjectionMatrix.SetValue(ref Data.ViewRotationProjectionMatrix);
		int count = _occluderPlanesCount / 4 * 6;
		gL.BindVertexArray(_occluderPlanesVertexArray);
		gL.DrawElements(GL.TRIANGLES, count, GL.UNSIGNED_SHORT, IntPtr.Zero);
	}

	private void DrawChunkOccluders()
	{
		GLFunctions gL = _graphics.GL;
		ZOnlyChunkProgram zOnlyMapChunkProgram = _gpuProgramStore.ZOnlyMapChunkProgram;
		gL.ActiveTexture((GL)(33984 + _occlusionCullingSetup.MapAtlasTextureUnit));
		gL.BindTexture(GL.TEXTURE_2D, _occlusionCullingSetup.MapAtlasTexture);
		gL.UseProgram(zOnlyMapChunkProgram);
		zOnlyMapChunkProgram.ViewProjectionMatrix.SetValue(ref Data.ViewRotationProjectionMatrix);
		if (_occlusionCullingSetup.UseOpaqueChunkOccluders)
		{
			byte b = System.Math.Min(_occlusionCullingSetup.RequestedOpaqueChunkOccludersCount, _opaqueOccludersCount);
			for (int i = 0; i < b; i++)
			{
				byte b2 = _opaqueOccludersIDs[i];
				zOnlyMapChunkProgram.ModelMatrix.SetValue(ref _opaqueDrawTasks[b2].ModelMatrix);
				gL.BindVertexArray(_opaqueDrawTasks[b2].VertexArray);
				gL.DrawElements(GL.TRIANGLES, _opaqueDrawTasks[b2].DataCount, GL.UNSIGNED_INT, _opaqueDrawTasks[b2].DataOffset);
			}
		}
		if (_occlusionCullingSetup.UseAlphaTestedChunkOccluders)
		{
			zOnlyMapChunkProgram.Time.SetValue(Data.Time);
			gL.Disable(GL.CULL_FACE);
			int num = System.Math.Min(System.Math.Min(8, _alphaTestedDrawTaskCount), _farProgramAlphaTestedChunkStartIndex);
			for (int j = 0; j < num; j++)
			{
				zOnlyMapChunkProgram.ModelMatrix.SetValue(ref _alphaTestedDrawTasks[j].ModelMatrix);
				gL.BindVertexArray(_alphaTestedDrawTasks[j].VertexArray);
				gL.DrawElements(GL.TRIANGLES, _alphaTestedDrawTasks[j].DataCount, GL.UNSIGNED_INT, _alphaTestedDrawTasks[j].DataOffset);
			}
			gL.Enable(GL.CULL_FACE);
		}
	}

	public void DrawOccluders()
	{
		if (_occlusionCullingSetup.UseChunkOccluderPlanes)
		{
			DrawChunkOccluderPlanes();
		}
		if (_occlusionCullingSetup.UseOpaqueChunkOccluders || _occlusionCullingSetup.UseAlphaTestedChunkOccluders)
		{
			DrawChunkOccluders();
		}
	}

	private void InitSunShadows()
	{
		UseSunShadows = true;
		InitShadowCasting();
		_cascadedShadowMapping = new CascadedShadowMapping(_graphics);
		_cascadedShadowMapping.Init(DrawShadowCasters);
	}

	private void DisposeSunShadows()
	{
		_cascadedShadowMapping.Release();
		_cascadedShadowMapping.Dispose();
		_cascadedShadowMapping = null;
		DisposeEntitiesShadowMapGPUData();
	}

	private void InitShadowCasting()
	{
		for (int i = 0; i < 4; i++)
		{
			_cascadeDrawTaskId[i] = new ushort[1000];
		}
		_sunShadowCasting.DirectionType = SunShadowCastingSettings.ShadowDirectionType.TopDown;
		_sunShadowCasting.Direction = Vector3.Down;
		_sunShadowCasting.ShadowIntensity = 0.68f;
		_sunShadowCasting.UseSafeAngle = true;
		_sunShadowCasting.UseChunkShadowCasters = false;
		_sunShadowCasting.UseEntitiesModelVFX = true;
		_sunShadowCasting.UseDrawInstanced = false;
		_sunShadowCasting.UseSmartCascadeDispatch = true;
		InitEntitiesShadowMapGPUData();
	}

	private void ResetSunShadowsCounters()
	{
		for (int i = 0; i < 4; i++)
		{
			_cascadeEntityDrawTaskCount[i] = 0;
			_cascadeChunkDrawTaskCount[i] = 0;
			_cascadeAnimatedBlockDrawTaskCount[i] = 0;
		}
		_entityShadowMapDrawTaskCount = 0;
		_incomingEntityShadowMapDrawTaskCount = 0;
		_chunkShadowMapDrawTaskCount = 0;
		_incomingChunkShadowMapDrawTaskCount = 0;
		_animatedBlockShadowMapDrawTaskCount = 0;
		_incomingAnimatedBlockShadowMapDrawTaskCount = 0;
	}

	public void SetSunShadowsMaxWorldHeight(float maxWorldHeight)
	{
		_cascadedShadowMapping.SetSunShadowsMaxWorldHeight(maxWorldHeight);
	}

	public void SetSunShadowCastersDrawInstancedEnabled(bool enable)
	{
		_sunShadowCasting.UseDrawInstanced = enable;
	}

	public void SetSunShadowCastersSmartCascadeDispatchEnabled(bool enable)
	{
		_sunShadowCasting.UseSmartCascadeDispatch = enable;
	}

	public void SetSunShadowsSafeAngleEnabled(bool enable)
	{
		_sunShadowCasting.UseSafeAngle = enable;
	}

	public void SetSunShadowsEnabled(bool enable)
	{
		if (UseSunShadows != enable)
		{
			UseSunShadows = enable;
			_gpuProgramStore.DeferredProgram.UseDeferredShadow = enable;
			_gpuProgramStore.DeferredProgram.Reset();
		}
	}

	public void SetSunShadowsWithChunks(bool enable)
	{
		if (_sunShadowCasting.UseChunkShadowCasters != enable)
		{
			_sunShadowCasting.UseChunkShadowCasters = enable;
			_sunShadowCasting.UseChunkShadowCasters = enable;
			_gpuProgramStore.DeferredProgram.UseDeferredShadowIndoorFading = enable;
			_gpuProgramStore.DeferredProgram.Reset();
		}
	}

	public void ToggleSunShadowsWithModelVFXs()
	{
		_sunShadowCasting.UseEntitiesModelVFX = !_sunShadowCasting.UseEntitiesModelVFX;
		_gpuProgramStore.BlockyModelShadowMapProgram.UseModelVFX = _sunShadowCasting.UseEntitiesModelVFX;
		_gpuProgramStore.BlockyModelShadowMapProgram.Reset();
	}

	public void SetSunShadowsIntensity(float value)
	{
		float shadowIntensity = MathHelper.Clamp(value, 0f, 1f);
		_sunShadowCasting.ShadowIntensity = shadowIntensity;
	}

	public void SetSunShadowsDirectionTopDown()
	{
		if (_sunShadowCasting.DirectionType != 0)
		{
			_sunShadowCasting.DirectionType = SunShadowCastingSettings.ShadowDirectionType.TopDown;
			_sunShadowCasting.Direction = Vector3.Down;
			SetSunShadowsUseCleanBackfaces(enable: false);
		}
	}

	public void SetSunShadowsDirectionCustom(Vector3 direction)
	{
		Vector3 vector = Vector3.Normalize(direction);
		if (_sunShadowCasting.DirectionType != SunShadowCastingSettings.ShadowDirectionType.StaticCustom || _sunShadowCasting.Direction != vector)
		{
			_sunShadowCasting.DirectionType = SunShadowCastingSettings.ShadowDirectionType.StaticCustom;
			_sunShadowCasting.Direction = vector;
			SetSunShadowsUseCleanBackfaces(enable: true);
		}
	}

	public void SetSunShadowsDirectionSun(bool useCleanBackFaces)
	{
		if (_sunShadowCasting.DirectionType != SunShadowCastingSettings.ShadowDirectionType.DynamicSun)
		{
			_sunShadowCasting.DirectionType = SunShadowCastingSettings.ShadowDirectionType.DynamicSun;
			SetSunShadowsUseCleanBackfaces(useCleanBackFaces);
		}
	}

	public void ToggleSunShadowCastersDrawInstanced()
	{
		_sunShadowCasting.UseDrawInstanced = !_sunShadowCasting.UseDrawInstanced;
		_gpuProgramStore.BlockyModelShadowMapProgram.UseDrawInstanced = _sunShadowCasting.UseDrawInstanced;
		_gpuProgramStore.BlockyModelShadowMapProgram.Reset();
		_gpuProgramStore.MapChunkShadowMapProgram.UseDrawInstanced = _sunShadowCasting.UseDrawInstanced;
		_gpuProgramStore.MapChunkShadowMapProgram.Reset();
	}

	public void ToggleSunShadowsBiasMethod1()
	{
		_gpuProgramStore.BlockyModelShadowMapProgram.UseBiasMethod1 = !_gpuProgramStore.BlockyModelShadowMapProgram.UseBiasMethod1;
		_gpuProgramStore.BlockyModelShadowMapProgram.UseBiasMethod2 = false;
		_gpuProgramStore.BlockyModelShadowMapProgram.Reset();
	}

	public void ToggleSunShadowsBiasMethod2()
	{
		_gpuProgramStore.BlockyModelShadowMapProgram.UseBiasMethod2 = !_gpuProgramStore.BlockyModelShadowMapProgram.UseBiasMethod2;
		_gpuProgramStore.BlockyModelShadowMapProgram.UseBiasMethod1 = false;
		_gpuProgramStore.BlockyModelShadowMapProgram.Reset();
	}

	public void SetSunShadowsCascadeCount(int count)
	{
		_cascadedShadowMapping.SetSunShadowsCascadeCount(count);
		int count2 = _cascadedShadowMapping.CascadesSettings.Count;
		_gpuProgramStore.ParticleErosionProgram.SunShadowCascadeCount = (uint)count2;
		_gpuProgramStore.ParticleErosionProgram.Reset();
		_gpuProgramStore.ParticleProgram.SunShadowCascadeCount = (uint)count2;
		_gpuProgramStore.ParticleProgram.Reset();
		_gpuProgramStore.MapChunkAlphaBlendedProgram.SunShadowCascadeCount = (uint)count2;
		_gpuProgramStore.MapChunkAlphaBlendedProgram.Reset();
		if (_gpuProgramStore.DeferredProgram.DebugShadowCascades)
		{
			_gpuProgramStore.DeferredProgram.CascadeCount = (uint)count2;
			_gpuProgramStore.DeferredProgram.Reset();
		}
	}

	public void SetSunShadowMappingUseLinearZ(bool enable)
	{
		_cascadedShadowMapping.SetSunShadowMappingUseLinearZ(enable);
		_gpuProgramStore.ParticleErosionProgram.UseLinearZ = enable;
		_gpuProgramStore.ParticleErosionProgram.Reset();
		_gpuProgramStore.ParticleProgram.UseLinearZ = enable;
		_gpuProgramStore.ParticleProgram.Reset();
		_gpuProgramStore.MapChunkAlphaBlendedProgram.UseLinearZ = enable;
		_gpuProgramStore.MapChunkAlphaBlendedProgram.Reset();
	}

	public void SetSunShadowsUseCleanBackfaces(bool enable)
	{
		_gpuProgramStore.DeferredShadowProgram.UseCleanBackfaces = enable;
		_gpuProgramStore.DeferredShadowProgram.Reset();
	}

	public void SetDeferredShadowsBlurEnabled(bool enable)
	{
		if (_cascadedShadowMapping.DeferredShadowSettings.UseBlur != enable)
		{
			_cascadedShadowMapping.SetDeferredShadowsBlurEnabled(enable);
			_gpuProgramStore.DeferredProgram.UseDeferredShadowBlurred = enable;
			_gpuProgramStore.DeferredProgram.Reset();
		}
	}

	public void SetSunShadowsGlobalKDopEnabled(bool enable)
	{
		_cascadedShadowMapping.SetSunShadowsGlobalKDopEnabled(enable);
	}

	public void SetSunShadowsSlopeScaleBias(float factor, float units)
	{
		_cascadedShadowMapping.SetSunShadowsSlopeScaleBias(factor, units);
	}

	public void SetSunShadowMapResolution(uint width, uint height = 0u)
	{
		_cascadedShadowMapping.SetSunShadowMapResolution(width, height);
	}

	public void SetDeferredShadowResolutionScale(float scale)
	{
		_cascadedShadowMapping.SetDeferredShadowResolutionScale(scale);
	}

	public void SetSunShadowMapCachingEnabled(bool enable)
	{
		_cascadedShadowMapping.SetSunShadowMapCachingEnabled(enable);
	}

	public void SetSunShadowMappingStableProjectionEnabled(bool enable)
	{
		_cascadedShadowMapping.SetSunShadowMappingStableProjectionEnabled(enable);
	}

	public void SetDeferredShadowsNoiseEnabled(bool enable)
	{
		_cascadedShadowMapping.SetDeferredShadowsNoiseEnabled(enable);
	}

	public void SetDeferredShadowsManualModeEnabled(bool enable)
	{
		_cascadedShadowMapping.SetDeferredShadowsManualModeEnabled(enable);
	}

	public void SetDeferredShadowsFadingEnabled(bool enable)
	{
		_cascadedShadowMapping.SetDeferredShadowsFadingEnabled(enable);
	}

	public void SetDeferredShadowsWithSingleSampleEnabled(bool enable)
	{
		_cascadedShadowMapping.SetDeferredShadowsWithSingleSampleEnabled(enable);
	}

	public void SetDeferredShadowsCameraBiasEnabled(bool enable)
	{
		_cascadedShadowMapping.SetDeferredShadowsCameraBiasEnabled(enable);
	}

	public void SetDeferredShadowsNormalBiasEnabled(bool enable)
	{
		_cascadedShadowMapping.SetDeferredShadowsNormalBiasEnabled(enable);
	}

	public int GetShadowCascadeDrawCallCount(int cascadeId)
	{
		Debug.Assert(cascadeId < 4);
		return _cascadeStats[cascadeId].DrawCallCount;
	}

	public int GetShadowCascadeKiloTriangleCount(int cascadeId)
	{
		Debug.Assert(cascadeId < 4);
		return _cascadeStats[cascadeId].KiloTriangleCount;
	}

	public void SetupEntityShadowMapDataTexture(uint unitId)
	{
		_gl.ActiveTexture((GL)(33984 + unitId));
		_gl.BindTexture(GL.TEXTURE_BUFFER, _entityShadowMapDataBufferTexture.CurrentTexture);
	}

	private void InitEntitiesShadowMapGPUData()
	{
		_entityShadowMapDataBufferTexture.CreateStorage(GL.RGBA32F, GL.STREAM_DRAW, useDoubleBuffering: true, _entityShadowMapBufferSize, 1024u, GPUBuffer.GrowthPolicy.GrowthAutoNoLimit);
	}

	private void DisposeEntitiesShadowMapGPUData()
	{
		_entityShadowMapDataBufferTexture.DestroyStorage();
	}

	private void PingPongEntityShadowMapDataBuffers()
	{
		_entityShadowMapDataBufferTexture.Swap();
	}

	public unsafe void SendEntityShadowMapDataToGPU()
	{
		uint num = (uint)(_entityShadowMapDrawTaskCount * GPUEntityShadowMapDataSize);
		if (num != 0)
		{
			_entityShadowMapDataBufferTexture.GrowStorageIfNecessary(num);
			IntPtr pointer = _entityShadowMapDataBufferTexture.BeginTransfer(num);
			for (int i = 0; i < _entityShadowMapDrawTaskCount; i++)
			{
				IntPtr pointer2 = IntPtr.Add(pointer, i * GPUEntityShadowMapDataSize);
				Matrix* ptr = (Matrix*)pointer2.ToPointer();
				*ptr = _entityShadowMapDrawTasks[i].ModelMatrix;
				Vector4* ptr2 = (Vector4*)IntPtr.Add(pointer2, sizeof(Matrix)).ToPointer();
				*ptr2 = new Vector4((int)_entityShadowMapDrawTasks[i].CascadeFirstLast.X, (int)_entityShadowMapDrawTasks[i].CascadeFirstLast.Y, 0f, 0f);
				ptr2[1] = new Vector4(_entityShadowMapDrawTasks[i].ModelVFXId, _entityShadowMapDrawTasks[i].InvModelHeight, _entityShadowMapDrawTasks[i].ModelVFXAnimationProgress, 0f);
			}
			_entityShadowMapDataBufferTexture.EndTransfer();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PrepareForIncomingEntitySunShadowCasterDrawTasks(int size)
	{
		_incomingEntityShadowMapDrawTaskCount += size;
		ArrayUtils.GrowArrayIfNecessary(ref _entityShadowMapDrawTasks, _incomingEntityShadowMapDrawTaskCount, 200);
		ArrayUtils.GrowArrayIfNecessary(ref _entitiesBoundingVolumes, _incomingEntityShadowMapDrawTaskCount, 200);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterEntitySunShadowCasterDrawTask(ref BoundingSphere boundingSphere, ref Matrix modelMatrix, GLVertexArray vertexArray, int dataCount, GLBuffer animationData, uint animationDataOffset, uint animationDataCount, float modelHeight, float modelVFXAnimationProgress, int modelVFXId)
	{
		int entityShadowMapDrawTaskCount = _entityShadowMapDrawTaskCount;
		_entitiesBoundingVolumes[entityShadowMapDrawTaskCount] = boundingSphere;
		_entityShadowMapDrawTasks[entityShadowMapDrawTaskCount].ModelMatrix = modelMatrix;
		_entityShadowMapDrawTasks[entityShadowMapDrawTaskCount].VertexArray = vertexArray;
		_entityShadowMapDrawTasks[entityShadowMapDrawTaskCount].DataCount = dataCount;
		_entityShadowMapDrawTasks[entityShadowMapDrawTaskCount].AnimationData = animationData;
		_entityShadowMapDrawTasks[entityShadowMapDrawTaskCount].AnimationDataOffset = animationDataOffset;
		_entityShadowMapDrawTasks[entityShadowMapDrawTaskCount].AnimationDataSize = (ushort)(animationDataCount * 64);
		_entityShadowMapDrawTasks[entityShadowMapDrawTaskCount].InvModelHeight = 1f / modelHeight;
		_entityShadowMapDrawTasks[entityShadowMapDrawTaskCount].ModelVFXAnimationProgress = modelVFXAnimationProgress;
		_entityShadowMapDrawTasks[entityShadowMapDrawTaskCount].ModelVFXId = modelVFXId;
		_entityShadowMapDrawTaskCount++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PrepareForIncomingMapChunkSunShadowCasterDrawTasks(int size)
	{
		_incomingChunkShadowMapDrawTaskCount += size;
		ArrayUtils.GrowArrayIfNecessary(ref _chunkShadowMapDrawTasks, _incomingChunkShadowMapDrawTaskCount, 200);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterMapChunkSunShadowCasterDrawTask(ref Matrix modelMatrix, GLVertexArray vertexArray, int dataCount, IntPtr dataOffset)
	{
		int chunkShadowMapDrawTaskCount = _chunkShadowMapDrawTaskCount;
		_chunkShadowMapDrawTasks[chunkShadowMapDrawTaskCount].ModelMatrix = modelMatrix;
		_chunkShadowMapDrawTasks[chunkShadowMapDrawTaskCount].VertexArray = vertexArray;
		_chunkShadowMapDrawTasks[chunkShadowMapDrawTaskCount].DataCount = dataCount;
		_chunkShadowMapDrawTasks[chunkShadowMapDrawTaskCount].DataOffset = dataOffset;
		_chunkShadowMapDrawTaskCount++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PrepareForIncomingMapBlockAnimatedSunShadowCasterDrawTasks(int size)
	{
		_incomingAnimatedBlockShadowMapDrawTaskCount += size;
		ArrayUtils.GrowArrayIfNecessary(ref _animatedBlockShadowMapDrawTasks, _incomingAnimatedBlockShadowMapDrawTaskCount, 200);
		ArrayUtils.GrowArrayIfNecessary(ref _animatedBlockBoundingVolumes, _incomingAnimatedBlockShadowMapDrawTaskCount, 200);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterMapBlockAnimatedSunShadowCasterDrawTask(ref BoundingBox boundingBox, ref Matrix modelMatrix, GLVertexArray vertexArray, int indicesCount, GLBuffer animationData, uint animationDataOffset, uint animationDataCount)
	{
		int animatedBlockShadowMapDrawTaskCount = _animatedBlockShadowMapDrawTaskCount;
		_animatedBlockBoundingVolumes[animatedBlockShadowMapDrawTaskCount] = boundingBox;
		_animatedBlockShadowMapDrawTasks[animatedBlockShadowMapDrawTaskCount].ModelMatrix = modelMatrix;
		_animatedBlockShadowMapDrawTasks[animatedBlockShadowMapDrawTaskCount].VertexArray = vertexArray;
		_animatedBlockShadowMapDrawTasks[animatedBlockShadowMapDrawTaskCount].DataCount = indicesCount;
		_animatedBlockShadowMapDrawTasks[animatedBlockShadowMapDrawTaskCount].DataOffset = IntPtr.Zero;
		_animatedBlockShadowMapDrawTasks[animatedBlockShadowMapDrawTaskCount].AnimationData = animationData;
		_animatedBlockShadowMapDrawTasks[animatedBlockShadowMapDrawTaskCount].AnimationDataOffset = animationDataOffset;
		_animatedBlockShadowMapDrawTasks[animatedBlockShadowMapDrawTaskCount].AnimationDataSize = (ushort)(animationDataCount * 64);
		_animatedBlockShadowMapDrawTaskCount++;
	}

	public void PrepareShadowCastersForDraw()
	{
		if ((!_sunShadowCasting.UseSmartCascadeDispatch && !_sunShadowCasting.UseDrawInstanced) || _cascadedShadowMapping.CascadesSettings.Count == 1)
		{
			return;
		}
		int itemCount = _entityShadowMapDrawTaskCount + _chunkShadowMapDrawTaskCount + _animatedBlockShadowMapDrawTaskCount;
		ArrayUtils.GrowArrayIfNecessary(ref _cascadeDrawTaskId[0], itemCount, 500);
		ArrayUtils.GrowArrayIfNecessary(ref _cascadeDrawTaskId[1], itemCount, 500);
		ArrayUtils.GrowArrayIfNecessary(ref _cascadeDrawTaskId[2], itemCount, 500);
		ArrayUtils.GrowArrayIfNecessary(ref _cascadeDrawTaskId[3], itemCount, 500);
		for (int i = 0; i < _entityShadowMapDrawTaskCount; i++)
		{
			ContainmentType containmentType = _cascadedShadowMapping.CascadeFrustums[0].Contains(_entitiesBoundingVolumes[i]);
			ContainmentType containmentType2 = _cascadedShadowMapping.CascadeFrustums[1].Contains(_entitiesBoundingVolumes[i]);
			ContainmentType containmentType3 = _cascadedShadowMapping.CascadeFrustums[2].Contains(_entitiesBoundingVolumes[i]);
			ContainmentType containmentType4 = _cascadedShadowMapping.CascadeFrustums[3].Contains(_entitiesBoundingVolumes[i]);
			if (_sunShadowCasting.UseDrawInstanced)
			{
				int num = 3;
				int num2 = 0;
				if (_cascadedShadowMapping.CascadeNeedsUpdate[0] && containmentType != 0)
				{
					num = System.Math.Min(num, 0);
					num2 = System.Math.Max(num2, 0);
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[1] && containmentType2 != 0 && containmentType != ContainmentType.Contains)
				{
					num = System.Math.Min(num, 1);
					num2 = System.Math.Max(num2, 1);
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[2] && containmentType3 != 0 && containmentType2 != ContainmentType.Contains)
				{
					num = System.Math.Min(num, 2);
					num2 = System.Math.Max(num2, 2);
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[3] && containmentType4 != 0 && containmentType3 != ContainmentType.Contains)
				{
					num = System.Math.Min(num, 3);
					num2 = System.Math.Max(num2, 3);
				}
				if (num <= num2)
				{
					ushort num3 = _cascadeEntityDrawTaskCount[0];
					_cascadeDrawTaskId[0][num3] = (ushort)i;
					_cascadeEntityDrawTaskCount[0]++;
					_entityShadowMapDrawTasks[i].CascadeFirstLast.X = (ushort)num;
					_entityShadowMapDrawTasks[i].CascadeFirstLast.Y = (ushort)num2;
				}
			}
			else
			{
				if (_cascadedShadowMapping.CascadeNeedsUpdate[0] && containmentType != 0)
				{
					ushort num4 = _cascadeEntityDrawTaskCount[0];
					_cascadeDrawTaskId[0][num4] = (ushort)i;
					_cascadeEntityDrawTaskCount[0]++;
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[1] && containmentType2 != 0 && containmentType != ContainmentType.Contains)
				{
					ushort num5 = _cascadeEntityDrawTaskCount[1];
					_cascadeDrawTaskId[1][num5] = (ushort)i;
					_cascadeEntityDrawTaskCount[1]++;
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[2] && containmentType3 != 0 && containmentType2 != ContainmentType.Contains)
				{
					ushort num6 = _cascadeEntityDrawTaskCount[2];
					_cascadeDrawTaskId[2][num6] = (ushort)i;
					_cascadeEntityDrawTaskCount[2]++;
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[3] && containmentType4 != 0 && containmentType3 != ContainmentType.Contains)
				{
					ushort num7 = _cascadeEntityDrawTaskCount[3];
					_cascadeDrawTaskId[3][num7] = (ushort)i;
					_cascadeEntityDrawTaskCount[3]++;
				}
			}
		}
		BoundingBox box = default(BoundingBox);
		for (int j = 0; j < _chunkShadowMapDrawTaskCount; j++)
		{
			box.Min = _chunkShadowMapDrawTasks[j].ModelMatrix.Translation;
			box.Max = _chunkShadowMapDrawTasks[j].ModelMatrix.Translation + new Vector3(32f);
			ContainmentType containmentType5 = _cascadedShadowMapping.CascadeFrustums[0].Contains(box);
			ContainmentType containmentType6 = _cascadedShadowMapping.CascadeFrustums[1].Contains(box);
			ContainmentType containmentType7 = _cascadedShadowMapping.CascadeFrustums[2].Contains(box);
			ContainmentType containmentType8 = _cascadedShadowMapping.CascadeFrustums[3].Contains(box);
			if (_sunShadowCasting.UseDrawInstanced)
			{
				int num8 = 3;
				int num9 = 0;
				if (_cascadedShadowMapping.CascadeNeedsUpdate[0] && containmentType5 != 0)
				{
					num8 = System.Math.Min(num8, 0);
					num9 = System.Math.Max(num9, 0);
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[1] && containmentType6 != 0 && containmentType5 != ContainmentType.Contains)
				{
					num8 = System.Math.Min(num8, 1);
					num9 = System.Math.Max(num9, 1);
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[2] && containmentType7 != 0 && containmentType6 != ContainmentType.Contains)
				{
					num8 = System.Math.Min(num8, 2);
					num9 = System.Math.Max(num9, 2);
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[3] && containmentType8 != 0 && containmentType7 != ContainmentType.Contains)
				{
					num8 = System.Math.Min(num8, 3);
					num9 = System.Math.Max(num9, 3);
				}
				if (num8 <= num9)
				{
					int num10 = _cascadeChunkDrawTaskCount[0] + _cascadeEntityDrawTaskCount[0];
					_cascadeDrawTaskId[0][num10] = (ushort)j;
					_cascadeChunkDrawTaskCount[0]++;
					_chunkShadowMapDrawTasks[j].CascadeFirstLast.X = (ushort)num8;
					_chunkShadowMapDrawTasks[j].CascadeFirstLast.Y = (ushort)num9;
				}
			}
			else
			{
				if (_cascadedShadowMapping.CascadeNeedsUpdate[0] && containmentType5 != 0)
				{
					int num11 = _cascadeChunkDrawTaskCount[0] + _cascadeEntityDrawTaskCount[0];
					_cascadeDrawTaskId[0][num11] = (ushort)j;
					_cascadeChunkDrawTaskCount[0]++;
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[1] && containmentType6 != 0 && containmentType5 != ContainmentType.Contains)
				{
					int num12 = _cascadeChunkDrawTaskCount[1] + _cascadeEntityDrawTaskCount[1];
					_cascadeDrawTaskId[1][num12] = (ushort)j;
					_cascadeChunkDrawTaskCount[1]++;
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[2] && containmentType7 != 0 && containmentType6 != ContainmentType.Contains)
				{
					int num13 = _cascadeChunkDrawTaskCount[2] + _cascadeEntityDrawTaskCount[2];
					_cascadeDrawTaskId[2][num13] = (ushort)j;
					_cascadeChunkDrawTaskCount[2]++;
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[3] && containmentType8 != 0 && containmentType7 != ContainmentType.Contains)
				{
					int num14 = _cascadeChunkDrawTaskCount[3] + _cascadeEntityDrawTaskCount[3];
					_cascadeDrawTaskId[3][num14] = (ushort)j;
					_cascadeChunkDrawTaskCount[3]++;
				}
			}
		}
		for (int k = 0; k < _animatedBlockShadowMapDrawTaskCount; k++)
		{
			ContainmentType containmentType9 = _cascadedShadowMapping.CascadeFrustums[0].Contains(_animatedBlockBoundingVolumes[k]);
			ContainmentType containmentType10 = _cascadedShadowMapping.CascadeFrustums[1].Contains(_animatedBlockBoundingVolumes[k]);
			ContainmentType containmentType11 = _cascadedShadowMapping.CascadeFrustums[2].Contains(_animatedBlockBoundingVolumes[k]);
			ContainmentType containmentType12 = _cascadedShadowMapping.CascadeFrustums[3].Contains(_animatedBlockBoundingVolumes[k]);
			if (_sunShadowCasting.UseDrawInstanced)
			{
				int num15 = 3;
				int num16 = 0;
				if (_cascadedShadowMapping.CascadeNeedsUpdate[0] && containmentType9 != 0)
				{
					num15 = System.Math.Min(num15, 0);
					num16 = System.Math.Max(num16, 0);
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[1] && containmentType10 != 0 && containmentType9 != ContainmentType.Contains)
				{
					num15 = System.Math.Min(num15, 1);
					num16 = System.Math.Max(num16, 1);
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[2] && containmentType11 != 0 && containmentType10 != ContainmentType.Contains)
				{
					num15 = System.Math.Min(num15, 2);
					num16 = System.Math.Max(num16, 2);
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[3] && containmentType12 != 0 && containmentType11 != ContainmentType.Contains)
				{
					num15 = System.Math.Min(num15, 3);
					num16 = System.Math.Max(num16, 3);
				}
				if (num15 <= num16)
				{
					ushort num17 = _cascadeEntityDrawTaskCount[0];
					_cascadeDrawTaskId[0][num17] = (ushort)k;
					_cascadeAnimatedBlockDrawTaskCount[0]++;
					_animatedBlockShadowMapDrawTasks[k].CascadeFirstLast.X = (ushort)num15;
					_animatedBlockShadowMapDrawTasks[k].CascadeFirstLast.Y = (ushort)num16;
				}
			}
			else
			{
				if (_cascadedShadowMapping.CascadeNeedsUpdate[0] && containmentType9 != 0)
				{
					int num18 = _cascadeAnimatedBlockDrawTaskCount[0] + _cascadeChunkDrawTaskCount[0] + _cascadeEntityDrawTaskCount[0];
					_cascadeDrawTaskId[0][num18] = (ushort)k;
					_cascadeAnimatedBlockDrawTaskCount[0]++;
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[1] && containmentType10 != 0 && containmentType9 != ContainmentType.Contains)
				{
					int num19 = _cascadeAnimatedBlockDrawTaskCount[1] + _cascadeChunkDrawTaskCount[1] + _cascadeEntityDrawTaskCount[1];
					_cascadeDrawTaskId[1][num19] = (ushort)k;
					_cascadeAnimatedBlockDrawTaskCount[1]++;
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[2] && containmentType11 != 0 && containmentType10 != ContainmentType.Contains)
				{
					int num20 = _cascadeAnimatedBlockDrawTaskCount[2] + _cascadeChunkDrawTaskCount[2] + _cascadeEntityDrawTaskCount[2];
					_cascadeDrawTaskId[2][num20] = (ushort)k;
					_cascadeAnimatedBlockDrawTaskCount[2]++;
				}
				if (_cascadedShadowMapping.CascadeNeedsUpdate[3] && containmentType12 != 0 && containmentType11 != ContainmentType.Contains)
				{
					int num21 = _cascadeAnimatedBlockDrawTaskCount[3] + _cascadeChunkDrawTaskCount[3] + _cascadeEntityDrawTaskCount[3];
					_cascadeDrawTaskId[3][num21] = (ushort)k;
					_cascadeAnimatedBlockDrawTaskCount[3]++;
				}
			}
		}
	}

	private void DrawShadowCasters()
	{
		GLFunctions gL = _graphics.GL;
		int num = _renderTargetStore.ShadowMap.Width / _cascadedShadowMapping.CascadesSettings.Count;
		if (_sunShadowCasting.UseDrawInstanced)
		{
			int drawnVertices = gL.DrawnVertices;
			int drawCallsCount = gL.DrawCallsCount;
			float num2 = 1f / (float)_cascadedShadowMapping.CascadesSettings.Count;
			_gl.Viewport(0, 0, _renderTargetStore.ShadowMap.Width, _renderTargetStore.ShadowMap.Height);
			DrawEntityShadowCasters();
			if (_sunShadowCasting.UseChunkShadowCasters)
			{
				DrawMapChunkShadowCasters();
				DrawMapBlockAnimatedShadowCasters();
			}
			_cascadeStats[0].KiloTriangleCount = (ushort)((gL.DrawnVertices - drawnVertices) / 3000);
			_cascadeStats[0].DrawCallCount = (ushort)(gL.DrawCallsCount - drawCallsCount);
			for (int i = 1; i < _cascadeStats.Length; i++)
			{
				_cascadeStats[i].KiloTriangleCount = 0;
				_cascadeStats[i].DrawCallCount = 0;
			}
			return;
		}
		for (int j = 0; j < _cascadedShadowMapping.CascadesSettings.Count; j++)
		{
			if (_cascadedShadowMapping.CascadeNeedsUpdate[j])
			{
				int x = j * num;
				int drawnVertices2 = gL.DrawnVertices;
				int drawCallsCount2 = gL.DrawCallsCount;
				_gl.Viewport(x, 0, num, _renderTargetStore.ShadowMap.Height);
				DrawEntityShadowCasters(j);
				if (_sunShadowCasting.UseChunkShadowCasters)
				{
					DrawMapChunkShadowCasters(j);
					DrawMapBlockAnimatedShadowCasters(j);
				}
				_cascadeStats[j].KiloTriangleCount = (ushort)((gL.DrawnVertices - drawnVertices2) / 3000);
				_cascadeStats[j].DrawCallCount = (ushort)(gL.DrawCallsCount - drawCallsCount2);
			}
			else
			{
				_cascadeStats[j].KiloTriangleCount = 0;
				_cascadeStats[j].DrawCallCount = 0;
			}
		}
	}

	private void DrawEntityShadowCasters(int targetCascade = -1)
	{
		Debug.Assert(targetCascade != -1 || _sunShadowCasting.UseDrawInstanced, "Invalid usage - either UseDrawInstanced, or specify a target shadow cascade.");
		Debug.Assert(targetCascade < _cascadedShadowMapping.CascadesSettings.Count, $"Invalid usage - impossible to draw cascade {targetCascade} when there are only {_cascadedShadowMapping.CascadesSettings.Count}.");
		GLFunctions gL = _graphics.GL;
		ZOnlyBlockyModelProgram blockyModelShadowMapProgram = _gpuProgramStore.BlockyModelShadowMapProgram;
		gL.UseProgram(blockyModelShadowMapProgram);
		if (_sunShadowCasting.UseDrawInstanced)
		{
			float x = 1f / (float)_cascadedShadowMapping.CascadesSettings.Count;
			blockyModelShadowMapProgram.ViewportInfos.SetValue(x, _renderTargetStore.ShadowMap.Width);
			if (blockyModelShadowMapProgram.UseBiasMethod2)
			{
				blockyModelShadowMapProgram.ViewMatrix.SetValue(Data.SunShadowRenderData.VirtualSunViewRotationMatrix);
			}
			blockyModelShadowMapProgram.ViewProjectionMatrix.SetValue(Data.SunShadowRenderData.VirtualSunViewRotationProjectionMatrix);
		}
		else
		{
			if (blockyModelShadowMapProgram.UseBiasMethod2)
			{
				blockyModelShadowMapProgram.ViewMatrix.SetValue(ref Data.SunShadowRenderData.VirtualSunViewRotationMatrix[targetCascade]);
			}
			blockyModelShadowMapProgram.ViewProjectionMatrix.SetValue(ref Data.SunShadowRenderData.VirtualSunViewRotationProjectionMatrix[targetCascade]);
		}
		if (_sunShadowCasting.UseDrawInstanced && _cascadedShadowMapping.CascadesSettings.Count > 1)
		{
			if (_sunShadowCasting.UseEntitiesModelVFX)
			{
				blockyModelShadowMapProgram.Time.SetValue(Data.Time);
				for (int i = 0; i < _cascadeEntityDrawTaskCount[0]; i++)
				{
					ushort num = _cascadeDrawTaskId[0][i];
					ref EntityShadowMapDrawTask reference = ref _entityShadowMapDrawTasks[num];
					blockyModelShadowMapProgram.DrawId.SetValue(num);
					blockyModelShadowMapProgram.ModelVFXId.SetValue(reference.ModelVFXId);
					blockyModelShadowMapProgram.NodeBlock.SetBufferRange(reference.AnimationData, reference.AnimationDataOffset, reference.AnimationDataSize);
					gL.BindVertexArray(reference.VertexArray);
					gL.DrawElementsInstanced(GL.TRIANGLES, reference.DataCount, GL.UNSIGNED_SHORT, (IntPtr)0, reference.CascadeFirstLast.Y - reference.CascadeFirstLast.X + 1);
				}
			}
			else
			{
				for (int j = 0; j < _cascadeEntityDrawTaskCount[0]; j++)
				{
					ushort num2 = _cascadeDrawTaskId[0][j];
					ref EntityShadowMapDrawTask reference2 = ref _entityShadowMapDrawTasks[num2];
					blockyModelShadowMapProgram.DrawId.SetValue(num2);
					blockyModelShadowMapProgram.NodeBlock.SetBufferRange(reference2.AnimationData, reference2.AnimationDataOffset, reference2.AnimationDataSize);
					gL.BindVertexArray(reference2.VertexArray);
					gL.DrawElementsInstanced(GL.TRIANGLES, reference2.DataCount, GL.UNSIGNED_SHORT, (IntPtr)0, reference2.CascadeFirstLast.Y - reference2.CascadeFirstLast.X + 1);
				}
			}
		}
		else if (_sunShadowCasting.UseSmartCascadeDispatch && _cascadedShadowMapping.CascadesSettings.Count > 1)
		{
			if (_sunShadowCasting.UseEntitiesModelVFX)
			{
				blockyModelShadowMapProgram.Time.SetValue(Data.Time);
				for (int k = 0; k < _cascadeEntityDrawTaskCount[targetCascade]; k++)
				{
					ushort num3 = _cascadeDrawTaskId[targetCascade][k];
					ref EntityShadowMapDrawTask reference3 = ref _entityShadowMapDrawTasks[num3];
					blockyModelShadowMapProgram.DrawId.SetValue(num3);
					blockyModelShadowMapProgram.ModelVFXId.SetValue(reference3.ModelVFXId);
					blockyModelShadowMapProgram.NodeBlock.SetBufferRange(reference3.AnimationData, reference3.AnimationDataOffset, reference3.AnimationDataSize);
					gL.BindVertexArray(reference3.VertexArray);
					gL.DrawElements(GL.TRIANGLES, reference3.DataCount, GL.UNSIGNED_SHORT, (IntPtr)0);
				}
			}
			else
			{
				for (int l = 0; l < _cascadeEntityDrawTaskCount[targetCascade]; l++)
				{
					ushort num4 = _cascadeDrawTaskId[targetCascade][l];
					ref EntityShadowMapDrawTask reference4 = ref _entityShadowMapDrawTasks[num4];
					blockyModelShadowMapProgram.DrawId.SetValue(num4);
					blockyModelShadowMapProgram.NodeBlock.SetBufferRange(reference4.AnimationData, reference4.AnimationDataOffset, reference4.AnimationDataSize);
					gL.BindVertexArray(reference4.VertexArray);
					gL.DrawElements(GL.TRIANGLES, reference4.DataCount, GL.UNSIGNED_SHORT, (IntPtr)0);
				}
			}
		}
		else if (_sunShadowCasting.UseEntitiesModelVFX)
		{
			blockyModelShadowMapProgram.Time.SetValue(Data.Time);
			for (int m = 0; m < _entityShadowMapDrawTaskCount; m++)
			{
				ref EntityShadowMapDrawTask reference5 = ref _entityShadowMapDrawTasks[m];
				blockyModelShadowMapProgram.DrawId.SetValue(m);
				blockyModelShadowMapProgram.ModelVFXId.SetValue(reference5.ModelVFXId);
				blockyModelShadowMapProgram.NodeBlock.SetBufferRange(reference5.AnimationData, reference5.AnimationDataOffset, reference5.AnimationDataSize);
				gL.BindVertexArray(reference5.VertexArray);
				gL.DrawElements(GL.TRIANGLES, reference5.DataCount, GL.UNSIGNED_SHORT, (IntPtr)0);
			}
		}
		else
		{
			for (int n = 0; n < _entityShadowMapDrawTaskCount; n++)
			{
				ref EntityShadowMapDrawTask reference6 = ref _entityShadowMapDrawTasks[n];
				blockyModelShadowMapProgram.DrawId.SetValue(n);
				blockyModelShadowMapProgram.NodeBlock.SetBufferRange(reference6.AnimationData, reference6.AnimationDataOffset, reference6.AnimationDataSize);
				gL.BindVertexArray(reference6.VertexArray);
				gL.DrawElements(GL.TRIANGLES, reference6.DataCount, GL.UNSIGNED_SHORT, (IntPtr)0);
			}
		}
	}

	private void DrawMapChunkShadowCasters(int targetCascade = -1)
	{
		Debug.Assert(targetCascade != -1 || _sunShadowCasting.UseDrawInstanced, "Invalid usage - either UseDrawInstanced, or specify a target shadow cascade.");
		Debug.Assert(targetCascade < _cascadedShadowMapping.CascadesSettings.Count, $"Invalid usage - impossible to draw cascade {targetCascade} when there are only {_cascadedShadowMapping.CascadesSettings.Count}.");
		GLFunctions gL = _graphics.GL;
		gL.Disable(GL.CULL_FACE);
		ZOnlyChunkProgram mapChunkShadowMapProgram = _gpuProgramStore.MapChunkShadowMapProgram;
		gL.UseProgram(mapChunkShadowMapProgram);
		if (_sunShadowCasting.UseDrawInstanced)
		{
			float x = 1f / (float)_cascadedShadowMapping.CascadesSettings.Count;
			mapChunkShadowMapProgram.ViewportInfos.SetValue(x, _renderTargetStore.ShadowMap.Width);
			mapChunkShadowMapProgram.ViewProjectionMatrix.SetValue(Data.SunShadowRenderData.VirtualSunViewRotationProjectionMatrix);
			mapChunkShadowMapProgram.LightPositions.SetValue(Data.SunShadowRenderData.VirtualSunPositions);
		}
		else
		{
			mapChunkShadowMapProgram.ViewProjectionMatrix.SetValue(ref Data.SunShadowRenderData.VirtualSunViewRotationProjectionMatrix[targetCascade]);
			mapChunkShadowMapProgram.LightPositions.SetValue(Data.SunShadowRenderData.VirtualSunPositions[targetCascade]);
		}
		mapChunkShadowMapProgram.Time.SetValue(Data.Time);
		if (_sunShadowCasting.UseDrawInstanced && _cascadedShadowMapping.CascadesSettings.Count > 1)
		{
			ushort num = _cascadeEntityDrawTaskCount[0];
			for (int i = 0; i < _cascadeChunkDrawTaskCount[0]; i++)
			{
				ushort num2 = _cascadeDrawTaskId[0][num + i];
				ref ChunkShadowMapDrawTask reference = ref _chunkShadowMapDrawTasks[num2];
				mapChunkShadowMapProgram.TargetCascades.SetValue(reference.CascadeFirstLast.X, reference.CascadeFirstLast.Y);
				mapChunkShadowMapProgram.ModelMatrix.SetValue(ref reference.ModelMatrix);
				gL.BindVertexArray(reference.VertexArray);
				gL.DrawElementsInstanced(GL.TRIANGLES, reference.DataCount, GL.UNSIGNED_INT, reference.DataOffset, reference.CascadeFirstLast.Y - reference.CascadeFirstLast.X + 1);
			}
		}
		else if (_sunShadowCasting.UseSmartCascadeDispatch && _cascadedShadowMapping.CascadesSettings.Count > 1)
		{
			ushort num3 = _cascadeEntityDrawTaskCount[targetCascade];
			mapChunkShadowMapProgram.TargetCascades.SetValue(targetCascade, 0);
			for (int j = 0; j < _cascadeChunkDrawTaskCount[targetCascade]; j++)
			{
				ushort num4 = _cascadeDrawTaskId[targetCascade][num3 + j];
				ref ChunkShadowMapDrawTask reference2 = ref _chunkShadowMapDrawTasks[num4];
				mapChunkShadowMapProgram.ModelMatrix.SetValue(ref reference2.ModelMatrix);
				gL.BindVertexArray(reference2.VertexArray);
				gL.DrawElements(GL.TRIANGLES, reference2.DataCount, GL.UNSIGNED_INT, reference2.DataOffset);
			}
		}
		else
		{
			mapChunkShadowMapProgram.TargetCascades.SetValue(targetCascade, 0);
			for (int k = 0; k < _chunkShadowMapDrawTaskCount; k++)
			{
				ref ChunkShadowMapDrawTask reference3 = ref _chunkShadowMapDrawTasks[k];
				mapChunkShadowMapProgram.ModelMatrix.SetValue(ref reference3.ModelMatrix);
				gL.BindVertexArray(reference3.VertexArray);
				gL.DrawElements(GL.TRIANGLES, reference3.DataCount, GL.UNSIGNED_INT, reference3.DataOffset);
			}
		}
		gL.Enable(GL.CULL_FACE);
	}

	private void DrawMapBlockAnimatedShadowCasters(int targetCascade = -1)
	{
		Debug.Assert(targetCascade != -1 || _sunShadowCasting.UseDrawInstanced, "Invalid usage - either UseDrawInstanced, or specify a target shadow cascade.");
		Debug.Assert(targetCascade < _cascadedShadowMapping.CascadesSettings.Count, $"Invalid usage - impossible to draw cascade {targetCascade} when there are only {_cascadedShadowMapping.CascadesSettings.Count}.");
		GLFunctions gL = _graphics.GL;
		gL.Disable(GL.CULL_FACE);
		ZOnlyChunkProgram mapBlockAnimatedShadowMapProgram = _gpuProgramStore.MapBlockAnimatedShadowMapProgram;
		gL.UseProgram(mapBlockAnimatedShadowMapProgram);
		if (_sunShadowCasting.UseDrawInstanced)
		{
			float x = 1f / (float)_cascadedShadowMapping.CascadesSettings.Count;
			mapBlockAnimatedShadowMapProgram.ViewportInfos.SetValue(x, _renderTargetStore.ShadowMap.Width);
			mapBlockAnimatedShadowMapProgram.ViewProjectionMatrix.SetValue(Data.SunShadowRenderData.VirtualSunViewRotationProjectionMatrix);
			mapBlockAnimatedShadowMapProgram.LightPositions.SetValue(Data.SunShadowRenderData.VirtualSunPositions);
		}
		else
		{
			mapBlockAnimatedShadowMapProgram.ViewProjectionMatrix.SetValue(ref Data.SunShadowRenderData.VirtualSunViewRotationProjectionMatrix[targetCascade]);
			mapBlockAnimatedShadowMapProgram.LightPositions.SetValue(Data.SunShadowRenderData.VirtualSunPositions[targetCascade]);
		}
		mapBlockAnimatedShadowMapProgram.Time.SetValue(Data.Time);
		if (_sunShadowCasting.UseDrawInstanced && _cascadedShadowMapping.CascadesSettings.Count > 1)
		{
			int num = _cascadeChunkDrawTaskCount[0] + _cascadeEntityDrawTaskCount[0];
			for (int i = 0; i < _cascadeAnimatedBlockDrawTaskCount[0]; i++)
			{
				ushort num2 = _cascadeDrawTaskId[0][num + i];
				ref AnimatedBlockShadowMapDrawTask reference = ref _animatedBlockShadowMapDrawTasks[num2];
				mapBlockAnimatedShadowMapProgram.TargetCascades.SetValue(reference.CascadeFirstLast.X, reference.CascadeFirstLast.Y);
				mapBlockAnimatedShadowMapProgram.ModelMatrix.SetValue(ref reference.ModelMatrix);
				mapBlockAnimatedShadowMapProgram.NodeBlock.SetBufferRange(reference.AnimationData, reference.AnimationDataOffset, reference.AnimationDataSize);
				gL.BindVertexArray(reference.VertexArray);
				gL.DrawElementsInstanced(GL.TRIANGLES, reference.DataCount, GL.UNSIGNED_INT, reference.DataOffset, reference.CascadeFirstLast.Y - reference.CascadeFirstLast.X + 1);
			}
		}
		else if (_sunShadowCasting.UseSmartCascadeDispatch && _cascadedShadowMapping.CascadesSettings.Count > 1)
		{
			int num3 = _cascadeChunkDrawTaskCount[targetCascade] + _cascadeEntityDrawTaskCount[targetCascade];
			mapBlockAnimatedShadowMapProgram.TargetCascades.SetValue(targetCascade, 0);
			for (int j = 0; j < _cascadeAnimatedBlockDrawTaskCount[targetCascade]; j++)
			{
				ushort num4 = _cascadeDrawTaskId[targetCascade][num3 + j];
				ref AnimatedBlockShadowMapDrawTask reference2 = ref _animatedBlockShadowMapDrawTasks[num4];
				mapBlockAnimatedShadowMapProgram.ModelMatrix.SetValue(ref reference2.ModelMatrix);
				mapBlockAnimatedShadowMapProgram.NodeBlock.SetBufferRange(reference2.AnimationData, reference2.AnimationDataOffset, reference2.AnimationDataSize);
				gL.BindVertexArray(reference2.VertexArray);
				gL.DrawElements(GL.TRIANGLES, reference2.DataCount, GL.UNSIGNED_INT, reference2.DataOffset);
			}
		}
		else
		{
			mapBlockAnimatedShadowMapProgram.TargetCascades.SetValue(targetCascade, 0);
			for (int k = 0; k < _animatedBlockShadowMapDrawTaskCount; k++)
			{
				ref AnimatedBlockShadowMapDrawTask reference3 = ref _animatedBlockShadowMapDrawTasks[k];
				mapBlockAnimatedShadowMapProgram.ModelMatrix.SetValue(ref reference3.ModelMatrix);
				mapBlockAnimatedShadowMapProgram.NodeBlock.SetBufferRange(reference3.AnimationData, reference3.AnimationDataOffset, reference3.AnimationDataSize);
				gL.BindVertexArray(reference3.VertexArray);
				gL.DrawElements(GL.TRIANGLES, reference3.DataCount, GL.UNSIGNED_INT, reference3.DataOffset);
			}
		}
		gL.Enable(GL.CULL_FACE);
	}

	public void UpdateSunShadowRenderData()
	{
		Vector3 vector = _sunShadowCasting.Direction;
		Data.SunShadowRenderData.DynamicShadowIntensity = _sunShadowCasting.ShadowIntensity;
		if (_sunShadowCasting.DirectionType == SunShadowCastingSettings.ShadowDirectionType.DynamicSun)
		{
			float num = Data.SunPositionWS.Y + 0.2f;
			vector = ((!(num > 0f)) ? Data.SunPositionWS : (-Data.SunPositionWS));
			Data.SunShadowRenderData.DynamicShadowIntensity = MathHelper.Lerp(_sunShadowCasting.ShadowIntensity, 0.99f, 1f - MathHelper.Clamp(System.Math.Abs(num) * 10f, 0f, 1f));
		}
		if (_sunShadowCasting.UseSafeAngle)
		{
			vector = Vector3.Lerp(vector, Vector3.Down, 0.35f);
		}
		CascadedShadowMapping.InputParams csmInputParams = default(CascadedShadowMapping.InputParams);
		csmInputParams.LightDirection = vector;
		csmInputParams.WorldFieldOfView = Data.WorldFieldOfView;
		csmInputParams.AspectRatio = Data.AspectRatio;
		csmInputParams.NearClipDistance = 0.1f;
		csmInputParams.CameraPosition = Data.CameraPosition;
		csmInputParams.ViewRotationMatrix = Data.ViewRotationMatrix;
		csmInputParams.ViewRotationProjectionMatrix = Data.ViewRotationProjectionMatrix;
		csmInputParams.IsSpatialContinuityLost = IsSpatialContinuityLost();
		csmInputParams.QuantifiedCameraMotion = QuantifyCameraMotion();
		csmInputParams.CameraPositionDelta = Data.CameraPosition - PreviousData.CameraPosition;
		csmInputParams.FrameId = Data.FrameCounter;
		_cascadedShadowMapping.Update(ref csmInputParams, ref Data.SunShadowRenderData);
	}

	public void BuildShadowMap()
	{
		_cascadedShadowMapping.BuildShadowMap();
	}

	public void DrawDeferredShadow()
	{
		_cascadedShadowMapping.DrawDeferredShadow(SceneDataBuffer, Data.FrustumFarCornersWS);
	}

	public void ToggleFreeze()
	{
		_cascadedShadowMapping.ToggleFreeze();
	}

	public void ToggleCameraFrustumDebug()
	{
		_cascadedShadowMapping.ToggleCameraFrustumDebug(Data.CameraPosition);
	}

	public void ToggleCameraFrustumSplitsDebug()
	{
		_cascadedShadowMapping.ToggleCameraFrustumSplitsDebug(Data.CameraPosition);
	}

	public void ToggleShadowCascadeFrustumDebug()
	{
		_cascadedShadowMapping.ToggleShadowCascadeFrustumDebug(Data.CameraPosition);
	}

	public void DebugDrawShadowRelated()
	{
		_cascadedShadowMapping.DebugDrawShadowRelated(ref Data.ViewProjectionMatrix);
	}

	public void ToggleSunShadowMapCascadeDebug()
	{
		DeferredProgram deferredProgram = _gpuProgramStore.DeferredProgram;
		deferredProgram.CascadeCount = (uint)_cascadedShadowMapping.CascadesSettings.Count;
		deferredProgram.DebugShadowCascades = !deferredProgram.DebugShadowCascades;
		deferredProgram.Reset();
	}

	public string WriteShadowMappingStateToString()
	{
		string text = "ShadowMapping state :";
		text = text + "\n.Enabled: " + UseSunShadows;
		text = text + "\n.Intensity: " + _sunShadowCasting.ShadowIntensity;
		text = text + "\n.Light dir.: " + _sunShadowCasting.DirectionType;
		text = text + "\n.Safe angle: " + _sunShadowCasting.UseSafeAngle;
		text = text + "\n.Chunks shadow: " + _sunShadowCasting.UseChunkShadowCasters;
		text = text + "\n.ModelVFX shadow: " + _sunShadowCasting.UseEntitiesModelVFX;
		string text2 = (_gpuProgramStore.BlockyModelShadowMapProgram.UseBiasMethod1 ? "model hack#1, " : "");
		text2 += (_gpuProgramStore.BlockyModelShadowMapProgram.UseBiasMethod2 ? "model hack#2, " : "");
		text = text + "\n.Bias methods: " + text2;
		text = text + "\n.Draw Instanced: " + _sunShadowCasting.UseDrawInstanced;
		text = text + "\n.Cascade smart dispatch: " + _sunShadowCasting.UseSmartCascadeDispatch;
		return text + "\n" + _cascadedShadowMapping.WriteShadowMappingStateToString();
	}
}
