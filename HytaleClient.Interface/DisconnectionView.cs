using System.Collections.Generic;
using HytaleClient.Application;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Utils;

namespace HytaleClient.Interface;

internal class DisconnectionView : InterfaceComponent
{
	private AppDisconnection _appDisconnection;

	private Label _titleLabel;

	private Label _reasonLabel;

	private Label _detailsLabel;

	private TextButton _reconnectButton;

	private float _secondsUntilAutoReconnect;

	private float _secondsUntilDoReconnect;

	public DisconnectionView(Interface @interface)
		: base(@interface, null)
	{
		_appDisconnection = @interface.App.Disconnection;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("Disconnection.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_titleLabel = uIFragment.Get<Label>("Title");
		_reasonLabel = uIFragment.Get<Label>("Reason");
		_detailsLabel = uIFragment.Get<Label>("Details");
		uIFragment.Get<TextButton>("BackToMainMenuButton").Activating = Dismiss;
		_reconnectButton = uIFragment.Get<TextButton>("ReconnectButton");
		_reconnectButton.Activating = Reconnect;
		if (base.IsMounted)
		{
			Setup();
		}
	}

	protected override void OnMounted()
	{
		Setup();
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	private void Animate(float deltaTime)
	{
		if (_secondsUntilDoReconnect > 0f)
		{
			_secondsUntilDoReconnect -= deltaTime;
			if (_secondsUntilDoReconnect <= 0f)
			{
				_appDisconnection.Reconnect();
			}
		}
		else if (!_reconnectButton.Disabled && _secondsUntilAutoReconnect != 0f)
		{
			_secondsUntilAutoReconnect -= deltaTime;
			_reconnectButton.Text = Desktop.Provider.GetText("ui.disconnection.autoConnectingIn", new Dictionary<string, string> { 
			{
				"seconds",
				((int)_secondsUntilAutoReconnect).ToString()
			} });
			_reconnectButton.Layout();
			if (_secondsUntilAutoReconnect <= 0f)
			{
				Reconnect();
			}
		}
	}

	private void Setup()
	{
		_secondsUntilAutoReconnect = OptionsHelper.AutoReconnectDelay;
		_secondsUntilDoReconnect = 0f;
		_titleLabel.Text = Desktop.Provider.GetText(_appDisconnection.DisconnectedOnLoadingScreen ? "ui.disconnection.titleFailedToConnect" : "ui.disconnection.titleDisconnected");
		_reconnectButton.Text = Desktop.Provider.GetText(_appDisconnection.DisconnectedOnLoadingScreen ? "ui.disconnection.tryAgain" : "ui.disconnection.reconnect");
		_reconnectButton.Disabled = false;
		_reasonLabel.Text = _appDisconnection.Reason ?? Desktop.Provider.GetText("ui.disconnection.errors.unexpectedError");
		_detailsLabel.Text = "(" + _appDisconnection.ExceptionMessage + ")";
		_detailsLabel.Parent.Visible = _appDisconnection.Reason == null;
		Interface.SocialBar.SetContainer(Find<Group>("SocialBarContainer"));
	}

	protected internal override void Validate()
	{
		Dismiss();
	}

	protected internal override void Dismiss()
	{
		Interface.App.MainMenu.Open(Interface.App.MainMenu.CurrentPage);
	}

	private void Reconnect()
	{
		if (!_reconnectButton.Disabled)
		{
			_secondsUntilDoReconnect = 0.2f;
			_reconnectButton.Text = Desktop.Provider.GetText(_appDisconnection.DisconnectedOnLoadingScreen ? "ui.disconnection.tryAgain.inProgress" : "ui.disconnection.reconnect.inProgress");
			_reconnectButton.Disabled = true;
			_reconnectButton.Layout();
		}
	}
}
