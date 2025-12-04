namespace HytaleClient.Data.Entities;

internal struct ClientMovementStates
{
	public bool IsIdle;

	public bool IsHorizontalIdle;

	public bool IsJumping;

	public bool IsFlying;

	public bool IsSprinting;

	public bool IsWalking;

	public bool IsCrouching;

	public bool IsForcedCrouching;

	public bool IsFalling;

	public bool IsClimbing;

	public bool IsInFluid;

	public bool IsSwimming;

	public bool IsSwimJumping;

	public bool IsOnGround;

	public bool IsEntityCollided;

	public bool IsMantling;

	public bool IsSliding;

	public bool IsMounting;

	public bool IsRolling;

	private static ClientMovementStates _idle = new ClientMovementStates
	{
		IsIdle = true
	};

	public static ClientMovementStates Idle => _idle;

	public override bool Equals(object obj)
	{
		return obj is ClientMovementStates other && Equals(other);
	}

	public bool Equals(ClientMovementStates other)
	{
		return IsIdle == other.IsIdle && IsHorizontalIdle == other.IsHorizontalIdle && IsJumping == other.IsJumping && IsFlying == other.IsFlying && IsSprinting == other.IsSprinting && IsWalking == other.IsWalking && IsCrouching == other.IsCrouching && IsForcedCrouching == other.IsForcedCrouching && IsFalling == other.IsFalling && IsClimbing == other.IsClimbing && IsInFluid == other.IsInFluid && IsSwimming == other.IsSwimming && IsSwimJumping == other.IsSwimJumping && IsOnGround == other.IsOnGround && IsEntityCollided == other.IsEntityCollided && IsMantling == other.IsMantling && IsSliding == other.IsSliding && IsMounting == other.IsMounting && IsRolling == other.IsRolling;
	}

	public override int GetHashCode()
	{
		int num = 0;
		num |= IsIdle.GetHashCode();
		num |= IsHorizontalIdle.GetHashCode() << 1;
		num |= IsJumping.GetHashCode() << 2;
		num |= IsFlying.GetHashCode() << 3;
		num |= IsSprinting.GetHashCode() << 4;
		num |= IsWalking.GetHashCode() << 5;
		num |= IsCrouching.GetHashCode() << 6;
		num |= IsForcedCrouching.GetHashCode() << 7;
		num |= IsFalling.GetHashCode() << 8;
		num |= IsClimbing.GetHashCode() << 9;
		num |= IsInFluid.GetHashCode() << 10;
		num |= IsSwimming.GetHashCode() << 11;
		num |= IsSwimJumping.GetHashCode() << 12;
		num |= IsOnGround.GetHashCode() << 13;
		num |= IsEntityCollided.GetHashCode() << 14;
		num |= IsMantling.GetHashCode() << 15;
		num |= IsSliding.GetHashCode() << 16;
		num |= IsMounting.GetHashCode() << 17;
		return num | (IsRolling.GetHashCode() << 18);
	}

	public static bool operator ==(ClientMovementStates value1, ClientMovementStates value2)
	{
		if (value1.IsIdle != value2.IsIdle)
		{
			return false;
		}
		if (value1.IsHorizontalIdle != value2.IsHorizontalIdle)
		{
			return false;
		}
		if (value1.IsJumping != value2.IsJumping)
		{
			return false;
		}
		if (value1.IsFlying != value2.IsFlying)
		{
			return false;
		}
		if (value1.IsSprinting != value2.IsSprinting)
		{
			return false;
		}
		if (value1.IsWalking != value2.IsWalking)
		{
			return false;
		}
		if (value1.IsCrouching != value2.IsCrouching)
		{
			return false;
		}
		if (value1.IsForcedCrouching != value2.IsForcedCrouching)
		{
			return false;
		}
		if (value1.IsFalling != value2.IsFalling)
		{
			return false;
		}
		if (value1.IsClimbing != value2.IsClimbing)
		{
			return false;
		}
		if (value1.IsInFluid != value2.IsInFluid)
		{
			return false;
		}
		if (value1.IsSwimming != value2.IsSwimming)
		{
			return false;
		}
		if (value1.IsSwimJumping != value2.IsSwimJumping)
		{
			return false;
		}
		if (value1.IsOnGround != value2.IsOnGround)
		{
			return false;
		}
		if (value1.IsEntityCollided != value2.IsEntityCollided)
		{
			return false;
		}
		if (value1.IsMantling != value2.IsMantling)
		{
			return false;
		}
		if (value1.IsSliding != value2.IsSliding)
		{
			return false;
		}
		if (value1.IsMounting != value2.IsMounting)
		{
			return false;
		}
		if (value1.IsRolling != value2.IsRolling)
		{
			return false;
		}
		return true;
	}

	public static bool operator !=(ClientMovementStates value1, ClientMovementStates value2)
	{
		return !(value1 == value2);
	}
}
