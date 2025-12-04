namespace Epic.OnlineServices.Mods;

public struct UpdateModOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public ModIdentifier? Mod { get; set; }
}
