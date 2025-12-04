namespace Epic.OnlineServices.Ecom;

public struct CopyEntitlementByIdOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String EntitlementId { get; set; }
}
