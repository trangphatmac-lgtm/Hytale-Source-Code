using HytaleClient.InGame.Modules.InterfaceRenderPreview;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame;

[UIMarkupElement(AcceptsChildren = true)]
internal class CharacterPreviewComponent : Element
{
	private Interface _interface;

	private int _id;

	public CharacterPreviewComponent(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_interface = (Interface)Desktop.Provider;
		_id = _interface.InGameView.NextPreviewId++;
	}

	protected override void OnUnmounted()
	{
		_interface.App.InGame.Instance.InterfaceRenderPreviewModule.RemovePreview(_id);
	}

	protected override void LayoutSelf()
	{
		Update();
	}

	private void Update()
	{
		Rectangle anchoredRectangle = base.AnchoredRectangle;
		anchoredRectangle.Offset(Desktop.ViewportRectangle.Location);
		_interface.App.InGame.Instance.InterfaceRenderPreviewModule.AddCharacterPreview(new InterfaceRenderPreviewModule.PreviewParams
		{
			Id = _id,
			Rotatable = false,
			Translation = new float[3] { 0f, -70f, -200f },
			Rotation = new float[3] { 0f, 15f, 0f },
			Scale = 0.05f,
			Ortho = false,
			Viewport = anchoredRectangle
		});
	}
}
