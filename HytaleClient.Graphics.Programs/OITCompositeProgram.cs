using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class OITCompositeProgram : GPUProgram
{
	public Uniform OITMethod;

	public Uniform InputResolutionUsed;

	private Uniform AccumulationQuarterResTexture;

	private Uniform RevealAddQuarterResTexture;

	private Uniform AccumulationHalfResTexture;

	private Uniform RevealAddHalfResTexture;

	private Uniform AccumulationTexture;

	private Uniform RevealAddTexture;

	private Uniform BackgroundTexture;

	public OITCompositeProgram()
		: base("ScreenVS.glsl", "OITCompositeFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_FAR_CORNERS", "1");
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> defines = new Dictionary<string, string>();
		uint fragmentShader = CompileFragmentShader(defines);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		AccumulationQuarterResTexture.SetValue(6);
		RevealAddQuarterResTexture.SetValue(5);
		AccumulationHalfResTexture.SetValue(4);
		RevealAddHalfResTexture.SetValue(3);
		BackgroundTexture.SetValue(2);
		RevealAddTexture.SetValue(1);
		AccumulationTexture.SetValue(0);
	}
}
