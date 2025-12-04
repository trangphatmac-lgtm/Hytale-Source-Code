using HytaleClient.Data.Items;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class BlockInfoPanel : Panel
{
	private ItemPreviewComponent _itemPreview;

	private Label _nameLabel;

	private Button _infoButton;

	private string _blockItemId;

	public Group Panel { get; private set; }

	public BlockInfoPanel(InGameView inGameView, Element parent = null)
		: base(inGameView, parent)
	{
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Pages/Inventory/BlockInfoPanel.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		Panel = uIFragment.Get<Group>("Panel");
		_itemPreview = uIFragment.Get<ItemPreviewComponent>("ItemPreview");
		_nameLabel = uIFragment.Get<Label>("NameLabel");
		_infoButton = uIFragment.Get<Button>("InfoButton");
	}

	public void Update()
	{
		ClientItemBase clientItemBase = null;
		if (_inGameView.InventoryWindow != null && base.Visible)
		{
			JToken obj = _inGameView.InventoryWindow.WindowData["blockItemId"];
			string text = ((obj != null) ? obj.ToObject<string>() : null);
			if (text != null)
			{
				clientItemBase = _inGameView.Items[text];
			}
		}
		_blockItemId = clientItemBase?.Id;
		UpdatePreview();
		if (clientItemBase != null)
		{
			_nameLabel.Text = Desktop.Provider.GetText("items." + clientItemBase.Id + ".name");
			string text2 = Desktop.Provider.GetText("items." + clientItemBase.Id + ".description", null, returnFallback: false);
			if (text2 != null)
			{
				_infoButton.TooltipText = text2;
				_infoButton.Visible = true;
			}
			else
			{
				_infoButton.Visible = false;
			}
		}
		else
		{
			_nameLabel.Text = "";
			_infoButton.Visible = false;
		}
	}

	public void UpdatePreview()
	{
		if (_blockItemId != null && Desktop.GetLayer(2) == null)
		{
			_itemPreview.SetItemId(_blockItemId);
		}
		else
		{
			_itemPreview.SetItemId(null);
		}
	}
}
