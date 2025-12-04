using System.Collections.Generic;

namespace HytaleClient.Data.BlockyModels;

public struct BlockyAnimationJson
{
	public int Duration;

	public bool HoldLastKeyframe;

	public IDictionary<string, AnimationNode> NodeAnimations;
}
