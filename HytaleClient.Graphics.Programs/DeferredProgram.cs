using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class DeferredProgram : GPUProgram
{
	public enum DebugPixelInfo
	{
		None,
		UseCleanShadowBackfaces,
		HasBloom,
		HasSSAO,
		FinalSSAO,
		FinalAmbient,
		FinalLight
	}

	public UniformBufferObject SceneDataBlock;

	public Uniform FarCorners;

	public Uniform DebugShadowMatrix;

	private Uniform ColorTexture;

	private Uniform LightTexture;

	private Uniform DepthTexture;

	private Uniform SSAOTexture;

	private Uniform TopDownProjectionTexture;

	private Uniform FogNoiseTexture;

	private Uniform ShadowTexture;

	public bool ReverseZ;

	public bool UseFog;

	public bool UseLight;

	public bool UseLinearZ;

	public bool UseDownsampledZ;

	public bool UseLowResLighting;

	public bool UseSSAO;

	public bool UseCloudsShadows = true;

	public bool UseUnderwaterCaustics = true;

	public bool UseSkyAmbient = true;

	public bool UseSmartUpsampling = false;

	public bool UseLightBufferCompression = false;

	public bool UseDithering;

	public bool UseSmoothNearMoodColor;

	public bool UseMoodFog;

	public bool UseDeferredShadow = true;

	public bool UseDeferredShadowBlurred = true;

	public bool UseDeferredShadowIndoorFading = false;

	public bool HasInputNormalsInWorldSpace = true;

	public bool DebugShadowCascades = false;

	public uint CascadeCount = 1u;

	public DebugPixelInfo DebugPixelInfoView = DebugPixelInfo.None;

	public DeferredProgram(bool reverseZ, bool useDownsampledZ, bool useDeferredFog, bool useDeferredLight, bool useLowResLighting, bool useSSAO)
		: base("ScreenVS.glsl", "DeferredFS.glsl")
	{
		ReverseZ = reverseZ;
		UseDownsampledZ = useDownsampledZ;
		UseFog = useDeferredFog;
		UseLight = useDeferredLight;
		UseLowResLighting = useLowResLighting;
		UseSSAO = useSSAO;
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_FAR_CORNERS", "1");
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("REVERSE_Z", ReverseZ ? "1" : "0");
		dictionary2.Add("USE_FOG", UseFog ? "1" : "0");
		dictionary2.Add("USE_LIGHT", UseLight ? "1" : "0");
		dictionary2.Add("USE_LINEAR_Z", UseLinearZ ? "1" : "0");
		dictionary2.Add("USE_LOWRES_Z", UseDownsampledZ ? "1" : "0");
		dictionary2.Add("USE_LOWRES_LIGHT", UseLowResLighting ? "1" : "0");
		dictionary2.Add("USE_SSAO", UseSSAO ? "1" : "0");
		dictionary2.Add("USE_EDGE_AWARE_UPSAMPLING", UseSmartUpsampling ? "1" : "0");
		dictionary2.Add("USE_CLOUDS_SHADOWS", UseCloudsShadows ? "1" : "0");
		dictionary2.Add("USE_UNDERWATER_CAUSTICS", UseUnderwaterCaustics ? "1" : "0");
		dictionary2.Add("USE_SKY_AMBIENT", UseSkyAmbient ? "1" : "0");
		dictionary2.Add("USE_LBUFFER_COMPRESSION", UseLightBufferCompression ? "1" : "0");
		dictionary2.Add("USE_FOG_DITHERING", UseDithering ? "1" : "0");
		dictionary2.Add("USE_SMOOTH_NEAR_MOOD_FOG_COLOR", UseSmoothNearMoodColor ? "1" : "0");
		dictionary2.Add("USE_MOOD_FOG", UseMoodFog ? "1" : "0");
		dictionary2.Add("USE_DEFERRED_SHADOW", UseDeferredShadow ? "1" : "0");
		dictionary2.Add("USE_DEFERRED_SHADOW_BLURRED", UseDeferredShadowBlurred ? "1" : "0");
		dictionary2.Add("USE_DEFERRED_SHADOW_INDOOR_FADING", UseDeferredShadowIndoorFading ? "1" : "0");
		dictionary2.Add("INPUT_NORMALS_IN_WS", HasInputNormalsInWorldSpace ? "1" : "0");
		dictionary2.Add("DEBUG_SHADOW_CASCADES", DebugShadowCascades ? "1" : "0");
		dictionary2.Add("CASCADE_COUNT", CascadeCount.ToString());
		dictionary2.Add("DEBUG_PIXELS", (DebugPixelInfoView != 0) ? "1" : "0");
		switch (DebugPixelInfoView)
		{
		case DebugPixelInfo.UseCleanShadowBackfaces:
			dictionary2.Add("DEBUG_PIXELS_USE_CLEAN_SHADOW_BACKFACES", "1");
			break;
		case DebugPixelInfo.HasBloom:
			dictionary2.Add("DEBUG_PIXELS_HAS_BLOOM", "1");
			break;
		case DebugPixelInfo.HasSSAO:
			dictionary2.Add("DEBUG_PIXELS_HAS_SSAO", "1");
			break;
		case DebugPixelInfo.FinalSSAO:
			dictionary2.Add("DEBUG_PIXELS_FINAL_SSAO", "1");
			break;
		case DebugPixelInfo.FinalAmbient:
			dictionary2.Add("DEBUG_PIXELS_FINAL_AMBIENT", "1");
			break;
		case DebugPixelInfo.FinalLight:
			dictionary2.Add("DEBUG_PIXELS_FINAL_LIGHT", "1");
			break;
		}
		uint fragmentShader = CompileFragmentShader(dictionary2);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		if (UseFog)
		{
			FogNoiseTexture.SetValue(7);
		}
		if (UseDeferredShadow)
		{
			ShadowTexture.SetValue(6);
		}
		if (UseUnderwaterCaustics || UseCloudsShadows)
		{
			TopDownProjectionTexture.SetValue(4);
		}
		if (UseSSAO)
		{
			SSAOTexture.SetValue(3);
		}
		DepthTexture.SetValue(2);
		LightTexture.SetValue(1);
		ColorTexture.SetValue(0);
		SceneDataBlock.SetupBindingPoint(this, 0u);
	}
}
