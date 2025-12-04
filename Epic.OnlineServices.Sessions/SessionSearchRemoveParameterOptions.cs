namespace Epic.OnlineServices.Sessions;

public struct SessionSearchRemoveParameterOptions
{
	public Utf8String Key { get; set; }

	public ComparisonOp ComparisonOp { get; set; }
}
