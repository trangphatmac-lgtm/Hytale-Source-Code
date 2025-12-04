namespace HytaleClient.Math;

public static class Ray_Hytale
{
	public static Vector3 GetAt(this Ray ray, float t)
	{
		return ray.Position + t * ray.Direction;
	}

	public static float Distance(this Ray ray, float t)
	{
		return ray.Direction.Length() * t;
	}
}
