namespace HytaleClient.Interface.InGame;

internal class CustomMarkupErrorOverlay : MarkupErrorOverlay
{
	private readonly InGameView _inGameView;

	public CustomMarkupErrorOverlay(InGameView inGameView)
		: base(inGameView.Desktop, null, "Custom UI — Markup Error")
	{
		_inGameView = inGameView;
	}

	protected internal override void Dismiss()
	{
		_inGameView.Dismiss();
	}
}
