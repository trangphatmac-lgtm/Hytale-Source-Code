namespace Epic.OnlineServices.CustomInvites;

public struct SendCustomInviteOptions
{
	public ProductUserId LocalUserId { get; set; }

	public ProductUserId[] TargetUserIds { get; set; }
}
