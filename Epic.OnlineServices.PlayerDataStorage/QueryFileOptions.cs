namespace Epic.OnlineServices.PlayerDataStorage;

public struct QueryFileOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String Filename { get; set; }
}
