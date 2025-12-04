namespace Epic.OnlineServices.Connect;

public struct LoginCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public ContinuanceToken ContinuanceToken { get; set; }

	public Result? GetResultCode()
	{
		return ResultCode;
	}

	internal void Set(ref LoginCallbackInfoInternal other)
	{
		ResultCode = other.ResultCode;
		ClientData = other.ClientData;
		LocalUserId = other.LocalUserId;
		ContinuanceToken = other.ContinuanceToken;
	}
}
