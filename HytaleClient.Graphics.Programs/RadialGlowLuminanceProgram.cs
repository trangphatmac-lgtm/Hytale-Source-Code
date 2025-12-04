using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class RadialGlowLuminanceProgram : GPUProgram
{
	public Uniform MVPMatrix;

	public Uniform SunMVPMatrix;

	public Uniform ScaleFactor;

	private Uniform GlowMaskTexture;

	private int _nbSamples;

	public RadialGlowLuminanceProgram(int nbSamples)
		: base("RadialGlowLuminanceVS.glsl", "RadialGlowLuminanceFS.glsl")
	{
		_nbSamples = nbSamples;
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("NB_SAMPLES", _nbSamples.ToString());
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("NB_SAMPLES", _nbSamples.ToString());
		uint fragmentShader = CompileFragmentShader(dictionary2);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		GlowMaskTexture.SetValue(0);
	}
}
