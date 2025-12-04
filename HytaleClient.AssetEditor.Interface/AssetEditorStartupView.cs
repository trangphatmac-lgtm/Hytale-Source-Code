using System.IO;
using HytaleClient.Data;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.AssetEditor.Interface;

public class AssetEditorStartupView : Element
{
	private Texture _splashScreenTexture;

	public AssetEditorStartupView(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void OnMounted()
	{
		Image image = new Image(File.ReadAllBytes(Path.Combine(Paths.EditorData, "Splashscreen.png")));
		_splashScreenTexture = new Texture(Texture.TextureTypes.Texture2D);
		_splashScreenTexture.CreateTexture2D(image.Width, image.Height, image.Pixels, 5, GL.LINEAR, GL.LINEAR);
	}

	protected override void OnUnmounted()
	{
		_splashScreenTexture.Dispose();
		_splashScreenTexture = null;
	}

	protected override void PrepareForDrawSelf()
	{
		Rectangle viewportRectangle = Desktop.ViewportRectangle;
		int num = viewportRectangle.Width / viewportRectangle.Height;
		if ((float)num > (float)_splashScreenTexture.Width / (float)_splashScreenTexture.Height)
		{
			viewportRectangle.Width = _splashScreenTexture.Width * Desktop.ViewportRectangle.Height / _splashScreenTexture.Height;
			viewportRectangle.X = Desktop.ViewportRectangle.Center.X - viewportRectangle.Width / 2;
		}
		else
		{
			viewportRectangle.Height = _splashScreenTexture.Height * Desktop.ViewportRectangle.Width / _splashScreenTexture.Width;
			viewportRectangle.Y = Desktop.ViewportRectangle.Center.Y - viewportRectangle.Height / 2;
		}
		Desktop.Batcher2D.RequestDrawTexture(_splashScreenTexture, new Rectangle(0, 0, _splashScreenTexture.Width, _splashScreenTexture.Height), viewportRectangle, UInt32Color.White);
	}
}
