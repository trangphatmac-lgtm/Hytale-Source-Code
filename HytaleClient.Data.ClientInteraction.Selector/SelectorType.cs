using System;
using System.Collections.Generic;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;

namespace HytaleClient.Data.ClientInteraction.Selector;

internal abstract class SelectorType
{
	public abstract Selector NewSelector(Random random);

	internal static void SelectNearbyEntities(GameInstance gameInstance, Entity attacker, float range, Action<Entity> consumer, Predicate<Entity> filter)
	{
		Vector3 position = attacker.Position;
		position.Y += attacker.EyeOffset;
		SelectNearbyEntities(gameInstance, attacker, position, range, consumer, filter);
	}

	internal static void SelectNearbyEntities(GameInstance gameInstance, Entity attacker, Vector3 pos, float range, Action<Entity> consumer, Predicate<Entity> filter)
	{
		List<Entity> entitiesInSphere = gameInstance.EntityStoreModule.GetEntitiesInSphere(pos, range);
		foreach (Entity item in entitiesInSphere)
		{
			if (item.NetworkId != attacker.NetworkId && !item.IsDead() && item.IsTangible() && !item.PredictionId.HasValue && (filter == null || filter(item)))
			{
				consumer(item);
			}
		}
	}

	internal static Vector3 GenerateDebugColor()
	{
		Random random = new Random();
		return new Vector3(random.NextFloat(0f, 1f), random.NextFloat(0f, 1f), random.NextFloat(0f, 1f));
	}
}
