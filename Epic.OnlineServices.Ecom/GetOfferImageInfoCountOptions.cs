namespace Epic.OnlineServices.Ecom;

public struct GetOfferImageInfoCountOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String OfferId { get; set; }
}
