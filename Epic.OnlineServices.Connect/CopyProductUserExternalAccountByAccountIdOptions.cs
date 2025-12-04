namespace Epic.OnlineServices.Connect;

public struct CopyProductUserExternalAccountByAccountIdOptions
{
	public ProductUserId TargetUserId { get; set; }

	public Utf8String AccountId { get; set; }
}
