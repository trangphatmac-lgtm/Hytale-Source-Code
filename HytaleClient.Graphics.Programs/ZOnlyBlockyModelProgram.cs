using System.Collections.Generic;
using HytaleClient.Data.BlockyModels;

namespace HytaleClient.Graphics.Programs;

internal class ZOnlyBlockyModelProgram : GPUProgram
{
	public UniformBufferObject NodeBlock;

	public Uniform ModelMatrix;

	public Uniform ViewMatrix;

	public Uniform ViewProjectionMatrix;

	public Uniform ViewportInfos;

	public Uniform InvModelHeight;

	public Uniform Time;

	public Uniform DrawId;

	public Uniform ModelVFXAnimationProgress;

	public Uniform ModelVFXId;

	private Uniform Texture0;

	private Uniform Texture1;

	private Uniform Texture2;

	private Uniform NoiseTexture;

	private Uniform EntityShadowMapDataBuffer;

	private Uniform ModelVFXDataBuffer;

	public readonly Attrib AttribNodeIndex;

	public readonly Attrib AttribAtlasIndexAndShadingModeAndGradientId;

	public readonly Attrib AttribPosition;

	public readonly Attrib AttribTexCoords;

	public bool UseBiasMethod1 = false;

	public bool UseBiasMethod2 = false;

	public bool UseDrawInstanced = false;

	public bool UseModelVFX = false;

	private BlockyModelProgram _blockyModelProgram;

	public ZOnlyBlockyModelProgram(BlockyModelProgram blockyModelProgram, bool useModelVFX = false, string variationName = null)
		: base("ZOnlyBlockyModelVS.glsl", "ZOnlyBlockyModelFS.glsl", variationName)
	{
		_blockyModelProgram = blockyModelProgram;
		UseModelVFX = useModelVFX;
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("MAX_NODES_COUNT", BlockyModel.MaxNodeCount.ToString());
		dictionary.Add("USE_BIAS_METHOD_1", UseBiasMethod1 ? "1" : "0");
		dictionary.Add("USE_BIAS_METHOD_2", UseBiasMethod2 ? "1" : "0");
		dictionary.Add("USE_DRAW_INSTANCED", UseDrawInstanced ? "1" : "0");
		dictionary.Add("USE_MODEL_VFX", UseModelVFX ? "1" : "0");
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("USE_BIAS_METHOD_1", UseBiasMethod1 ? "1" : "0");
		dictionary2.Add("USE_BIAS_METHOD_2", UseBiasMethod2 ? "1" : "0");
		dictionary2.Add("USE_DRAW_INSTANCED", UseDrawInstanced ? "1" : "0");
		dictionary2.Add("USE_MODEL_VFX", UseModelVFX ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary2);
		List<AttribBindingInfo> list = new List<AttribBindingInfo>(5);
		list.Add(new AttribBindingInfo(_blockyModelProgram.AttribNodeIndex.Index, "vertNodeIndex"));
		list.Add(new AttribBindingInfo(_blockyModelProgram.AttribAtlasIndexAndShadingModeAndGradientId.Index, "vertAtlasIndexAndShadingModeAndGradientId"));
		list.Add(new AttribBindingInfo(_blockyModelProgram.AttribPosition.Index, "vertPosition"));
		list.Add(new AttribBindingInfo(_blockyModelProgram.AttribTexCoords.Index, "vertTexCoords"));
		return MakeProgram(vertexShader, fragmentShader, list, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		Texture0.SetValue(0);
		Texture1.SetValue(1);
		Texture2.SetValue(2);
		NoiseTexture.SetValue(4);
		ModelVFXDataBuffer.SetValue(6);
		EntityShadowMapDataBuffer.SetValue(7);
		NodeBlock.SetupBindingPoint(this, 5u);
	}
}
