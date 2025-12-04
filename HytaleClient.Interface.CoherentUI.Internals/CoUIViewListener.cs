using Coherent.UI;

namespace HytaleClient.Interface.CoherentUI.Internals;

internal class CoUIViewListener : ViewListener
{
	private readonly CoUIManager _manager;

	private readonly WebView _webView;

	public CoUIViewListener(WebView webView, CoUIManager manager)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected O, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		_webView = webView;
		_manager = manager;
		((ViewListener)this).ViewCreated += new CoherentUI_OnViewCreated(_webView.OnCoherentViewCreated);
		((ViewListener)this).ReadyForBindings += new CoherentUI_OnReadyForBindings(_webView.OnCoherentReadyForBindings);
		((ViewListener)this).Draw += new CoherentUI_OnDraw(_webView.OnCoherentDraw);
		((ViewListener)this).CursorChanged += new CoherentUI_OnCursorChanged(_webView.OnCursorChanged);
		((ViewListener)this).NavigateTo += new CoherentUI_OnNavigateTo(_webView.OnNavigateTo);
		((ViewListener)this).ScriptMessage += new CoherentUI_OnScriptMessage(_webView.OnCoherentScriptMessage);
		((ViewListener)this).Error += new CoherentUI_OnError(_webView.OnCoherentError);
		((ViewListener)this).FailLoad += new CoherentUI_OnFailLoad(_webView.OnCoherentFailLoad);
		((ViewListener)this).FinishLoad += new CoherentUI_OnFinishLoad(_webView.OnCoherentFinishLoad);
	}

	public override void CreateSurface(bool sharedMemory, uint width, uint height, SurfaceResponse response)
	{
		response.Signal(_manager.TextureBufferHelper.CreateSharedMemory(width, height));
	}

	public override void DestroySurface(CoherentHandle surface, bool usesSharedMemory)
	{
		_manager.TextureBufferHelper.DestroySharedMemory(surface);
	}
}
