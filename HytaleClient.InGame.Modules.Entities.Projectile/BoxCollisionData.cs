using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class BoxCollisionData : BasicCollisionData
{
	public float CollisionEnd;

	public Vector3 CollisionNormal;

	public void SetEnd(float collisionEnd, Vector3 collisionNormal)
	{
		CollisionEnd = collisionEnd;
		CollisionNormal = collisionNormal;
	}
}
