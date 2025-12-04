using System;
using HytaleClient.Application;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface.MainMenu;

[UIMarkupElement]
internal class PlayerPreviewComponent : Element
{
	private Interface _interface;

	private static int NextPlayerPreviewId;

	private readonly int _id = ++NextPlayerPreviewId;

	public PlayerPreviewComponent(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_interface = (Interface)Desktop.Provider;
	}

	protected override void OnUnmounted()
	{
		AppMainMenu mainMenu = _interface.App.MainMenu;
		int id = _id;
		mainMenu.RemoveCharacterFromScreen(id.ToString());
	}

	protected override void LayoutSelf()
	{
		Rectangle anchoredRectangle = base.AnchoredRectangle;
		anchoredRectangle.Offset(Desktop.ViewportRectangle.Location);
		AppMainMenu mainMenu = _interface.App.MainMenu;
		AppMainMenu.AddCharacterOnScreenEvent addCharacterOnScreenEvent = new AppMainMenu.AddCharacterOnScreenEvent();
		int id = _id;
		addCharacterOnScreenEvent.Id = id.ToString();
		addCharacterOnScreenEvent.InitialModelAngle = -(float)System.Math.PI / 8f;
		addCharacterOnScreenEvent.Scale = 1f;
		addCharacterOnScreenEvent.Viewport = anchoredRectangle;
		mainMenu.AddCharacterOnScreen(addCharacterOnScreenEvent);
	}
}
