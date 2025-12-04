namespace Epic.OnlineServices.Stats;

public struct CopyStatByIndexOptions
{
	public ProductUserId TargetUserId { get; set; }

	public uint StatIndex { get; set; }
}
