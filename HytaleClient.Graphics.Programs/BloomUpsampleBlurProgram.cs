using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class BloomUpsampleBlurProgram : GPUProgram
{
	public Uniform PixelSize;

	public Uniform Scale;

	public Uniform Intensity;

	private Uniform ColorTexture;

	private Uniform ColorLowResTexture;

	public int UpsampleMethod;

	public BloomUpsampleBlurProgram()
		: base("ScreenVS.glsl", "BloomUpsampleBlurFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("METHOD", UpsampleMethod.ToString());
		uint fragmentShader = CompileFragmentShader(dictionary);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		ColorTexture.SetValue(0);
		ColorLowResTexture.SetValue(1);
	}
}
