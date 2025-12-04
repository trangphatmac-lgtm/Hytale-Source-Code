namespace HytaleClient.Graphics.Programs;

internal class BasicFogProgram : GPUProgram
{
	public Uniform MVPMatrix;

	public Uniform ModelMatrix;

	public Uniform Color;

	public Uniform Opacity;

	public Uniform CameraPosition;

	public Uniform FogColor;

	public Uniform FogParams;

	public readonly Attrib AttribPosition;

	public readonly Attrib AttribTexCoords;

	public BasicFogProgram()
		: base("BasicFogVS.glsl", "BasicFogFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		uint fragmentShader = CompileFragmentShader();
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}
}
