namespace Epic.OnlineServices.Ecom;

public struct CopyItemReleaseByIndexOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String ItemId { get; set; }

	public uint ReleaseIndex { get; set; }
}
