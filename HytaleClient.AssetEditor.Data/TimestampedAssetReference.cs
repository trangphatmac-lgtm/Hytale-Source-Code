using HytaleClient.Protocol;

namespace HytaleClient.AssetEditor.Data;

public class TimestampedAssetReference
{
	public readonly string Path;

	public readonly string Timestamp;

	public TimestampedAssetReference(string path, string timestamp)
	{
		Path = path;
		Timestamp = timestamp;
	}

	public TimestampedAssetReference ToPacket()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		return new TimestampedAssetReference(Path, Timestamp);
	}
}
