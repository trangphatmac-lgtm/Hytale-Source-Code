namespace Epic.OnlineServices.TitleStorage;

public struct CopyFileMetadataAtIndexOptions
{
	public ProductUserId LocalUserId { get; set; }

	public uint Index { get; set; }
}
