#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics;

internal class PostEffectRenderer
{
	private struct DepthOfFieldDrawParams
	{
		public float NearBlurMax;

		public float NearBlurry;

		public float NearSharp;

		public float FarSharp;

		public float FarBlurry;

		public float FarBlurMax;

		public Matrix ProjectionMatrix;
	}

	private struct BloomDrawParams
	{
		public float GlobalIntensity;

		public float Power;

		public float SunshaftIntensity;

		public float SunshaftScale;

		public float SunMoonIntensity;

		public int ApplyBloom;

		public float[] Intensities;

		public float PowIntensity;

		public float PowPower;

		public Matrix SunMVP;

		public bool IsSunVisible;

		public bool IsMoonVisible;

		public bool isBloomAllowed;

		public Vector3 SunColor;

		public Vector4 MoonColor;

		public float Time;
	}

	private struct DrawParams
	{
		public float Time;

		public float DistortionAmplitude;

		public float DistortionFrequency;

		public float ColorBrightness;

		public float ColorContrast;

		public float ColorSaturation;

		public float VolumetricSunshaftStrength;

		public Vector3 ColorFilter;

		public Vector2 DebugTileResolution;

		public DepthOfFieldDrawParams DepthOfFieldParams;

		public BloomDrawParams BloomParams;

		public void InitDefault()
		{
			Time = 0f;
			DistortionFrequency = 0f;
			DistortionAmplitude = 0f;
			ColorBrightness = 0f;
			ColorContrast = 1f;
			ColorSaturation = 1f;
			ColorFilter = new Vector3(1f);
			DebugTileResolution = new Vector2(0f);
			BloomParams.SunColor = new Vector3(-1f);
			VolumetricSunshaftStrength = 2f;
		}
	}

	private struct Settings
	{
		public bool UseDepthOfField;

		public bool UseBloom;

		public bool UseTemporalAA;

		public bool UseFXAAA;

		public bool UseSharpenPostEffect;

		public bool RequestScreenBlur;

		public BloomSettings BloomSettings;

		public DepthOfFieldSettings DoFSettings;

		public BlurredScreenSettings BlurredScreenSettings;

		public void InitDefault()
		{
			UseFXAAA = true;
			UseSharpenPostEffect = true;
			BloomSettings.DrawSun = null;
			BlurredScreenSettings.ScreenBlurStrength = 2;
			BlurredScreenSettings.ScreenBlurScale = 1f;
		}
	}

	private struct BloomSettings
	{
		public int Version;

		public bool UseSun;

		public bool UseMoon;

		public bool UseSunshaft;

		public bool UseFullbright;

		public bool UsePow;

		public int DownsampleMethod;

		public int UpsampleMethod;

		public GLTexture GlowMask;

		public Action DrawSun;

		public GLTexture SunTexture;

		public Action DrawMoon;

		public GLTexture MoonTexture;
	}

	private struct DepthOfFieldSettings
	{
		public int Version;
	}

	private struct BlurredScreenSettings
	{
		public int ScreenBlurStrength;

		public float ScreenBlurScale;
	}

	private bool _hasCameraMoved;

	private DrawParams _postEffectDrawParameters;

	private Settings _postEffectSettings;

	private RenderTarget _outputRenderTarget;

	private GLTexture _input;

	private GLTexture _inputFX;

	private GLTexture _depthInput;

	private int _width;

	private int _height;

	private float _renderScale;

	private readonly GraphicsDevice _graphics;

	private readonly GPUProgramStore _gpuProgramStore;

	private GLFunctions _gl;

	private GLSampler _linearSampler;

	private PostEffectProgram _postEffectProgram;

	private Profiling _profiling;

	private int _renderingProfileDepthOfField;

	private int _renderingProfileBloom;

	private int _renderingProfileCombineAndFxaa;

	private int _renderingProfileBlur;

	private int _renderingProfileTaa;

	public bool NeedsJittering => _postEffectSettings.UseTemporalAA && !NeedsScreenBlur;

	public bool IsBloomEnabled => _postEffectSettings.UseBloom;

	public bool IsTemporalAAEnabled => _postEffectSettings.UseTemporalAA;

	public bool IsDepthOfFieldEnabled => _postEffectSettings.UseDepthOfField;

	public bool IsDistortionEnabled => _postEffectProgram.UseDistortion;

	private bool NeedsScreenBlur => _postEffectSettings.RequestScreenBlur && _postEffectSettings.BlurredScreenSettings.ScreenBlurStrength > 0;

	public PostEffectRenderer(GraphicsDevice graphics, Profiling profiling, PostEffectProgram program)
	{
		_graphics = graphics;
		_gpuProgramStore = graphics.GPUProgramStore;
		_gl = _graphics.GL;
		_profiling = profiling;
		_postEffectProgram = program;
		InitSampler();
		_postEffectSettings = default(Settings);
		_postEffectSettings.InitDefault();
		_postEffectDrawParameters = default(DrawParams);
		_postEffectDrawParameters.InitDefault();
		SetupDepthOfField();
	}

	public void Dispose()
	{
		DisposeSampler();
	}

	private void InitSampler()
	{
		_linearSampler = _gl.GenSampler();
		_gl.SamplerParameteri(_linearSampler, GL.TEXTURE_WRAP_S, GL.CLAMP_TO_EDGE);
		_gl.SamplerParameteri(_linearSampler, GL.TEXTURE_WRAP_T, GL.CLAMP_TO_EDGE);
		_gl.SamplerParameteri(_linearSampler, GL.TEXTURE_MIN_FILTER, GL.LINEAR);
		_gl.SamplerParameteri(_linearSampler, GL.TEXTURE_MAG_FILTER, GL.LINEAR);
	}

	private void DisposeSampler()
	{
		_gl.DeleteSampler(_linearSampler);
	}

	public void Resize(int width, int height, float renderScale)
	{
		_width = width;
		_height = height;
		_renderScale = renderScale;
	}

	public void SetupRenderingProfiles(int renderingProfileDepthOfField, int renderingProfileBloom, int renderingProfileCombineAndFxaa, int renderingProfileTaa, int renderingProfileBlur)
	{
		_renderingProfileDepthOfField = renderingProfileDepthOfField;
		_renderingProfileBloom = renderingProfileBloom;
		_renderingProfileCombineAndFxaa = renderingProfileCombineAndFxaa;
		_renderingProfileTaa = renderingProfileTaa;
		_renderingProfileBlur = renderingProfileBlur;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetInputs(GLTexture input, GLTexture inputFX, int width, int height, float renderScale)
	{
		_input = input;
		_inputFX = inputFX;
		Resize(width, height, renderScale);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetOutput(RenderTarget output)
	{
		_outputRenderTarget = output;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void BindOutputFramebuffer()
	{
		if (_outputRenderTarget != null)
		{
			_outputRenderTarget.Bind(clear: true, setupViewport: true);
		}
		else
		{
			RenderTarget.BindHardwareFramebuffer();
		}
	}

	public void Draw(GLTexture input, GLTexture inputFX, int width, int height, float renderScale, RenderTarget output = null)
	{
		SetInputs(input, inputFX, width, height, renderScale);
		SetOutput(output);
		if (_input == GLTexture.None)
		{
			throw new Exception("RenderTarget was never specified! you are missing a call to SetInputs");
		}
		_gl.AssertDisabled(GL.BLEND);
		_gl.AssertActiveTexture(GL.TEXTURE0);
		if (_postEffectSettings.UseBloom)
		{
			if (_postEffectSettings.BloomSettings.UseSun && _postEffectSettings.BloomSettings.DrawSun == null)
			{
				throw new Exception("Bloom : Action DrawSun was never specified! you are missing a call to InitBloom");
			}
			if (_postEffectSettings.BloomSettings.UseMoon && _postEffectSettings.BloomSettings.DrawMoon == null)
			{
				throw new Exception("Bloom : Action DrawSun was never specified! you are missing a call to InitBloom");
			}
			if (_postEffectSettings.BloomSettings.UseSun && _postEffectDrawParameters.BloomParams.SunColor == new Vector3(-1f))
			{
				throw new Exception("Bloom : SunMVP was never specified! you are missing a call to UpdateBloomParameters");
			}
			_profiling.StartMeasure(_renderingProfileBloom);
			DrawBloom();
			_profiling.StopMeasure(_renderingProfileBloom);
		}
		else
		{
			_profiling.SkipMeasure(_renderingProfileBloom);
		}
		if (_postEffectSettings.UseDepthOfField)
		{
			if (_postEffectDrawParameters.DepthOfFieldParams.ProjectionMatrix == Matrix.Identity)
			{
				throw new Exception("Depth of field : Projection Matrix was not updated ! you are missing a call to UpdateDepthOfFieldParameters");
			}
			if (_depthInput == GLTexture.None)
			{
				throw new Exception("Depth of field : DepthTexture was never set, you are missing a call to UpdateDepthOfFieldParameters");
			}
			_profiling.StartMeasure(_renderingProfileDepthOfField);
			DrawDepthOfField();
			_profiling.StopMeasure(_renderingProfileDepthOfField);
		}
		else
		{
			_profiling.SkipMeasure(_renderingProfileDepthOfField);
		}
		RenderTargetStore rTStore = _graphics.RTStore;
		_profiling.StartMeasure(_renderingProfileCombineAndFxaa);
		if (_postEffectSettings.UseBloom || _postEffectSettings.UseDepthOfField)
		{
			_gl.Viewport(0, 0, _width, _height);
		}
		if (NeedsScreenBlur || _postEffectSettings.UseTemporalAA)
		{
			rTStore.FinalSceneColor.Bind(clear: false, setupViewport: true);
		}
		else
		{
			BindOutputFramebuffer();
		}
		_graphics.GL.UseProgram(_postEffectProgram);
		if (_postEffectProgram.UseVolumetricSunshaft)
		{
			_gl.ActiveTexture(GL.TEXTURE9);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.VolumetricSunshaft.GetTexture(RenderTarget.Target.Color0));
		}
		if (_postEffectSettings.UseBloom)
		{
			_gl.ActiveTexture(GL.TEXTURE7);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurXResBy2.GetTexture(RenderTarget.Target.Color0));
			_postEffectProgram.ApplyBloom.SetValue(_postEffectDrawParameters.BloomParams.ApplyBloom);
		}
		if (_postEffectProgram.UseDepthOfField)
		{
			if (_postEffectProgram.DepthOfFieldVersion == 0)
			{
				_postEffectProgram.NearBlurMax.SetValue(_postEffectDrawParameters.DepthOfFieldParams.NearBlurMax);
				_postEffectProgram.FarBlurMax.SetValue(_postEffectDrawParameters.DepthOfFieldParams.FarBlurMax);
				_postEffectProgram.NearBlurry.SetValue(_postEffectDrawParameters.DepthOfFieldParams.NearBlurry);
				_postEffectProgram.NearSharp.SetValue(_postEffectDrawParameters.DepthOfFieldParams.NearSharp);
				_postEffectProgram.FarSharp.SetValue(_postEffectDrawParameters.DepthOfFieldParams.FarSharp);
				_postEffectProgram.FarBlurry.SetValue(_postEffectDrawParameters.DepthOfFieldParams.FarBlurry);
				_postEffectProgram.ProjectionMatrix.SetValue(ref _postEffectDrawParameters.DepthOfFieldParams.ProjectionMatrix);
				_gl.ActiveTexture(GL.TEXTURE1);
				_gl.BindTexture(GL.TEXTURE_2D, _depthInput);
			}
			else if (_postEffectProgram.DepthOfFieldVersion == 1)
			{
				_postEffectProgram.NearBlurry.SetValue(_postEffectDrawParameters.DepthOfFieldParams.NearBlurry);
				_postEffectProgram.NearSharp.SetValue(_postEffectDrawParameters.DepthOfFieldParams.NearSharp);
				_postEffectProgram.FarSharp.SetValue(_postEffectDrawParameters.DepthOfFieldParams.FarSharp);
				_postEffectProgram.FarBlurry.SetValue(_postEffectDrawParameters.DepthOfFieldParams.FarBlurry);
				_postEffectProgram.ProjectionMatrix.SetValue(ref _postEffectDrawParameters.DepthOfFieldParams.ProjectionMatrix);
				_gl.ActiveTexture(GL.TEXTURE2);
				_gl.BindTexture(GL.TEXTURE_2D, rTStore.DOFBlurY.GetTexture(RenderTarget.Target.Color0));
				_gl.ActiveTexture(GL.TEXTURE1);
				_gl.BindTexture(GL.TEXTURE_2D, _depthInput);
			}
			else if (_postEffectProgram.DepthOfFieldVersion == 2)
			{
				_postEffectProgram.NearBlurry.SetValue(_postEffectDrawParameters.DepthOfFieldParams.NearBlurry);
				_postEffectProgram.NearSharp.SetValue(_postEffectDrawParameters.DepthOfFieldParams.NearSharp);
				_postEffectProgram.FarSharp.SetValue(_postEffectDrawParameters.DepthOfFieldParams.FarSharp);
				_postEffectProgram.FarBlurry.SetValue(_postEffectDrawParameters.DepthOfFieldParams.FarBlurry);
				_postEffectProgram.ProjectionMatrix.SetValue(ref _postEffectDrawParameters.DepthOfFieldParams.ProjectionMatrix);
				_gl.ActiveTexture(GL.TEXTURE3);
				_gl.BindTexture(GL.TEXTURE_2D, rTStore.DOFBlurYBis.GetTexture(RenderTarget.Target.Color1));
				_gl.ActiveTexture(GL.TEXTURE2);
				_gl.BindTexture(GL.TEXTURE_2D, rTStore.DOFBlurYBis.GetTexture(RenderTarget.Target.Color0));
				_gl.ActiveTexture(GL.TEXTURE1);
				_gl.BindTexture(GL.TEXTURE_2D, _depthInput);
			}
			else if (_postEffectProgram.DepthOfFieldVersion == 3)
			{
				_gl.ActiveTexture(GL.TEXTURE6);
				_gl.BindTexture(GL.TEXTURE_2D, _input);
				_gl.ActiveTexture(GL.TEXTURE5);
				_gl.BindTexture(GL.TEXTURE_2D, rTStore.NearFarField.GetTexture(RenderTarget.Target.Color1));
				_gl.ActiveTexture(GL.TEXTURE4);
				_gl.BindTexture(GL.TEXTURE_2D, rTStore.NearFarField.GetTexture(RenderTarget.Target.Color0));
				_gl.ActiveTexture(GL.TEXTURE3);
				_gl.BindTexture(GL.TEXTURE_2D, rTStore.NearCoCBlurY.GetTexture(RenderTarget.Target.Color0));
				_gl.ActiveTexture(GL.TEXTURE2);
				_gl.BindTexture(GL.TEXTURE_2D, rTStore.Downsample.GetTexture(RenderTarget.Target.Color2));
				_gl.ActiveTexture(GL.TEXTURE1);
				_gl.BindTexture(GL.TEXTURE_2D, rTStore.CoC.GetTexture(RenderTarget.Target.Color0));
			}
		}
		_gl.ActiveTexture(GL.TEXTURE8);
		_gl.BindTexture(GL.TEXTURE_2D, _inputFX);
		_gl.ActiveTexture(GL.TEXTURE0);
		_gl.BindSampler(0u, _linearSampler);
		_gl.BindTexture(GL.TEXTURE_2D, _input);
		float x = 1f / (float)_width;
		float y = 1f / (float)_height;
		_postEffectProgram.PixelSize.SetValue(x, y);
		_postEffectProgram.Time.SetValue(_postEffectDrawParameters.Time);
		_postEffectProgram.DistortionAmplitude.SetValue(_postEffectDrawParameters.DistortionAmplitude);
		_postEffectProgram.DistortionFrequency.SetValue(_postEffectDrawParameters.DistortionFrequency);
		_postEffectProgram.ColorBrightnessContrast.SetValue(_postEffectDrawParameters.ColorBrightness, _postEffectDrawParameters.ColorContrast);
		_postEffectProgram.ColorSaturation.SetValue(_postEffectDrawParameters.ColorSaturation);
		_postEffectProgram.ColorFilter.SetValue(_postEffectDrawParameters.ColorFilter);
		_postEffectProgram.VolumetricSunshaftStrength.SetValue(_postEffectDrawParameters.VolumetricSunshaftStrength);
		if (_postEffectProgram.DebugTiles)
		{
			_postEffectProgram.DebugTileResolution.SetValue(_postEffectDrawParameters.DebugTileResolution);
		}
		_graphics.ScreenTriangleRenderer.Draw();
		_gl.BindSampler(0u, GLSampler.None);
		_profiling.StopMeasure(_renderingProfileCombineAndFxaa);
		if (NeedsJittering)
		{
			_profiling.StartMeasure(_renderingProfileTaa);
			DrawTemporalAA();
			_profiling.StopMeasure(_renderingProfileTaa);
		}
		else
		{
			_profiling.SkipMeasure(_renderingProfileTaa);
		}
		if (NeedsScreenBlur)
		{
			_profiling.StartMeasure(_renderingProfileBlur);
			DrawBlurredScreen();
			_profiling.StopMeasure(_renderingProfileBlur);
		}
		else
		{
			_profiling.SkipMeasure(_renderingProfileBlur);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UpdateDistortion(float time, float distortionAmplitude, float distortionFrequency)
	{
		_postEffectDrawParameters.Time = time;
		_postEffectDrawParameters.DistortionAmplitude = distortionAmplitude;
		_postEffectDrawParameters.DistortionFrequency = distortionFrequency;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UpdateColorFilters(Vector3 colorFilter, float colorSaturation)
	{
		_postEffectDrawParameters.ColorFilter = colorFilter;
		_postEffectDrawParameters.ColorSaturation = colorSaturation;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UpdateDebugTileResolution(Vector2 resolution)
	{
		_postEffectDrawParameters.DebugTileResolution = resolution;
	}

	public void UseBlur(bool enable)
	{
		_postEffectSettings.RequestScreenBlur = enable;
	}

	public void SetBlurStrength(int strength)
	{
		Debug.Assert(strength >= 0 && strength <= 3, $"Invalid blur strength {strength}. Valid values are [0-3].");
		_postEffectSettings.BlurredScreenSettings.ScreenBlurStrength = strength;
	}

	private void DrawBlurredScreen()
	{
		RenderTargetStore rTStore = _graphics.RTStore;
		RenderTarget sceneColorHalfRes = rTStore.SceneColorHalfRes;
		RenderTarget renderTarget = ((_postEffectSettings.BlurredScreenSettings.ScreenBlurStrength == 3) ? rTStore.BlurXResBy8 : rTStore.BlurXResBy4);
		RenderTarget renderTarget2 = ((_postEffectSettings.BlurredScreenSettings.ScreenBlurStrength == 3) ? rTStore.BlurYResBy8 : rTStore.BlurYResBy4);
		rTStore.FinalSceneColor.CopyColorTo(rTStore.SceneColorHalfRes, GL.COLOR_ATTACHMENT0, GL.COLOR_ATTACHMENT0, GL.LINEAR, bindSource: false, rebindSourceAfter: false);
		GraphicsDevice graphics = _graphics;
		BlurProgram blurProgram = _gpuProgramStore.BlurProgram;
		renderTarget.Bind(clear: false, setupViewport: true);
		_gl.UseProgram(blurProgram);
		_gl.BindTexture(GL.TEXTURE_2D, sceneColorHalfRes.GetTexture(RenderTarget.Target.Color0));
		blurProgram.PixelSize.SetValue(1f / (float)sceneColorHalfRes.Width, 1f / (float)sceneColorHalfRes.Height);
		float screenBlurScale = _postEffectSettings.BlurredScreenSettings.ScreenBlurScale;
		blurProgram.BlurScale.SetValue(screenBlurScale);
		blurProgram.HorizontalPass.SetValue(1f);
		graphics.ScreenTriangleRenderer.Draw();
		renderTarget.Unbind();
		renderTarget2.Bind(clear: false, setupViewport: false);
		_gl.BindTexture(GL.TEXTURE_2D, renderTarget.GetTexture(RenderTarget.Target.Color0));
		blurProgram.PixelSize.SetValue(1f / (float)renderTarget.Width, 1f / (float)renderTarget.Height);
		blurProgram.HorizontalPass.SetValue(0f);
		graphics.ScreenTriangleRenderer.Draw();
		renderTarget2.Unbind();
		if (_postEffectSettings.BlurredScreenSettings.ScreenBlurStrength > 1)
		{
			renderTarget.Bind(clear: false, setupViewport: false);
			_gl.UseProgram(blurProgram);
			_gl.BindTexture(GL.TEXTURE_2D, renderTarget2.GetTexture(RenderTarget.Target.Color0));
			blurProgram.HorizontalPass.SetValue(1f);
			graphics.ScreenTriangleRenderer.Draw();
			renderTarget.Unbind();
			renderTarget2.Bind(clear: false, setupViewport: false);
			_gl.BindTexture(GL.TEXTURE_2D, renderTarget.GetTexture(RenderTarget.Target.Color0));
			blurProgram.HorizontalPass.SetValue(0f);
			graphics.ScreenTriangleRenderer.Draw();
			renderTarget2.Unbind();
		}
		ScreenBlitProgram screenBlitProgram = _gpuProgramStore.ScreenBlitProgram;
		BindOutputFramebuffer();
		_gl.Viewport(0, 0, _width, _height);
		_gl.BindTexture(GL.TEXTURE_2D, renderTarget2.GetTexture(RenderTarget.Target.Color0));
		_gl.UseProgram(screenBlitProgram);
		screenBlitProgram.MipLevel.SetValue(0);
		graphics.ScreenTriangleRenderer.Draw();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UpdateTemporalAA(bool hasCameraMoved)
	{
		_hasCameraMoved = hasCameraMoved;
	}

	public void UseTemporalAA(bool enable)
	{
		if (enable != _postEffectSettings.UseTemporalAA)
		{
			_postEffectSettings.UseTemporalAA = enable;
			PostEffectProgram postEffectProgram = _postEffectProgram;
			postEffectProgram.SharpenStrength = (enable ? 0.2f : 0.1f);
			postEffectProgram.Reset();
		}
	}

	public void UseFXAA(bool enable)
	{
		if (enable != _postEffectSettings.UseFXAAA)
		{
			_postEffectSettings.UseFXAAA = enable;
			PostEffectProgram postEffectProgram = _postEffectProgram;
			postEffectProgram.UseFXAA = enable;
			postEffectProgram.Reset();
		}
	}

	public void UseFXAASharpened(bool enable, float strength = -1f)
	{
		if (enable != _postEffectSettings.UseSharpenPostEffect || strength != -1f)
		{
			_postEffectSettings.UseSharpenPostEffect = enable;
			PostEffectProgram postEffectProgram = _postEffectProgram;
			postEffectProgram.UseSharpenEffect = enable;
			if (strength != -1f)
			{
				postEffectProgram.SharpenStrength = strength;
			}
			postEffectProgram.Reset();
		}
	}

	private void DrawTemporalAA()
	{
		RenderTargetStore rTStore = _graphics.RTStore;
		rTStore.FinalSceneColor.Unbind();
		BindOutputFramebuffer();
		_gl.Viewport(0, 0, _width, _height);
		_gl.ActiveTexture(GL.TEXTURE1);
		_gl.BindTexture(GL.TEXTURE_2D, rTStore.PreviousFinalSceneColor.GetTexture(RenderTarget.Target.Color0));
		_gl.ActiveTexture(GL.TEXTURE0);
		_gl.BindTexture(GL.TEXTURE_2D, rTStore.FinalSceneColor.GetTexture(RenderTarget.Target.Color0));
		TemporalAAProgram temporalAAProgram = _gpuProgramStore.TemporalAAProgram;
		_gl.UseProgram(temporalAAProgram);
		temporalAAProgram.PixelSize.SetValue(rTStore.FinalSceneColor.InvWidth, rTStore.FinalSceneColor.InvHeight);
		temporalAAProgram.NeighborHoodCheck.SetValue(_hasCameraMoved ? 1 : 0);
		_graphics.ScreenTriangleRenderer.Draw();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPostFXBrightness(float value)
	{
		_postEffectDrawParameters.ColorBrightness = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPostFXContrast(float value)
	{
		_postEffectDrawParameters.ColorContrast = value;
	}

	public void SetVolumetricSunshaftStrength(float value)
	{
		_postEffectDrawParameters.VolumetricSunshaftStrength = value;
	}

	public void InitDepthOfField(GLTexture depthTexture, bool useDepthOfField = false, int version = 2, float nearBlurry = 1f, float nearSharp = 2f, float farSharp = 30f, float farBlurry = 70f, float nearBlurMax = 0.5f, float farBlurMax = 0.3f)
	{
		_depthInput = depthTexture;
		_postEffectSettings.UseDepthOfField = useDepthOfField;
		_postEffectProgram.UseDepthOfField = useDepthOfField;
		_postEffectSettings.DoFSettings.Version = version;
		_postEffectProgram.DepthOfFieldVersion = version;
		_postEffectProgram.Reset();
		SetupDepthOfField(nearBlurry, nearSharp, farSharp, farBlurry, nearBlurMax, farBlurMax);
		_postEffectDrawParameters.DepthOfFieldParams.ProjectionMatrix = Matrix.Identity;
	}

	public void SetupDepthOfField(float nearBlurry = 1f, float nearSharp = 2f, float farSharp = 30f, float farBlurry = 70f, float nearBlurMax = 0.5f, float farBlurMax = 0.3f)
	{
		_postEffectDrawParameters.DepthOfFieldParams.NearBlurry = nearBlurry;
		_postEffectDrawParameters.DepthOfFieldParams.NearSharp = nearSharp;
		_postEffectDrawParameters.DepthOfFieldParams.FarSharp = farSharp;
		_postEffectDrawParameters.DepthOfFieldParams.FarBlurry = farBlurry;
		_postEffectDrawParameters.DepthOfFieldParams.NearBlurMax = nearBlurMax;
		_postEffectDrawParameters.DepthOfFieldParams.FarBlurMax = farBlurMax;
	}

	public void SetDepthOfFieldVersion(int version)
	{
		if (_postEffectSettings.DoFSettings.Version != version)
		{
			_postEffectSettings.DoFSettings.Version = version;
			PostEffectProgram postEffectProgram = _postEffectProgram;
			postEffectProgram.DepthOfFieldVersion = version;
			postEffectProgram.Reset();
		}
	}

	public void UseDepthOfField(bool enable)
	{
		if (_postEffectSettings.UseDepthOfField != enable)
		{
			_postEffectSettings.UseDepthOfField = enable;
			PostEffectProgram postEffectProgram = _postEffectProgram;
			postEffectProgram.UseDepthOfField = enable;
			postEffectProgram.Reset();
		}
	}

	public void UpdateDepthOfField(Matrix projectionMatrix)
	{
		_postEffectDrawParameters.DepthOfFieldParams.ProjectionMatrix = projectionMatrix;
	}

	private void DrawDepthOfField()
	{
		int version = _postEffectSettings.DoFSettings.Version;
		DepthOfFieldDrawParams depthOfFieldParams = _postEffectDrawParameters.DepthOfFieldParams;
		RenderTargetStore rTStore = _graphics.RTStore;
		switch (version)
		{
		case 0:
			return;
		case 1:
		{
			rTStore.SceneColor.CopyColorTo(rTStore.SceneColorHalfRes, GL.COLOR_ATTACHMENT0, GL.COLOR_ATTACHMENT0, GL.LINEAR, bindSource: true, rebindSourceAfter: false);
			BlurProgram blurProgram = _gpuProgramStore.BlurProgram;
			rTStore.DOFBlurX.Bind(clear: true, setupViewport: true);
			_gl.UseProgram(blurProgram);
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.SceneColorHalfRes.GetTexture(RenderTarget.Target.Color0));
			blurProgram.PixelSize.SetValue(1f / (float)rTStore.DOFBlurX.Width, 1f / (float)rTStore.DOFBlurX.Height);
			float value2 = depthOfFieldParams.NearBlurMax + depthOfFieldParams.FarBlurMax;
			blurProgram.BlurScale.SetValue(value2);
			blurProgram.HorizontalPass.SetValue(1f);
			_graphics.ScreenTriangleRenderer.Draw();
			rTStore.DOFBlurX.Unbind();
			rTStore.DOFBlurY.Bind(clear: true, setupViewport: false);
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.DOFBlurX.GetTexture(RenderTarget.Target.Color0));
			blurProgram.HorizontalPass.SetValue(0f);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.DOFBlurY.Unbind();
			break;
		}
		case 2:
		{
			rTStore.SceneColor.CopyColorTo(rTStore.SceneColorHalfRes, GL.COLOR_ATTACHMENT0, GL.COLOR_ATTACHMENT0, GL.LINEAR, bindSource: true, rebindSourceAfter: false);
			DoFBlurProgram doFBlurProgram = _gpuProgramStore.DoFBlurProgram;
			_gl.UseProgram(doFBlurProgram);
			rTStore.DOFBlurXBis.Bind(clear: true, setupViewport: true);
			_gl.ActiveTexture(GL.TEXTURE1);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.SceneColorHalfRes.GetTexture(RenderTarget.Target.Color0));
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.SceneColorHalfRes.GetTexture(RenderTarget.Target.Color0));
			doFBlurProgram.PixelSize.SetValue(1f / (float)rTStore.DOFBlurXBis.Width, 1f / (float)rTStore.DOFBlurXBis.Height);
			float value3 = depthOfFieldParams.NearBlurMax * 2f;
			float value4 = depthOfFieldParams.FarBlurMax * 2f;
			doFBlurProgram.NearBlurScale.SetValue(value3);
			doFBlurProgram.FarBlurScale.SetValue(value4);
			doFBlurProgram.HorizontalPass.SetValue(1f);
			_graphics.ScreenTriangleRenderer.Draw();
			rTStore.DOFBlurXBis.Unbind();
			rTStore.DOFBlurYBis.Bind(clear: true, setupViewport: false);
			_gl.ActiveTexture(GL.TEXTURE1);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.DOFBlurXBis.GetTexture(RenderTarget.Target.Color1));
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.DOFBlurXBis.GetTexture(RenderTarget.Target.Color0));
			doFBlurProgram.HorizontalPass.SetValue(0f);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.DOFBlurYBis.Unbind();
			break;
		}
		case 3:
		{
			Vector2 value = new Vector2(1f / (float)rTStore.NearCoCBlurX.Width, 1f / (float)rTStore.NearCoCBlurX.Height);
			DoFCircleOfConfusionProgram doFCircleOfConfusionProgram = _gpuProgramStore.DoFCircleOfConfusionProgram;
			DoFDownsampleProgram doFDownsampleProgram = _gpuProgramStore.DoFDownsampleProgram;
			DoFNearCoCBlurProgram doFNearCoCBlurProgram = _gpuProgramStore.DoFNearCoCBlurProgram;
			MaxProgram doFNearCoCMaxProgram = _gpuProgramStore.DoFNearCoCMaxProgram;
			DepthOfFieldAdvancedProgram depthOfFieldAdvancedProgram = _gpuProgramStore.DepthOfFieldAdvancedProgram;
			DoFFillProgram doFFillProgram = _gpuProgramStore.DoFFillProgram;
			rTStore.CoC.Bind(clear: true, setupViewport: true);
			_gl.UseProgram(doFCircleOfConfusionProgram);
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, _depthInput);
			doFCircleOfConfusionProgram.NearBlurry.SetValue(depthOfFieldParams.NearBlurry);
			doFCircleOfConfusionProgram.NearSharp.SetValue(depthOfFieldParams.NearSharp);
			doFCircleOfConfusionProgram.FarSharp.SetValue(depthOfFieldParams.FarSharp);
			doFCircleOfConfusionProgram.FarBlurry.SetValue(depthOfFieldParams.FarBlurry);
			doFCircleOfConfusionProgram.ProjectionMatrix.SetValue(ref depthOfFieldParams.ProjectionMatrix);
			if (doFCircleOfConfusionProgram.UseLinearZ)
			{
				doFCircleOfConfusionProgram.FarClip.SetValue(1024f);
			}
			_graphics.ScreenTriangleRenderer.Draw();
			rTStore.CoC.Unbind();
			rTStore.Downsample.Bind(clear: true, setupViewport: true);
			_gl.UseProgram(doFDownsampleProgram);
			_gl.ActiveTexture(GL.TEXTURE2);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.CoC.GetTexture(RenderTarget.Target.Color0));
			_gl.ActiveTexture(GL.TEXTURE1);
			_gl.BindTexture(GL.TEXTURE_2D, _input);
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, _input);
			doFDownsampleProgram.PixelSize.SetValue(1f / (float)rTStore.SceneColor.Width, 1f / (float)rTStore.SceneColor.Height);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.Downsample.Unbind();
			rTStore.NearCoCMaxX.Bind(clear: true, setupViewport: false);
			_gl.UseProgram(doFNearCoCMaxProgram);
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.Downsample.GetTexture(RenderTarget.Target.Color2));
			doFNearCoCMaxProgram.PixelSize.SetValue(value);
			doFNearCoCMaxProgram.HorizontalPass.SetValue(1f);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.NearCoCMaxX.Unbind();
			rTStore.NearCoCMaxY.Bind(clear: true, setupViewport: false);
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.NearCoCMaxX.GetTexture(RenderTarget.Target.Color0));
			doFNearCoCMaxProgram.HorizontalPass.SetValue(0f);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.NearCoCMaxY.Unbind();
			rTStore.NearCoCBlurX.Bind(clear: true, setupViewport: false);
			_gl.UseProgram(doFNearCoCBlurProgram);
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.NearCoCMaxY.GetTexture(RenderTarget.Target.Color0));
			doFNearCoCBlurProgram.PixelSize.SetValue(value);
			doFNearCoCBlurProgram.HorizontalPass.SetValue(1f);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.NearCoCBlurX.Unbind();
			rTStore.NearCoCBlurY.Bind(clear: true, setupViewport: false);
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.NearCoCBlurX.GetTexture(RenderTarget.Target.Color0));
			doFNearCoCBlurProgram.HorizontalPass.SetValue(0f);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.NearCoCBlurY.Unbind();
			rTStore.NearFarField.Bind(clear: true, setupViewport: true);
			_gl.UseProgram(depthOfFieldAdvancedProgram);
			_gl.ActiveTexture(GL.TEXTURE5);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.NearCoCBlurY.GetTexture(RenderTarget.Target.Color0));
			_gl.ActiveTexture(GL.TEXTURE4);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.Downsample.GetTexture(RenderTarget.Target.Color2));
			_gl.ActiveTexture(GL.TEXTURE3);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.Downsample.GetTexture(RenderTarget.Target.Color1));
			_gl.ActiveTexture(GL.TEXTURE2);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.Downsample.GetTexture(RenderTarget.Target.Color2));
			_gl.ActiveTexture(GL.TEXTURE1);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.Downsample.GetTexture(RenderTarget.Target.Color1));
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.Downsample.GetTexture(RenderTarget.Target.Color0));
			depthOfFieldAdvancedProgram.NearBlurMax.SetValue(depthOfFieldParams.NearBlurMax);
			depthOfFieldAdvancedProgram.FarBlurMax.SetValue(depthOfFieldParams.FarBlurMax);
			depthOfFieldAdvancedProgram.PixelSize.SetValue(value);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.NearFarField.Unbind();
			rTStore.Fill.Bind(clear: true, setupViewport: true);
			_gl.UseProgram(doFFillProgram);
			_gl.ActiveTexture(GL.TEXTURE3);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.NearFarField.GetTexture(RenderTarget.Target.Color1));
			_gl.ActiveTexture(GL.TEXTURE2);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.NearFarField.GetTexture(RenderTarget.Target.Color0));
			_gl.ActiveTexture(GL.TEXTURE1);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.NearCoCBlurY.GetTexture(RenderTarget.Target.Color0));
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.Downsample.GetTexture(RenderTarget.Target.Color2));
			doFFillProgram.PixelSize.SetValue(value);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.Fill.Unbind();
			break;
		}
		}
		_gl.ActiveTexture(GL.TEXTURE0);
	}

	public void InitBloom(GLTexture sunTexture, GLTexture moonTexture, GLTexture glowMask, Action drawSun = null, Action drawMoon = null, bool useBloom = false, bool useSun = false, bool useMoon = false, bool useSunshaft = false, bool usePow = false, bool useFullbright = false, int version = 0, float globalIntensity = 0.3f, float power = 8f, float sunIntensity = 0.25f, float sunshaftIntensity = 0.3f, float sunshaftScaleFactor = 4f)
	{
		_postEffectSettings.UseBloom = useBloom;
		_postEffectProgram.UseBloom = useBloom;
		_postEffectSettings.BloomSettings.SunTexture = sunTexture;
		_postEffectSettings.BloomSettings.GlowMask = glowMask;
		_postEffectSettings.BloomSettings.DrawSun = drawSun;
		_postEffectSettings.BloomSettings.MoonTexture = moonTexture;
		_postEffectSettings.BloomSettings.DrawMoon = drawMoon;
		SetBloomVersion(version);
		_postEffectSettings.BloomSettings.UseSun = useSun;
		_postEffectSettings.BloomSettings.UseMoon = useMoon;
		_gpuProgramStore.BloomSelectProgram.SunOrMoon = useSun || useMoon;
		_postEffectProgram.UseSunshaft = useSunshaft;
		_postEffectSettings.BloomSettings.UseSunshaft = useSunshaft;
		_gpuProgramStore.BloomCompositeProgram.UseSunshaft = useSunshaft;
		_postEffectSettings.BloomSettings.UseFullbright = useFullbright;
		_gpuProgramStore.BloomSelectProgram.Fullbright = useFullbright;
		_postEffectSettings.BloomSettings.UsePow = usePow;
		_gpuProgramStore.BloomSelectProgram.Pow = usePow;
		bool sunFbPow = _gpuProgramStore.BloomSelectProgram.SunOrMoon || _postEffectSettings.BloomSettings.UseFullbright || _postEffectSettings.BloomSettings.UsePow;
		_gpuProgramStore.BloomCompositeProgram.SunFbPow = sunFbPow;
		_postEffectProgram.SunFbPow = sunFbPow;
		_gpuProgramStore.BloomSelectProgram.Reset();
		_gpuProgramStore.BloomCompositeProgram.Reset();
		_postEffectProgram.Reset();
		SetBloomGlobalIntensity(globalIntensity);
		_postEffectDrawParameters.BloomParams.Intensities = new float[5] { 1f, 2f, 3f, 4f, 5f };
		SetBloomPower(power);
		SetSunIntensity(sunIntensity);
		SetSunshaftIntensity(sunshaftIntensity);
		SetSunshaftScaleFactor(sunshaftScaleFactor);
		SetBloomOnPowIntensity(0.04f);
		SetBloomOnPowPower(5f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UpdateBloom(Matrix sunMVPMatrix, bool isSunVisible, bool allowBloom, Vector3 sunColor, bool isMoonVisible, Vector4 moonColor, float time)
	{
		_postEffectDrawParameters.BloomParams.SunMVP = sunMVPMatrix;
		_postEffectDrawParameters.BloomParams.IsSunVisible = isSunVisible;
		_postEffectDrawParameters.BloomParams.IsMoonVisible = isMoonVisible;
		_postEffectDrawParameters.BloomParams.isBloomAllowed = allowBloom;
		_postEffectDrawParameters.BloomParams.SunColor = sunColor;
		_postEffectDrawParameters.BloomParams.MoonColor = moonColor;
		_postEffectDrawParameters.BloomParams.Time = time;
		BloomSettings bloomSettings = _postEffectSettings.BloomSettings;
		bool flag = _postEffectSettings.UseBloom && allowBloom;
		_postEffectDrawParameters.BloomParams.ApplyBloom = (flag ? 1 : 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetSunshaftScaleFactor(float factor)
	{
		_postEffectDrawParameters.BloomParams.SunshaftScale = factor;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetSunshaftIntensity(float intensity)
	{
		_postEffectDrawParameters.BloomParams.SunshaftIntensity = intensity;
	}

	public void SetBloomVersion(int version)
	{
		if (version == 0)
		{
			_postEffectSettings.BloomSettings.Version = 0;
			SetDownsampleMethod(2);
			SetUpsampleMethod(1);
		}
		else
		{
			_postEffectSettings.BloomSettings.Version = 1;
			SetDownsampleMethod(2);
			SetUpsampleMethod(2);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDownsampleMethod(int method)
	{
		_postEffectSettings.BloomSettings.DownsampleMethod = MathHelper.Clamp(method, 0, 3);
		_gpuProgramStore.BloomDownsampleBlurProgram.DownsampleMethod = _postEffectSettings.BloomSettings.DownsampleMethod;
		_gpuProgramStore.BloomDownsampleBlurProgram.Reset();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetUpsampleMethod(int method)
	{
		_postEffectSettings.BloomSettings.UpsampleMethod = MathHelper.Clamp(method, 0, 2);
		_gpuProgramStore.BloomUpsampleBlurProgram.UpsampleMethod = _postEffectSettings.BloomSettings.UpsampleMethod;
		_gpuProgramStore.BloomUpsampleBlurProgram.Reset();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBloomGlobalIntensity(float intensity)
	{
		_postEffectDrawParameters.BloomParams.GlobalIntensity = intensity;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBloomIntensities(float i0, float i1, float i2, float i3, float i4)
	{
		_postEffectDrawParameters.BloomParams.Intensities[0] = i0;
		_postEffectDrawParameters.BloomParams.Intensities[1] = i1;
		_postEffectDrawParameters.BloomParams.Intensities[2] = i2;
		_postEffectDrawParameters.BloomParams.Intensities[3] = i3;
		_postEffectDrawParameters.BloomParams.Intensities[4] = i4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBloomPower(float power)
	{
		_postEffectDrawParameters.BloomParams.Power = power;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetSunIntensity(float intensity)
	{
		_postEffectDrawParameters.BloomParams.SunMoonIntensity = intensity;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBloomOnPowIntensity(float intensity)
	{
		_postEffectDrawParameters.BloomParams.PowIntensity = intensity;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBloomOnPowPower(float power)
	{
		_postEffectDrawParameters.BloomParams.PowPower = power;
	}

	public void UseBloom(bool enable)
	{
		if (enable != _postEffectSettings.UseBloom)
		{
			_postEffectSettings.UseBloom = enable;
			PostEffectProgram postEffectProgram = _postEffectProgram;
			postEffectProgram.UseBloom = enable;
			postEffectProgram.Reset();
		}
	}

	public void UseBloomOnSun(bool enable)
	{
		if (enable != _postEffectSettings.BloomSettings.UseSun)
		{
			bool useMoon = _postEffectSettings.BloomSettings.UseMoon;
			_postEffectSettings.BloomSettings.UseSun = enable;
			_gpuProgramStore.BloomSelectProgram.SunOrMoon = enable || useMoon;
			bool sunFbPow = _gpuProgramStore.BloomSelectProgram.SunOrMoon || _postEffectSettings.BloomSettings.UseFullbright || _postEffectSettings.BloomSettings.UsePow;
			_gpuProgramStore.BloomCompositeProgram.SunFbPow = sunFbPow;
			_postEffectProgram.SunFbPow = sunFbPow;
			_gpuProgramStore.BloomSelectProgram.Reset();
			_gpuProgramStore.BloomCompositeProgram.Reset();
			_postEffectProgram.Reset();
		}
	}

	public void UseBloomOnMoon(bool enable)
	{
		if (enable != _postEffectSettings.BloomSettings.UseMoon)
		{
			bool useSun = _postEffectSettings.BloomSettings.UseSun;
			_postEffectSettings.BloomSettings.UseMoon = enable;
			_gpuProgramStore.BloomSelectProgram.SunOrMoon = enable || useSun;
			bool sunFbPow = _gpuProgramStore.BloomSelectProgram.SunOrMoon || _postEffectSettings.BloomSettings.UseFullbright || _postEffectSettings.BloomSettings.UsePow;
			_gpuProgramStore.BloomCompositeProgram.SunFbPow = sunFbPow;
			_postEffectProgram.SunFbPow = sunFbPow;
			_gpuProgramStore.BloomSelectProgram.Reset();
			_gpuProgramStore.BloomCompositeProgram.Reset();
			_postEffectProgram.Reset();
		}
	}

	public void UseBloomOnFullbright(bool enable)
	{
		if (enable != _postEffectSettings.BloomSettings.UseFullbright)
		{
			_postEffectSettings.BloomSettings.UseFullbright = enable;
			_gpuProgramStore.BloomSelectProgram.Fullbright = enable;
			bool sunFbPow = _gpuProgramStore.BloomSelectProgram.SunOrMoon || _postEffectSettings.BloomSettings.UseFullbright || _postEffectSettings.BloomSettings.UsePow;
			_gpuProgramStore.BloomCompositeProgram.SunFbPow = sunFbPow;
			_postEffectProgram.SunFbPow = sunFbPow;
			_gpuProgramStore.BloomSelectProgram.Reset();
			_gpuProgramStore.BloomCompositeProgram.Reset();
			_postEffectProgram.Reset();
		}
	}

	public void UseBloomOnFullscreen(bool enable)
	{
		if (enable != _postEffectSettings.BloomSettings.UsePow)
		{
			_postEffectSettings.BloomSettings.UsePow = enable;
			_gpuProgramStore.BloomSelectProgram.Pow = enable;
			bool sunFbPow = _gpuProgramStore.BloomSelectProgram.SunOrMoon || _postEffectSettings.BloomSettings.UseFullbright || _postEffectSettings.BloomSettings.UsePow;
			_gpuProgramStore.BloomCompositeProgram.SunFbPow = sunFbPow;
			_postEffectProgram.SunFbPow = sunFbPow;
			_gpuProgramStore.BloomSelectProgram.Reset();
			_gpuProgramStore.BloomCompositeProgram.Reset();
			_postEffectProgram.Reset();
		}
	}

	public void UseBloomSunShaft(bool enable)
	{
		if (enable != _postEffectSettings.BloomSettings.UseSunshaft)
		{
			_postEffectSettings.BloomSettings.UseSunshaft = enable;
			_gpuProgramStore.BloomCompositeProgram.UseSunshaft = enable;
			_postEffectProgram.UseSunshaft = enable;
			_gpuProgramStore.BloomCompositeProgram.Reset();
			_postEffectProgram.Reset();
		}
	}

	public void UseDitheringOnBloom(bool enable)
	{
		_gpuProgramStore.BloomSelectProgram.UseDithering = enable;
		_gpuProgramStore.BloomSelectProgram.Reset();
	}

	public string PrintBloomState()
	{
		BloomSettings bloomSettings = _postEffectSettings.BloomSettings;
		BloomDrawParams bloomParams = _postEffectDrawParameters.BloomParams;
		string text = "Bloom state :";
		if (_postEffectSettings.UseBloom)
		{
			float[] intensities = bloomParams.Intensities;
			text += " on";
			text = text + " v" + bloomSettings.Version;
			if (bloomSettings.UseSun)
			{
				text += " sun";
			}
			if (bloomSettings.UseMoon)
			{
				text += " moon";
			}
			if (bloomSettings.UsePow)
			{
				text += " pow";
			}
			if (bloomSettings.UseFullbright)
			{
				text += " fb";
			}
			if (bloomSettings.UseSunshaft)
			{
				text += " sunshaft";
			}
			text = text + "\n down " + bloomSettings.DownsampleMethod;
			text = text + " up " + bloomSettings.UpsampleMethod;
			text += "\n global intensity=";
			text += bloomParams.GlobalIntensity;
			text += "\n intensities=";
			text = text + intensities[0] + " " + intensities[1] + " " + intensities[2] + " " + intensities[3] + " " + intensities[4];
			text += "\n power=";
			text += bloomParams.Power;
			text += "\n sunshaft_scale=";
			text += bloomParams.SunshaftScale;
			text += "\n sunshaft_intensity=";
			text += bloomParams.SunshaftIntensity;
			text += "\n sun_moon_intensity=";
			text += bloomParams.SunMoonIntensity;
			if (bloomSettings.UsePow)
			{
				text += "\n pow_intensity=";
				text += bloomParams.PowIntensity;
				text += "\n pow_power=";
				text += bloomParams.PowPower;
			}
		}
		else
		{
			text += " off";
		}
		return text;
	}

	private void DrawBloom()
	{
		_gl.AssertActiveTexture(GL.TEXTURE0);
		BloomSettings bloomSettings = _postEffectSettings.BloomSettings;
		BloomDrawParams bloomParams = _postEffectDrawParameters.BloomParams;
		RenderTargetStore rTStore = _graphics.RTStore;
		bool useSun = bloomSettings.UseSun;
		bool useMoon = bloomSettings.UseMoon;
		bool flag = useSun || useMoon;
		bool useSunshaft = bloomSettings.UseSunshaft;
		bool usePow = bloomSettings.UsePow;
		bool useFullbright = bloomSettings.UseFullbright;
		bool flag2 = _postEffectSettings.UseBloom && bloomParams.isBloomAllowed;
		_postEffectDrawParameters.BloomParams.ApplyBloom = (flag2 ? 1 : 0);
		if (!flag2)
		{
			return;
		}
		if (useSun || useMoon)
		{
			BasicProgram basicProgram = _gpuProgramStore.BasicProgram;
			_gl.UseProgram(basicProgram);
			_gl.Enable(GL.DEPTH_TEST);
			rTStore.SunRT.Bind(clear: true, setupViewport: true);
			if (bloomParams.IsSunVisible && useSun)
			{
				basicProgram.Opacity.SetValue(1f);
				basicProgram.Color.SetValue(bloomParams.SunColor.X, bloomParams.SunColor.Y, bloomParams.SunColor.Z);
				_gl.BindTexture(GL.TEXTURE_2D, bloomSettings.SunTexture);
				bloomSettings.DrawSun();
			}
			if (bloomParams.IsMoonVisible && useMoon)
			{
				basicProgram.Opacity.SetValue(bloomParams.MoonColor.W);
				basicProgram.Color.SetValue(bloomParams.MoonColor.X, bloomParams.MoonColor.Y, bloomParams.MoonColor.Z);
				_gl.BindTexture(GL.TEXTURE_2D, bloomSettings.MoonTexture);
				bloomSettings.DrawMoon();
			}
			rTStore.SunRT.Unbind();
			_gl.Disable(GL.DEPTH_TEST);
		}
		BlurProgram blurProgram = _gpuProgramStore.BlurProgram;
		_graphics.ScreenTriangleRenderer.BindVertexArray();
		if (useSunshaft && bloomParams.IsSunVisible)
		{
			_gl.UseProgram(blurProgram);
			rTStore.SunshaftX.Bind(clear: false, setupViewport: true);
			_gl.BindTexture(GL.TEXTURE_2D, _input);
			blurProgram.PixelSize.SetValue(1f / (float)rTStore.SunshaftX.Width, 1f / (float)rTStore.SunshaftX.Height);
			blurProgram.BlurScale.SetValue(1f);
			blurProgram.HorizontalPass.SetValue(1f);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.SunshaftX.Unbind();
			rTStore.SunshaftY.Bind(clear: false, setupViewport: false);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.SunshaftX.GetTexture(RenderTarget.Target.Color0));
			blurProgram.HorizontalPass.SetValue(0f);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.SunshaftY.Unbind();
			RadialGlowMaskProgram radialGlowMaskProgram = _gpuProgramStore.RadialGlowMaskProgram;
			_gl.UseProgram(radialGlowMaskProgram);
			rTStore.SunshaftX.Bind(clear: false, setupViewport: false);
			_gl.ActiveTexture(GL.TEXTURE2);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.LinearZ.GetTexture(RenderTarget.Target.Color0));
			_gl.ActiveTexture(GL.TEXTURE1);
			_gl.BindTexture(GL.TEXTURE_2D, bloomSettings.GlowMask);
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.SunshaftY.GetTexture(RenderTarget.Target.Color0));
			radialGlowMaskProgram.MVPMatrix.SetValue(ref _graphics.ScreenMatrix);
			radialGlowMaskProgram.SunMVPMatrix.SetValue(ref bloomParams.SunMVP);
			_graphics.ScreenQuadRenderer.Draw();
			rTStore.SunshaftX.Unbind();
			RadialGlowLuminanceProgram radialGlowLuminanceProgram = _gpuProgramStore.RadialGlowLuminanceProgram;
			_gl.UseProgram(radialGlowLuminanceProgram);
			rTStore.SunshaftY.Bind(clear: false, setupViewport: false);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.SunshaftX.GetTexture(RenderTarget.Target.Color0));
			radialGlowLuminanceProgram.MVPMatrix.SetValue(ref _graphics.ScreenMatrix);
			radialGlowLuminanceProgram.SunMVPMatrix.SetValue(ref bloomParams.SunMVP);
			radialGlowLuminanceProgram.ScaleFactor.SetValue(0f - bloomParams.SunshaftScale);
			_graphics.ScreenQuadRenderer.Draw();
			rTStore.SunshaftY.Unbind();
		}
		if (flag || usePow || useFullbright)
		{
			BloomSelectProgram bloomSelectProgram = _gpuProgramStore.BloomSelectProgram;
			_gl.UseProgram(bloomSelectProgram);
			rTStore.BlurXResBy2.Bind(clear: false, setupViewport: true);
			if (flag)
			{
				int value = ((bloomParams.IsSunVisible || bloomParams.IsMoonVisible) ? 1 : 0);
				bloomSelectProgram.UseSunOrMoon.SetValue(value);
				if (bloomSelectProgram.UseDithering)
				{
					bloomSelectProgram.Time.SetValue(bloomParams.Time);
				}
				if (bloomParams.IsSunVisible || bloomParams.IsMoonVisible)
				{
					_gl.ActiveTexture(GL.TEXTURE1);
					_gl.BindTexture(GL.TEXTURE_2D, rTStore.SunRT.GetTexture(RenderTarget.Target.Color0));
					float sunMoonIntensity = bloomParams.SunMoonIntensity;
					sunMoonIntensity *= (bloomParams.IsMoonVisible ? bloomParams.MoonColor.W : 1f);
					bloomSelectProgram.SunMoonIntensity.SetValue(sunMoonIntensity);
				}
			}
			if (usePow || useFullbright)
			{
				_gl.ActiveTexture(GL.TEXTURE0);
				_gl.BindTexture(GL.TEXTURE_2D, _input);
				bloomSelectProgram.Power.SetValue(bloomParams.Power);
			}
			if (usePow)
			{
				bloomSelectProgram.PowerOptions.SetValue(bloomParams.PowIntensity, bloomParams.PowPower);
			}
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.BlurXResBy2.Unbind();
			BloomDownsampleBlurProgram bloomDownsampleBlurProgram = _gpuProgramStore.BloomDownsampleBlurProgram;
			_gl.UseProgram(bloomDownsampleBlurProgram);
			rTStore.BlurXResBy4.Bind(clear: false, setupViewport: true);
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurXResBy2.GetTexture(RenderTarget.Target.Color0));
			bloomDownsampleBlurProgram.PixelSize.SetValue(_renderScale / (float)rTStore.BlurXResBy2.Width, _renderScale / (float)rTStore.BlurXResBy2.Height);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.BlurXResBy4.Unbind();
			rTStore.BlurXResBy8.Bind(clear: false, setupViewport: true);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurXResBy4.GetTexture(RenderTarget.Target.Color0));
			bloomDownsampleBlurProgram.PixelSize.SetValue(_renderScale / (float)rTStore.BlurXResBy4.Width, _renderScale / (float)rTStore.BlurXResBy4.Height);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.BlurXResBy8.Unbind();
			rTStore.BlurXResBy16.Bind(clear: false, setupViewport: true);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurXResBy8.GetTexture(RenderTarget.Target.Color0));
			bloomDownsampleBlurProgram.PixelSize.SetValue(_renderScale / (float)rTStore.BlurXResBy8.Width, _renderScale / (float)rTStore.BlurXResBy8.Height);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.BlurXResBy16.Unbind();
			rTStore.BlurXResBy32.Bind(clear: false, setupViewport: true);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurXResBy16.GetTexture(RenderTarget.Target.Color0));
			bloomDownsampleBlurProgram.PixelSize.SetValue(_renderScale / (float)rTStore.BlurXResBy16.Width, _renderScale / (float)rTStore.BlurXResBy16.Height);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.BlurXResBy32.Unbind();
			BloomUpsampleBlurProgram bloomUpsampleBlurProgram = _gpuProgramStore.BloomUpsampleBlurProgram;
			_gl.UseProgram(bloomUpsampleBlurProgram);
			rTStore.BlurYResBy16.Bind(clear: false, setupViewport: true);
			_gl.ActiveTexture(GL.TEXTURE1);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurXResBy32.GetTexture(RenderTarget.Target.Color0));
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurXResBy16.GetTexture(RenderTarget.Target.Color0));
			bloomUpsampleBlurProgram.PixelSize.SetValue(_renderScale / (float)rTStore.BlurXResBy32.Width, _renderScale / (float)rTStore.BlurXResBy32.Height);
			bloomUpsampleBlurProgram.Scale.SetValue(1f);
			bloomUpsampleBlurProgram.Intensity.SetValue(bloomParams.Intensities[4], bloomParams.Intensities[3]);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.BlurYResBy16.Unbind();
			rTStore.BlurYResBy8.Bind(clear: false, setupViewport: true);
			_gl.ActiveTexture(GL.TEXTURE1);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurYResBy16.GetTexture(RenderTarget.Target.Color0));
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurXResBy8.GetTexture(RenderTarget.Target.Color0));
			bloomUpsampleBlurProgram.PixelSize.SetValue(_renderScale / (float)rTStore.BlurYResBy16.Width, _renderScale / (float)rTStore.BlurYResBy16.Height);
			bloomUpsampleBlurProgram.Scale.SetValue(1f);
			bloomUpsampleBlurProgram.Intensity.SetValue(1f, bloomParams.Intensities[2]);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.BlurYResBy8.Unbind();
			rTStore.BlurYResBy4.Bind(clear: false, setupViewport: true);
			_gl.ActiveTexture(GL.TEXTURE1);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurYResBy8.GetTexture(RenderTarget.Target.Color0));
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurXResBy4.GetTexture(RenderTarget.Target.Color0));
			bloomUpsampleBlurProgram.PixelSize.SetValue(_renderScale / (float)rTStore.BlurYResBy8.Width, _renderScale / (float)rTStore.BlurYResBy8.Height);
			bloomUpsampleBlurProgram.Scale.SetValue(1f);
			bloomUpsampleBlurProgram.Intensity.SetValue(1f, bloomParams.Intensities[1]);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.BlurYResBy4.Unbind();
			rTStore.BlurYResBy2.Bind(clear: false, setupViewport: true);
			_gl.ActiveTexture(GL.TEXTURE1);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurYResBy4.GetTexture(RenderTarget.Target.Color0));
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurXResBy2.GetTexture(RenderTarget.Target.Color0));
			bloomUpsampleBlurProgram.PixelSize.SetValue(_renderScale / (float)rTStore.BlurYResBy4.Width, _renderScale / (float)rTStore.BlurYResBy4.Height);
			bloomUpsampleBlurProgram.Scale.SetValue(1f);
			bloomUpsampleBlurProgram.Intensity.SetValue(1f, bloomParams.Intensities[0]);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.BlurYResBy2.Unbind();
		}
		if (flag || usePow || useFullbright || useSunshaft)
		{
			rTStore.BlurXResBy2.Bind(clear: false, setupViewport: true);
			BloomCompositeProgram bloomCompositeProgram = _gpuProgramStore.BloomCompositeProgram;
			_gl.UseProgram(bloomCompositeProgram);
			if (useSunshaft)
			{
				_gl.ActiveTexture(GL.TEXTURE3);
				_gl.BindTexture(GL.TEXTURE_2D, rTStore.SunshaftY.GetTexture(RenderTarget.Target.Color0));
				bloomCompositeProgram.SunshaftIntensity.SetValue(bloomParams.IsSunVisible ? bloomParams.SunshaftIntensity : 0f);
			}
			_gl.ActiveTexture(GL.TEXTURE0);
			_gl.BindTexture(GL.TEXTURE_2D, rTStore.BlurYResBy2.GetTexture(RenderTarget.Target.Color0));
			bloomCompositeProgram.BloomIntensity.SetValue(bloomParams.GlobalIntensity);
			_graphics.ScreenTriangleRenderer.DrawRaw();
			rTStore.BlurXResBy2.Unbind();
		}
	}

	public void UseDistortion(bool enable)
	{
		_postEffectProgram.UseDistortion = enable;
		_postEffectProgram.Reset();
	}
}
