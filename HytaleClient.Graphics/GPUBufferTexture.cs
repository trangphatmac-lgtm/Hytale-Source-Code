#define DEBUG
using System;
using System.Diagnostics;

namespace HytaleClient.Graphics;

public struct GPUBufferTexture
{
	private static GLFunctions _gl;

	private GPUBuffer _gpuBuffer;

	private GL _storageFormat;

	private GLTexture _bufferTextureCurrentRef;

	private GLTexture _bufferTexturePing;

	private GLTexture _bufferTexturePong;

	public GLBuffer CurrentBuffer => _gpuBuffer.Current;

	public GLTexture CurrentTexture => _bufferTextureCurrentRef;

	public bool UseDoubleBuffering => _gpuBuffer.UseDoubleBuffering;

	public static void InitializeGL(GLFunctions gl)
	{
		_gl = gl;
	}

	public static void ReleaseGL()
	{
		_gl = null;
	}

	public void CreateStorage(GL storageFormat, GL usage, bool useDoubleBuffering, uint size, uint growth, GPUBuffer.GrowthPolicy policy, uint sizeLimit = 0u)
	{
		Debug.Assert(_bufferTextureCurrentRef == GLTexture.None, "ERROR: attempt to CreateStorage for a GPUBufferTexture that already has storage allocated.");
		_gpuBuffer.CreateStorage(GL.TEXTURE_BUFFER, usage, useDoubleBuffering, size, growth, policy, sizeLimit);
		_storageFormat = storageFormat;
		_bufferTexturePing = _gl.GenTexture();
		_gl.BindTexture(GL.TEXTURE_BUFFER, _bufferTexturePing);
		_gl.TexBuffer(GL.TEXTURE_BUFFER, _storageFormat, _gpuBuffer.BufferPing.InternalId);
		if (UseDoubleBuffering)
		{
			_bufferTexturePong = _gl.GenTexture();
			_gl.BindTexture(GL.TEXTURE_BUFFER, _bufferTexturePong);
			_gl.TexBuffer(GL.TEXTURE_BUFFER, _storageFormat, _gpuBuffer.BufferPong.InternalId);
		}
		_bufferTextureCurrentRef = _bufferTexturePing;
	}

	public void DestroyStorage()
	{
		Debug.Assert(_bufferTextureCurrentRef != GLTexture.None, "ERROR: attempt to DestroyStorage for a GPUBufferTexture that has no storage allocated.");
		_gl.DeleteTexture(_bufferTexturePong);
		if (UseDoubleBuffering)
		{
			_gl.DeleteTexture(_bufferTexturePing);
		}
		_bufferTextureCurrentRef = (_bufferTexturePing = (_bufferTexturePong = GLTexture.None));
		_gpuBuffer.DestroyStorage();
	}

	public void Swap()
	{
		Debug.Assert(UseDoubleBuffering, "ERROR: trying to swap a single buffered GPUBuffer");
		_gpuBuffer.Swap();
		_bufferTextureCurrentRef = ((_bufferTextureCurrentRef == _bufferTexturePing) ? _bufferTexturePong : _bufferTexturePing);
	}

	public void GrowStorageIfNecessary(uint transferSize)
	{
		_gpuBuffer.GrowStorageIfNecessary(transferSize);
	}

	public IntPtr BeginTransfer(uint transferSize)
	{
		return _gpuBuffer.BeginTransfer(transferSize);
	}

	public void EndTransfer()
	{
		_gpuBuffer.EndTransfer();
	}

	public void TransferCopy(IntPtr cpuDataPtr, uint transferSize, uint destinationOffset = 0u)
	{
		_gpuBuffer.TransferCopy(cpuDataPtr, transferSize, destinationOffset);
	}
}
