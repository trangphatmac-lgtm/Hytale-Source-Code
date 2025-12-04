using System;
using System.Collections.Generic;
using HytaleClient.Data.EntityUI;
using HytaleClient.Data.EntityUI.CombatText;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame.EntityUI;

internal class CombatTextUIComponentRenderer : EntityUIComponentRenderer<ClientCombatTextUIComponent>
{
	private struct CombatText
	{
		public float HitAngleModifier;

		public string Text;

		public Vector2f PositionOffset;
	}

	private Dictionary<(int, int), CombatText> _combatTexts = new Dictionary<(int, int), CombatText>();

	private int _combatTextIndex;

	private UIFragment _uiFragment;

	private readonly Random _random = new Random();

	public CombatTextUIComponentRenderer(InGameView inGameView)
		: base(inGameView)
	{
	}

	public override void Build(Element parent)
	{
		_inGameView.Interface.TryGetDocument("InGame/EntityUI/CombatText.ui", out var document);
		_uiFragment = document.Instantiate(parent.Desktop, parent);
	}

	protected override bool ShouldBeVisibleForEntity(Entity entity, int entitiesCount, float distanceToCamera)
	{
		int entityUIMaxEntities = base._settings.EntityUIMaxEntities;
		float entityUIMaxDistance = base._settings.EntityUIMaxDistance;
		bool flag = entitiesCount >= entityUIMaxEntities;
		bool flag2 = distanceToCamera > entityUIMaxDistance;
		return !flag && !flag2;
	}

	public override void RegisterDrawTasksForEntity(ClientCombatTextUIComponent component, Entity entity, Matrix transformationMatrix, float distanceToCamera, int entitiesCount, ref EntityUIDrawTask[] drawTasks, ref int drawTasksCount)
	{
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Expected O, but got Unknown
		if (!ShouldBeVisibleForEntity(entity, entitiesCount, distanceToCamera))
		{
			return;
		}
		if (entity.CombatTextsCount > 0)
		{
			float num = System.Math.Abs(component.MinRandomPositionOffset.X);
			float num2 = System.Math.Abs(component.MinRandomPositionOffset.Y);
			float num3 = System.Math.Abs(component.MaxRandomPositionOffset.X);
			float num4 = System.Math.Abs(component.MaxRandomPositionOffset.Y);
			for (int i = 0; i < entity.CombatTextsCount; i++)
			{
				int num5 = _combatTextIndex++;
				(int, int) key = (entity.NetworkId, num5);
				ApplyTransitionState(key, component.Duration);
				switch (num5 % 4)
				{
				case 0:
					num = 0f - num;
					num3 = 0f - num3;
					break;
				case 1:
					num2 = 0f - num2;
					num4 = 0f - num4;
					break;
				case 2:
					num = 0f - num;
					num3 = 0f - num3;
					num2 = 0f - num2;
					num4 = 0f - num4;
					break;
				}
				float num6 = _random.NextFloat(num, num3);
				float num7 = _random.NextFloat(num2, num4);
				Entity.CombatText combatText = entity.CombatTexts[i];
				_combatTexts[key] = new CombatText
				{
					HitAngleModifier = (float)combatText.HitAngleDeg * component.HitAngleModifierStrength,
					Text = combatText.Text,
					PositionOffset = new Vector2f(num6, num7)
				};
			}
			entity.ClearCombatTexts();
		}
		float num8 = _inGameView.Desktop.ViewportRectangle.Width;
		float num9 = _inGameView.Desktop.ViewportRectangle.Height;
		float num10 = num8 / 2f - component.ViewportMargin;
		float num11 = num9 / 2f - component.ViewportMargin;
		foreach (KeyValuePair<(int, int), TransitionState> transitionState in _transitionStates)
		{
			if (entity.NetworkId == transitionState.Key.Item1)
			{
				CombatText combatText2 = _combatTexts[transitionState.Key];
				float progress = transitionState.Value.Progress;
				int num12 = drawTasksCount++;
				EntityUIDrawTask task = drawTasks[num12];
				task.ComponentId = component.Id;
				task.StringValue = combatText2.Text;
				task.TransformationMatrix = component.ApplyHitboxOffset(transformationMatrix);
				float value = task.TransformationMatrix.M41 + combatText2.PositionOffset.X;
				float value2 = task.TransformationMatrix.M42 + combatText2.PositionOffset.Y;
				value = MathHelper.Clamp(value, 0f - num10, num10);
				value2 = MathHelper.Clamp(value2, 0f - num11, num11);
				task.TransformationMatrix.M41 = value;
				task.TransformationMatrix.M42 = value2;
				AnimationEvent[] animationEvents = component.AnimationEvents;
				foreach (AnimationEvent animationEvent in animationEvents)
				{
					animationEvent.ApplyAnimationState(ref task, progress);
				}
				task.TransformationMatrix.M41 += combatText2.HitAngleModifier * progress;
				drawTasks[num12] = task;
			}
		}
	}

	public override void PrepareForDraw(ClientCombatTextUIComponent component, EntityUIDrawTask task)
	{
		Label label = _uiFragment.Get<Label>("Text");
		label.Text = task.StringValue;
		label.Style.FontSize = component.FontSize;
		if (task.Scale.HasValue)
		{
			label.Style.FontSize *= task.Scale.Value;
		}
		label.Style.TextColor = component.TextColorUInt32;
		Group group = _uiFragment.Get<Group>("Container");
		group.Layout();
		group.PrepareForDraw();
	}

	protected override void OnTransitionRemoved((int, int) transitionKey)
	{
		_combatTexts.Remove(transitionKey);
	}
}
