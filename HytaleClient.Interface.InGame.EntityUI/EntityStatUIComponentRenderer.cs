using System;
using System.Collections.Generic;
using HytaleClient.Data.EntityUI;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame.EntityUI;

internal class EntityStatUIComponentRenderer : EntityUIComponentRenderer<ClientEntityStatUIComponent>
{
	private UIFragment _healthBarFragment;

	private HashSet<int> _entitiesWithComponent = new HashSet<int>();

	public EntityStatUIComponentRenderer(InGameView inGameView)
		: base(inGameView)
	{
	}

	public override void Build(Element parent)
	{
		_inGameView.Interface.TryGetDocument("InGame/EntityUI/HealthBar.ui", out var document);
		_healthBarFragment = document.Instantiate(parent.Desktop, parent);
	}

	protected override bool ShouldBeVisibleForEntity(Entity entity, int entitiesCount, float distanceToCamera)
	{
		int entityUIMaxEntities = base._settings.EntityUIMaxEntities;
		float entityUIMaxDistance = base._settings.EntityUIMaxDistance;
		bool visibilityPrediction = entity.VisibilityPrediction;
		bool debugUI = _inGameView.InGame.Instance.EntityStoreModule.CurrentSetup.DebugUI;
		bool flag = entity.SmoothHealth == -1f;
		bool flag2 = entity.SmoothHealth == 1f;
		bool flag3 = entitiesCount >= entityUIMaxEntities;
		bool flag4 = distanceToCamera > entityUIMaxDistance;
		return visibilityPrediction && !flag3 && !flag4 && !flag && (!flag2 || debugUI);
	}

	public override void RegisterDrawTasksForEntity(ClientEntityStatUIComponent component, Entity entity, Matrix transformationMatrix, float distanceToCamera, int entitiesCount, ref EntityUIDrawTask[] drawTasks, ref int drawTasksCount)
	{
		bool flag = ShouldBeVisibleForEntity(entity, entitiesCount, distanceToCamera);
		float entityUIHideDelay = base._settings.EntityUIHideDelay;
		float entityUIFadeInDuration = base._settings.EntityUIFadeInDuration;
		float entityUIFadeOutDuration = base._settings.EntityUIFadeOutDuration;
		(int, int) key = (entity.NetworkId, 0);
		if (!_entitiesWithComponent.Contains(entity.NetworkId))
		{
			if (flag)
			{
				ApplyTransitionState(key, 0f - entityUIFadeInDuration);
				_entitiesWithComponent.Add(entity.NetworkId);
			}
		}
		else if (!flag)
		{
			ApplyTransitionState(key, entityUIFadeOutDuration, entityUIHideDelay);
			_entitiesWithComponent.Remove(entity.NetworkId);
		}
		TransitionState value;
		bool flag2 = _transitionStates.TryGetValue(key, out value);
		if (_entitiesWithComponent.Contains(entity.NetworkId) || flag2)
		{
			int num = (flag2 ? GetTransitionOpacity(value) : 255);
			if (num != 0)
			{
				int num2 = drawTasksCount++;
				EntityUIDrawTask entityUIDrawTask = drawTasks[num2];
				entityUIDrawTask.FloatValue = entity.SmoothHealth;
				entityUIDrawTask.ComponentId = component.Id;
				entityUIDrawTask.TransformationMatrix = component.ApplyHitboxOffset(transformationMatrix);
				entityUIDrawTask.Opacity = Convert.ToByte(num);
				drawTasks[num2] = entityUIDrawTask;
			}
		}
	}

	private int GetTransitionOpacity(TransitionState state)
	{
		if (state.DelayTimer > 0f)
		{
			return (state.Duration > 0f) ? 255 : 0;
		}
		if (state.Duration > 0f)
		{
			return (int)(state.Timer / state.Duration * 255f);
		}
		if (state.Duration < 0f)
		{
			return 255 - (int)(state.Timer / state.Duration * 255f);
		}
		return 0;
	}

	public override void PrepareForDraw(ClientEntityStatUIComponent component, EntityUIDrawTask task)
	{
		_healthBarFragment.Get<ProgressBar>("Fill").Value = task.FloatValue;
		Group group = _healthBarFragment.Get<Group>("Container");
		group.Layout();
		group.PrepareForDraw();
	}
}
