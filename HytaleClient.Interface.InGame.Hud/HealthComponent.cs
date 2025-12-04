namespace HytaleClient.Interface.InGame.Hud;

internal class HealthComponent : EntityStatBarComponent
{
	public HealthComponent(InGameView view)
		: base(view, "InGame/Hud/Health/Health.ui")
	{
	}

	protected override void UpdateVisibility()
	{
		Interface.InGameView.UpdateHealthVisibility(doLayout: true);
	}
}
