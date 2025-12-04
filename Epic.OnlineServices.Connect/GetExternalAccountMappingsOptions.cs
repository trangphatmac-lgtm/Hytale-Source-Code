namespace Epic.OnlineServices.Connect;

public struct GetExternalAccountMappingsOptions
{
	public ProductUserId LocalUserId { get; set; }

	public ExternalAccountType AccountIdType { get; set; }

	public Utf8String TargetExternalUserId { get; set; }
}
