namespace Epic.OnlineServices.Auth;

public struct QueryIdTokenOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetAccountId { get; set; }
}
