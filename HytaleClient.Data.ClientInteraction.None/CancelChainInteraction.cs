using HytaleClient.Data.ClientInteraction.Client;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class CancelChainInteraction : SimpleInstantInteraction
{
	public CancelChainInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		ChainingInteraction.NamedSequenceData.Remove(Interaction.ChainId);
	}
}
