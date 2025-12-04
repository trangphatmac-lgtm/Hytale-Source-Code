using System;
using HytaleClient.Application;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Utils;

namespace HytaleClient.Interface.MainMenu.Pages;

internal class HomePage : InterfaceComponent
{
	public readonly MainMenuView MainMenuView;

	public HomePage(MainMenuView mainMenuView)
		: base(mainMenuView.Interface, null)
	{
		MainMenuView = mainMenuView;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("MainMenu/HomePage.ui", out var document);
		UIFragment fragment = document.Instantiate(Desktop, this);
		SetupButton("AdventureButton", delegate
		{
			Interface.App.MainMenu.Open(AppMainMenu.MainMenuPage.Adventure);
		}, 15);
		SetupButton("MinigamesButton", delegate
		{
			Interface.App.MainMenu.Open(AppMainMenu.MainMenuPage.Minigames);
		}, 15);
		SetupButton("ServersButton", delegate
		{
			Interface.App.MainMenu.Open(AppMainMenu.MainMenuPage.Servers);
		}, 15);
		SetupButton("SharedSinglePlayerButton", delegate
		{
			Interface.App.MainMenu.Open(AppMainMenu.MainMenuPage.SharedSinglePlayer);
		}, 15);
		SetupButton("AvatarButton", delegate
		{
			Interface.App.MainMenu.Open(AppMainMenu.MainMenuPage.MyAvatar);
		}, 15);
		SetupButton("SettingsButton", delegate
		{
			Interface.App.MainMenu.Open(AppMainMenu.MainMenuPage.Settings);
		}, 9);
		SetupButton("QuitButton", OnQuit, 9);
		fragment.Get<Label>("Version").Text = ((BuildInfo.RevisionId != null) ? (BuildInfo.Version + " Rev. " + BuildInfo.RevisionId) : BuildInfo.Version);
		if (base.IsMounted)
		{
			Interface.SocialBar.SetContainer(Find<Group>("SocialBarContainer"));
		}
		void SetupButton(string name, Action onActivate, int hoverPadding)
		{
			TextButton button = fragment.Get<TextButton>(name);
			button.Activating = onActivate;
			button.MouseEntered = delegate
			{
				button.Padding.Left += hoverPadding;
				button.Layout();
			};
			button.MouseExited = delegate
			{
				button.Padding.Left -= hoverPadding;
				button.Layout();
			};
		}
	}

	protected override void OnMounted()
	{
		MainMenuView.ShowTopBar(showTopBar: false);
		Interface.SocialBar.SetContainer(Find<Group>("SocialBarContainer"));
	}

	private void OnQuit()
	{
		Interface.Logger.Info("User closed game from user interface");
		Interface.App.Exit();
	}
}
