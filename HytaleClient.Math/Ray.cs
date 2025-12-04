using System;
using System.Diagnostics;

namespace HytaleClient.Math;

[Serializable]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct Ray : IEquatable<Ray>
{
	public Vector3 Position;

	public Vector3 Direction;

	internal string DebugDisplayString => "Pos( " + Position.DebugDisplayString + " ) \r\n" + "Dir( " + Direction.DebugDisplayString + " )";

	public Ray(Vector3 position, Vector3 direction)
	{
		Position = position;
		Direction = direction;
	}

	public override bool Equals(object obj)
	{
		return obj is Ray && Equals((Ray)obj);
	}

	public bool Equals(Ray other)
	{
		return Position.Equals(other.Position) && Direction.Equals(other.Direction);
	}

	public override int GetHashCode()
	{
		return Position.GetHashCode() ^ Direction.GetHashCode();
	}

	public float? Intersects(BoundingBox box)
	{
		float? num = null;
		float? num2 = null;
		if (MathHelper.WithinEpsilon(Direction.X, 0f))
		{
			if (Position.X < box.Min.X || Position.X > box.Max.X)
			{
				return null;
			}
		}
		else
		{
			num = (box.Min.X - Position.X) / Direction.X;
			num2 = (box.Max.X - Position.X) / Direction.X;
			if (num > num2)
			{
				float? num3 = num;
				num = num2;
				num2 = num3;
			}
		}
		if (MathHelper.WithinEpsilon(Direction.Y, 0f))
		{
			if (Position.Y < box.Min.Y || Position.Y > box.Max.Y)
			{
				return null;
			}
		}
		else
		{
			float num4 = (box.Min.Y - Position.Y) / Direction.Y;
			float num5 = (box.Max.Y - Position.Y) / Direction.Y;
			if (num4 > num5)
			{
				float num6 = num4;
				num4 = num5;
				num5 = num6;
			}
			if ((num.HasValue && num > num5) || (num2.HasValue && num4 > num2))
			{
				return null;
			}
			if (!num.HasValue || num4 > num)
			{
				num = num4;
			}
			if (!num2.HasValue || num5 < num2)
			{
				num2 = num5;
			}
		}
		if (MathHelper.WithinEpsilon(Direction.Z, 0f))
		{
			if (Position.Z < box.Min.Z || Position.Z > box.Max.Z)
			{
				return null;
			}
		}
		else
		{
			float num7 = (box.Min.Z - Position.Z) / Direction.Z;
			float num8 = (box.Max.Z - Position.Z) / Direction.Z;
			if (num7 > num8)
			{
				float num9 = num7;
				num7 = num8;
				num8 = num9;
			}
			if ((num.HasValue && num > num8) || (num2.HasValue && num7 > num2))
			{
				return null;
			}
			if (!num.HasValue || num7 > num)
			{
				num = num7;
			}
			if (!num2.HasValue || num8 < num2)
			{
				num2 = num8;
			}
		}
		if (num.HasValue && num < 0f && num2 > 0f)
		{
			return 0f;
		}
		if (num < 0f)
		{
			return null;
		}
		return num;
	}

	public void Intersects(ref BoundingBox box, out float? result)
	{
		result = Intersects(box);
	}

	public float? Intersects(BoundingSphere sphere)
	{
		Intersects(ref sphere, out var result);
		return result;
	}

	public float? Intersects(Plane plane)
	{
		Intersects(ref plane, out var result);
		return result;
	}

	public float? Intersects(BoundingFrustum frustum)
	{
		frustum.Intersects(ref this, out var result);
		return result;
	}

	public void Intersects(ref Plane plane, out float? result)
	{
		float num = Vector3.Dot(Direction, plane.Normal);
		if (System.Math.Abs(num) < 1E-05f)
		{
			result = null;
			return;
		}
		result = (0f - plane.D - Vector3.Dot(plane.Normal, Position)) / num;
		if (result < 0f)
		{
			if (result < -1E-05f)
			{
				result = null;
			}
			else
			{
				result = 0f;
			}
		}
	}

	public void Intersects(ref BoundingSphere sphere, out float? result)
	{
		Vector3 vector = sphere.Center - Position;
		float num = vector.LengthSquared();
		float num2 = sphere.Radius * sphere.Radius;
		if (num < num2)
		{
			result = 0f;
			return;
		}
		Vector3.Dot(ref Direction, ref vector, out var result2);
		if (result2 < 0f)
		{
			result = null;
			return;
		}
		float num3 = num2 + result2 * result2 - num;
		result = ((num3 < 0f) ? null : new float?(result2 - (float)System.Math.Sqrt(num3)));
	}

	public static bool operator !=(Ray a, Ray b)
	{
		return !a.Equals(b);
	}

	public static bool operator ==(Ray a, Ray b)
	{
		return a.Equals(b);
	}

	public override string ToString()
	{
		return "{{Position:" + Position.ToString() + " Direction:" + Direction.ToString() + "}}";
	}
}
