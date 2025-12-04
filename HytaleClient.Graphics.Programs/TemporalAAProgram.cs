namespace HytaleClient.Graphics.Programs;

internal class TemporalAAProgram : GPUProgram
{
	public Uniform PixelSize;

	public Uniform NeighborHoodCheck;

	private Uniform ColorTexture;

	private Uniform PreviousColorTexture;

	public TemporalAAProgram()
		: base("ScreenVS.glsl", "TemporalAAFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		uint fragmentShader = CompileFragmentShader();
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		ColorTexture.SetValue(0);
		PreviousColorTexture.SetValue(1);
	}
}
