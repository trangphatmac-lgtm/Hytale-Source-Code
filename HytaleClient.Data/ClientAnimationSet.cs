using System.Collections.Generic;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.Data;

internal class ClientAnimationSet
{
	public readonly string Id;

	public readonly List<EntityAnimation> Animations = new List<EntityAnimation>();

	public Rangef PassiveNextDelay;

	public float WeightSum;

	public ClientAnimationSet(string id)
	{
		Id = id;
	}

	public ClientAnimationSet(string id, ClientAnimationSet clientAnimationSet)
	{
		Id = id;
		Animations = clientAnimationSet.Animations;
		PassiveNextDelay = clientAnimationSet.PassiveNextDelay;
		WeightSum = clientAnimationSet.WeightSum;
	}

	public EntityAnimation GetWeightedAnimation(int seed)
	{
		if (Animations.Count == 0)
		{
			return null;
		}
		if (Animations.Count == 1)
		{
			return Animations[0];
		}
		if (WeightSum == 0f)
		{
			return Animations[seed % Animations.Count];
		}
		float num = StaticRandom.NextFloat(seed, 0f, WeightSum);
		EntityAnimation result = null;
		foreach (EntityAnimation animation in Animations)
		{
			result = animation;
			num -= animation.Weight;
			if (num <= 0f)
			{
				break;
			}
		}
		return result;
	}
}
