using System.Collections.Generic;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;

namespace HytaleClient.AssetEditor.Interface.Editor;

internal class ToastNotifications : Element
{
	private class ToastNotification
	{
		public Element Element;

		public float TimeLeft;
	}

	private const float Duration = 5f;

	private readonly List<ToastNotification> _notifications = new List<ToastNotification>();

	private readonly List<ToastNotification> _removedNotifications = new List<ToastNotification>();

	public ToastNotifications(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_layoutMode = LayoutMode.Bottom;
		Anchor = new Anchor
		{
			Width = 350,
			Right = 10,
			Bottom = 10
		};
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Clear();
		_notifications.Clear();
		Desktop.UnregisterAnimationCallback(Animate);
	}

	private TextButton AddNotification(AssetEditorPopupNotificationType type)
	{
		Desktop.Provider.TryGetDocument("AssetEditor/ToastNotification.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		TextButton label = uIFragment.Get<TextButton>("Label");
		ToastNotification notification = new ToastNotification
		{
			Element = label,
			TimeLeft = 5f
		};
		label.Background = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "Background" + ((object)(AssetEditorPopupNotificationType)(ref type)).ToString());
		label.Activating = delegate
		{
			_notifications.Remove(notification);
			Remove(label);
			Layout();
		};
		_notifications.Add(notification);
		return label;
	}

	public void AddNotification(AssetEditorPopupNotificationType type, string text)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		TextButton textButton = AddNotification(type);
		textButton.Text = text;
		Layout();
	}

	public void AddNotification(AssetEditorPopupNotificationType type, FormattedMessage message)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		TextButton textButton = AddNotification(type);
		textButton.TextSpans = FormattedMessageConverter.GetLabelSpans(message, Desktop.Provider);
		Layout();
	}

	private void Animate(float deltaTime)
	{
		if (_notifications.Count <= 0)
		{
			return;
		}
		foreach (ToastNotification notification in _notifications)
		{
			notification.TimeLeft -= deltaTime;
			if (notification.TimeLeft <= 0f)
			{
				_removedNotifications.Add(notification);
				Remove(notification.Element);
			}
		}
		if (_removedNotifications.Count <= 0)
		{
			return;
		}
		foreach (ToastNotification removedNotification in _removedNotifications)
		{
			_notifications.Remove(removedNotification);
		}
		_removedNotifications.Clear();
		Layout();
	}
}
