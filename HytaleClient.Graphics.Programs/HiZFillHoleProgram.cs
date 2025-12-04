namespace HytaleClient.Graphics.Programs;

internal class HiZFillHoleProgram : GPUProgram
{
	private Uniform DepthTexture;

	public HiZFillHoleProgram()
		: base("ScreenVS.glsl", "HiZFillHoleFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		uint fragmentShader = CompileFragmentShader();
		return MakeProgram(vertexShader, fragmentShader);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		DepthTexture.SetValue(0);
	}
}
