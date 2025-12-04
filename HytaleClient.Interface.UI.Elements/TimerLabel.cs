using System;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class TimerLabel : Label
{
	public enum TimerDirection
	{
		CountDown,
		CountUp
	}

	private float _millisecondsLeft;

	private float _milliseconds;

	[UIMarkupProperty]
	public TimerDirection Direction;

	[UIMarkupProperty]
	public bool Paused;

	[UIMarkupProperty]
	public int Seconds
	{
		set
		{
			_milliseconds = value;
			_millisecondsLeft = value;
		}
	}

	public TimerLabel(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
		UpdateText();
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	private void Animate(float deltaTime)
	{
		if (_millisecondsLeft != 0f && !Paused)
		{
			_millisecondsLeft -= deltaTime;
			if (_millisecondsLeft < 0f)
			{
				_millisecondsLeft = 0f;
			}
			UpdateText();
		}
	}

	private void UpdateText()
	{
		float num = ((Direction == TimerDirection.CountUp) ? (_milliseconds - _millisecondsLeft) : _millisecondsLeft);
		double num2 = System.Math.Floor(num);
		int num3 = (int)System.Math.Floor(num2 / 3600.0);
		int num4 = (int)System.Math.Floor(num2 / 60.0);
		int num5 = (int)num2 % 60;
		base.Text = ((num3 > 0) ? $"{num3:D2}:{num4:D2}:{num5:D2}" : $"{num4:D2}:{num5:D2}");
		Layout();
	}
}
