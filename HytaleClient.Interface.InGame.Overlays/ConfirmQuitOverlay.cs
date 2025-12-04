using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.InGame.Overlays;

internal class ConfirmQuitOverlay : InterfaceComponent
{
	public readonly InGameView InGameView;

	public ConfirmQuitOverlay(InGameView inGameView)
		: base(inGameView.Interface, null)
	{
		InGameView = inGameView;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Overlays/ConfirmQuitOverlay.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		uIFragment.Get<TextButton>("ReturnToGame").Activating = Dismiss;
		uIFragment.Get<TextButton>("QuitToDesktop").Activating = Validate;
	}

	protected internal override void Dismiss()
	{
		InGameView.InGame.TryClosePageOrOverlay();
	}

	protected internal override void Validate()
	{
		InGameView.InGame.RequestExit(exitApplication: true);
	}
}
