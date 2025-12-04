namespace Epic.OnlineServices.UserInfo;

public struct QueryUserInfoByExternalAccountOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String ExternalAccountId { get; set; }

	public ExternalAccountType AccountType { get; set; }
}
