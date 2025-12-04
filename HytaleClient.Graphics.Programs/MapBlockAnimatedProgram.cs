using System;
using System.Collections.Generic;
using HytaleClient.Data.Map;

namespace HytaleClient.Graphics.Programs;

internal class MapBlockAnimatedProgram : GPUProgram
{
	public UniformBufferObject SceneDataBlock;

	public UniformBufferObject NodeBlock;

	public Uniform ModelMatrix;

	public Uniform ViewProjectionMatrix;

	public readonly Attrib AttribPositionAndDoubleSidedAndBlockId;

	public readonly Attrib AttribTexCoords;

	public readonly Attrib AttribDataPacked;

	public bool UseDebugBoundaries;

	public bool Deferred;

	public bool UseLightBufferCompression;

	public bool UseForwardClusteredLighting;

	public bool UseLightDirectAccess = true;

	public bool UseCustomZDistribution = true;

	public bool WriteRenderConfigBitsInAlpha;

	public bool UseDithering;

	public bool UseSmoothNearMoodColor;

	public bool UseMoodFog;

	public bool UseFog;

	private readonly int MaxNodeCount;

	private bool _useSceneDataOverride;

	public MapBlockAnimatedProgram(int maxNodeCount, bool useDeferred, bool useSceneDataOverride, bool writeRenderConfigBitsInAlpha, string variationName = null)
		: base("MapChunkVS.glsl", "MapChunkFS.glsl", variationName)
	{
		MaxNodeCount = maxNodeCount;
		Deferred = useDeferred;
		WriteRenderConfigBitsInAlpha = writeRenderConfigBitsInAlpha;
		_useSceneDataOverride = useSceneDataOverride;
		UseFog = (UseMoodFog = Deferred);
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("DEFERRED", Deferred ? "1" : "0");
		dictionary.Add("ALPHA_TEST", "1");
		dictionary.Add("ALPHA_BLEND", "0");
		dictionary.Add("NEAR", "1");
		dictionary.Add("ANIMATED", "1");
		int maxNodeCount = MaxNodeCount;
		dictionary.Add("MAX_NODES_COUNT", maxNodeCount.ToString());
		dictionary.Add("USE_LOD", "0");
		dictionary.Add("DEBUG_BOUNDARIES", UseDebugBoundaries ? "1" : "0");
		dictionary.Add("USE_SCENE_DATA_OVERRIDE", _useSceneDataOverride ? "1" : "0");
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("DEFERRED", Deferred ? "1" : "0");
		dictionary2.Add("USE_LBUFFER_COMPRESSION", UseLightBufferCompression ? "1" : "0");
		dictionary2.Add("ALPHA_TEST", "1");
		dictionary2.Add("ALPHA_BLEND", "0");
		dictionary2.Add("NEAR", "1");
		dictionary2.Add("ANIMATED", "1");
		dictionary2.Add("USE_CLUSTERED_LIGHTING", "0");
		dictionary2.Add("WRITE_RENDERCONFIG_IN_ALPHA", (!Deferred && WriteRenderConfigBitsInAlpha) ? "1" : "0");
		dictionary2.Add("USE_FOG_DITHERING", UseDithering ? "1" : "0");
		dictionary2.Add("USE_SMOOTH_NEAR_MOOD_FOG_COLOR", UseSmoothNearMoodColor ? "1" : "0");
		dictionary2.Add("USE_MOOD_FOG", UseMoodFog ? "1" : "0");
		dictionary2.Add("USE_FOG", UseFog ? "1" : "0");
		dictionary2.Add("DEBUG_OCCLUSION_CULLING", "0");
		dictionary2.Add("USE_SCENE_DATA_OVERRIDE", _useSceneDataOverride ? "1" : "0");
		ClientBlockType.ClientShaderEffect[] array = (ClientBlockType.ClientShaderEffect[])Enum.GetValues(typeof(ClientBlockType.ClientShaderEffect));
		for (int i = 0; i < array.Length; i++)
		{
			ClientBlockType.ClientShaderEffect clientShaderEffect = array[i];
			string key = "EFFECT_" + clientShaderEffect.ToString().ToUpper();
			maxNodeCount = (int)clientShaderEffect;
			string value = maxNodeCount.ToString();
			dictionary.Add(key, value);
			dictionary2.Add(key, value);
		}
		ShadingMode[] array2 = (ShadingMode[])Enum.GetValues(typeof(ShadingMode));
		for (int j = 0; j < array2.Length; j++)
		{
			ShadingMode shadingMode = array2[j];
			string key2 = "SHADING_" + shadingMode.ToString().ToUpper();
			maxNodeCount = (int)shadingMode;
			string value2 = maxNodeCount.ToString();
			dictionary.Add(key2, value2);
			dictionary2.Add(key2, value2);
		}
		uint vertexShader = CompileVertexShader(dictionary);
		uint fragmentShader = CompileFragmentShader(dictionary2);
		List<AttribBindingInfo> list = new List<AttribBindingInfo>(5);
		list.Add(new AttribBindingInfo(0u, "vertPositionAndDoubleSidedAndBlockId"));
		list.Add(new AttribBindingInfo(1u, "vertTexCoords"));
		list.Add(new AttribBindingInfo(2u, "vertDataPacked"));
		return MakeProgram(vertexShader, fragmentShader, list, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		SceneDataBlock.SetupBindingPoint(this, 0u);
		NodeBlock.SetupBindingPoint(this, 5u);
	}
}
