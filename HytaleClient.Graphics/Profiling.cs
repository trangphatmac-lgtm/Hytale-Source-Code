#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Common.Collections;
using HytaleClient.Common.Memory;
using HytaleClient.Core;

namespace HytaleClient.Graphics;

internal class Profiling : Disposable
{
	public struct MeasureInfo
	{
		public bool IsEnabled;

		public bool IsAlwaysEnabled;

		public bool IsExternal;

		public bool HasGpuStats;

		public int AccumulatedFrameCount;
	}

	public struct CPUMeasure
	{
		public float AccumulatedElapsedTime;

		public float ElapsedTime;

		public float MaxElapsedTime;
	}

	public struct GPUMeasure
	{
		public float AccumulatedElapsedTime;

		public float ElapsedTime;

		public float MaxElapsedTime;

		public int DrawCalls;

		public int DrawnVertices;
	}

	private struct MeasureAtStart
	{
		public float ElapsedTime;

		public int DrawCalls;

		public int DrawnVertices;
	}

	private GLFunctions _gl;

	private int _maxMeasures;

	private int _iterationsBeforeReset;

	private bool _initialized;

	private string[] _measureNames;

	private MeasureInfo[] _measureInfos;

	private CPUMeasure[] _cpuMeasures;

	private MeasureAtStart[] _measureAtStart;

	private GPUMeasure[] _gpuMeasures;

	private GPUTimer[] _gpuTimers;

	private NativeArray<bool> _measureIdUsed;

	private Stopwatch _stopwatch = Stopwatch.StartNew();

	public int MeasureCount { get; private set; }

	public ref MeasureInfo GetMeasureInfo(int id)
	{
		return ref _measureInfos[id];
	}

	public ref CPUMeasure GetCPUMeasure(int id)
	{
		return ref _cpuMeasures[id];
	}

	public ref GPUMeasure GetGPUMeasure(int id)
	{
		return ref _gpuMeasures[id];
	}

	public string GetMeasureName(int id)
	{
		return _measureNames[id];
	}

	public Profiling(GLFunctions gl = null)
	{
		_gl = gl;
	}

	public void Initialize(int maxMeasures, int iterationsBeforeReset = 200)
	{
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		if (_maxMeasures != 0)
		{
			Release();
		}
		Debug.Assert(_maxMeasures == 0, "Profiling already Initialize()'d. Please Release() before re-using it.");
		_iterationsBeforeReset = iterationsBeforeReset;
		_maxMeasures = maxMeasures;
		_measureNames = new string[_maxMeasures];
		_measureInfos = new MeasureInfo[_maxMeasures];
		_cpuMeasures = new CPUMeasure[_maxMeasures];
		_measureAtStart = new MeasureAtStart[_maxMeasures];
		_gpuMeasures = new GPUMeasure[_maxMeasures];
		_gpuTimers = new GPUTimer[_maxMeasures];
		_measureIdUsed = new NativeArray<bool>(_maxMeasures, (Allocator)0, (AllocOptions)0);
		_initialized = true;
	}

	private void Release()
	{
		Debug.Assert(_maxMeasures != 0, "Profiling never Initialize()'d - or already Release()'d.");
		if (!_initialized)
		{
			return;
		}
		_measureIdUsed.Dispose();
		for (int i = 0; i < MeasureCount; i++)
		{
			if (_measureInfos[i].HasGpuStats)
			{
				_gpuTimers[i].DestroyStorage();
			}
		}
		_gpuTimers = null;
		_gpuMeasures = null;
		_measureAtStart = null;
		_cpuMeasures = null;
		_measureInfos = null;
		_measureNames = null;
		MeasureCount = 0;
		_maxMeasures = 0;
	}

	protected override void DoDispose()
	{
		if (_maxMeasures != 0)
		{
			Release();
		}
	}

	public void SwapMeasureBuffers()
	{
		for (int i = 0; i < MeasureCount; i++)
		{
			if (_measureInfos[i].HasGpuStats)
			{
				_gpuTimers[i].Swap();
			}
		}
	}

	public void CreateMeasure(string name, int measureId, bool cpuOnly = false, bool alwaysEnabled = false, bool isExternal = false)
	{
		Debug.Assert(measureId < _maxMeasures, "Total amount of Profiling measures has been exceeded.");
		Debug.Assert(!_measureIdUsed[measureId], $"Measure Id {measureId} is already in use.");
		Debug.Assert(_gl != null || cpuOnly, "Trying to create a gpu measure when the gpu profiling is not enabled.");
		_measureIdUsed[measureId] = true;
		_measureNames[measureId] = name;
		_measureInfos[measureId].HasGpuStats = !cpuOnly;
		_measureInfos[measureId].IsAlwaysEnabled = alwaysEnabled;
		_measureInfos[measureId].IsExternal = isExternal;
		if (!cpuOnly)
		{
			_gpuTimers[measureId].CreateStorage(useDoubleBuffering: true);
		}
		MeasureCount++;
	}

	public void SetMeasureEnabled(int measureId, bool enabled)
	{
		_measureInfos[measureId].IsEnabled = enabled;
	}

	public void StartMeasure(int measureId)
	{
		Debug.Assert(!_measureInfos[measureId].IsExternal);
		ref MeasureInfo reference = ref _measureInfos[measureId];
		if (reference.IsEnabled || reference.IsAlwaysEnabled)
		{
			ref MeasureAtStart reference2 = ref _measureAtStart[measureId];
			if (reference.HasGpuStats)
			{
				reference2.DrawCalls = _gl.DrawCallsCount;
				reference2.DrawnVertices = _gl.DrawnVertices;
				_gpuTimers[measureId].RequestStart();
			}
			reference2.ElapsedTime = (float)_stopwatch.Elapsed.TotalMilliseconds;
		}
	}

	public void StopMeasure(int measureId)
	{
		Debug.Assert(!_measureInfos[measureId].IsExternal);
		ref MeasureInfo reference = ref _measureInfos[measureId];
		if (!reference.IsEnabled && !reference.IsAlwaysEnabled)
		{
			return;
		}
		ref CPUMeasure reference2 = ref _cpuMeasures[measureId];
		ref GPUMeasure reference3 = ref _gpuMeasures[measureId];
		ref MeasureAtStart reference4 = ref _measureAtStart[measureId];
		bool flag = reference.AccumulatedFrameCount > _iterationsBeforeReset;
		if (flag)
		{
			reference.AccumulatedFrameCount = 0;
			reference2.AccumulatedElapsedTime = 0f;
			reference3.AccumulatedElapsedTime = 0f;
		}
		float num = (reference2.ElapsedTime = (float)_stopwatch.Elapsed.TotalMilliseconds - reference4.ElapsedTime);
		reference2.AccumulatedElapsedTime += num;
		reference2.MaxElapsedTime = System.Math.Max(reference2.MaxElapsedTime, num);
		if (reference.HasGpuStats)
		{
			reference3.DrawCalls = _gl.DrawCallsCount - reference4.DrawCalls;
			reference3.DrawnVertices = _gl.DrawnVertices - reference4.DrawnVertices;
			_gpuTimers[measureId].RequestStop();
			if (reference.AccumulatedFrameCount > 0 || flag)
			{
				_gpuTimers[measureId].FetchPreviousResultFromGPU();
				float num2 = (float)_gpuTimers[measureId].ElapsedTimeInMilliseconds;
				reference3.MaxElapsedTime = System.Math.Max(reference3.MaxElapsedTime, num2);
				reference3.AccumulatedElapsedTime += num2;
				reference3.ElapsedTime = num2;
			}
		}
		reference.AccumulatedFrameCount++;
	}

	public void RegisterExternalMeasure(int measureId, float cpuMeasuredTime, float gpuMeasuredTime = 0f)
	{
		Debug.Assert(_measureInfos[measureId].IsExternal);
		ref MeasureInfo reference = ref _measureInfos[measureId];
		ref CPUMeasure reference2 = ref _cpuMeasures[measureId];
		ref GPUMeasure reference3 = ref _gpuMeasures[measureId];
		if (reference.AccumulatedFrameCount > _iterationsBeforeReset)
		{
			reference.AccumulatedFrameCount = 0;
			reference2.AccumulatedElapsedTime = 0f;
			reference3.AccumulatedElapsedTime = 0f;
		}
		reference2.AccumulatedElapsedTime += cpuMeasuredTime;
		reference2.ElapsedTime = cpuMeasuredTime;
		reference2.MaxElapsedTime = System.Math.Max(cpuMeasuredTime, reference2.MaxElapsedTime);
		reference3.AccumulatedElapsedTime += gpuMeasuredTime;
		reference3.ElapsedTime = gpuMeasuredTime;
		reference3.MaxElapsedTime = System.Math.Max(gpuMeasuredTime, reference3.MaxElapsedTime);
		reference.AccumulatedFrameCount++;
	}

	public void ClearMeasure(int measureId)
	{
		_measureInfos[measureId].AccumulatedFrameCount = 0;
		_cpuMeasures[measureId].AccumulatedElapsedTime = 0f;
		_cpuMeasures[measureId].MaxElapsedTime = 0f;
		_gpuMeasures[measureId].AccumulatedElapsedTime = 0f;
		_gpuMeasures[measureId].MaxElapsedTime = 0f;
	}

	public void SkipMeasure(int measureId)
	{
		ref MeasureInfo reference = ref _measureInfos[measureId];
		if (reference.IsEnabled || reference.IsAlwaysEnabled)
		{
			ref CPUMeasure reference2 = ref _cpuMeasures[measureId];
			ref GPUMeasure reference3 = ref _gpuMeasures[measureId];
			reference.AccumulatedFrameCount = 0;
			reference2.AccumulatedElapsedTime = 0f;
			reference2.ElapsedTime = 0f;
			reference3.AccumulatedElapsedTime = 0f;
			reference3.ElapsedTime = 0f;
			reference3.DrawCalls = 0;
			reference3.DrawnVertices = 0;
		}
	}

	public string WriteMeasures()
	{
		string text = "Measure Name ............................ CPU avg (max) [ -- GPU avg -- Draw calls -- Triangles])";
		for (int i = 0; i < MeasureCount; i++)
		{
			if (_measureInfos[i].AccumulatedFrameCount > 1)
			{
				int num = i;
				float num2 = (float)System.Math.Round(_cpuMeasures[num].AccumulatedElapsedTime / (float)_measureInfos[num].AccumulatedFrameCount, 4);
				float num3 = (float)System.Math.Round(_cpuMeasures[num].MaxElapsedTime, 4);
				string text2 = "\n" + i + "." + _measureNames[num];
				for (int j = 0; (double)j < 64.0 - (double)text2.Length * 0.8; j++)
				{
					text2 += ".";
				}
				text2 += $"{num2} ({num3})";
				if (_measureInfos[i].HasGpuStats)
				{
					float num4 = (float)System.Math.Round(_gpuMeasures[num].AccumulatedElapsedTime / (float)_measureInfos[num].AccumulatedFrameCount, 4);
					int num5 = _gpuMeasures[num].DrawnVertices / 3;
					text2 += $" -- {num4} -- {_gpuMeasures[num].DrawCalls} -- {num5} => {(float)num5 / num4} tris/ms";
				}
				text += text2;
			}
		}
		return text;
	}
}
