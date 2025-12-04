using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class DeferredShadowProgram : GPUProgram
{
	public UniformBufferObject SceneDataBlock;

	public Uniform FarCorners;

	private Uniform ShadowMap;

	private Uniform DepthTexture;

	private Uniform GBuffer0Texture;

	private Uniform GBuffer1Texture;

	public bool UseLinearZ;

	public bool UseNoise = true;

	public bool UseManualMode = false;

	public bool UseFading = false;

	public bool UseSingleSample = true;

	public bool UseCameraBias = false;

	public bool UseNormalBias = true;

	public bool UseCleanBackfaces = false;

	public bool HasInputNormalsInWorldSpace = true;

	public uint CascadeCount = 1u;

	public DeferredShadowProgram()
		: base("ScreenVS.glsl", "DeferredShadowFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_FAR_CORNERS", "1");
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("USE_LINEAR_Z", UseLinearZ ? "1" : "0");
		dictionary2.Add("USE_NOISE", UseNoise ? "1" : "0");
		dictionary2.Add("USE_CAMERA_BIAS", UseCameraBias ? "1" : "0");
		dictionary2.Add("USE_NORMAL_BIAS", UseNormalBias ? "1" : "0");
		dictionary2.Add("USE_FADING", UseFading ? "1" : "0");
		dictionary2.Add("USE_MANUAL_MODE", UseManualMode ? "1" : "0");
		dictionary2.Add("USE_SINGLE_SAMPLE", UseSingleSample ? "1" : "0");
		dictionary2.Add("USE_CLEAN_BACKFACES", UseCleanBackfaces ? "1" : "0");
		dictionary2.Add("CASCADE_COUNT", CascadeCount.ToString());
		dictionary2.Add("INPUT_NORMALS_IN_WS", HasInputNormalsInWorldSpace ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary2);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		GBuffer1Texture.SetValue(3);
		GBuffer0Texture.SetValue(2);
		ShadowMap.SetValue(1);
		DepthTexture.SetValue(0);
		SceneDataBlock.SetupBindingPoint(this, 0u);
	}
}
