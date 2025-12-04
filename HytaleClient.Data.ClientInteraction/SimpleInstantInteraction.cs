using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction;

internal abstract class SimpleInstantInteraction : SimpleInteraction
{
	public SimpleInstantInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		if (firstRun)
		{
			FirstRun(gameInstance, clickType, hasAnyButtonClick, type, context);
			base.Tick0(gameInstance, clickType, hasAnyButtonClick, firstRun, time, type, context);
		}
	}

	protected abstract void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context);
}
