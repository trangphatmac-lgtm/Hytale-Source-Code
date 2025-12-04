using System;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Protocol;

namespace HytaleClient.Data.EntityUI.CombatText;

public class OpacityAnimationEvent : AnimationEvent
{
	public float StartOpacity;

	public float EndOpacity;

	public OpacityAnimationEvent(CombatTextEntityUIComponentAnimationEvent animationEvent)
		: base(animationEvent)
	{
		StartOpacity = animationEvent.StartOpacity;
		EndOpacity = animationEvent.EndOpacity;
	}

	public override void ApplyAnimationState(ref EntityUIDrawTask task, float progress)
	{
		if (progress < StartAt)
		{
			task.Opacity = (byte)(255f * StartOpacity);
			return;
		}
		if (progress > EndAt)
		{
			task.Opacity = (byte)(255f * EndOpacity);
			return;
		}
		float num = System.Math.Abs(StartOpacity - EndOpacity) * (progress - StartAt) / (EndAt - StartAt);
		task.Opacity = (byte)(255f * ((StartOpacity < EndOpacity) ? (StartOpacity + num) : (EndOpacity - num)));
	}
}
