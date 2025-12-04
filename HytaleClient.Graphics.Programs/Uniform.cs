using System;
using System.Runtime.CompilerServices;
using HytaleClient.Math;
using NLog;

namespace HytaleClient.Graphics.Programs;

internal struct Uniform
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private int _location;

	private static GLFunctions _gl;

	public string Name;

	private GPUProgram _program;

	private object _value;

	private string _failedAssertValueMessage;

	public bool IsValid => _location != -1;

	public static void InitializeGL(GLFunctions gl)
	{
		_gl = gl;
	}

	public static void ReleaseGL()
	{
		_gl = null;
	}

	public Uniform(int location, string name, GPUProgram program)
	{
		_location = location;
		Name = name;
		_program = program;
		_value = null;
		_failedAssertValueMessage = "Unexpected value for uniform " + name + " in program " + _program.GetType().Name + "!";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetValue(int value)
	{
		_program.AssertInUse();
		_value = value;
		_gl.Uniform1i(_location, value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetValue(int x, int y)
	{
		_program.AssertInUse();
		_gl.Uniform2i(_location, x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetValue(int x, int y, int z)
	{
		_program.AssertInUse();
		_gl.Uniform3i(_location, x, y, z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void SetValue(int[] values, int count)
	{
		_program.AssertInUse();
		_value = values;
		fixed (int* ptr = values)
		{
			_gl.Uniform1iv(_location, count, (IntPtr)ptr);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void SetValue(int[] values, int start, int count)
	{
		_program.AssertInUse();
		_value = values;
		fixed (int* ptr = values)
		{
			_gl.Uniform1iv(_location, count, IntPtr.Add((IntPtr)ptr, start * 4));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetValue(float value)
	{
		_program.AssertInUse();
		_value = value;
		_gl.Uniform1f(_location, value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void SetValue(float[] values)
	{
		_program.AssertInUse();
		_value = values;
		fixed (float* ptr = values)
		{
			_gl.Uniform1fv(_location, values.Length, (IntPtr)ptr);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetValue(Vector2 vector)
	{
		_program.AssertInUse();
		_value = vector;
		_gl.Uniform2f(_location, vector.X, vector.Y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void SetValue(Vector2[] vectors)
	{
		_program.AssertInUse();
		_value = vectors;
		fixed (Vector2* ptr = vectors)
		{
			_gl.Uniform2fv(_location, vectors.Length, (IntPtr)ptr);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetValue(float x, float y)
	{
		_program.AssertInUse();
		_value = new Vector2(x, y);
		_gl.Uniform2f(_location, x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetValue(Vector3 vector)
	{
		_program.AssertInUse();
		_value = vector;
		_gl.Uniform3f(_location, vector.X, vector.Y, vector.Z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void SetValue(Vector3[] vectors)
	{
		_program.AssertInUse();
		_value = vectors;
		fixed (Vector3* ptr = vectors)
		{
			_gl.Uniform3fv(_location, vectors.Length, (IntPtr)ptr);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void SetValue(Vector3[] vectors, int count)
	{
		_program.AssertInUse();
		_value = vectors;
		fixed (Vector3* ptr = vectors)
		{
			_gl.Uniform3fv(_location, count, (IntPtr)ptr);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void SetValue(Vector3[] vectors, int start, int count)
	{
		_program.AssertInUse();
		_value = vectors;
		fixed (Vector3* ptr = vectors)
		{
			_gl.Uniform3fv(_location, count, IntPtr.Add((IntPtr)ptr, start * sizeof(Vector3)));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetValue(float x, float y, float z)
	{
		_program.AssertInUse();
		_value = new Vector3(x, y, z);
		_gl.Uniform3f(_location, x, y, z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetValue(Vector4 vector)
	{
		_program.AssertInUse();
		_value = vector;
		_gl.Uniform4f(_location, vector.X, vector.Y, vector.Z, vector.W);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void SetValue(Vector4[] vectors)
	{
		_program.AssertInUse();
		_value = vectors;
		fixed (Vector4* ptr = vectors)
		{
			_gl.Uniform4fv(_location, vectors.Length, (IntPtr)ptr);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void SetValue(Vector4[] vectors, int count)
	{
		_program.AssertInUse();
		_value = vectors;
		fixed (Vector4* ptr = vectors)
		{
			_gl.Uniform4fv(_location, count, (IntPtr)ptr);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void SetValue(Vector4[] vectors, int start, int count)
	{
		_program.AssertInUse();
		_value = vectors;
		fixed (Vector4* ptr = vectors)
		{
			_gl.Uniform4fv(_location, count, IntPtr.Add((IntPtr)ptr, start * sizeof(Vector4)));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetValue(float x, float y, float z, float w)
	{
		_program.AssertInUse();
		_value = new Vector4(x, y, z, w);
		_gl.Uniform4f(_location, x, y, z, w);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void SetValue(ref Matrix matrix)
	{
		_program.AssertInUse();
		_value = matrix;
		fixed (Matrix* ptr = &matrix)
		{
			_gl.UniformMatrix4fv(_location, 1, transpose: false, (IntPtr)ptr);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void SetValue(Matrix[] matrices)
	{
		_program.AssertInUse();
		_value = matrices;
		fixed (Matrix* ptr = matrices)
		{
			_gl.UniformMatrix4fv(_location, matrices.Length, transpose: false, (IntPtr)ptr);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetValue(IntPtr matrix, int matrixCount)
	{
		_program.AssertInUse();
		_value = new Tuple<IntPtr, int>(matrix, matrixCount);
		_gl.UniformMatrix4fv(_location, matrixCount, transpose: false, matrix);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Reset()
	{
		_value = null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AssertValue(int value)
	{
		if (_value as int? != value)
		{
			throw new Exception(_failedAssertValueMessage);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AssertValue(float value)
	{
		if (_value as float? != value)
		{
			throw new Exception(_failedAssertValueMessage);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AssertValue(Vector2 vector)
	{
		if (_value as Vector2? != vector)
		{
			throw new Exception(_failedAssertValueMessage);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AssertValue(Vector3 vector)
	{
		if (_value as Vector3? != vector)
		{
			throw new Exception(_failedAssertValueMessage);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AssertValue(Vector4 vector)
	{
		if (_value as Vector4? != vector)
		{
			throw new Exception(_failedAssertValueMessage);
		}
	}
}
