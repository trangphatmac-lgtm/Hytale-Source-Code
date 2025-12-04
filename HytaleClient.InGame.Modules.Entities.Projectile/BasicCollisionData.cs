using System.Collections.Generic;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class BasicCollisionData
{
	private class BasicCollisionDataComparer : Comparer<BasicCollisionData>
	{
		public override int Compare(BasicCollisionData x, BasicCollisionData y)
		{
			return x.CollisionStart.CompareTo(y.CollisionStart);
		}
	}

	public static readonly IComparer<BasicCollisionData> CollisionStartComparator = new BasicCollisionDataComparer();

	public Vector3 CollisionPoint;

	public float CollisionStart;

	public void SetStart(Vector3 point, float start)
	{
		CollisionPoint = point;
		CollisionStart = start;
	}
}
