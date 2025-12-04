using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Items;

public class ClientItemPullbackConfig
{
	public Vector3? LeftOffsetOverride;

	public Vector3? LeftRotationOverride;

	public Vector3? RightOffsetOverride;

	public Vector3? RightRotationOverride;

	public ClientItemPullbackConfig(ItemPullbackConfiguration properties)
	{
		if (properties.LeftOffsetOverride != null)
		{
			LeftOffsetOverride = new Vector3(properties.LeftOffsetOverride.X, properties.LeftOffsetOverride.Y, properties.LeftOffsetOverride.Z);
		}
		if (properties.LeftRotationOverride != null)
		{
			LeftRotationOverride = new Vector3(properties.LeftRotationOverride.X, properties.LeftRotationOverride.Y, properties.LeftRotationOverride.Z);
		}
		if (properties.RightOffsetOverride != null)
		{
			RightOffsetOverride = new Vector3(properties.RightOffsetOverride.X, properties.RightOffsetOverride.Y, properties.RightOffsetOverride.Z);
		}
		if (properties.RightRotationOverride != null)
		{
			RightRotationOverride = new Vector3(properties.RightRotationOverride.X, properties.RightRotationOverride.Y, properties.RightRotationOverride.Z);
		}
	}

	public ClientItemPullbackConfig()
	{
	}
}
