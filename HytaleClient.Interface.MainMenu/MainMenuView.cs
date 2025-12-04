#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Application;
using HytaleClient.Interface.MainMenu.Pages;
using HytaleClient.Interface.MainMenu.Pages.MyAvatar;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using SDL2;

namespace HytaleClient.Interface.MainMenu;

internal class MainMenuView : InterfaceComponent
{
	private readonly Group _pageContainer;

	public readonly HomePage HomePage;

	public readonly AdventurePage AdventurePage;

	public readonly MinigamesPage MinigamesPage;

	public readonly ServersPage ServersPage;

	public readonly MyAvatarPage MyAvatarPage;

	public readonly SettingsPage SettingsPage;

	public readonly SharedSinglePlayerPage SharedSinglePlayerPage;

	public readonly TopBarComponent TopBar;

	internal readonly AppMainMenu MainMenu;

	private SoundStyle _dismissalSound;

	public MainMenuView(Interface @interface)
		: base(@interface, null)
	{
		MainMenu = @interface.App.MainMenu;
		_pageContainer = new Group(Desktop, this);
		HomePage = new HomePage(this);
		AdventurePage = new AdventurePage(this);
		MinigamesPage = new MinigamesPage(this);
		ServersPage = new ServersPage(this);
		MyAvatarPage = new MyAvatarPage(this);
		SettingsPage = new SettingsPage(this);
		SharedSinglePlayerPage = new SharedSinglePlayerPage(this);
		TopBar = new TopBarComponent(this);
	}

	public void Build()
	{
		TopBar.Build();
		HomePage.Build();
		AdventurePage.Build();
		MinigamesPage.Build();
		ServersPage.Build();
		MyAvatarPage.Build();
		SettingsPage.Build();
		SharedSinglePlayerPage.Build();
		Interface.TryGetDocument("MainMenu/Common.ui", out var document);
		_dismissalSound = document.ResolveNamedValue<SoundStyle>(Interface, "DismissalSound");
		if (base.IsMounted)
		{
			OnPageChanged();
		}
	}

	protected override void OnUnmounted()
	{
		MinigamesPage.DisposeGameTextures();
	}

	protected internal override void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (Interface.App.MainMenu.CurrentPage == AppMainMenu.MainMenuPage.Servers && Desktop.IsShortcutKeyDown && (int)keycode == 102)
		{
			ServersPage.FocusTextSearchInput();
		}
		else
		{
			base.OnKeyDown(keycode, repeat);
		}
	}

	public void ShowTopBar(bool showTopBar)
	{
		TopBar.Visible = showTopBar;
		Layout();
	}

	public void OpenUnsavedCharacterChangesModal(Action onContinue)
	{
		Interface.ModalDialog.Setup(new ModalDialog.DialogSetup
		{
			Title = "ui.myAvatar.unsavedChanges.title",
			Text = "ui.myAvatar.unsavedChanges.text",
			ConfirmationText = "ui.general.save",
			CancelText = "ui.general.discardChanges",
			Dismissable = false,
			OnConfirm = delegate
			{
				MainMenu.SaveCharacter();
				onContinue();
			},
			OnCancel = onContinue
		});
		Desktop.SetLayer(4, Interface.ModalDialog);
	}

	public void OpenAssetEditorMissingPathDialog()
	{
		Interface.ModalDialog.Setup(new ModalDialog.DialogSetup
		{
			Title = "ui.mainMenu.assetEditor.errorModal.missingDirectory.title",
			Text = "ui.mainMenu.assetEditor.errorModal.missingDirectory.title",
			ConfirmationText = "ui.mainMenu.assetEditor.errorModal.openSettingsButton",
			Dismissable = true,
			OnConfirm = delegate
			{
				MainMenu.Open(AppMainMenu.MainMenuPage.Settings);
			}
		});
		Desktop.SetLayer(4, Interface.ModalDialog);
	}

	public void OpenAssetEditorInvalidPathDialog()
	{
		Interface.ModalDialog.Setup(new ModalDialog.DialogSetup
		{
			Title = "ui.mainMenu.assetEditor.errorModal.invalidDirectory.title",
			Text = "ui.mainMenu.assetEditor.errorModal.invalidDirectory.text",
			ConfirmationText = "ui.mainMenu.assetEditor.errorModal.openSettingsButton",
			Dismissable = true,
			OnConfirm = delegate
			{
				MainMenu.Open(AppMainMenu.MainMenuPage.Settings);
			}
		});
		Desktop.SetLayer(4, Interface.ModalDialog);
	}

	public void OnPageChanged()
	{
		Debug.Assert(base.IsMounted);
		InterfaceComponent interfaceComponent = null;
		switch (Interface.App.MainMenu.CurrentPage)
		{
		case AppMainMenu.MainMenuPage.Home:
			interfaceComponent = HomePage;
			break;
		case AppMainMenu.MainMenuPage.Adventure:
			interfaceComponent = AdventurePage;
			break;
		case AppMainMenu.MainMenuPage.Minigames:
			interfaceComponent = MinigamesPage;
			break;
		case AppMainMenu.MainMenuPage.Servers:
			interfaceComponent = ServersPage;
			break;
		case AppMainMenu.MainMenuPage.Settings:
			interfaceComponent = SettingsPage;
			break;
		case AppMainMenu.MainMenuPage.MyAvatar:
			interfaceComponent = MyAvatarPage;
			break;
		case AppMainMenu.MainMenuPage.SharedSinglePlayer:
			interfaceComponent = SharedSinglePlayerPage;
			break;
		}
		if (_pageContainer.Children.Count != 1 || interfaceComponent != _pageContainer.Children[0])
		{
			_pageContainer.Clear();
			_pageContainer.Add(interfaceComponent);
			Layout();
			if (TopBar.IsMounted)
			{
				TopBar.OnPageChanged();
			}
			if (Interface.App.MainMenu.CurrentPage == AppMainMenu.MainMenuPage.Home || Interface.App.MainMenu.CurrentPage == AppMainMenu.MainMenuPage.MyAvatar)
			{
				Interface.App.MainMenu.ResetCharacters();
			}
		}
	}

	protected internal override void Dismiss()
	{
		if (_dismissalSound?.SoundPath != null)
		{
			Interface.PlaySound(_dismissalSound);
		}
		if (Interface.App.MainMenu.CurrentPage == AppMainMenu.MainMenuPage.Adventure)
		{
			AdventurePage.Dismiss();
		}
		else if (Interface.App.MainMenu.CurrentPage == AppMainMenu.MainMenuPage.Settings)
		{
			SettingsPage.Dismiss();
		}
		else if (Interface.App.MainMenu.CurrentPage == AppMainMenu.MainMenuPage.MyAvatar && Interface.App.MainMenu.HasUnsavedSkinChanges())
		{
			OpenUnsavedCharacterChangesModal(delegate
			{
				Interface.App.MainMenu.Open(AppMainMenu.MainMenuPage.Home);
			});
		}
		else if (Interface.App.MainMenu.CurrentPage != 0)
		{
			Interface.App.MainMenu.Open(AppMainMenu.MainMenuPage.Home);
		}
	}
}
