using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using SDL2;

namespace HytaleClient.Interface.MainMenu.Pages.MyAvatar;

internal class SkinJsonPopup : InterfaceComponent
{
	private Label _jsonLabel;

	private string _json;

	public SkinJsonPopup(MyAvatarPage page)
		: base(page.Interface, null)
	{
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("MainMenu/MyAvatar/JsonPopup.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_jsonLabel = uIFragment.Get<Label>("Json");
		uIFragment.Get<TextButton>("CopyButton").Activating = delegate
		{
			SDL.SDL_SetClipboardText(_json ?? "");
		};
		uIFragment.Get<TextButton>("CloseButton").Activating = Dismiss;
	}

	protected override void OnMounted()
	{
		_json = ((object)Interface.App.MainMenu.GetSkinJson()).ToString();
		_jsonLabel.Text = _json;
		_jsonLabel.Parent.Layout();
	}

	protected override void OnUnmounted()
	{
		_json = null;
	}

	protected internal override void Dismiss()
	{
		Desktop.ClearLayer(2);
	}
}
