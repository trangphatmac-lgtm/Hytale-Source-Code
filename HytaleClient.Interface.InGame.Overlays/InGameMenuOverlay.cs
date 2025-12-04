using HytaleClient.Interface.Common;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.InGame.Overlays;

internal class InGameMenuOverlay : InterfaceComponent
{
	public readonly InGameView InGameView;

	private Group _settingsOverlay;

	private Group _settingsContainer;

	private bool _areSettingsOpen = false;

	public InGameMenuOverlay(InGameView inGameView)
		: base(inGameView.Interface, null)
	{
		InGameView = inGameView;
	}

	public void Build()
	{
		if (_areSettingsOpen)
		{
			_settingsContainer.Remove(InGameView.Interface.SettingsComponent);
		}
		Clear();
		Interface.TryGetDocument("InGame/Overlays/MenuOverlay.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		uIFragment.Get<TextButton>("ReturnToGame").Activating = delegate
		{
			InGameView.InGame.TryClosePageOrOverlay();
		};
		uIFragment.Get<TextButton>("OpenSettings").Activating = ShowSettings;
		uIFragment.Get<TextButton>("BackToMainMenu").Activating = delegate
		{
			InGameView.InGame.RequestExit();
		};
		uIFragment.Get<TextButton>("BackToDesktop").Activating = delegate
		{
			InGameView.InGame.RequestExit(exitApplication: true);
		};
		_settingsOverlay = uIFragment.Get<Group>("SettingsOverlay");
		_settingsOverlay.Find<Group>("SettingsBackButtonContainer").Add(new BackButton(Interface, Dismiss));
		_settingsContainer = uIFragment.Get<Group>("SettingsContainer");
		if (_areSettingsOpen)
		{
			_settingsContainer.Add(InGameView.Interface.SettingsComponent);
		}
		else
		{
			Remove(_settingsOverlay);
		}
		if (base.IsMounted)
		{
			Interface.SocialBar.SetContainer(Find<Group>("SocialBarContainer"));
		}
	}

	protected override void OnMounted()
	{
		InGameView.InGame.SetSceneBlurEnabled(enabled: true);
		Interface.SocialBar.SetContainer(Find<Group>("SocialBarContainer"));
	}

	protected override void OnUnmounted()
	{
		InGameView.InGame.SetSceneBlurEnabled(enabled: false);
	}

	protected internal override void Dismiss()
	{
		if (_areSettingsOpen)
		{
			CloseSettings();
		}
		else
		{
			InGameView.InGame.TryClosePageOrOverlay();
		}
	}

	public void ShowSettings()
	{
		_areSettingsOpen = true;
		_settingsContainer.Add(InGameView.Interface.SettingsComponent);
		Add(_settingsOverlay);
		Layout();
	}

	private void CloseSettings()
	{
		_areSettingsOpen = false;
		_settingsContainer.Remove(InGameView.Interface.SettingsComponent);
		Remove(_settingsOverlay);
		Layout();
	}
}
