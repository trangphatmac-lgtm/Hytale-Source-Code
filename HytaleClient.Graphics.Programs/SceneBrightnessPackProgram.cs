namespace HytaleClient.Graphics.Programs;

internal class SceneBrightnessPackProgram : GPUProgram
{
	public Uniform SunOcclusionHistory;

	public SceneBrightnessPackProgram()
		: base("SceneBrightnessPackVS.glsl", null)
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		string[] transformFeedbackVaryings = new string[1] { "outSceneBrightness" };
		return MakeProgram(vertexShader, null, ignoreMissingUniforms: false, transformFeedbackVaryings);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		SunOcclusionHistory.SetValue(0);
	}
}
