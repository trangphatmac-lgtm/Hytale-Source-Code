namespace Epic.OnlineServices.Ecom;

public struct QueryOwnershipOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String[] CatalogItemIds { get; set; }

	public Utf8String CatalogNamespace { get; set; }
}
