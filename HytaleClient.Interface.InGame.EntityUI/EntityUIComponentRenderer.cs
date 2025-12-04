using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Data.EntityUI;
using HytaleClient.Data.UserSettings;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame.EntityUI;

internal abstract class EntityUIComponentRenderer<T> where T : ClientEntityUIComponent
{
	protected struct TransitionState
	{
		public float DelayTimer;

		public float Duration;

		public float Timer;

		public float Progress;
	}

	protected readonly InGameView _inGameView;

	protected readonly Dictionary<(int, int), TransitionState> _transitionStates = new Dictionary<(int, int), TransitionState>();

	protected HytaleClient.Data.UserSettings.Settings _settings => _inGameView.Interface.App.Settings;

	protected EntityUIComponentRenderer(InGameView inGameView)
	{
		_inGameView = inGameView;
	}

	public abstract void Build(Element parent);

	protected abstract bool ShouldBeVisibleForEntity(Entity entity, int entitiesCount, float distanceToCamera);

	public abstract void RegisterDrawTasksForEntity(T component, Entity entity, Matrix transformationMatrix, float distanceToCamera, int entitiesCount, ref EntityUIDrawTask[] drawTasks, ref int drawTasksCount);

	public abstract void PrepareForDraw(T component, EntityUIDrawTask task);

	protected void ApplyTransitionState((int, int) key, float duration, float delay = 0f)
	{
		if (_transitionStates.TryGetValue(key, out var value))
		{
			if (duration * value.Timer > 0f)
			{
				return;
			}
			if (value.DelayTimer > 0f)
			{
				_transitionStates.Remove(key);
				return;
			}
			value.Duration = duration;
			value.Timer = ((System.Math.Abs(value.Timer) > System.Math.Abs(duration)) ? duration : (0f - value.Timer));
		}
		else
		{
			TransitionState transitionState = default(TransitionState);
			transitionState.DelayTimer = delay;
			transitionState.Duration = duration;
			transitionState.Timer = duration;
			value = transitionState;
		}
		_transitionStates[key] = value;
	}

	public void Animate(float deltaTime)
	{
		foreach (var item in _transitionStates.Keys.ToList())
		{
			TransitionState value = _transitionStates[item];
			if (value.DelayTimer > 0f)
			{
				value.DelayTimer -= deltaTime;
				_transitionStates[item] = value;
				break;
			}
			bool flag = false;
			if (value.Duration < 0f && value.Timer < 0f)
			{
				value.Timer += deltaTime;
				if (value.Timer >= 0f)
				{
					flag = true;
				}
			}
			else if (value.Duration > 0f && value.Timer > 0f)
			{
				value.Timer -= deltaTime;
				if (value.Timer <= 0f)
				{
					flag = true;
				}
			}
			if (flag)
			{
				_transitionStates.Remove(item);
				OnTransitionRemoved(item);
			}
			else
			{
				value.Progress = 1f - value.Timer / value.Duration;
				_transitionStates[item] = value;
			}
		}
	}

	protected virtual void OnTransitionRemoved((int, int) transitionKey)
	{
	}
}
