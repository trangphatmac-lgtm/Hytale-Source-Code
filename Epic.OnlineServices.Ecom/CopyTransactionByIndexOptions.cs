namespace Epic.OnlineServices.Ecom;

public struct CopyTransactionByIndexOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public uint TransactionIndex { get; set; }
}
