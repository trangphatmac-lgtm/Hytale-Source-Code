#define DEBUG
using System.Diagnostics;

namespace HytaleClient.Graphics.Programs;

internal class MapChunkAlphaBlendedProgram : MapChunkBaseProgram
{
	public struct TextureUnitLayout
	{
		public byte Texture;

		public byte SceneDepth;

		public byte SceneDepthLowRes;

		public byte Normals;

		public byte Refraction;

		public byte SceneColor;

		public byte Caustics;

		public byte CloudShadow;

		public byte FogNoise;

		public byte ShadowMap;

		public byte LightIndicesOrDataBuffer;

		public byte LightGrid;

		public byte OITMoments;

		public byte OITTotalOpticalDepth;
	}

	public Uniform DebugOverdraw;

	public Uniform CurrentInvViewportSize;

	public Uniform OITParams;

	private Uniform MomentsTexture;

	private Uniform TotalOpticalDepthTexture;

	public Uniform InvTextureAtlasSize;

	public Uniform WaterTintColor;

	public Uniform WaterQuality;

	private Uniform NormalsTexture;

	private Uniform DepthTexture;

	private Uniform LowResDepthTexture;

	private Uniform SceneTexture;

	private Uniform RefractionTexture;

	private Uniform FogNoiseTexture;

	private Uniform ShadowMap;

	private Uniform CausticsTexture;

	private Uniform CloudShadowTexture;

	private TextureUnitLayout _textureUnitLayout;

	public ref TextureUnitLayout TextureUnits => ref _textureUnitLayout;

	public MapChunkAlphaBlendedProgram(bool useForwardSunShadows, string variationName = null)
		: base(alphaTest: false, alphaBlend: true, near: true, useDeferred: false, useLOD: false, variationName)
	{
		UseForwardSunShadows = useForwardSunShadows;
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

	protected override void InitUniforms()
	{
		base.InitUniforms();
		GPUProgram._gl.UseProgram(this);
		MomentsTexture.SetValue(_textureUnitLayout.OITMoments);
		TotalOpticalDepthTexture.SetValue(_textureUnitLayout.OITTotalOpticalDepth);
		CloudShadowTexture.SetValue(_textureUnitLayout.CloudShadow);
		CausticsTexture.SetValue(_textureUnitLayout.Caustics);
		LightIndicesOrDataBufferTexture.SetValue(_textureUnitLayout.LightIndicesOrDataBuffer);
		LightGridTexture.SetValue(_textureUnitLayout.LightGrid);
		ShadowMap.SetValue(_textureUnitLayout.ShadowMap);
		if (UseMoodFog)
		{
			FogNoiseTexture.SetValue(_textureUnitLayout.FogNoise);
		}
		LowResDepthTexture.SetValue(_textureUnitLayout.SceneDepthLowRes);
		NormalsTexture.SetValue(_textureUnitLayout.Normals);
		RefractionTexture.SetValue(_textureUnitLayout.Refraction);
		SceneTexture.SetValue(_textureUnitLayout.SceneColor);
		DepthTexture.SetValue(_textureUnitLayout.SceneDepth);
	}
}
