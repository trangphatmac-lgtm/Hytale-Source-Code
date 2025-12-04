namespace Epic.OnlineServices.Connect;

public struct IdToken
{
	public ProductUserId ProductUserId { get; set; }

	public Utf8String JsonWebToken { get; set; }

	internal void Set(ref IdTokenInternal other)
	{
		ProductUserId = other.ProductUserId;
		JsonWebToken = other.JsonWebToken;
	}
}
