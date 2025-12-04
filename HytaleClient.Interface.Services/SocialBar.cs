using System;
using HytaleClient.Application.Services;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.Services;

internal class SocialBar : InterfaceComponent
{
	private Label _friendsCount;

	private Label _newNotificationsCount;

	private Group _statusIcon;

	private Label _statusLabel;

	private UInt32Color _statusOnlineColor;

	private UInt32Color _statusOfflineColor;

	public SocialBar(Interface @interface, Element parent = null)
		: base(@interface, parent)
	{
	}

	protected override void OnMounted()
	{
		UpdateServiceInformation();
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("Services/SocialBar.ui", out var document);
		_statusOfflineColor = document.ResolveNamedValue<UInt32Color>(Interface, "StatusOfflineColor");
		_statusOnlineColor = document.ResolveNamedValue<UInt32Color>(Interface, "StatusOnlineColor");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		uIFragment.Get<Label>("Username").Text = Interface.App.AuthManager.Settings.Username;
		_friendsCount = uIFragment.Get<Label>("FriendsCount");
		_newNotificationsCount = uIFragment.Get<Label>("NewNotificationsCount");
		_statusIcon = uIFragment.Get<Group>("StatusIcon");
		_statusLabel = uIFragment.Get<Label>("StatusLabel");
	}

	public void SetContainer(Element container)
	{
		if (!Interface.HasMarkupError)
		{
			Parent?.Remove(this);
			container.Add(this);
		}
	}

	public void UpdateServiceInformation()
	{
		if (!base.IsMounted || Interface.HasMarkupError)
		{
			return;
		}
		int num = System.Math.Min(0, 99);
		int num2 = 0;
		foreach (Guid friend in Interface.App.HytaleServices.Friends)
		{
			if (Interface.App.HytaleServices.UserStates.TryGetValue(friend, out var value) && value.Online)
			{
				num2++;
			}
		}
		_friendsCount.Text = Desktop.Provider.FormatNumber(num2);
		if (num > 0)
		{
			_newNotificationsCount.Text = Desktop.Provider.FormatNumber(num);
			_newNotificationsCount.Parent.Visible = true;
		}
		else
		{
			_newNotificationsCount.Parent.Visible = false;
		}
		if (Interface.ServiceState == HytaleServices.ServiceState.Connected)
		{
			_statusLabel.Text = Desktop.Provider.GetText("ui.socialMenu.status.online");
			_statusLabel.Style.TextColor = _statusOnlineColor;
			_statusIcon.Background = new PatchStyle("Services/StatusOnline.png");
		}
		else
		{
			switch (Interface.ServiceState)
			{
			case HytaleServices.ServiceState.Disconnected:
				_statusLabel.Text = Desktop.Provider.GetText("ui.socialMenu.status.disconnected");
				break;
			case HytaleServices.ServiceState.Authenticating:
				_statusLabel.Text = Desktop.Provider.GetText("ui.socialMenu.status.authenticating");
				break;
			case HytaleServices.ServiceState.Connecting:
				_statusLabel.Text = Desktop.Provider.GetText("ui.socialMenu.status.connecting");
				break;
			}
			_statusLabel.Style.TextColor = _statusOfflineColor;
			_statusIcon.Background = new PatchStyle("Services/StatusOffline.png");
		}
		Layout();
	}
}
