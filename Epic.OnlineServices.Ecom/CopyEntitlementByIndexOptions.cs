namespace Epic.OnlineServices.Ecom;

public struct CopyEntitlementByIndexOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public uint EntitlementIndex { get; set; }
}
