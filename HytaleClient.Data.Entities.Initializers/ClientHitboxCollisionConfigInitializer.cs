using HytaleClient.Protocol;

namespace HytaleClient.Data.Entities.Initializers;

public class ClientHitboxCollisionConfigInitializer
{
	public static void Initialize(HitboxCollisionConfig hitboxCollisionConfig, ref ClientHitboxCollisionConfig clientHitboxCollisionConfig)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		CollisionType collisionType_ = hitboxCollisionConfig.CollisionType_;
		CollisionType val = collisionType_;
		if ((int)val == 0)
		{
			clientHitboxCollisionConfig.CollisionType = ClientHitboxCollisionConfig.ClientCollisionType.Hard;
		}
		else
		{
			clientHitboxCollisionConfig.CollisionType = ClientHitboxCollisionConfig.ClientCollisionType.Soft;
		}
		clientHitboxCollisionConfig.SoftCollisionOffsetRatio = hitboxCollisionConfig.SoftCollisionOffsetRatio;
	}
}
