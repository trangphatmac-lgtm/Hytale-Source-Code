namespace Epic.OnlineServices.PlayerDataStorage;

public struct CopyFileMetadataAtIndexOptions
{
	public ProductUserId LocalUserId { get; set; }

	public uint Index { get; set; }
}
