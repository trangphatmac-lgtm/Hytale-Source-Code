using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Core;
using HytaleClient.Data.ClientInteraction;
using HytaleClient.Data.Items;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame.Hud.Abilities;

internal class BaseAbility : Element
{
	public enum IconName
	{
		ShieldAbility,
		SwordChargeUpAttack
	}

	public const string StaminaDepletedEffectId = "Stamina_Error_State";

	public const int ChargesMax = 6;

	public const int ChargeTimeMax = 999;

	public const int RemainingTimeMax = 10000000;

	public const int TimerFontSizeMax = 24;

	public const int TimerFontSizeMin = 18;

	private const float MaxErrorAnimationPercentage = 1f;

	private const int ErrorAnimationTotal = 4;

	private const float MinErrorAnimationPercentage = 0.4f;

	private const float ErrorAnimationSpeed = 10f;

	public readonly InGameView InGameView;

	protected InputBinding _inputBidingKey;

	private Dictionary<string, Group> _inputBidingsMouse = new Dictionary<string, Group>();

	private Label _inputBidingLabel;

	private Group _inputBidingContainer;

	private Group[] _icons = new Group[Enum.GetNames(typeof(IconName)).Length];

	private Group _selectedIcon;

	private Label _cooldownTimer;

	private Label _shadowCooldownTimer;

	private Group _cooldownTimerContainer;

	private Group _errorOverlay;

	private int _errorAnimationCount;

	protected float _errorTargetPercentage = 0f;

	protected float _errorRenderPercentage = 0f;

	private bool _isStaminaErrorEffect = false;

	private string _lastChainId;

	private ProgressBar _cooldownBar;

	private CircularProgressBar _chargeProgressBar;

	private Group _abilityChargesContainer;

	private List<AbilityCharge> _abilityCharges;

	protected float _cooldownTargetPercentage = 0f;

	protected float _cooldownRenderPercentage = 0f;

	protected float _chargeTargetPercentage = 0f;

	protected float _chargeRenderPercentage = 0f;

	private float animationSpeed = 50f;

	protected InteractionType _iterationType;

	public BaseAbility(InGameView inGameView, Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		InGameView = inGameView;
	}

	public virtual void Build()
	{
		Desktop.Provider.TryGetDocument("InGame/Hud/Abilities/Ability.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_inputBidingsMouse.Add(InputBinding.GetMouseBoundInputLabel(Input.MouseButton.SDL_BUTTON_LEFT), uIFragment.Get<Group>("MouseLeft"));
		_inputBidingsMouse.Add(InputBinding.GetMouseBoundInputLabel(Input.MouseButton.SDL_BUTTON_MIDDLE), uIFragment.Get<Group>("MouseMiddle"));
		_inputBidingsMouse.Add(InputBinding.GetMouseBoundInputLabel(Input.MouseButton.SDL_BUTTON_RIGHT), uIFragment.Get<Group>("MouseRight"));
		_inputBidingLabel = uIFragment.Get<Label>("InputBinding");
		_inputBidingContainer = uIFragment.Get<Group>("InputBindingContainer");
		_icons[0] = uIFragment.Get<Group>("IconShieldAbility");
		_icons[1] = uIFragment.Get<Group>("IconSwordChargeUpAttack");
		_cooldownTimer = uIFragment.Get<Label>("CooldownTimer");
		_shadowCooldownTimer = uIFragment.Get<Label>("ShadowCooldownTimer");
		_cooldownTimerContainer = uIFragment.Get<Group>("CooldownTimerContainer");
		_abilityChargesContainer = uIFragment.Get<Group>("AbilityChargesContainer");
		_errorOverlay = uIFragment.Get<Group>("ErrorOverlay");
		_errorOverlay.Visible = false;
		_chargeProgressBar = uIFragment.Get<CircularProgressBar>("ChargeProgressBar");
		Label cooldownTimer = _cooldownTimer;
		bool visible = (_shadowCooldownTimer.Visible = false);
		cooldownTimer.Visible = visible;
		_cooldownBar = uIFragment.Get<ProgressBar>("Cooldown");
	}

	protected void SetIcon(IconName iconName)
	{
		_selectedIcon = _icons[(int)iconName];
		if (_selectedIcon != null)
		{
			_selectedIcon.Visible = true;
			_selectedIcon.Layout();
		}
	}

	public void ShowErrorOverlay()
	{
		_errorOverlay.Visible = true;
		_errorOverlay.Parent.Layout();
		_errorTargetPercentage = 0.4f;
		_errorRenderPercentage = 1f;
		_errorAnimationCount = 0;
	}

	public void HideErrorOverlay()
	{
		_errorOverlay.Visible = false;
		_errorOverlay.Parent.Layout();
		_isStaminaErrorEffect = false;
		_errorTargetPercentage = 0.4f;
		_errorRenderPercentage = 1f;
	}

	public void CooldownError(string rootInteractionId)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		InteractionContext interactionContext = InteractionContext.ForInteraction(InGameView.InGame.Instance?.LocalPlayer, _iterationType);
		interactionContext.GetRootInteractionId(InGameView.InGame.Instance, _iterationType, out var id);
		ClientRootInteraction clientRootInteraction = InGameView.InGame.Instance.InteractionModule.RootInteractions[id];
		if (!(clientRootInteraction.Id != rootInteractionId))
		{
			ShowErrorOverlay();
		}
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
		UpdateInputBiding();
		SetCharges();
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	protected virtual void Animate(float deltaTime)
	{
		if (InGameView.InGame.Instance?.LocalPlayer != null)
		{
			SetCooldownPercentages();
			if (_cooldownTargetPercentage != _cooldownRenderPercentage)
			{
				_cooldownRenderPercentage = MathHelper.Lerp(_cooldownRenderPercentage, _cooldownTargetPercentage, deltaTime * animationSpeed);
				AnimateCooldown();
			}
			if (_errorTargetPercentage != _errorRenderPercentage)
			{
				_errorRenderPercentage = MathHelper.Lerp(_errorRenderPercentage, _errorTargetPercentage, deltaTime * 10f);
				AnimateError();
			}
			SetChargesPercentage();
			if (_chargeTargetPercentage != _chargeRenderPercentage)
			{
				_chargeRenderPercentage = MathHelper.Lerp(_chargeRenderPercentage, _chargeTargetPercentage, deltaTime * animationSpeed);
				AnimateChargeProgressBar();
			}
		}
	}

	private void SetCooldownPercentages()
	{
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		if (_abilityCharges.Count > 1)
		{
			if (_cooldownRenderPercentage != 0f || _cooldownTimer.Visible)
			{
				_cooldownRenderPercentage = (_cooldownTargetPercentage = 0f);
				Label cooldownTimer = _cooldownTimer;
				bool visible = (_shadowCooldownTimer.Visible = false);
				cooldownTimer.Visible = visible;
				SetSelectedIconOpacity(1f);
				_selectedIcon.Layout();
				_cooldownTimerContainer.Layout();
				AnimateCooldown();
			}
			return;
		}
		InteractionContext interactionContext = InteractionContext.ForInteraction(InGameView.InGame.Instance?.LocalPlayer, _iterationType);
		interactionContext.GetRootInteractionId(InGameView.InGame.Instance, _iterationType, out var id);
		ClientRootInteraction clientRootInteraction = InGameView.InGame.Instance.InteractionModule.RootInteractions[id];
		float num = 0.35f;
		if (clientRootInteraction.RootInteraction.Cooldown != null)
		{
			num = clientRootInteraction.RootInteraction.Cooldown.Cooldown;
		}
		string text = clientRootInteraction.RootInteraction.Cooldown?.CooldownId;
		if (text == null)
		{
			text = clientRootInteraction.Id;
		}
		InteractionModule interactionModule = InGameView.InGame.Instance.InteractionModule;
		if (interactionModule.GetCooldown(text) == null)
		{
			if (_cooldownRenderPercentage != 0f)
			{
				_cooldownRenderPercentage = (_cooldownTargetPercentage = 0f);
				Label cooldownTimer2 = _cooldownTimer;
				bool visible = (_shadowCooldownTimer.Visible = false);
				cooldownTimer2.Visible = visible;
				SetSelectedIconOpacity(1f);
				_selectedIcon.Layout();
				_cooldownTimerContainer.Layout();
				AnimateCooldown();
				SetChargesStates(_abilityCharges.Count);
				if (!_isStaminaErrorEffect)
				{
					HideErrorOverlay();
				}
			}
		}
		else
		{
			float num2 = System.Math.Min(interactionModule.GetCooldown(text).GetCooldownRemainingTime(), 10000000f);
			string cooldownRemainingTime = GetCooldownRemainingTime(num2);
			float val = 1f - num2 / num;
			val = System.Math.Max(val, 0f);
			if (_cooldownTargetPercentage != val)
			{
				_cooldownTargetPercentage = val;
				_shadowCooldownTimer.Text = cooldownRemainingTime;
				_cooldownTimer.Text = cooldownRemainingTime;
				SetSelectedIconOpacity(0.4f);
				Label cooldownTimer3 = _cooldownTimer;
				bool visible = (_shadowCooldownTimer.Visible = true);
				cooldownTimer3.Visible = visible;
				_cooldownTimerContainer.Layout();
			}
		}
	}

	private void SetChargesPercentage()
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		if (_abilityCharges.Count <= 1)
		{
			if (_chargeRenderPercentage != 0f)
			{
				_chargeRenderPercentage = (_chargeTargetPercentage = 0f);
				AnimateChargeProgressBar();
			}
			return;
		}
		InteractionContext interactionContext = InteractionContext.ForInteraction(InGameView.InGame.Instance?.LocalPlayer, _iterationType);
		interactionContext.GetRootInteractionId(InGameView.InGame.Instance, _iterationType, out var id);
		ClientRootInteraction clientRootInteraction = InGameView.InGame.Instance.InteractionModule.RootInteractions[id];
		string text = clientRootInteraction.RootInteraction.Cooldown?.CooldownId;
		if (text == null)
		{
			text = clientRootInteraction.Id;
		}
		InteractionModule interactionModule = InGameView.InGame.Instance.InteractionModule;
		if (interactionModule.GetCooldown(text) == null)
		{
			if (_chargeRenderPercentage != 0f)
			{
				_chargeRenderPercentage = (_chargeTargetPercentage = 0f);
				Label cooldownTimer = _cooldownTimer;
				bool visible = (_shadowCooldownTimer.Visible = false);
				cooldownTimer.Visible = visible;
				_cooldownTimerContainer.Layout();
				AnimateChargeProgressBar();
				SetChargesStates(_abilityCharges.Count);
			}
			return;
		}
		Cooldown cooldown = interactionModule.GetCooldown(text);
		float chargeTimer = cooldown.GetChargeTimer();
		int chargeCount = cooldown.GetChargeCount();
		if (_errorOverlay.Visible && chargeCount != 0 && !_isStaminaErrorEffect)
		{
			HideErrorOverlay();
		}
		if (chargeCount == 0)
		{
			SetSelectedIconOpacity(0.4f);
		}
		else
		{
			SetSelectedIconOpacity(1f);
		}
		SetChargesStates(chargeCount);
		float[] charges = cooldown.GetCharges();
		float num;
		float num2;
		if (chargeCount >= charges.Length)
		{
			num = cooldown.GetCooldownMax();
			num2 = System.Math.Min(cooldown.GetCooldownRemainingTime(), 10000000f);
		}
		else
		{
			num = charges[chargeCount];
			num2 = System.Math.Min(num - chargeTimer, 10000000f);
		}
		float val = 1f - num2 / num;
		val = System.Math.Max(val, 0f);
		if (_chargeTargetPercentage != val)
		{
			_chargeTargetPercentage = val;
		}
	}

	private string GetCooldownRemainingTime(float remainingTime)
	{
		if (remainingTime > 999f)
		{
			SetCooldownTimerFontSize(18f);
			return 999 + "+";
		}
		if (_cooldownTimer.Style.FontSize != 24f || _shadowCooldownTimer.Style.FontSize != 24f)
		{
			SetCooldownTimerFontSize(24f);
		}
		if (remainingTime < 0f)
		{
			return "";
		}
		int num = ((!(remainingTime < 1f)) ? 1 : 10);
		return (System.Math.Truncate(remainingTime * (float)num) / (double)num).ToString();
	}

	private void SetCooldownTimerFontSize(float fontSize)
	{
		_cooldownTimer.Style.FontSize = fontSize;
		_shadowCooldownTimer.Style.FontSize = fontSize;
		_cooldownTimerContainer.Layout();
	}

	private void SetChargesStates(int currentCharge)
	{
		for (int i = 0; i < _abilityCharges.Count; i++)
		{
			if (i > currentCharge - 1)
			{
				_abilityCharges[i].SetEmpty();
			}
			else
			{
				_abilityCharges[i].SetFull();
			}
		}
	}

	public void SetCharges()
	{
		_abilityCharges = new List<AbilityCharge>();
		if (_abilityChargesContainer == null)
		{
			return;
		}
		_abilityChargesContainer.Clear();
		if (InGameView.InGame.Instance == null)
		{
			return;
		}
		ClientItemBase currentInteractionItem = GetCurrentInteractionItem();
		int chargeAmount = GetChargeAmount(currentInteractionItem);
		if (chargeAmount <= 1)
		{
			return;
		}
		for (int i = 0; i < chargeAmount; i++)
		{
			AbilityCharge abilityCharge = new AbilityCharge(InGameView, Desktop, _abilityChargesContainer);
			abilityCharge.Build();
			_abilityCharges.Add(abilityCharge);
			abilityCharge.SetFull();
			if (i >= 6)
			{
				abilityCharge.Visible = false;
			}
		}
	}

	private ClientItemBase GetCurrentInteractionItem()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		InteractionContext interactionContext = InteractionContext.ForInteraction(InGameView.InGame.Instance, InGameView.InGame.Instance.InventoryModule, _iterationType);
		interactionContext.GetRootInteractionId(InGameView.InGame.Instance, _iterationType, out var id);
		ClientItemStack activeHotbarItem = InGameView.InGame.Instance.InventoryModule.GetActiveHotbarItem();
		ClientItemBase item = InGameView.InGame.Instance.ItemLibraryModule.GetItem(activeHotbarItem?.Id);
		if (IsCurrentInteractionItem(item, id))
		{
			return item;
		}
		ClientItemStack utilityItem = InGameView.InGame.Instance.InventoryModule.GetUtilityItem(InGameView.InGame.Instance.InventoryModule.UtilityActiveSlot);
		ClientItemBase item2 = InGameView.InGame.Instance.ItemLibraryModule.GetItem(utilityItem?.Id);
		if (IsCurrentInteractionItem(item2, id))
		{
			return item2;
		}
		return null;
	}

	private bool IsCurrentInteractionItem(ClientItemBase item, int currentInteractionId)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (item == null || item.Interactions == null)
		{
			return false;
		}
		item.Interactions.TryGetValue(_iterationType, out var value);
		return value == currentInteractionId;
	}

	private int GetChargeAmount(ClientItemBase item)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		if (item == null)
		{
			return 0;
		}
		if (!item.Interactions.ContainsKey(_iterationType))
		{
			return 0;
		}
		int num = item.Interactions[_iterationType];
		ClientRootInteraction clientRootInteraction = InGameView.InGame.Instance.InteractionModule.RootInteractions[num];
		return (clientRootInteraction.RootInteraction.Cooldown?.ChargeTimes?.Length).GetValueOrDefault();
	}

	private void SetSelectedIconOpacity(float opacity)
	{
		UInt32Color uInt32Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(opacity * 255f));
		if (_selectedIcon.Background.Color.GetA() != uInt32Color.GetA())
		{
			_selectedIcon.Background.Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(opacity * 255f));
			_selectedIcon.Layout();
			SetInputBidingsOpacity(opacity);
		}
	}

	private void SetInputBidingsOpacity(float opacity)
	{
		_inputBidingLabel.Style.TextColor.SetA((byte)(opacity * 255f));
		foreach (string key in _inputBidingsMouse.Keys)
		{
			_inputBidingsMouse[key].Background.Color.SetA((byte)(opacity * 255f));
		}
		_inputBidingLabel.Parent.Layout();
		_inputBidingContainer.Layout();
	}

	protected void AnimateCooldown()
	{
		_cooldownBar.Value = MathHelper.Clamp(_cooldownRenderPercentage, 0f, 1f);
		_cooldownBar.Layout();
	}

	protected void AnimateError()
	{
		_errorOverlay.Background.Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(_errorRenderPercentage * 255f));
		_errorOverlay.Parent.Layout();
		float num = 0.1f;
		if (_errorAnimationCount > 4)
		{
			HideErrorOverlay();
			return;
		}
		if (_errorRenderPercentage > 1f - num && _errorTargetPercentage == 1f)
		{
			_errorTargetPercentage = 0.4f;
			_errorAnimationCount++;
		}
		if (_errorRenderPercentage < 0.4f + num && _errorTargetPercentage == 0.4f)
		{
			_errorTargetPercentage = 1f;
			_errorAnimationCount++;
		}
	}

	public void OnStartChain(string rootInteractionId)
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		_lastChainId = rootInteractionId;
		Entity.UniqueEntityEffect[] array = InGameView.InGame.Instance?.LocalPlayer.EntityEffects;
		for (int i = 0; i < array.Length; i++)
		{
			Entity.UniqueEntityEffect uniqueEntityEffect = array[i];
			Dictionary<string, int> entityEffectIndicesByIds = InGameView.InGame.Instance.EntityStoreModule.EntityEffectIndicesByIds;
			if (entityEffectIndicesByIds.TryGetValue("Stamina_Error_State", out var value) && uniqueEntityEffect.NetworkEffectIndex == value)
			{
				InteractionContext interactionContext = InteractionContext.ForInteraction(InGameView.InGame.Instance?.LocalPlayer, _iterationType);
				interactionContext.GetRootInteractionId(InGameView.InGame.Instance, _iterationType, out var id);
				ClientRootInteraction clientRootInteraction = InGameView.InGame.Instance.InteractionModule.RootInteractions[id];
				if (!(clientRootInteraction.Id != _lastChainId))
				{
					ShowErrorOverlay();
					_lastChainId = null;
					_isStaminaErrorEffect = true;
				}
				break;
			}
		}
	}

	public void OnEffectRemoved(int effectIndex)
	{
		Dictionary<string, int> entityEffectIndicesByIds = InGameView.InGame.Instance.EntityStoreModule.EntityEffectIndicesByIds;
		if (entityEffectIndicesByIds.TryGetValue("Stamina_Error_State", out var value) && effectIndex == value)
		{
			_isStaminaErrorEffect = false;
			_lastChainId = null;
			HideErrorOverlay();
		}
	}

	protected void AnimateChargeProgressBar()
	{
		_chargeProgressBar.Value = MathHelper.Clamp(_chargeRenderPercentage, 0f, 1f);
		_chargeProgressBar.Layout();
	}

	public virtual void UpdateInputBiding()
	{
		if (_inputBidingLabel == null)
		{
			return;
		}
		foreach (string key in _inputBidingsMouse.Keys)
		{
			_inputBidingsMouse[key].Visible = false;
		}
		_inputBidingLabel.Visible = false;
		if (_inputBidingKey == null)
		{
			return;
		}
		if (_inputBidingsMouse.Keys.ToArray().Contains(_inputBidingKey.BoundInputLabel))
		{
			Group group = _inputBidingsMouse[_inputBidingKey.BoundInputLabel];
			if (group != null)
			{
				group.Visible = true;
				Layout();
			}
		}
		else
		{
			_inputBidingLabel.Text = _inputBidingKey.BoundInputLabel;
			_inputBidingLabel.Style.FontSize = System.Math.Max(13 - _inputBidingKey.BoundInputLabel.ToArray().Length, 8);
			_inputBidingLabel.Visible = true;
			Layout();
		}
	}
}
