namespace Epic.OnlineServices.Ecom;

public struct CopyLastRedeemedEntitlementByIndexOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public uint RedeemedEntitlementIndex { get; set; }
}
