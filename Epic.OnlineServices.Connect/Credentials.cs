namespace Epic.OnlineServices.Connect;

public struct Credentials
{
	public Utf8String Token { get; set; }

	public ExternalCredentialType Type { get; set; }

	internal void Set(ref CredentialsInternal other)
	{
		Token = other.Token;
		Type = other.Type;
	}
}
