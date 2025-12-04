using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Pages;

internal class PaintingEditorPage : InterfaceComponent
{
	public PaintingEditorPage(InGameView inGameView)
		: base(inGameView.Interface, null)
	{
	}

	public void Build()
	{
		Clear();
		new Element(Desktop, this)
		{
			Anchor = new Anchor
			{
				Width = 200,
				Height = 200
			},
			Background = new PatchStyle(305420031u)
		};
	}

	protected override void OnMounted()
	{
	}

	protected override void OnUnmounted()
	{
	}
}
