using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Protocol;

namespace HytaleClient.Data.EntityUI.CombatText;

public class PositionAnimationEvent : AnimationEvent
{
	public Vector2f PositionOffset;

	public PositionAnimationEvent(CombatTextEntityUIComponentAnimationEvent animationEvent)
		: base(animationEvent)
	{
		PositionOffset = animationEvent.PositionOffset;
	}

	public override void ApplyAnimationState(ref EntityUIDrawTask task, float progress)
	{
		if (!(progress < StartAt) && !(progress > EndAt))
		{
			float num = (progress - StartAt) / (EndAt - StartAt);
			if (PositionOffset.X != 0f)
			{
				task.TransformationMatrix.M41 += num * PositionOffset.X;
			}
			if (PositionOffset.Y != 0f)
			{
				task.TransformationMatrix.M42 += num * PositionOffset.Y;
			}
		}
	}
}
