using HytaleClient.Interface.UI.Elements;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal abstract class Panel : InterfaceComponent
{
	protected readonly InGameView _inGameView;

	public Panel(InGameView inGameView, Element parent = null)
		: base(inGameView.Interface, parent)
	{
		_inGameView = inGameView;
	}
}
