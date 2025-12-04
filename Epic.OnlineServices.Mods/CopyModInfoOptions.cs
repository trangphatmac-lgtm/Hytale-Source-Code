namespace Epic.OnlineServices.Mods;

public struct CopyModInfoOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public ModEnumerationType Type { get; set; }
}
