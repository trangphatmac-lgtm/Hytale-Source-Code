#define DEBUG
using System.Collections.Generic;
using System.Diagnostics;

namespace HytaleClient.Graphics.Programs;

internal class ParticleProgram : GPUProgram
{
	public struct TextureUnitLayout
	{
		public byte Atlas;

		public byte LinearFilteredAtlas;

		public byte UVMotion;

		public byte FXDataBuffer;

		public byte LightIndicesOrDataBuffer;

		public byte LightGrid;

		public byte ShadowMap;

		public byte FogNoise;

		public byte SceneDepth;

		public byte OITMoments;

		public byte OITTotalOpticalDepth;
	}

	public UniformBufferObject SceneDataBlock;

	public UniformBufferObject PointLightBlock;

	public Uniform DebugOverdraw;

	public Uniform InvTextureAtlasSize;

	public Uniform CurrentInvViewportSize;

	public Uniform OITParams;

	private Uniform MomentsTexture;

	private Uniform TotalOpticalDepthTexture;

	private Uniform LightGridTexture;

	private Uniform LightIndicesOrDataBufferTexture;

	private Uniform ShadowMap;

	private Uniform FogNoiseTexture;

	private Uniform SmoothTexture;

	private Uniform Texture;

	private Uniform DepthTexture;

	private Uniform UVMotionTexture;

	private Uniform SpawnerDataBuffer;

	public readonly Attrib AttribData1;

	public readonly Attrib AttribData2;

	public readonly Attrib AttribData3;

	public readonly Attrib AttribData4;

	public bool UseDebugOverdraw;

	public bool UseDebugTexture;

	public bool UseDebugUVMotion;

	public bool UseForwardClusteredLighting;

	public bool UseLightDirectAccess;

	public bool UseCustomZDistribution;

	public bool UseSunShadows;

	public bool UseLinearZ;

	public uint SunShadowCascadeCount = 1u;

	public bool UseSmoothNearMoodColor;

	public bool UseMoodFog;

	public bool UseFog;

	public bool UseOIT;

	public bool UseDistortionRT;

	public bool UseErosion;

	private TextureUnitLayout _textureUnitLayout;

	public ParticleProgram(bool useForwardClusteredLighting = true, bool useLightDirectAccess = true, bool useCustomZDistribution = true, bool useSunShadows = true, bool useDistortionRT = false, bool useErosion = false, string variationName = null)
		: base("ParticleVS.glsl", "ParticleFS.glsl", variationName)
	{
		UseForwardClusteredLighting = useForwardClusteredLighting;
		UseLightDirectAccess = useLightDirectAccess;
		UseCustomZDistribution = useCustomZDistribution;
		UseSunShadows = useSunShadows;
		UseDistortionRT = useDistortionRT;
		UseErosion = useErosion;
	}

	public void SetupTextureUnits(ref TextureUnitLayout textureUnitLayout, bool initUniforms = false)
	{
		Debug.Assert(GPUProgram.IsResourceBindingLayoutValid(textureUnitLayout), "Invalid TextureUnitLayout.");
		_textureUnitLayout = textureUnitLayout;
		if (initUniforms)
		{
			InitUniforms();
		}
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_SMOOTH_NEAR_MOOD_FOG_COLOR", UseSmoothNearMoodColor ? "1" : "0");
		dictionary.Add("USE_MOOD_FOG", UseMoodFog ? "1" : "0");
		dictionary.Add("USE_FOG", UseFog ? "1" : "0");
		dictionary.Add("USE_CLUSTERED_LIGHTING", UseForwardClusteredLighting ? "1" : "0");
		dictionary.Add("USE_DIRECT_ACCESS", UseLightDirectAccess ? "1" : "0");
		dictionary.Add("USE_CUSTOM_Z_DISTRIBUTION", UseCustomZDistribution ? "1" : "0");
		dictionary.Add("USE_SUN_SHADOWS", UseSunShadows ? "1" : "0");
		dictionary.Add("USE_LINEAR_Z", UseLinearZ ? "1" : "0");
		dictionary.Add("CASCADE_COUNT", SunShadowCascadeCount.ToString());
		dictionary.Add("USE_NOISE", "0");
		dictionary.Add("USE_SINGLE_SAMPLE", "1");
		dictionary.Add("USE_CAMERA_BIAS", "0");
		dictionary.Add("USE_NORMAL_BIAS", "0");
		dictionary.Add("USE_DISTORTION_RT", UseDistortionRT ? "1" : "0");
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("DEBUG_TEXTURE", UseDebugTexture ? "1" : "0");
		dictionary2.Add("DEBUG_OVERDRAW", UseDebugOverdraw ? "1" : "0");
		dictionary2.Add("DEBUG_UVMOTION", UseDebugUVMotion ? "1" : "0");
		dictionary2.Add("USE_MOOD_FOG", UseMoodFog ? "1" : "0");
		dictionary2.Add("USE_FOG", UseFog ? "1" : "0");
		dictionary2.Add("USE_DISTORTION_RT", UseDistortionRT ? "1" : "0");
		dictionary2.Add("USE_OIT", UseOIT ? "1" : "0");
		dictionary2.Add("USE_EROSION", UseErosion ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary2);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		MomentsTexture.SetValue(_textureUnitLayout.OITMoments);
		TotalOpticalDepthTexture.SetValue(_textureUnitLayout.OITTotalOpticalDepth);
		LightIndicesOrDataBufferTexture.SetValue(_textureUnitLayout.LightIndicesOrDataBuffer);
		LightGridTexture.SetValue(_textureUnitLayout.LightGrid);
		if (UseSunShadows)
		{
			ShadowMap.SetValue(_textureUnitLayout.ShadowMap);
		}
		if (UseMoodFog)
		{
			FogNoiseTexture.SetValue(_textureUnitLayout.FogNoise);
		}
		SpawnerDataBuffer.SetValue(_textureUnitLayout.FXDataBuffer);
		UVMotionTexture.SetValue(_textureUnitLayout.UVMotion);
		DepthTexture.SetValue(_textureUnitLayout.SceneDepth);
		SmoothTexture.SetValue(_textureUnitLayout.LinearFilteredAtlas);
		Texture.SetValue(_textureUnitLayout.Atlas);
		SceneDataBlock.SetupBindingPoint(this, 0u);
		PointLightBlock.SetupBindingPoint(this, 2u);
	}
}
