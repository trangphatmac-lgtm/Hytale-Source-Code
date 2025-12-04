using HytaleClient.Core;
using HytaleClient.Graphics.Particles;
using HytaleClient.Graphics.Trails;
using HytaleClient.Math;
using NLog;

namespace HytaleClient.Graphics;

internal class FXSystem : Disposable
{
	public enum RenderMode
	{
		BlendLinear,
		BlendAdd,
		Erosion,
		Distortion
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly byte FXDrawTagForceFieldColor = 1;

	public readonly byte FXDrawTagForceFieldDistorion = 2;

	public readonly ParticleFXSystem Particles;

	public readonly TrailFXSystem Trails;

	public readonly ForceFieldFXSystem ForceFields;

	private readonly GraphicsDevice _graphics;

	private readonly Profiling _profiling;

	private readonly FXRenderer _fxRenderer;

	private GLSampler _smoothSampler;

	private readonly float _engineTimeStep;

	private int _renderingProfileSendVertexData;

	public GLSampler SmoothSampler => _smoothSampler;

	public void SetupDrawDataTexture(uint textureUnitId)
	{
		_fxRenderer.SetupDrawDataTexture(textureUnitId);
	}

	public void DrawErosion()
	{
		_fxRenderer.DrawErosion();
	}

	public void DrawTransparencyLowRes()
	{
		_fxRenderer.DrawTransparencyLowRes();
	}

	public void DrawTransparency()
	{
		_fxRenderer.DrawTransparency();
	}

	public void DrawDistortion()
	{
		_fxRenderer.DrawDistortion();
	}

	public FXSystem(GraphicsDevice graphics, Profiling profiling, float engineTimeStep)
	{
		_graphics = graphics;
		_profiling = profiling;
		_engineTimeStep = engineTimeStep;
		if (_graphics != null)
		{
			Particles = new ParticleFXSystem(_graphics, _engineTimeStep);
			Trails = new TrailFXSystem(_graphics, _engineTimeStep);
			_fxRenderer = new FXRenderer(_graphics);
			_fxRenderer.Initialize(Particles.MaxParticleCount + Trails.SegmentBufferStorageMaxCount, Particles.MaxParticleDrawCount + Trails.MaxParticleDrawCount);
			ForceFields = new ForceFieldFXSystem(_graphics);
			GLFunctions gL = _graphics.GL;
			_smoothSampler = gL.GenSampler();
			gL.SamplerParameteri(_smoothSampler, GL.TEXTURE_WRAP_S, GL.CLAMP_TO_EDGE);
			gL.SamplerParameteri(_smoothSampler, GL.TEXTURE_WRAP_T, GL.CLAMP_TO_EDGE);
			gL.SamplerParameteri(_smoothSampler, GL.TEXTURE_MIN_FILTER, GL.LINEAR);
			gL.SamplerParameteri(_smoothSampler, GL.TEXTURE_MAG_FILTER, GL.LINEAR);
		}
	}

	protected override void DoDispose()
	{
		if (_graphics != null)
		{
			GLFunctions gL = _graphics.GL;
			gL.DeleteSampler(_smoothSampler);
			ForceFields.Dispose();
			Trails.Dispose();
			Particles.Dispose();
		}
	}

	public void SetupRenderingProfile(int renderingProfileSendVertexData)
	{
		_renderingProfileSendVertexData = renderingProfileSendVertexData;
	}

	public void BeginFrame()
	{
		ForceFields.BeginFrame();
		Particles.BeginFrame();
		_fxRenderer.BeginFrame();
	}

	public void PrepareForDraw(Vector3 cameraPosition)
	{
		Particles.DispatchSpawnersDrawTasks();
		PrepareVertexDataStorage();
		if (_fxRenderer.TryBeginDrawDataTransfer(out var dataPtr))
		{
			Particles.PrepareForDraw(_fxRenderer, cameraPosition, dataPtr);
			Trails.PrepareForDraw(_fxRenderer, cameraPosition, dataPtr);
			_fxRenderer.EndDrawDataTransfer();
		}
		_profiling.StartMeasure(_renderingProfileSendVertexData);
		_fxRenderer.SendVertexDataToGPU();
		_profiling.StopMeasure(_renderingProfileSendVertexData);
	}

	private void PrepareVertexDataStorage()
	{
		_fxRenderer.ClearVertexData();
		Particles.PrepareErosionVertexDataStorage(_fxRenderer);
		Trails.PrepareErosionVertexDataStorage(_fxRenderer);
		Particles.PrepareLowResVertexDataStorage(_fxRenderer);
		Particles.PrepareBlendVertexDataStorage(_fxRenderer);
		Trails.PrepareBlendVertexDataStorage(_fxRenderer);
		Particles.PrepareFPVVertexDataStorage(_fxRenderer);
		Trails.PrepareFPVVertexDataStorage(_fxRenderer);
		Particles.PrepareDistortionVertexDataStorage(_fxRenderer);
		Trails.PrepareDistortionVertexDataStorage(_fxRenderer);
	}

	public void ProcessFXTasks()
	{
	}
}
