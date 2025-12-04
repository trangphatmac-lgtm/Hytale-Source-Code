using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Hud.StatusEffects;

internal class TrinketBuffStatusEffect : BaseStatusEffect
{
	public string Id;

	protected SoundStyle _buffStartSound;

	protected SoundStyle _buffEffectElapsedSound;

	protected SoundStyle _buffCooldownCompletedSound;

	public TrinketBuffStatusEffect(InGameView InGameView, Desktop desktop, Element parent, string id = "")
		: base(InGameView, desktop, parent)
	{
		Id = id;
	}

	public override void Build()
	{
		_renderPercentage = 1f;
		_targetPercentage = 1f;
		Desktop.Provider.TryGetDocument("InGame/Hud/StatusEffects/StatusEffect.ui", out var document);
		document.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "BuffStartSound", out _buffStartSound);
		document.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "BuffEffectElapsed", out _buffEffectElapsedSound);
		document.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "BuffCooldownCompleted", out _buffCooldownCompletedSound);
		base.Build();
		SetBuffColor(BarColor.Green);
		SetIcon();
		SetArrowVisible(StatusEffectArrows.Buff);
	}

	private void SetIcon()
	{
		string texturePath = "InGame/Hud/StatusEffects/Assets/Icons/" + Id + ".png";
		_buffIcon.Background = new PatchStyle(texturePath);
	}
}
