using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels.SelectionCommands;

internal class SetCommand : BaseMultipleMaterialsCommand
{
	public SetCommand(InGameView inGameView, Desktop desktop, Element parent = null)
		: base(inGameView, desktop, parent)
	{
	}

	public override string GetChatCommand()
	{
		return "/set " + base.GetChatCommand();
	}
}
