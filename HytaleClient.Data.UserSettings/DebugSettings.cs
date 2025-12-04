using HytaleClient.Protocol;

namespace HytaleClient.Data.UserSettings;

public class DebugSettings
{
	public bool ShowDebugMarkers = false;

	public DebugSettings Clone()
	{
		return new DebugSettings
		{
			ShowDebugMarkers = ShowDebugMarkers
		};
	}

	public SyncPlayerPreferences CreatePacket()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		return new SyncPlayerPreferences(ShowDebugMarkers);
	}
}
