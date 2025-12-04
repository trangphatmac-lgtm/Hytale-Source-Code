using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class BloomSelectProgram : GPUProgram
{
	public Uniform Power;

	public Uniform SunMoonIntensity;

	public Uniform PowerOptions;

	public Uniform UseSunOrMoon;

	public Uniform Time;

	private Uniform SceneColorTexture;

	private Uniform SunMoonTexture;

	public bool SunOrMoon;

	public bool Fullbright;

	public bool Pow;

	public bool UseDithering;

	public BloomSelectProgram()
		: base("ScreenVS.glsl", "BloomSelectFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("SUN_OR_MOON", SunOrMoon ? "1" : "0");
		dictionary.Add("FULLBRIGHT", Fullbright ? "1" : "0");
		dictionary.Add("POW", Pow ? "1" : "0");
		dictionary.Add("USE_DITHERING", UseDithering ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		if (Pow || Fullbright)
		{
			SceneColorTexture.SetValue(0);
		}
		if (SunOrMoon)
		{
			SunMoonTexture.SetValue(1);
		}
	}
}
