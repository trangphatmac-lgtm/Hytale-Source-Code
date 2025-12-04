namespace Epic.OnlineServices.UserInfo;

public struct CopyExternalUserInfoByAccountTypeOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }

	public ExternalAccountType AccountType { get; set; }
}
