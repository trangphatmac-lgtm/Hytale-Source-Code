namespace Epic.OnlineServices.Ecom;

public struct QueryOffersOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String OverrideCatalogNamespace { get; set; }
}
