namespace Epic.OnlineServices.P2P;

public struct AddNotifyPeerConnectionRequestOptions
{
	public ProductUserId LocalUserId { get; set; }

	public SocketId? SocketId { get; set; }
}
