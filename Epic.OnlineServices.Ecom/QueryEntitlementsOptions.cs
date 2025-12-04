namespace Epic.OnlineServices.Ecom;

public struct QueryEntitlementsOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String[] EntitlementNames { get; set; }

	public bool IncludeRedeemed { get; set; }

	public Utf8String OverrideCatalogNamespace { get; set; }
}
