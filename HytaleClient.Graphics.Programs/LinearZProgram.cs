namespace HytaleClient.Graphics.Programs;

internal class LinearZProgram : GPUProgram
{
	public Uniform ProjectionMatrix;

	public Uniform InvFarClip;

	private Uniform DepthTexture;

	public bool UseFullscreenTriangle = true;

	public LinearZProgram()
		: base("ScreenVS.glsl", "LinearZFS.glsl")
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
		DepthTexture.SetValue(0);
	}
}
