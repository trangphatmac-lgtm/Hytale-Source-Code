using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class HiZReprojectProgram : GPUProgram
{
	public Uniform Resolutions;

	public Uniform ReprojectMatrix;

	public Uniform ProjectionMatrix;

	public Uniform InvalidScreenAreas;

	private Uniform DepthTexture;

	public readonly int MaxInvalidScreenAreas;

	public HiZReprojectProgram()
		: base("HiZReprojectVS.glsl", null)
	{
		MaxInvalidScreenAreas = 10;
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		int maxInvalidScreenAreas = MaxInvalidScreenAreas;
		dictionary.Add("MAX_INVALID_AREAS", maxInvalidScreenAreas.ToString());
		uint vertexShader = CompileVertexShader(dictionary);
		return MakeProgram(vertexShader);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		DepthTexture.SetValue(0);
	}
}
