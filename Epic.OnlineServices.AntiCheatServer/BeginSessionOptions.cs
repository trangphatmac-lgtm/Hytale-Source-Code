namespace Epic.OnlineServices.AntiCheatServer;

public struct BeginSessionOptions
{
	public uint RegisterTimeoutSeconds { get; set; }

	public Utf8String ServerName { get; set; }

	public bool EnableGameplayData { get; set; }

	public ProductUserId LocalUserId { get; set; }
}
