namespace Epic.OnlineServices.Mods;

public struct InstallModOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public ModIdentifier? Mod { get; set; }

	public bool RemoveAfterExit { get; set; }
}
