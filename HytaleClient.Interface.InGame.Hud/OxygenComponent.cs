namespace HytaleClient.Interface.InGame.Hud;

internal class OxygenComponent : EntityStatBarComponent
{
	public OxygenComponent(InGameView view)
		: base(view, "InGame/Hud/Oxygen/Oxygen.ui", hideIfFull: true)
	{
	}

	protected override void UpdateVisibility()
	{
		Interface.InGameView.UpdateOxygenVisibility(doLayout: true);
	}
}
