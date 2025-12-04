#define DEBUG
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Networking;
using Utf8Json;

namespace HytaleClient.Interface.InGame.Hud;

internal class EventTitleComponent : InterfaceComponent
{
	private class Title
	{
		public string Primary;

		public string Secondary;

		public bool IsMajor;

		public float ExpiresAt;
	}

	public readonly InGameView InGameView;

	private Element _majorTitleContainer;

	private Element _minorTitleContainer;

	private readonly Queue<Title> _queue = new Queue<Title>();

	private Title _currentTitle;

	private bool _isAnimating;

	private float _displayTimer;

	private float _totalPendingDuration;

	public EventTitleComponent(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
		InGameView = inGameView;
		Interface.RegisterForEventFromEngine<PacketHandler.EventTitle>("eventTitle.show", OnShowEventTitle);
		Interface.RegisterForEventFromEngine<int>("eventTitle.hide", OnHideEventTitle);
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Hud/EventTitle/Major.ui", out var document);
		_majorTitleContainer = document.Instantiate(Desktop, null).RootElements[0];
		Interface.TryGetDocument("InGame/Hud/EventTitle/Minor.ui", out var document2);
		_minorTitleContainer = document2.Instantiate(Desktop, null).RootElements[0];
	}

	private void OnShowEventTitle(PacketHandler.EventTitle title)
	{
		FormattedMessage message = JsonSerializer.Deserialize<FormattedMessage>(title.PrimaryTitle);
		FormattedMessage message2 = JsonSerializer.Deserialize<FormattedMessage>(title.SecondaryTitle);
		Queue(FormattedMessageConverter.GetString(message, InGameView.Interface), FormattedMessageConverter.GetString(message2, InGameView.Interface), title.IsMajor, title.Duration);
	}

	private void OnHideEventTitle(int fadeDuration)
	{
		ResetState();
	}

	protected override void OnUnmounted()
	{
		if (_isAnimating)
		{
			StopAnimation();
		}
	}

	public void ResetState()
	{
		Clear();
		if (_isAnimating)
		{
			StopAnimation();
		}
		_displayTimer = 0f;
		_totalPendingDuration = 0f;
		_currentTitle = null;
	}

	private void Animate(float deltaTime)
	{
		_displayTimer += deltaTime;
		Debug.Assert(_currentTitle != null || _queue.Count > 0);
		if (_currentTitle != null)
		{
			if (_currentTitle.ExpiresAt > _displayTimer)
			{
				return;
			}
			if (_queue.Count == 0)
			{
				ResetState();
				return;
			}
		}
		Show(_queue.Dequeue());
	}

	public void Queue(string primaryTitle, string secondaryTitle = null, bool isMajor = true, float duration = 4f)
	{
		_totalPendingDuration += duration;
		_queue.Enqueue(new Title
		{
			Primary = primaryTitle,
			Secondary = secondaryTitle,
			IsMajor = isMajor,
			ExpiresAt = _totalPendingDuration
		});
		if (!_isAnimating)
		{
			StartAnimation();
		}
	}

	private void StartAnimation()
	{
		Debug.Assert(!_isAnimating);
		Desktop.RegisterAnimationCallback(Animate);
		_isAnimating = true;
	}

	private void StopAnimation()
	{
		Debug.Assert(_isAnimating);
		Desktop.UnregisterAnimationCallback(Animate);
		_isAnimating = false;
	}

	private void Show(Title title)
	{
		Clear();
		_currentTitle = title;
		Add(title.IsMajor ? _majorTitleContainer : _minorTitleContainer);
		Find<Label>("PrimaryTitle").Text = title.Primary;
		Find<Label>("SecondaryTitle").Text = title.Secondary;
		Layout();
	}
}
