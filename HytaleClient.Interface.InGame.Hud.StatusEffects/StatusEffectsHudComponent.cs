using System.Collections.Generic;
using System.Linq;
using HytaleClient.Data.Items;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.InGame.Hud.StatusEffects;

internal class StatusEffectsHudComponent : InterfaceComponent
{
	public readonly InGameView InGameView;

	private static readonly int[] _trinketsSlots = new int[3] { 5, 6, 7 };

	private Dictionary<string, TrinketBuffStatusEffect> _trinketBuffs = new Dictionary<string, TrinketBuffStatusEffect>();

	private Dictionary<int, EntityEffectBuff> _entityEffectBuffs = new Dictionary<int, EntityEffectBuff>();

	private Dictionary<int, DebuffStatusEffect> _debuffs = new Dictionary<int, DebuffStatusEffect>();

	private Group _buffsContainer;

	private Group _debuffsContainer;

	private Group _buffHudContainer;

	private static readonly string[] _permanentTrinketBuffs = new string[8] { "Trinket_Magic_Feather", "Trinket_Ring_Of_Fire", "Trinket_The_Camels_Straw", "Trinket_Pocket_Cactus", "Trinket_Shoe_Glue", "Trinket_Power_Glove", "Trinket_Bear_Tooth_Necklace", "Trinket_Avatars_Capacitor" };

	private LastStandSkullStatusEffect _lastStandSkull = null;

	private VampireFangsStatusEffect _vampireFangs = null;

	public StatusEffectsHudComponent(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
		InGameView = inGameView;
	}

	public void Build()
	{
		Clear();
		_trinketBuffs = new Dictionary<string, TrinketBuffStatusEffect>();
		_debuffs = new Dictionary<int, DebuffStatusEffect>();
		Interface.TryGetDocument("InGame/Hud/StatusEffects/StatusEffectHud.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_buffHudContainer = uIFragment.Get<Group>("StatusEffectsHudContainer");
		_buffsContainer = uIFragment.Get<Group>("BuffsContainer");
		_debuffsContainer = uIFragment.Get<Group>("DebuffsContainer");
	}

	public void UpdateTrinketsBuffs()
	{
		List<TrinketBuffStatusEffect> list = new List<TrinketBuffStatusEffect>();
		foreach (KeyValuePair<string, TrinketBuffStatusEffect> trinketBuff in _trinketBuffs)
		{
			list.Add(trinketBuff.Value);
		}
		int[] trinketsSlots = _trinketsSlots;
		foreach (int num in trinketsSlots)
		{
			ClientItemStack clientItemStack = InGameView.InGame.Instance.InventoryModule._armorInventory[num];
			if (clientItemStack == null)
			{
				continue;
			}
			_trinketBuffs.TryGetValue(clientItemStack.Id, out var value);
			if (_permanentTrinketBuffs.Contains(clientItemStack.Id) || !(clientItemStack.Id != LastStandSkullStatusEffect._lastStandSkullName) || !(clientItemStack.Id != VampireFangsStatusEffect.VampireFangsName))
			{
				if (value == null)
				{
					AddTrinketBuff(clientItemStack.Id);
					continue;
				}
				_buffsContainer.Reorder(value, _buffsContainer.Children.Count - 1);
				list.Remove(value);
			}
		}
		foreach (TrinketBuffStatusEffect item in list)
		{
			_buffsContainer.Remove(item);
			_trinketBuffs.Remove(item.Id);
		}
		_buffHudContainer.Layout();
	}

	public void OnEffectAdded(int effectIndex)
	{
		_lastStandSkull?.OnEffectAdded(effectIndex);
		_vampireFangs?.OnEffectAdded(effectIndex);
		PlayerEntity playerEntity = InGameView.InGame.Instance?.LocalPlayer;
		if (playerEntity == null)
		{
			return;
		}
		Entity.UniqueEntityEffect[] entityEffects = playerEntity.EntityEffects;
		Entity.UniqueEntityEffect? uniqueEntityEffect = null;
		Entity.UniqueEntityEffect[] array = entityEffects;
		for (int i = 0; i < array.Length; i++)
		{
			Entity.UniqueEntityEffect value = array[i];
			if (value.NetworkEffectIndex == effectIndex)
			{
				uniqueEntityEffect = value;
				break;
			}
		}
		if (uniqueEntityEffect.HasValue)
		{
			if (uniqueEntityEffect.Value.IsDebuff)
			{
				AddDebuff(uniqueEntityEffect.Value);
			}
			else if (uniqueEntityEffect.Value.StatusEffectIcon != null)
			{
				AddEntityEffectBuff(uniqueEntityEffect.Value);
			}
		}
	}

	private void AddDebuff(Entity.UniqueEntityEffect entityEffect)
	{
		if (_debuffs.ContainsKey(entityEffect.NetworkEffectIndex))
		{
			DebuffStatusEffect debuffStatusEffect = _debuffs[entityEffect.NetworkEffectIndex];
			debuffStatusEffect.SetInitialCountdown(entityEffect.RemainingDuration);
			return;
		}
		DebuffStatusEffect debuffStatusEffect2 = new DebuffStatusEffect(InGameView, Desktop, _debuffsContainer, entityEffect, entityEffect.NetworkEffectIndex);
		_debuffs.Add(debuffStatusEffect2.Id, debuffStatusEffect2);
		debuffStatusEffect2.Build();
		_buffHudContainer.Layout();
	}

	private void AddEntityEffectBuff(Entity.UniqueEntityEffect entityEffect)
	{
		if (_entityEffectBuffs.ContainsKey(entityEffect.NetworkEffectIndex))
		{
			EntityEffectBuff entityEffectBuff = _entityEffectBuffs[entityEffect.NetworkEffectIndex];
			entityEffectBuff.SetInitialCountdown(entityEffect.RemainingDuration);
			_buffsContainer.Reorder(entityEffectBuff, 0);
			_buffHudContainer.Layout();
		}
		else
		{
			EntityEffectBuff entityEffectBuff2 = new EntityEffectBuff(InGameView, Desktop, _buffsContainer, entityEffect, entityEffect.NetworkEffectIndex);
			_entityEffectBuffs.Add(entityEffectBuff2.Id, entityEffectBuff2);
			entityEffectBuff2.Build();
			_buffsContainer.Reorder(entityEffectBuff2, 0);
			_buffHudContainer.Layout();
		}
	}

	public void OnEffectRemoved(int effectIndex)
	{
		_lastStandSkull?.OnEffectRemoved(effectIndex);
		_vampireFangs?.OnEffectRemoved(effectIndex);
		if (_debuffs.ContainsKey(effectIndex))
		{
			DebuffStatusEffect debuffStatusEffect = _debuffs[effectIndex];
			_debuffsContainer.Remove(debuffStatusEffect);
			_debuffs.Remove(debuffStatusEffect.Id);
			_buffHudContainer.Layout();
		}
		else if (_entityEffectBuffs.ContainsKey(effectIndex))
		{
			EntityEffectBuff entityEffectBuff = _entityEffectBuffs[effectIndex];
			_buffsContainer.Remove(entityEffectBuff);
			_entityEffectBuffs.Remove(entityEffectBuff.Id);
			_buffHudContainer.Layout();
		}
	}

	private void AddTrinketBuff(string id)
	{
		if (id == LastStandSkullStatusEffect._lastStandSkullName)
		{
			AddLastStandSkullBuff(id);
			return;
		}
		if (id == VampireFangsStatusEffect.VampireFangsName)
		{
			AddVampireFangsBuff(id);
			return;
		}
		TrinketPermanentBuff trinketPermanentBuff = new TrinketPermanentBuff(InGameView, Desktop, _buffsContainer, id);
		_trinketBuffs.Add(trinketPermanentBuff.Id, trinketPermanentBuff);
		trinketPermanentBuff.Build();
		_buffsContainer.Reorder(trinketPermanentBuff, _buffsContainer.Children.Count - 1);
	}

	private void AddLastStandSkullBuff(string id)
	{
		LastStandSkullStatusEffect lastStandSkullStatusEffect = (_lastStandSkull = new LastStandSkullStatusEffect(InGameView, Desktop, _buffsContainer, id));
		_trinketBuffs.Add(lastStandSkullStatusEffect.Id, lastStandSkullStatusEffect);
		lastStandSkullStatusEffect.Build();
		_buffsContainer.Reorder(lastStandSkullStatusEffect, _buffsContainer.Children.Count - 1);
	}

	private void AddVampireFangsBuff(string id)
	{
		VampireFangsStatusEffect vampireFangsStatusEffect = (_vampireFangs = new VampireFangsStatusEffect(InGameView, Desktop, _buffsContainer, id));
		_trinketBuffs.Add(vampireFangsStatusEffect.Id, vampireFangsStatusEffect);
		vampireFangsStatusEffect.Build();
		_buffsContainer.Reorder(vampireFangsStatusEffect, _buffsContainer.Children.Count - 1);
	}
}
