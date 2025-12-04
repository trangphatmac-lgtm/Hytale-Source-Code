using HytaleClient.Protocol;

namespace HytaleClient.Data.Entities.Initializers;

internal static class ClientMovementStatesProtocolHelper
{
	public static void Parse(MovementStates networkMovementStates, ref ClientMovementStates movementStates)
	{
		movementStates.IsIdle = networkMovementStates.Idle;
		movementStates.IsHorizontalIdle = networkMovementStates.HorizontalIdle;
		movementStates.IsJumping = networkMovementStates.Jumping;
		movementStates.IsFlying = networkMovementStates.Flying;
		movementStates.IsWalking = networkMovementStates.Walking;
		movementStates.IsSprinting = networkMovementStates.Sprinting;
		movementStates.IsCrouching = networkMovementStates.Crouching;
		movementStates.IsForcedCrouching = networkMovementStates.ForcedCrouching;
		movementStates.IsFalling = networkMovementStates.Falling;
		movementStates.IsClimbing = networkMovementStates.Climbing;
		movementStates.IsInFluid = networkMovementStates.InFluid;
		movementStates.IsSwimming = networkMovementStates.Swimming;
		movementStates.IsSwimJumping = networkMovementStates.SwimJumping;
		movementStates.IsOnGround = networkMovementStates.OnGround;
		movementStates.IsMantling = networkMovementStates.Mantling;
		movementStates.IsSliding = networkMovementStates.Sliding;
		movementStates.IsMounting = networkMovementStates.Mounting;
		movementStates.IsRolling = networkMovementStates.Rolling;
	}

	public static MovementStates ToPacket(ref ClientMovementStates movementStates)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		MovementStates val = new MovementStates();
		val.Idle = movementStates.IsIdle;
		val.HorizontalIdle = movementStates.IsHorizontalIdle;
		val.Jumping = movementStates.IsJumping;
		val.Flying = movementStates.IsFlying;
		val.Walking = movementStates.IsWalking;
		val.Running = !movementStates.IsIdle && !movementStates.IsHorizontalIdle && !movementStates.IsWalking && !movementStates.IsSprinting;
		val.Sprinting = movementStates.IsSprinting;
		val.Crouching = movementStates.IsCrouching;
		val.ForcedCrouching = movementStates.IsForcedCrouching;
		val.Falling = movementStates.IsFalling;
		val.Climbing = movementStates.IsClimbing;
		val.InFluid = movementStates.IsInFluid;
		val.Swimming = movementStates.IsSwimming;
		val.SwimJumping = movementStates.IsSwimJumping;
		val.OnGround = movementStates.IsOnGround;
		val.Mantling = movementStates.IsMantling;
		val.Sliding = movementStates.IsSliding;
		val.Mounting = movementStates.IsMounting;
		val.Rolling = movementStates.IsRolling;
		return val;
	}
}
