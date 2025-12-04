using System;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.CharacterController;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class MovementConditionInteraction : SimpleInteraction
{
	public MovementConditionInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Expected I4, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		CharacterControllerModule characterControllerModule = gameInstance.CharacterControllerModule;
		bool flag = characterControllerModule.ForwardsTimestamp > characterControllerModule.BackwardsTimestamp;
		bool flag2 = characterControllerModule.BackwardsTimestamp > characterControllerModule.ForwardsTimestamp;
		bool flag3 = characterControllerModule.LeftTimestamp > characterControllerModule.RightTimestamp;
		bool flag4 = characterControllerModule.RightTimestamp > characterControllerModule.LeftTimestamp;
		MovementDirection val = (MovementDirection)0;
		if (flag && flag3 && Interaction.ForwardLeft != int.MinValue)
		{
			val = (MovementDirection)5;
		}
		else if (flag && flag4 && Interaction.ForwardRight != int.MinValue)
		{
			val = (MovementDirection)6;
		}
		else if (flag2 && flag3 && Interaction.BackLeft != int.MinValue)
		{
			val = (MovementDirection)7;
		}
		else if (flag2 && flag4 && Interaction.BackRight != int.MinValue)
		{
			val = (MovementDirection)8;
		}
		else if (flag && Interaction.Forward != int.MinValue)
		{
			val = (MovementDirection)1;
		}
		else if (flag2 && Interaction.Back != int.MinValue)
		{
			val = (MovementDirection)2;
		}
		else if (flag3 && Interaction.Left != int.MinValue)
		{
			val = (MovementDirection)3;
		}
		else if (flag4 && Interaction.Right != int.MinValue)
		{
			val = (MovementDirection)4;
		}
		context.State.MovementDirection_ = val;
		context.State.State = (InteractionState)0;
		MovementDirection val2 = val;
		MovementDirection val3 = val2;
		switch ((int)val3)
		{
		case 0:
			context.Jump(context.Labels[0]);
			break;
		case 1:
			context.Jump(context.Labels[1]);
			break;
		case 2:
			context.Jump(context.Labels[2]);
			break;
		case 3:
			context.Jump(context.Labels[3]);
			break;
		case 4:
			context.Jump(context.Labels[4]);
			break;
		case 5:
			context.Jump(context.Labels[5]);
			break;
		case 6:
			context.Jump(context.Labels[6]);
			break;
		case 7:
			context.Jump(context.Labels[7]);
			break;
		case 8:
			context.Jump(context.Labels[8]);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public override void Compile(InteractionModule module, ClientRootInteraction.OperationsBuilder builder)
	{
		ClientRootInteraction.Label[] array = new ClientRootInteraction.Label[9];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = builder.CreateUnresolvedLabel();
		}
		builder.AddOperation(Id, array);
		ClientRootInteraction.Label label = builder.CreateUnresolvedLabel();
		Resolve(module, builder, Interaction.Failed, array[0], label);
		Resolve(module, builder, Interaction.Forward, array[1], label);
		Resolve(module, builder, Interaction.Back, array[2], label);
		Resolve(module, builder, Interaction.Left, array[3], label);
		Resolve(module, builder, Interaction.Right, array[4], label);
		Resolve(module, builder, Interaction.ForwardLeft, array[5], label);
		Resolve(module, builder, Interaction.ForwardRight, array[6], label);
		Resolve(module, builder, Interaction.BackLeft, array[7], label);
		Resolve(module, builder, Interaction.BackRight, array[8], label);
		builder.ResolveLabel(label);
	}

	public static void Resolve(InteractionModule module, ClientRootInteraction.OperationsBuilder builder, int id, ClientRootInteraction.Label label, ClientRootInteraction.Label endLabel)
	{
		builder.ResolveLabel(label);
		if (id != int.MinValue)
		{
			ClientInteraction clientInteraction = module.Interactions[id];
			clientInteraction.Compile(module, builder);
		}
		builder.Jump(endLabel);
	}
}
