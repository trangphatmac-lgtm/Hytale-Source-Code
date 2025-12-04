using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels.SelectionCommands;

internal class FillCommand : BaseMultipleMaterialsCommand
{
	public FillCommand(InGameView inGameView, Desktop desktop, Element parent = null)
		: base(inGameView, desktop, parent)
	{
	}

	public override string GetChatCommand()
	{
		return "/fill " + base.GetChatCommand();
	}
}
