#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Graphics;
using HytaleClient.Math;
using HytaleClient.Networking;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.InGame.Modules;

internal class FXModule : Module
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const byte FXAtlasIndex = 3;

	private const int TextureSpacing = 2;

	public Dictionary<string, Rectangle> ImageLocations;

	public Texture TextureAtlas;

	private GLFunctions _gl;

	private Texture _uvMotionTextureArray2D;

	private const int _uvMotionTextureArrayHeight = 64;

	private const int _uvMotionTextureArrayWidth = 64;

	private int _uvMotionTextureArrayLayerCount;

	public GLTexture UVMotionTextureArray2D => _uvMotionTextureArray2D.GLTexture;

	public int UVMotionTextureCount => _uvMotionTextureArrayLayerCount;

	public FXModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_gl = gameInstance.Engine.Graphics.GL;
		CreateGPUData();
	}

	public void PrepareAtlas(Dictionary<string, PacketHandler.TextureInfo> particleTextureInfos, Dictionary<string, PacketHandler.TextureInfo> trailTextureInfos, out Dictionary<string, Rectangle> upcomingImageLocations, out byte[][] upcomingAtlasPixelsPerLevel, CancellationToken cancellationToken)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		upcomingImageLocations = new Dictionary<string, Rectangle>();
		Dictionary<string, PacketHandler.TextureInfo> dictionary = new Dictionary<string, PacketHandler.TextureInfo>();
		foreach (KeyValuePair<string, PacketHandler.TextureInfo> trailTextureInfo in trailTextureInfos)
		{
			dictionary[trailTextureInfo.Key] = trailTextureInfo.Value;
		}
		foreach (KeyValuePair<string, PacketHandler.TextureInfo> particleTextureInfo in particleTextureInfos)
		{
			dictionary[particleTextureInfo.Key] = particleTextureInfo.Value;
		}
		List<PacketHandler.TextureInfo> list = new List<PacketHandler.TextureInfo>(dictionary.Values);
		list.Sort((PacketHandler.TextureInfo a, PacketHandler.TextureInfo b) => b.Height.CompareTo(a.Height));
		Point zero = Point.Zero;
		int num = 0;
		int num2 = 512;
		for (int i = 0; i < list.Count; i++)
		{
			PacketHandler.TextureInfo textureInfo = list[i];
			if (cancellationToken.IsCancellationRequested)
			{
				upcomingAtlasPixelsPerLevel = null;
				return;
			}
			if (zero.X + textureInfo.Width > TextureAtlas.Width)
			{
				zero.X = 0;
				zero.Y = num;
			}
			while (zero.Y + textureInfo.Height > num2)
			{
				num2 <<= 1;
			}
			upcomingImageLocations.Add(textureInfo.Checksum, new Rectangle(zero.X, zero.Y, textureInfo.Width, textureInfo.Height));
			num = System.Math.Max(num, zero.Y + textureInfo.Height + 2);
			zero.X += textureInfo.Width + 2;
		}
		byte[] array = new byte[TextureAtlas.Width * num2 * 4];
		for (int j = 0; j < list.Count; j++)
		{
			PacketHandler.TextureInfo textureInfo2 = list[j];
			if (cancellationToken.IsCancellationRequested)
			{
				upcomingAtlasPixelsPerLevel = null;
				return;
			}
			try
			{
				Image image = new Image(AssetManager.GetAssetUsingHash(textureInfo2.Checksum));
				for (int k = 0; k < image.Height; k++)
				{
					Rectangle rectangle = upcomingImageLocations[textureInfo2.Checksum];
					int dstOffset = ((rectangle.Y + k) * TextureAtlas.Width + rectangle.X) * 4;
					Buffer.BlockCopy(image.Pixels, k * image.Width * 4, array, dstOffset, image.Width * 4);
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to load FX texture: " + AssetManager.GetAssetLocalPathUsingHash(textureInfo2.Checksum));
			}
		}
		upcomingAtlasPixelsPerLevel = Texture.BuildMipmapPixels(array, TextureAtlas.Width, TextureAtlas.MipmapLevelCount);
	}

	public void CreateAtlasTextures(Dictionary<string, Rectangle> imageLocations, byte[][] atlasPixelsPerLevel)
	{
		ImageLocations = imageLocations;
		TextureAtlas.UpdateTexture2DMipMaps(atlasPixelsPerLevel);
		_gameInstance.ParticleSystemStoreModule.UpdateTextures();
		_gameInstance.TrailStoreModule.UpdateTextures();
	}

	public void PrepareUVMotionTextureArray(List<string> uvMotionTexturePaths, out byte[][] upcomingAtlasPixelsPerLevel)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		_uvMotionTextureArrayLayerCount = uvMotionTexturePaths.Count;
		byte[] array = new byte[4096 * _uvMotionTextureArrayLayerCount * 4];
		int num = 0;
		for (int i = 0; i < uvMotionTexturePaths.Count; i++)
		{
			string text = uvMotionTexturePaths[i];
			try
			{
				Image image = new Image(AssetManager.GetBuiltInAsset(Path.Combine("Common", text)));
				int dstOffset = num * 64 * 4;
				Buffer.BlockCopy(image.Pixels, 0, array, dstOffset, image.Width * image.Height * 4);
				num += image.Height;
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to load UV motion texture: " + text);
			}
		}
		upcomingAtlasPixelsPerLevel = Texture.BuildMipmapPixels(array, 64, TextureAtlas.MipmapLevelCount);
	}

	public void CreateUVMotionTextureArray(byte[][] atlasPixelsPerLevel)
	{
		_uvMotionTextureArray2D.UpdateTexture2DArray(64, 64, _uvMotionTextureArrayLayerCount, atlasPixelsPerLevel[0]);
	}

	private void CreateGPUData()
	{
		TextureAtlas = new Texture(Texture.TextureTypes.Texture2D);
		TextureAtlas.CreateTexture2D(2048, 32, null, 0, GL.NEAREST_MIPMAP_NEAREST);
		_uvMotionTextureArray2D = new Texture(Texture.TextureTypes.Texture2DArray);
		_uvMotionTextureArray2D.CreateTexture2DArray(64, 64, _uvMotionTextureArrayLayerCount, null, GL.LINEAR, GL.LINEAR, GL.REPEAT, GL.REPEAT);
	}

	private void DestroyGPUData()
	{
		_uvMotionTextureArray2D.Dispose();
		TextureAtlas.Dispose();
	}

	protected override void DoDispose()
	{
		DestroyGPUData();
	}
}
