using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class SkyProgram : GPUProgram
{
	public Uniform MVPMatrix;

	public Uniform StarsOpacity;

	public Uniform TopGradientColor;

	public Uniform SunsetColor;

	private Uniform SunOcclusionHistory;

	public Uniform CameraPosition;

	public Uniform FogFrontColor;

	public Uniform FogBackColor;

	public Uniform FogMoodParams;

	public Uniform SunPosition;

	public Uniform SunScale;

	public Uniform SunGlowColor;

	public Uniform MoonOpacity;

	public Uniform MoonScale;

	public Uniform MoonGlowColor;

	public Uniform DrawSkySunMoonStars;

	public readonly Attrib AttribPosition;

	public readonly Attrib AttribTexCoords;

	public bool UseMoodFog = true;

	public bool UseDitheringOnFog = true;

	public bool UseDitheringOnSky = true;

	public SkyProgram()
		: base("SkyAndCloudsVS.glsl", "SkyFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("SKY_VERSION", "1");
		dictionary.Add("USE_MOOD_FOG", UseMoodFog ? "1" : "0");
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("USE_MOOD_FOG", UseMoodFog ? "1" : "0");
		dictionary2.Add("FOG_DITHERING", UseDitheringOnFog ? "1" : "0");
		dictionary2.Add("SKY_DITHERING", UseDitheringOnSky ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary2);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		SunOcclusionHistory.SetValue(4);
	}
}
