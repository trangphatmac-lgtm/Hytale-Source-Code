using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class BlurProgram : GPUProgram
{
	public Uniform PixelSize;

	public Uniform BlurScale;

	public Uniform HorizontalPass;

	private Uniform ColorTexture;

	public bool UseEdgeAwareness;

	private bool _blurCustomChannels;

	private string _depthChannels;

	private string _channelsToBlur;

	public BlurProgram(bool blurCustomChannels, string customChannelsToBlur = "rgba", string depthChannels = "", string variationName = null)
		: base("ScreenVS.glsl", "BlurFS.glsl", variationName)
	{
		_blurCustomChannels = blurCustomChannels;
		_channelsToBlur = customChannelsToBlur;
		_depthChannels = depthChannels;
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_CUSTOM_CHANNELS", _blurCustomChannels ? "1" : "0");
		dictionary.Add("BLUR_CHANNELS", _channelsToBlur);
		dictionary.Add("USE_EDGE_AWARENESS", UseEdgeAwareness ? "1" : "0");
		dictionary.Add("DEPTH_CHANNELS", _depthChannels);
		uint fragmentShader = CompileFragmentShader(dictionary);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		ColorTexture.SetValue(0);
	}
}
