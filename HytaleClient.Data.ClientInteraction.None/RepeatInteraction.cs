using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class RepeatInteraction : SimpleInteraction
{
	public RepeatInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected I4, but got Unknown
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		if (firstRun && Interaction.Repeat != -1)
		{
			context.InstanceStore.RemainingRepeats = Interaction.Repeat;
		}
		if (context.InstanceStore.ForkedChain != null)
		{
			InteractionState clientState = context.InstanceStore.ForkedChain.ClientState;
			InteractionState val = clientState;
			switch ((int)val)
			{
			case 4:
				context.State.State = (InteractionState)4;
				return;
			case 0:
				if (Interaction.Repeat != -1 && context.InstanceStore.RemainingRepeats <= 0)
				{
					context.State.State = (InteractionState)0;
					base.Tick0(gameInstance, clickType, hasAnyButtonClick, firstRun, time, type, context);
					return;
				}
				context.State.State = (InteractionState)4;
				break;
			case 3:
				context.State.State = (InteractionState)3;
				base.Tick0(gameInstance, clickType, hasAnyButtonClick, firstRun, time, type, context);
				return;
			}
		}
		context.InstanceStore.ForkedChain = context.Fork(context.Duplicate(), Interaction.ForkInteractions);
		context.State.State = (InteractionState)4;
		if (Interaction.Repeat != -1)
		{
			context.InstanceStore.RemainingRepeats--;
		}
	}

	protected override void MatchServer0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		context.State.State = (InteractionState)4;
	}
}
