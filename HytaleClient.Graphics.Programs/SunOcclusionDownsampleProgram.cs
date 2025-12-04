namespace HytaleClient.Graphics.Programs;

internal class SunOcclusionDownsampleProgram : GPUProgram
{
	public Uniform CameraPosition;

	public Uniform CameraDirection;

	private Uniform ColorTexture;

	private Uniform DepthTexture;

	public SunOcclusionDownsampleProgram()
		: base("ScreenVS.glsl", "SunOcclusionDownsampleFS.glsl")
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
		DepthTexture.SetValue(1);
		ColorTexture.SetValue(0);
	}
}
