namespace Epic.OnlineServices.Friends;

public struct RejectInviteOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }
}
