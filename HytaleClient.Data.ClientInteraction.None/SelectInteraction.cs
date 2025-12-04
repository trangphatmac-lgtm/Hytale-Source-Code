using System;
using System.Collections.Generic;
using HytaleClient.Data.ClientInteraction.Selector;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.None;

internal class SelectInteraction : SimpleInteraction
{
	private readonly SelectorType _selector;

	public SelectInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
		if (interaction.AoeCircleSelector_ != null)
		{
			_selector = new AOECircleSelector(interaction.AoeCircleSelector_);
			return;
		}
		if (interaction.HorizontalSelector_ != null)
		{
			_selector = new HorizontalSelector(interaction.HorizontalSelector_);
			return;
		}
		if (interaction.StabSelector_ != null)
		{
			_selector = new StabSelector(interaction.StabSelector_);
			return;
		}
		if (interaction.AoeCylinderSelector_ != null)
		{
			_selector = new AOECylinderSelector(interaction.AoeCylinderSelector_);
			return;
		}
		if (interaction.RaycastSelector_ != null)
		{
			_selector = new RaycastSelector(interaction.RaycastSelector_);
			return;
		}
		throw new ArgumentException("Missing selector for interaction");
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Invalid comparison between Unknown and I4
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Invalid comparison between Unknown and I4
		if (_selector == null)
		{
			return;
		}
		if (firstRun || context.InstanceStore.EntitySelector == null)
		{
			context.InstanceStore.EntitySelector = _selector.NewSelector(gameInstance.InteractionModule.Random);
			if (firstRun && time <= 0f && Interaction.RunTime > 0f)
			{
				return;
			}
		}
		HytaleClient.Data.ClientInteraction.Selector.Selector entitySelector = context.InstanceStore.EntitySelector;
		entitySelector.Tick(gameInstance, context.Entity, System.Math.Min(time, Interaction.RunTime), Interaction.RunTime);
		if (Interaction.HitEntity != int.MinValue || Interaction.HitEntityRules != null)
		{
			InteractionMetaStore instanceStore = context.InstanceStore;
			HashSet<int> obj = context.InstanceStore.HitEntities ?? new HashSet<int>();
			HashSet<int> hashSet = obj;
			instanceStore.HitEntities = obj;
			HashSet<int> hitEntities = hashSet;
			entitySelector.SelectTargetEntities(gameInstance, context.Entity, delegate(Entity entity, Vector4 hit)
			{
				//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
				//IL_01f1: Expected O, but got Unknown
				//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
				//IL_01f6: Expected O, but got Unknown
				//IL_0263: Unknown result type (might be due to invalid IL or missing references)
				//IL_0278: Unknown result type (might be due to invalid IL or missing references)
				//IL_0284: Expected O, but got Unknown
				if (!entity.PredictionId.HasValue && hitEntities.Add(entity.NetworkId))
				{
					int num = Interaction.HitEntity;
					if (num != int.MinValue && (entity.NetworkId == context.Entity.NetworkId || entity.IsDead() || !entity.IsTangible()))
					{
						num = int.MinValue;
					}
					if (Interaction.HitEntityRules != null)
					{
						HitEntity[] hitEntityRules = Interaction.HitEntityRules;
						foreach (HitEntity val in hitEntityRules)
						{
							bool flag = true;
							EntityMatcher[] matchers = val.Matchers;
							foreach (EntityMatcher matcher in matchers)
							{
								if (!MatchRule(matcher, gameInstance, context.Entity, entity))
								{
									flag = false;
									break;
								}
							}
							if (flag)
							{
								num = val.Next;
							}
						}
					}
					if (num != int.MinValue)
					{
						InteractionMetaStore instanceStore2 = context.InstanceStore;
						List<SelectedHitEntity> obj2 = context.InstanceStore.RecordedHits ?? new List<SelectedHitEntity>();
						List<SelectedHitEntity> list = obj2;
						instanceStore2.RecordedHits = obj2;
						List<SelectedHitEntity> list2 = list;
						list2.Add(new SelectedHitEntity(entity.NetworkId, new Vector3f(hit.X, hit.Y, hit.Z), entity.Position.ToPositionPacket(), entity.BodyOrientation.ToDirectionPacket()));
						InteractionContext interactionContext = context.Duplicate();
						interactionContext.MetaStore.TargetEntity = entity;
						interactionContext.MetaStore.HitLocation = hit;
						interactionContext.MetaStore.TargetBlock = null;
						interactionContext.MetaStore.TargetBlockRaw = null;
						interactionContext.MetaStore.SelectMetaStore = context.InstanceStore;
						context.ForkPredicted(new InteractionChainData(), context.Chain.Type, interactionContext, num);
					}
				}
			}, Filter);
			context.State.AttackerPos = gameInstance.LocalPlayer.Position.ToPositionPacket();
			context.State.AttackerRot = gameInstance.LocalPlayer.BodyOrientation.ToDirectionPacket();
			if (context.State.HitEntities?.Length != context.InstanceStore.HitEntities?.Count)
			{
				context.State.HitEntities = context.InstanceStore.RecordedHits?.ToArray();
			}
			if (context.Labels != null && hitEntities.Count == 0 && (int)context.State.State == 0 && ((int)Interaction.FailOn == 1 || (int)Interaction.FailOn == 3))
			{
				context.State.State = (InteractionState)3;
			}
		}
		base.Tick0(gameInstance, clickType, hasAnyButtonClick, firstRun, time, type, context);
		bool Filter(Entity e)
		{
			if (Interaction.IgnoreOwner && e.NetworkId == gameInstance.LocalPlayer.NetworkId)
			{
				return false;
			}
			return context.Entity.NetworkId != e.NetworkId;
		}
	}

	public override InteractionChain MapForkChain(InteractionContext context, InteractionChainData data)
	{
		if (data.BlockPosition_ != null)
		{
			return null;
		}
		Dictionary<ulong, InteractionChain> forkedChains = context.Chain.ForkedChains;
		foreach (InteractionChain value in forkedChains.Values)
		{
			if (value.BaseForkedChainId.EntryIndex == context.Entry.Index)
			{
				InteractionChainData chainData = value.ChainData;
				if (chainData.EntityId == data.EntityId)
				{
					return value;
				}
			}
		}
		return null;
	}

	private static bool MatchRule(EntityMatcher matcher, GameInstance gameInstance, Entity attacker, Entity target)
	{
		return MatchRule0(matcher, gameInstance, attacker, target) ^ matcher.Invert;
	}

	private static bool MatchRule0(EntityMatcher matcher, GameInstance gameInstance, Entity attacker, Entity target)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected I4, but got Unknown
		EntityMatcherType type = matcher.Type;
		EntityMatcherType val = type;
		return (int)val switch
		{
			1 => !target.IsInvulnerable(), 
			2 => target.PlayerSkin != null, 
			0 => true, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}
}
