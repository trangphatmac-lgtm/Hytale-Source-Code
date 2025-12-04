using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class BasicProgram : GPUProgram
{
	public Uniform MVPMatrix;

	public Uniform Color;

	public Uniform Opacity;

	public readonly Attrib AttribPosition;

	public readonly Attrib AttribTexCoords;

	private readonly bool _writeAlphaChannel;

	public BasicProgram(bool writeAlphaChannel = true, string variationName = null)
		: base("BasicVS.glsl", "BasicFS.glsl", variationName)
	{
		_writeAlphaChannel = writeAlphaChannel;
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_DISCARD", "1");
		dictionary.Add("USE_COLOR_AND_OPACITY", "1");
		dictionary.Add("WRITE_ALPHA", _writeAlphaChannel ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}
}
