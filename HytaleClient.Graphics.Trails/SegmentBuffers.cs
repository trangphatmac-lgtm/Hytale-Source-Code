using HytaleClient.Math;

namespace HytaleClient.Graphics.Trails;

internal struct SegmentBuffers : IFXDataStorage
{
	public int Count;

	public Vector3[] TrailPosition;

	public Quaternion[] Rotation;

	public float[] Length;

	public int[] Life;

	public void Initialize(int maxSegments)
	{
		Count = maxSegments;
		TrailPosition = new Vector3[maxSegments];
		Rotation = new Quaternion[maxSegments];
		Length = new float[maxSegments];
		Life = new int[maxSegments];
	}

	public void Release()
	{
	}
}
