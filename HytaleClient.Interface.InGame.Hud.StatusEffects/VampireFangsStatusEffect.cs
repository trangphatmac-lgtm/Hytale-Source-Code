using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Hud.StatusEffects;

internal class VampireFangsStatusEffect : TrinketBuffStatusEffect
{
	private bool _isSatiated = false;

	private float _initialCountdown;

	private int _satiatedEffectId;

	private string _satiatedEffectName = "Trinket_Vampire_Fangs_Satiated";

	public static readonly string VampireFangsName = "Trinket_Vampire_Fangs";

	public VampireFangsStatusEffect(InGameView InGameView, Desktop desktop, Element parent, string id = "")
		: base(InGameView, desktop, parent, id)
	{
		_satiatedEffectId = InGameView.InGame.Instance.EntityStoreModule.EntityEffectIndicesByIds[_satiatedEffectName];
	}

	public override void Build()
	{
		base.Build();
		_renderPercentage = 1f;
		_targetPercentage = 1f;
		SetBuffColor(BarColor.Green);
		AnimateBars();
		SetArrowVisible(StatusEffectArrows.Buff);
		SetIcon();
		SetEnabledBackground();
		Entity.UniqueEntityEffect? vampireFangsEffect = GetVampireFangsEffect();
		if (vampireFangsEffect.HasValue)
		{
			OnEffectAdded(vampireFangsEffect.Value.NetworkEffectIndex);
		}
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
		string texturePath = "InGame/Hud/StatusEffects/Assets/Icons/" + VampireFangsName + ".png";
		_buffIcon.Background = new PatchStyle(texturePath);
		_buffIcon.Layout();
	}

	private void SetDisabledIcon()
	{
		string texturePath = "InGame/Hud/StatusEffects/Assets/Icons/" + VampireFangsName + "_Disabled.png";
		_buffIcon.Background = new PatchStyle(texturePath);
		_buffIcon.Layout();
	}

	public void OnEffectAdded(int effectIndex)
	{
		if (effectIndex == _satiatedEffectId)
		{
			_isSatiated = true;
			Entity.UniqueEntityEffect? vampireFangsEffect = GetVampireFangsEffect();
			if (vampireFangsEffect.HasValue)
			{
				_initialCountdown = vampireFangsEffect.Value.RemainingDuration;
				SetArrowVisible(StatusEffectArrows.BuffDisabled);
				SetBuffColor(BarColor.Grey);
				SetDisabledBackground();
				SetDisabledIcon();
			}
		}
	}

	public void OnEffectRemoved(int effectIndex)
	{
		if (effectIndex == _satiatedEffectId)
		{
			_isSatiated = false;
			_cooldownTargetPercentage = 0f;
			_cooldownRenderPercentage = 0f;
			AnimateCooldown();
			SetArrowVisible(StatusEffectArrows.Buff);
			SetBuffColor(BarColor.Green);
			SetEnabledBackground();
			SetIcon();
			Desktop.Provider.PlaySound(_buffCooldownCompletedSound);
		}
	}

	protected override void Animate(float deltaTime)
	{
		if (_isSatiated)
		{
			Entity.UniqueEntityEffect? vampireFangsEffect = GetVampireFangsEffect();
			if (vampireFangsEffect.HasValue)
			{
				SetCountdownPercentage(vampireFangsEffect.Value.RemainingDuration);
				base.Animate(deltaTime);
			}
		}
	}

	public void SetCountdownPercentage(float remainingTime)
	{
		_cooldownTargetPercentage = 1f - remainingTime / _initialCountdown;
	}

	private Entity.UniqueEntityEffect? GetVampireFangsEffect()
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
			if (value.NetworkEffectIndex == _satiatedEffectId)
			{
				result = value;
				break;
			}
		}
		return result;
	}
}
