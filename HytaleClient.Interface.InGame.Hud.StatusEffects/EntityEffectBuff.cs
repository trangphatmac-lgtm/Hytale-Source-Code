using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Hud.StatusEffects;

internal class EntityEffectBuff : BaseStatusEffect
{
	public int Id;

	private float _initialCountdown;

	private int _effectId;

	private string _iconPath;

	public EntityEffectBuff(InGameView inGameView, Desktop desktop, Element parent, Entity.UniqueEntityEffect entityEffect, int id)
		: base(inGameView, desktop, parent)
	{
		_initialCountdown = entityEffect.RemainingDuration;
		_effectId = entityEffect.NetworkEffectIndex;
		_iconPath = entityEffect.StatusEffectIcon;
		Id = id;
	}

	public override void Build()
	{
		_renderPercentage = 1f;
		_targetPercentage = 1f;
		base.Build();
		SetBuffColor(BarColor.Green);
		Desktop.Provider.TryGetDocument("InGame/Hud/StatusEffects/StatusEffect.ui", out var document);
		PatchStyle statusEffectBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "BuffBackground");
		SetStatusEffectBackground(statusEffectBackground);
		SetIcon();
		SetArrowVisible(StatusEffectArrows.Buff);
	}

	private void SetIcon()
	{
		if (!InGameView.TryMountAssetTexture(_iconPath, out var textureArea))
		{
			textureArea = Desktop.Provider.MissingTexture;
		}
		PatchStyle background = new PatchStyle(textureArea);
		_buffIcon.Background = background;
	}

	public void SetCountdownPercentage(float remainingTime)
	{
		_targetPercentage = remainingTime / _initialCountdown;
	}

	public void SetInitialCountdown(float initialCountdown)
	{
		_initialCountdown = initialCountdown;
	}

	protected override void Animate(float deltaTime)
	{
		PlayerEntity playerEntity = InGameView.InGame.Instance?.LocalPlayer;
		if (playerEntity == null)
		{
			return;
		}
		Entity.UniqueEntityEffect? uniqueEntityEffect = null;
		Entity.UniqueEntityEffect[] entityEffects = playerEntity.EntityEffects;
		for (int i = 0; i < entityEffects.Length; i++)
		{
			Entity.UniqueEntityEffect value = entityEffects[i];
			if (value.NetworkEffectIndex == _effectId)
			{
				uniqueEntityEffect = value;
			}
		}
		if (uniqueEntityEffect.HasValue)
		{
			SetCountdownPercentage(uniqueEntityEffect.Value.RemainingDuration);
			base.Animate(deltaTime);
		}
	}
}
