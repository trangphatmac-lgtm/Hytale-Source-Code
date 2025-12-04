using HytaleClient.Math;

namespace HytaleClient.Data.BlockyModels;

public struct BlockyModelNodeJson
{
	public string Name;

	public Vector3 Position;

	public Quaternion Orientation;

	public NodeShape Shape;

	public BlockyModelNodeJson[] Children;
}
