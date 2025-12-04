using HytaleClient.Application;
using HytaleClient.InGame.Modules;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame.Overlays;

internal class MachinimaEditorOverlay : InterfaceComponent
{
	private readonly InGameView _inGameView;

	public MachinimaEditorOverlay(InGameView inGameView)
		: base(inGameView.Interface, null)
	{
		_inGameView = inGameView;
	}

	public void Build()
	{
		Clear();
	}

	protected override void OnMounted()
	{
		_inGameView.InGame.Instance.EditorWebViewModule.SetCurrentWebView(EditorWebViewModule.WebViewType.MachinimaEditor);
	}

	protected override void OnUnmounted()
	{
		if (Interface.App.Stage == App.AppStage.InGame)
		{
			_inGameView.InGame.Instance.EditorWebViewModule.SetCurrentWebView(EditorWebViewModule.WebViewType.None);
		}
	}

	public override Element HitTest(Point position)
	{
		return this;
	}
}
