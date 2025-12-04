using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction;

internal class SimpleInteraction : ClientInteraction
{
	private const int FailedLabelIndex = 0;

	public SimpleInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		BaseTick(context);
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

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
	}

	protected override void MatchServer0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		context.State.State = context.ServerData.State;
		BaseTick(context);
	}

	private void BaseTick(InteractionContext context)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Invalid comparison between Unknown and I4
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Interaction.WaitForDataFrom_ == 1 && context.ServerData != null && (int)context.ServerData.State == 3)
		{
			context.State.State = (InteractionState)3;
		}
		if ((int)context.State.State == 3 && context.Labels != null)
		{
			context.Jump(context.Labels[0]);
		}
	}
}
