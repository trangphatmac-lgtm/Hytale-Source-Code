namespace HytaleClient.Graphics.Gizmos.Models;

public class CameraModel
{
	private static readonly float[] Vertices = new float[128]
	{
		-0.5f, -0.25f, -0.25f, 0f, 0f, 0f, 0f, 0f, -0.5f, 0.25f,
		-0.25f, 0f, 0f, 0f, 0f, 0f, 0.25f, 0.25f, -0.25f, 0f,
		0f, 0f, 0f, 0f, 0.25f, -0.25f, -0.25f, 0f, 0f, 0f,
		0f, 0f, -0.5f, -0.25f, 0.25f, 0f, 0f, 0f, 0f, 0f,
		-0.5f, 0.25f, 0.25f, 0f, 0f, 0f, 0f, 0f, 0.25f, 0.25f,
		0.25f, 0f, 0f, 0f, 0f, 0f, 0.25f, -0.25f, 0.25f, 0f,
		0f, 0f, 0f, 0f, 0.25f, -0.125f, -0.125f, 0f, 0f, 0f,
		0f, 0f, 0.25f, 0.125f, -0.125f, 0f, 0f, 0f, 0f, 0f,
		0.5f, 0.125f, -0.125f, 0f, 0f, 0f, 0f, 0f, 0.5f, -0.125f,
		-0.125f, 0f, 0f, 0f, 0f, 0f, 0.25f, -0.125f, 0.125f, 0f,
		0f, 0f, 0f, 0f, 0.25f, 0.125f, 0.125f, 0f, 0f, 0f,
		0f, 0f, 0.5f, 0.125f, 0.125f, 0f, 0f, 0f, 0f, 0f,
		0.5f, -0.125f, 0.125f, 0f, 0f, 0f, 0f, 0f
	};

	private static readonly ushort[] Indices = new ushort[48]
	{
		0, 1, 1, 2, 2, 3, 3, 0, 4, 5,
		5, 6, 6, 7, 7, 4, 0, 4, 1, 5,
		2, 6, 3, 7, 8, 9, 9, 10, 10, 11,
		11, 8, 12, 13, 13, 14, 14, 15, 15, 12,
		8, 12, 9, 13, 10, 14, 11, 15
	};

	public static PrimitiveModelData BuildModelData()
	{
		return new PrimitiveModelData(Vertices, Indices);
	}
}
