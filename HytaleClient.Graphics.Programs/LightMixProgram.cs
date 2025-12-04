using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class LightMixProgram : GPUProgram
{
	public bool UseLightBufferCompression = false;

	public LightMixProgram(string variationName = null)
		: base("ScreenVS.glsl", "LightMixFS.glsl", variationName)
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_LBUFFER_COMPRESSION", UseLightBufferCompression ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}
}
