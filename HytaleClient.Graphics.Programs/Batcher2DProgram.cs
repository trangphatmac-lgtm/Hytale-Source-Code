namespace HytaleClient.Graphics.Programs;

internal class Batcher2DProgram : GPUProgram
{
	public enum TextureUnit
	{
		Texture,
		MaskTexture,
		FontTexture
	}

	public readonly Uniform MVPMatrix;

	private readonly Uniform Texture;

	private readonly Uniform MaskTexture;

	private readonly Uniform FontTexture;

	public readonly Attrib AttribPosition;

	public readonly Attrib AttribTexCoords;

	public readonly Attrib AttribScissor;

	public readonly Attrib AttribMaskTextureArea;

	public readonly Attrib AttribMaskBounds;

	public readonly Attrib AttribFillColor;

	public readonly Attrib AttribOutlineColor;

	public readonly Attrib AttribSDFSettings;

	public readonly Attrib AttribFontId;

	public Batcher2DProgram()
		: base("Batcher2DVS.glsl", "Batcher2DFS.glsl")
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
		MaskTexture.SetValue(1);
		FontTexture.SetValue(2);
	}
}
