using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels.SelectionCommands;

internal abstract class BaseSelectionCommand : Element
{
	protected InGameView _inGameView;

	public BaseSelectionCommand(InGameView inGameView, Desktop desktop, Element parent = null)
		: base(desktop, parent)
	{
		_inGameView = inGameView;
	}

	public abstract string GetChatCommand();

	public abstract void Build();
}
