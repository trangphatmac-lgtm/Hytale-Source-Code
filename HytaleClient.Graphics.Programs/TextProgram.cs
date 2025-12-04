namespace HytaleClient.Graphics.Programs;

internal class TextProgram : GPUProgram
{
	public Uniform MVPMatrix;

	public Uniform Position;

	public Uniform FogColor;

	public Uniform FogParams;

	public Uniform FillThreshold;

	public Uniform FillBlurThreshold;

	public Uniform OutlineThreshold;

	public Uniform OutlineBlurThreshold;

	public Uniform OutlineOffset;

	public Uniform Opacity;

	public readonly Attrib AttribPosition;

	public readonly Attrib AttribTexCoords;

	public readonly Attrib AttribFillColor;

	public readonly Attrib AttribOutlineColor;

	public TextProgram()
		: base("TextVS.glsl", "TextFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		uint fragmentShader = CompileFragmentShader();
		return MakeProgram(vertexShader, fragmentShader);
	}
}
