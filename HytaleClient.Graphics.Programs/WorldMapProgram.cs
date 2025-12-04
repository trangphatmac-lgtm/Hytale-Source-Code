namespace HytaleClient.Graphics.Programs;

internal class WorldMapProgram : GPUProgram
{
	public Uniform MVPMatrix;

	private Uniform Texture;

	private Uniform MaskTexture;

	public readonly Attrib AttribPosition;

	public readonly Attrib AttribTexCoords;

	public WorldMapProgram()
		: base("BasicVS.glsl", "WorldMapFS.glsl")
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
		Texture.SetValue(0);
		MaskTexture.SetValue(1);
	}
}
