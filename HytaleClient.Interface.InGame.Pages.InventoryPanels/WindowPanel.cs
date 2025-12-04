using HytaleClient.Interface.UI.Elements;
using HytaleClient.Networking;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal abstract class WindowPanel : Panel
{
	protected PacketHandler.InventoryWindow _inventoryWindow;

	public WindowPanel(InGameView inGameView, Element parent = null)
		: base(inGameView, parent)
	{
	}

	public void SetupWindow(PacketHandler.InventoryWindow inventoryWindow)
	{
		_inventoryWindow = inventoryWindow;
		Setup();
	}

	public void UpdateWindow(PacketHandler.InventoryWindow inventoryWindow)
	{
		_inventoryWindow = inventoryWindow;
		Update();
	}

	public void RefreshWindow()
	{
		Setup();
	}

	protected virtual void Setup()
	{
	}

	protected virtual void Update()
	{
	}
}
