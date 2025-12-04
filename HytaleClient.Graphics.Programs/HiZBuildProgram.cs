namespace HytaleClient.Graphics.Programs;

internal class HiZBuildProgram : GPUProgram
{
	private Uniform HiZBuffer;

	public HiZBuildProgram()
		: base("ScreenVS.glsl", "HiZBuildFS.glsl")
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
		HiZBuffer.SetValue(0);
	}
}
