namespace Epic.OnlineServices.KWS;

public struct CreateUserCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public Utf8String KWSUserId { get; set; }

	public bool IsMinor { get; set; }

	public Result? GetResultCode()
	{
		return ResultCode;
	}

	internal void Set(ref CreateUserCallbackInfoInternal other)
	{
		ResultCode = other.ResultCode;
		ClientData = other.ClientData;
		LocalUserId = other.LocalUserId;
		KWSUserId = other.KWSUserId;
		IsMinor = other.IsMinor;
	}
}
