using System;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame;

[UIMarkupElement(AcceptsChildren = false)]
internal class ItemIcon : Element
{
	private InGameView _inGameView;

	[UIMarkupProperty]
	public string ItemId;

	public ItemIcon(Desktop Desktop, Element parent)
		: base(Desktop, parent)
	{
		IUIProvider provider = Desktop.Provider;
		IUIProvider iUIProvider = provider;
		if (!(iUIProvider is CustomUIProvider customUIProvider))
		{
			if (!(iUIProvider is Interface @interface))
			{
				throw new Exception("IUIProvider must be of type CustomUIProvider or Interface");
			}
			_inGameView = @interface.InGameView;
		}
		else
		{
			_inGameView = customUIProvider.Interface.InGameView;
		}
	}

	protected override void ApplyStyles()
	{
		if (ItemId != null && _inGameView.Items.TryGetValue(ItemId, out var value) && value.Icon != null)
		{
			Background = new PatchStyle(_inGameView.GetTextureAreaForItemIcon(value.Icon));
		}
		else
		{
			Background = null;
		}
		base.ApplyStyles();
	}
}
