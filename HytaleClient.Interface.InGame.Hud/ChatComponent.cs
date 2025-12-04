using System;
using System.Collections.Generic;
using HytaleClient.Application;
using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using SDL2;

namespace HytaleClient.Interface.InGame.Hud;

internal class ChatComponent : InterfaceComponent
{
	private class ChatLogEntry
	{
		public FormattedMessage Message;

		public float FloatingExpirationTime;

		public Group FloatingGroup;

		public Group FullGroup;
	}

	public const float FloatingChatDuration = 5f;

	public readonly InGameView InGameView;

	private float _currentTime;

	private readonly List<ChatLogEntry> _chatLogEntries = new List<ChatLogEntry>();

	private readonly List<ChatLogEntry> _floatingChatLogEntries = new List<ChatLogEntry>();

	private readonly DropOutStack<string> _sentMessages = new DropOutStack<string>(20);

	private int _sentMessageCursor = -1;

	private Group _floatingContainer;

	private Group _floatingChatLog;

	private Group _fullContainer;

	private Group _fullChatLog;

	private TextField _chatInput;

	private ItemTooltipLayer _itemTooltip;

	private SDL_Keycode? _keyCodeTrigger;

	private bool _discardNextTextInput = false;

	private int _logHeight;

	private int _fontSize;

	private int _messageSpacing;

	private UInt32Color _textColor;

	public bool IsOpen()
	{
		return Interface.App.Stage == App.AppStage.InGame && InGameView.InGame.Instance.Chat.IsOpen;
	}

	public ChatComponent(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
		InGameView = inGameView;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Hud/Chat.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, null);
		_itemTooltip = new ItemTooltipLayer(InGameView)
		{
			ShowArrow = false
		};
		_floatingContainer = uIFragment.Get<Group>("FloatingContainer");
		_floatingChatLog = uIFragment.Get<Group>("FloatingChatLog");
		_fullContainer = uIFragment.Get<Group>("FullContainer");
		_fullChatLog = uIFragment.Get<Group>("FullChatLog");
		_chatInput = uIFragment.Get<TextField>("ChatInput");
		_chatInput.Validating = OnSendMessage;
		_chatInput.KeyDown = delegate(SDL_Keycode keycode)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Invalid comparison between Unknown and I4
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Invalid comparison between Unknown and I4
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Invalid comparison between Unknown and I4
			if ((int)keycode == 1073741905 || (int)keycode == 1073741906)
			{
				_sentMessageCursor = MathHelper.Clamp(_sentMessageCursor + (((int)keycode == 1073741906) ? 1 : (-1)), -1, _sentMessages.Count - 1);
				string value = "";
				if (_sentMessageCursor > -1)
				{
					value = _sentMessages.PeekAt(_sentMessages.Count - _sentMessageCursor - 1);
				}
				_chatInput.Value = value;
			}
		};
		_logHeight = document.ResolveNamedValue<int>(Desktop.Provider, "LogHeight");
		_fontSize = document.ResolveNamedValue<int>(Desktop.Provider, "FontSize");
		_messageSpacing = document.ResolveNamedValue<int>(Desktop.Provider, "MessageSpacing");
		_textColor = document.ResolveNamedValue<UInt32Color>(Desktop.Provider, "TextColor");
		foreach (ChatLogEntry chatLogEntry in _chatLogEntries)
		{
			if (chatLogEntry.FloatingGroup != null)
			{
				chatLogEntry.FloatingGroup.Parent.Remove(chatLogEntry.FloatingGroup);
				_floatingChatLog.Add(chatLogEntry.FloatingGroup);
			}
			chatLogEntry.FullGroup.Parent.Remove(chatLogEntry.FullGroup);
			_fullChatLog.Add(chatLogEntry.FullGroup);
		}
		Add(_fullContainer);
		Add(_floatingContainer);
		_fullContainer.Visible = IsOpen();
		_floatingContainer.Visible = !_fullContainer.Visible;
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
		_chatInput.Value = "";
		_chatLogEntries.Clear();
		_sentMessages.Clear();
		_sentMessageCursor = -1;
		_currentTime = 0f;
		_floatingContainer.Visible = true;
		_fullContainer.Visible = false;
		_floatingChatLog.Clear();
		_floatingChatLogEntries.Clear();
		_fullChatLog.Clear();
		_itemTooltip.Stop();
	}

	private void Animate(float deltaTime)
	{
		bool flag = false;
		_currentTime += deltaTime;
		while (_floatingChatLogEntries.Count > 0)
		{
			ChatLogEntry chatLogEntry = _floatingChatLogEntries[0];
			if (chatLogEntry.FloatingExpirationTime < _currentTime)
			{
				_floatingChatLogEntries.RemoveAt(0);
				_floatingChatLog.Remove(chatLogEntry.FloatingGroup);
				chatLogEntry.FloatingGroup = null;
				flag = true;
				continue;
			}
			break;
		}
		if (flag && !IsOpen() && InGameView.InGame.IsHudVisible)
		{
			LayoutFloatingChat();
		}
	}

	internal void OnHudVisibilityChanged()
	{
		ApplyVisibility();
	}

	internal void OnOpened(SDL_Keycode? keyCodeTrigger, bool isCommand)
	{
		_keyCodeTrigger = keyCodeTrigger;
		_chatInput.Value = (isCommand ? "/" : "");
		ApplyVisibility();
	}

	internal void OnClosed()
	{
		_chatInput.Value = "";
		ApplyVisibility();
	}

	private void ApplyVisibility()
	{
		bool flag = IsOpen();
		_floatingContainer.Visible = !flag && InGameView.InGame.IsHudVisible;
		_fullContainer.Visible = flag;
		if (flag)
		{
			_fullChatLog.SetScroll(0, int.MaxValue);
			Desktop.FocusElement(this);
		}
		else if (InGameView.InGame.IsHudVisible)
		{
			_floatingChatLog.SetScroll(0, int.MaxValue);
			if (Desktop.FocusedElement == this)
			{
				Desktop.FocusElement(null);
			}
		}
		Layout();
	}

	private void OnSendMessage()
	{
		string text = _chatInput.Value.Trim();
		if (!IsOpen())
		{
			return;
		}
		_chatInput.Value = "";
		if (text.Length > 0)
		{
			if (_sentMessages.PeekAt(_sentMessages.Count - 1) != text)
			{
				_sentMessages.Push(text);
			}
			_sentMessageCursor = -1;
			InGameView.InGame.SendChatMessageOrExecuteCommand(text);
		}
		InGameView.InGame.Instance.Chat.Close();
	}

	protected internal override void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		_discardNextTextInput = (SDL_Keycode?)keycode == _keyCodeTrigger && repeat == 0;
	}

	protected internal override void OnKeyUp(SDL_Keycode keycode)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		base.OnKeyUp(keycode);
		if (IsOpen())
		{
			Desktop.FocusElement(_chatInput);
		}
	}

	protected internal override void OnTextInput(string text)
	{
		if (!_discardNextTextInput)
		{
			_chatInput.OnTextInput(text);
		}
	}

	public void OnReceiveMessage(FormattedMessage message)
	{
		ChatLogEntry chatLogEntry = new ChatLogEntry
		{
			Message = message,
			FloatingGroup = new Group(Desktop, _floatingChatLog)
			{
				LayoutMode = LayoutMode.Top
			},
			FullGroup = new Group(Desktop, _fullChatLog)
			{
				LayoutMode = LayoutMode.Top
			},
			FloatingExpirationTime = _currentTime + 5f
		};
		_chatLogEntries.Add(chatLogEntry);
		_floatingChatLogEntries.Add(chatLogEntry);
		while (_fullChatLog.Children.Count > Interface.App.Settings.MaxChatMessages)
		{
			_fullChatLog.RemoveAt(0);
		}
		List<Label.LabelSpan> labelSpans = FormattedMessageConverter.GetLabelSpans(message, Interface, new SpanStyle
		{
			Color = _textColor
		});
		new Label(Desktop, chatLogEntry.FloatingGroup)
		{
			Anchor = 
			{
				Vertical = _messageSpacing
			},
			Style = 
			{
				Wrap = true,
				FontSize = _fontSize
			},
			TextSpans = labelSpans
		};
		new Label(Desktop, chatLogEntry.FullGroup)
		{
			Anchor = 
			{
				Vertical = _messageSpacing
			},
			Style = 
			{
				Wrap = true,
				FontSize = _fontSize
			},
			TextSpans = labelSpans,
			TagMouseEntered = OnHoverTag
		};
		if (base.IsMounted)
		{
			if (IsOpen())
			{
				_fullChatLog.Layout();
			}
			else if (InGameView.InGame.IsHudVisible)
			{
				LayoutFloatingChat();
			}
		}
	}

	private void OnHoverTag(Label.LabelSpanPortion portion = null)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		if (portion == null)
		{
			_itemTooltip.Stop();
			return;
		}
		ChatTagType val = (ChatTagType)Enum.Parse(typeof(ChatTagType), portion.Span.Params["tagType"].ToString());
		ChatTagType val2 = val;
		if ((int)val2 == 0)
		{
			ClientItemStack stack = new ClientItemStack((string)portion.Span.Params["id"]);
			_itemTooltip.UpdateTooltip(portion.CenterPoint, stack);
			_itemTooltip.Start();
		}
	}

	public void InsertItemTag(string itemId)
	{
		if (!IsOpen())
		{
			InGameView.InGame.Instance.Chat.TryOpen();
		}
		_chatInput.InsertAtCursor(" <Item:" + itemId + ">");
		Desktop.FocusElement(_chatInput);
	}

	private void LayoutFloatingChat(bool layoutChildren = true)
	{
		if (_floatingChatLog.Children.Count == 0)
		{
			_floatingContainer.Visible = false;
			return;
		}
		if (!_floatingContainer.Visible || layoutChildren)
		{
			_floatingContainer.Visible = true;
			_floatingContainer.Layout(_rectangleAfterPadding);
		}
		int num = _floatingChatLog.Anchor.Top.Value * 2 - 2;
		foreach (Element child in _floatingChatLog.Children)
		{
			num += Desktop.UnscaleRound(child.AnchoredRectangle.Height);
		}
		if (num >= _logHeight)
		{
			num = _logHeight;
		}
		_floatingContainer.Anchor.Height = num;
		_floatingContainer.Layout(null, layoutChildren: false);
	}

	protected override void AfterChildrenLayout()
	{
		if (!IsOpen() && InGameView.InGame.IsHudVisible)
		{
			LayoutFloatingChat(layoutChildren: false);
		}
	}

	protected internal override void Dismiss()
	{
		InGameView.InGame.Instance.Chat.Close();
	}
}
