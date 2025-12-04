namespace Epic.OnlineServices.Ecom;

public struct QueryEntitlementTokenOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String[] EntitlementNames { get; set; }
}
