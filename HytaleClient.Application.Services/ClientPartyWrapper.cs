using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Auth.Proto.Protocol;

namespace HytaleClient.Application.Services;

public class ClientPartyWrapper
{
	public Guid Leader;

	public List<Guid> Members;

	public string PartyId { get; }

	public ClientPartyWrapper(ClientParty party)
	{
		PartyId = party.PartyId;
		Leader = party.Leader.Uuid_;
		Members = party.Members.Select((ClientUser member) => member.Uuid_).ToList();
		if (!Members.Contains(Leader))
		{
			Members.Add(Leader);
		}
	}
}
