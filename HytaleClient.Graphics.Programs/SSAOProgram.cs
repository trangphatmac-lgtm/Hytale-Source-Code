using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class SSAOProgram : GPUProgram
{
	public Uniform PackedParameters;

	public Uniform ViewportSize;

	public Uniform ProjectionMatrix;

	public Uniform ViewMatrix;

	public Uniform ReprojectMatrix;

	public Uniform FarCorners;

	public Uniform SamplesData;

	public Uniform TemporalSampleOffset;

	private Uniform DepthTexture;

	private Uniform TapsSourceTexture;

	private Uniform GBufferTexture;

	private Uniform SSAOCacheTexture;

	private Uniform ShadowTexture;

	public int SamplesCount = 8;

	public bool UseTemporalFiltering = true;

	public bool HasInputNormalsInWorldSpace = true;

	public SSAOProgram()
		: base("ScreenVS.glsl", "SSAOFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_FAR_CORNERS", "1");
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("SAMPLES_COUNT", SamplesCount.ToString());
		dictionary2.Add("USE_TEMPORAL_FILTERING", UseTemporalFiltering ? "1" : "0");
		dictionary2.Add("INPUT_NORMALS_IN_WS", HasInputNormalsInWorldSpace ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary2);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		ShadowTexture.SetValue(4);
		SSAOCacheTexture.SetValue(3);
		GBufferTexture.SetValue(2);
		TapsSourceTexture.SetValue(1);
		DepthTexture.SetValue(0);
	}
}
