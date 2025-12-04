using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class BloomDownsampleBlurProgram : GPUProgram
{
	public Uniform PixelSize;

	private Uniform ColorTexture;

	public int DownsampleMethod;

	public BloomDownsampleBlurProgram()
		: base("ScreenVS.glsl", "BloomDownsampleBlurFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("METHOD", DownsampleMethod.ToString());
		uint fragmentShader = CompileFragmentShader(dictionary);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		ColorTexture.SetValue(0);
	}
}
