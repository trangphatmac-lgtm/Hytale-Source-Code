namespace Epic.OnlineServices.UserInfo;

public struct GetExternalUserInfoCountOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }
}
