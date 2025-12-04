namespace Epic.OnlineServices.Connect;

public struct LinkAccountOptions
{
	public ProductUserId LocalUserId { get; set; }

	public ContinuanceToken ContinuanceToken { get; set; }
}
