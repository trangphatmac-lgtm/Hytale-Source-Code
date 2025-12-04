namespace HytaleClient.Graphics.Programs;

internal class DoFBlurProgram : GPUProgram
{
	public Uniform PixelSize;

	public Uniform NearBlurScale;

	public Uniform FarBlurScale;

	public Uniform HorizontalPass;

	private Uniform SceneColorTexture;

	private Uniform SceneColorTexture2;

	public DoFBlurProgram()
		: base("ScreenVS.glsl", "DoFBlurFS.glsl")
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
		SceneColorTexture.SetValue(0);
		SceneColorTexture2.SetValue(1);
	}
}
