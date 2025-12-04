using System;
using System.Collections.Generic;
using HytaleClient.Data;
using HytaleClient.Graphics;
using HytaleClient.Math;

namespace HytaleClient.Interface.AssetEditor.Utils;

public class TextureAtlasUtils
{
	public static Texture CreateTextureAtlas(Dictionary<string, Image> textures, out Dictionary<string, Point> textureLocations)
	{
		int num = 0;
		int num2 = 0;
		foreach (Image value2 in textures.Values)
		{
			num += value2.Width;
			if (value2.Height > num2)
			{
				num2 = value2.Height;
			}
		}
		textureLocations = new Dictionary<string, Point>();
		byte[] array = new byte[num * num2 * 4];
		int num3 = 0;
		int num4 = 0;
		foreach (KeyValuePair<string, Image> texture2 in textures)
		{
			Image value = texture2.Value;
			for (int i = 0; i < value.Height; i++)
			{
				int dstOffset = (i * num + num3) * 4;
				Buffer.BlockCopy(value.Pixels, i * value.Width * 4, array, dstOffset, value.Width * 4);
			}
			textureLocations.Add(texture2.Key, new Point(num3, 0));
			num3 += value.Width;
			num4++;
		}
		Texture texture = new Texture(Texture.TextureTypes.Texture2D);
		texture.CreateTexture2D(num, num2, null, 5, GL.NEAREST_MIPMAP_NEAREST);
		if (textureLocations.Count > 0)
		{
			byte[][] pixelsPerMipmapLevel = Texture.BuildMipmapPixels(array, num, texture.MipmapLevelCount);
			texture.UpdateTexture2DMipMaps(pixelsPerMipmapLevel);
		}
		return texture;
	}
}
