using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Data.EntityStats;
using HytaleClient.Data.UserSettings;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class ChargingInteraction : ClientInteraction
{
	private const int MinimumPrimaryChargingDuration = 200;

	private const float ChargingCanceled = -2f;

	private static readonly double DisplayDelay = 0.2;

	private readonly float _highestChargeValue;

	private readonly bool _shouldDisplayProgress;

	private readonly float[] _sortedKeys;

	public ChargingInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
		if (interaction.ChargedNext != null)
		{
			_highestChargeValue = interaction.ChargedNext.Keys.Max();
			_shouldDisplayProgress = interaction.DisplayProgress && (interaction.ChargedNext.Count != 1 || interaction.ChargedNext.First().Key != 0f);
			_sortedKeys = interaction.ChargedNext.Keys.ToArray();
			Array.Sort(_sortedKeys);
		}
		else
		{
			_shouldDisplayProgress = false;
		}
		if (Interaction.RunTime > 0f)
		{
			_highestChargeValue = Interaction.RunTime;
		}
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Invalid comparison between Unknown and I4
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_056d: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b1: Unknown result type (might be due to invalid IL or missing references)
		if (ClientInteraction.Failed(context.State.State))
		{
			return;
		}
		if (Interaction.Forks != null)
		{
			bool flag = context.InstanceStore.ForkedChain == null || (int)context.InstanceStore.ForkedChain.ClientState != 4;
			foreach (KeyValuePair<InteractionType, int> fork in Interaction.Forks)
			{
				InputBinding inputBindingForType = gameInstance.InteractionModule.GetInputBindingForType(fork.Key);
				gameInstance.InteractionModule.DisableInput(fork.Key);
				if (!gameInstance.Input.CanConsumeBinding(inputBindingForType))
				{
					continue;
				}
				gameInstance.Input.ConsumeBinding(inputBindingForType);
				if (flag)
				{
					InteractionContext context2 = context.Duplicate();
					context.InstanceStore.ForkedChain = context.Fork(fork.Key, context2, fork.Value);
					if (context.State.ForkCounts == null)
					{
						context.State.ForkCounts = new Dictionary<InteractionType, int>();
					}
					context.State.ForkCounts.TryGetValue(fork.Key, out var value);
					context.State.ForkCounts[fork.Key] = value + 1;
				}
			}
		}
		bool failOnDamage = Interaction.FailOnDamage;
		List<DamageInfo> damageInfos = gameInstance.InteractionModule.DamageInfos;
		if (firstRun)
		{
			gameInstance.CameraModule.SetTargetMouseModifier(Interaction.MouseSensitivityAdjustmentTarget, Interaction.MouseSensitivityAdjustmentDuration);
			context.State.State = (InteractionState)4;
			return;
		}
		if (damageInfos.Count != 0 && failOnDamage)
		{
			gameInstance.CameraModule.SetTargetMouseModifier(1f, gameInstance.App.Settings.ResetMouseSensitivityDuration);
			context.State.State = (InteractionState)3;
			ClientRootInteraction.Label[] labels = context.Labels;
			float[] sortedKeys = _sortedKeys;
			context.Jump(labels[(sortedKeys != null) ? sortedKeys.Length : 0]);
			return;
		}
		if (damageInfos.Count > 0 && Interaction.ChargingDelay_ != null)
		{
			ChargingDelay chargingDelay_ = Interaction.ChargingDelay_;
			for (int i = 0; i < damageInfos.Count; i++)
			{
				DamageInfo val = damageInfos[i];
				float num = val.DamageAmount / (context.Entity.GetEntityStat(DefaultEntityStats.Health)?.Max ?? 1f);
				if (!(num < chargingDelay_.MinHealth))
				{
					num = MathHelper.Min(num, chargingDelay_.MaxHealth);
					float amount = (num - chargingDelay_.MinHealth) / (chargingDelay_.MaxHealth - chargingDelay_.MinHealth);
					float num2 = MathHelper.Lerp(chargingDelay_.MinDelay, chargingDelay_.MaxDelay, amount);
					context.InstanceStore.TotalDelay += num2;
				}
			}
			if (chargingDelay_.MaxTotalDelay > 0f)
			{
				context.InstanceStore.TotalDelay = MathHelper.Min(context.InstanceStore.TotalDelay, chargingDelay_.MaxTotalDelay);
			}
			context.InstanceStore.TotalDelay = MathHelper.Min(context.InstanceStore.TotalDelay, time);
		}
		time -= context.InstanceStore.TotalDelay;
		bool flag2 = (double)time >= DisplayDelay && _shouldDisplayProgress;
		if (flag2 && !context.InstanceStore.ChargingVisible)
		{
			context.InstanceStore.ChargingVisible = true;
			gameInstance.App.Interface.TriggerEvent("combat.setShowChargeProgress", true, Interaction.ChargedNext.Keys.Select((float threshold) => threshold / _highestChargeValue).ToArray());
		}
		if (clickType == InteractionModule.ClickType.Held && ((Interaction.AllowIndefiniteHold && Interaction.RunTime <= 0f) || time < _highestChargeValue))
		{
			if (Interaction.CancelOnOtherClick && hasAnyButtonClick)
			{
				gameInstance.CameraModule.SetTargetMouseModifier(1f, gameInstance.App.Settings.ResetMouseSensitivityDuration);
				context.State.ChargeValue = -2f;
				context.State.State = (InteractionState)0;
				return;
			}
			context.State.State = (InteractionState)4;
			int num3 = (int)System.Math.Min((double)time / (double)_highestChargeValue * 100.0, 100.0);
			if (num3 != context.InstanceStore.PrimaryChargingLastProgress)
			{
				context.InstanceStore.PrimaryChargingLastProgress = num3;
				if (flag2)
				{
					gameInstance.App.Interface.TriggerEvent("combat.setChargeProgress", num3);
				}
			}
		}
		else
		{
			gameInstance.CameraModule.SetTargetMouseModifier(1f, gameInstance.App.Settings.ResetMouseSensitivityDuration);
			context.State.State = (InteractionState)0;
			context.State.ChargeValue = time;
			JumpToChargeValue(context);
		}
	}

	private void JumpToChargeValue(InteractionContext context)
	{
		if (Interaction.ChargedNext == null)
		{
			return;
		}
		float num = 2.1474836E+09f;
		int num2 = -1;
		int num3 = 0;
		float[] sortedKeys = _sortedKeys;
		foreach (float num4 in sortedKeys)
		{
			if (context.State.ChargeValue < num4)
			{
				num3++;
				continue;
			}
			float num5 = context.State.ChargeValue - num4;
			if (num2 == -1 || num5 < num)
			{
				num = num5;
				num2 = num3;
			}
			num3++;
		}
		if (num2 != -1)
		{
			context.Jump(context.Labels[num2]);
		}
	}

	public override void Compile(InteractionModule module, ClientRootInteraction.OperationsBuilder builder)
	{
		ClientRootInteraction.Label label = builder.CreateUnresolvedLabel();
		ClientRootInteraction.Label[] array = new ClientRootInteraction.Label[(Interaction.ChargedNext?.Count ?? 0) + 1];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = builder.CreateUnresolvedLabel();
		}
		builder.AddOperation(Id, array);
		builder.Jump(label);
		int num = 0;
		if (_sortedKeys != null)
		{
			float[] sortedKeys = _sortedKeys;
			foreach (float key in sortedKeys)
			{
				builder.ResolveLabel(array[num]);
				ClientInteraction clientInteraction = module.Interactions[Interaction.ChargedNext[key]];
				clientInteraction.Compile(module, builder);
				builder.Jump(label);
				num++;
			}
		}
		builder.ResolveLabel(array[num]);
		if (Interaction.Failed != int.MinValue)
		{
			ClientInteraction clientInteraction2 = module.Interactions[Interaction.Failed];
			clientInteraction2.Compile(module, builder);
		}
		builder.ResolveLabel(label);
	}

	public override void Handle(GameInstance gameInstance, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		base.Handle(gameInstance, firstRun, time, type, context);
		if ((int)context.State.State != 4)
		{
			gameInstance.App.Interface.TriggerEvent("combat.setShowChargeProgress", false);
		}
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
		gameInstance.App.Interface.TriggerEvent("combat.setShowChargeProgress", false);
	}

	protected override void MatchServer0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Invalid comparison between Unknown and I4
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		if ((int)context.ServerData.State != 4 || context.ServerData.Progress >= _highestChargeValue)
		{
			if ((int)context.State.State == 3 && context.Labels != null)
			{
				ClientRootInteraction.Label[] labels = context.Labels;
				float[] sortedKeys = _sortedKeys;
				context.Jump(labels[(sortedKeys != null) ? sortedKeys.Length : 0]);
			}
			else
			{
				context.State.State = (InteractionState)0;
				context.State.ChargeValue = context.ServerData.Progress;
				JumpToChargeValue(context);
			}
		}
	}
}
