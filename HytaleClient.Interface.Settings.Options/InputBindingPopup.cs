using System.Collections.Generic;
using HytaleClient.Core;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using SDL2;

namespace HytaleClient.Interface.Settings.Options;

internal class InputBindingPopup : Panel
{
	private readonly Label _bindingName;

	private readonly SettingsComponent _settingsComponent;

	public InputBindingPopup(SettingsComponent settingsComponent)
		: base(settingsComponent.Desktop, null)
	{
		_settingsComponent = settingsComponent;
		settingsComponent.Interface.TryGetDocument("Common/Settings/InputBindingPopup.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_bindingName = uIFragment.Get<Label>("BindingName");
	}

	protected internal override void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if ((int)keycode != 27)
		{
			_settingsComponent.OnInputBindingKeyPress(keycode);
		}
		else
		{
			base.OnKeyDown(keycode, repeat);
		}
	}

	protected override void OnMouseButtonDown(MouseButtonEvent @event)
	{
		_settingsComponent.OnInputBindingMousePress((Input.MouseButton)@event.Button);
	}

	public void Setup(string binding)
	{
		_bindingName.Text = Desktop.Provider.GetText("ui.settings.bindingPopup.bind", new Dictionary<string, string> { 
		{
			"binding",
			Desktop.Provider.GetText("ui.settings.bindings." + binding)
		} });
	}

	protected internal override void Dismiss()
	{
		_settingsComponent.StopEditingInputBinding();
	}
}
