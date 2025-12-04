using System;
using System.Runtime.CompilerServices;

namespace HytaleClient.Graphics.Programs;

internal struct UniformBufferObject
{
	private uint _blockIndex;

	private uint _bindingPointIndex;

	public string Name;

	private static GLFunctions _gl;

	public static void InitializeGL(GLFunctions gl)
	{
		_gl = gl;
	}

	public static void ReleaseGL()
	{
		_gl = null;
	}

	public UniformBufferObject(GPUProgram program, uint blockIndex, string name)
	{
		_blockIndex = blockIndex;
		_bindingPointIndex = uint.MaxValue;
		Name = name;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetupBindingPoint(GPUProgram program, uint bindingPointIndex)
	{
		_bindingPointIndex = bindingPointIndex;
		if (_blockIndex != uint.MaxValue)
		{
			_gl.UniformBlockBinding(program.ProgramId, _blockIndex, bindingPointIndex);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBuffer(GLBuffer buffer)
	{
		_gl.BindBufferBase(GL.UNIFORM_BUFFER, _bindingPointIndex, buffer.InternalId);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBufferRange(GLBuffer buffer, uint offset, uint count)
	{
		_gl.BindBufferRange(GL.UNIFORM_BUFFER, _bindingPointIndex, buffer.InternalId, (IntPtr)offset, (IntPtr)count);
	}
}
