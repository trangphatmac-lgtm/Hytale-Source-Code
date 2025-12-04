using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class MovingBoxBoxCollisionEvaluator : BlockContactData
{
	protected class Collision1D
	{
		public const int CollisionOutside = 0;

		public const int CollisionInside = 1;

		public const int CollisionTouchMin = 2;

		public const int CollisionTouchMax = 3;

		public float P;

		public float V;

		public float Min;

		public float Max;

		public float TEnter;

		public float TLeave;

		public float Normal;

		public int Kind;

		public bool Touching;

		public bool IsColliding(float min, float max)
		{
			Min = min;
			Max = max;
			TEnter = float.MinValue;
			TLeave = float.MaxValue;
			Normal = 0f;
			Touching = false;
			float num = min - P;
			if (num >= -1E-05f)
			{
				if (V < num - 1E-05f)
				{
					return false;
				}
				Normal = -1f;
				ComputeTouchOrOutside(max, num, 2);
				return true;
			}
			num = max - P;
			if (num <= 1E-05f)
			{
				if (V > num + 1E-05f)
				{
					return false;
				}
				Normal = 1f;
				ComputeTouchOrOutside(min, num, 3);
				return true;
			}
			TEnter = 0f;
			if (V < 0f)
			{
				TLeave = ClampPos((min - P) / V);
				Normal = 1f;
			}
			else if (V > 0f)
			{
				TLeave = ClampPos((max - P) / V);
				Normal = -1f;
			}
			Kind = 1;
			return true;
		}

		private void ComputeTouchOrOutside(float border, float dist, int touchCode)
		{
			if (V != 0f)
			{
				TEnter = MathHelper.Clamp(dist / V, 0f, 1f);
				if (TEnter != 0f && (double)TEnter < 1E-08)
				{
					TEnter = 0f;
				}
				TLeave = ClampPos((border - P) / V);
				Kind = 0;
			}
			else
			{
				TEnter = 0f;
				Kind = touchCode;
			}
		}

		private float ClampPos(float v)
		{
			return (v >= 0f) ? v : 0f;
		}
	}

	protected bool _touching;

	protected BoundingBox _collider;

	protected Vector3 _pos;

	protected Vector3 _v;

	protected bool _checkForOnGround = true;

	public bool ComputeOverlaps;

	protected Collision1D _cX = new Collision1D();

	protected Collision1D _cY = new Collision1D();

	protected Collision1D _cZ = new Collision1D();

	public void SetCollisionData(BlockCollisionData data, CollisionConfig collisionConfig, int hitboxIndex)
	{
		data.SetStart(CollisionPoint, CollisionStart);
		data.SetEnd(CollisionEnd, CollisionNormal);
		data.SetBlockData(collisionConfig);
		data.SetDetailBoxIndex(hitboxIndex);
		data.SetTouchingOverlapping(_touching, Overlapping);
	}

	public MovingBoxBoxCollisionEvaluator SetCollider(BoundingBox collider)
	{
		_collider = collider;
		return this;
	}

	public MovingBoxBoxCollisionEvaluator SetMove(Vector3 pos, Vector3 v)
	{
		_pos = pos;
		_v = v;
		_cX.V = v.X;
		_cY.V = v.Y;
		_cZ.V = v.Z;
		return this;
	}

	public bool IsBoundingBoxColliding(BoundingBox blockBoundingBox, float x, float y, float z)
	{
		_cX.P = _pos.X - x;
		_cY.P = _pos.Y - y;
		_cZ.P = _pos.Z - z;
		OnGround = false;
		_touching = false;
		Overlapping = false;
		if (!_cX.IsColliding(blockBoundingBox.Min.X - _collider.Max.X, blockBoundingBox.Max.X - _collider.Min.X))
		{
			return false;
		}
		if (!_cY.IsColliding(blockBoundingBox.Min.Y - _collider.Max.Y, blockBoundingBox.Max.Y - _collider.Min.Y))
		{
			return false;
		}
		if (!_cZ.IsColliding(blockBoundingBox.Min.Z - _collider.Max.Z, blockBoundingBox.Max.Z - _collider.Min.Z))
		{
			return false;
		}
		if (_cX.Kind == 1 && _cY.Kind == 1 && _cZ.Kind == 1)
		{
			Overlapping = true;
			if (!ComputeOverlaps)
			{
				return false;
			}
			CollisionStart = 0f;
			CollisionEnd = float.MaxValue;
			CollisionNormal = Vector3.Zero;
			if (_cX.TLeave < CollisionEnd)
			{
				CollisionEnd = _cX.TLeave;
				CollisionNormal = new Vector3(_cX.Normal, 0f, 0f);
			}
			if (_cY.TLeave < CollisionEnd)
			{
				CollisionEnd = _cY.TLeave;
				CollisionNormal = new Vector3(0f, _cY.Normal, 0f);
			}
			if (_cZ.TLeave < CollisionEnd)
			{
				CollisionEnd = _cZ.TLeave;
				CollisionNormal = new Vector3(0f, 0f, _cZ.Normal);
			}
			return true;
		}
		CollisionStart = float.MinValue;
		CollisionEnd = float.MaxValue;
		if (_cX.Kind == 0)
		{
			CollisionNormal = new Vector3(_cX.Normal, 0f, 0f);
			CollisionStart = _cX.TEnter;
		}
		if (_cY.Kind == 0 && _cY.TEnter > CollisionStart)
		{
			CollisionNormal = new Vector3(0f, _cY.Normal, 0f);
			CollisionStart = _cY.TEnter;
		}
		if (_cZ.Kind == 0 && _cZ.TEnter > CollisionStart)
		{
			CollisionNormal = new Vector3(0f, 0f, _cZ.Normal);
			CollisionStart = _cZ.TEnter;
		}
		if (CollisionStart > float.MinValue)
		{
			CollisionEnd = MathHelper.Min(_cX.TLeave, _cY.TLeave, _cZ.TLeave);
			if (CollisionStart > CollisionEnd)
			{
				return false;
			}
			CollisionPoint = _pos;
			CollisionPoint += _v * CollisionStart;
			if (_checkForOnGround && _cY.Kind == 3)
			{
				CollisionNormal = new Vector3(0f, _cY.Normal, 0f);
				OnGround = true;
				_touching = true;
				return false;
			}
			_touching = _cX.Kind >= 2 || _cY.Kind >= 2 || _cZ.Kind >= 2;
			return !_touching;
		}
		if (_checkForOnGround && _cY.Kind == 3)
		{
			CollisionStart = MathHelper.Max(_cX.TEnter, _cY.TEnter, _cZ.TEnter);
			CollisionEnd = MathHelper.Min(_cX.TLeave, _cY.TLeave, _cZ.TLeave);
			CollisionPoint = _pos;
			CollisionPoint += _v * CollisionStart;
			CollisionNormal = new Vector3(0f, _cY.Normal, 0f);
			OnGround = true;
			_touching = true;
		}
		return false;
	}
}
