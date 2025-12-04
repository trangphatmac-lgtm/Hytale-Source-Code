#define DEBUG
using System.Diagnostics;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics;

internal class OrderIndependentTransparency
{
	public enum Method
	{
		None,
		WBOIT,
		WBOITExt,
		POIT,
		MOIT
	}

	public enum ResolutionScale
	{
		Full,
		Half,
		Quarter,
		Eighth,
		End
	}

	private int _profileOITPrepass;

	private int _profileOITAccumulateQuarterRes;

	private int _profileOITAccumulateHalfRes;

	private int _profileOITAccumulateFullRes;

	private int _profileOITComposite;

	private readonly Profiling _profiling;

	private readonly RenderTargetStore _renderTargetStore;

	private readonly GraphicsDevice _graphics;

	private readonly GLFunctions _gl;

	private Method _method;

	private ResolutionScale _prepassResolutionScale;

	private byte _moitMomentUnit;

	private byte _moitTotalOpticalDepthUnit;

	private DrawTransparencyFunc _drawTransparentsFullRes;

	private DrawTransparencyFunc _drawTransparentsHalfRes;

	private DrawTransparencyFunc _drawTransparentsQuarterRes;

	private byte _edgesStencilBit;

	private bool _drawHalfResEdgeFixup = false;

	private bool _drawQuarterResEdgeFixup = false;

	private bool _useFallback;

	public Method CurrentMethod => _method;

	public bool HasFullResPass => _drawTransparentsFullRes != null;

	public bool HasHalfResPass => _drawTransparentsHalfRes != null;

	public bool HasQuarterResPass => _drawTransparentsQuarterRes != null;

	public bool NeedsZBufferHalfRes => (_method == Method.MOIT && _prepassResolutionScale >= ResolutionScale.Half) || HasHalfResPass;

	public bool NeedsZBufferQuarterRes => (_method == Method.MOIT && _prepassResolutionScale >= ResolutionScale.Quarter) || HasQuarterResPass;

	public bool NeedsZBufferEighthRes => _method == Method.MOIT && _prepassResolutionScale >= ResolutionScale.Eighth;

	public void UseEdgeFixupPass(bool fixupHalfRes, bool fixupQuarterRes, byte edgesStencilBit)
	{
		Debug.Assert(edgesStencilBit < 8, $"Invalid stencil bit id requested for Edges: {edgesStencilBit}. Valide entries are[0-7].");
		_drawHalfResEdgeFixup = fixupHalfRes;
		_drawQuarterResEdgeFixup = fixupQuarterRes;
		_edgesStencilBit = edgesStencilBit;
	}

	public OrderIndependentTransparency(GraphicsDevice graphics, RenderTargetStore renderTargetStore, Profiling profiling)
	{
		_graphics = graphics;
		_gl = _graphics.GL;
		_renderTargetStore = renderTargetStore;
		_profiling = profiling;
		_useFallback = !_graphics.SupportsDrawBuffersBlend;
	}

	public void Init()
	{
	}

	public void Dispose()
	{
	}

	public void SetupRenderingProfiles(int profileOITPrepass, int profileOITAccumulateQuarterRes, int profileOITAccumulateHalfRes, int profileOITAccumulateFullRes, int profileOITComposite)
	{
		_profileOITPrepass = profileOITPrepass;
		_profileOITAccumulateQuarterRes = profileOITAccumulateQuarterRes;
		_profileOITAccumulateHalfRes = profileOITAccumulateHalfRes;
		_profileOITAccumulateFullRes = profileOITAccumulateFullRes;
		_profileOITComposite = profileOITComposite;
	}

	public void SetMethod(Method method)
	{
		_method = method;
	}

	public void SetPrepassResolutionScale(ResolutionScale scale)
	{
		Debug.Assert(scale < ResolutionScale.End, "Unsupported OIT prepass ResolutionScale.");
		if (scale != _prepassResolutionScale)
		{
			_prepassResolutionScale = scale;
			float num = 1f;
			switch (scale)
			{
			case ResolutionScale.Full:
				num /= 1f;
				break;
			case ResolutionScale.Half:
				num /= 2f;
				break;
			case ResolutionScale.Quarter:
				num /= 4f;
				break;
			case ResolutionScale.Eighth:
				num /= 8f;
				break;
			default:
				Debug.Assert(condition: false, "OITPrepass setup error.");
				break;
			}
			_renderTargetStore.SetMomentsTransparencyResolutionScale(num);
		}
	}

	public void RegisterDrawTransparentsFunc(DrawTransparencyFunc drawFullResFunc, DrawTransparencyFunc drawHalfResFunc, DrawTransparencyFunc drawQuarterResFunc)
	{
		_drawTransparentsFullRes = drawFullResFunc;
		_drawTransparentsHalfRes = drawHalfResFunc;
		_drawTransparentsQuarterRes = drawQuarterResFunc;
	}

	public void SetupTextureUnits(byte moitMomentUnit, byte moitTotalOpticalDepthUnit)
	{
		Debug.Assert(moitMomentUnit != moitTotalOpticalDepthUnit);
		_moitMomentUnit = moitMomentUnit;
		_moitTotalOpticalDepthUnit = moitTotalOpticalDepthUnit;
	}

	public void SkipInternalMeasures()
	{
		_profiling.SkipMeasure(_profileOITPrepass);
		_profiling.SkipMeasure(_profileOITAccumulateQuarterRes);
		_profiling.SkipMeasure(_profileOITAccumulateHalfRes);
		_profiling.SkipMeasure(_profileOITAccumulateFullRes);
		_profiling.SkipMeasure(_profileOITComposite);
	}

	public void Draw(bool hasFullResItems = true, bool hasHalfResItems = true, bool hasQuarterResItems = true)
	{
		Debug.Assert(_drawTransparentsFullRes != null || _drawTransparentsHalfRes != null || _drawTransparentsQuarterRes != null, "No TransparencyFunc was defined. Make sure you call RegisterDrawTransparentsFunc.");
		if (_method == Method.None || (hasFullResItems && hasHalfResItems && hasQuarterResItems))
		{
			SkipInternalMeasures();
			return;
		}
		_graphics.SaveColorMask();
		_gl.ColorMask(red: true, green: true, blue: true, alpha: true);
		_gl.BlendEquation(GL.FUNC_ADD);
		bool flag = _drawQuarterResEdgeFixup && HasQuarterResPass;
		bool flag2 = _drawHalfResEdgeFixup && HasHalfResPass;
		bool flag3 = hasQuarterResItems && HasQuarterResPass;
		bool flag4 = hasHalfResItems && HasHalfResPass;
		bool flag5 = (hasFullResItems && HasFullResPass) || flag2 || flag;
		if (_method == Method.MOIT)
		{
			_profiling.StartMeasure(_profileOITPrepass);
			DrawPrepass(_drawTransparentsFullRes, _drawTransparentsHalfRes, _drawTransparentsQuarterRes);
			_profiling.StopMeasure(_profileOITPrepass);
		}
		else
		{
			_profiling.SkipMeasure(_profileOITPrepass);
		}
		if (flag3)
		{
			_profiling.StartMeasure(_profileOITAccumulateQuarterRes);
			DrawAccumulation(_renderTargetStore.TransparencyQuarterRes, _drawTransparentsQuarterRes);
			_profiling.StopMeasure(_profileOITAccumulateQuarterRes);
		}
		else
		{
			_profiling.SkipMeasure(_profileOITAccumulateQuarterRes);
		}
		if (flag4)
		{
			_profiling.StartMeasure(_profileOITAccumulateHalfRes);
			DrawAccumulation(_renderTargetStore.TransparencyHalfRes, _drawTransparentsHalfRes);
			_profiling.StopMeasure(_profileOITAccumulateHalfRes);
		}
		else
		{
			_profiling.SkipMeasure(_profileOITAccumulateHalfRes);
		}
		if (flag5)
		{
			DrawTransparencyFunc drawFixupFunc = (flag2 ? _drawTransparentsHalfRes : null);
			DrawTransparencyFunc drawFixupFunc2 = (flag ? _drawTransparentsQuarterRes : null);
			_profiling.StartMeasure(_profileOITAccumulateFullRes);
			DrawAccumulation(_renderTargetStore.Transparency, _drawTransparentsFullRes, drawFixupFunc, drawFixupFunc2);
			_profiling.StopMeasure(_profileOITAccumulateFullRes);
		}
		else
		{
			_profiling.SkipMeasure(_profileOITAccumulateFullRes);
		}
		_graphics.RestoreColorMask();
		_profiling.StartMeasure(_profileOITComposite);
		DrawComposite(flag5, flag4, flag3);
		_profiling.StopMeasure(_profileOITComposite);
	}

	private void DrawPrepass(DrawTransparencyFunc drawFullResFunc, DrawTransparencyFunc drawHalfResFunc, DrawTransparencyFunc drawQuarterResFunc)
	{
		Debug.Assert(drawFullResFunc != null || drawHalfResFunc != null || drawQuarterResFunc != null);
		Debug.Assert(_method == Method.MOIT, "OIT Prepass is only required for MOIT.");
		bool setupViewport = _prepassResolutionScale != ResolutionScale.Full;
		_renderTargetStore.MomentsTransparencyCapture.Bind(clear: false, setupViewport);
		float[] data = new float[4];
		float[] array = new float[4] { 1f, 1f, 1f, 1f };
		_gl.ClearBufferfv(GL.COLOR, 0, data);
		_gl.ClearBufferfv(GL.COLOR, 1, data);
		_gl.BlendFunci(0u, GL.ONE, GL.ONE);
		_gl.BlendFunci(1u, GL.ONE, GL.ONE);
		drawFullResFunc?.Invoke((int)_method, 0, _renderTargetStore.MomentsTransparencyCapture.InvResolution, sendDataToGPU: true);
		drawHalfResFunc?.Invoke((int)_method, 0, _renderTargetStore.MomentsTransparencyCapture.InvResolution, sendDataToGPU: true);
		drawQuarterResFunc?.Invoke((int)_method, 0, _renderTargetStore.MomentsTransparencyCapture.InvResolution, sendDataToGPU: true);
		_renderTargetStore.MomentsTransparencyCapture.Unbind();
	}

	private void DrawTransparentGeometry(bool sendDataToGPU, int oitMethod, int extra, Vector2 invViewportSize, DrawTransparencyFunc drawFunc, DrawTransparencyFunc drawFixupFunc1 = null, DrawTransparencyFunc drawFixupFunc2 = null)
	{
		drawFunc?.Invoke(oitMethod, extra, invViewportSize, sendDataToGPU);
		if (drawFixupFunc1 != null || drawFixupFunc2 != null)
		{
			_gl.Enable(GL.STENCIL_TEST);
			_gl.StencilMask(0u);
			_gl.StencilOp(GL.KEEP, GL.KEEP, GL.KEEP);
			_gl.StencilFunc(GL.EQUAL, 1 << (int)_edgesStencilBit, (uint)(1 << (int)_edgesStencilBit));
			drawFixupFunc1?.Invoke(oitMethod, extra, invViewportSize, sendDataToGPU: false);
			drawFixupFunc2?.Invoke(oitMethod, extra, invViewportSize, sendDataToGPU: false);
			_gl.Disable(GL.STENCIL_TEST);
		}
	}

	private void DrawAccumulation(RenderTarget transparencyRenderTarget, DrawTransparencyFunc drawFunc, DrawTransparencyFunc drawFixupFunc1 = null, DrawTransparencyFunc drawFixupFunc2 = null)
	{
		Debug.Assert(drawFunc != null || drawFixupFunc1 != null || drawFixupFunc2 != null);
		Debug.Assert(transparencyRenderTarget != null);
		Debug.Assert(_moitMomentUnit != _moitTotalOpticalDepthUnit || _method != Method.MOIT, "Invalid TextureUnit used for MOIT.");
		if (_method == Method.MOIT)
		{
			_gl.ActiveTexture((GL)(33984 + _moitMomentUnit));
			_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.MomentsTransparencyCapture.GetTexture(RenderTarget.Target.Color0));
			_gl.ActiveTexture((GL)(33984 + _moitTotalOpticalDepthUnit));
			_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.MomentsTransparencyCapture.GetTexture(RenderTarget.Target.Color1));
		}
		transparencyRenderTarget.Bind(clear: false, setupViewport: true);
		float[] data = new float[4];
		float[] data2 = new float[4] { 1f, 1f, 1f, 0f };
		_gl.ClearBufferfv(GL.COLOR, 0, data);
		_gl.ClearBufferfv(GL.COLOR, 1, data2);
		Method oitMethod = ((_method == Method.MOIT) ? (_method + 1) : _method);
		bool sendDataToGPU = _method != Method.MOIT;
		GL sfactor = GL.ONE;
		GL dfactor = GL.ONE;
		GL srcRGB = GL.NO_ERROR;
		GL dstRGB = GL.ONE_MINUS_SRC_COLOR;
		GL srcAlpha = GL.ONE;
		GL dstAlpha = GL.ONE;
		if (!_useFallback)
		{
			_gl.BlendFunci(0u, sfactor, dfactor);
			_gl.BlendFuncSeparatei(1u, srcRGB, dstRGB, srcAlpha, dstAlpha);
			DrawTransparentGeometry(sendDataToGPU, (int)oitMethod, 0, transparencyRenderTarget.InvResolution, drawFunc, drawFixupFunc1, drawFixupFunc2);
		}
		else
		{
			_gl.DrawBuffer(GL.COLOR_ATTACHMENT0);
			_gl.BlendFunc(sfactor, dfactor);
			DrawTransparentGeometry(sendDataToGPU, (int)oitMethod, 0, transparencyRenderTarget.InvResolution, drawFunc, drawFixupFunc1, drawFixupFunc2);
			_gl.DrawBuffer(GL.COLOR_ATTACHMENT1);
			_gl.BlendFuncSeparate(srcRGB, dstRGB, srcAlpha, dstAlpha);
			DrawTransparentGeometry(sendDataToGPU, (int)oitMethod, 1, transparencyRenderTarget.InvResolution, drawFunc, drawFixupFunc1, drawFixupFunc2);
			transparencyRenderTarget.SetupDrawBuffers();
		}
		transparencyRenderTarget.Unbind();
	}

	private void DrawComposite(bool needsFullResPass, bool needsHalfResPass, bool needsQuarterResPass)
	{
		_gl.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
		bool setupViewport = !needsFullResPass && (needsHalfResPass || needsQuarterResPass);
		_renderTargetStore.SceneColor.Bind(clear: false, setupViewport);
		if (_method == Method.POIT)
		{
			_gl.Disable(GL.BLEND);
			_gl.ActiveTexture(GL.TEXTURE2);
			_gl.BindSampler(2u, GLSampler.None);
			_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.FinalSceneColor.GetTexture(RenderTarget.Target.Color0));
		}
		_gl.ActiveTexture(GL.TEXTURE6);
		_gl.BindSampler(6u, GLSampler.None);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.TransparencyQuarterRes.GetTexture(RenderTarget.Target.Color0));
		_gl.ActiveTexture(GL.TEXTURE5);
		_gl.BindSampler(5u, GLSampler.None);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.TransparencyQuarterRes.GetTexture(RenderTarget.Target.Color1));
		_gl.ActiveTexture(GL.TEXTURE4);
		_gl.BindSampler(4u, GLSampler.None);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.TransparencyHalfRes.GetTexture(RenderTarget.Target.Color0));
		_gl.ActiveTexture(GL.TEXTURE3);
		_gl.BindSampler(3u, GLSampler.None);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.TransparencyHalfRes.GetTexture(RenderTarget.Target.Color1));
		_gl.ActiveTexture(GL.TEXTURE1);
		_gl.BindSampler(1u, GLSampler.None);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.Transparency.GetTexture(RenderTarget.Target.Color1));
		_gl.ActiveTexture(GL.TEXTURE0);
		_gl.BindSampler(0u, GLSampler.None);
		_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.Transparency.GetTexture(RenderTarget.Target.Color0));
		OITCompositeProgram oITCompositeProgram = _graphics.GPUProgramStore.OITCompositeProgram;
		_gl.UseProgram(oITCompositeProgram);
		oITCompositeProgram.OITMethod.SetValue((int)_method);
		Vector4 value = default(Vector4);
		value.X = (needsFullResPass ? 1 : 0);
		value.Y = (needsHalfResPass ? 1 : 0);
		value.Z = (needsQuarterResPass ? 1 : 0);
		value.W = 0f;
		oITCompositeProgram.InputResolutionUsed.SetValue(value);
		_graphics.ScreenTriangleRenderer.Draw();
		if (_method == Method.POIT)
		{
			_gl.Enable(GL.BLEND);
		}
	}
}
