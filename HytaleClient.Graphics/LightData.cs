using System;
using HytaleClient.Math;

namespace HytaleClient.Graphics;

public struct LightData
{
	public BoundingSphere Sphere;

	public Vector3 Color;

	public static float ComputeRadiusFromColor(Vector3 color)
	{
		Vector3 vector = color * 15f * 0.635f;
		return System.Math.Max(vector.X, System.Math.Max(vector.Y, vector.Z));
	}
}
