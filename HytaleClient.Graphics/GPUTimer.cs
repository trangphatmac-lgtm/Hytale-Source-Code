#define DEBUG
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HytaleClient.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 34)]
internal struct GPUTimer
{
	private static GLFunctions _gl;

	private byte _maxBuffering;

	private byte _current;

	private ulong _time;

	private unsafe fixed uint _queries[6];

	public ulong ElapsedTime => _time;

	public double ElapsedTimeInMilliseconds => (double)_time / 1000000.0;

	public static void InitializeGL(GLFunctions gl)
	{
		_gl = gl;
	}

	public static void ReleaseGL()
	{
		_gl = null;
	}

	public unsafe void CreateStorage(bool useDoubleBuffering)
	{
		Debug.Assert(_maxBuffering == 0, "GPUTimer storage is already created - or never destroyed.");
		_maxBuffering = (byte)(useDoubleBuffering ? 2u : 3u);
		_current = 0;
		for (int i = 0; i < _maxBuffering; i++)
		{
			_queries[i * 2] = _gl.GenQuery();
			_queries[i * 2 + 1] = _gl.GenQuery();
		}
	}

	public unsafe void DestroyStorage()
	{
		Debug.Assert(_maxBuffering != 0, "GPUTimer storage was never created - or already destroyed.");
		for (int i = 0; i < _maxBuffering; i++)
		{
			_gl.DeleteQuery(_queries[i * 2]);
			_gl.DeleteQuery(_queries[i * 2 + 1]);
		}
		_maxBuffering = 0;
	}

	public void Swap()
	{
		Debug.Assert(_maxBuffering != 0, "GPUTimer storage was never created.");
		_current = (byte)((_current + 1) % _maxBuffering);
	}

	public unsafe void RequestStart()
	{
		Debug.Assert(_maxBuffering != 0, "GPUTimer storage was never created.");
		_gl.QueryCounter(_queries[_current * 2], GL.TIMESTAMP);
	}

	public unsafe void RequestStop()
	{
		Debug.Assert(_maxBuffering != 0, "GPUTimer storage was never created.");
		_gl.QueryCounter(_queries[_current * 2 + 1], GL.TIMESTAMP);
	}

	public unsafe void FetchPreviousResultFromGPU()
	{
		Debug.Assert(_maxBuffering != 0, "GPUTimer storage was never created.");
		byte b = (byte)((_current + 1) % _maxBuffering);
		_gl.GetQueryObjectui64v(_queries[b * 2], GL.QUERY_RESULT, out var param);
		_gl.GetQueryObjectui64v(_queries[b * 2 + 1], GL.QUERY_RESULT, out var param2);
		_time = param2 - param;
	}
}
