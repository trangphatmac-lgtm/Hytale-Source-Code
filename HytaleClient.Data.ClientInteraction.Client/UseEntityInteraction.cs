using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class UseEntityInteraction : SimpleInstantInteraction
{
	public UseEntityInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		if (context.MetaStore.TargetEntity == null)
		{
			context.State.State = (InteractionState)3;
			return;
		}
		Entity targetEntity = context.MetaStore.TargetEntity;
		context.State.EntityId = targetEntity.NetworkId;
		if (targetEntity.Interactions.TryGetValue(type, out var value))
		{
			context.State.State = (InteractionState)0;
			context.Execute(gameInstance.InteractionModule.RootInteractions[value]);
		}
		else
		{
			context.State.State = (InteractionState)3;
		}
	}

	protected override void MatchServer0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		context.State.State = context.ServerData.State;
		if ((int)context.State.State == 0)
		{
			context.Execute(gameInstance.InteractionModule.RootInteractions[context.ServerData.EnteredRootInteraction]);
		}
		base.MatchServer0(gameInstance, clickType, hasAnyButtonClick, type, context);
	}
}
