using System;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Collision;

public struct BlockIterator
{
	private const int MaxIterations = 5000;

	private readonly Vector3 _direction;

	private readonly float _maxDistance;

	private IntVector3 _block;

	private Vector3 _position;

	private float _t;

	private int _iterations;

	public BlockIterator(Ray ray, float maxDistance)
		: this(ray.Position, ray.Direction, maxDistance)
	{
	}

	public BlockIterator(Vector3 origin, Vector3 direction, float maxDistance)
	{
		IntVector3 block = new IntVector3(origin);
		this = new BlockIterator(offset: new Vector3(origin.X - (float)block.X, origin.Y - (float)block.Y, origin.Z - (float)block.Z), block: block, direction: direction, maxDistance: maxDistance);
	}

	public BlockIterator(IntVector3 block, Vector3 offset, Vector3 direction, float maxDistance)
	{
		if (offset.X < 0f || offset.Y < 0f || offset.Z < 0f || offset.X > 1f || offset.Y > 1f || offset.Z > 1f)
		{
			Vector3 vector = offset;
			throw new Exception("Offset is out of bounds 0 <= ? <= 1! Given: " + vector.ToString());
		}
		_block = block;
		_direction = Vector3.Normalize(direction);
		_maxDistance = maxDistance;
		_position = offset;
		_t = 0f;
		_iterations = 0;
	}

	public bool HasNext()
	{
		return _t <= _maxDistance || _iterations > 5000;
	}

	public void Step(out IntVector3 b, out Vector3 p, out Vector3 q, out IntVector3 n)
	{
		float num = Intersection();
		b = _block;
		p = _position;
		q = _position + num * _direction;
		n = IntVector3.Zero;
		if (_direction.X < 0f && q.X <= 0f)
		{
			q.X += 1f;
			n.X = 1;
			_block.X--;
		}
		else if (_direction.X > 0f && q.X >= 1f)
		{
			q.X -= 1f;
			n.X = -1;
			_block.X++;
		}
		if (_direction.Y < 0f && q.Y <= 0f)
		{
			q.Y += 1f;
			n.Y = 1;
			_block.Y--;
		}
		else if (_direction.Y > 0f && q.Y >= 1f)
		{
			q.Y -= 1f;
			n.Y = -1;
			_block.Y++;
		}
		if (_direction.Z < 0f && q.Z <= 0f)
		{
			q.Z += 1f;
			n.Z = 1;
			_block.Z--;
		}
		else if (_direction.Z > 0f && q.Z >= 1f)
		{
			q.Z -= 1f;
			n.Z = -1;
			_block.Z++;
		}
		_t += num;
		_position = q;
		_iterations++;
	}

	private float Intersection()
	{
		Vector3 position = _position;
		Vector3 direction = _direction;
		float num = 0f;
		if (direction.X < 0f)
		{
			float num2 = (0f - position.X) / direction.X;
			float num3 = position.Z + direction.Z * num2;
			float num4 = position.Y + direction.Y * num2;
			if (num2 > num && num3 >= 0f && num3 <= 1f && num4 >= 0f && num4 <= 1f)
			{
				num = num2;
			}
		}
		else if (direction.X > 0f)
		{
			float num2 = (1f - position.X) / direction.X;
			float num3 = position.Z + direction.Z * num2;
			float num4 = position.Y + direction.Y * num2;
			if (num2 > num && num3 >= 0f && num3 <= 1f && num4 >= 0f && num4 <= 1f)
			{
				num = num2;
			}
		}
		if (direction.Y < 0f)
		{
			float num2 = (0f - position.Y) / direction.Y;
			float num3 = position.X + direction.X * num2;
			float num4 = position.Z + direction.Z * num2;
			if (num2 > num && num3 >= 0f && num3 <= 1f && num4 >= 0f && num4 <= 1f)
			{
				num = num2;
			}
		}
		else if (direction.Y > 0f)
		{
			float num2 = (1f - position.Y) / direction.Y;
			float num3 = position.X + direction.X * num2;
			float num4 = position.Z + direction.Z * num2;
			if (num2 > num && num3 >= 0f && num3 <= 1f && num4 >= 0f && num4 <= 1f)
			{
				num = num2;
			}
		}
		if (direction.Z < 0f)
		{
			float num2 = (0f - position.Z) / direction.Z;
			float num3 = position.X + direction.X * num2;
			float num4 = position.Y + direction.Y * num2;
			if (num2 > num && num3 >= 0f && num3 <= 1f && num4 >= 0f && num4 <= 1f)
			{
				num = num2;
			}
		}
		else if (direction.Z > 0f)
		{
			float num2 = (1f - position.Z) / direction.Z;
			float num3 = position.X + direction.X * num2;
			float num4 = position.Y + direction.Y * num2;
			if (num2 > num && num3 >= 0f && num3 <= 1f && num4 >= 0f && num4 <= 1f)
			{
				num = num2;
			}
		}
		return num;
	}
}
