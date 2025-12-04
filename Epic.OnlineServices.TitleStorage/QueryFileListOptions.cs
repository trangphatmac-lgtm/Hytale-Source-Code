namespace Epic.OnlineServices.TitleStorage;

public struct QueryFileListOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String[] ListOfTags { get; set; }
}
