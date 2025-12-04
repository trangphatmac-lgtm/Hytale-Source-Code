using HytaleClient.Data.ClientInteraction.Client;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class ChainFlagInteraction : SimpleInstantInteraction
{
	public ChainFlagInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		if (!ChainingInteraction.NamedSequenceData.TryGetValue(Interaction.ChainId, out var value))
		{
			ChainingInteraction.NamedSequenceData.Add(Interaction.ChainId, value = new ChainingInteraction.ChainData());
		}
		value.CurrentFlag = Interaction.Flag;
	}
}
