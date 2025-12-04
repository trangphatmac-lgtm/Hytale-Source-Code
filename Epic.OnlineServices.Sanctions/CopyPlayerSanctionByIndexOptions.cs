namespace Epic.OnlineServices.Sanctions;

public struct CopyPlayerSanctionByIndexOptions
{
	public ProductUserId TargetUserId { get; set; }

	public uint SanctionIndex { get; set; }
}
