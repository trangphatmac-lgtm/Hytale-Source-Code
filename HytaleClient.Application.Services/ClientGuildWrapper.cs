using System.Collections.Generic;
using System.Linq;
using HytaleClient.Auth.Proto.Protocol;

namespace HytaleClient.Application.Services;

public class ClientGuildWrapper
{
	public string Name;

	public string GuildId;

	public ClientGuildMemberWrapper Leader;

	public List<ClientGuildMemberWrapper> Officers;

	public List<ClientGuildMemberWrapper> Members;

	public Dictionary<ClientGuildPermission, ClientGuildRank> Permissions;

	public ClientGuildWrapper(ClientGuild guild)
	{
		Name = guild.Name;
		GuildId = guild.GuildId;
		Leader = new ClientGuildMemberWrapper(guild.Leader);
		Officers = guild.Officers.Select((ClientGuildMember member) => new ClientGuildMemberWrapper(member)).ToList();
		Members = guild.Members.Select((ClientGuildMember member) => new ClientGuildMemberWrapper(member)).ToList();
		Permissions = guild.Permissions.Permissions;
	}
}
