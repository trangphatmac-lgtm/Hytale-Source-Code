namespace Epic.OnlineServices.PlayerDataStorage;

public struct DuplicateFileOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String SourceFilename { get; set; }

	public Utf8String DestinationFilename { get; set; }
}
