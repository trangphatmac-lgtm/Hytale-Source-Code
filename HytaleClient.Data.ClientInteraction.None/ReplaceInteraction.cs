using HytaleClient.Data.Items;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class ReplaceInteraction : ClientInteraction
{
	public ReplaceInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (!ClientInteraction.Failed(context.State.State) && firstRun)
		{
			int interactions = GetInteractions(gameInstance, context);
			if (interactions == int.MinValue)
			{
				context.State.State = (InteractionState)3;
				return;
			}
			context.State.State = (InteractionState)0;
			context.Execute(gameInstance.InteractionModule.RootInteractions[interactions]);
		}
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
	}

	protected override void MatchServer0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		context.State.State = context.ServerData.State;
		if ((int)context.State.State == 0)
		{
			context.Execute(gameInstance.InteractionModule.RootInteractions[context.ServerData.EnteredRootInteraction]);
		}
	}

	public int GetInteractions(GameInstance gameInstance, InteractionContext context)
	{
		int result = Interaction.DefaultValue;
		if (context.OriginalItemType != null)
		{
			ClientItemBase item = gameInstance.ItemLibraryModule.GetItem(context.OriginalItemType);
			int value = int.MinValue;
			if ((item?.InteractionVars?.TryGetValue(Interaction.Variable, out value)).GetValueOrDefault())
			{
				result = value;
			}
		}
		return result;
	}
}
