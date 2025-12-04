using HytaleClient.Math;

namespace HytaleClient.Data.BlockyModels;

public struct OrientationFrame
{
	public int Time;

	public Quaternion Delta;

	public string InterpolationType;
}
