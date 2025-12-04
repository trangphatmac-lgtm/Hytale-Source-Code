using HytaleClient.Data.Entities;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class ConditionInteraction : SimpleInteraction
{
	public ConditionInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		if ((int)context.State.State <= 0)
		{
			bool flag = true;
			if (Interaction.HasRequiredGameMode && Interaction.RequiredGameMode != gameInstance.GameMode)
			{
				flag = false;
			}
			ref ClientMovementStates movementStates = ref gameInstance.CharacterControllerModule.MovementController.MovementStates;
			if (Interaction.HasJumping && Interaction.Jumping != movementStates.IsJumping)
			{
				flag = false;
			}
			if (Interaction.HasSwimming && Interaction.Swimming != movementStates.IsSwimming)
			{
				flag = false;
			}
			if (Interaction.HasCrouching && Interaction.Crouching != movementStates.IsCrouching)
			{
				flag = false;
			}
			if (Interaction.HasRunning && Interaction.Running != movementStates.IsSprinting)
			{
				flag = false;
			}
			if (Interaction.HasFlying && Interaction.Flying != movementStates.IsFlying)
			{
				flag = false;
			}
			context.State.State = (InteractionState)((!flag) ? 3 : 0);
			base.Tick0(gameInstance, clickType, hasAnyButtonClick, firstRun, time, type, context);
		}
	}
}
