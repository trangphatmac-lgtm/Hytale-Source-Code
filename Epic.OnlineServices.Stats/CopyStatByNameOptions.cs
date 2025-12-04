namespace Epic.OnlineServices.Stats;

public struct CopyStatByNameOptions
{
	public ProductUserId TargetUserId { get; set; }

	public Utf8String Name { get; set; }
}
