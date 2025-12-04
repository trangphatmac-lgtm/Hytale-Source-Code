using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class ZOnlyProgram : GPUProgram
{
	public Uniform ModelMatrix;

	public Uniform ViewProjectionMatrix;

	public Uniform Time;

	public Uniform TargetCascades;

	public Uniform ViewportInfos;

	private Uniform Texture;

	public readonly Attrib AttribPosition;

	private bool _alphaTest;

	public bool AlphaTest
	{
		get
		{
			return _alphaTest;
		}
		set
		{
			_alphaTest = value;
			_fragmentShaderResource.FileName = (value ? "ZOnlyFS.glsl" : null);
		}
	}

	public ZOnlyProgram(bool alphaTest, string variationName = null)
		: base("ZOnlyVS.glsl", "ZOnlyFS.glsl", variationName)
	{
		AlphaTest = alphaTest;
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("ALPHA_TEST", AlphaTest ? "1" : "0");
		uint vertexShader = CompileVertexShader(dictionary);
		List<AttribBindingInfo> list = new List<AttribBindingInfo>(5);
		list.Add(new AttribBindingInfo(0u, "vertPosition"));
		if (AlphaTest)
		{
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			dictionary2.Add("ALPHA_TEST", AlphaTest ? "1" : "0");
			uint fragmentShader = CompileFragmentShader(dictionary2);
			if (AlphaTest)
			{
				list.Add(new AttribBindingInfo(2u, "vertTexCoords"));
			}
			return MakeProgram(vertexShader, fragmentShader, list, ignoreMissingUniforms: true);
		}
		return MakeProgram(vertexShader, list, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		if (AlphaTest)
		{
			GPUProgram._gl.UseProgram(this);
			Texture.SetValue(0);
		}
	}
}
