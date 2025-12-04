using System;
using System.Threading;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Graphics;
using HytaleClient.Math;
using NLog;

namespace HytaleClient.InGame.Modules;

internal class ScreenEffectRenderer : Disposable
{
	private readonly Engine _engine;

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly GLTexture ScreenEffectTexture;

	private string _currentScreenEffectTextureChecksum;

	public Vector4 Color = Vector4.Zero;

	public bool IsScreenEffectTextureLoading { get; private set; } = false;


	public bool HasTexture { get; private set; } = false;


	public ScreenEffectRenderer(Engine engine)
	{
		_engine = engine;
		ScreenEffectTexture = _engine.Graphics.GL.GenTexture();
	}

	protected override void DoDispose()
	{
		GLFunctions gL = _engine.Graphics.GL;
		gL.DeleteTexture(ScreenEffectTexture);
	}

	public unsafe void Initialize()
	{
		GLFunctions gL = _engine.Graphics.GL;
		gL.BindTexture(GL.TEXTURE_2D, ScreenEffectTexture);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 10497);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 10497);
		fixed (byte* ptr = _engine.Graphics.TransparentPixel)
		{
			gL.TexImage2D(GL.TEXTURE_2D, 0, 6408, 1, 1, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
		}
	}

	public unsafe void RequestTextureUpdate(string targetScreenEffectTextureChecksum, bool forceUpdate = false)
	{
		if (!forceUpdate && targetScreenEffectTextureChecksum == _currentScreenEffectTextureChecksum)
		{
			return;
		}
		_currentScreenEffectTextureChecksum = targetScreenEffectTextureChecksum;
		GLFunctions gl = _engine.Graphics.GL;
		if (targetScreenEffectTextureChecksum == null)
		{
			IsScreenEffectTextureLoading = false;
			gl.BindTexture(GL.TEXTURE_2D, ScreenEffectTexture);
			fixed (byte* ptr = _engine.Graphics.TransparentPixel)
			{
				gl.TexImage2D(GL.TEXTURE_2D, 0, 6408, 1, 1, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
			}
			HasTexture = false;
			return;
		}
		IsScreenEffectTextureLoading = true;
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				Image image = new Image(AssetManager.GetAssetUsingHash(targetScreenEffectTextureChecksum));
				_engine.RunOnMainThread(this, delegate
				{
					gl.BindTexture(GL.TEXTURE_2D, ScreenEffectTexture);
					fixed (byte* ptr2 = image.Pixels)
					{
						gl.TexImage2D(GL.TEXTURE_2D, 0, 6408, image.Width, image.Height, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr2);
					}
					IsScreenEffectTextureLoading = false;
					HasTexture = true;
				});
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to load screen effect: " + AssetManager.GetAssetLocalPathUsingHash(targetScreenEffectTextureChecksum));
			}
		});
	}
}
