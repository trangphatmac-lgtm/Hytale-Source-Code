using System;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;

namespace HytaleClient.Data.ClientInteraction.Selector;

internal interface Selector
{
	void Tick(GameInstance gameInstance, Entity attacker, float time, float runTime);

	void SelectTargetEntities(GameInstance gameInstance, Entity attacker, EntityHitConsumer consumer, Predicate<Entity> filter);
}
