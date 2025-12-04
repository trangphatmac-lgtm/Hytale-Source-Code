using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class VolumetricSunshaftProgram : GPUProgram
{
	public UniformBufferObject SceneDataBlock;

	public Uniform FarCorners;

	public Uniform SunDirection;

	public Uniform SunColor;

	private Uniform ShadowMap;

	private Uniform DepthTexture;

	private Uniform GBuffer0Texture;

	public bool UseManualMode = false;

	public uint CascadeCount = 1u;

	public VolumetricSunshaftProgram()
		: base("ScreenVS.glsl", "VolumetricSunshaftFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_FAR_CORNERS", "1");
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("USE_MANUAL_MODE", UseManualMode ? "1" : "0");
		dictionary2.Add("CASCADE_COUNT", CascadeCount.ToString());
		uint fragmentShader = CompileFragmentShader(dictionary2);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		GBuffer0Texture.SetValue(2);
		ShadowMap.SetValue(1);
		DepthTexture.SetValue(0);
		SceneDataBlock.SetupBindingPoint(this, 0u);
	}
}
