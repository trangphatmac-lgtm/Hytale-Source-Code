using System;
using System.Collections.Generic;
using HytaleClient.Data.Map;

namespace HytaleClient.Graphics.Programs;

internal abstract class MapChunkBaseProgram : GPUProgram
{
	public UniformBufferObject SceneDataBlock;

	public UniformBufferObject PointLightBlock;

	public Uniform ModelMatrix;

	protected Uniform LightGridTexture;

	protected Uniform LightIndicesOrDataBufferTexture;

	public readonly Attrib AttribPositionAndDoubleSidedAndBlockId;

	public readonly Attrib AttribTexCoords;

	public readonly Attrib AttribDataPacked;

	public float LODDistance = 160f;

	public bool UseLOD;

	public bool UseFoliageFading = true;

	public bool UseDebugBoundaries;

	public bool Deferred;

	public bool UseLightBufferCompression;

	public bool UseForwardClusteredLighting = true;

	public bool UseLightDirectAccess = true;

	public bool UseCustomZDistribution = true;

	public bool WriteRenderConfigBitsInAlpha = true;

	public bool UseForwardSunShadows;

	public bool UseLinearZ;

	public uint SunShadowCascadeCount = 1u;

	public bool UseCloudsShadows;

	public bool UseUnderwaterCaustics = true;

	public bool UseSkyAmbient = true;

	public bool UseDithering;

	public bool UseSmoothNearMoodColor;

	public bool UseMoodFog;

	public bool UseFog;

	public bool UseOIT;

	private readonly bool _alphaTest;

	private readonly bool _alphaBlend;

	private readonly bool _near;

	public MapChunkBaseProgram(bool alphaTest, bool alphaBlend, bool near, bool useDeferred, bool useLOD, string variationName = null)
		: base("MapChunkVS.glsl", "MapChunkFS.glsl", variationName)
	{
		_alphaTest = alphaTest;
		_alphaBlend = alphaBlend;
		_near = near;
		Deferred = useDeferred;
		UseLOD = useLOD;
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("DEFERRED", Deferred ? "1" : "0");
		dictionary.Add("ALPHA_TEST", _alphaTest ? "1" : "0");
		dictionary.Add("ALPHA_BLEND", _alphaBlend ? "1" : "0");
		dictionary.Add("NEAR", _near ? "1" : "0");
		dictionary.Add("ANIMATED", "0");
		dictionary.Add("USE_FOLIAGE_FADING", UseFoliageFading ? "1" : "0");
		dictionary.Add("USE_LOD", UseLOD ? "1" : "0");
		dictionary.Add("LOD_DISTANCE", LODDistance.ToString());
		dictionary.Add("USE_FOG_DITHERING", UseDithering ? "1" : "0");
		dictionary.Add("USE_SMOOTH_NEAR_MOOD_FOG_COLOR", UseSmoothNearMoodColor ? "1" : "0");
		dictionary.Add("USE_MOOD_FOG", UseMoodFog ? "1" : "0");
		dictionary.Add("USE_FOG", UseFog ? "1" : "0");
		dictionary.Add("USE_OIT", (!Deferred && UseOIT) ? "1" : "0");
		dictionary.Add("USE_CLOUDS_SHADOWS", UseCloudsShadows ? "1" : "0");
		dictionary.Add("USE_UNDERWATER_CAUSTICS", UseUnderwaterCaustics ? "1" : "0");
		dictionary.Add("USE_SKY_AMBIENT", UseSkyAmbient ? "1" : "0");
		dictionary.Add("DEBUG_BOUNDARIES", UseDebugBoundaries ? "1" : "0");
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("DEFERRED", Deferred ? "1" : "0");
		dictionary2.Add("USE_LBUFFER_COMPRESSION", UseLightBufferCompression ? "1" : "0");
		dictionary2.Add("ALPHA_TEST", _alphaTest ? "1" : "0");
		dictionary2.Add("ALPHA_BLEND", _alphaBlend ? "1" : "0");
		dictionary2.Add("NEAR", _near ? "1" : "0");
		dictionary2.Add("ANIMATED", "0");
		dictionary2.Add("USE_CLOUDS_SHADOWS", UseCloudsShadows ? "1" : "0");
		dictionary2.Add("USE_UNDERWATER_CAUSTICS", UseUnderwaterCaustics ? "1" : "0");
		dictionary2.Add("USE_SKY_AMBIENT", UseSkyAmbient ? "1" : "0");
		dictionary2.Add("USE_CLUSTERED_LIGHTING", (!Deferred && UseForwardClusteredLighting) ? "1" : "0");
		dictionary2.Add("USE_DIRECT_ACCESS", (!Deferred && UseLightDirectAccess) ? "1" : "0");
		dictionary2.Add("USE_CUSTOM_Z_DISTRIBUTION", (!Deferred && UseCustomZDistribution) ? "1" : "0");
		dictionary2.Add("WRITE_RENDERCONFIG_IN_ALPHA", (!Deferred && WriteRenderConfigBitsInAlpha) ? "1" : "0");
		dictionary2.Add("USE_FORWARD_SUN_SHADOWS", (!Deferred && UseForwardSunShadows) ? "1" : "0");
		dictionary2.Add("USE_LINEAR_Z", UseLinearZ ? "1" : "0");
		dictionary2.Add("CASCADE_COUNT", SunShadowCascadeCount.ToString());
		dictionary2.Add("USE_NOISE", "1");
		dictionary2.Add("USE_SINGLE_SAMPLE", "1");
		dictionary2.Add("USE_CAMERA_BIAS", "0");
		dictionary2.Add("USE_NORMAL_BIAS", "1");
		dictionary2.Add("USE_FOG_DITHERING", (!Deferred && UseDithering) ? "1" : "0");
		dictionary2.Add("USE_SMOOTH_NEAR_MOOD_FOG_COLOR", (!Deferred && UseSmoothNearMoodColor) ? "1" : "0");
		dictionary2.Add("USE_MOOD_FOG", (!Deferred && UseMoodFog) ? "1" : "0");
		dictionary2.Add("USE_FOG", (!Deferred && UseFog) ? "1" : "0");
		dictionary2.Add("USE_OIT", (!Deferred && UseOIT) ? "1" : "0");
		ClientBlockType.ClientShaderEffect[] array = (ClientBlockType.ClientShaderEffect[])Enum.GetValues(typeof(ClientBlockType.ClientShaderEffect));
		for (int i = 0; i < array.Length; i++)
		{
			ClientBlockType.ClientShaderEffect clientShaderEffect = array[i];
			string key = "EFFECT_" + clientShaderEffect.ToString().ToUpper();
			int num = (int)clientShaderEffect;
			string value = num.ToString();
			dictionary.Add(key, value);
			dictionary2.Add(key, value);
		}
		ShadingMode[] array2 = (ShadingMode[])Enum.GetValues(typeof(ShadingMode));
		for (int j = 0; j < array2.Length; j++)
		{
			ShadingMode shadingMode = array2[j];
			string key2 = "SHADING_" + shadingMode.ToString().ToUpper();
			int num = (int)shadingMode;
			string value2 = num.ToString();
			dictionary.Add(key2, value2);
			dictionary2.Add(key2, value2);
		}
		uint vertexShader = CompileVertexShader(dictionary);
		uint fragmentShader = CompileFragmentShader(dictionary2);
		List<AttribBindingInfo> list = new List<AttribBindingInfo>(5);
		list.Add(new AttribBindingInfo(0u, "vertPositionAndDoubleSidedAndBlockId"));
		list.Add(new AttribBindingInfo(1u, "vertTexCoords"));
		list.Add(new AttribBindingInfo(2u, "vertDataPacked"));
		return MakeProgram(vertexShader, fragmentShader, list, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		SceneDataBlock.SetupBindingPoint(this, 0u);
		if (!Deferred && UseForwardClusteredLighting)
		{
			PointLightBlock.SetupBindingPoint(this, 2u);
		}
	}
}
