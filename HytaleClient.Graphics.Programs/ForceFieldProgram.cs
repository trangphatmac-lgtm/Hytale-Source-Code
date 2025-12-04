#define DEBUG
using System.Collections.Generic;
using System.Diagnostics;

namespace HytaleClient.Graphics.Programs;

internal class ForceFieldProgram : GPUProgram
{
	public struct TextureUnitLayout
	{
		public byte Texture;

		public byte SceneDepth;

		public byte OITMoments;

		public byte OITTotalOpticalDepth;
	}

	public readonly int DrawModeColor = 0;

	public readonly int DrawModeDistortion = 1;

	public readonly int BlendModePremultLinear = 0;

	public readonly int BlendModeAdd = 1;

	public readonly int BlendModeLinear = 2;

	public readonly int OutlineModeNone = 0;

	public readonly int OutlineModeUV = 1;

	public readonly int OutlineModeNormal = 2;

	public UniformBufferObject SceneDataBlock;

	public Uniform ViewMatrix;

	public Uniform ViewProjectionMatrix;

	public Uniform ModelMatrix;

	public Uniform NormalMatrix;

	public Uniform ColorOpacity;

	public Uniform IntersectionHighlightColorOpacity;

	public Uniform IntersectionHighlightThickness;

	public Uniform UVAnimationSpeed;

	public Uniform OutlineMode;

	public Uniform DrawAndBlendMode;

	public Uniform CurrentInvViewportSize;

	public Uniform OITParams;

	private Uniform MomentsTexture;

	private Uniform TotalOpticalDepthTexture;

	private Uniform Texture;

	private Uniform DepthTexture;

	public readonly Attrib AttribPosition;

	public readonly Attrib AttribTexCoords;

	public readonly Attrib AttribNormal;

	public bool UseOIT;

	private bool _discardFaceHighlight;

	private bool _useUndergroundColor;

	private TextureUnitLayout _textureUnitLayout;

	public ForceFieldProgram(bool discardFaceHighlight = false, bool useUndergroundColor = false, string variationName = null)
		: base("BasicVS.glsl", "ForceFieldFS.glsl", variationName)
	{
		_discardFaceHighlight = discardFaceHighlight;
		_useUndergroundColor = useUndergroundColor;
	}

	public void SetupTextureUnits(ref TextureUnitLayout textureUnitLayout, bool initUniforms = false)
	{
		Debug.Assert(GPUProgram.IsResourceBindingLayoutValid(textureUnitLayout), "Invalid TextureUnitLayout.");
		_textureUnitLayout = textureUnitLayout;
		if (initUniforms)
		{
			InitUniforms();
		}
	}

	public override bool Initialize()
	{
		base.Initialize();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("NEED_POS_VS", "1");
		dictionary.Add("NEED_POS_WS", _discardFaceHighlight ? "1" : "0");
		dictionary.Add("USE_VERT_NORMALS", "1");
		uint vertexShader = CompileVertexShader(dictionary);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2.Add("USE_OIT", UseOIT ? "1" : "0");
		dictionary2.Add("DISCARD_FACE_HIGHLIGHT", _discardFaceHighlight ? "1" : "0");
		dictionary2.Add("USE_UNDERGROUND_COLOR", _useUndergroundColor ? "1" : "0");
		uint fragmentShader = CompileFragmentShader(dictionary2);
		return MakeProgram(vertexShader, fragmentShader, null, ignoreMissingUniforms: true);
	}

	protected override void InitUniforms()
	{
		GPUProgram._gl.UseProgram(this);
		MomentsTexture.SetValue(_textureUnitLayout.OITMoments);
		TotalOpticalDepthTexture.SetValue(_textureUnitLayout.OITTotalOpticalDepth);
		DepthTexture.SetValue(_textureUnitLayout.SceneDepth);
		Texture.SetValue(_textureUnitLayout.Texture);
		SceneDataBlock.SetupBindingPoint(this, 0u);
	}
}
