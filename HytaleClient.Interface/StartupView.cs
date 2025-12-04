using System.IO;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Graphics;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Interface;

internal class StartupView : InterfaceComponent
{
	private Texture _splashScreenTexture;

	private float _lerpLoadingProgress;

	public StartupView(Interface @interface)
		: base(@interface, null)
	{
	}

	protected override void OnMounted()
	{
		Image image = new Image(File.ReadAllBytes(Path.Combine(Paths.GameData, "Splashscreen.png")));
		_splashScreenTexture = new Texture(Texture.TextureTypes.Texture2D);
		_splashScreenTexture.CreateTexture2D(image.Width, image.Height, image.Pixels, 5, GL.LINEAR, GL.LINEAR);
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		_splashScreenTexture.Dispose();
		_splashScreenTexture = null;
		Desktop.UnregisterAnimationCallback(Animate);
	}

	private void Animate(float deltaTime)
	{
		_lerpLoadingProgress = MathHelper.Lerp(_lerpLoadingProgress, AssetManager.BuiltInAssetsMetadataLoadProgress, MathHelper.Min(deltaTime * 10f, 1f));
	}

	protected override void PrepareForDrawSelf()
	{
		Engine engine = Interface.Engine;
		Rectangle viewport = engine.Window.Viewport;
		if (engine.Window.AspectRatio > (double)((float)_splashScreenTexture.Width / (float)_splashScreenTexture.Height))
		{
			viewport.Width = _splashScreenTexture.Width * engine.Window.Viewport.Height / _splashScreenTexture.Height;
			viewport.X = engine.Window.Viewport.Center.X - viewport.Width / 2;
		}
		else
		{
			viewport.Height = _splashScreenTexture.Height * engine.Window.Viewport.Width / _splashScreenTexture.Width;
			viewport.Y = engine.Window.Viewport.Center.Y - viewport.Height / 2;
		}
		Desktop.Batcher2D.RequestDrawTexture(_splashScreenTexture, new Rectangle(0, 0, _splashScreenTexture.Width, _splashScreenTexture.Height), viewport, UInt32Color.White);
		int width = (int)((float)viewport.Width * _lerpLoadingProgress);
		int num = viewport.Height / 144;
		Desktop.Batcher2D.RequestDrawTexture(Desktop.Graphics.WhitePixelTexture, new Rectangle(0, 0, 1, 1), new Rectangle(viewport.X, viewport.Bottom - num, width, num), UInt32Color.FromRGBA(2915031551u));
	}
}
