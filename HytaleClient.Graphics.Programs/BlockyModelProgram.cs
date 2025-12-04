using System;
using System.Collections.Generic;
using HytaleClient.Data.BlockyModels;

namespace HytaleClient.Graphics.Programs;

internal class BlockyModelProgram : GPUProgram
{
	public UniformBufferObject SceneDataBlock;

	public UniformBufferObject NodeBlock;

	public UniformBufferObject PointLightBlock;

	public Uniform DrawId;

	public Uniform CurrentInvViewportSize;

	public Uniform InvModelHeight;

	public Uniform ModelMatrix;

	public Uniform StaticLightColor;

	public Uniform BottomTint;

	public Uniform TopTint;

	public Uniform UseDithering;

	public Uniform ModelVFXAnimationProgress;

	public Uniform ModelVFXHighlightColorAndThickness;

	public Uniform ModelVFXNoiseParams;

	public Uniform ModelVFXPackedParams;

	public Uniform ModelVFXPostColor;

	public Uniform AtlasSizeFactor0;

	public Uniform AtlasSizeFactor1;

	public Uniform AtlasSizeFactor2;

	public Uniform ViewMatrix;

	public Uniform ViewProjectionMatrix;

	public Uniform NearScreendoorThreshold;

	private Uniform Texture0;

	private Uniform Texture1;

	private Uniform Texture2;

	private Uniform GradientAtlasTexture;

	private Uniform NoiseTexture;

	private Uniform EntityDataBuffer;

	private Uniform ModelVFXDataBuffer;

	private Uniform LightGridTexture;

	private Uniform LightIndicesBufferTexture;

	private Uniform LightBufferTexture;

	public readonly Attrib AttribNodeIndex;

	public readonly Attrib AttribAtlasIndexAndShadingModeAndGradientId;

	public readonly Attrib AttribPosition;

	public readonly Attrib AttribTexCoords;

	public bool Deferred;

	public bool UseLightBufferCompression;

	public bool UseLightDirectAccess = true;

	public bool UseCustomZDistribution = true;

	private readonly bool _useForwardClusteredLighting = true;

	private readonly bool _useEntityDataBuffer;

	private readonly bool _useDistortionRT;

	private readonly bool _useCompleteForwardVersion;

	private readonly bool _firstPersonView;

	private readonly bool _useSceneDataOverride;

	public BlockyModelProgram(bool useDeferred, bool useSceneDataOverride, bool useCompleteForwardVersion, bool firstPersonView = false, bool useEntityDataBuffer = false, bool useDistortionRT = false, string variationName = null)
		: base("BlockyModelVS.glsl", "BlockyModelFS.glsl", variationName)
	{
		Deferred = !useDistortionRT && useDeferred;
		_useSceneDataOverride = useSceneDataOverride;
		_firstPersonView = firstPersonView;
		_useCompleteForwardVersion = !useDistortionRT && !useDeferred && useCompleteForwardVersion;
		_useForwardClusteredLighting = _useCompleteForwardVersion;
		_useEntityDataBuffer = useEntityDataBuffer;
		_useDistortionRT = useDistortionRT;
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("DEFERRED", Deferred ? "1" : "0");
		dictionary.Add("MAX_NODES_COUNT", BlockyModel.MaxNodeCount.ToString());
		dictionary.Add("COMPLETE_VERSION", _useCompleteForwardVersion ? "1" : "0");
		dictionary.Add("FIRST_PERSON_VIEW", _firstPersonView ? "1" : "0");
		dictionary.Add("USE_CLUSTERED_LIGHTING", (!Deferred && _useForwardClusteredLighting) ? "1" : "0");
		dictionary.Add("USE_SCENE_DATA_OVERRIDE", _useSceneDataOverride ? "1" : "0");
		dictionary.Add("USE_ENTITY_DATA_BUFFER", _useEntityDataBuffer ? "1" : "0");
		dictionary.Add("USE_DISTORTION_RT", _useDistortionRT ? "1" : "0");
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("DEFERRED", Deferred ? "1" : "0");
		dictionary2.Add("USE_LBUFFER_COMPRESSION", UseLightBufferCompression ? "1" : "0");
		dictionary2.Add("COMPLETE_VERSION", _useCompleteForwardVersion ? "1" : "0");
		dictionary2.Add("FIRST_PERSON_VIEW", _firstPersonView ? "1" : "0");
		dictionary2.Add("USE_CLUSTERED_LIGHTING", (!Deferred && _useForwardClusteredLighting) ? "1" : "0");
		dictionary2.Add("USE_DIRECT_ACCESS", UseLightDirectAccess ? "1" : "0");
		dictionary2.Add("USE_CUSTOM_Z_DISTRIBUTION", UseCustomZDistribution ? "1" : "0");
		dictionary2.Add("USE_SCENE_DATA_OVERRIDE", _useSceneDataOverride ? "1" : "0");
		dictionary2.Add("USE_ENTITY_DATA_BUFFER", _useEntityDataBuffer ? "1" : "0");
		dictionary2.Add("USE_DISTORTION_RT", _useDistortionRT ? "1" : "0");
		ShadingMode[] array = (ShadingMode[])Enum.GetValues(typeof(ShadingMode));
		for (int i = 0; i < array.Length; i++)
		{
			ShadingMode shadingMode = array[i];
			string key = "SHADING_" + shadingMode.ToString().ToUpper();
			int num = (int)shadingMode;
			string value = num.ToString();
			dictionary.Add(key, value);
			dictionary2.Add(key, value);
		}
		uint vertexShader = CompileVertexShader(dictionary);
		uint fragmentShader = CompileFragmentShader(dictionary2);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		Texture0.SetValue(0);
		Texture1.SetValue(1);
		Texture2.SetValue(2);
		GradientAtlasTexture.SetValue(3);
		NoiseTexture.SetValue(4);
		EntityDataBuffer.SetValue(5);
		ModelVFXDataBuffer.SetValue(6);
		SceneDataBlock.SetupBindingPoint(this, 0u);
		PointLightBlock.SetupBindingPoint(this, 2u);
		NodeBlock.SetupBindingPoint(this, 5u);
	}
}
