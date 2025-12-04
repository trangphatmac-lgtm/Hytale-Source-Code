namespace Epic.OnlineServices.Friends;

public struct AcceptInviteOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }
}
