namespace Epic.OnlineServices.UserInfo;

public struct CopyBestDisplayNameOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }
}
