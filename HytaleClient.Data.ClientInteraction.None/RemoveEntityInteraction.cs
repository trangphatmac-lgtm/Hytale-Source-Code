using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class RemoveEntityInteraction : SimpleInstantInteraction
{
	public RemoveEntityInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		Entity entity = ClientInteraction.GetEntity(gameInstance, context, Interaction.EntityTarget);
		if (entity != null)
		{
			entity.Removed = true;
		}
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		Entity entity = ClientInteraction.GetEntity(gameInstance, context, Interaction.EntityTarget);
		if (entity != null)
		{
			entity.Removed = false;
		}
	}
}
