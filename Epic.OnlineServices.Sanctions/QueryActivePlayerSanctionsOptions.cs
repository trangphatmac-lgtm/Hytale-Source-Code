namespace Epic.OnlineServices.Sanctions;

public struct QueryActivePlayerSanctionsOptions
{
	public ProductUserId TargetUserId { get; set; }

	public ProductUserId LocalUserId { get; set; }
}
