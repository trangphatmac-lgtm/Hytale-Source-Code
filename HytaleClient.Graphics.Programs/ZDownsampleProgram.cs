using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class ZDownsampleProgram : GPUProgram
{
	public enum DownsamplingMode
	{
		Z_MAX,
		Z_MIN,
		Z_MIN_MAX,
		Z_ROTATED_GRID
	}

	public Uniform Mode;

	public Uniform ProjectionMatrix;

	public Uniform FarClipAndInverse;

	public Uniform PixelSize;

	private Uniform ZBuffer;

	private readonly bool WriteToColor;

	private readonly bool WriteToDepth;

	private readonly bool UseLinearZ;

	public ZDownsampleProgram(bool writeToColor, bool writeToDepth, bool useLinearZ, string variationName = null)
		: base("ScreenVS.glsl", "ZDownsampleFS.glsl", variationName)
	{
		WriteToColor = writeToColor;
		WriteToDepth = writeToDepth;
		UseLinearZ = useLinearZ;
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("OUTPUT_COLOR", WriteToColor ? "1" : "0");
		dictionary.Add("OUTPUT_DEPTH", WriteToDepth ? "1" : "0");
		dictionary.Add("INPUT_IS_LINEAR", UseLinearZ ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		ZBuffer.SetValue(0);
	}
}
