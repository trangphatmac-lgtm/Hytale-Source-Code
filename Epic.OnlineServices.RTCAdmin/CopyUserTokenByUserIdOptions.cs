namespace Epic.OnlineServices.RTCAdmin;

public struct CopyUserTokenByUserIdOptions
{
	public ProductUserId TargetUserId { get; set; }

	public uint QueryId { get; set; }
}
