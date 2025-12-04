using System;

namespace Epic.OnlineServices.RTCData;

public struct SendDataOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }

	public ArraySegment<byte> Data { get; set; }
}
