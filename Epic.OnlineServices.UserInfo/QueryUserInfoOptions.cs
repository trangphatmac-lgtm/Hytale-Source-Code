namespace Epic.OnlineServices.UserInfo;

public struct QueryUserInfoOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }
}
