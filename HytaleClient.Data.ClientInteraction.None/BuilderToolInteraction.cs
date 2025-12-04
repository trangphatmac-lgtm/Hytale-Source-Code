using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class BuilderToolInteraction : SimpleInteraction
{
	public BuilderToolInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
		interaction.AllowIndefiniteHold = true;
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		gameInstance.BuilderToolsModule.OnInteraction(type, clickType, context, firstRun);
		base.Tick0(gameInstance, clickType, hasAnyButtonClick, firstRun, time, type, context);
	}
}
