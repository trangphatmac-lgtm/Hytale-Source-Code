using System;
using System.IO;
using System.Net;
using System.Threading;
using HytaleClient.Application;
using HytaleClient.Data;
using HytaleClient.Graphics;

namespace HytaleClient.Interface;

internal class ExternalTextureLoader
{
	private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

	public event EventHandler<TextureArea> OnComplete;

	public event EventHandler<Exception> OnFailure;

	public void Cancel()
	{
		_tokenSource.Cancel();
	}

	public static ExternalTextureLoader FromUrl(App app, string url)
	{
		ExternalTextureLoader loader = new ExternalTextureLoader();
		CancellationToken token = loader._tokenSource.Token;
		ThreadPool.QueueUserWorkItem(delegate
		{
			Image image;
			try
			{
				WebClient webClient = new WebClient();
				byte[] data = webClient.DownloadData(url);
				if (token.IsCancellationRequested)
				{
					return;
				}
				image = new Image(data);
				if (token.IsCancellationRequested)
				{
					return;
				}
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Exception exception = ex2;
				app.Engine.RunOnMainThread(app.Interface, delegate
				{
					loader.OnFailure?.Invoke(null, exception);
				});
				return;
			}
			app.Engine.RunOnMainThread(app.Interface, delegate
			{
				if (!token.IsCancellationRequested)
				{
					Texture texture = new Texture(Texture.TextureTypes.Texture2D);
					texture.CreateTexture2D(image.Width, image.Height, image.Pixels);
					TextureArea e = new TextureArea(texture, 0, 0, image.Width, image.Height, 1);
					loader.OnComplete?.Invoke(null, e);
				}
			});
		});
		return loader;
	}

	public static TextureArea FromPath(string path)
	{
		Image image = new Image(File.ReadAllBytes(path));
		return FromImage(image);
	}

	public static TextureArea FromImage(Image image)
	{
		Texture texture = new Texture(Texture.TextureTypes.Texture2D);
		texture.CreateTexture2D(image.Width, image.Height, image.Pixels, 5, GL.LINEAR, GL.LINEAR);
		return new TextureArea(texture, 0, 0, image.Width, image.Height, 1);
	}
}
