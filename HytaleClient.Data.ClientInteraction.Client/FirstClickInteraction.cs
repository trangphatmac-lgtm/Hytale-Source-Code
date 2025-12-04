using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class FirstClickInteraction : ClientInteraction
{
	private const int FailedLabelIndex = 0;

	public FirstClickInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		if (clickType != 0 && context.Labels != null)
		{
			context.State.State = (InteractionState)3;
			context.Jump(context.Labels[0]);
		}
		else
		{
			context.State.State = (InteractionState)0;
		}
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
	}

	protected override void MatchServer0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Tick0(gameInstance, clickType, hasAnyButtonClick, firstRun: true, 0f, type, context);
	}

	public override void Compile(InteractionModule module, ClientRootInteraction.OperationsBuilder builder)
	{
		if (Interaction.Next == int.MinValue && Interaction.Failed == int.MinValue)
		{
			builder.AddOperation(Id);
			return;
		}
		ClientRootInteraction.Label label = builder.CreateUnresolvedLabel();
		ClientRootInteraction.Label label2 = builder.CreateUnresolvedLabel();
		builder.AddOperation(Id, label);
		if (Interaction.Next != int.MinValue)
		{
			ClientInteraction clientInteraction = module.Interactions[Interaction.Next];
			clientInteraction.Compile(module, builder);
		}
		if (Interaction.Failed != int.MinValue)
		{
			builder.Jump(label2);
		}
		builder.ResolveLabel(label);
		if (Interaction.Failed != int.MinValue)
		{
			ClientInteraction clientInteraction2 = module.Interactions[Interaction.Failed];
			clientInteraction2.Compile(module, builder);
		}
		builder.ResolveLabel(label2);
	}
}
