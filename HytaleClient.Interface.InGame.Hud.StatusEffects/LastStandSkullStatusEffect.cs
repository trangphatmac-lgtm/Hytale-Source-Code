using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Hud.StatusEffects;

internal class LastStandSkullStatusEffect : TrinketBuffStatusEffect
{
	private bool _isLastStandSkullActive = false;

	private float _initialCountdown;

	private int _lastStandSkullEffectId;

	public static readonly string _lastStandSkullName = "Trinket_Last_Stand_Skull";

	public LastStandSkullStatusEffect(InGameView InGameView, Desktop desktop, Element parent, string id = "")
		: base(InGameView, desktop, parent, id)
	{
		_lastStandSkullEffectId = InGameView.InGame.Instance.EntityStoreModule.EntityEffectIndicesByIds[_lastStandSkullName];
	}

	public override void Build()
	{
		base.Build();
		_renderPercentage = 0f;
		_targetPercentage = 0f;
		SetBuffColor(BarColor.Green);
		SetDisabledIcon();
		AnimateBars();
		SetArrowVisible(StatusEffectArrows.BuffDisabled);
		SetDisabledBackground();
	}

	private void SetDisabledBackground()
	{
		Desktop.Provider.TryGetDocument("InGame/Hud/StatusEffects/StatusEffect.ui", out var document);
		PatchStyle statusEffectBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "DisabledBackground");
		SetStatusEffectBackground(statusEffectBackground);
	}

	private void SetEnabledBackground()
	{
		Desktop.Provider.TryGetDocument("InGame/Hud/StatusEffects/StatusEffect.ui", out var document);
		PatchStyle statusEffectBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "BuffBackground");
		SetStatusEffectBackground(statusEffectBackground);
	}

	private void SetIcon()
	{
		string texturePath = "InGame/Hud/StatusEffects/Assets/Icons/" + Id + ".png";
		_buffIcon.Background = new PatchStyle(texturePath);
		_buffIcon.Layout();
	}

	private void SetDisabledIcon()
	{
		string texturePath = "InGame/Hud/StatusEffects/Assets/Icons/" + Id + "_Disabled.png";
		_buffIcon.Background = new PatchStyle(texturePath);
		_buffIcon.Layout();
	}

	public void OnEffectAdded(int effectIndex)
	{
		if (effectIndex != _lastStandSkullEffectId)
		{
			return;
		}
		_isLastStandSkullActive = true;
		Entity.UniqueEntityEffect? lastStandSkullEffect = GetLastStandSkullEffect();
		if (lastStandSkullEffect.HasValue)
		{
			_initialCountdown = lastStandSkullEffect.Value.RemainingDuration;
			SetArrowVisible(StatusEffectArrows.Buff);
			SetEnabledBackground();
			SetIcon();
			if (_buffStartSound != null)
			{
				Desktop.Provider.PlaySound(_buffStartSound);
			}
		}
	}

	public void OnEffectRemoved(int effectIndex)
	{
		if (effectIndex == _lastStandSkullEffectId)
		{
			_isLastStandSkullActive = false;
			_targetPercentage = 0f;
			_renderPercentage = 0f;
			AnimateBars();
			SetArrowVisible(StatusEffectArrows.BuffDisabled);
			SetDisabledBackground();
			SetDisabledIcon();
			if (_buffEffectElapsedSound != null)
			{
				Desktop.Provider.PlaySound(_buffEffectElapsedSound);
			}
		}
	}

	protected override void Animate(float deltaTime)
	{
		if (_isLastStandSkullActive)
		{
			Entity.UniqueEntityEffect? lastStandSkullEffect = GetLastStandSkullEffect();
			if (lastStandSkullEffect.HasValue)
			{
				SetCountdownPercentage(lastStandSkullEffect.Value.RemainingDuration);
				base.Animate(deltaTime);
			}
		}
	}

	public void SetCountdownPercentage(float remainingTime)
	{
		_targetPercentage = remainingTime / _initialCountdown;
	}

	private Entity.UniqueEntityEffect? GetLastStandSkullEffect()
	{
		PlayerEntity playerEntity = InGameView.InGame.Instance?.LocalPlayer;
		if (playerEntity == null)
		{
			return null;
		}
		Entity.UniqueEntityEffect? result = null;
		Entity.UniqueEntityEffect[] entityEffects = playerEntity.EntityEffects;
		for (int i = 0; i < entityEffects.Length; i++)
		{
			Entity.UniqueEntityEffect value = entityEffects[i];
			if (value.NetworkEffectIndex == _lastStandSkullEffectId)
			{
				result = value;
				break;
			}
		}
		return result;
	}
}
