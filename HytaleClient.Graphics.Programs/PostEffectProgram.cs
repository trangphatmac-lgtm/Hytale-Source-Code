using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class PostEffectProgram : GPUProgram
{
	public Uniform PixelSize;

	public Uniform DebugTileResolution;

	public Uniform Time;

	public Uniform DistortionAmplitude;

	public Uniform DistortionFrequency;

	public Uniform ColorBrightnessContrast;

	public Uniform ColorSaturation;

	public Uniform ColorFilter;

	public Uniform ProjectionMatrix;

	public Uniform FarClip;

	public Uniform ApplyBloom;

	public Uniform VolumetricSunshaftStrength;

	public Uniform NearBlurMax;

	public Uniform NearBlurry;

	public Uniform NearSharp;

	public Uniform FarSharp;

	public Uniform FarBlurry;

	public Uniform FarBlurMax;

	private Uniform ColorTexture;

	private Uniform DistortionTexture;

	private Uniform DepthTexture;

	private Uniform BlurTexture;

	private Uniform NearBlurTexture;

	private Uniform FarBlurTexture;

	private Uniform CoCTexture;

	private Uniform CoCLowResTexture;

	private Uniform NearCoCBlurredLowResTexture;

	private Uniform NearFieldLowResTexture;

	private Uniform FarFieldLowResTexture;

	private Uniform SceneColorLowResTexturePoint;

	private Uniform BloomTexture;

	private Uniform VolumetricSunshaftTexture;

	public bool UseFXAA = true;

	public bool UseSharpenEffect = true;

	public float SharpenStrength = 0.1f;

	private readonly bool UseAntiAliasingHighQuality;

	private readonly bool DiscardDark;

	public bool ReverseZ;

	public bool DebugTiles;

	public int DepthOfFieldVersion = 3;

	public bool UseDepthOfField;

	public bool UseBloom;

	public bool SunFbPow;

	public bool UseSunshaft;

	public bool UseDistortion = true;

	public bool UseVolumetricSunshaft;

	private bool UseLinearZ;

	public PostEffectProgram(bool reverseZ, bool useAntiAliasingHighQuality, bool discardDark, string variationName = null)
		: base("ScreenVS.glsl", "PostEffectFS.glsl", variationName)
	{
		ReverseZ = reverseZ;
		UseAntiAliasingHighQuality = useAntiAliasingHighQuality;
		DiscardDark = discardDark;
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("REVERSE_Z", ReverseZ ? "1" : "0");
		dictionary.Add("DEBUG_TILES", DebugTiles ? "1" : "0");
		dictionary.Add("USE_FXAA_HIGH_QUALITY", UseAntiAliasingHighQuality ? "1" : "0");
		dictionary.Add("USE_FXAA", UseFXAA ? "1" : "0");
		dictionary.Add("DISCARD_DARK", DiscardDark ? "1" : "0");
		dictionary.Add("USE_SHARPEN", UseSharpenEffect ? "1" : "0");
		dictionary.Add("SHARPEN_STRENGTH", SharpenStrength.ToString(GPUProgram.DecimalPointFormatting));
		dictionary.Add("DOF_VERSION", DepthOfFieldVersion.ToString());
		dictionary.Add("USE_DOF", UseDepthOfField ? "1" : "0");
		dictionary.Add("USE_BLOOM", UseBloom ? "1" : "0");
		dictionary.Add("SUN_FB_POW", SunFbPow ? "1" : "0");
		dictionary.Add("USE_SUNSHAFT", UseSunshaft ? "1" : "0");
		dictionary.Add("USE_LINEAR_Z", UseLinearZ ? "1" : "0");
		dictionary.Add("USE_DISTORTION", UseDistortion ? "1" : "0");
		dictionary.Add("USE_VOL_SUNSHAFT", UseVolumetricSunshaft ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		DistortionTexture.SetValue(8);
		ColorTexture.SetValue(0);
		if (UseDepthOfField)
		{
			if (DepthOfFieldVersion != 3)
			{
				DepthTexture.SetValue(1);
			}
			if (DepthOfFieldVersion == 1)
			{
				BlurTexture.SetValue(2);
			}
			else if (DepthOfFieldVersion == 2)
			{
				NearBlurTexture.SetValue(2);
				FarBlurTexture.SetValue(3);
			}
			else if (DepthOfFieldVersion == 3)
			{
				CoCTexture.SetValue(1);
				CoCLowResTexture.SetValue(2);
				NearCoCBlurredLowResTexture.SetValue(3);
				NearFieldLowResTexture.SetValue(4);
				FarFieldLowResTexture.SetValue(5);
				SceneColorLowResTexturePoint.SetValue(6);
			}
		}
		if (UseBloom)
		{
			BloomTexture.SetValue(7);
		}
		if (UseVolumetricSunshaft)
		{
			VolumetricSunshaftTexture.SetValue(9);
		}
	}
}
