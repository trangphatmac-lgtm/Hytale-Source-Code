namespace HytaleClient.Graphics.Programs;

internal class DoFDownsampleProgram : GPUProgram
{
	public Uniform PixelSize;

	private Uniform CoCTexture;

	private Uniform ColorTextureLinear;

	private Uniform ColorTexturePoint;

	public DoFDownsampleProgram()
		: base("ScreenVS.glsl", "DoFDownsampleFS.glsl")
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
		ColorTextureLinear.SetValue(0);
		ColorTexturePoint.SetValue(1);
		CoCTexture.SetValue(2);
	}
}
