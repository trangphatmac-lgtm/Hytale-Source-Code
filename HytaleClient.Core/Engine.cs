#define DEBUG
#define TRACE
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using HytaleClient.Application;
using HytaleClient.Audio;
using HytaleClient.Graphics;
using HytaleClient.Utils;
using NLog;
using SDL2;

namespace HytaleClient.Core;

internal class Engine : Disposable
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const float TimeStep = 1f / 60f;

	public const int TimeStepMilliseconds = 16;

	public const float MaxAccumulatedTickTime = 1f / 12f;

	private const int MaxDistributedTaskTime = 8;

	private const int DistributedTaskQueueBackPressureLength = 500;

	private readonly ConcurrentQueue<Tuple<Disposable, Action>> _mainThreadActionQueue = new ConcurrentQueue<Tuple<Disposable, Action>>();

	private readonly ConcurrentQueue<Tuple<Disposable, Action>> _distributedMainThreadActionQueue = new ConcurrentQueue<Tuple<Disposable, Action>>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private double _timeSpentInQueuedActions;

	public Window Window { get; private set; }

	public GraphicsDevice Graphics { get; private set; }

	public AudioDevice Audio { get; private set; }

	public AnimationSystem AnimationSystem { get; private set; }

	public FXSystem FXSystem { get; private set; }

	public Profiling Profiling { get; private set; }

	public OcclusionCulling OcclusionCulling { get; private set; }

	public double TimeSpentInQueuedActions => _timeSpentInQueuedActions;

	public static void Initialize()
	{
		if (BuildInfo.Platform == Platform.Windows)
		{
			bool flag = WindowsDPIHelper.TryEnableDpiAwareness();
			Trace.WriteLine($"TryEnableDpiAwareness returned {flag}", "Engine");
		}
		if (SDL.SDL_Init(32u) < 0)
		{
			throw new Exception("SDL_Init failed: " + SDL.SDL_GetError());
		}
		if (SDL_ttf.TTF_Init() < 0)
		{
			throw new Exception("TTF_Init failed: " + SDL.SDL_GetError());
		}
		SDL.SDL_StartTextInput();
	}

	public Engine(Window.WindowSettings windowSettings, bool allowBatcher2dToGrow = false)
	{
		GraphicsDevice.SetupGLAttributes();
		Window = new Window(windowSettings);
		Graphics = new GraphicsDevice(Window, allowBatcher2dToGrow);
		GLFunctions gL = Graphics.GL;
		gL.Viewport(Window.Viewport);
		gL.ClearColor(0f, 0f, 0f, 1f);
		gL.Clear(GL.COLOR_BUFFER_BIT);
		SDL.SDL_GL_SwapWindow(Window.Handle);
		Profiling = new Profiling(Graphics.GL);
		AnimationSystem = new AnimationSystem(Graphics.GL);
		FXSystem = new FXSystem(Graphics, Profiling, 1f / 60f);
		OcclusionCulling = new OcclusionCulling(Graphics, Profiling);
	}

	public void InitializeAudio(uint outputDeviceId, string masterVolumeRTPC, float masterVolume, string[] categoryRTPCs, float[] categoryVolumes)
	{
		Debug.Assert(Audio == null);
		Audio = new AudioDevice(outputDeviceId, masterVolumeRTPC, masterVolume, categoryRTPCs, categoryVolumes, Enum.GetNames(typeof(App.SoundGroupType)).Length);
	}

	protected override void DoDispose()
	{
		Audio?.Dispose();
		OcclusionCulling?.Dispose();
		FXSystem?.Dispose();
		AnimationSystem?.Dispose();
		Profiling?.Dispose();
		Graphics?.Dispose();
		Window.Dispose();
		SDL_ttf.TTF_Quit();
		SDL.SDL_Quit();
	}

	public void SetMouseRelativeModeRaw(bool isRawMode)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		SDL.SDL_SetHint("SDL_MOUSE_RELATIVE_MODE_WARP", isRawMode ? "0" : "1");
	}

	public void RunOnMainThread(Disposable disposeGate, Action action, bool allowCallFromMainThread = false, bool distributed = false)
	{
		Debug.Assert(allowCallFromMainThread || !ThreadHelper.IsMainThread());
		if (distributed)
		{
			_distributedMainThreadActionQueue.Enqueue(Tuple.Create(disposeGate, action));
		}
		else
		{
			_mainThreadActionQueue.Enqueue(Tuple.Create(disposeGate, action));
		}
	}

	public void Temp_ProcessQueuedActions()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		_stopwatch.Restart();
		Tuple<Disposable, Action> result;
		while (_mainThreadActionQueue.TryDequeue(out result))
		{
			if (!result.Item1.Disposed)
			{
				result.Item2();
			}
		}
		if (!_distributedMainThreadActionQueue.IsEmpty)
		{
			while (_stopwatch.ElapsedMilliseconds < 8 && _distributedMainThreadActionQueue.TryDequeue(out result))
			{
				if (!result.Item1.Disposed)
				{
					result.Item2();
				}
			}
			int count = _distributedMainThreadActionQueue.Count;
			if (count > 500)
			{
				Logger.Warn("Distributed task queue is getting too large: {0}", count);
				while (_distributedMainThreadActionQueue.Count > 500 && _distributedMainThreadActionQueue.TryDequeue(out result))
				{
					if (!result.Item1.Disposed)
					{
						result.Item2();
					}
				}
			}
		}
		_timeSpentInQueuedActions = stopwatch.Elapsed.TotalMilliseconds;
	}
}
