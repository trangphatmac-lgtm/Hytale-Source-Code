namespace Epic.OnlineServices.Connect;

public struct LoginStatusChangedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public LoginStatus PreviousStatus { get; set; }

	public LoginStatus CurrentStatus { get; set; }

	public Result? GetResultCode()
	{
		return null;
	}

	internal void Set(ref LoginStatusChangedCallbackInfoInternal other)
	{
		ClientData = other.ClientData;
		LocalUserId = other.LocalUserId;
		PreviousStatus = other.PreviousStatus;
		CurrentStatus = other.CurrentStatus;
	}
}
