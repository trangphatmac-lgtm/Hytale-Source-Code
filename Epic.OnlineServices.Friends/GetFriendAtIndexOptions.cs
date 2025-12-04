namespace Epic.OnlineServices.Friends;

public struct GetFriendAtIndexOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public int Index { get; set; }
}
