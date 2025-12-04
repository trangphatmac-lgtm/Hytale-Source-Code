namespace Epic.OnlineServices.Friends;

public struct GetStatusOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }
}
