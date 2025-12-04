namespace HytaleClient.Graphics.Programs;

internal class CubemapProgram : GPUProgram
{
	public Uniform MVPMatrix;

	private Uniform Texture;

	public readonly Attrib AttribPosition;

	public CubemapProgram()
		: base("CubemapVS.glsl", "CubemapFS.glsl")
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
		Texture.SetValue(0);
	}
}
