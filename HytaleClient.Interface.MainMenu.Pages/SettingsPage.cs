using HytaleClient.Application;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.MainMenu.Pages;

internal class SettingsPage : InterfaceComponent
{
	public readonly MainMenuView MainMenuView;

	private Group _container;

	public SettingsPage(MainMenuView mainMenuView)
		: base(mainMenuView.Interface, null)
	{
		MainMenuView = mainMenuView;
	}

	public void Build()
	{
		if (base.IsMounted)
		{
			_container.Remove(MainMenuView.Interface.SettingsComponent);
		}
		Clear();
		Interface.TryGetDocument("MainMenu/SettingsPage.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_container = uIFragment.Get<Group>("Container");
		if (base.IsMounted)
		{
			_container.Add(MainMenuView.Interface.SettingsComponent);
		}
	}

	protected override void OnMounted()
	{
		MainMenuView.ShowTopBar(showTopBar: true);
		_container.Add(MainMenuView.Interface.SettingsComponent);
	}

	protected override void OnUnmounted()
	{
		_container.Remove(MainMenuView.Interface.SettingsComponent);
	}

	protected internal override void Dismiss()
	{
		Interface.App.Settings.Save();
		Interface.App.MainMenu.Open(AppMainMenu.MainMenuPage.Home);
	}
}
