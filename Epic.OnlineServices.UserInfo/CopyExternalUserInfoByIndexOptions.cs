namespace Epic.OnlineServices.UserInfo;

public struct CopyExternalUserInfoByIndexOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }

	public uint Index { get; set; }
}
