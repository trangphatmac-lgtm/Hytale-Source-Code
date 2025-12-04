namespace HytaleClient.Graphics.Programs;

internal class DoFFillProgram : GPUProgram
{
	public Uniform PixelSize;

	private Uniform CoCLowResTexture;

	private Uniform NearCoCBlurredLowResTexture;

	private Uniform NearFieldLowResTexture;

	private Uniform FarFieldLowResTexture;

	public DoFFillProgram()
		: base("ScreenVS.glsl", "DoFFillFS.glsl")
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
		CoCLowResTexture.SetValue(0);
		NearCoCBlurredLowResTexture.SetValue(1);
		NearFieldLowResTexture.SetValue(2);
		FarFieldLowResTexture.SetValue(3);
	}
}
