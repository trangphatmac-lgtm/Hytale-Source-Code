#define DEBUG
using System;
using System.Diagnostics;

namespace HytaleClient.Graphics;

public struct GPUBuffer
{
	public enum GrowthPolicy
	{
		GrowthAutoNoLimit,
		GrowthAutoWithLimit,
		GrowthManual,
		Never
	}

	private static GLFunctions _gl;

	private GLBuffer _bufferCurrentRef;

	private GLBuffer _bufferPing;

	private GLBuffer _bufferPong;

	private GL _targetType;

	private GL _usageType;

	private bool _useDoubleBuffering;

	private uint _size;

	private uint _growth;

	private uint _sizeLimit;

	private GrowthPolicy _growthPolicy;

	private bool _isTransfering;

	public GLBuffer Current => _bufferCurrentRef;

	public GLBuffer BufferPing => _bufferPing;

	public GLBuffer BufferPong => _bufferPong;

	public bool UseDoubleBuffering => _useDoubleBuffering;

	public static void InitializeGL(GLFunctions gl)
	{
		_gl = gl;
	}

	public static void ReleaseGL()
	{
		_gl = null;
	}

	public void CreateStorage(GL target, GL usage, bool useDoubleBuffering, uint size, uint growth, GrowthPolicy policy, uint sizeLimit = 0u)
	{
		Debug.Assert(_bufferCurrentRef == GLBuffer.None, "ERROR: attempt to CreateStorage for a GPUBuffer that already has storage allocated.");
		_targetType = target;
		_usageType = usage;
		_size = size;
		_growth = growth;
		_growthPolicy = policy;
		_sizeLimit = sizeLimit;
		_useDoubleBuffering = useDoubleBuffering;
		_bufferPing = _gl.GenBuffer();
		_gl.BindBuffer(_targetType, _bufferPing);
		_gl.BufferData(_targetType, (IntPtr)_size, IntPtr.Zero, _usageType);
		if (_useDoubleBuffering)
		{
			_bufferPong = _gl.GenBuffer();
			_gl.BindBuffer(_targetType, _bufferPong);
			_gl.BufferData(_targetType, (IntPtr)_size, IntPtr.Zero, _usageType);
		}
		if (_targetType == GL.PIXEL_UNPACK_BUFFER)
		{
			_gl.BindBuffer(GL.PIXEL_UNPACK_BUFFER, GLBuffer.None);
		}
		_bufferCurrentRef = _bufferPing;
	}

	public void DestroyStorage()
	{
		Debug.Assert(_bufferCurrentRef != GLBuffer.None, "ERROR: attempt to DestroyStorage for a GPUBuffer that has no storage allocated.");
		_gl.DeleteBuffer(_bufferPong);
		if (UseDoubleBuffering)
		{
			_gl.DeleteBuffer(_bufferPing);
		}
		_bufferCurrentRef = (_bufferPing = (_bufferPong = GLBuffer.None));
	}

	public void Swap()
	{
		Debug.Assert(UseDoubleBuffering, "ERROR: trying to swap a single buffered GPUBuffer");
		_bufferCurrentRef = ((_bufferCurrentRef == _bufferPing) ? _bufferPong : _bufferPing);
	}

	public bool GrowStorageIfNecessary(uint transferSize)
	{
		Debug.Assert(_growthPolicy != GrowthPolicy.Never, "ERROR: GPUBuffer w/ GrowthPolicy 'Never' tried to increase its size.");
		bool result = false;
		if (transferSize > _size)
		{
			_size += System.Math.Max(_growth, transferSize - _size);
			_size = ((_growthPolicy == GrowthPolicy.GrowthAutoWithLimit) ? System.Math.Min(_size, _sizeLimit) : _size);
			_gl.BindBuffer(_targetType, _bufferPing);
			_gl.BufferData(_targetType, (IntPtr)_size, IntPtr.Zero, _usageType);
			if (UseDoubleBuffering)
			{
				_gl.BindBuffer(_targetType, _bufferPong);
				_gl.BufferData(_targetType, (IntPtr)_size, IntPtr.Zero, _usageType);
			}
			result = true;
			if (_targetType == GL.PIXEL_UNPACK_BUFFER)
			{
				_gl.BindBuffer(GL.PIXEL_UNPACK_BUFFER, GLBuffer.None);
			}
		}
		return result;
	}

	public IntPtr BeginTransfer(uint transferSize, uint transferStartOffset = 0u)
	{
		Debug.Assert(!_isTransfering, "Trying to call BeginTransfer() but a transfer was in progres already. Are you missing a call to EndTransfer()?");
		_isTransfering = true;
		Debug.Assert(transferSize != 0, "Trying to transfer 0 data will cause a GL_INVALID_OPEARTION.");
		Debug.Assert(transferStartOffset == 0 || (_growthPolicy != 0 && _growthPolicy != GrowthPolicy.GrowthAutoWithLimit), "Trying to transfer data w/ an offset & while using auto growth is unsafe, and should be avoided. Make sure you grow your buffer manually if you want to Transfer its content in more than one batch.");
		if (_growthPolicy == GrowthPolicy.GrowthAutoNoLimit || _growthPolicy == GrowthPolicy.GrowthAutoWithLimit)
		{
			GrowStorageIfNecessary(transferSize);
		}
		GL access = (UseDoubleBuffering ? ((GL)34u) : ((GL)10u));
		_gl.BindBuffer(_targetType, _bufferCurrentRef);
		return _gl.MapBufferRange(_targetType, (IntPtr)transferStartOffset, (IntPtr)transferSize, access);
	}

	public void EndTransfer()
	{
		Debug.Assert(_isTransfering, "Trying to call EndTransfer() but no transfer was in progres. Are you missing a call to BeginTransfer()?");
		_isTransfering = false;
		_gl.UnmapBuffer(_targetType);
		if (_targetType == GL.PIXEL_UNPACK_BUFFER)
		{
			_gl.BindBuffer(GL.PIXEL_UNPACK_BUFFER, GLBuffer.None);
		}
	}

	public void TransferCopy(IntPtr cpuDataPtr, uint transferSize, uint destinationOffset = 0u)
	{
		if (_growthPolicy != GrowthPolicy.GrowthManual && _growthPolicy != GrowthPolicy.Never)
		{
			GrowStorageIfNecessary(transferSize);
		}
		_gl.BindBuffer(_targetType, _bufferCurrentRef);
		_gl.BufferSubData(_targetType, (IntPtr)destinationOffset, (IntPtr)transferSize, cpuDataPtr);
	}

	public void UnpackToTexture2D(GLTexture texture, int level, int xoffset, int yoffset, int width, int height, GL format, GL type)
	{
		Debug.Assert(_targetType == GL.PIXEL_UNPACK_BUFFER, "ERROR: attempt to transfer data via PBO w/ a Buffer that is not ready for it.");
		_gl.BindBuffer(_targetType, _bufferCurrentRef);
		_gl.BindTexture(GL.TEXTURE_2D, texture);
		_gl.TexSubImage2D(GL.TEXTURE_2D, level, xoffset, yoffset, width, height, format, type, IntPtr.Zero);
		_gl.BindBuffer(_targetType, GLBuffer.None);
	}

	public void UnpackToTexture3D(GLTexture texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, GL format, GL type)
	{
		Debug.Assert(_targetType == GL.PIXEL_UNPACK_BUFFER, "ERROR: attempt to transfer data via PBO w/ a Buffer that is not ready for it.");
		_gl.BindBuffer(_targetType, _bufferCurrentRef);
		_gl.BindTexture(GL.TEXTURE_3D, texture);
		_gl.TexSubImage3D(GL.TEXTURE_3D, level, xoffset, yoffset, zoffset, width, height, depth, format, type, IntPtr.Zero);
		_gl.BindBuffer(_targetType, GLBuffer.None);
	}
}
