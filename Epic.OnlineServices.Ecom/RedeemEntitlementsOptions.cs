namespace Epic.OnlineServices.Ecom;

public struct RedeemEntitlementsOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String[] EntitlementIds { get; set; }
}
