namespace Epic.OnlineServices.UserInfo;

public struct CopyBestDisplayNameWithPlatformOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }

	public uint TargetPlatformType { get; set; }
}
