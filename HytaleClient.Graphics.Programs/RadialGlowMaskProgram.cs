using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class RadialGlowMaskProgram : GPUProgram
{
	public Uniform MVPMatrix;

	public Uniform SunMVPMatrix;

	public Uniform ProjectionMatrix;

	private Uniform SceneColorTexture;

	private Uniform GlowMaskTexture;

	private Uniform DepthTexture;

	public RadialGlowMaskProgram()
		: base("RadialGlowMaskVS.glsl", "RadialGlowMaskFS.glsl")
	{
	}

	public override bool Initialize()
	{
		base.Initialize();
		uint vertexShader = CompileVertexShader();
		Dictionary<string, string> defines = new Dictionary<string, string>();
		uint fragmentShader = CompileFragmentShader(defines);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		DepthTexture.SetValue(2);
		GlowMaskTexture.SetValue(1);
		SceneColorTexture.SetValue(0);
	}
}
