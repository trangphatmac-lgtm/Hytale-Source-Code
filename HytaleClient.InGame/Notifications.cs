using System;
using HytaleClient.Data.Items;
using HytaleClient.Interface.Messages;
using HytaleClient.Protocol;
using NLog;
using Utf8Json;

namespace HytaleClient.InGame;

internal class Notifications
{
	public class ClientNotification
	{
		public FormattedMessage Message;

		public FormattedMessage SecondaryMessage;

		public NotificationStyle Style;

		public string Icon;

		public ClientItemStack Item;

		public ClientNotification(Notification notification)
		{
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			if (notification.Message == null)
			{
				throw new Exception("Message cannot be empty");
			}
			Message = JsonSerializer.Deserialize<FormattedMessage>(notification.Message);
			if (notification.SecondaryMessage != null)
			{
				SecondaryMessage = JsonSerializer.Deserialize<FormattedMessage>(notification.SecondaryMessage);
			}
			Style = notification.Style;
			Icon = notification.Icon;
			if (notification.Item_ != null)
			{
				Item = new ClientItemStack(notification.Item_);
			}
		}

		public ClientNotification(FormattedMessage message, NotificationStyle style, string icon)
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			Message = message;
			Style = style;
			Icon = icon;
		}
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly GameInstance _gameInstance;

	public Notifications(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
	}

	public void AddNotification(string message, string icon)
	{
		FormattedMessage message2 = new FormattedMessage
		{
			RawText = message
		};
		_gameInstance.App.Interface.InGameView.NotificationFeedComponent.OnReceiveNotification(new ClientNotification(message2, (NotificationStyle)0, icon));
	}

	public void AddNotification(Notification notification)
	{
		ClientNotification notification2;
		try
		{
			notification2 = new ClientNotification(notification);
		}
		catch (Exception ex)
		{
			_gameInstance.Chat.Error("Failed to parse notification!");
			Logger.Error<Exception>(ex);
			return;
		}
		_gameInstance.App.Interface.InGameView.NotificationFeedComponent.OnReceiveNotification(notification2);
	}
}
