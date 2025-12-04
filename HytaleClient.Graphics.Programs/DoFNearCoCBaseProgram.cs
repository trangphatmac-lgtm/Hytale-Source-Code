using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal abstract class DoFNearCoCBaseProgram : GPUProgram
{
	public Uniform MVPMatrix;

	public Uniform PixelSize;

	public Uniform HorizontalPass;

	protected Uniform CoCTexture;

	public bool UseFullscreenTriangle = true;

	public DoFNearCoCBaseProgram(string vertexShaderFileName, string fragmentShaderFileName)
		: base(vertexShaderFileName, fragmentShaderFileName)
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_MVP_MATRIX", (!UseFullscreenTriangle) ? "1" : "0");
		dictionary.Add("USE_VBO_ATTRIBUTES", (!UseFullscreenTriangle) ? "1" : "0");
		dictionary.Add("USE_FULLSCREEN_TRIANGLE", UseFullscreenTriangle ? "1" : "0");
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> defines = new Dictionary<string, string>();
		uint fragmentShader = CompileFragmentShader(defines);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		CoCTexture.SetValue(0);
	}
}
