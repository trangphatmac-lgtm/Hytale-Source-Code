using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class DoFCircleOfConfusionProgram : GPUProgram
{
	public Uniform ProjectionMatrix;

	public Uniform FarClip;

	public Uniform NearBlurry;

	public Uniform NearSharp;

	public Uniform FarSharp;

	public Uniform FarBlurry;

	private Uniform DepthTexture;

	public bool UseLinearZ;

	public DoFCircleOfConfusionProgram()
		: base("ScreenVS.glsl", "DoFCircleOfConfusionFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_LINEAR_Z", UseLinearZ ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		DepthTexture.SetValue(0);
	}
}
