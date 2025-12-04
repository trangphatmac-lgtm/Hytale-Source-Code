#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Programs;
using HytaleClient.Interface;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules.ImmersiveScreen.Screens;

internal class ImmersiveImageScreen : BaseImmersiveScreen
{
	private int _imageWidth;

	private int _imageHeight;

	private readonly QuadRenderer _quadRenderer;

	private readonly GLTexture _texture;

	private byte[] _paintingImageData;

	private readonly Image _missingImage;

	public ImmersiveImageScreen(GameInstance gameInstance, Vector3 blockPosition, ViewScreen screen)
		: base(gameInstance, blockPosition, screen)
	{
		_missingImage = BaseInterface.MakeMissingImage("");
		GraphicsDevice graphics = _gameInstance.Engine.Graphics;
		_quadRenderer = new QuadRenderer(graphics, graphics.GPUProgramStore.BasicProgram.AttribPosition, graphics.GPUProgramStore.BasicProgram.AttribTexCoords);
		GLFunctions gL = graphics.GL;
		_texture = gL.GenTexture();
		gL.BindTexture(GL.TEXTURE_2D, _texture);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 33071);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 33071);
	}

	public void SetViewData(ImmersiveView viewData)
	{
		if (viewData.Painting != null)
		{
			try
			{
				PaintingView painting = viewData.Painting;
				_imageWidth = painting.Width;
				_imageHeight = painting.Height;
				byte[] array = (byte[])(object)painting.Data;
				if (array == null)
				{
					array = new byte[painting.Width * painting.Height * 4];
				}
				UpdatePaintingImageData(array);
				return;
			}
			catch (Exception ex)
			{
				_gameInstance.App.DevTools.Error("Immersive image screen failed to update pixels: " + ex.Message);
				_imageWidth = _missingImage.Width;
				_imageHeight = _missingImage.Height;
				SetPixels(_missingImage.Pixels);
				return;
			}
		}
		string text = viewData.Image?.File;
		try
		{
			if (text == null)
			{
				throw new Exception("No image provided.");
			}
			if (!text.StartsWith("ImmersiveScreens/"))
			{
				throw new Exception("Invalid path prefix, must start with ImmersiveScreens/");
			}
			if (!_gameInstance.HashesByServerAssetPath.TryGetValue(text, out var value))
			{
				throw new Exception("Asset not found");
			}
			Image image = new Image(AssetManager.GetAssetUsingHash(value));
			_imageWidth = image.Width;
			_imageHeight = image.Height;
			SetPixels(image.Pixels);
		}
		catch (Exception ex2)
		{
			_gameInstance.App.DevTools.Error("Immersive image screen failed to load image with path " + text + ": " + ex2.Message);
			_imageWidth = _missingImage.Width;
			_imageHeight = _missingImage.Height;
			SetPixels(_missingImage.Pixels);
		}
	}

	public void UpdatePaintingImageData(byte[] data)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (data.Length != _imageWidth * _imageHeight * 4)
		{
			throw new Exception($"Byte array does not match desired length ${data.Length} != {_imageWidth * _imageHeight * 4}");
		}
		_paintingImageData = data;
		SetPixels(data);
	}

	protected override void DoDispose()
	{
		_quadRenderer.Dispose();
		_gameInstance.Engine.Graphics.GL.DeleteTexture(_texture);
	}

	private unsafe void SetPixels(byte[] pixels)
	{
		GLFunctions gL = _gameInstance.Engine.Graphics.GL;
		gL.BindTexture(GL.TEXTURE_2D, _texture);
		fixed (byte* ptr = pixels)
		{
			gL.TexImage2D(GL.TEXTURE_2D, 0, 6408, _imageWidth, _imageHeight, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
		}
	}

	public override void Draw()
	{
		if (!NeedsDrawing())
		{
			throw new Exception("Draw called when it was not required. Please check with NeedsDrawing() first before calling this.");
		}
		GLFunctions gL = _gameInstance.Engine.Graphics.GL;
		BasicProgram basicProgram = _gameInstance.Engine.Graphics.GPUProgramStore.BasicProgram;
		basicProgram.AssertInUse();
		basicProgram.Color.AssertValue(_gameInstance.Engine.Graphics.WhiteColor);
		basicProgram.Opacity.AssertValue(1f);
		basicProgram.MVPMatrix.SetValue(ref _mvpMatrix);
		gL.BindTexture(GL.TEXTURE_2D, _texture);
		_quadRenderer.Draw();
	}
}
