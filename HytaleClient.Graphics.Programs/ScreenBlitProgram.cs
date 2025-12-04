using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class ScreenBlitProgram : GPUProgram
{
	public Uniform MipLevel;

	private readonly bool _writeAlphaChannel;

	public ScreenBlitProgram(bool writeAlphaChannel = true, string variationName = null)
		: base("ScreenVS.glsl", "BasicFS.glsl", variationName)
	{
		_writeAlphaChannel = writeAlphaChannel;
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_DISCARD", "0");
		dictionary.Add("USE_COLOR_AND_OPACITY", "0");
		dictionary.Add("WRITE_ALPHA", _writeAlphaChannel ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}
}
