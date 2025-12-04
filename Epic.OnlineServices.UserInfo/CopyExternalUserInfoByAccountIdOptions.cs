namespace Epic.OnlineServices.UserInfo;

public struct CopyExternalUserInfoByAccountIdOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }

	public Utf8String AccountId { get; set; }
}
