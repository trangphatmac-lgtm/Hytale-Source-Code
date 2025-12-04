using HytaleClient.Data.Map;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class ChangeStateInteraction : SimpleBlockInteraction
{
	public ChangeStateInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void InteractWithBlock(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context, BlockPosition targetBlockHit, int blockId)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		ClientBlockType clientBlockType = gameInstance.MapModule.ClientBlockTypes[blockId];
		string value = null;
		clientBlockType.StatesReverse?.TryGetValue(blockId, out value);
		value = value ?? "default";
		if (Interaction.StateChanges.TryGetValue(value, out var value2) && clientBlockType.States != null && clientBlockType.States.TryGetValue(value2, out var value3))
		{
			gameInstance.MapModule.SetClientBlock(targetBlockHit.X, targetBlockHit.Y, targetBlockHit.Z, value3);
		}
		else
		{
			context.State.State = (InteractionState)3;
		}
	}
}
