using System;
using System.Collections.Generic;
using HytaleClient.Application;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface.MainMenu;

internal class TopBarComponent : InterfaceComponent
{
	public readonly MainMenuView MainMenuView;

	private int _selectedNavigationItemMarkerTargetPosition;

	private int _selectedNavigationItemMarkerLerpPosition;

	private bool _mustInitializeMarker;

	private TextButton.TextButtonStyle _buttonStyle;

	private TextButton.TextButtonStyle _buttonSelectedStyle;

	private Dictionary<AppMainMenu.MainMenuPage, TextButton> _buttonMapping = new Dictionary<AppMainMenu.MainMenuPage, TextButton>();

	public Element SelectedMarker { get; private set; }

	public Element HoverMarker { get; private set; }

	public TopBarComponent(MainMenuView mainMenuView)
		: base(mainMenuView.Interface, mainMenuView)
	{
		MainMenuView = mainMenuView;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("MainMenu/TopBar/TopBar.ui", out var document);
		_buttonStyle = document.ResolveNamedValue<TextButton.TextButtonStyle>(Desktop.Provider, "ButtonStyle");
		_buttonSelectedStyle = document.ResolveNamedValue<TextButton.TextButtonStyle>(Desktop.Provider, "ButtonSelectedStyle");
		UIFragment fragment = document.Instantiate(Desktop, this);
		App app = Interface.App;
		fragment.Get<Button>("BackButton").Activating = delegate
		{
			app.MainMenu.Open(AppMainMenu.MainMenuPage.Home);
		};
		SelectedMarker = fragment.Get<Element>("SelectedMarker");
		HoverMarker = fragment.Get<Element>("HoverMarker");
		_buttonMapping.Clear();
		MakeButton("NavigationItemAdventure", AppMainMenu.MainMenuPage.Adventure);
		MakeButton("NavigationItemMinigames", AppMainMenu.MainMenuPage.Minigames);
		MakeButton("NavigationItemServers", AppMainMenu.MainMenuPage.Servers);
		MakeButton("NavigationItemSharedSinglePlayer", AppMainMenu.MainMenuPage.SharedSinglePlayer);
		MakeButton("NavigationItemAvatar", AppMainMenu.MainMenuPage.MyAvatar);
		MakeButton("NavigationItemSettings", AppMainMenu.MainMenuPage.Settings);
		if (base.IsMounted)
		{
			Interface.SocialBar.SetContainer(Find<Group>("SocialBarContainer"));
			Layout();
			_mustInitializeMarker = true;
			OnPageChanged();
		}
		void MakeButton(string id, AppMainMenu.MainMenuPage page)
		{
			TextButton button = fragment.Get<TextButton>(id);
			_buttonMapping.Add(page, button);
			button.Activating = delegate
			{
				OpenPage(page);
			};
			button.MouseEntered = delegate
			{
				UpdateHoveredNavigationItem(button);
			};
			button.MouseExited = delegate
			{
				UpdateHoveredNavigationItem(null);
			};
		}
	}

	private void OpenPage(AppMainMenu.MainMenuPage page, bool confirmed = false)
	{
		if (!confirmed && MainMenuView.MainMenu.HasUnsavedSkinChanges())
		{
			MainMenuView.OpenUnsavedCharacterChangesModal(delegate
			{
				OpenPage(page, confirmed: true);
			});
		}
		else
		{
			MainMenuView.MainMenu.Open(page);
		}
	}

	protected override void OnMounted()
	{
		Interface.SocialBar.SetContainer(Find<Group>("SocialBarContainer"));
		Desktop.RegisterAnimationCallback(Animate);
		_mustInitializeMarker = true;
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	public void OnPageChanged()
	{
		App app = Interface.App;
		if (!_buttonMapping.ContainsKey(app.MainMenu.CurrentPage))
		{
			return;
		}
		foreach (TextButton value in _buttonMapping.Values)
		{
			value.Style = _buttonStyle;
		}
		TextButton textButton = _buttonMapping[app.MainMenu.CurrentPage];
		textButton.Style = _buttonSelectedStyle;
		Rectangle anchoredRectangle = textButton.AnchoredRectangle;
		_selectedNavigationItemMarkerTargetPosition = (int)((float)anchoredRectangle.Left / Desktop.Scale - (float)SelectedMarker.AnchoredRectangle.Width / Desktop.Scale / 2f + (float)anchoredRectangle.Width / 2f / Desktop.Scale) - (int)((float)SelectedMarker.Parent.AnchoredRectangle.Left / Desktop.Scale);
		if (_mustInitializeMarker)
		{
			SelectedMarker.Anchor.Left = (_selectedNavigationItemMarkerLerpPosition = _selectedNavigationItemMarkerTargetPosition);
			_mustInitializeMarker = false;
		}
		Layout();
	}

	public void UpdateHoveredNavigationItem(TextButton topBarButtonComponent)
	{
		if (topBarButtonComponent == null)
		{
			HoverMarker.Visible = false;
			return;
		}
		int num = (int)((float)topBarButtonComponent.AnchoredRectangle.Left / Desktop.Scale);
		int num2 = (int)((float)topBarButtonComponent.AnchoredRectangle.Width / Desktop.Scale / 2f);
		int num3 = (int)((float)HoverMarker.Parent.AnchoredRectangle.Left / Desktop.Scale);
		int num4 = HoverMarker.Anchor.Width.Value / 2;
		HoverMarker.Anchor.Left = num - num4 + num2 - num3;
		HoverMarker.Visible = true;
		Layout();
	}

	private void Animate(float deltaTime)
	{
		if (_selectedNavigationItemMarkerLerpPosition != _selectedNavigationItemMarkerTargetPosition)
		{
			_selectedNavigationItemMarkerLerpPosition = (int)MathHelper.Lerp(_selectedNavigationItemMarkerLerpPosition, _selectedNavigationItemMarkerTargetPosition, System.Math.Min(deltaTime * 10f, 1f));
			SelectedMarker.Anchor.Left = _selectedNavigationItemMarkerLerpPosition;
			SelectedMarker.Layout();
		}
	}
}
