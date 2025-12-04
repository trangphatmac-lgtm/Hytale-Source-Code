using System;
using HytaleClient.Auth.Proto.Protocol;

namespace HytaleClient.Application.Services;

public class ClientPartyInvitationWrapper
{
	public readonly Guid By;

	public readonly string PartyIdHex;

	public readonly long ExpiresAt;

	public ClientPartyInvitationWrapper(Guid by, string partyIdHex, long expiresAt)
	{
		By = by;
		PartyIdHex = partyIdHex;
		ExpiresAt = expiresAt;
	}

	public ClientPartyInvitationWrapper(ClientPartyInvitation invitation)
	{
		By = invitation.InvitedBy.Uuid_;
		PartyIdHex = invitation.PartyIdHex;
		ExpiresAt = invitation.ExpiresAt;
	}
}
