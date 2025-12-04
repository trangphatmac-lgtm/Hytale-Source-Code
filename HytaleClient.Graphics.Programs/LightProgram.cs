using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class LightProgram : GPUProgram
{
	public Uniform ModelMatrix;

	public Uniform ViewMatrix;

	public Uniform ProjectionMatrix;

	private Uniform DepthTexture;

	public Uniform FarClip;

	public Uniform FarCorners;

	public Uniform InvScreenSize;

	public Uniform Color;

	public Uniform PositionSize;

	public Uniform GlobalLightPositionSizes;

	public Uniform GlobalLightColors;

	public Uniform LightGroup;

	public Uniform UseLightGroup;

	public Uniform TransferMethod;

	public Uniform Debug;

	public readonly Attrib AttribPosition;

	public bool UseLinearZ;

	public bool UseLightBufferCompression = false;

	public int MaxDeferredLights;

	public LightProgram(int maxDeferredLights)
		: base("LightVS.glsl", "LightFS.glsl")
	{
		MaxDeferredLights = maxDeferredLights;
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_LINEAR_Z", UseLinearZ ? "1" : "0");
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("USE_LINEAR_Z", UseLinearZ ? "1" : "0");
		dictionary2.Add("MAX_DEFERRED_LIGHTS", MaxDeferredLights.ToString());
		dictionary2.Add("USE_LBUFFER_COMPRESSION", UseLightBufferCompression ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary2);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		DepthTexture.SetValue(0);
	}
}
