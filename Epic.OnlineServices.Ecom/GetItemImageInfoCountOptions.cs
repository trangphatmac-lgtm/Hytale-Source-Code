namespace Epic.OnlineServices.Ecom;

public struct GetItemImageInfoCountOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String ItemId { get; set; }
}
