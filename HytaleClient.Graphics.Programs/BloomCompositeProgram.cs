using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class BloomCompositeProgram : GPUProgram
{
	private Uniform BloomTexture;

	private Uniform SunshaftTexture;

	public Uniform BloomIntensity;

	public Uniform SunshaftIntensity;

	public bool SunFbPow;

	public bool UseSunshaft;

	public int BloomVersion;

	public BloomCompositeProgram()
		: base("ScreenVS.glsl", "BloomCompositeFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("SUN_FB_POW", SunFbPow ? "1" : "0");
		dictionary.Add("USE_SUNSHAFT", UseSunshaft ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		if (SunFbPow || UseSunshaft)
		{
			BloomTexture.SetValue(0);
		}
		if (UseSunshaft)
		{
			SunshaftTexture.SetValue(3);
		}
	}
}
