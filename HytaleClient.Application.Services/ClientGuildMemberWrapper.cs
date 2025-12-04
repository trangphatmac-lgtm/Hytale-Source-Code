using System;
using HytaleClient.Auth.Proto.Protocol;

namespace HytaleClient.Application.Services;

public class ClientGuildMemberWrapper
{
	public Guid Uuid;

	public ClientGuildRank Rank;

	public ClientGuildMemberWrapper(ClientGuildMember guildMember)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		Uuid = guildMember.User.Uuid_;
		Rank = guildMember.Rank;
	}

	public ClientGuildMemberWrapper(Guid uuid, ClientGuildRank rank)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		Uuid = uuid;
		Rank = rank;
	}
}
