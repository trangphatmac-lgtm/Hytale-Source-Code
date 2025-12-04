namespace HytaleClient.Graphics.Programs;

internal class DebugDrawMapProgram : GPUProgram
{
	public Uniform Viewport;

	public Uniform TextureSize;

	public Uniform MipLevel;

	public Uniform Opacity;

	public Uniform Layer;

	public Uniform Multiplier;

	public Uniform DebugMaxOverdraw;

	public Uniform DebugZ;

	public Uniform LinearZ;

	public Uniform DebugTexture2DArray;

	public Uniform CubemapFace;

	public Uniform NormalQuantization;

	public Uniform ChromaSubsampling;

	public Uniform ColorChannels;

	private Uniform Texture2D;

	private Uniform Texture2DArray;

	private Uniform TextureCubemap;

	public DebugDrawMapProgram()
		: base("ScreenVS.glsl", "DebugDrawMapFS.glsl")
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
		Texture2D.SetValue(0);
		Texture2DArray.SetValue(1);
		TextureCubemap.SetValue(2);
	}
}
