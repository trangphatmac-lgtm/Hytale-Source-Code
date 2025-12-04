#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Application;
using HytaleClient.Core;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Programs;
using HytaleClient.Interface.CoherentUI;
using HytaleClient.Math;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.InGame.Modules;

internal class EditorWebViewModule : Module
{
	public enum WebViewType
	{
		None,
		MachinimaEditor
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly WebView WebView;

	public readonly GLTexture Texture;

	private readonly QuadRenderer _quadRenderer;

	private WebViewType _currentWebView;

	private readonly Engine _engine;

	private static Matrix Matrix = Matrix.CreateTranslation(0f, 0f, -1f) * Matrix.CreateOrthographicOffCenter(0f, 1f, 0f, 1f, 0.1f, 1000f);

	public EditorWebViewModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_engine = _gameInstance.Engine;
		_quadRenderer = new QuadRenderer(_engine.Graphics, _engine.Graphics.GPUProgramStore.BasicProgram.AttribPosition, _engine.Graphics.GPUProgramStore.BasicProgram.AttribTexCoords);
		WebView = new WebView(_gameInstance.Engine, _gameInstance.App.CoUIManager, "coui://interface/index.html", _engine.Window.Viewport.Width, _engine.Window.Viewport.Height, _engine.Window.ViewportScale);
		GLFunctions gL = _engine.Graphics.GL;
		Texture = gL.GenTexture();
		gL.BindTexture(GL.TEXTURE_2D, Texture);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 33071);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 33071);
		WebView.TriggerEvent("i18n.setClientMessages", Language.LoadLanguage(_gameInstance.App.Settings.Language));
		WebView.RegisterForEvent("closeInGameOverlay", _gameInstance, delegate
		{
			_gameInstance.App.InGame.SetCurrentOverlay(AppInGame.InGameOverlay.None);
		});
		WebView.RegisterForEvent("reload", _engine, OnReload);
	}

	protected override void DoDispose()
	{
		WebView.UnregisterFromEvent("closeInGameOverlay");
		WebView.UnregisterFromEvent("reload");
		_gameInstance.App.CoUIManager.RunInThread(delegate
		{
			WebView.Destroy();
			_engine.RunOnMainThread(_engine, delegate
			{
				WebView.Dispose();
			});
		});
		_quadRenderer.Dispose();
		GLFunctions gL = _engine.Graphics.GL;
		gL.DeleteTexture(Texture);
	}

	private void OnReload()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (_currentWebView != 0)
		{
			WebView.Reload();
			WebView.TriggerEvent("i18n.setClientMessages", Language.LoadLanguage(_gameInstance.App.Settings.Language));
		}
	}

	public void OnWindowSizeChanged()
	{
		WebView.Resize(_engine.Window.Viewport.Width, _engine.Window.Viewport.Height, _engine.Window.ViewportScale);
	}

	public void SetCurrentWebView(WebViewType webViewType)
	{
		_currentWebView = webViewType;
		WebView.TriggerEvent("setInGameOverlay", _currentWebView);
	}

	public bool NeedsDrawing()
	{
		return _currentWebView != WebViewType.None;
	}

	public void Draw()
	{
		if (!NeedsDrawing())
		{
			throw new Exception("Draw called when it was not required. Please check with NeedsDrawing() first before calling this.");
		}
		GLFunctions gL = _engine.Graphics.GL;
		BasicProgram basicProgram = _engine.Graphics.GPUProgramStore.BasicProgram;
		gL.Disable(GL.DEPTH_TEST);
		gL.BlendFunc(GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
		gL.AssertEnabled(GL.BLEND);
		gL.AssertBlendFunc(GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
		basicProgram.AssertInUse();
		basicProgram.Color.SetValue(_engine.Graphics.WhiteColor);
		basicProgram.Opacity.SetValue(1f);
		basicProgram.MVPMatrix.SetValue(ref Matrix);
		gL.BindTexture(GL.TEXTURE_2D, Texture);
		WebView.RenderToTexture();
		_quadRenderer.Draw();
		gL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
		gL.Enable(GL.DEPTH_TEST);
	}
}
