namespace HytaleClient.Graphics.Programs;

internal class DepthOfFieldAdvancedProgram : GPUProgram
{
	public Uniform PixelSize;

	public Uniform FarBlurMax;

	public Uniform NearBlurMax;

	private Uniform SceneColorLowResTexture;

	private Uniform ColorMulFarCoCLowResTextureLinear;

	private Uniform CoCLowResTextureLinear;

	private Uniform ColorMulFarCoCLowResTexturePoint;

	private Uniform CoCLowResTexturePoint;

	private Uniform NearCoCBlurredLowResTexture;

	public DepthOfFieldAdvancedProgram()
		: base("ScreenVS.glsl", "DepthOfFieldAdvancedFS.glsl")
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
		SceneColorLowResTexture.SetValue(0);
		ColorMulFarCoCLowResTextureLinear.SetValue(1);
		CoCLowResTextureLinear.SetValue(2);
		ColorMulFarCoCLowResTexturePoint.SetValue(3);
		CoCLowResTexturePoint.SetValue(4);
		NearCoCBlurredLowResTexture.SetValue(5);
	}
}
