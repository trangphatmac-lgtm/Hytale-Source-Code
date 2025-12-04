using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class WieldingInteraction : ChargingInteraction
{
	public WieldingInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Invalid comparison between Unknown and I4
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Expected O, but got Unknown
		PlayerEntity localPlayer = gameInstance.LocalPlayer;
		if (localPlayer.HasStaminaDepletedEffect)
		{
			gameInstance.App.Interface.InGameView.Wielding = false;
			gameInstance.App.Interface.InGameView.UpdateStaminaPanelVisibility(doLayout: true);
			gameInstance.App.Interface.InGameView.ReticleComponent.ResetClientEvent();
			localPlayer.ActionCameraSettings = null;
			localPlayer.ModelRenderer.SetCameraNodes(localPlayer.CameraSettings);
			context.State.State = (InteractionState)3;
			context.State.ChargeValue = 0f;
			if (context.Labels != null)
			{
				context.Jump(context.Labels[Interaction.ChargedNext?.Count ?? 0]);
			}
			return;
		}
		base.Tick0(gameInstance, clickType, hasAnyButtonClick, firstRun, time, type, context);
		if (firstRun)
		{
			gameInstance.App.Interface.InGameView.Wielding = true;
			gameInstance.App.Interface.InGameView.UpdateStaminaPanelVisibility(doLayout: true);
			gameInstance.App.Interface.InGameView.ReticleComponent.OnClientEvent((ItemReticleClientEvent)1, context.HeldItem.Id);
			CameraSettings val = new CameraSettings(localPlayer.CameraSettings);
			for (int i = 0; i < val.Yaw.TargetNodes.Length; i++)
			{
				val.Yaw.TargetNodes[i] = (CameraNode)4;
			}
			localPlayer.ActionCameraSettings = val;
			localPlayer.ModelRenderer.SetCameraNodes(val);
		}
		else if ((int)context.State.State == 0)
		{
			gameInstance.App.Interface.InGameView.Wielding = false;
			gameInstance.App.Interface.InGameView.UpdateStaminaPanelVisibility(doLayout: true);
			gameInstance.App.Interface.InGameView.ReticleComponent.ResetClientEvent();
			localPlayer.ActionCameraSettings = null;
			localPlayer.ModelRenderer.SetCameraNodes(localPlayer.CameraSettings);
		}
	}
}
