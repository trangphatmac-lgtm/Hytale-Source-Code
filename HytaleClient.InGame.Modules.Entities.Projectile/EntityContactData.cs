using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class EntityContactData
{
	public Vector3 CollisionPoint;

	public float CollisionStart;

	public float CollisionEnd;

	public Entity EntityReference;

	public string CollisionDetailName;

	public void Assign(Vector3 collisionPoint, float collisionStart, float collisionEnd, Entity entityReference, string collisionDetailName)
	{
		CollisionPoint = collisionPoint;
		CollisionStart = collisionStart;
		CollisionEnd = collisionEnd;
		EntityReference = entityReference;
		CollisionDetailName = collisionDetailName;
	}

	public void Clear()
	{
		EntityReference = null;
	}
}
