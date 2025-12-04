using System.Collections.Generic;
using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.InGame;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame.Hud;

internal class NotificationFeedComponent : InterfaceComponent
{
	private class Notification
	{
		public FormattedMessage Message;

		public FormattedMessage SecondaryMessage;

		public float ExpirationTime;

		public NotificationStyle Style;

		public string Icon;

		public ClientItemStack Item;

		public Group Element;
	}

	public const float NotificationDuration = 5f;

	private float _currentTime;

	private InGameView _inGameView;

	private Group _notificationFeed;

	private ItemGrid.ItemGridStyle _itemGridStyle;

	private List<Notification> _notifications = new List<Notification>();

	public NotificationFeedComponent(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
		_inGameView = inGameView;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Hud/NotificationFeed.ui", out var document);
		_itemGridStyle = document.ResolveNamedValue<ItemGrid.ItemGridStyle>(Interface, "ItemGridStyle");
		_itemGridStyle.SlotBackground = null;
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_notificationFeed = uIFragment.Get<Group>("NotificationFeed");
		RebuildFeed();
	}

	protected override void OnMounted()
	{
		_currentTime = 0f;
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	public void ResetState()
	{
		_currentTime = 0f;
		_notifications.Clear();
		_notificationFeed.Clear();
	}

	public void RebuildFeed()
	{
		_notificationFeed.Clear();
		Interface.TryGetDocument("InGame/Hud/Notification.ui", out var document);
		foreach (Notification notification in _notifications)
		{
			AddNotification(notification, document);
		}
		_notificationFeed.Layout();
	}

	private void Animate(float deltaTime)
	{
		bool flag = false;
		_currentTime += deltaTime;
		while (_notifications.Count > 0)
		{
			Notification notification = _notifications[0];
			if (notification.ExpirationTime < _currentTime)
			{
				_notifications.RemoveAt(0);
				_notificationFeed.Remove(notification.Element);
				notification.Element = null;
				flag = true;
				continue;
			}
			break;
		}
		if (flag)
		{
			_notificationFeed.Layout();
		}
	}

	private void AddNotification(Notification notification, Document doc)
	{
		UIFragment uIFragment = doc.Instantiate(Desktop, _notificationFeed);
		UInt32Color value = doc.ResolveNamedValue<UInt32Color>(Interface, "MessageColor" + ((object)(NotificationStyle)(ref notification.Style)).ToString());
		uIFragment.Get<Label>("Message").TextSpans = FormattedMessageConverter.GetLabelSpans(notification.Message, Interface, new SpanStyle
		{
			Color = value
		});
		if (notification.SecondaryMessage != null)
		{
			UInt32Color value2 = doc.ResolveNamedValue<UInt32Color>(Interface, "SecondaryMessageColor" + ((object)(NotificationStyle)(ref notification.Style)).ToString());
			uIFragment.Get<Label>("SecondaryMessage").TextSpans = FormattedMessageConverter.GetLabelSpans(notification.SecondaryMessage, Interface, new SpanStyle
			{
				Color = value2
			});
		}
		else
		{
			uIFragment.Get<Label>("SecondaryMessage").Visible = false;
		}
		ClientItemBase value3;
		if (notification.Icon != null && _inGameView.TryMountAssetTexture(notification.Icon, out var textureArea))
		{
			uIFragment.Get<Group>("Icon").Background = new PatchStyle(textureArea);
		}
		else if (notification.Item != null && _inGameView.Items.TryGetValue(notification.Item.Id, out value3))
		{
			ItemGrid itemGrid = new ItemGrid(Desktop, uIFragment.Get<Group>("Icon"));
			itemGrid.SlotsPerRow = 1;
			itemGrid.Slots = new ItemGridSlot[1];
			itemGrid.RenderItemQualityBackground = false;
			itemGrid.Style = _itemGridStyle;
			ItemGrid itemGrid2 = itemGrid;
			itemGrid2.Slots[0] = new ItemGridSlot(notification.Item);
		}
		notification.Element = uIFragment.Get<Group>("Notification");
	}

	public void OnReceiveNotification(Notifications.ClientNotification notification)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		Interface.TryGetDocument("InGame/Hud/Notification.ui", out var document);
		Notification notification2 = new Notification
		{
			Message = notification.Message,
			SecondaryMessage = notification.SecondaryMessage,
			ExpirationTime = _currentTime + 5f,
			Icon = notification.Icon,
			Item = notification.Item,
			Style = notification.Style
		};
		AddNotification(notification2, document);
		_notifications.Add(notification2);
		_notificationFeed.Layout();
	}
}
