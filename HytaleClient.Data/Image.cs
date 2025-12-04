using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Data;

public class Image
{
	private static readonly object Lock = new object();

	public readonly byte[] Pixels;

	public readonly int Width;

	public readonly int Height;

	public readonly string Name;

	private const int MinimumPackingHeight = 32;

	public Image(int width, int height, byte[] pixels)
	{
		Width = width;
		Height = height;
		Pixels = pixels;
	}

	public Image(byte[] data)
		: this("", data)
	{
	}

	public unsafe Image(string name, byte[] data)
	{
		Name = name;
		lock (Lock)
		{
			IntPtr intPtr2;
			fixed (byte* ptr = data)
			{
				IntPtr intPtr = SDL.SDL_RWFromMem((IntPtr)ptr, data.Length);
				intPtr2 = SDL_image.IMG_Load_RW(intPtr, 1);
			}
			if (intPtr2 == IntPtr.Zero)
			{
				throw new Exception("Could not load image: " + name + " - " + SDL.SDL_GetError());
			}
			SDL_Surface* ptr2 = (SDL_Surface*)(void*)intPtr2;
			SDL_PixelFormat* ptr3 = (SDL_PixelFormat*)(void*)((SDL_Surface)ptr2).format;
			if (((SDL_PixelFormat)ptr3).format != SDL.SDL_PIXELFORMAT_ABGR8888)
			{
				IntPtr intPtr3 = SDL.SDL_ConvertSurfaceFormat(intPtr2, SDL.SDL_PIXELFORMAT_ABGR8888, 0u);
				SDL.SDL_FreeSurface(intPtr2);
				intPtr2 = intPtr3;
			}
			SDL_Surface* ptr4 = (SDL_Surface*)(void*)intPtr2;
			Width = ((SDL_Surface)ptr4).w;
			Height = ((SDL_Surface)ptr4).h;
			Pixels = new byte[Width * Height * 4];
			Marshal.Copy(((SDL_Surface)ptr4).pixels, Pixels, 0, Pixels.Length);
			SDL.SDL_FreeSurface(intPtr2);
		}
	}

	public Image(string name, int width, int height, byte[] pixels)
	{
		if (pixels.Length != width * height * 4)
		{
			throw new ArgumentException("Could not load image: " + name + " - Pixels array length must be width * height * 4");
		}
		Name = name;
		Width = width;
		Height = height;
		Pixels = pixels;
	}

	public static bool TryGetPngDimensions(string path, out int width, out int height)
	{
		try
		{
			using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(path)))
			{
				binaryReader.BaseStream.Position += 16L;
				byte[] array = binaryReader.ReadBytes(4);
				width = (array[0] << 24) | (array[1] << 16) | (array[2] << 8) | array[3];
				byte[] array2 = binaryReader.ReadBytes(4);
				height = (array2[0] << 24) | (array2[1] << 16) | (array2[2] << 8) | array2[3];
			}
			return true;
		}
		catch
		{
			width = (height = -1);
			return false;
		}
	}

	public void SavePNG(string path, int destWidth, int destHeight, uint redMask = 255u, uint greenMask = 65280u, uint blueMask = 16711680u, uint alphaMask = 4278190080u)
	{
		DoWithSurface(destWidth, destHeight, delegate(IntPtr surface)
		{
			if (SDL_image.IMG_SavePNG(surface, path) != 0)
			{
				throw new Exception("Could not save PNG: " + path + " - " + SDL.SDL_GetError());
			}
		}, redMask, greenMask, blueMask, alphaMask);
	}

	public void SavePNG(string path, uint redMask = 255u, uint greenMask = 65280u, uint blueMask = 16711680u, uint alphaMask = 4278190080u)
	{
		SavePNG(path, Width, Height, redMask, greenMask, blueMask, alphaMask);
	}

	public unsafe void DoWithSurface(int destWidth, int destHeight, Action<IntPtr> action, uint redMask = 255u, uint greenMask = 65280u, uint blueMask = 16711680u, uint alphaMask = 4278190080u)
	{
		lock (Lock)
		{
			fixed (byte* ptr = Pixels)
			{
				IntPtr intPtr = SDL.SDL_CreateRGBSurfaceFrom((IntPtr)ptr, Width, Height, 32, Width * 4, redMask, greenMask, blueMask, alphaMask);
				if (destWidth != Width || destHeight != Height)
				{
					IntPtr intPtr2 = SDL.SDL_CreateRGBSurface(((SDL_Surface)(void*)intPtr).flags, destWidth, destHeight, 32, redMask, greenMask, blueMask, alphaMask);
					if (SDL.SDL_BlitScaled(intPtr, IntPtr.Zero, intPtr2, IntPtr.Zero) < 0)
					{
						throw new Exception("SDL_BlitScaled failed: " + SDL.SDL_GetError());
					}
					SDL.SDL_FreeSurface(intPtr);
					intPtr = intPtr2;
				}
				try
				{
					action(intPtr);
				}
				finally
				{
					SDL.SDL_FreeSurface(intPtr);
				}
			}
		}
	}

	public static Image Pack(string name, int width, List<Image> images, out Dictionary<Image, Point> imageLocations, bool doPadding, CancellationToken cancellationToken = default(CancellationToken))
	{
		byte[] array = Pack(width, images, out imageLocations, doPadding, cancellationToken);
		return new Image(name, width, array.Length / width / 4, array);
	}

	public static byte[] Pack(int width, List<Image> images, out Dictionary<Image, Point> imageLocations, bool doPadding, CancellationToken cancellationToken = default(CancellationToken))
	{
		int num = 32;
		imageLocations = new Dictionary<Image, Point>();
		Point zero = Point.Zero;
		int num2 = 0;
		int num3 = (doPadding ? 2 : 0);
		int num4 = (doPadding ? 1 : 0);
		foreach (Image image2 in images)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				imageLocations = null;
				return null;
			}
			if (zero.X + image2.Width + num3 > width)
			{
				zero.X = 0;
				zero.Y = num2;
			}
			while (zero.Y + image2.Height >= num)
			{
				num <<= 1;
			}
			num2 = System.Math.Max(num2, zero.Y + image2.Height + num3);
			imageLocations[image2] = new Point(zero.X + num4, zero.Y + num4);
			zero.X += image2.Width + num3;
		}
		byte[] pixels = new byte[width * num * 4];
		foreach (Image image3 in images)
		{
			Image image = image3;
			if (cancellationToken.IsCancellationRequested)
			{
				imageLocations = null;
				return null;
			}
			zero = imageLocations[image];
			for (int i = 0; i < image.Height; i++)
			{
				int dstOffset = ((zero.Y + i) * width + zero.X) * 4;
				Buffer.BlockCopy(image.Pixels, i * image.Width * 4, pixels, dstOffset, image.Width * 4);
			}
			if (doPadding)
			{
				CopyPixel(0, 0, zero.X - 1, zero.Y - 1);
				Buffer.BlockCopy(image.Pixels, 0, pixels, ((zero.Y - 1) * width + zero.X) * 4, image.Width * 4);
				CopyPixel(image.Width - 1, 0, zero.X + image.Width, zero.Y - 1);
				for (int j = 0; j < image.Height; j++)
				{
					CopyPixel(0, j, zero.X - 1, zero.Y + j);
				}
				for (int k = 0; k < image.Height; k++)
				{
					CopyPixel(image.Width - 1, k, zero.X + image.Width, zero.Y + k);
				}
				CopyPixel(0, image.Height - 1, zero.X - 1, zero.Y + image.Height);
				Buffer.BlockCopy(image.Pixels, (image.Height - 1) * image.Width * 4, pixels, ((zero.Y + image.Height) * width + zero.X) * 4, image.Width * 4);
				CopyPixel(image.Width - 1, image.Height - 1, zero.X + image.Width, zero.Y + image.Height);
			}
			void CopyPixel(int x, int y, int dstX, int dstY)
			{
				pixels[(dstY * width + dstX) * 4] = image.Pixels[(y * image.Width + x) * 4];
				pixels[(dstY * width + dstX) * 4 + 2] = image.Pixels[(y * image.Width + x) * 4 + 2];
				pixels[(dstY * width + dstX) * 4 + 3] = image.Pixels[(y * image.Width + x) * 4 + 3];
				pixels[(dstY * width + dstX) * 4 + 1] = image.Pixels[(y * image.Width + x) * 4 + 1];
			}
		}
		return pixels;
	}
}
