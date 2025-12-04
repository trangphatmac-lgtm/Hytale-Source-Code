using System.Collections.Generic;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame.Hud.StatusEffects;

internal abstract class BaseStatusEffect : Element
{
	protected enum StatusEffectArrows
	{
		Buff,
		Debuff,
		BuffDisabled
	}

	protected enum BarColor
	{
		Green,
		Orange,
		Red,
		Grey
	}

	public readonly InGameView InGameView;

	private static readonly float[] _percentageByIndex = new float[6] { 1f, 0.875f, 0.625f, 0.375f, 0.125f, 0f };

	protected float _targetPercentage = 0f;

	protected float _renderPercentage = 0f;

	private ProgressBar[] _progressBars;

	private Group _statusEffectContainer;

	private Group _barContainer;

	protected Group _buffIcon;

	private UIPath _statusEffectBarFillGreen;

	private UIPath _statusEffectBarFillOrange;

	private UIPath _statusEffectBarFillRed;

	private UIPath _statusEffectBarFillGrey;

	private float animationSpeed = 10f;

	private ProgressBar _cooldownBar;

	private Group _cooldownContainer;

	protected float _cooldownTargetPercentage = 0f;

	protected float _cooldownRenderPercentage = 0f;

	private Group _statusEffectArrowContainer;

	private Dictionary<StatusEffectArrows, Group> _statusEffectsArrows = new Dictionary<StatusEffectArrows, Group>();

	public BaseStatusEffect(InGameView inGameView, Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		InGameView = inGameView;
	}

	public virtual void Build()
	{
		Desktop.Provider.TryGetDocument("InGame/Hud/StatusEffects/StatusEffect.ui", out var document);
		_statusEffectBarFillGreen = document.ResolveNamedValue<UIPath>(Desktop.Provider, "StatusEffectBarFillGreen");
		_statusEffectBarFillOrange = document.ResolveNamedValue<UIPath>(Desktop.Provider, "StatusEffectBarFillOrange");
		_statusEffectBarFillRed = document.ResolveNamedValue<UIPath>(Desktop.Provider, "StatusEffectBarFillRed");
		_statusEffectBarFillGrey = document.ResolveNamedValue<UIPath>(Desktop.Provider, "StatusEffectBarFillGrey");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_statusEffectContainer = uIFragment.Get<Group>("StatusEffect");
		_buffIcon = uIFragment.Get<Group>("StatusEffectIcon");
		_cooldownBar = uIFragment.Get<ProgressBar>("Cooldown");
		_cooldownContainer = uIFragment.Get<Group>("CooldownContainer");
		_statusEffectArrowContainer = uIFragment.Get<Group>("StatusEffectArrows");
		_statusEffectsArrows = new Dictionary<StatusEffectArrows, Group>();
		_statusEffectsArrows.Add(StatusEffectArrows.Buff, uIFragment.Get<Group>("ArrowBuff"));
		_statusEffectsArrows.Add(StatusEffectArrows.Debuff, uIFragment.Get<Group>("ArrowDebuff"));
		_statusEffectsArrows.Add(StatusEffectArrows.BuffDisabled, uIFragment.Get<Group>("ArrowBuffDisabled"));
		_statusEffectArrowContainer = uIFragment.Get<Group>("StatusEffectArrows");
		_statusEffectsArrows = new Dictionary<StatusEffectArrows, Group>();
		_statusEffectsArrows.Add(StatusEffectArrows.Buff, uIFragment.Get<Group>("ArrowBuff"));
		_statusEffectsArrows.Add(StatusEffectArrows.Debuff, uIFragment.Get<Group>("ArrowDebuff"));
		_statusEffectsArrows.Add(StatusEffectArrows.BuffDisabled, uIFragment.Get<Group>("ArrowBuffDisabled"));
		Desktop.Provider.TryGetDocument("InGame/Hud/StatusEffects/StatusEffectProgressDecrease.ui", out var document2);
		UIFragment uIFragment2 = document2.Instantiate(Desktop, uIFragment.Get<Group>("StatusEffectProgressBarsContainer"));
		_progressBars = new ProgressBar[5];
		for (int i = 0; i < 5; i++)
		{
			_progressBars[i] = uIFragment2.Get<ProgressBar>("Bar" + (i + 1));
		}
		_barContainer = uIFragment2.Get<Group>("StatusEffectProgressBars");
		AnimateBars();
		AnimateCooldown();
	}

	protected void SetBuffColor(BarColor barColor)
	{
		UIPath barTexturePath = _statusEffectBarFillGreen;
		switch (barColor)
		{
		case BarColor.Orange:
			barTexturePath = _statusEffectBarFillOrange;
			break;
		case BarColor.Red:
			barTexturePath = _statusEffectBarFillRed;
			break;
		case BarColor.Grey:
			barTexturePath = _statusEffectBarFillGrey;
			break;
		}
		for (int i = 0; i < _progressBars.Length; i++)
		{
			_progressBars[i].BarTexturePath = barTexturePath;
		}
		_barContainer.Layout();
	}

	protected void SetArrowVisible(StatusEffectArrows statusEffectArrowName)
	{
		foreach (KeyValuePair<StatusEffectArrows, Group> statusEffectsArrow in _statusEffectsArrows)
		{
			statusEffectsArrow.Value.Visible = statusEffectsArrow.Key == statusEffectArrowName;
		}
		_statusEffectArrowContainer.Layout();
	}

	protected void SetStatusEffectBackground(PatchStyle statusEffectBackground)
	{
		_statusEffectContainer.Background = statusEffectBackground;
		_statusEffectContainer.Layout();
	}

	protected void AnimateBars()
	{
		for (int i = 0; i < _percentageByIndex.Length - 1; i++)
		{
			if (_renderPercentage >= _percentageByIndex[i + 1])
			{
				ClearBars(i);
				_progressBars[i].Value = GetCurrentBarPercentage(i + 1);
				break;
			}
		}
		_barContainer.Layout();
	}

	private void ClearBars(int index)
	{
		for (int i = 0; i < _progressBars.Length; i++)
		{
			ProgressBar progressBar = _progressBars[i];
			if (i <= index)
			{
				progressBar.Value = 0f;
			}
			else if (i > index)
			{
				progressBar.Value = 1f;
			}
		}
	}

	private float GetCurrentBarPercentage(int index)
	{
		float num = _percentageByIndex[index - 1] - _percentageByIndex[index];
		float num2 = _renderPercentage - _percentageByIndex[index];
		return num2 / num;
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	protected virtual void Animate(float deltaTime)
	{
		if (_targetPercentage != _renderPercentage)
		{
			_renderPercentage = MathHelper.Lerp(_renderPercentage, _targetPercentage, deltaTime * animationSpeed);
			AnimateBars();
		}
		if (_cooldownTargetPercentage != _cooldownRenderPercentage)
		{
			_cooldownRenderPercentage = MathHelper.Lerp(_cooldownRenderPercentage, _cooldownTargetPercentage, deltaTime * animationSpeed);
			AnimateCooldown();
		}
	}

	protected void AnimateCooldown()
	{
		_cooldownBar.Value = _cooldownRenderPercentage;
		_cooldownContainer.Layout();
	}
}
