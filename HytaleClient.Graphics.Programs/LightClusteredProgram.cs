using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class LightClusteredProgram : GPUProgram
{
	public Uniform ProjectionMatrix;

	public Uniform FarCorners;

	public Uniform FarClip;

	public Uniform LightGridResolution;

	public Uniform ZSlicesParams;

	public Uniform UseLBufferCompression;

	public UniformBufferObject PointLightBlock;

	private Uniform DepthTexture;

	private Uniform LightGridTexture;

	private Uniform LightIndicesOrDataBufferTexture;

	public bool UseLinearZ;

	public bool UseLightDirectAccess = true;

	public bool UseCustomZDistribution = true;

	public bool Debug;

	public LightClusteredProgram()
		: base("ScreenVS.glsl", "LightClusteredFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_FAR_CORNERS", "1");
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("DEBUG", Debug ? "1" : "0");
		dictionary2.Add("USE_LINEAR_Z", UseLinearZ ? "1" : "0");
		dictionary2.Add("USE_DIRECT_ACCESS", UseLightDirectAccess ? "1" : "0");
		dictionary2.Add("USE_CUSTOM_Z_DISTRIBUTION", UseCustomZDistribution ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary2);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		LightIndicesOrDataBufferTexture.SetValue(2);
		LightGridTexture.SetValue(1);
		DepthTexture.SetValue(0);
		PointLightBlock.SetupBindingPoint(this, 2u);
	}
}
