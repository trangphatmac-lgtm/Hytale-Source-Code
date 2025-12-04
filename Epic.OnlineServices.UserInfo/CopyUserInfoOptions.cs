namespace Epic.OnlineServices.UserInfo;

public struct CopyUserInfoOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }
}
