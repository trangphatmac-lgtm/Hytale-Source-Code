namespace Epic.OnlineServices.TitleStorage;

public struct QueryFileOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String Filename { get; set; }
}
