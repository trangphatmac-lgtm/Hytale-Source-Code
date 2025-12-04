namespace Epic.OnlineServices.Ecom;

public struct GetItemReleaseCountOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String ItemId { get; set; }
}
