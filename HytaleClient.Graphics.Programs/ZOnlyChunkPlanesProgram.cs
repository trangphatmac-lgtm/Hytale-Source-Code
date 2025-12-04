using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class ZOnlyChunkPlanesProgram : GPUProgram
{
	public Uniform ViewProjectionMatrix;

	public readonly Attrib AttribPosition;

	public ZOnlyChunkPlanesProgram(string variationName = null)
		: base("ZOnlyChunkPlanesVS.glsl", "ZOnlyChunkFS.glsl", variationName)
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("ALPHA_TEST", "0");
		dictionary.Add("USE_DRAW_INSTANCED", "0");
		uint fragmentShader = CompileFragmentShader(dictionary);
		List<AttribBindingInfo> list = new List<AttribBindingInfo>(5);
		list.Add(new AttribBindingInfo(0u, "vertPosition"));
		return MakeProgram(vertexShader, fragmentShader, list, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
	}
}
