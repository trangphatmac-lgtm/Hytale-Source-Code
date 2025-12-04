using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Protocol;

namespace HytaleClient.Data.EntityUI.CombatText;

public abstract class AnimationEvent
{
	public float StartAt;

	public float EndAt;

	public AnimationEvent(CombatTextEntityUIComponentAnimationEvent animationEvent)
	{
		StartAt = animationEvent.StartAt;
		EndAt = animationEvent.EndAt;
	}

	public abstract void ApplyAnimationState(ref EntityUIDrawTask task, float progress);
}
