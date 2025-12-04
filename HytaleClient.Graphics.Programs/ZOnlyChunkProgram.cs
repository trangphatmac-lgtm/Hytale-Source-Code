using System.Collections.Generic;

namespace HytaleClient.Graphics.Programs;

internal class ZOnlyChunkProgram : GPUProgram
{
	public UniformBufferObject NodeBlock;

	public Uniform ModelMatrix;

	public Uniform ViewProjectionMatrix;

	public Uniform Time;

	public Uniform LightPositions;

	public Uniform TargetCascades;

	public Uniform ViewportInfos;

	private Uniform Texture;

	private readonly bool BuildsShadowMaps;

	private readonly bool Animated;

	public bool AlphaTest = true;

	public bool UseDrawInstanced = false;

	public bool UseBackfaceCulling = true;

	public bool UseDistantBackfaceCulling = false;

	public float DistantBackfaceCullingDistance = 92f;

	private readonly int MaxNodeCount;

	private float _mipLodBias;

	private bool _useCompressedPosition;

	private bool _useFoliageCulling;

	public ZOnlyChunkProgram(bool buildShadowMaps, bool animated, int maxNodeCount, bool alphaTest, bool useCompressedPosition, bool useFoliageCulling, float mipLodBias = 0f, string variationName = null)
		: base("ZOnlyChunkVS.glsl", "ZOnlyChunkFS.glsl", variationName)
	{
		BuildsShadowMaps = buildShadowMaps;
		Animated = animated;
		MaxNodeCount = maxNodeCount;
		AlphaTest = alphaTest;
		_useCompressedPosition = useCompressedPosition;
		_useFoliageCulling = useFoliageCulling;
		_mipLodBias = mipLodBias;
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("ALPHA_TEST", AlphaTest ? "1" : "0");
		dictionary.Add("USE_COMPRESSED_POSITION", _useCompressedPosition ? "1" : "0");
		dictionary.Add("USE_FOLIAGE_CULLING", _useFoliageCulling ? "1" : "0");
		dictionary.Add("USE_DRAW_INSTANCED", UseDrawInstanced ? "1" : "0");
		dictionary.Add("USE_BACKFACE_CULLING", UseBackfaceCulling ? "1" : "0");
		dictionary.Add("USE_DISTANT_BACKFACE_CULLING", UseDistantBackfaceCulling ? "1" : "0");
		dictionary.Add("DISTANT_BACKFACE_CULLING_DISTANCE", DistantBackfaceCullingDistance.ToString());
		dictionary.Add("SHADOW_VERSION", BuildsShadowMaps ? "1" : "0");
		dictionary.Add("ANIMATED", Animated ? "1" : "0");
		int maxNodeCount = MaxNodeCount;
		dictionary.Add("MAX_NODES_COUNT", maxNodeCount.ToString());
		uint vertexShader = CompileVertexShader(dictionary);
		List<AttribBindingInfo> list = new List<AttribBindingInfo>(5);
		list.Add(new AttribBindingInfo(0u, "vertPositionAndDoubleSidedAndBlockId"));
		list.Add(new AttribBindingInfo(2u, "vertDataPacked"));
		if (AlphaTest || UseDrawInstanced)
		{
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			dictionary2.Add("USE_DRAW_INSTANCED", UseDrawInstanced ? "1" : "0");
			dictionary2.Add("ALPHA_TEST", AlphaTest ? "1" : "0");
			dictionary2.Add("MIP_LOD_BIAS", _mipLodBias.ToString());
			uint fragmentShader = CompileFragmentShader(dictionary2);
			if (AlphaTest)
			{
				list.Add(new AttribBindingInfo(1u, "vertTexCoords"));
			}
			return MakeProgram(vertexShader, fragmentShader, list, ignoreMissingUniforms: true);
		}
		return MakeProgram(vertexShader, list, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		if (AlphaTest)
		{
			GPUProgram._gl.UseProgram(this);
			Texture.SetValue(0);
		}
		if (Animated)
		{
			NodeBlock.SetupBindingPoint(this, 5u);
		}
	}
}
