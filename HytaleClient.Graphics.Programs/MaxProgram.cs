using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class MaxProgram : GPUProgram
{
	public Uniform PixelSize;

	public Uniform HorizontalPass;

	protected Uniform ColorTexture;

	private bool _useVec3;

	private int _kernelSize;

	public MaxProgram(bool useVec3, int kernelSize, string variationName = null)
		: base("ScreenVS.glsl", "MaxFilterFS.glsl", variationName)
	{
		_useVec3 = useVec3;
		_kernelSize = kernelSize;
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("USE_VEC3", _useVec3 ? "1" : "0");
		dictionary.Add("KERNEL_SIZE", _kernelSize.ToString());
		uint fragmentShader = CompileFragmentShader(dictionary);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		ColorTexture.SetValue(0);
	}
}
