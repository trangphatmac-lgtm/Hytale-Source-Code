using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class BlockContactData
{
	public Vector3 CollisionNormal;

	public Vector3 CollisionPoint;

	public float CollisionStart;

	public float CollisionEnd;

	public bool OnGround;

	public int Damage;

	public bool IsSubmergeFluid;

	public bool Overlapping;

	public void Clear()
	{
	}

	public void Assign(BlockContactData other)
	{
		Assign(other, other.Damage, other.IsSubmergeFluid);
	}

	public void Assign(BlockContactData other, int damage, bool isSubmergedFluid)
	{
		CollisionNormal = other.CollisionNormal;
		CollisionPoint = other.CollisionPoint;
		CollisionStart = other.CollisionStart;
		CollisionEnd = other.CollisionEnd;
		OnGround = other.OnGround;
		Overlapping = other.Overlapping;
		SetDamageAndSubmerged(damage, isSubmergedFluid);
	}

	public void SetDamageAndSubmerged(int damage, bool isSubmerge)
	{
		Damage = damage;
		IsSubmergeFluid = isSubmerge;
	}
}
