namespace Epic.OnlineServices.Ecom;

public struct GetEntitlementsByNameCountOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String EntitlementName { get; set; }
}
