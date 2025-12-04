#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Graphics;

internal class AnimationSystem
{
	public enum TransferMethod
	{
		Sequential,
		ParallelSeparate,
		ParallelInterleaved,
		MAX
	}

	private struct AnimationTask
	{
		public AnimatedRenderer Renderer;

		public bool SkipUpdate;
	}

	private const int AnimationTasksDefaultSize = 500;

	private const int AnimationTasksGrowth = 250;

	private int _incomingAnimationTaskCount;

	private int _animationTaskCount;

	private AnimationTask[] _animationTasks = new AnimationTask[500];

	private TransferMethod _transferMethodId;

	private int _processedAnimationTaskCount;

	private uint _processedNodeTransferSize;

	private uint _nodeTransferSize;

	private bool _useDoubleBuffering = true;

	private GPUBuffer _nodeBuffer;

	private const uint NodeBufferDefaultSize = 16384000u;

	private const uint NodeBufferGrowth = 8192000u;

	private uint _nodeBufferSize = 16384000u;

	private readonly int UniformBufferOffsetAlignment;

	private bool _isEnabled = true;

	private bool _useParallelExecution = true;

	private readonly GLFunctions _gl;

	public bool HasProcessed { get; private set; } = false;


	public bool IsEnabled => _isEnabled;

	public GLBuffer NodeBuffer => _nodeBuffer.Current;

	public void SetEnabled(bool enable)
	{
		_isEnabled = enable;
	}

	public bool UseParallelExecution(bool enable)
	{
		return _useParallelExecution = enable;
	}

	public void SetTransferMethod(TransferMethod methodId)
	{
		Debug.Assert(methodId < TransferMethod.MAX, $"Animation data transfer methodId {methodId} is invalid. We only have 3 methods to transfer the animation data.");
		_transferMethodId = methodId;
	}

	public AnimationSystem(GLFunctions gl)
	{
		_gl = gl;
		int[] array = new int[1];
		_gl.GetIntegerv(GL.UNIFORM_BUFFER_OFFSET_ALIGNMENT, array);
		UniformBufferOffsetAlignment = array[0];
		CreateGPUData();
	}

	public void Dispose()
	{
		DestroyGPUData();
	}

	private void CreateGPUData()
	{
		_nodeBuffer.CreateStorage(GL.UNIFORM_BUFFER, GL.STREAM_DRAW, _useDoubleBuffering, _nodeBufferSize, 8192000u, GPUBuffer.GrowthPolicy.GrowthManual);
	}

	private void DestroyGPUData()
	{
		_nodeBuffer.DestroyStorage();
	}

	public void BeginFrame()
	{
		_animationTaskCount = 0;
		_incomingAnimationTaskCount = 0;
		_nodeTransferSize = 0u;
		_processedAnimationTaskCount = 0;
		_processedNodeTransferSize = 0u;
		PingPongBuffers();
		HasProcessed = false;
	}

	private void PingPongBuffers()
	{
		if (_useDoubleBuffering)
		{
			_nodeBuffer.Swap();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PrepareForIncomingTasks(int size)
	{
		_incomingAnimationTaskCount += size;
		ArrayUtils.GrowArrayIfNecessary(ref _animationTasks, _incomingAnimationTaskCount, 250);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterAnimationTask(AnimatedRenderer renderer, bool skipUpdate)
	{
		Debug.Assert(!renderer.SelfManagedNodeBuffer, "AnimationSystem cannot process an AnimatedRenderer that is self managing its node buffer.");
		_animationTasks[_animationTaskCount].Renderer = renderer;
		_animationTasks[_animationTaskCount].SkipUpdate = skipUpdate;
		_animationTaskCount++;
		renderer.NodeBufferOffset = ReserveStorage(renderer.NodeCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private uint ReserveStorage(uint nodeCount)
	{
		uint nodeTransferSize = _nodeTransferSize;
		_nodeTransferSize += nodeCount * 64;
		long num = _nodeTransferSize % UniformBufferOffsetAlignment;
		_nodeTransferSize += (uint)(int)((num != 0L) ? (UniformBufferOffsetAlignment - num) : 0);
		return nodeTransferSize;
	}

	private unsafe void SendDataToGPU(bool sendInParallel = false)
	{
		uint num = _nodeTransferSize - _processedNodeTransferSize;
		if (num == 0)
		{
			return;
		}
		bool flag = _nodeBuffer.GrowStorageIfNecessary(_nodeTransferSize);
		num = (flag ? _nodeTransferSize : num);
		uint transferStartOffset = ((!flag) ? _processedNodeTransferSize : 0u);
		int num2 = ((!flag) ? _processedAnimationTaskCount : 0);
		IntPtr dataPtr = _nodeBuffer.BeginTransfer(num, transferStartOffset);
		Debug.Assert(dataPtr != IntPtr.Zero);
		if (!sendInParallel)
		{
			Matrix* ptr = (Matrix*)dataPtr.ToPointer();
			for (int j = num2; j < _animationTaskCount; j++)
			{
				AnimationTask animationTask = _animationTasks[j];
				uint num3 = (animationTask.Renderer.NodeBufferOffset - _processedNodeTransferSize) / 64;
				for (int k = 0; k < animationTask.Renderer.NodeCount; k++)
				{
					ptr[num3 + k] = animationTask.Renderer.NodeMatrices[k];
				}
			}
		}
		else
		{
			Parallel.For(num2, _animationTaskCount, delegate(int i)
			{
				Matrix* ptr2 = (Matrix*)dataPtr.ToPointer();
				AnimationTask animationTask2 = _animationTasks[i];
				uint num4 = (animationTask2.Renderer.NodeBufferOffset - _processedNodeTransferSize) / 64;
				for (int l = 0; l < animationTask2.Renderer.NodeCount; l++)
				{
					ptr2[num4 + l] = animationTask2.Renderer.NodeMatrices[l];
				}
			});
		}
		_nodeBuffer.EndTransfer();
	}

	private void ProcessAnimationTask(int i)
	{
		if (!_animationTasks[i].SkipUpdate)
		{
			_animationTasks[i].Renderer.UpdatePose();
		}
	}

	private void ProcessAnimationTasksOnSingleCore()
	{
		if (_isEnabled)
		{
			for (int i = _processedAnimationTaskCount; i < _animationTaskCount; i++)
			{
				ProcessAnimationTask(i);
			}
		}
		SendDataToGPU();
		HasProcessed = true;
	}

	private unsafe void ProcessAnimationTasksOnMultiCore()
	{
		switch (_transferMethodId)
		{
		case TransferMethod.Sequential:
		case TransferMethod.ParallelSeparate:
			if (_isEnabled)
			{
				Parallel.For(_processedAnimationTaskCount, _animationTaskCount, delegate(int i)
				{
					ProcessAnimationTask(i);
				});
			}
			SendDataToGPU(_transferMethodId != TransferMethod.Sequential);
			break;
		case TransferMethod.ParallelInterleaved:
		{
			uint num = _nodeTransferSize - _processedNodeTransferSize;
			if (num == 0)
			{
				break;
			}
			bool flag = _nodeBuffer.GrowStorageIfNecessary(_nodeTransferSize);
			num = (flag ? _nodeTransferSize : num);
			uint transferStartOffset = ((!flag) ? _processedNodeTransferSize : 0u);
			int fromInclusive = ((!flag) ? _processedAnimationTaskCount : 0);
			IntPtr dataPtr = _nodeBuffer.BeginTransfer(num, transferStartOffset);
			if (_isEnabled)
			{
				Parallel.For(fromInclusive, _animationTaskCount, delegate(int i)
				{
					if (i >= _processedAnimationTaskCount)
					{
						ProcessAnimationTask(i);
					}
					Matrix* ptr2 = (Matrix*)dataPtr.ToPointer();
					AnimationTask animationTask2 = _animationTasks[i];
					uint num3 = (animationTask2.Renderer.NodeBufferOffset - _processedNodeTransferSize) / 64;
					for (int k = 0; k < animationTask2.Renderer.NodeCount; k++)
					{
						ptr2[num3 + k] = animationTask2.Renderer.NodeMatrices[k];
					}
				});
			}
			else
			{
				Parallel.For(fromInclusive, _animationTaskCount, delegate(int i)
				{
					Matrix* ptr = (Matrix*)dataPtr.ToPointer();
					AnimationTask animationTask = _animationTasks[i];
					uint num2 = (animationTask.Renderer.NodeBufferOffset - _processedNodeTransferSize) / 64;
					for (int j = 0; j < animationTask.Renderer.NodeCount; j++)
					{
						ptr[num2 + j] = animationTask.Renderer.NodeMatrices[j];
					}
				});
			}
			_nodeBuffer.EndTransfer();
			break;
		}
		}
		HasProcessed = true;
	}

	public void ProcessAnimationTasks()
	{
		if (_useParallelExecution)
		{
			ProcessAnimationTasksOnMultiCore();
		}
		else
		{
			ProcessAnimationTasksOnSingleCore();
		}
		_processedAnimationTaskCount = _animationTaskCount;
		_processedNodeTransferSize = _nodeTransferSize;
	}

	public void ProcessHitBlockAnimation(float hitTimer, ref Matrix baseMatrix, out Matrix animatedMatrix)
	{
		Matrix matrix = Matrix.CreateScale(0.98f);
		if (hitTimer > 0.3f)
		{
			animatedMatrix = Matrix.CreateScale(0f);
		}
		else if (hitTimer > 0f)
		{
			float num = hitTimer / 0.3f;
			float num2 = num * ((float)System.Math.PI * 3f);
			float num3 = 0.04f * (1f - (float)System.Math.Pow(num - 1f, 4.0));
			float pitch = num3 * (float)System.Math.Sin(num2);
			float roll = num3 * (float)System.Math.Cos(num2);
			Matrix.CreateFromYawPitchRoll(0f, pitch, roll, out animatedMatrix);
			Matrix matrix2 = Matrix.CreateTranslation(0f, 16f, 0f);
			Matrix matrix3 = Matrix.Invert(matrix2);
			Matrix.Multiply(ref matrix3, ref animatedMatrix, out animatedMatrix);
			Matrix.Multiply(ref animatedMatrix, ref matrix2, out animatedMatrix);
			Matrix.Multiply(ref matrix, ref animatedMatrix, out animatedMatrix);
		}
		else
		{
			animatedMatrix = Matrix.Identity;
		}
		Matrix.Multiply(ref animatedMatrix, ref baseMatrix, out animatedMatrix);
	}
}
