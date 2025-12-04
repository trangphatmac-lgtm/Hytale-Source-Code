using System;
using System.IO;
using Hypixel.ProtoPlus;
using HytaleClient.Auth.Proto.Protocol;

namespace HytaleClient.Application.Services;

public class ServerQueueUtil
{
	public static ProtoSerializable ReadResponseFrom(ClientServerQueueReply reply)
	{
		ProtoBinaryReader val = ProtoBinaryReader.Create((byte[])(object)reply.EncodedResponse);
		try
		{
			int num = ((BinaryReader)(object)val).ReadByte();
			return (ProtoSerializable)(num switch
			{
				0 => ClientServerQueueTicket.Supplier().Deserialize(val), 
				1 => ClientServerQueueFinal.Supplier().Deserialize(val), 
				2 => ClientServerQueueFailure.Supplier().Deserialize(val), 
				3 => ClientServerQueueStatus.Supplier().Deserialize(val), 
				4 => ClientServerQueueWorldTransfer.Supplier().Deserialize(val), 
				_ => throw new IOException("Unknown queue response " + num), 
			});
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}
}
