namespace Epic.OnlineServices.PlayerDataStorage;

public struct DeleteFileOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String Filename { get; set; }
}
