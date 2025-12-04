using HytaleClient.Data.EntityUI.CombatText;
using HytaleClient.Graphics;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Interface.InGame.EntityUI;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.EntityUI;

internal class ClientCombatTextUIComponent : ClientEntityUIComponent
{
	public Vector2f MinRandomPositionOffset;

	public Vector2f MaxRandomPositionOffset;

	public float ViewportMargin;

	public float Duration;

	public float HitAngleModifierStrength;

	public float FontSize;

	public Color TextColor;

	public AnimationEvent[] AnimationEvents;

	public EntityUIComponentRenderer<ClientCombatTextUIComponent> Renderer;

	public UInt32Color TextColorUInt32 => UInt32Color.FromRGBA((byte)TextColor.Red, (byte)TextColor.Green, (byte)TextColor.Blue, byte.MaxValue);

	private ClientCombatTextUIComponent()
	{
	}

	public ClientCombatTextUIComponent(int id, EntityUIComponent component, EntityUIComponentRenderer<ClientCombatTextUIComponent> renderer)
		: base(id, component)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Expected I4, but got Unknown
		RangeVector2f combatTextRandomPositionOffsetRange = component.CombatTextRandomPositionOffsetRange;
		MinRandomPositionOffset = new Vector2f(combatTextRandomPositionOffsetRange.X.Min, combatTextRandomPositionOffsetRange.Y.Min);
		MaxRandomPositionOffset = new Vector2f(combatTextRandomPositionOffsetRange.X.Max, combatTextRandomPositionOffsetRange.Y.Max);
		ViewportMargin = component.CombatTextViewportMargin;
		Duration = component.CombatTextDuration;
		HitAngleModifierStrength = component.CombatTextHitAngleModifierStrength;
		FontSize = component.CombatTextFontSize;
		TextColor = component.CombatTextColor;
		AnimationEvents = new AnimationEvent[component.CombatTextAnimationEvents.Length];
		for (int i = 0; i < component.CombatTextAnimationEvents.Length; i++)
		{
			CombatTextEntityUIComponentAnimationEvent val = component.CombatTextAnimationEvents[i];
			CombatTextEntityUIAnimationEventType type = val.Type;
			CombatTextEntityUIAnimationEventType val2 = type;
			switch ((int)val2)
			{
			case 0:
				AnimationEvents[i] = new ScaleAnimationEvent(component.CombatTextAnimationEvents[i]);
				break;
			case 1:
				AnimationEvents[i] = new PositionAnimationEvent(component.CombatTextAnimationEvents[i]);
				break;
			case 2:
				AnimationEvents[i] = new OpacityAnimationEvent(component.CombatTextAnimationEvents[i]);
				break;
			}
		}
		Renderer = renderer;
	}

	public override ClientEntityUIComponent Clone()
	{
		return new ClientCombatTextUIComponent
		{
			Id = Id,
			HitboxOffset = HitboxOffset,
			Unknown = Unknown,
			Renderer = Renderer,
			MinRandomPositionOffset = MinRandomPositionOffset,
			MaxRandomPositionOffset = MaxRandomPositionOffset,
			ViewportMargin = ViewportMargin,
			Duration = Duration,
			HitAngleModifierStrength = HitAngleModifierStrength,
			FontSize = FontSize,
			TextColor = TextColor,
			AnimationEvents = AnimationEvents
		};
	}

	public override void RegisterDrawTasksForEntity(Entity entity, Matrix transformationMatrix, float distanceToCamera, int entitiesCount, ref EntityUIDrawTask[] drawTasks, ref int drawTasksCount)
	{
		Renderer.RegisterDrawTasksForEntity(this, entity, transformationMatrix, distanceToCamera, entitiesCount, ref drawTasks, ref drawTasksCount);
	}

	public override void PrepareForDraw(EntityUIDrawTask task)
	{
		Renderer.PrepareForDraw(this, task);
	}
}
