#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Math;

namespace HytaleClient.Graphics;

internal class RenderTarget : Disposable
{
	public enum Target
	{
		Depth,
		Color0,
		Color1,
		Color2,
		Color3,
		MAX
	}

	private struct TextureTargetData
	{
		public GLTexture Texture;

		public bool IsTextureExternal;

		public GL InternalFormat;

		public GL Format;

		public GL Type;

		public int MipLevelCount;

		public int SampleCount;
	}

	private int _width;

	private int _height;

	private float _invWidth;

	private float _invHeight;

	private static int MaxSampleCount;

	private static GLFunctions _gl;

	private readonly GLFramebuffer _framebuffer;

	private TextureTargetData[] _targetData = new TextureTargetData[5];

	private GL _maskClearBits;

	private static bool ForceUnbind;

	private string _name;

	public int Width => _width;

	public int Height => _height;

	public float InvWidth => _invWidth;

	public float InvHeight => _invHeight;

	public Vector2 Resolution => new Vector2(_width, _height);

	public Vector2 InvResolution => new Vector2(_invWidth, _invHeight);

	public static void InitializeGL(GLFunctions gl)
	{
		_gl = gl;
		int[] array = new int[1];
		gl.GetIntegerv(GL.MAX_SAMPLES, array);
		MaxSampleCount = array[0];
	}

	public static void ReleaseGL()
	{
		_gl = null;
	}

	public RenderTarget(int width, int height, string name)
	{
		_width = width;
		_height = height;
		_invWidth = (float)(1.0 / (double)width);
		_invHeight = (float)(1.0 / (double)height);
		_framebuffer = _gl.GenFramebuffer();
		_name = name;
	}

	public void Resize(int width, int height, bool forceResizeExternalTextures = false)
	{
		_width = width;
		_height = height;
		_invWidth = (float)(1.0 / (double)width);
		_invHeight = (float)(1.0 / (double)height);
		for (int i = 0; i < _targetData.Length; i++)
		{
			if (GLTexture.None != _targetData[i].Texture && (forceResizeExternalTextures || !_targetData[i].IsTextureExternal))
			{
				_gl.BindTexture(GL.TEXTURE_2D, _targetData[i].Texture);
				_gl.TexImage2D(GL.TEXTURE_2D, 0, (int)_targetData[i].InternalFormat, width, height, 0, _targetData[i].Format, _targetData[i].Type, IntPtr.Zero);
				if (_targetData[i].MipLevelCount > 1)
				{
					_gl.GenerateMipmap(GL.TEXTURE_2D);
				}
			}
		}
	}

	public void SetClearBits(bool clearColor, bool clearDepth, bool clearStencil)
	{
		GL gL = GL.NO_ERROR;
		if (clearColor)
		{
			gL |= GL.COLOR_BUFFER_BIT;
		}
		if (clearDepth)
		{
			gL |= GL.DEPTH_BUFFER_BIT;
		}
		if (clearStencil)
		{
			gL |= GL.STENCIL_BUFFER_BIT;
		}
		_maskClearBits = gL;
	}

	public void AddTexture(Target target, GL internalFormat, GL format, GL type, GL minFilter, GL magFilter, GL clampMode = GL.CLAMP_TO_EDGE, bool requestCompareModeForDepth = false, bool requestMipMapChain = false, int sampleCount = 1)
	{
		Debug.Assert(!requestMipMapChain || sampleCount <= 1, "RenderTarget cannot have both multisampling and mipmaps.");
		sampleCount = ((sampleCount < MaxSampleCount) ? sampleCount : MaxSampleCount);
		GL target2 = ((sampleCount > 1) ? GL.TEXTURE_2D_MULTISAMPLE : GL.TEXTURE_2D);
		GLTexture texture = _gl.GenTexture();
		_gl.BindTexture(target2, texture);
		if (sampleCount == 1)
		{
			_gl.TexParameteri(target2, GL.TEXTURE_MIN_FILTER, (int)minFilter);
			_gl.TexParameteri(target2, GL.TEXTURE_MAG_FILTER, (int)magFilter);
			_gl.TexParameteri(target2, GL.TEXTURE_WRAP_S, (int)clampMode);
			_gl.TexParameteri(target2, GL.TEXTURE_WRAP_T, (int)clampMode);
		}
		if (target == Target.Depth && requestCompareModeForDepth)
		{
			_gl.TexParameteri(target2, GL.TEXTURE_COMPARE_MODE, 34894);
			_gl.TexParameteri(target2, GL.TEXTURE_COMPARE_FUNC, 515);
		}
		if (clampMode == GL.CLAMP_TO_BORDER && sampleCount == 1)
		{
			float[] param = new float[4] { 1f, 1f, 1f, 1f };
			_gl.TexParameterfv(target2, GL.TEXTURE_BORDER_COLOR, param);
		}
		if (sampleCount == 1)
		{
			_gl.TexImage2D(GL.TEXTURE_2D, 0, (int)internalFormat, Width, Height, 0, format, type, IntPtr.Zero);
		}
		else
		{
			_gl.TexImage2DMultisample(GL.TEXTURE_2D_MULTISAMPLE, sampleCount, (int)internalFormat, Width, Height, fixedsamplelocations: false);
		}
		int num = 1;
		if (requestMipMapChain)
		{
			num += (int)System.Math.Floor(System.Math.Log(System.Math.Max(Width, Height), 2.0));
			_gl.GenerateMipmap(GL.TEXTURE_2D);
		}
		UseAsRenderTexture(texture, skipDispose: false, target, internalFormat, format, type, num, sampleCount);
	}

	public void UseAsRenderTexture(GLTexture texture, bool skipDispose, Target target, GL internalFormat, GL format, GL type, int levelCount = 1, int sampleCount = 1)
	{
		Debug.Assert(levelCount <= 1 || sampleCount <= 1, "RenderTarget cannot have both multisampling and mipmaps.");
		_targetData[(int)target].Texture = texture;
		_targetData[(int)target].IsTextureExternal = skipDispose;
		_targetData[(int)target].InternalFormat = internalFormat;
		_targetData[(int)target].Format = format;
		_targetData[(int)target].Type = type;
		_targetData[(int)target].MipLevelCount = levelCount;
		_targetData[(int)target].SampleCount = sampleCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public GLTexture GetTexture(Target target)
	{
		return _targetData[(int)target].Texture;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetTextureMipLevelCount(Target target)
	{
		return _targetData[(int)target].MipLevelCount;
	}

	public void FinalizeSetup()
	{
		_gl.BindFramebuffer(GL.FRAMEBUFFER, _framebuffer);
		if (GLTexture.None != _targetData[0].Texture)
		{
			GL attachment;
			switch (_targetData[0].InternalFormat)
			{
			case GL.DEPTH24_STENCIL8:
			case GL.DEPTH32F_STENCIL8:
				_maskClearBits |= GL.INVALID_ENUM;
				attachment = GL.DEPTH_STENCIL_ATTACHMENT;
				break;
			case GL.DEPTH_COMPONENT16:
			case GL.DEPTH_COMPONENT24:
			case GL.DEPTH_COMPONENT32:
			case GL.DEPTH_COMPONENT32F:
				_maskClearBits |= GL.DEPTH_BUFFER_BIT;
				attachment = GL.DEPTH_ATTACHMENT;
				break;
			default:
				throw new Exception("RenderTarget DepthTexture format not properly handled.");
			}
			GL textarget = ((_targetData[0].SampleCount > 1) ? GL.TEXTURE_2D_MULTISAMPLE : GL.TEXTURE_2D);
			_gl.FramebufferTexture2D(GL.FRAMEBUFFER, attachment, textarget, _targetData[0].Texture, 0);
		}
		for (int i = 1; i < 5; i++)
		{
			if (GLTexture.None != _targetData[i].Texture)
			{
				_maskClearBits |= GL.COLOR_BUFFER_BIT;
				GL attachment2 = (GL)(36064 + (i - 1));
				GL textarget2 = ((_targetData[i].SampleCount > 1) ? GL.TEXTURE_2D_MULTISAMPLE : GL.TEXTURE_2D);
				_gl.FramebufferTexture2D(GL.FRAMEBUFFER, attachment2, textarget2, _targetData[i].Texture, 0);
			}
		}
		SetupDrawBuffers();
		GL gL = _gl.CheckFramebufferStatus(GL.FRAMEBUFFER);
		if (gL != GL.FRAMEBUFFER_COMPLETE)
		{
			throw new Exception("Incomplete Framebuffer object, status: " + gL);
		}
		_gl.BindFramebuffer(GL.FRAMEBUFFER, GLFramebuffer.None);
	}

	public void SetupDrawBuffers()
	{
		int num = 0;
		GL[] array = new GL[4];
		for (int i = 1; i < 5; i++)
		{
			if (GLTexture.None != _targetData[i].Texture)
			{
				GL gL = (GL)(36064 + (i - 1));
				array[num] = gL;
				num++;
			}
		}
		_gl.DrawBuffers(num, array);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void BindHardwareFramebuffer()
	{
		_gl.BindFramebuffer(GL.FRAMEBUFFER, GLFramebuffer.None);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Bind(bool clear, bool setupViewport)
	{
		_gl.BindFramebuffer(GL.FRAMEBUFFER, _framebuffer);
		if (clear)
		{
			_gl.Clear(_maskClearBits);
		}
		if (setupViewport)
		{
			_gl.Viewport(0, 0, Width, Height);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Unbind()
	{
		if (ForceUnbind)
		{
			_gl.BindFramebuffer(GL.FRAMEBUFFER, GLFramebuffer.None);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyDepthStencilTo(RenderTarget destination, bool bindSource)
	{
		if (bindSource)
		{
			_gl.BindFramebuffer(GL.READ_FRAMEBUFFER, _framebuffer);
		}
		_gl.BindFramebuffer(GL.DRAW_FRAMEBUFFER, destination._framebuffer);
		_gl.BlitFramebuffer(0, 0, Width, Height, 0, 0, destination.Width, destination.Height, GL.INVALID_ENUM, GL.NEAREST);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyColorTo(RenderTarget destination, GL sourceColorAttachment, GL destinationColorAttachment, GL filteringMode, bool bindSource, bool rebindSourceAfter)
	{
		if (bindSource)
		{
			_gl.BindFramebuffer(GL.READ_FRAMEBUFFER, _framebuffer);
		}
		_gl.ReadBuffer(sourceColorAttachment);
		_gl.BindFramebuffer(GL.DRAW_FRAMEBUFFER, destination._framebuffer);
		GL[] buffers = new GL[4]
		{
			destinationColorAttachment,
			GL.NO_ERROR,
			GL.NO_ERROR,
			GL.NO_ERROR
		};
		_gl.DrawBuffers(1, buffers);
		_gl.BlitFramebuffer(0, 0, Width, Height, 0, 0, destination.Width, destination.Height, GL.COLOR_BUFFER_BIT, filteringMode);
		if (rebindSourceAfter)
		{
			_gl.BindFramebuffer(GL.DRAW_FRAMEBUFFER, _framebuffer);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyColorToScreen(int screenWidth, int screenHeight, GL sourceColorAttachment, GL filteringMode, bool bindSource, bool rebindSourceAfter)
	{
		if (bindSource)
		{
			_gl.BindFramebuffer(GL.READ_FRAMEBUFFER, _framebuffer);
		}
		_gl.ReadBuffer(sourceColorAttachment);
		_gl.BindFramebuffer(GL.DRAW_FRAMEBUFFER, GLFramebuffer.None);
		_gl.DrawBuffer(GL.BACK);
		_gl.BlitFramebuffer(0, 0, Width, Height, 0, 0, screenWidth, screenHeight, GL.COLOR_BUFFER_BIT, filteringMode);
		if (rebindSourceAfter)
		{
			_gl.BindFramebuffer(GL.DRAW_FRAMEBUFFER, _framebuffer);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ResolveTo(RenderTarget destination, GL sourceColorAttachment, GL destinationColorAttachment, GL filteringMode, bool bindSource, bool rebindSourceAfter)
	{
		CopyColorTo(destination, sourceColorAttachment, destinationColorAttachment, filteringMode, bindSource, rebindSourceAfter);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ResolveToScreen(int screenWidth, int screenHeight, GL sourceColorAttachment, GL filteringMode, bool bindSource, bool rebindSourceAfter)
	{
		CopyColorToScreen(screenWidth, screenHeight, sourceColorAttachment, filteringMode, bindSource, rebindSourceAfter);
	}

	protected override void DoDispose()
	{
		TextureTargetData[] targetData = _targetData;
		for (int i = 0; i < targetData.Length; i++)
		{
			TextureTargetData textureTargetData = targetData[i];
			if (GLTexture.None != textureTargetData.Texture && !textureTargetData.IsTextureExternal)
			{
				_gl.DeleteTexture(textureTargetData.Texture);
			}
		}
		_gl.DeleteFramebuffer(_framebuffer);
	}

	public unsafe byte[] ReadPixels(int colorTarget, GL readColorType, bool bindBeforeRead = false)
	{
		if (bindBeforeRead)
		{
			_gl.BindFramebuffer(GL.READ_FRAMEBUFFER, _framebuffer);
		}
		GL source = (GL)(36064 + (colorTarget - 1));
		_gl.ReadBuffer(source);
		int num = 4;
		int num2 = Width * Height;
		byte[] array = new byte[num2 * num];
		byte[] array2 = new byte[num2 * num];
		fixed (byte* ptr = array)
		{
			_gl.ReadPixels(0, 0, Width, Height, readColorType, _targetData[colorTarget].Type, (IntPtr)ptr);
		}
		fixed (byte* ptr2 = array)
		{
			for (int i = 0; i < Height; i++)
			{
				Marshal.Copy((IntPtr)ptr2 + i * Width * num, array2, (Height - i - 1) * Width * num, Width * num);
			}
		}
		return array2;
	}

	public unsafe void WriteToFile(string filePath, bool colors, bool depth, bool skipBind = false)
	{
		if (!skipBind)
		{
			_gl.BindFramebuffer(GL.FRAMEBUFFER, _framebuffer);
		}
		GL gL = _gl.CheckFramebufferStatus(GL.FRAMEBUFFER);
		if (gL != GL.FRAMEBUFFER_COMPLETE)
		{
			throw new Exception("Incomplete Framebuffer object, status: " + gL);
		}
		int num = Width * Height;
		if (depth && GLTexture.None != _targetData[0].Texture)
		{
			GL gL2 = GL.DEPTH_ATTACHMENT;
			int num2 = 4;
			float[] array = new float[num];
			byte[] array2 = new byte[num * num2];
			float[] data = new float[1];
			_gl.GetFloatv(GL.DEPTH_CLEAR_VALUE, data);
			fixed (float* ptr = array)
			{
				_gl.ReadPixels(0, 0, Width, Height, GL.DEPTH_COMPONENT, GL.FLOAT, (IntPtr)ptr);
			}
			for (int i = 0; i < Height; i++)
			{
				for (int j = 0; j < Width; j++)
				{
					int num3 = 4 * (i * Width + j);
					int num4 = (Height - i - 1) * Width + j;
					float num5 = array[num4];
					float num6 = 0.1f;
					float num7 = 16f;
					num5 = 2f * num6 / (num7 + num6 - num5 * (num7 - num6));
					array2[num3] = (byte)(num5 * 255f);
					array2[num3 + 1] = (byte)(num5 * num5 * num5 * 255f);
					array2[num3 + 2] = (byte)(num5 * num5 * num5 * 255f);
					array2[num3 + 3] = (byte)(num5 * 255f);
				}
			}
			new Image(Width, Height, array2).SavePNG(filePath + "_" + gL2.ToString() + ".png", 16711680u, 65280u, 255u, 0u);
		}
		if (colors)
		{
			for (int k = 1; k < 5; k++)
			{
				if (GLTexture.None != _targetData[k].Texture)
				{
					GL gL3 = (GL)(36064 + (k - 1));
					byte[] pixels = ReadPixels(k, GL.BGRA);
					new Image(Width, Height, pixels).SavePNG(filePath + "_" + gL3.ToString() + ".png", 16711680u, 65280u, 255u);
				}
			}
		}
		if (!skipBind)
		{
			_gl.BindFramebuffer(GL.FRAMEBUFFER, GLFramebuffer.None);
		}
	}
}
