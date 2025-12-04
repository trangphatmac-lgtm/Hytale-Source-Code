#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Application;
using HytaleClient.Core;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame.Modules.ImmersiveScreen.Data;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.ImmersiveScreen.Screens;

internal class ImmersiveWebScreen : BaseImmersiveScreen
{
	private ClientMediaData _mediaData;

	public ImmersiveWebScreen(GameInstance gameInstance, Vector3 blockPosition, ViewScreen screen)
		: base(gameInstance, blockPosition, screen)
	{
	}

	public void SetViewData(ImmersiveView viewData)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		Debug.Assert((int)viewData.Web.App == 0);
		_mediaData = new ClientMediaData(viewData.Web.MediaData);
		if (_gameInstance.ImmersiveScreenModule.ActiveWebScreen == this)
		{
			SendMediaData();
		}
	}

	protected override void DoDispose()
	{
	}

	public string GetUrl()
	{
		return "coui://interface/immersiveView.html";
	}

	public void SendMediaData()
	{
		if (_gameInstance.ImmersiveScreenModule.CoUIWebView.IsReady)
		{
			_gameInstance.ImmersiveScreenModule.CoUIWebView.TriggerEvent("immersiveScreens.mediaData.update", _mediaData);
		}
		_gameInstance.App.Interface.InGameView.MediaBrowserPage.OnImmersiveViewDataUpdated(_mediaData);
	}

	public void OnActivate()
	{
		SendMediaData();
	}

	public void OnDeactivate()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		App app = _gameInstance.App;
		if (app.Stage == App.AppStage.InGame && (int)app.InGame.CurrentPage == 3)
		{
			app.InGame.SetCurrentPage((Page)0);
		}
		_gameInstance.App.Interface.InGameView.MediaBrowserPage.OnImmersiveViewDataUpdated(null);
	}

	public override void Draw()
	{
		if (!NeedsDrawing())
		{
			throw new Exception("Draw called when it was not required. Please check with NeedsDrawing() first before calling this.");
		}
		Debug.Assert(!_gameInstance.ImmersiveScreenModule.CoUIWebView.Disposed);
		if (_gameInstance.Engine.Window.GetState() != Window.WindowState.Minimized)
		{
			GLFunctions gL = _gameInstance.Engine.Graphics.GL;
			BasicProgram basicProgram = _gameInstance.Engine.Graphics.GPUProgramStore.BasicProgram;
			gL.BlendFunc(GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
			gL.AssertEnabled(GL.BLEND);
			gL.AssertBlendFunc(GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
			basicProgram.AssertInUse();
			basicProgram.Color.AssertValue(_gameInstance.Engine.Graphics.WhiteColor);
			basicProgram.Opacity.AssertValue(1f);
			basicProgram.MVPMatrix.SetValue(ref _mvpMatrix);
			gL.BindTexture(GL.TEXTURE_2D, _gameInstance.ImmersiveScreenModule.CoUIWebViewTexture);
			_gameInstance.ImmersiveScreenModule.CoUIWebView.RenderToTexture();
			_gameInstance.ImmersiveScreenModule.CoUIQuadRenderer.Draw();
			gL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
		}
	}
}
