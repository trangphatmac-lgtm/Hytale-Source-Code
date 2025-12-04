namespace Epic.OnlineServices.Platform;

public enum DesktopCrossplayStatus
{
	Ok,
	ApplicationNotBootstrapped,
	ServiceNotInstalled,
	ServiceStartFailed,
	ServiceNotRunning,
	OverlayDisabled,
	OverlayNotInstalled,
	OverlayTrustCheckFailed,
	OverlayLoadFailed
}
