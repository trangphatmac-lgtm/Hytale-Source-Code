namespace HytaleClient.Graphics.Programs;

internal class HiZCullProgram : GPUProgram
{
	public Uniform ViewProjectionMatrix;

	public Uniform ViewportSize;

	public Uniform HiZBuffer;

	public readonly Attrib AttribBoxMin;

	public readonly Attrib AttribBoxMax;

	public HiZCullProgram()
		: base("HiZCullVS.glsl", null)
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		string[] transformFeedbackVaryings = new string[1] { "outVisible" };
		return MakeProgram(vertexShader, null, ignoreMissingUniforms: false, transformFeedbackVaryings);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		HiZBuffer.SetValue(0);
	}
}
