namespace Epic.OnlineServices.Ecom;

public struct GetOfferItemCountOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String OfferId { get; set; }
}
