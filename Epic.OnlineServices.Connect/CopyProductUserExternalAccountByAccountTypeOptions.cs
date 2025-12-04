namespace Epic.OnlineServices.Connect;

public struct CopyProductUserExternalAccountByAccountTypeOptions
{
	public ProductUserId TargetUserId { get; set; }

	public ExternalAccountType AccountIdType { get; set; }
}
