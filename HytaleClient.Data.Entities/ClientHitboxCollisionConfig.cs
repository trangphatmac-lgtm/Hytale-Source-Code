using HytaleClient.Protocol;

namespace HytaleClient.Data.Entities;

public class ClientHitboxCollisionConfig
{
	public enum ClientCollisionType
	{
		Hard,
		Soft
	}

	public const int NoHitboxCollisionConfigIndex = -1;

	public ClientCollisionType CollisionType;

	public float SoftCollisionOffsetRatio;

	public ClientHitboxCollisionConfig()
	{
	}

	public ClientHitboxCollisionConfig(HitboxCollisionConfig hitboxCollisionConfig)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		CollisionType collisionType_ = hitboxCollisionConfig.CollisionType_;
		CollisionType val = collisionType_;
		if ((int)val == 0)
		{
			CollisionType = ClientCollisionType.Hard;
		}
		else
		{
			CollisionType = ClientCollisionType.Soft;
		}
		SoftCollisionOffsetRatio = hitboxCollisionConfig.SoftCollisionOffsetRatio;
	}

	public ClientHitboxCollisionConfig Clone()
	{
		ClientHitboxCollisionConfig clientHitboxCollisionConfig = new ClientHitboxCollisionConfig();
		clientHitboxCollisionConfig.CollisionType = CollisionType;
		clientHitboxCollisionConfig.SoftCollisionOffsetRatio = SoftCollisionOffsetRatio;
		return clientHitboxCollisionConfig;
	}
}
