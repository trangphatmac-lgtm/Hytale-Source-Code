using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.InGame.Modules.Machinima;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Client;

internal class MachinimaTool : ClientTool
{
	private readonly MachinimaModule _machinima;

	public override string ToolId => "Machinima";

	public MachinimaTool(GameInstance gameInstance)
		: base(gameInstance)
	{
		_machinima = _gameInstance.MachinimaModule;
	}

	public override void OnInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		if (clickType != InteractionModule.ClickType.None)
		{
			_machinima.OnInteraction(interactionType);
		}
	}
}
