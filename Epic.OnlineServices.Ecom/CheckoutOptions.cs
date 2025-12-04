namespace Epic.OnlineServices.Ecom;

public struct CheckoutOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String OverrideCatalogNamespace { get; set; }

	public CheckoutEntry[] Entries { get; set; }

	public CheckoutOrientation PreferredOrientation { get; set; }
}
