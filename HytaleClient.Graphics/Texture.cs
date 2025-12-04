#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Core;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Graphics;

public class Texture : Disposable
{
	public enum TextureTypes
	{
		Texture2D,
		Texture2DArray,
		Texture3D,
		TextureCubemap
	}

	public readonly TextureTypes TextureType;

	public const int DefaultAtlasWidth = 2048;

	public const int MinimumAtlasHeight = 32;

	public const int DefaultMipmapLevelCount = 5;

	protected static GLFunctions _gl;

	private int _width;

	private int _height;

	private int _depth;

	private int _layerCount;

	private int _mipmapLevelCount;

	private GL _internalFormat;

	private GL _format;

	private GL _type;

	private bool _requestMipMapChain;

	public int Width => _width;

	public int Height => _height;

	public int MipmapLevelCount => _mipmapLevelCount;

	public GLTexture GLTexture { get; protected set; }

	public static void InitializeGL(GLFunctions gl)
	{
		_gl = gl;
	}

	public static void ReleaseGL()
	{
		_gl = null;
	}

	public Texture(TextureTypes type)
	{
		TextureType = type;
	}

	public unsafe void CreateTexture2D(int width, int height, byte[] pixels = null, int mipmapLevelCount = 5, GL minFilter = GL.NEAREST, GL magFilter = GL.NEAREST, GL wrapS = GL.CLAMP_TO_EDGE, GL wrapT = GL.CLAMP_TO_EDGE, GL internalFormat = GL.RGBA, GL format = GL.RGBA, GL type = GL.UNSIGNED_BYTE, bool requestMipMapChain = false)
	{
		Debug.Assert(TextureType == TextureTypes.Texture2D);
		_width = width;
		_height = height;
		_mipmapLevelCount = mipmapLevelCount;
		_internalFormat = internalFormat;
		_format = format;
		_type = type;
		_requestMipMapChain = requestMipMapChain;
		GLTexture = _gl.GenTexture();
		_gl.BindTexture(GL.TEXTURE_2D, GLTexture);
		_gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAX_LEVEL, _mipmapLevelCount);
		_gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, (int)minFilter);
		_gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, (int)magFilter);
		_gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, (int)wrapS);
		_gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, (int)wrapT);
		fixed (byte* ptr = pixels)
		{
			_gl.TexImage2D(GL.TEXTURE_2D, 0, (int)internalFormat, width, height, 0, format, type, (IntPtr)ptr);
		}
		if (_requestMipMapChain)
		{
			_gl.GenerateMipmap(GL.TEXTURE_2D);
		}
	}

	public unsafe void UpdateTexture2DMipMaps(byte[][] pixelsPerMipmapLevel)
	{
		Debug.Assert(TextureType == TextureTypes.Texture2D);
		Debug.Assert(ThreadHelper.IsMainThread());
		if (base.Disposed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
		_height = pixelsPerMipmapLevel[0].Length / _width / 4;
		int num = _width;
		int num2 = _height;
		_gl.BindTexture(GL.TEXTURE_2D, GLTexture);
		for (int i = 0; i < pixelsPerMipmapLevel.Length; i++)
		{
			fixed (byte* ptr = pixelsPerMipmapLevel[i])
			{
				_gl.TexImage2D(GL.TEXTURE_2D, i, (int)_internalFormat, num, num2, 0, _format, _type, (IntPtr)ptr);
			}
			num = System.Math.Max(num / 2, 1);
			num2 = System.Math.Max(num2 / 2, 1);
		}
	}

	public unsafe void UpdateTexture2D(byte[] pixels)
	{
		Debug.Assert(TextureType == TextureTypes.Texture2D);
		Debug.Assert(ThreadHelper.IsMainThread());
		_gl.BindTexture(GL.TEXTURE_2D, GLTexture);
		fixed (byte* ptr = pixels)
		{
			_gl.TexSubImage2D(GL.TEXTURE_2D, 0, 0, 0, _width, _height, _format, _type, (IntPtr)ptr);
		}
		if (_requestMipMapChain)
		{
			_gl.GenerateMipmap(GL.TEXTURE_2D);
		}
	}

	public unsafe void CreateTextureCubemap(int width, int height, byte[][] pixels, int mipmapLevelCount, GL minFilter, GL magFilter, GL wrapS, GL wrapT, GL wrapR, GL internalFormat = GL.RGBA, GL format = GL.RGBA, GL type = GL.UNSIGNED_BYTE, bool requestMipMapChain = false)
	{
		Debug.Assert(TextureType == TextureTypes.TextureCubemap);
		_width = width;
		_height = height;
		_mipmapLevelCount = mipmapLevelCount;
		_internalFormat = internalFormat;
		_format = format;
		_type = type;
		_requestMipMapChain = requestMipMapChain;
		GLTexture = _gl.GenTexture();
		_gl.BindTexture(GL.TEXTURE_CUBE_MAP, GLTexture);
		_gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAX_LEVEL, _mipmapLevelCount);
		_gl.TexParameteri(GL.TEXTURE_CUBE_MAP, GL.TEXTURE_MIN_FILTER, (int)minFilter);
		_gl.TexParameteri(GL.TEXTURE_CUBE_MAP, GL.TEXTURE_MAG_FILTER, (int)magFilter);
		_gl.TexParameteri(GL.TEXTURE_CUBE_MAP, GL.TEXTURE_WRAP_S, (int)wrapS);
		_gl.TexParameteri(GL.TEXTURE_CUBE_MAP, GL.TEXTURE_WRAP_T, (int)wrapT);
		_gl.TexParameteri(GL.TEXTURE_CUBE_MAP, GL.TEXTURE_WRAP_R, (int)wrapR);
		fixed (byte* ptr = pixels[0])
		{
			fixed (byte* ptr2 = pixels[1])
			{
				fixed (byte* ptr3 = pixels[2])
				{
					fixed (byte* ptr4 = pixels[3])
					{
						fixed (byte* ptr6 = pixels[4])
						{
							fixed (byte* ptr5 = pixels[5])
							{
								_gl.TexImage2D(GL.TEXTURE_CUBE_MAP_POSITIVE_X, 0, (int)internalFormat, width, height, 0, format, type, (IntPtr)ptr);
								_gl.TexImage2D((GL)34070u, 0, (int)internalFormat, width, height, 0, format, type, (IntPtr)ptr2);
								_gl.TexImage2D((GL)34071u, 0, (int)internalFormat, width, height, 0, format, type, (IntPtr)ptr3);
								_gl.TexImage2D((GL)34072u, 0, (int)internalFormat, width, height, 0, format, type, (IntPtr)ptr4);
								_gl.TexImage2D((GL)34074u, 0, (int)internalFormat, width, height, 0, format, type, (IntPtr)ptr5);
								_gl.TexImage2D((GL)34073u, 0, (int)internalFormat, width, height, 0, format, type, (IntPtr)ptr6);
							}
						}
					}
				}
			}
		}
		if (requestMipMapChain)
		{
			_gl.GenerateMipmap(GL.TEXTURE_CUBE_MAP);
		}
	}

	public void CreateTexture3D(int width, int height, int depth, IntPtr data, GL minFilter, GL magFilter, GL wrapS, GL wrapT, GL wrapR, GL type, GL internalFormat, GL format, bool requestMipMapChain = false)
	{
		Debug.Assert(TextureType == TextureTypes.Texture3D);
		_width = width;
		_height = height;
		_depth = depth;
		_internalFormat = internalFormat;
		_format = format;
		_type = type;
		_requestMipMapChain = requestMipMapChain;
		GLTexture = _gl.GenTexture();
		_gl.BindTexture(GL.TEXTURE_3D, GLTexture);
		_gl.TexParameteri(GL.TEXTURE_3D, GL.TEXTURE_MIN_FILTER, (int)minFilter);
		_gl.TexParameteri(GL.TEXTURE_3D, GL.TEXTURE_MAG_FILTER, (int)magFilter);
		_gl.TexParameteri(GL.TEXTURE_3D, GL.TEXTURE_WRAP_S, (int)wrapS);
		_gl.TexParameteri(GL.TEXTURE_3D, GL.TEXTURE_WRAP_T, (int)wrapT);
		_gl.TexParameteri(GL.TEXTURE_3D, GL.TEXTURE_WRAP_R, (int)wrapR);
		_gl.TexImage3D(GL.TEXTURE_3D, 0, (int)internalFormat, width, height, depth, 0, format, type, data);
		if (_requestMipMapChain)
		{
			_gl.GenerateMipmap(GL.TEXTURE_3D);
		}
	}

	public unsafe void UpdateTexture3D(uint width, uint height, uint depth, ushort[] data)
	{
		Debug.Assert(TextureType == TextureTypes.Texture3D);
		bool flag = width != _width || height != _height || depth != _depth;
		if (flag)
		{
			_width = (int)width;
			_height = (int)height;
			_depth = (int)depth;
		}
		_gl.BindTexture(GL.TEXTURE_3D, GLTexture);
		fixed (ushort* ptr = data)
		{
			if (flag)
			{
				_gl.TexImage3D(GL.TEXTURE_3D, 0, (int)_internalFormat, (int)width, (int)height, (int)depth, 0, _format, _type, (IntPtr)ptr);
			}
			else
			{
				_gl.TexSubImage3D(GL.TEXTURE_3D, 0, 0, 0, 0, (int)width, (int)height, (int)depth, _format, _type, (IntPtr)ptr);
			}
		}
		if (_requestMipMapChain)
		{
			_gl.GenerateMipmap(GL.TEXTURE_3D);
		}
	}

	public unsafe void CreateTexture2DArray(int width, int height, int layerCount, byte[] pixels = null, GL minFilter = GL.NEAREST, GL magFilter = GL.NEAREST, GL wrapS = GL.CLAMP_TO_EDGE, GL wrapT = GL.CLAMP_TO_EDGE, GL internalFormat = GL.RGBA, GL format = GL.RGBA, GL type = GL.UNSIGNED_BYTE, bool requestMipMapChain = false)
	{
		Debug.Assert(TextureType == TextureTypes.Texture2DArray);
		_width = width;
		_height = height;
		_layerCount = layerCount;
		_internalFormat = internalFormat;
		_format = format;
		_type = type;
		_requestMipMapChain = requestMipMapChain;
		GLTexture = _gl.GenTexture();
		_gl.BindTexture(GL.TEXTURE_2D_ARRAY, GLTexture);
		_gl.TexParameteri(GL.TEXTURE_2D_ARRAY, GL.TEXTURE_MIN_FILTER, (int)minFilter);
		_gl.TexParameteri(GL.TEXTURE_2D_ARRAY, GL.TEXTURE_MAG_FILTER, (int)magFilter);
		_gl.TexParameteri(GL.TEXTURE_2D_ARRAY, GL.TEXTURE_WRAP_S, (int)wrapS);
		_gl.TexParameteri(GL.TEXTURE_2D_ARRAY, GL.TEXTURE_WRAP_T, (int)wrapT);
		fixed (byte* ptr = pixels)
		{
			_gl.TexImage3D(GL.TEXTURE_2D_ARRAY, 0, (int)internalFormat, width, height, layerCount, 0, format, type, (IntPtr)ptr);
		}
		if (_requestMipMapChain)
		{
			_gl.GenerateMipmap(GL.TEXTURE_2D_ARRAY);
		}
	}

	public unsafe void UpdateTexture2DArray(int width, int height, int layerCount, byte[] pixels)
	{
		Debug.Assert(TextureType == TextureTypes.Texture2DArray);
		bool flag = width != _width || height != _height || layerCount != _layerCount;
		if (flag)
		{
			_width = width;
			_height = height;
			_layerCount = layerCount;
		}
		_gl.BindTexture(GL.TEXTURE_2D_ARRAY, GLTexture);
		fixed (byte* ptr = pixels)
		{
			if (flag)
			{
				_gl.TexImage3D(GL.TEXTURE_2D_ARRAY, 0, (int)_internalFormat, width, height, layerCount, 0, _format, _type, (IntPtr)ptr);
			}
			else
			{
				_gl.TexSubImage3D(GL.TEXTURE_2D_ARRAY, 0, 0, 0, 0, width, height, layerCount, _format, _type, (IntPtr)ptr);
			}
		}
		if (_requestMipMapChain)
		{
			_gl.GenerateMipmap(GL.TEXTURE_2D_ARRAY);
		}
	}

	public unsafe void UpdateTexture2DArrayLayer(Texture texture, int layer)
	{
		Debug.Assert(TextureType == TextureTypes.Texture2DArray);
		Debug.Assert(texture.Width == _width && texture.Height == _height && texture._format == _format && layer <= _layerCount);
		byte[] array = new byte[_width * _height * 4];
		_gl.BindTexture(GL.TEXTURE_2D, texture.GLTexture);
		fixed (byte* ptr = array)
		{
			_gl.GetTexImage(GL.TEXTURE_2D, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
		}
		_gl.BindTexture(GL.TEXTURE_2D_ARRAY, GLTexture);
		fixed (byte* ptr2 = array)
		{
			_gl.TexSubImage3D(GL.TEXTURE_2D_ARRAY, 0, 0, 0, layer, _width, _height, 1, _format, _type, (IntPtr)ptr2);
		}
		if (_requestMipMapChain)
		{
			_gl.GenerateMipmap(GL.TEXTURE_2D_ARRAY);
		}
	}

	protected override void DoDispose()
	{
		DestroyGPUData();
	}

	public void DestroyGPUData()
	{
		_gl.DeleteTexture(GLTexture);
	}

	public static byte[][] BuildMipmapPixels(byte[] atlasPixels, int width, int mipmapLevelCount)
	{
		int num = atlasPixels.Length / width / 4;
		byte[][] array = new byte[mipmapLevelCount + 1][];
		array[0] = atlasPixels;
		int num2 = width;
		int num3 = num;
		byte[] array2 = array[0];
		for (int i = 1; i <= mipmapLevelCount; i++)
		{
			int num4 = num2;
			num2 = System.Math.Max(num2 / 2, 1);
			num3 = System.Math.Max(num3 / 2, 1);
			byte[] array3 = (array[i] = new byte[num2 * num3 * 4]);
			for (int j = 0; j < num3; j++)
			{
				for (int k = 0; k < num2; k++)
				{
					int num5 = j * 2 * num4 + k * 2;
					Vector4 vector = new Vector4((int)array2[num5 * 4], (int)array2[num5 * 4 + 1], (int)array2[num5 * 4 + 2], (int)array2[num5 * 4 + 3]);
					int num6 = j * 2 * num4 + k * 2 + 1;
					Vector4 vector2 = new Vector4((int)array2[num6 * 4], (int)array2[num6 * 4 + 1], (int)array2[num6 * 4 + 2], (int)array2[num6 * 4 + 3]);
					int num7 = (j * 2 + 1) * num4 + k * 2;
					Vector4 vector3 = new Vector4((int)array2[num7 * 4], (int)array2[num7 * 4 + 1], (int)array2[num7 * 4 + 2], (int)array2[num7 * 4 + 3]);
					int num8 = (j * 2 + 1) * num4 + k * 2 + 1;
					Vector4 vector4 = new Vector4((int)array2[num8 * 4], (int)array2[num8 * 4 + 1], (int)array2[num8 * 4 + 2], (int)array2[num8 * 4 + 3]);
					if (vector.W == 0f)
					{
						vector = vector2;
					}
					else if (vector2.W == 0f)
					{
						vector2 = vector;
					}
					Vector4 vector5 = Vector4.Lerp(vector, vector2, 0.5f);
					if (vector3.W == 0f)
					{
						vector3 = vector4;
					}
					else if (vector4.W == 0f)
					{
						vector4 = vector3;
					}
					Vector4 vector6 = Vector4.Lerp(vector3, vector4, 0.5f);
					if (vector5.W == 0f)
					{
						vector5 = vector6;
					}
					else if (vector6.W == 0f)
					{
						vector6 = vector5;
					}
					Vector4 vector7 = Vector4.Lerp(vector5, vector6, 0.5f);
					int num9 = j * num2 + k;
					array3[num9 * 4] = (byte)System.Math.Round(vector7.X);
					array3[num9 * 4 + 1] = (byte)System.Math.Round(vector7.Y);
					array3[num9 * 4 + 2] = (byte)System.Math.Round(vector7.Z);
					array3[num9 * 4 + 3] = (byte)System.Math.Round(vector7.W);
				}
			}
			array2 = array3;
		}
		return array;
	}
}
