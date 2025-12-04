using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Collision;

public static class Raycast
{
	public struct Result
	{
		public Ray Ray;

		public float NearT;

		public IntVector3 Normal;

		public Vector2 TextureCoord;

		public static Result Default { get; } = new Result
		{
			Ray = default(Ray),
			NearT = float.PositiveInfinity,
			Normal = IntVector3.Zero,
			TextureCoord = Vector2.Zero
		};


		public bool IsSuccess()
		{
			return !float.IsPositiveInfinity(NearT);
		}

		public Vector3 GetTarget()
		{
			return Ray.GetAt(NearT);
		}

		public float Distance()
		{
			return Ray.Direction.Length() * NearT;
		}
	}

	public struct Options
	{
		public static Options Default = new Options
		{
			Distance = 128f,
			Epsilon = 0f
		};

		public const float DefaultDistance = 128f;

		public const float DefaultEpsilon = 0f;

		public float Distance;

		public float Epsilon;

		public Options(float distance)
		{
			this = Default;
			Distance = distance;
		}

		public void SetBidirectional(bool state)
		{
			Epsilon = (state ? float.NegativeInfinity : 0f);
		}
	}

	public static bool RaycastBox(Ray ray, BoundingBox box, ref Result result, ref Options options)
	{
		return RaycastBox(ray, box.Min, box.Max, ref result, ref options);
	}

	public static bool RaycastBox(Ray ray, BoundingBox box, Vector3 offset, ref Result result, ref Options options)
	{
		Vector3 min = box.Min + offset;
		Vector3 max = box.Max + offset;
		return RaycastBox(ray, min, max, ref result, ref options);
	}

	public static bool RaycastBox(Ray ray, Vector3 min, Vector3 max, ref Result result, ref Options options)
	{
		Vector3 position = ray.Position;
		Vector3 direction = ray.Direction;
		bool flag = false;
		float num = (min.X - position.X) / direction.X;
		if (num < result.NearT && num > options.Epsilon)
		{
			float num2 = position.Z + direction.Z * num;
			float num3 = position.Y + direction.Y * num;
			if (num2 >= min.Z && num2 <= max.Z && num3 >= min.Y && num3 <= max.Y)
			{
				result.NearT = num;
				result.TextureCoord.X = num2;
				result.TextureCoord.Y = num3;
				result.Normal = IntVector3.Left;
				flag = true;
			}
		}
		num = (max.X - position.X) / direction.X;
		if (num < result.NearT && num > options.Epsilon)
		{
			float num2 = position.Z + direction.Z * num;
			float num3 = position.Y + direction.Y * num;
			if (num2 >= min.Z && num2 <= max.Z && num3 >= min.Y && num3 <= max.Y)
			{
				result.NearT = num;
				result.TextureCoord.X = num2;
				result.TextureCoord.Y = num3;
				result.Normal = IntVector3.Right;
				flag = true;
			}
		}
		num = (min.Y - position.Y) / direction.Y;
		if (num < result.NearT && num > options.Epsilon)
		{
			float num2 = position.X + direction.X * num;
			float num3 = position.Z + direction.Z * num;
			if (num2 >= min.X && num2 <= max.X && num3 >= min.Z && num3 <= max.Z)
			{
				result.NearT = num;
				result.TextureCoord.X = num2;
				result.TextureCoord.Y = num3;
				result.Normal = IntVector3.Down;
				flag = true;
			}
		}
		num = (max.Y - position.Y) / direction.Y;
		if (num < result.NearT && num > options.Epsilon)
		{
			float num2 = position.X + direction.X * num;
			float num3 = position.Z + direction.Z * num;
			if (num2 >= min.X && num2 <= max.X && num3 >= min.Z && num3 <= max.Z)
			{
				result.NearT = num;
				result.TextureCoord.X = num2;
				result.TextureCoord.Y = num3;
				result.Normal = IntVector3.Up;
				flag = true;
			}
		}
		num = (min.Z - position.Z) / direction.Z;
		if (num < result.NearT && num > options.Epsilon)
		{
			float num2 = position.X + direction.X * num;
			float num3 = position.Y + direction.Y * num;
			if (num2 >= min.X && num2 <= max.X && num3 >= min.Y && num3 <= max.Y)
			{
				result.NearT = num;
				result.TextureCoord.X = num2;
				result.TextureCoord.Y = num3;
				result.Normal = IntVector3.Forward;
				flag = true;
			}
		}
		num = (max.Z - position.Z) / direction.Z;
		if (num < result.NearT && num > options.Epsilon)
		{
			float num2 = position.X + direction.X * num;
			float num3 = position.Y + direction.Y * num;
			if (num2 >= min.X && num2 <= max.X && num3 >= min.Y && num3 <= max.Y)
			{
				result.NearT = num;
				result.TextureCoord.X = num2;
				result.TextureCoord.Y = num3;
				result.Normal = IntVector3.Backward;
				flag = true;
			}
		}
		if (flag)
		{
			result.Ray = ray;
		}
		return flag;
	}

	public static bool RaycastBox(Ray ray, BoundingBox box, ref Result result)
	{
		return RaycastBox(ray, box, ref result, ref Options.Default);
	}

	public static bool RaycastBox(Ray ray, BoundingBox box, Vector3 offset, ref Result result)
	{
		return RaycastBox(ray, box, offset, ref result, ref Options.Default);
	}

	public static bool RaycastBoxSingle(Ray ray, BoundingBox box, out Result result)
	{
		result = Result.Default;
		return RaycastBox(ray, box, ref result, ref Options.Default);
	}

	public static bool RaycastBoxSingle(Ray ray, BoundingBox box, Vector3 offset, out Result result)
	{
		result = Result.Default;
		return RaycastBox(ray, box, offset, ref result, ref Options.Default);
	}

	public static bool RaycastBoxSingle(Ray ray, BoundingBox box, out Result result, ref Options options)
	{
		result = Result.Default;
		return RaycastBox(ray, box, ref result, ref options);
	}

	public static bool RaycastBoxSingle(Ray ray, BoundingBox box, Vector3 offset, out Result result, ref Options options)
	{
		result = Result.Default;
		return RaycastBox(ray, box, offset, ref result, ref options);
	}
}
