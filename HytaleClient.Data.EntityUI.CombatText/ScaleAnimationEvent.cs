using System;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Protocol;

namespace HytaleClient.Data.EntityUI.CombatText;

public class ScaleAnimationEvent : AnimationEvent
{
	public float StartScale;

	public float EndScale;

	public ScaleAnimationEvent(CombatTextEntityUIComponentAnimationEvent animationEvent)
		: base(animationEvent)
	{
		StartScale = animationEvent.StartScale;
		EndScale = animationEvent.EndScale;
	}

	public override void ApplyAnimationState(ref EntityUIDrawTask task, float progress)
	{
		if (progress < StartAt)
		{
			task.Scale = StartScale;
			return;
		}
		if (progress > EndAt)
		{
			task.Scale = EndScale;
			return;
		}
		float num = System.Math.Abs(StartScale - EndScale) * (progress - StartAt) / (EndAt - StartAt);
		task.Scale = ((StartScale < EndScale) ? (EndScale + num) : (StartScale - num));
	}
}
