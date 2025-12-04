using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class BoxBlockIntersectionEvaluator : BlockContactData
{
	protected BoundingBox _box;

	protected Vector3 _worldUp = Vector3.Up;

	protected bool _touchCeil;

	protected int _resultCode;

	public BoxBlockIntersectionEvaluator()
	{
		SetStartEnd(0f, 1f);
	}

	public void SetCollisionData(BlockCollisionData data, CollisionConfig collisionConfig, int hitboxIndex)
	{
		data.SetStart(CollisionPoint, CollisionStart);
		data.SetEnd(CollisionEnd, CollisionNormal);
		data.SetBlockData(collisionConfig);
		data.SetDetailBoxIndex(hitboxIndex);
		data.SetTouchingOverlapping(CollisionMath.IsTouching(_resultCode), CollisionMath.IsOverlapping(_resultCode));
	}

	public BoxBlockIntersectionEvaluator SetBox(BoundingBox box)
	{
		_box = box;
		return this;
	}

	public BoxBlockIntersectionEvaluator ExpandBox(float radius)
	{
		_box.Grow(new Vector3(radius));
		return this;
	}

	public BoxBlockIntersectionEvaluator SetPosition(Vector3 pos)
	{
		CollisionPoint = pos;
		return this;
	}

	public BoxBlockIntersectionEvaluator SetBox(BoundingBox box, Vector3 pos)
	{
		return SetBox(box).SetPosition(pos);
	}

	public BoxBlockIntersectionEvaluator OffsetPosition(Vector3 offset)
	{
		CollisionPoint += offset;
		return this;
	}

	public BoxBlockIntersectionEvaluator SetStartEnd(float start, float end)
	{
		CollisionStart = start;
		CollisionEnd = end;
		return this;
	}

	public int IntersectBoxComputeTouch(BoundingBox otherBox, float x, float y, float z)
	{
		int num = (_resultCode = CollisionMath.IntersectAABBs(CollisionPoint.X, CollisionPoint.Y, CollisionPoint.Z, _box, x, y, z, otherBox));
		OnGround = false;
		_touchCeil = false;
		CollisionNormal = Vector3.Zero;
		Overlapping = CollisionMath.IsOverlapping(_resultCode);
		if (((uint)num & 7u) != 0)
		{
			if (_worldUp.Y != 0f)
			{
				if (((uint)num & 2u) != 0)
				{
					CollisionNormal.Y = ((y + otherBox.Min.Y < CollisionPoint.Y + _box.Min.Y) ? 1 : (-1));
					OnGround = CollisionNormal.Y == _worldUp.Y;
					_touchCeil = !OnGround;
				}
				else if (((uint)num & (true ? 1u : 0u)) != 0)
				{
					CollisionNormal.X = ((x + otherBox.Min.X < CollisionPoint.X + _box.Min.X) ? 1 : (-1));
				}
				else
				{
					CollisionNormal.Z = ((z + otherBox.Min.Z < CollisionPoint.Z + _box.Min.Z) ? 1 : (-1));
				}
			}
			else if (_worldUp.X != 0f)
			{
				if (((uint)num & (true ? 1u : 0u)) != 0)
				{
					CollisionNormal.X = ((x + otherBox.Min.X < CollisionPoint.X + _box.Min.X) ? 1 : (-1));
					OnGround = CollisionNormal.X == _worldUp.X;
					_touchCeil = !OnGround;
				}
				else if (((uint)num & 2u) != 0)
				{
					CollisionNormal.Y = ((y + otherBox.Min.Y < CollisionPoint.Y + _box.Min.Y) ? 1 : (-1));
				}
				else
				{
					CollisionNormal.Z = ((z + otherBox.Min.Z < CollisionPoint.Z + _box.Min.Z) ? 1 : (-1));
				}
			}
			else if (((uint)num & 4u) != 0)
			{
				CollisionNormal.Z = ((z + otherBox.Min.Z < CollisionPoint.Z + _box.Min.Z) ? 1 : (-1));
				OnGround = CollisionNormal.Z == _worldUp.Z;
				_touchCeil = !OnGround;
			}
			else if (((uint)num & 2u) != 0)
			{
				CollisionNormal.Y = ((y + otherBox.Min.Y < CollisionPoint.Y + _box.Min.Y) ? 1 : (-1));
			}
			else
			{
				CollisionNormal.X = ((x + otherBox.Min.X < CollisionPoint.X + _box.Min.X) ? 1 : (-1));
			}
		}
		return num;
	}
}
