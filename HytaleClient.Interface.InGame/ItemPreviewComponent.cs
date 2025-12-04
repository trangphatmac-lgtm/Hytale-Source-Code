using HytaleClient.Data.Items;
using HytaleClient.InGame.Modules.InterfaceRenderPreview;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame;

[UIMarkupElement(AcceptsChildren = true)]
internal class ItemPreviewComponent : Element
{
	private Interface _interface;

	private int _id;

	private string _itemId;

	public ItemPreviewComponent(Desktop Desktop, Element parent)
		: base(Desktop, parent)
	{
		_interface = (Interface)Desktop.Provider;
		_id = _interface.InGameView.NextPreviewId++;
	}

	protected override void OnUnmounted()
	{
		_interface.App.InGame.Instance?.InterfaceRenderPreviewModule.RemovePreview(_id);
	}

	protected override void LayoutSelf()
	{
		if (_itemId != null)
		{
			Update();
		}
	}

	public void SetItemId(string itemId)
	{
		_itemId = itemId;
		if (base.IsMounted)
		{
			Update();
		}
	}

	private void Update()
	{
		if (_itemId != null)
		{
			ClientItemBase item = _interface.InGameView.Items[_itemId];
			ClientItemIconProperties iconProperties = IconHelper.GetIconProperties(item);
			Rectangle anchoredRectangle = base.AnchoredRectangle;
			anchoredRectangle.Offset(Desktop.ViewportRectangle.Location);
			_interface.App.InGame.Instance.InterfaceRenderPreviewModule.AddItemPreview(new InterfaceRenderPreviewModule.ItemPreviewParams
			{
				Id = _id,
				ItemId = _itemId,
				Rotatable = false,
				Translation = new float[3]
				{
					iconProperties.Translation.Value.X,
					iconProperties.Translation.Value.Y,
					0f
				},
				Rotation = new float[3]
				{
					iconProperties.Rotation.Value.X,
					iconProperties.Rotation.Value.Y,
					iconProperties.Rotation.Value.Z
				},
				Scale = iconProperties.Scale * 0.8f,
				Ortho = true,
				Viewport = anchoredRectangle
			});
		}
		else
		{
			_interface.App.InGame.Instance.InterfaceRenderPreviewModule.RemovePreview(_id);
		}
	}
}
