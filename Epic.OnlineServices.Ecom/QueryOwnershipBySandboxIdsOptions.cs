namespace Epic.OnlineServices.Ecom;

public struct QueryOwnershipBySandboxIdsOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public Utf8String[] SandboxIds { get; set; }
}
