using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class CloudsProgram : GPUProgram
{
	public Uniform MVPMatrix;

	public Uniform Colors;

	public Uniform UVOffsets;

	public Uniform UVMotionParams;

	public Uniform CloudsTextureCount;

	private Uniform SunOcclusionHistory;

	public Uniform CameraPosition;

	public Uniform FogFrontColor;

	public Uniform FogBackColor;

	public Uniform FogMoodParams;

	public Uniform SunPosition;

	private Uniform Texture0;

	private Uniform Texture1;

	private Uniform Texture2;

	private Uniform Texture3;

	private Uniform FlowTexture;

	public readonly Attrib AttribPosition;

	public readonly Attrib AttribTexCoords;

	public bool UseMoodFog = true;

	public bool UseDithering = true;

	public CloudsProgram()
		: base("SkyAndCloudsVS.glsl", "CloudsFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("SKY_VERSION", "0");
		dictionary.Add("USE_MOOD_FOG", UseMoodFog ? "1" : "0");
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("USE_MOOD_FOG", UseMoodFog ? "1" : "0");
		dictionary2.Add("USE_FOG_DITHERING", UseDithering ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary2);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		Texture0.SetValue(0);
		Texture1.SetValue(1);
		Texture2.SetValue(2);
		Texture3.SetValue(3);
		SunOcclusionHistory.SetValue(4);
		FlowTexture.SetValue(5);
	}
}
