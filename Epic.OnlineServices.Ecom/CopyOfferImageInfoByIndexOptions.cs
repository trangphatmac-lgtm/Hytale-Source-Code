namespace Epic.OnlineServices.Ecom;

public struct CopyOfferImageInfoByIndexOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String OfferId { get; set; }

	public uint ImageInfoIndex { get; set; }
}
