using System.Collections.Generic;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.Services;

internal class QueueStatus : InterfaceComponent
{
	private Label _statusTextLabel;

	private Label _queueNameLabel;

	public QueueStatus(Interface @interface)
		: base(@interface, null)
	{
		base.Visible = false;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("Services/QueueStatus.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_statusTextLabel = uIFragment.Get<Label>("StatusText");
		_queueNameLabel = uIFragment.Get<Label>("QueueName");
		uIFragment.Get<TextButton>("LeaveButton").Activating = delegate
		{
			Interface.TriggerEvent("services.leaveGameQueue");
		};
	}

	public void Update()
	{
		if (!Interface.HasMarkupError)
		{
			if (Interface.QueueTicketName != null)
			{
				base.Visible = true;
				_statusTextLabel.Text = Interface.QueueTicketStatus ?? "";
				_queueNameLabel.Text = Interface.GetText("ui.socialMenu.queuedFor", new Dictionary<string, string> { { "game", Interface.QueueTicketName } });
				Layout(Parent.ContainerRectangle);
			}
			else
			{
				base.Visible = false;
			}
		}
	}
}
