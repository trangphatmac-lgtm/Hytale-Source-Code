using System.Linq;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class ApplyForceInteraction : SimpleInteraction
{
	private class ClientForce
	{
		public readonly Vector3 Direction;

		public readonly bool AdjustVertical;

		public readonly float Force;

		public ClientForce(AppliedForce force)
		{
			Direction = new Vector3(force.Direction.X, force.Direction.Y, force.Direction.Z);
			AdjustVertical = force.AdjustVertical;
			Force = force.Force;
		}
	}

	public static bool DebugDisplay;

	private readonly FloatRange? _verticalClamp;

	private readonly ClientForce[] _forces;

	private const int NextLabelIndex = 0;

	private const int GroundLabelIndex = 1;

	private const int CollisionLabelIndex = 2;

	public ApplyForceInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
		if (interaction.VerticalClamp != null)
		{
			_verticalClamp = new FloatRange(interaction.VerticalClamp.InclusiveMin, interaction.VerticalClamp.InclusiveMax);
		}
		_forces = Interaction.Forces.Select((AppliedForce v) => new ClientForce(v)).ToArray();
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		if (firstRun || (Interaction.Duration > 0f && time < Interaction.Duration))
		{
			gameInstance.CharacterControllerModule.MovementController.RaycastDistance = Interaction.RaycastDistance;
			gameInstance.CharacterControllerModule.MovementController.RaycastHeightOffset = Interaction.RaycastHeightOffset;
			gameInstance.CharacterControllerModule.MovementController.RaycastMode = Interaction.RaycastMode_;
			Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, gameInstance.LocalPlayer.LookOrientation.Y);
			float x = gameInstance.LocalPlayer.LookOrientation.X;
			x = _verticalClamp?.Clamp(x) ?? x;
			Quaternion rotation2 = Quaternion.CreateFromAxisAngle(Vector3.Right, x);
			ChangeVelocityType changeType = Interaction.ChangeVelocityType_;
			Vector3 position = gameInstance.LocalPlayer.Position;
			for (int i = 0; i < _forces.Length; i++)
			{
				ClientForce clientForce = _forces[i];
				Vector3 value = clientForce.Direction;
				if (clientForce.AdjustVertical)
				{
					Vector3.Transform(ref value, ref rotation2, out value);
				}
				value *= clientForce.Force;
				Vector3.Transform(ref value, ref rotation, out value);
				if (DebugDisplay)
				{
					gameInstance.DebugDisplayModule.AddForce(position, value, new Vector3(0f, (float)(i + 1) * (1f / (float)_forces.Length), 0f), 15f, fade: true);
				}
				gameInstance.CharacterControllerModule.MovementController.VelocityChange(value.X, value.Y, value.Z, changeType, Interaction.VelocityConfig_);
				changeType = (ChangeVelocityType)0;
				position += value;
			}
			context.State.State = (InteractionState)4;
			gameInstance.CharacterControllerModule.MovementController.ApplyMarioFallForce = false;
		}
		else
		{
			bool flag = time >= Interaction.GroundCheckDelay && Interaction.WaitForGround && gameInstance.CharacterControllerModule.MovementController.MovementStates.IsOnGround;
			bool flag2 = time >= Interaction.CollisionCheckDelay && Interaction.WaitForCollision && gameInstance.CharacterControllerModule.MovementController.MovementStates.IsEntityCollided;
			bool flag3 = (Interaction.RunTime <= 0f && !Interaction.WaitForCollision && !Interaction.WaitForGround) || (Interaction.RunTime > 0f && time >= Interaction.RunTime);
			context.State.ApplyForceState_ = (ApplyForceState)0;
			if (flag)
			{
				context.State.ApplyForceState_ = (ApplyForceState)1;
				context.State.State = (InteractionState)0;
				context.Jump(context.Labels[1]);
			}
			else if (flag2)
			{
				context.State.ApplyForceState_ = (ApplyForceState)2;
				context.State.State = (InteractionState)0;
				context.Jump(context.Labels[2]);
			}
			else if (flag3)
			{
				context.State.ApplyForceState_ = (ApplyForceState)3;
				context.State.State = (InteractionState)0;
				context.Jump(context.Labels[0]);
			}
			else
			{
				context.State.State = (InteractionState)4;
			}
			base.Tick0(gameInstance, clickType, hasAnyButtonClick, firstRun, time, type, context);
		}
	}

	public override void Handle(GameInstance gameInstance, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		base.Handle(gameInstance, firstRun, time, type, context);
		if ((int)context.State.State != 4)
		{
			gameInstance.CharacterControllerModule.MovementController.RaycastDistance = 0f;
			gameInstance.CharacterControllerModule.MovementController.RaycastHeightOffset = 0f;
			gameInstance.CharacterControllerModule.MovementController.RaycastMode = (RaycastMode)0;
		}
	}

	public override void Compile(InteractionModule module, ClientRootInteraction.OperationsBuilder builder)
	{
		ClientRootInteraction.Label[] array = new ClientRootInteraction.Label[3];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = builder.CreateUnresolvedLabel();
		}
		builder.AddOperation(Id, array);
		ClientRootInteraction.Label label = builder.CreateUnresolvedLabel();
		Resolve(module, builder, Interaction.Next, array[0], label);
		Resolve(module, builder, Interaction.GroundNext, array[1], label);
		Resolve(module, builder, Interaction.CollisionNext, array[2], label);
		builder.ResolveLabel(label);
	}

	public static void Resolve(InteractionModule module, ClientRootInteraction.OperationsBuilder builder, int id, ClientRootInteraction.Label label, ClientRootInteraction.Label endLabel)
	{
		builder.ResolveLabel(label);
		if (id != int.MinValue)
		{
			ClientInteraction clientInteraction = module.Interactions[id];
			clientInteraction.Compile(module, builder);
		}
		builder.Jump(endLabel);
	}
}
