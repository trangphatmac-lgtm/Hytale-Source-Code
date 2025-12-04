#define DEBUG
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics;

internal class RenderTargetStore
{
	public struct DebugMapParam
	{
		public enum ColorChannelBits
		{
			A = 1,
			B = 2,
			G = 4,
			R = 8,
			BA = 3,
			GB = 6,
			RB = 10,
			RG = 12,
			RGB = 14
		}

		public enum ChromaSubsamplingMode
		{
			None,
			Color,
			Light
		}

		public enum DebugMapInputType
		{
			Texture2D,
			Texture2DArray,
			RenderTarget,
			Cubemap
		}

		public struct Texture2DArrayInfo
		{
			public GLTexture Texture;

			public int Width;

			public int Height;

			public int LayerCount;
		}

		public DebugMapInputType InputType;

		public Texture Texture2D;

		public Texture2DArrayInfo Texture2DArray;

		public RenderTarget RenderTarget;

		public RenderTarget.Target Target;

		public bool HasZValues;

		public bool HasLinearZValues;

		public bool UseNormalQuantization;

		public ChromaSubsamplingMode ChromaSubSamplingMode;

		public float Multiplier;

		public float DebugMaxOverdraw;

		public ColorChannelBits ColorChannels;

		public Vector2 ViewportScale;

		public float Scale;

		public DebugMapParam(Texture texture, ColorChannelBits colorChannels = ColorChannelBits.RGB, bool isACubemap = false)
		{
			InputType = (isACubemap ? DebugMapInputType.Cubemap : DebugMapInputType.Texture2D);
			Texture2D = texture;
			Texture2DArray.Texture = GLTexture.None;
			Texture2DArray.Width = 0;
			Texture2DArray.Height = 0;
			Texture2DArray.LayerCount = 0;
			RenderTarget = null;
			Target = RenderTarget.Target.MAX;
			HasZValues = false;
			HasLinearZValues = false;
			UseNormalQuantization = false;
			ChromaSubSamplingMode = ChromaSubsamplingMode.None;
			Multiplier = 1f;
			DebugMaxOverdraw = 0f;
			ColorChannels = colorChannels;
			ViewportScale = Vector2.One;
			Scale = 1f;
		}

		public DebugMapParam(RenderTarget renderTarget, RenderTarget.Target target, bool hasZValues, bool hasLinearZValues, bool useNormalQuantization = false, ChromaSubsamplingMode chromaSubsamplingMode = ChromaSubsamplingMode.None, ColorChannelBits colorChannels = ColorChannelBits.RGB, float multiplier = 1f, float widthScale = 1f, float heightScale = 1f, float scale = 1f, float debugMaxOverdraw = 0f)
		{
			InputType = DebugMapInputType.RenderTarget;
			Texture2D = null;
			Texture2DArray.Texture = GLTexture.None;
			Texture2DArray.Width = 0;
			Texture2DArray.Height = 0;
			Texture2DArray.LayerCount = 0;
			RenderTarget = renderTarget;
			Target = target;
			HasZValues = hasZValues;
			HasLinearZValues = hasLinearZValues;
			UseNormalQuantization = useNormalQuantization;
			ChromaSubSamplingMode = chromaSubsamplingMode;
			Multiplier = multiplier;
			DebugMaxOverdraw = debugMaxOverdraw;
			ColorChannels = colorChannels;
			ViewportScale = new Vector2(widthScale, heightScale);
			Scale = scale;
		}

		public DebugMapParam(GLTexture texture, int width, int height, int layerCount)
		{
			InputType = DebugMapInputType.Texture2DArray;
			Texture2D = null;
			Texture2DArray.Texture = texture;
			Texture2DArray.Width = width;
			Texture2DArray.Height = height;
			Texture2DArray.LayerCount = layerCount;
			RenderTarget = null;
			Target = RenderTarget.Target.MAX;
			HasZValues = false;
			HasLinearZValues = false;
			UseNormalQuantization = false;
			ChromaSubSamplingMode = ChromaSubsamplingMode.None;
			Multiplier = 1f;
			DebugMaxOverdraw = 0f;
			ColorChannels = ColorChannelBits.RGB;
			ViewportScale = Vector2.One;
			Scale = 1f;
		}
	}

	public RenderTarget HardwareZ;

	public RenderTarget HardwareZHalfRes;

	public RenderTarget HardwareZQuarterRes;

	public RenderTarget HardwareZEighthRes;

	public RenderTarget LinearZ;

	public RenderTarget LinearZHalfRes;

	public RenderTarget Edges;

	public RenderTarget GBuffer;

	public RenderTarget PingSceneColor;

	public RenderTarget PongSceneColor;

	public RenderTarget PingFinalSceneColor;

	public RenderTarget PongFinalSceneColor;

	public RenderTarget SceneColorHalfRes;

	public RenderTarget LightBufferFullRes;

	public RenderTarget LightBufferHalfRes;

	public RenderTarget Transparency;

	public RenderTarget TransparencyHalfRes;

	public RenderTarget TransparencyQuarterRes;

	public RenderTarget MomentsTransparencyCapture;

	public RenderTarget Distortion;

	public RenderTarget DebugFXOverdraw;

	public RenderTarget VolumetricSunshaft;

	public RenderTarget ShadowMap;

	public RenderTarget DeferredShadow;

	public RenderTarget PingSSAO;

	public RenderTarget PongSSAO;

	public RenderTarget BlurSSAOAndShadowTmp;

	public RenderTarget BlurSSAOAndShadow;

	public RenderTarget DOFBlurXBis;

	public RenderTarget DOFBlurYBis;

	public RenderTarget DOFBlurX;

	public RenderTarget DOFBlurY;

	public RenderTarget CoC;

	public RenderTarget Downsample;

	public RenderTarget NearCoCMaxX;

	public RenderTarget NearCoCMaxY;

	public RenderTarget NearCoCBlurX;

	public RenderTarget NearCoCBlurY;

	public RenderTarget NearFarField;

	public RenderTarget Fill;

	public RenderTarget BlurXResBy2;

	public RenderTarget BlurYResBy2;

	public RenderTarget BlurXResBy4;

	public RenderTarget BlurYResBy4;

	public RenderTarget BlurXResBy8;

	public RenderTarget BlurYResBy8;

	public RenderTarget BlurXResBy16;

	public RenderTarget BlurYResBy16;

	public RenderTarget BlurXResBy32;

	public RenderTarget SunRT;

	public RenderTarget SunshaftX;

	public RenderTarget SunshaftY;

	public RenderTarget SunOcclusionBufferLowRes;

	public RenderTarget SunOcclusionHistory;

	public RenderTarget SSAORaw;

	public RenderTarget PreviousSSAORaw;

	public RenderTarget SceneColor;

	public RenderTarget PreviousSceneColor;

	public RenderTarget FinalSceneColor;

	public RenderTarget PreviousFinalSceneColor;

	private GraphicsDevice _graphics;

	private Vector2 _momentsTransparencyResolutionScale;

	private Vector2 _deferredShadowResolutionScale;

	private Vector2 _ssaoResolutionScale;

	private Vector2 _viewportSize;

	private Dictionary<string, DebugMapParam> _debugMapInfo = new Dictionary<string, DebugMapParam>();

	public RenderTargetStore(GraphicsDevice graphics, int width, int height, Vector2 shadowMapResolution, Vector2 deferredShadowResolutionScale, Vector2 ssaoResolutionScale)
	{
		_graphics = graphics;
		_momentsTransparencyResolutionScale = new Vector2(1f);
		_deferredShadowResolutionScale = deferredShadowResolutionScale;
		_ssaoResolutionScale = ssaoResolutionScale;
		int width2 = (int)((float)width * 0.5f);
		int height2 = (int)((float)height * 0.5f);
		int width3 = (int)((float)width * 0.25f);
		int height3 = (int)((float)height * 0.25f);
		int width4 = (int)((float)width * 0.125f);
		int height4 = (int)((float)height * 0.125f);
		int num = (int)((float)width * 0.0625f);
		int num2 = (int)((float)height * 0.0625f);
		int width5 = (int)((float)num * 0.5f);
		int height5 = (int)((float)num2 * 0.5f);
		HardwareZ = new RenderTarget(width, height, "HardwareZ");
		HardwareZ.AddTexture(RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8, GL.NEAREST, GL.NEAREST);
		HardwareZ.FinalizeSetup();
		HardwareZHalfRes = new RenderTarget(width2, height2, "HardwareZHalfRes");
		HardwareZHalfRes.AddTexture(RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8, GL.NEAREST, GL.NEAREST);
		HardwareZHalfRes.FinalizeSetup();
		HardwareZQuarterRes = new RenderTarget(width3, height3, "HardwareZQuarterRes");
		HardwareZQuarterRes.AddTexture(RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8, GL.NEAREST, GL.NEAREST);
		HardwareZQuarterRes.FinalizeSetup();
		HardwareZEighthRes = new RenderTarget(width4, height4, "HardwareZEighthRes");
		HardwareZEighthRes.AddTexture(RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8, GL.NEAREST, GL.NEAREST);
		HardwareZEighthRes.FinalizeSetup();
		LinearZ = new RenderTarget(width, height, "LinearZ");
		LinearZ.AddTexture(RenderTarget.Target.Color0, GL.R16F, GL.RED, GL.FLOAT, GL.NEAREST, GL.NEAREST);
		LinearZ.FinalizeSetup();
		LinearZHalfRes = new RenderTarget(width2, height2, "LinearZHalfRes");
		LinearZHalfRes.AddTexture(RenderTarget.Target.Color0, GL.R16F, GL.RED, GL.FLOAT, GL.NEAREST, GL.NEAREST);
		LinearZHalfRes.FinalizeSetup();
		Edges = new RenderTarget(width, height, "Edges");
		Edges.UseAsRenderTexture(HardwareZ.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		Edges.AddTexture(RenderTarget.Target.Color0, GL.R8, GL.RED, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		Edges.FinalizeSetup();
		Edges.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		GBuffer = new RenderTarget(width, height, "GBuffer");
		GBuffer.UseAsRenderTexture(HardwareZ.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		GBuffer.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.NEAREST, GL.NEAREST);
		GBuffer.AddTexture(RenderTarget.Target.Color2, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.NEAREST, GL.NEAREST);
		GBuffer.FinalizeSetup();
		GBuffer.SetClearBits(clearColor: false, clearDepth: true, clearStencil: true);
		PingSceneColor = new RenderTarget(width, height, "PingSceneColor");
		PingSceneColor.UseAsRenderTexture(HardwareZ.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		PingSceneColor.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.NEAREST, GL.LINEAR, GL.CLAMP_TO_EDGE, requestCompareModeForDepth: false, requestMipMapChain: true);
		PingSceneColor.FinalizeSetup();
		PingSceneColor.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		PongSceneColor = new RenderTarget(width, height, "PongSceneColor");
		PongSceneColor.UseAsRenderTexture(HardwareZ.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		PongSceneColor.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.NEAREST, GL.LINEAR, GL.CLAMP_TO_EDGE, requestCompareModeForDepth: false, requestMipMapChain: true);
		PongSceneColor.FinalizeSetup();
		PongSceneColor.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		PingFinalSceneColor = new RenderTarget(width, height, "PingFinalSceneColor");
		PingFinalSceneColor.UseAsRenderTexture(HardwareZ.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		PingFinalSceneColor.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		PingFinalSceneColor.FinalizeSetup();
		PingFinalSceneColor.SetClearBits(clearColor: false, clearDepth: false, clearStencil: false);
		PongFinalSceneColor = new RenderTarget(width, height, "PongFinalSceneColor");
		PongFinalSceneColor.UseAsRenderTexture(HardwareZ.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		PongFinalSceneColor.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		PongFinalSceneColor.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		PongFinalSceneColor.FinalizeSetup();
		PongFinalSceneColor.SetClearBits(clearColor: false, clearDepth: false, clearStencil: false);
		SceneColorHalfRes = new RenderTarget(width2, height2, "SceneColorHalfRes");
		SceneColorHalfRes.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR, GL.CLAMP_TO_EDGE, requestCompareModeForDepth: false, requestMipMapChain: true);
		SceneColorHalfRes.FinalizeSetup();
		SceneColorHalfRes.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		LightBufferFullRes = new RenderTarget(width, height, "LightBufferFullRes");
		LightBufferFullRes.UseAsRenderTexture(GBuffer.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		LightBufferFullRes.UseAsRenderTexture(GBuffer.GetTexture(RenderTarget.Target.Color2), skipDispose: true, RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE);
		LightBufferFullRes.FinalizeSetup();
		LightBufferFullRes.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		LightBufferHalfRes = new RenderTarget(width2, height2, "LightBufferHalfRes");
		LightBufferHalfRes.UseAsRenderTexture(HardwareZHalfRes.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		LightBufferHalfRes.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		LightBufferHalfRes.FinalizeSetup();
		LightBufferHalfRes.SetClearBits(clearColor: true, clearDepth: false, clearStencil: true);
		Transparency = new RenderTarget(width, height, "Transparency");
		Transparency.UseAsRenderTexture(HardwareZ.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		Transparency.AddTexture(RenderTarget.Target.Color0, GL.RGBA16F, GL.RGBA, GL.FLOAT, GL.NEAREST, GL.NEAREST);
		Transparency.AddTexture(RenderTarget.Target.Color1, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.NEAREST, GL.NEAREST);
		Transparency.FinalizeSetup();
		Transparency.SetClearBits(clearColor: false, clearDepth: false, clearStencil: false);
		TransparencyHalfRes = new RenderTarget(width2, height2, "TransparencyHalfRes");
		TransparencyHalfRes.UseAsRenderTexture(HardwareZHalfRes.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		TransparencyHalfRes.AddTexture(RenderTarget.Target.Color0, GL.RGBA16F, GL.RGBA, GL.FLOAT, GL.LINEAR, GL.LINEAR);
		TransparencyHalfRes.AddTexture(RenderTarget.Target.Color1, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		TransparencyHalfRes.FinalizeSetup();
		TransparencyHalfRes.SetClearBits(clearColor: false, clearDepth: false, clearStencil: false);
		TransparencyQuarterRes = new RenderTarget(width3, height3, "TransparencyQuarterRes");
		TransparencyQuarterRes.UseAsRenderTexture(HardwareZQuarterRes.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		TransparencyQuarterRes.AddTexture(RenderTarget.Target.Color0, GL.RGBA16F, GL.RGBA, GL.FLOAT, GL.LINEAR, GL.LINEAR);
		TransparencyQuarterRes.AddTexture(RenderTarget.Target.Color1, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		TransparencyQuarterRes.FinalizeSetup();
		TransparencyQuarterRes.SetClearBits(clearColor: false, clearDepth: false, clearStencil: false);
		MomentsTransparencyCapture = new RenderTarget(width, height, "MomentsTransparencyCapture");
		MomentsTransparencyCapture.UseAsRenderTexture(HardwareZ.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		MomentsTransparencyCapture.AddTexture(RenderTarget.Target.Color0, GL.RGBA32F, GL.RGBA, GL.FLOAT, GL.LINEAR, GL.LINEAR);
		MomentsTransparencyCapture.AddTexture(RenderTarget.Target.Color1, GL.R32F, GL.RED, GL.FLOAT, GL.LINEAR, GL.LINEAR);
		MomentsTransparencyCapture.FinalizeSetup();
		MomentsTransparencyCapture.SetClearBits(clearColor: false, clearDepth: false, clearStencil: false);
		Distortion = new RenderTarget(width2, height2, "Distortion");
		Distortion.UseAsRenderTexture(HardwareZHalfRes.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		Distortion.AddTexture(RenderTarget.Target.Color0, GL.RG16F, GL.RG, GL.FLOAT, GL.LINEAR, GL.LINEAR);
		Distortion.FinalizeSetup();
		Distortion.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		DebugFXOverdraw = new RenderTarget(width, height, "DebugFXOverdraw");
		DebugFXOverdraw.UseAsRenderTexture(HardwareZ.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		DebugFXOverdraw.AddTexture(RenderTarget.Target.Color0, GL.R16F, GL.RED, GL.FLOAT, GL.NEAREST, GL.NEAREST);
		DebugFXOverdraw.FinalizeSetup();
		DebugFXOverdraw.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		ShadowMap = new RenderTarget((int)shadowMapResolution.X, (int)shadowMapResolution.Y, "ShadowMap");
		ShadowMap.AddTexture(RenderTarget.Target.Depth, GL.DEPTH_COMPONENT32F, GL.DEPTH_COMPONENT, GL.FLOAT, GL.NEAREST, GL.NEAREST, GL.CLAMP_TO_BORDER, requestCompareModeForDepth: true);
		ShadowMap.FinalizeSetup();
		int width6 = (int)((float)width * deferredShadowResolutionScale.X);
		int height6 = (int)((float)height * deferredShadowResolutionScale.Y);
		DeferredShadow = new RenderTarget(width6, height6, "DeferredShadow");
		DeferredShadow.AddTexture(RenderTarget.Target.Color0, GL.R8, GL.RED, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		DeferredShadow.FinalizeSetup();
		int width7 = (int)((float)width * ssaoResolutionScale.X);
		int height7 = (int)((float)height * ssaoResolutionScale.Y);
		PingSSAO = new RenderTarget(width7, height7, "PingSSAO");
		PingSSAO.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		PingSSAO.FinalizeSetup();
		PongSSAO = new RenderTarget(width7, height7, "PongSSAO");
		PongSSAO.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		PongSSAO.FinalizeSetup();
		BlurSSAOAndShadowTmp = new RenderTarget(width7, height7, "BlurSSAOAndShadowTmp");
		BlurSSAOAndShadowTmp.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		BlurSSAOAndShadowTmp.FinalizeSetup();
		BlurSSAOAndShadow = new RenderTarget(width7, height7, "BlurSSAOAndShadow");
		BlurSSAOAndShadow.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		BlurSSAOAndShadow.FinalizeSetup();
		DOFBlurXBis = new RenderTarget(width2, height2, "DOFBlurXBis");
		DOFBlurXBis.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		DOFBlurXBis.AddTexture(RenderTarget.Target.Color1, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		DOFBlurXBis.FinalizeSetup();
		DOFBlurXBis.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		DOFBlurYBis = new RenderTarget(width2, height2, "DOFBlurYBis");
		DOFBlurYBis.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		DOFBlurYBis.AddTexture(RenderTarget.Target.Color1, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		DOFBlurYBis.FinalizeSetup();
		DOFBlurYBis.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		DOFBlurX = new RenderTarget(width2, height2, "DOFBlurX");
		DOFBlurX.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		DOFBlurX.FinalizeSetup();
		DOFBlurX.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		DOFBlurY = new RenderTarget(width2, height2, "DOFBlurY");
		DOFBlurY.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		DOFBlurY.FinalizeSetup();
		DOFBlurY.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		CoC = new RenderTarget(width, height, "CoC");
		CoC.AddTexture(RenderTarget.Target.Color0, GL.RG8, GL.RG, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		CoC.FinalizeSetup();
		CoC.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		Downsample = new RenderTarget(width2, height2, "Downsample");
		Downsample.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		Downsample.AddTexture(RenderTarget.Target.Color1, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		Downsample.AddTexture(RenderTarget.Target.Color2, GL.RG8, GL.RG, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		Downsample.FinalizeSetup();
		Downsample.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		NearCoCMaxX = new RenderTarget(width2, height2, "NearCoCMaxX");
		NearCoCMaxX.AddTexture(RenderTarget.Target.Color0, GL.R8, GL.RED, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		NearCoCMaxX.FinalizeSetup();
		NearCoCMaxX.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		NearCoCMaxY = new RenderTarget(width2, height2, "NearCoCMaxY");
		NearCoCMaxY.AddTexture(RenderTarget.Target.Color0, GL.R8, GL.RED, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		NearCoCMaxY.FinalizeSetup();
		NearCoCMaxY.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		NearCoCBlurX = new RenderTarget(width2, height2, "NearCoCBlurX");
		NearCoCBlurX.AddTexture(RenderTarget.Target.Color0, GL.R8, GL.RED, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		NearCoCBlurX.FinalizeSetup();
		NearCoCBlurX.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		NearCoCBlurY = new RenderTarget(width2, height2, "NearCoCBlurY");
		NearCoCBlurY.AddTexture(RenderTarget.Target.Color0, GL.R8, GL.RED, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		NearCoCBlurY.FinalizeSetup();
		NearCoCBlurY.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		NearFarField = new RenderTarget(width2, height2, "NearFarField");
		NearFarField.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		NearFarField.AddTexture(RenderTarget.Target.Color1, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		NearFarField.FinalizeSetup();
		NearFarField.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		Fill = new RenderTarget(width2, height2, "Fill");
		Fill.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		Fill.AddTexture(RenderTarget.Target.Color1, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		Fill.FinalizeSetup();
		Fill.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		BlurXResBy2 = new RenderTarget(width2, height2, "BlurXResBy2");
		BlurXResBy2.AddTexture(RenderTarget.Target.Color0, GL.RGB8, GL.RGB, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		BlurXResBy2.FinalizeSetup();
		BlurXResBy2.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		BlurYResBy2 = new RenderTarget(width2, height2, "BlurYResBy2");
		BlurYResBy2.AddTexture(RenderTarget.Target.Color0, GL.RGB8, GL.RGB, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		BlurYResBy2.FinalizeSetup();
		BlurYResBy2.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		BlurXResBy4 = new RenderTarget(width3, height3, "BlurXResBy4");
		BlurXResBy4.AddTexture(RenderTarget.Target.Color0, GL.RGB8, GL.RGB, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		BlurXResBy4.FinalizeSetup();
		BlurXResBy4.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		BlurYResBy4 = new RenderTarget(width3, height3, "BlurYResBy4");
		BlurYResBy4.AddTexture(RenderTarget.Target.Color0, GL.RGB8, GL.RGB, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		BlurYResBy4.FinalizeSetup();
		BlurYResBy4.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		BlurXResBy8 = new RenderTarget(width4, height4, "BlurXResBy8");
		BlurXResBy8.AddTexture(RenderTarget.Target.Color0, GL.RGB8, GL.RGB, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		BlurXResBy8.FinalizeSetup();
		BlurXResBy8.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		BlurYResBy8 = new RenderTarget(width4, height4, "BlurYResBy8");
		BlurYResBy8.AddTexture(RenderTarget.Target.Color0, GL.RGB8, GL.RGB, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		BlurYResBy8.FinalizeSetup();
		BlurYResBy8.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		BlurXResBy16 = new RenderTarget(num, num2, "BlurXResBy16");
		BlurXResBy16.AddTexture(RenderTarget.Target.Color0, GL.RGB8, GL.RGB, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		BlurXResBy16.FinalizeSetup();
		BlurXResBy16.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		BlurYResBy16 = new RenderTarget(num, num2, "BlurYResBy16");
		BlurYResBy16.AddTexture(RenderTarget.Target.Color0, GL.RGB8, GL.RGB, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		BlurYResBy16.FinalizeSetup();
		BlurYResBy16.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		BlurXResBy32 = new RenderTarget(width5, height5, "BlurXResBy32");
		BlurXResBy32.AddTexture(RenderTarget.Target.Color0, GL.RGB8, GL.RGB, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		BlurXResBy32.FinalizeSetup();
		BlurXResBy32.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		SunRT = new RenderTarget(width, height, "SunRT");
		SunRT.UseAsRenderTexture(HardwareZ.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		SunRT.AddTexture(RenderTarget.Target.Color0, GL.RGB8, GL.RGB, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		SunRT.FinalizeSetup();
		SunRT.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		SunshaftX = new RenderTarget(width3, height3, "SunshaftX");
		SunshaftX.AddTexture(RenderTarget.Target.Color0, GL.RGB8, GL.RGB, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		SunshaftX.FinalizeSetup();
		SunshaftX.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		SunshaftY = new RenderTarget(width3, height3, "SunshaftY");
		SunshaftY.AddTexture(RenderTarget.Target.Color0, GL.RGB8, GL.RGB, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		SunshaftY.FinalizeSetup();
		SunshaftY.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		VolumetricSunshaft = new RenderTarget(width2, height2, "VolumetricSunshaft");
		VolumetricSunshaft.UseAsRenderTexture(HardwareZHalfRes.GetTexture(RenderTarget.Target.Depth), skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		VolumetricSunshaft.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.NEAREST, GL.LINEAR, GL.CLAMP_TO_EDGE, requestCompareModeForDepth: false, requestMipMapChain: true);
		VolumetricSunshaft.FinalizeSetup();
		VolumetricSunshaft.SetClearBits(clearColor: true, clearDepth: false, clearStencil: false);
		SunOcclusionBufferLowRes = new RenderTarget(512, 256, "SunOcclusionBufferLowRes");
		SunOcclusionBufferLowRes.AddTexture(RenderTarget.Target.Color0, GL.R8, GL.RED, GL.UNSIGNED_BYTE, GL.LINEAR_MIPMAP_LINEAR, GL.LINEAR, GL.CLAMP_TO_EDGE, requestCompareModeForDepth: false, requestMipMapChain: true);
		SunOcclusionBufferLowRes.FinalizeSetup();
		SunOcclusionHistory = new RenderTarget(512, 1, "SunOcclusionHistory");
		SunOcclusionHistory.AddTexture(RenderTarget.Target.Color0, GL.R8, GL.RED, GL.UNSIGNED_BYTE, GL.LINEAR_MIPMAP_NEAREST, GL.NEAREST);
		SunOcclusionHistory.FinalizeSetup();
		SSAORaw = PingSSAO;
		PreviousSSAORaw = PongSSAO;
		SceneColor = PingSceneColor;
		PreviousSceneColor = PongSceneColor;
		InitDebugMapInfos();
	}

	public void Dispose()
	{
		SunOcclusionHistory.Dispose();
		SunOcclusionBufferLowRes.Dispose();
		VolumetricSunshaft.Dispose();
		SunshaftY.Dispose();
		SunshaftX.Dispose();
		SunRT.Dispose();
		BlurXResBy32.Dispose();
		BlurYResBy16.Dispose();
		BlurXResBy16.Dispose();
		BlurYResBy8.Dispose();
		BlurXResBy8.Dispose();
		BlurYResBy4.Dispose();
		BlurXResBy4.Dispose();
		BlurYResBy2.Dispose();
		BlurXResBy2.Dispose();
		Fill.Dispose();
		NearFarField.Dispose();
		NearCoCBlurY.Dispose();
		NearCoCBlurX.Dispose();
		NearCoCMaxY.Dispose();
		NearCoCMaxX.Dispose();
		Downsample.Dispose();
		CoC.Dispose();
		DOFBlurY.Dispose();
		DOFBlurX.Dispose();
		DOFBlurYBis.Dispose();
		DOFBlurXBis.Dispose();
		BlurSSAOAndShadowTmp.Dispose();
		BlurSSAOAndShadow.Dispose();
		PongSSAO.Dispose();
		PingSSAO.Dispose();
		DeferredShadow.Dispose();
		ShadowMap.Dispose();
		DebugFXOverdraw.Dispose();
		Distortion.Dispose();
		MomentsTransparencyCapture.Dispose();
		TransparencyQuarterRes.Dispose();
		TransparencyHalfRes.Dispose();
		Transparency.Dispose();
		LightBufferHalfRes.Dispose();
		LightBufferFullRes.Dispose();
		SceneColorHalfRes.Dispose();
		PongFinalSceneColor.Dispose();
		PingFinalSceneColor.Dispose();
		PongSceneColor.Dispose();
		PingSceneColor.Dispose();
		GBuffer.Dispose();
		Edges.Dispose();
		LinearZHalfRes.Dispose();
		LinearZ.Dispose();
		HardwareZEighthRes.Dispose();
		HardwareZQuarterRes.Dispose();
		HardwareZHalfRes.Dispose();
		HardwareZ.Dispose();
	}

	public void Resize(int windowWidth, int windowHeight, float renderScale = 1f)
	{
		_viewportSize.X = windowWidth;
		_viewportSize.Y = windowHeight;
		int num = (int)(_viewportSize.X * renderScale);
		int num2 = (int)(_viewportSize.Y * renderScale);
		int width = (int)((float)num * 0.5f);
		int height = (int)((float)num2 * 0.5f);
		int width2 = (int)((float)num * 0.25f);
		int height2 = (int)((float)num2 * 0.25f);
		int width3 = (int)((float)num * 0.125f);
		int height3 = (int)((float)num2 * 0.125f);
		int num3 = (int)((float)num * 0.0625f);
		int height4 = (int)((float)num2 * 0.0625f);
		int width4 = (int)((float)num3 * 0.5f);
		int height5 = (int)((float)num3 * 0.5f);
		HardwareZ.Resize(num, num2);
		HardwareZHalfRes.Resize(width, height);
		HardwareZQuarterRes.Resize(width2, height2);
		HardwareZEighthRes.Resize(width3, height3);
		LinearZ.Resize(num, num2);
		LinearZHalfRes.Resize(width, height);
		Edges.Resize(num, num2);
		GBuffer.Resize(num, num2);
		PingSceneColor.Resize(num, num2);
		PongSceneColor.Resize(num, num2);
		PingFinalSceneColor.Resize(num, num2);
		PongFinalSceneColor.Resize(num, num2);
		SceneColorHalfRes.Resize(width, height);
		LightBufferFullRes.Resize(num, num2);
		LightBufferHalfRes.Resize(width, height);
		Transparency.Resize(num, num2);
		TransparencyHalfRes.Resize(width, height);
		TransparencyQuarterRes.Resize(width2, height2);
		int width5 = (int)((float)num * _momentsTransparencyResolutionScale.X);
		int height6 = (int)((float)num2 * _momentsTransparencyResolutionScale.Y);
		MomentsTransparencyCapture.Resize(width5, height6);
		Distortion.Resize(width, height);
		DebugFXOverdraw.Resize(num, num2);
		int width6 = (int)((float)num * _deferredShadowResolutionScale.X);
		int height7 = (int)((float)num2 * _deferredShadowResolutionScale.Y);
		DeferredShadow.Resize(width6, height7);
		ResizeSSAOBuffers(num, num2, _ssaoResolutionScale);
		SunshaftX.Resize(width2, height2);
		SunshaftY.Resize(width2, height2);
		SunRT.Resize(num, num2);
		VolumetricSunshaft.Resize(width, height);
		BlurXResBy32.Resize(width4, height5);
		BlurYResBy16.Resize(num3, height4);
		BlurXResBy16.Resize(num3, height4);
		BlurYResBy8.Resize(width3, height3);
		BlurXResBy8.Resize(width3, height3);
		BlurYResBy4.Resize(width2, height2);
		BlurXResBy4.Resize(width2, height2);
		BlurXResBy2.Resize(width, height);
		BlurYResBy2.Resize(width, height);
		Fill.Resize(width, height);
		NearFarField.Resize(width, height);
		NearCoCBlurY.Resize(width, height);
		NearCoCBlurX.Resize(width, height);
		NearCoCMaxY.Resize(width, height);
		NearCoCMaxX.Resize(width, height);
		Downsample.Resize(width, height);
		CoC.Resize(num, num2);
		DOFBlurY.Resize(width, height);
		DOFBlurX.Resize(width, height);
		DOFBlurYBis.Resize(width, height);
		DOFBlurXBis.Resize(width, height);
	}

	public void SetMomentsTransparencyResolutionScale(float scale)
	{
		Debug.Assert(scale == 1f || scale == 0.5f || scale == 0.25f || scale == 0.125f, "Unsupported resolution scale for Moments RenderTarget.");
		_momentsTransparencyResolutionScale = new Vector2(scale);
		int width = (int)((float)HardwareZ.Width * _momentsTransparencyResolutionScale.X);
		int height = (int)((float)HardwareZ.Height * _momentsTransparencyResolutionScale.Y);
		GLTexture texture = HardwareZ.GetTexture(RenderTarget.Target.Depth);
		if (scale == 0.5f)
		{
			texture = HardwareZHalfRes.GetTexture(RenderTarget.Target.Depth);
		}
		else if (scale == 0.25f)
		{
			texture = HardwareZQuarterRes.GetTexture(RenderTarget.Target.Depth);
		}
		else if (scale == 0.125f)
		{
			texture = HardwareZEighthRes.GetTexture(RenderTarget.Target.Depth);
		}
		MomentsTransparencyCapture.UseAsRenderTexture(texture, skipDispose: true, RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8);
		MomentsTransparencyCapture.FinalizeSetup();
		MomentsTransparencyCapture.Resize(width, height);
	}

	public void SetDeferredShadowResolutionScale(float scale)
	{
		_deferredShadowResolutionScale = new Vector2(scale);
		int width = (int)((float)GBuffer.Width * _deferredShadowResolutionScale.X);
		int height = (int)((float)GBuffer.Height * _deferredShadowResolutionScale.Y);
		DeferredShadow.Resize(width, height);
	}

	public void ResizeShadowMap(int width, int height)
	{
		ShadowMap.Resize(width, height);
	}

	public void ResizeSSAOBuffers(int gbufferWidth, int gbufferHeight, Vector2 ssaoResolutionScale)
	{
		_ssaoResolutionScale = ssaoResolutionScale;
		int width = (int)((float)gbufferWidth * ssaoResolutionScale.X);
		int height = (int)((float)gbufferHeight * ssaoResolutionScale.Y);
		PingSSAO.Resize(width, height);
		PongSSAO.Resize(width, height);
		BlurSSAOAndShadowTmp.Resize(width, height);
		BlurSSAOAndShadow.Resize(width, height);
	}

	public void BeginFrame()
	{
		PingPongFinalRenderTarget();
		PingPongSSAOTarget();
	}

	private void PingPongFinalRenderTarget()
	{
		if (SceneColor == PingSceneColor)
		{
			PreviousSceneColor = PingSceneColor;
			SceneColor = PongSceneColor;
			FinalSceneColor = PingFinalSceneColor;
			PreviousFinalSceneColor = PongFinalSceneColor;
		}
		else
		{
			PreviousSceneColor = PongSceneColor;
			SceneColor = PingSceneColor;
			FinalSceneColor = PongFinalSceneColor;
			PreviousFinalSceneColor = PingFinalSceneColor;
		}
	}

	private void PingPongSSAOTarget()
	{
		if (SSAORaw == PingSSAO)
		{
			PreviousSSAORaw = PingSSAO;
			SSAORaw = PongSSAO;
		}
		else
		{
			PreviousSSAORaw = PongSSAO;
			SSAORaw = PingSSAO;
		}
	}

	private void InitDebugMapInfos()
	{
		_debugMapInfo.Add("blur", new DebugMapParam(BlurYResBy4, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false));
		_debugMapInfo.Add("scene_color_final", new DebugMapParam(PingFinalSceneColor, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false));
		_debugMapInfo.Add("scene_color_half", new DebugMapParam(SceneColorHalfRes, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false));
		_debugMapInfo.Add("gbuffer0", new DebugMapParam(GBuffer, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false));
		_debugMapInfo.Add("gbuffer_albedo", new DebugMapParam(GBuffer, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.Color, DebugMapParam.ColorChannelBits.RG));
		_debugMapInfo.Add("gbuffer_normal", new DebugMapParam(GBuffer, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false, useNormalQuantization: true, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.BA));
		_debugMapInfo.Add("gbuffer1", new DebugMapParam(GBuffer, RenderTarget.Target.Color2, hasZValues: false, hasLinearZValues: false));
		_debugMapInfo.Add("gbuffer_light", new DebugMapParam(GBuffer, RenderTarget.Target.Color2, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.Light, DebugMapParam.ColorChannelBits.RG));
		_debugMapInfo.Add("gbuffer_sun", new DebugMapParam(GBuffer, RenderTarget.Target.Color2, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.B));
		_debugMapInfo.Add("hw_z", new DebugMapParam(HardwareZ, RenderTarget.Target.Depth, hasZValues: true, hasLinearZValues: false));
		_debugMapInfo.Add("hw_z_half", new DebugMapParam(HardwareZHalfRes, RenderTarget.Target.Depth, hasZValues: true, hasLinearZValues: false));
		_debugMapInfo.Add("hw_z_quarter", new DebugMapParam(HardwareZQuarterRes, RenderTarget.Target.Depth, hasZValues: true, hasLinearZValues: false));
		_debugMapInfo.Add("hw_z_eighth", new DebugMapParam(HardwareZEighthRes, RenderTarget.Target.Depth, hasZValues: true, hasLinearZValues: false));
		_debugMapInfo.Add("linear_z", new DebugMapParam(LinearZ, RenderTarget.Target.Color0, hasZValues: true, hasLinearZValues: true));
		_debugMapInfo.Add("linear_z_half", new DebugMapParam(LinearZHalfRes, RenderTarget.Target.Color0, hasZValues: true, hasLinearZValues: true));
		_debugMapInfo.Add("edges", new DebugMapParam(Edges, RenderTarget.Target.Color0, hasZValues: true, hasLinearZValues: true));
		_debugMapInfo.Add("lbuffer", new DebugMapParam(LightBufferFullRes, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.Light, DebugMapParam.ColorChannelBits.RG));
		_debugMapInfo.Add("lbuffer_low", new DebugMapParam(LightBufferHalfRes, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false));
		_debugMapInfo.Add("blend_moment", new DebugMapParam(MomentsTransparencyCapture, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false));
		_debugMapInfo.Add("blend_tod", new DebugMapParam(MomentsTransparencyCapture, RenderTarget.Target.Color1, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.R));
		_debugMapInfo.Add("blend_accu", new DebugMapParam(Transparency, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false));
		_debugMapInfo.Add("blend_weight", new DebugMapParam(Transparency, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.A));
		_debugMapInfo.Add("blend_reveal", new DebugMapParam(Transparency, RenderTarget.Target.Color1, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.R));
		_debugMapInfo.Add("blend_add", new DebugMapParam(Transparency, RenderTarget.Target.Color1, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.A));
		_debugMapInfo.Add("blend_beta", new DebugMapParam(Transparency, RenderTarget.Target.Color1, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.A));
		_debugMapInfo.Add("blend_accu_lowres", new DebugMapParam(TransparencyHalfRes, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false));
		_debugMapInfo.Add("blend_weight_lowres", new DebugMapParam(TransparencyHalfRes, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.A));
		_debugMapInfo.Add("blend_reveal_lowres", new DebugMapParam(TransparencyHalfRes, RenderTarget.Target.Color1, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.R));
		_debugMapInfo.Add("blend_add_lowres", new DebugMapParam(TransparencyHalfRes, RenderTarget.Target.Color1, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.A));
		_debugMapInfo.Add("blend_beta_lowres", new DebugMapParam(TransparencyHalfRes, RenderTarget.Target.Color1, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.A));
		_debugMapInfo.Add("shadowmap", new DebugMapParam(ShadowMap, RenderTarget.Target.Depth, hasZValues: true, hasLinearZValues: false));
		_debugMapInfo.Add("deferredshadow", new DebugMapParam(DeferredShadow, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.R));
		_debugMapInfo.Add("shadow", new DebugMapParam(BlurSSAOAndShadow, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.A));
		_debugMapInfo.Add("ssao", new DebugMapParam(BlurSSAOAndShadow, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.R));
		_debugMapInfo.Add("ssao_raw", new DebugMapParam(SSAORaw, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.R));
		_debugMapInfo.Add("bloom", new DebugMapParam(BlurXResBy2, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false));
		_debugMapInfo.Add("vol_sunshaft", new DebugMapParam(VolumetricSunshaft, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false));
		_debugMapInfo.Add("sun_occlusion", new DebugMapParam(SunOcclusionBufferLowRes, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false));
		_debugMapInfo.Add("sun_occlusion_history", new DebugMapParam(SunOcclusionHistory, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.RGB, 1f, 0.1f));
		_debugMapInfo.Add("distortion", new DebugMapParam(Distortion, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.RGB, 50f));
		_debugMapInfo.Add("overdraw", new DebugMapParam(DebugFXOverdraw, RenderTarget.Target.Color0, hasZValues: false, hasLinearZValues: false, useNormalQuantization: false, DebugMapParam.ChromaSubsamplingMode.None, DebugMapParam.ColorChannelBits.R, 1f, 1f, 1f, 1f, 10f));
	}

	public void SetDebugMapChromaSubsamplingMode(string mapName, DebugMapParam.ChromaSubsamplingMode chromaSubsamplingMode)
	{
		if (_debugMapInfo.TryGetValue(mapName, out var value))
		{
			value.ChromaSubSamplingMode = chromaSubsamplingMode;
			value.ColorChannels = ((chromaSubsamplingMode != 0) ? DebugMapParam.ColorChannelBits.RG : DebugMapParam.ColorChannelBits.RGB);
			_debugMapInfo[mapName] = value;
		}
	}

	public string GetDebugMapList()
	{
		return string.Format("{0}", string.Join(", ", _debugMapInfo.Keys));
	}

	public bool ContainsDebugMap(string mapName)
	{
		return _debugMapInfo.ContainsKey(mapName);
	}

	public void RegisterDebugMap(string mapName, Texture texture)
	{
		_debugMapInfo.Add(mapName, new DebugMapParam(texture));
	}

	public void RegisterDebugMap2DArray(string mapName, GLTexture texture, int width, int height, int layerCount)
	{
		_debugMapInfo.Add(mapName, new DebugMapParam(texture, width, height, layerCount));
	}

	public void RegisterDebugMapCubemap(string mapName, Texture texture)
	{
		_debugMapInfo.Add(mapName, new DebugMapParam(texture, DebugMapParam.ColorChannelBits.RGB, isACubemap: true));
	}

	public void UnregisterDebugMap(string mapName)
	{
		_debugMapInfo.Remove(mapName);
	}

	public void SetDebugMapViewport(string mapName, float width, float height)
	{
		if (_debugMapInfo.TryGetValue(mapName, out var value))
		{
			value.ViewportScale = new Vector2(width, height);
			_debugMapInfo[mapName] = value;
		}
	}

	public void SetDebugMapScale(string mapName, float scale)
	{
		if (_debugMapInfo.TryGetValue(mapName, out var value))
		{
			value.Scale = scale;
			_debugMapInfo[mapName] = value;
		}
	}

	public void DebugDrawMaps(string[] mapNames, bool verticalDisplay, float opacity = 0f, int mipLevel = 0, int layer = 0)
	{
		GLFunctions gL = _graphics.GL;
		bool flag = false;
		Vector4 viewport = new Vector4(0f, 0f, _viewportSize.X, _viewportSize.Y);
		GLTexture map = GLTexture.None;
		bool debugAsTexture2DArray = false;
		bool flag2 = false;
		int textureWidth = 0;
		int textureHeight = 0;
		for (int i = 0; i < mapNames.Length; i++)
		{
			DebugMapParam debugMapParam = _debugMapInfo[mapNames[i]];
			switch (debugMapParam.InputType)
			{
			case DebugMapParam.DebugMapInputType.Texture2D:
				debugAsTexture2DArray = false;
				flag2 = false;
				map = debugMapParam.Texture2D.GLTexture;
				textureWidth = debugMapParam.Texture2D.Width;
				textureHeight = debugMapParam.Texture2D.Height;
				break;
			case DebugMapParam.DebugMapInputType.RenderTarget:
				debugAsTexture2DArray = false;
				flag2 = false;
				map = debugMapParam.RenderTarget.GetTexture(debugMapParam.Target);
				textureWidth = debugMapParam.RenderTarget.Width;
				textureHeight = debugMapParam.RenderTarget.Height;
				break;
			case DebugMapParam.DebugMapInputType.Texture2DArray:
				debugAsTexture2DArray = true;
				flag2 = false;
				map = debugMapParam.Texture2DArray.Texture;
				textureWidth = debugMapParam.Texture2DArray.Width;
				textureHeight = debugMapParam.Texture2DArray.Height;
				break;
			case DebugMapParam.DebugMapInputType.Cubemap:
				debugAsTexture2DArray = false;
				flag2 = true;
				map = debugMapParam.Texture2D.GLTexture;
				textureWidth = debugMapParam.Texture2D.Width;
				textureHeight = debugMapParam.Texture2D.Height;
				break;
			default:
				Debug.Assert(condition: false, $"Invalid DebugMapInputType {(int)debugMapParam.InputType}");
				break;
			}
			float num = ((mapNames.Length == 1) ? debugMapParam.Scale : 1f);
			Vector2 vector = debugMapParam.ViewportScale * _viewportSize;
			float num2 = 1f;
			if (mapNames.Length > 1 || vector != Vector2.Zero || num != 1f)
			{
				num2 = 1f / (float)mapNames.Length;
				flag = true;
				vector *= num * num2;
				int num3 = ((!verticalDisplay) ? ((int)((float)i * num2 * _viewportSize.X)) : 0);
				int num4 = (verticalDisplay ? ((int)((float)i * num2 * _viewportSize.Y)) : 0);
				viewport = new Vector4(num3, num4, (int)vector.X, (int)vector.Y);
				gL.Viewport(num3, num4, (int)vector.X, (int)vector.Y);
			}
			if (flag2)
			{
				flag = true;
				float num5 = _viewportSize.Y / 3f;
				float num6 = _viewportSize.X / 4f;
				gL.Viewport((int)(2f * num6), (int)num5, (int)num6, (int)num5);
				DebugDrawMap(debugMapParam.HasZValues, debugMapParam.HasLinearZValues, debugAsTexture2DArray, 1, debugMapParam.UseNormalQuantization, debugMapParam.ChromaSubSamplingMode, (int)debugMapParam.ColorChannels, textureWidth, textureHeight, map, opacity, mipLevel, layer, debugMapParam.Multiplier, debugMapParam.DebugMaxOverdraw, viewport);
				gL.Viewport(0, (int)num5, (int)num6, (int)num5);
				DebugDrawMap(debugMapParam.HasZValues, debugMapParam.HasLinearZValues, debugAsTexture2DArray, 2, debugMapParam.UseNormalQuantization, debugMapParam.ChromaSubSamplingMode, (int)debugMapParam.ColorChannels, textureWidth, textureHeight, map, opacity, mipLevel, layer, debugMapParam.Multiplier, debugMapParam.DebugMaxOverdraw, viewport);
				gL.Viewport((int)num6, (int)(2f * num5), (int)num6, (int)num5);
				DebugDrawMap(debugMapParam.HasZValues, debugMapParam.HasLinearZValues, debugAsTexture2DArray, 3, debugMapParam.UseNormalQuantization, debugMapParam.ChromaSubSamplingMode, (int)debugMapParam.ColorChannels, textureWidth, textureHeight, map, opacity, mipLevel, layer, debugMapParam.Multiplier, debugMapParam.DebugMaxOverdraw, viewport);
				gL.Viewport((int)num6, 0, (int)num6, (int)num5);
				DebugDrawMap(debugMapParam.HasZValues, debugMapParam.HasLinearZValues, debugAsTexture2DArray, 4, debugMapParam.UseNormalQuantization, debugMapParam.ChromaSubSamplingMode, (int)debugMapParam.ColorChannels, textureWidth, textureHeight, map, opacity, mipLevel, layer, debugMapParam.Multiplier, debugMapParam.DebugMaxOverdraw, viewport);
				gL.Viewport((int)num6, (int)num5, (int)num6, (int)num5);
				DebugDrawMap(debugMapParam.HasZValues, debugMapParam.HasLinearZValues, debugAsTexture2DArray, 5, debugMapParam.UseNormalQuantization, debugMapParam.ChromaSubSamplingMode, (int)debugMapParam.ColorChannels, textureWidth, textureHeight, map, opacity, mipLevel, layer, debugMapParam.Multiplier, debugMapParam.DebugMaxOverdraw, viewport);
				gL.Viewport((int)(3f * num6), (int)num5, (int)num6, (int)num5);
				DebugDrawMap(debugMapParam.HasZValues, debugMapParam.HasLinearZValues, debugAsTexture2DArray, 6, debugMapParam.UseNormalQuantization, debugMapParam.ChromaSubSamplingMode, (int)debugMapParam.ColorChannels, textureWidth, textureHeight, map, opacity, mipLevel, layer, debugMapParam.Multiplier, debugMapParam.DebugMaxOverdraw, viewport);
			}
			else
			{
				DebugDrawMap(debugMapParam.HasZValues, debugMapParam.HasLinearZValues, debugAsTexture2DArray, 0, debugMapParam.UseNormalQuantization, debugMapParam.ChromaSubSamplingMode, (int)debugMapParam.ColorChannels, textureWidth, textureHeight, map, opacity, mipLevel, layer, debugMapParam.Multiplier, debugMapParam.DebugMaxOverdraw, viewport);
			}
		}
		if (flag)
		{
			gL.Viewport(0, 0, (int)_viewportSize.X, (int)_viewportSize.Y);
		}
	}

	public void DebugDrawMap(bool debugAsZMap, bool debugAsLinearZ, bool debugAsTexture2DArray, int cubemapFaceID, bool useNormalQuantization, DebugMapParam.ChromaSubsamplingMode chromaSubsamplingMode, int colorChannels, int textureWidth, int textureHeight, GLTexture map, float opacity, int mipLevel, int layer, float multiplier, float debugMaxOverdraw, Vector4 viewport)
	{
		GLFunctions gL = _graphics.GL;
		DebugDrawMapProgram debugDrawMapProgram = _graphics.GPUProgramStore.DebugDrawMapProgram;
		gL.UseProgram(debugDrawMapProgram);
		if (debugAsTexture2DArray)
		{
			gL.ActiveTexture(GL.TEXTURE1);
			gL.BindTexture(GL.TEXTURE_2D_ARRAY, map);
			gL.ActiveTexture(GL.TEXTURE0);
		}
		else if (cubemapFaceID != 0)
		{
			gL.ActiveTexture(GL.TEXTURE2);
			gL.BindTexture(GL.TEXTURE_CUBE_MAP, map);
			gL.ActiveTexture(GL.TEXTURE0);
		}
		else
		{
			gL.ActiveTexture(GL.TEXTURE0);
			gL.BindTexture(GL.TEXTURE_2D, map);
		}
		int value = (debugAsLinearZ ? 1 : 0);
		int value2 = ((debugAsLinearZ || debugAsZMap) ? 1 : 0);
		int value3 = (debugAsTexture2DArray ? 1 : 0);
		debugDrawMapProgram.DebugZ.SetValue(value2);
		debugDrawMapProgram.LinearZ.SetValue(value);
		debugDrawMapProgram.DebugTexture2DArray.SetValue(value3);
		debugDrawMapProgram.CubemapFace.SetValue(cubemapFaceID);
		debugDrawMapProgram.NormalQuantization.SetValue(useNormalQuantization ? 1 : 0);
		debugDrawMapProgram.ChromaSubsampling.SetValue((int)chromaSubsamplingMode);
		debugDrawMapProgram.ColorChannels.SetValue(colorChannels);
		debugDrawMapProgram.Opacity.SetValue(opacity);
		debugDrawMapProgram.MipLevel.SetValue(mipLevel);
		debugDrawMapProgram.Layer.SetValue(layer);
		debugDrawMapProgram.Multiplier.SetValue(multiplier);
		debugDrawMapProgram.DebugMaxOverdraw.SetValue(debugMaxOverdraw);
		debugDrawMapProgram.TextureSize.SetValue((float)textureWidth, (float)textureHeight);
		viewport = ((viewport == Vector4.One) ? new Vector4(0f, 0f, _viewportSize.X, _viewportSize.Y) : viewport);
		debugDrawMapProgram.Viewport.SetValue(viewport);
		_graphics.ScreenTriangleRenderer.Draw();
	}
}
